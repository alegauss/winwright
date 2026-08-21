using Winwright.Acting;
using Winwright.Projects;
using Winwright.Tracing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW30. One walk and one read is a coin toss against a shell that drops synthesised input: three
/// runs in ten reported a submenu that did not expand, against a build with nothing wrong with it,
/// wearing the wording of a real defect.
/// </summary>
public class RetryTests
{
    /// <summary>Fails the first <paramref name="times"/> attempts and then works, deterministically.</summary>
    private static Func<string> FlakyFor(int times)
    {
        var made = 0;
        return () => ++made > times ? "expanded" : "nothing";
    }

    private static bool Worked(string answer) => answer == "expanded";

    [Fact]
    public void Something_that_works_first_time_is_attempted_once()
    {
        var attempted = Retry.Bounded(FlakyFor(0), Worked);

        Assert.True(attempted.Succeeded);
        Assert.Equal(1, attempted.Attempts);
        Assert.False(attempted.NeededMoreThanOne);
        Assert.Equal("worked first time.", attempted.ToString());
    }

    [Fact]
    public void Something_flaky_is_attempted_again_and_the_count_is_kept()
    {
        var attempted = Retry.Bounded(FlakyFor(2), Worked);

        Assert.True(attempted.Succeeded);
        Assert.Equal(3, attempted.Attempts);
        Assert.True(attempted.NeededMoreThanOne);
        Assert.Equal("worked on attempt 3 of 3.", attempted.ToString());
    }

    [Fact]
    public void Something_that_genuinely_stopped_working_still_goes_red()
    {
        var attempted = Retry.Bounded(FlakyFor(99), Worked);

        Assert.False(attempted.Succeeded);
        Assert.Equal(3, attempted.Attempts);
        Assert.Equal("did not work in 3 attempts.", attempted.ToString());
    }

    [Fact]
    public void The_attempts_stop_at_the_cap_and_never_at_the_first_green()
    {
        var made = 0;
        Retry.Bounded(
            () =>
            {
                made++;
                return "nothing";
            },
            Worked,
            cap: 4);

        Assert.Equal(4, made);
    }

    [Fact]
    public void An_attempt_after_the_first_success_is_never_made()
    {
        var made = 0;
        Retry.Bounded(
            () =>
            {
                made++;
                return "expanded";
            },
            Worked,
            cap: 5);

        Assert.Equal(1, made);
    }

    [Fact]
    public void There_is_no_form_of_this_without_a_cap()
    {
        var uncapped = typeof(Retry).GetMethods()
            .Where(method => method.Name == nameof(Retry.Bounded))
            .Where(method => !method.GetParameters().Any(parameter => parameter.Name == "cap"));

        Assert.Empty(uncapped);
    }

    [Fact]
    public void A_cap_big_enough_to_hide_a_failure_is_refused_by_name()
    {
        var refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Retry.Bounded(FlakyFor(0), Worked, cap: Retry.MostAttempts + 1));

        Assert.Contains("is not a cap", refusal.Message);
        Assert.Contains("nobody will ever see fail", refusal.Message);
    }

    [Fact]
    public void A_cap_of_nothing_is_refused_too()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Retry.Bounded(FlakyFor(0), Worked, cap: 0));
    }

    [Fact]
    public void The_count_reaches_the_record()
    {
        var step = new TraceStep { Verb = "expand", Locator = "MenuItem#recent", Verdict = StepVerdict.Ok };

        var recorded = Retry.Recorded(step, Retry.Bounded(FlakyFor(2), Worked));

        Assert.Equal(3, recorded.Attempts);
        Assert.Equal("worked on attempt 3 of 3.", recorded.Detail);
        Assert.Contains("\"attempts\":3", TraceFormat.Line(recorded));
    }

    [Fact]
    public void A_green_that_took_one_attempt_carries_no_note_about_it()
    {
        var step = new TraceStep { Verb = "expand", Locator = "MenuItem#recent", Verdict = StepVerdict.Ok };

        var recorded = Retry.Recorded(step, Retry.Bounded(FlakyFor(0), Worked));

        Assert.Equal(1, recorded.Attempts);
        Assert.Null(recorded.Detail);
    }

    [Fact]
    public void A_step_that_already_had_something_to_say_keeps_it()
    {
        var step = new TraceStep
        {
            Verb = "expand",
            Locator = "MenuItem#recent",
            Verdict = StepVerdict.Failed,
            Detail = "the submenu never arrived",
        };

        var recorded = Retry.Recorded(step, Retry.Bounded(FlakyFor(1), Worked));

        Assert.Equal("the submenu never arrived (worked on attempt 2 of 3.)", recorded.Detail);
    }

    [Fact]
    public void Every_step_says_it_was_attempted_at_least_once()
    {
        var step = new TraceStep { Verb = "click", Locator = "#save", Verdict = StepVerdict.Ok };

        Assert.Equal(1, step.Attempts);
    }

    [Fact]
    public void The_cap_is_a_number_about_the_project_and_not_about_a_case()
    {
        var root = Directory.CreateTempSubdirectory("winwright-attempts-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(root, ProjectDeclaration.FileName), """{ "attempts": 2 }""");
            Assert.Equal(2, ProjectDeclaration.Find(root).Attempts);

            File.WriteAllText(Path.Combine(root, ProjectDeclaration.FileName), "{}");
            Assert.Equal(Retry.DefaultCap, ProjectDeclaration.Find(root).Attempts);

            File.WriteAllText(Path.Combine(root, ProjectDeclaration.FileName), """{ "attempts": 0 }""");
            Assert.Throws<ArgumentException>(() => ProjectDeclaration.Find(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

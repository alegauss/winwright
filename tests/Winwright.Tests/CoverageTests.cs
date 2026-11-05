using Winwright.Verdicts;

using Xunit;

using static Winwright.Tests.Fixtures;

namespace Winwright.Tests;

/// <summary>
/// WW6. The word <em>every</em> is earned rather than default. A run where an assertion could not
/// be evaluated is not the run where all of them passed, and printing the same green for both is
/// how a timing assertion got dropped into an info line nobody reads.
/// </summary>
public class CoverageTests
{
    private static RunVerdict Clean() => RunVerdict.Over([
        AssertionResult.Pass("the window is titled Claude"),
        AssertionResult.Pass("the tray icon is present"),
    ]);

    private static RunVerdict WithAHole() => RunVerdict.Over([
        AssertionResult.Pass("the window is titled Claude"),
        AssertionResult.Unchecked("the tray menu opens", FreeNotificationArea),
    ]);

    [Fact]
    public void A_run_where_everything_ran_and_passed_earns_the_word()
    {
        Assert.True(Coverage.EarnsEvery(Clean()));
        Assert.Equal("every check passed", Coverage.RequireEvery(Clean()));
        Assert.Equal("every assertion passed (2 of 2).", VerdictSummary.Sentence(Clean()));
    }

    [Fact]
    public void One_assertion_that_never_ran_takes_the_word_away()
    {
        Assert.False(Coverage.EarnsEvery(WithAHole()));

        var refusal = Assert.Throws<UnearnedGreenException>(() => Coverage.RequireEvery(WithAHole()));

        Assert.Equal(["the tray menu opens"], refusal.NotRun);
        Assert.Contains("the tray menu opens never ran", refusal.Message);
    }

    [Fact]
    public void The_sentence_names_what_did_not_run_rather_than_counting_it()
    {
        var sentence = VerdictSummary.Sentence(WithAHole());

        Assert.DoesNotContain("every", sentence);
        Assert.Equal("1 of 2 assertions passed; 1 never ran: the tray menu opens.", sentence);
    }

    [Fact]
    public void A_failure_takes_the_word_away_too_and_the_refusal_says_which_reason()
    {
        var verdict = RunVerdict.Over([
            AssertionResult.Fail("the window is titled Claude", "read back 'Claude (2)'"),
        ]);

        var refusal = Assert.Throws<UnearnedGreenException>(() => Coverage.RequireEvery(verdict));

        Assert.Contains("the window is titled Claude failed", refusal.Message);
        Assert.DoesNotContain("never ran", refusal.Message);
    }

    [Fact]
    public void Both_reasons_are_named_when_both_are_there()
    {
        var verdict = RunVerdict.Over([
            AssertionResult.Fail("the report renders", "the file was never written"),
            AssertionResult.Unchecked("the tray menu opens", FreeNotificationArea),
        ]);

        Assert.Equal(
            "0 of 2 assertions passed; 1 failed: the report renders; 1 never ran: the tray menu opens.",
            VerdictSummary.Sentence(verdict));
    }

    [Fact]
    public void A_project_can_ask_for_its_own_wording_and_still_be_refused()
    {
        var refusal = Assert.Throws<UnearnedGreenException>(
            () => Coverage.RequireEvery(WithAHole(), "all tray checks passed"));

        Assert.Equal("all tray checks passed", refusal.Unearned);
        Assert.StartsWith("'all tray checks passed' is not this run", refusal.Message);
    }

    [Fact]
    public void A_sweep_earns_the_word_only_where_every_environment_did()
    {
        var clean = SweepVerdict.Over([
            new EnvironmentRun("light", Clean()),
            new EnvironmentRun("dark", Clean()),
        ]);

        Assert.True(Coverage.EarnsEvery(clean));
        Assert.Equal("every assertion passed in 2 environments.", SweepSummary.Sentence(clean));
    }

    [Fact]
    public void A_sweep_with_one_hole_in_three_places_names_it_once()
    {
        var sweep = SweepVerdict.Over([
            new EnvironmentRun("light", WithAHole()),
            new EnvironmentRun("dark", WithAHole()),
            new EnvironmentRun("high contrast", WithAHole()),
        ]);

        var sentence = SweepSummary.Sentence(sweep);

        Assert.DoesNotContain("every", sentence);
        Assert.Equal("3 environments walked; 1 never ran: the tray menu opens.", sentence);
        Assert.Throws<UnearnedGreenException>(() => Coverage.RequireEvery(sweep));
    }

    [Fact]
    public void The_word_is_absent_from_every_sentence_a_hole_can_produce()
    {
        foreach (var verdict in new[] { WithAHole(), RunVerdict.Over([
            AssertionResult.Fail("the report renders", "the file was never written"),
            AssertionResult.Unchecked("the tray menu opens", FreeNotificationArea),
        ]) })
        {
            Assert.DoesNotContain("every", VerdictSummary.Sentence(verdict), StringComparison.OrdinalIgnoreCase);
        }
    }
}

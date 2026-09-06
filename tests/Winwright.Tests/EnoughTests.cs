using Winwright.Typing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW410 and WW413. The two things the typing tool refuses to do off a run that cannot support
/// them: reach a verdict at all, and attribute a difference to one half of a mechanism.
/// <para>
/// Reachable here because <see cref="Enough" /> is the one piece of that reasoning that is not
/// inside a private verdict — which is why it was put there. The runners are driven at a round each
/// by <c>TypingArmRunTests</c>, and what that case can see is the sentence; what this sees is the
/// rule the sentence comes from.
/// </para>
/// </summary>
public sealed class EnoughTests
{
    [Fact]
    public void A_run_below_the_floor_of_rounds_reaches_no_verdict_at_all()
    {
        var said = Enough.Concluded(1, () => "the confident sentence");

        Assert.Contains(Enough.TooFew, said, StringComparison.Ordinal);
        Assert.DoesNotContain("the confident sentence", said, StringComparison.Ordinal);

        // The number it was given and the number it wants, because a refusal that does not say what
        // would have worked costs a reader the source. This is the rule WW404 applies to the sync
        // and WW412 to the session, said once more about a measurement.
        Assert.Contains("1 round(s)", said, StringComparison.Ordinal);
        Assert.Contains($"{Enough.Rounds} rounds or more", said, StringComparison.Ordinal);
    }

    [Fact]
    public void A_run_at_the_floor_says_whatever_the_runner_worked_out()
    {
        Assert.Equal("the confident sentence", Enough.Concluded(Enough.Rounds, () => "the confident sentence"));
    }

    [Fact]
    public void The_reading_is_not_even_computed_below_the_floor()
    {
        // Deferred and not merely discarded. A verdict is arithmetic over dictionaries the run has
        // already filled, so computing one to throw it away is cheap and wrong for a different
        // reason: it is the shape in which somebody later prints it by accident.
        var reached = false;

        Enough.Concluded(
            1,
            () =>
            {
                reached = true;
                return "";
            });

        Assert.False(reached, "the verdict was computed for a run too short to carry one");
    }

    [Fact]
    public void A_difference_resting_on_too_few_faults_is_the_counts_and_not_a_shape()
    {
        var said = Enough.Attributed(2, "whole 2 of 300, quiet 0 of 300", () => "the band survives");

        Assert.DoesNotContain("the band survives", said, StringComparison.Ordinal);
        Assert.Contains("Too few to attribute", said, StringComparison.Ordinal);

        // The counts survive the refusal, which is the half that makes it a reading rather than a
        // shrug: what the run measured is still the answer, and only the shape drawn over it goes.
        Assert.Contains("whole 2 of 300, quiet 0 of 300", said, StringComparison.Ordinal);
        Assert.Contains("4 in 3600", said, StringComparison.Ordinal);
    }

    [Fact]
    public void A_difference_with_enough_under_it_is_attributed()
    {
        Assert.Equal(
            "the band survives",
            Enough.Attributed(Enough.Faults, "whole 5 of 300", () => "the band survives"));
    }

    [Fact]
    public void Every_runner_reaches_its_verdict_through_the_guard()
    {
        // Both ways, and read off the sources because a verdict is private to its runner. A runner
        // that prints Verdict directly is one that concluded off a round, which is the fault WW410
        // was filed about arriving again in the file next door.
        var loose = new List<string>();

        foreach (var file in Checkout.SourcesIn(Checkout.At("tools", "Winwright.Typing")))
        {
            var lines = File.ReadLines(file).Select(Checkout.Code).ToList();

            var printing = lines
                .Where(one => one.Contains("Console.WriteLine(Verdict(", StringComparison.Ordinal))
                .ToList();

            if (printing.Count > 0)
                loose.Add($"{Path.GetFileName(file)} prints a verdict without asking Enough");
        }

        Assert.True(loose.Count == 0, string.Join("; ", loose));

        // And that the guard is reached at all, which the check above passes trivially without.
        var guarded = Checkout.SourcesIn(Checkout.At("tools", "Winwright.Typing"))
            .Count(one => File.ReadLines(one).Select(Checkout.Code)
                .Any(line => line.Contains("Enough.Concluded(", StringComparison.Ordinal)));

        Assert.True(guarded >= 5, $"only {guarded} runner(s) reach their verdict through the guard");
    }
}

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
    public void Every_arm_concludes_nothing_below_the_floor_its_own_rate_sets_and_does_at_it()
    {
        // WW426. The floor moved on to the arm, so it is asserted of each arm through the call its
        // runner makes: one round short of the floor is the refusal, naming the number that would
        // have worked, and the floor itself is whatever the runner worked out.
        Assert.All(
            Arms.All,
            arm =>
            {
                var refused = Enough.Concluded(arm.Floor - 1, arm.About, () => "the confident sentence");

                Assert.Contains(Enough.TooFew, refused, StringComparison.Ordinal);
                Assert.Contains($"{arm.Floor} rounds or more", refused, StringComparison.Ordinal);
                Assert.DoesNotContain("one to three percent", refused, StringComparison.Ordinal);

                Assert.Equal("the confident sentence", Enough.Concluded(arm.Floor, arm.About, () => "the confident sentence"));
            });
    }

    [Fact]
    public void An_arms_floor_is_where_a_clean_run_rules_out_the_rate_it_is_about()
    {
        // The arithmetic, held rather than restated: at the floor a run of nothing bounds the rate at
        // or under the one the arm declares, and one round fewer does not - unless the shared floor
        // is what binds, which no arm may go below.
        Assert.All(
            Arms.All,
            arm =>
            {
                Assert.InRange(arm.About, double.Epsilon, 1);
                Assert.True(arm.Floor >= Enough.Rounds, $"{arm.Name} asks for {arm.Floor}, under the shared floor");
                // A hair of tolerance for the same reason Enough.Floor takes one: a rate declared as
                // one over a count divides back to that count and the last bit of a double.
                Assert.True(
                    3.0 / arm.Floor <= arm.About * (1 + 1e-9),
                    $"{arm.Name} at {arm.Floor} rounds bounds {3.0 / arm.Floor:P2}, looser than {arm.About:P2}");
                Assert.True(
                    arm.Floor == Enough.Rounds || 3.0 / (arm.Floor - 1) > arm.About,
                    $"{arm.Name} asks for {arm.Floor} where {arm.Floor - 1} already rules out {arm.About:P2}");
            });

        // The one the design was filed about. WW410's thirty was a rounding error for transfer,
        // whose rate is one in twelve hundred - so its floor has to be well past the few hundred its
        // own prose says expects a fraction of a fault.
        Assert.True(Arms.Named("transfer")!.Floor > 1200);

        // And a rate that is not one is refused rather than turned into a floor of every round.
        Assert.Throws<ArgumentOutOfRangeException>(() => Enough.Floor(0));
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

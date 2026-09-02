using Winwright.Capturing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW188. The pairing in <see cref="CaptureArms" /> is checked against the engine in both
/// directions. An arm added later is red here until somebody says what provokes it — which is the
/// question Block K's first criterion asks, asked at the unit a reader actually meets.
/// </summary>
public sealed class CaptureArmTests
{
    [Fact]
    public void Every_arm_the_engine_declares_is_paired_with_something()
    {
        var paired = CaptureArms.Known.Select(one => one.Arm).ToHashSet();

        var missing = CaptureArms.Declared().Where(one => !paired.Contains(one)).ToList();

        Assert.True(
            missing.Count == 0,
            $"{missing.Count} arm(s) of this refusal are provoked by nobody: {string.Join(", ", missing)}");
    }

    [Fact]
    public void Nothing_is_paired_that_the_engine_no_longer_declares()
    {
        var declared = CaptureArms.Declared().ToHashSet();

        var gone = CaptureArms.Known.Where(one => !declared.Contains(one.Arm)).ToList();

        Assert.True(
            gone.Count == 0,
            $"{gone.Count} pairing(s) name an arm the engine no longer has: "
                + string.Join(", ", gone.Select(one => one.Arm)));
    }

    [Fact]
    public void No_arm_is_paired_twice_and_every_one_the_engine_has_is_counted()
    {
        var paired = CaptureArms.Known.Select(one => one.Arm).ToList();

        Assert.Equal(paired.Count, paired.Distinct().Count());

        // Seven. It was five when WW188 wrote this down, WW195 added one and WW334 the seventh —
        // which is the whole point of the pairing: each arrived already provoked rather than a task
        // later. Unsaid is not among them, being what a throw that named no arm carries.
        Assert.Equal(7, CaptureArms.Declared().Count);
        Assert.Equal(CaptureArms.Declared().Count, paired.Count);
        Assert.DoesNotContain(WrongCapture.Unsaid, CaptureArms.Declared());
    }

    [Fact]
    public void Every_pairing_names_a_flag_or_says_why_it_has_none()
    {
        Assert.All(
            CaptureArms.Known,
            one => Assert.True(
                one.ThroughTheFixture ^ (one.Why is not null),
                $"{one.Arm} names {(one.ThroughTheFixture ? "a flag and a reason it has none" : "neither")}"));
    }

    [Fact]
    public void A_flag_named_here_is_one_the_fixture_actually_has()
    {
        // Read off the built article, the way Provocation reads its own: a flag named in a pairing
        // and absent from the fixture is a shape nobody can provoke, which is the failure this
        // whole check exists to stop.
        var flags = Fixture.Catalogue();

        Assert.All(
            CaptureArms.Known.Where(one => one.ThroughTheFixture),
            one => Assert.Contains($"--{one.Flag}", flags, StringComparison.Ordinal));
    }

    [Fact]
    public void The_case_an_arm_names_is_one_this_suite_really_runs()
    {
        Assert.All(
            CaptureArms.Known,
            one =>
            {
                var found = Provocation.CaseNamed(one.Case);

                Assert.True(found is not null, $"{one.Arm} names {one.Case}, which this suite does not have");
                Assert.True(Provocation.IsACase(found!), $"{one.Case} is not a case this suite runs");
            });
    }

    [Fact]
    public void A_refusal_says_which_arm_it_is_rather_than_leaving_it_to_the_sentence()
    {
        // The reason this is keyed on the arm. A case matching a phrase starts matching a different
        // arm the day somebody rewords a message, and three of these open with the same six
        // words before they say anything that tells them apart.
        var refused = new WrongCaptureException(WrongCapture.RegionCovered, "the capture is of a window");

        Assert.Equal(WrongCapture.RegionCovered, refused.Arm);

        // And one thrown without saying carries Unsaid rather than defaulting into a real arm,
        // because a refusal that quietly claimed to be one of the real arms would pair with its check.
        Assert.Equal(WrongCapture.Unsaid, new WrongCaptureException("the capture is of a window").Arm);
    }

    [Fact]
    public void The_pairing_reads_as_a_count_and_then_a_line_each()
    {
        var rendered = CaptureArms.Render();

        Assert.Equal(CaptureArms.Known.Count + 1, rendered.Count);
        Assert.StartsWith($"{CaptureArms.Known.Count} arm(s) of one refusal: ", rendered[0], StringComparison.Ordinal);
        Assert.All(rendered.Skip(1), line => Assert.StartsWith("  ", line, StringComparison.Ordinal));
    }
}

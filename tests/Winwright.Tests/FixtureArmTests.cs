using System.Diagnostics;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW200. The pairing in <see cref="FixtureArms" /> is checked against the built fixture in both
/// directions, by running it rather than by loading it. WW196 asked this question of four refusals
/// and could not ask it of the fifth: the suite references the fixture without its assembly on
/// purpose, so there is no enum here to sweep.
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class FixtureArmTests
{
    /// <summary>Run it to completion, reading what it refused with.</summary>
    private static (int Code, string Said) Ran(params string[] flags)
    {
        var start = new ProcessStartInfo(Fixture.Executable())
        {
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var flag in flags)
            start.ArgumentList.Add(flag);

        using var running = Process.Start(start)!;
        var said = running.StandardError.ReadToEnd();

        // Stopped rather than only reported. A command line that turns out to be a shape the fixture
        // accepts opens a window and waits, and this is the case that would be handing it one — the
        // first draft of this pairing did, and left a fixture running behind a red.
        if (!running.WaitForExit(20_000))
        {
            running.Kill(entireProcessTree: true);
            Assert.Fail($"'{string.Join(' ', flags)}' is a shape this fixture accepts, so it never refused anything");
        }

        return (running.ExitCode, said);
    }

    [Fact]
    public void Every_arm_the_fixture_declares_is_provoked_by_something()
    {
        var paired = FixtureArms.Known.Select(one => one.Arm).ToHashSet(StringComparer.Ordinal);

        var missing = FixtureArms.Declared().Where(one => !paired.Contains(one)).ToList();

        Assert.True(
            missing.Count == 0,
            $"{missing.Count} arm(s) of the fixture's refusal are provoked by nobody: "
                + string.Join(", ", missing));
    }

    [Fact]
    public void Nothing_is_paired_that_the_fixture_no_longer_declares()
    {
        var declared = FixtureArms.Declared().ToHashSet(StringComparer.Ordinal);

        var gone = FixtureArms.Known.Where(one => !declared.Contains(one.Arm)).ToList();

        Assert.True(
            gone.Count == 0,
            $"{gone.Count} pairing(s) name an arm the fixture no longer has: "
                + string.Join(", ", gone.Select(one => one.Arm)));
    }

    [Fact]
    public void Every_arm_is_provoked_by_running_the_fixture_wrong()
    {
        // The whole of it, and the only way available: the arm a person gets is asserted off a real
        // run rather than off a call this suite could make into a library it does not reference.
        // The exit code is read off the fixture's own heading rather than written down here, which
        // is WW161's rule: a number transcribed twice is a number that drifts.
        var exits = FixtureArms.Exits(Fixture.Catalogue());
        Assert.True(exits > 0, "the fixture's arm heading says nothing about what it exits with");

        Assert.All(
            FixtureArms.Known,
            one =>
            {
                var ran = Ran(one.Driven.Split(' ', StringSplitOptions.RemoveEmptyEntries));

                Assert.Equal(exits, ran.Code);
                Assert.Contains($"{FixtureArms.Spelling}{one.Arm}", ran.Said, StringComparison.Ordinal);
            });
    }

    [Fact]
    public void The_arm_leads_the_refusal_so_a_person_can_find_it_without_quoting_a_phrase()
    {
        // Why the name is at the head rather than only inside the exception. A reader who has just
        // been handed a refusal greps for a word; WW188's argument against matching a phrase is the
        // same argument, pointed at a person instead of at a case.
        var ran = Ran("--nonesuch");

        Assert.StartsWith($"{FixtureArms.Spelling}NoSuchShape:", ran.Said.Trim(), StringComparison.Ordinal);
    }

    [Fact]
    public void The_two_places_a_number_is_asked_for_are_one_arm()
    {
        // WW196's judgement, applied here: a flag given something that is not a whole number and a
        // rectangle field that is not one send the reader to write a number either way, so counting
        // them separately would be counting values rather than refusals.
        Assert.Contains("refused NotAWholeNumber", Ran("--loading=twoseconds").Said, StringComparison.Ordinal);
        Assert.Contains("refused NotAWholeNumber", Ran("--intrude=200,200,300,tall").Said, StringComparison.Ordinal);

        Assert.Equal(1, FixtureArms.Known.Count(one => one.Arm == "NotAWholeNumber"));
    }

    [Fact]
    public void No_arm_is_paired_twice_and_every_reason_says_something()
    {
        var named = FixtureArms.Known.Select(one => one.Arm).ToList();

        Assert.Equal(named.Count, named.Distinct(StringComparer.Ordinal).Count());
        Assert.All(FixtureArms.Known, one => Assert.False(string.IsNullOrWhiteSpace(one.Because)));
        Assert.All(FixtureArms.Known, one => Assert.False(string.IsNullOrWhiteSpace(one.Driven)));
    }

    [Fact]
    public void The_list_is_read_off_the_article_and_discriminates()
    {
        // A reader that found nothing would pass the pairing by arithmetic, and one that found
        // everything would pass it by accident. Both ends are named.
        var declared = FixtureArms.Declared();

        Assert.NotEmpty(declared);
        Assert.Contains("NoSuchShape", declared, StringComparer.Ordinal);
        Assert.DoesNotContain("Unsaid", declared, StringComparer.Ordinal);

        // And a catalogue that never printed the heading answers none rather than guessing.
        Assert.Empty(FixtureArms.Declared("This fixture knows:\n  --show  a window"));
    }

    [Fact]
    public void The_pairing_reads_as_a_count_and_then_a_line_each()
    {
        var rendered = FixtureArms.Render();

        Assert.Equal(FixtureArms.Known.Count + 1, rendered.Count);
        Assert.StartsWith(
            $"{FixtureArms.Known.Count} arm(s) of the fixture's own refusal",
            rendered[0],
            StringComparison.Ordinal);
        Assert.All(rendered.Skip(1), line => Assert.StartsWith("  ", line, StringComparison.Ordinal));
    }
}

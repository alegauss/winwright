using System.Text.RegularExpressions;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW428. The runner's waits, read back against the runner.
/// <para>
/// Four waits, each argued in a paragraph beside itself and none of them naming another, so the
/// numbers agreed with nothing and the total could not be read at all. They are one list now, and
/// this is what keeps the fifth from being argued beside it: a bound spelled anywhere else in the
/// script is a wait the list does not know about, and a row nothing says is a number nobody meets.
/// </para>
/// </summary>
public sealed class RunnerWaitsTests
{
    private static string Runner() => File.ReadAllText(Checkout.At("tools", "run-tests-vm.ps1"));

    private static string Probe() => File.ReadAllText(Checkout.At("tools", "desk-probe.ps1"));

    /// <summary>The list as the runner declares it, from its opening to the line that shuts it.</summary>
    private static string Declared()
    {
        var runner = Runner();
        var opens = runner.IndexOf("$script:Waits = @(", StringComparison.Ordinal);

        Assert.True(opens > 0, "the runner declares no list of waits");

        var shuts = runner.IndexOf("\n)", opens, StringComparison.Ordinal);
        Assert.True(shuts > opens, "the runner's list of waits never closes");

        return runner[opens..shuts];
    }

    /// <summary>Every row: what it is called, how long it has, what it is for and how it gives up.</summary>
    private static IReadOnlyList<(string Named, string Seconds, string For, string Giving)> Rows()
    {
        var found = Regex.Matches(
            Declared(),
            @"Named = '(?<named>\w+)'\s*\r?\n\s*Seconds = (?<seconds>[^\r\n]+)\r?\n\s*For = (?<for>[^\r\n]+)\r?\n\s*Giving = '(?<giving>[^']+)'",
            RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));

        return found
            .Select(one => (
                one.Groups["named"].Value,
                one.Groups["seconds"].Value.Trim(),
                one.Groups["for"].Value.Trim(),
                one.Groups["giving"].Value))
            .ToList();
    }

    [Fact]
    public void Every_wait_the_runner_declares_says_what_it_is_for_and_how_it_gives_up()
    {
        var rows = Rows();

        // The four the task counted, and the control on the reading: a regex that matched nothing
        // would make every check below pass about an empty list.
        Assert.True(rows.Count >= 4, $"only {rows.Count} wait(s) were read out of the runner's list");
        Assert.Equal(["desk", "run", "session", "tools"], rows.Select(one => one.Named).Order(StringComparer.Ordinal));

        Assert.All(
            rows,
            row =>
            {
                Assert.False(string.IsNullOrWhiteSpace(row.Seconds), $"{row.Named} says no length");
                Assert.True(row.For.Length > 20, $"{row.Named} says what it is for in {row.For.Length} characters");
                Assert.False(string.IsNullOrWhiteSpace(row.Giving), $"{row.Named} says nothing when it gives up");
            });
    }

    [Fact]
    public void The_words_each_wait_gives_up_with_are_words_the_runner_writes()
    {
        // A row whose sentence appears nowhere is a wait nobody meets the refusal of, which is the
        // half of WW428 that keeps the list from becoming a table of numbers beside the code.
        var runner = Runner();
        var list = Declared();

        Assert.All(
            Rows(),
            row =>
            {
                var elsewhere = runner.Replace(list, "", StringComparison.Ordinal);

                Assert.True(
                    elsewhere.Contains(row.Giving, StringComparison.Ordinal),
                    $"the runner never writes '{row.Giving}', so the '{row.Named}' row names a refusal nobody meets");
            });
    }

    [Fact]
    public void No_bound_is_spelled_outside_the_list()
    {
        // The rule the list exists for. A deadline built from a number written where it is used is
        // the state WW428 was filed about, and it reads as ordinary code until somebody tries to add
        // the four up.
        var list = Declared();
        var elsewhere = Runner().Replace(list, "", StringComparison.Ordinal);

        var spelled = Regex
            .Matches(elsewhere, @"Add(?:Minutes|Seconds)\(\s*\d", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))
            .Select(one => one.Value)
            .ToList();

        Assert.True(
            spelled.Count == 0,
            $"{spelled.Count} deadline(s) are built from a number spelled where they are used rather "
                + $"than from a row of the list: {string.Join(", ", spelled)}");

        // And the reading is doing something: the runner does build deadlines, out of the rows.
        Assert.Contains("AddSeconds($tools.Seconds)", elsewhere, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_name_the_runner_asks_for_is_a_row_of_the_list()
    {
        var named = Rows().Select(one => one.Named).ToHashSet(StringComparer.Ordinal);

        var asked = Regex
            .Matches(Runner(), @"Waited '(?<named>\w+)'", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))
            .Select(one => one.Groups["named"].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(asked);
        Assert.All(asked, one => Assert.True(named.Contains(one), $"the runner asks for a wait called '{one}', which it does not declare"));
    }

    [Fact]
    public void The_holder_walk_is_sent_by_whoever_is_about_to_use_it()
    {
        // WW432. The walk reached the guest with the sync, and the bound's own comment said it ran
        // only after one — true today, by an ordering nothing held. A desk probe given a bound would
        // have asked the guest for a file no sync had put there and been told the guest could not be
        // asked, which is a sentence about the wrong thing.
        var runner = Runner();

        Assert.Contains("function Send-HolderWalk", runner, StringComparison.Ordinal);

        // The walk's own caller sends it first, which is what removes the ordering rather than
        // documenting it.
        var asking = runner.IndexOf("function Get-WhatHoldsGuest", StringComparison.Ordinal);
        var runs = runner.IndexOf("$script:GuestSync\\holders.ps1", asking, StringComparison.Ordinal);
        var sends = runner.IndexOf("Send-HolderWalk -Vmx", asking, StringComparison.Ordinal);

        Assert.True(asking > 0 && sends > asking, "the walk does not send itself before asking");
        Assert.True(sends < runs, "the walk runs the file before sending it");

        // And one spelling of the copy, not two: the sync needs it before its own program runs, and
        // a second copy beside that one is where the two would come to disagree.
        Assert.Single(
            Regex.Matches(
                runner,
                @"copyFileFromHostToGuest[^\r\n]*holders\.ps1",
                RegexOptions.CultureInvariant,
                TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void The_desk_row_is_what_the_probe_actually_spends()
    {
        // The one row argued in another file, held to it. The probe takes its looks a fixed pause
        // apart, and a row that drifted from them would be the total reading wrong by the one wait
        // this runner does not decide.
        var looks = Regex.Match(
            Probe(),
            @"Get-DeskLooks -Count (?<count>\d+) -PauseMs (?<pause>\d+)",
            RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));

        Assert.True(looks.Success, "the probe no longer says how many looks it takes or how far apart");

        var spends = int.Parse(looks.Groups["count"].Value, System.Globalization.CultureInfo.InvariantCulture)
            * int.Parse(looks.Groups["pause"].Value, System.Globalization.CultureInfo.InvariantCulture)
            / 1000;

        var row = Assert.Single(Rows(), one => one.Named == "desk");

        Assert.Equal(spends.ToString(System.Globalization.CultureInfo.InvariantCulture), row.Seconds);
    }
}

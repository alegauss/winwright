using Winwright.Asserting;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW56. An assertion trusted without ever being watched fail, which is the check that passes
/// forever — worse than the absent one it was written instead of, because it also reports that the
/// ground is covered.
/// <para>
/// The reading is against a window read as a set of names, which is the shape claude-tray's tab
/// check had: a hand-written expectation that kept passing while the window grew and shrank around
/// it.
/// </para>
/// </summary>
public sealed class FalsifiableTests
{
    /// <summary>The window as the check reads it: the tab headers, in order.</summary>
    private static readonly IReadOnlyList<string> Tabs = ["Panes", "Status", "Config"];

    /// <summary>A check that really reads the window it is handed.</summary>
    private static AssertionResult HeadersAre(IReadOnlyList<string> read) =>
        read.SequenceEqual(Tabs, StringComparer.Ordinal)
            ? AssertionResult.Pass("the tab headers", $"read {string.Join(", ", read)}")
            : AssertionResult.Fail("the tab headers", $"read {string.Join(", ", read)}");

    /// <summary>A check that reports on nothing, which is the defect this task is about.</summary>
    private static AssertionResult AlwaysGreen(IReadOnlyList<string> read) =>
        AssertionResult.Pass("the tab headers", "three headers were expected and three were read");

    private static Injection<IReadOnlyList<string>> Renamed(string from, string to) =>
        new($"'{from}' renamed to '{to}'", read => read.Select(one => one == from ? to : one).ToList());

    private static Injection<IReadOnlyList<string>> Removed(string what) =>
        new($"'{what}' removed", read => read.Where(one => one != what).ToList());

    [Fact]
    public void A_check_watched_go_red_on_every_declared_defect_can_fail()
    {
        var falsified = Falsification.Of(
            "the tab headers", Tabs, HeadersAre, Removed("Config"), Renamed("Panes", "Views"));

        Assert.True(falsified.CanFail);
        Assert.Empty(falsified.Missed);
        Assert.All(falsified.Injections, one => Assert.Equal(Bite.TurnedItRed, one.Outcome));
        Assert.Contains("was watched go red on all 2 declared defects", falsified.Sentence());
    }

    [Fact]
    public void A_check_that_stays_green_with_the_defect_in_place_is_the_finding()
    {
        // The count check claude-tray had: three expected, three read, and the third one renamed.
        var falsified = Falsification.Of("the tab headers", Tabs, AlwaysGreen, Renamed("Config", "Logs"));

        Assert.False(falsified.CanFail);
        Assert.Equal(["'Config' renamed to 'Logs'"], falsified.Missed);
        Assert.Contains("its green does not cover it", falsified.Sentence());

        var result = falsified.AsAssertion();
        Assert.Equal(AssertionOutcome.Failed, result.Outcome);
        Assert.Equal("the tab headers can fail", result.Name);
    }

    [Fact]
    public void One_defect_caught_does_not_excuse_another_that_was_not()
    {
        // Reads the count and the first header, so a rename of the first bites and the last does not.
        static AssertionResult HalfACheck(IReadOnlyList<string> read) =>
            read.Count == 3 && read[0] == "Panes"
                ? AssertionResult.Pass("the tab headers", "three read, first is Panes")
                : AssertionResult.Fail("the tab headers", $"read {string.Join(", ", read)}");

        var falsified = Falsification.Of(
            "the tab headers", Tabs, HalfACheck, Renamed("Panes", "Views"), Renamed("Config", "Logs"));

        Assert.False(falsified.CanFail);
        Assert.Equal(["'Config' renamed to 'Logs'"], falsified.Missed);
    }

    [Fact]
    public void An_injection_that_changed_nothing_is_told_apart_from_a_check_that_missed()
    {
        var inert = new Injection<IReadOnlyList<string>>("'Logs' removed", read => read.ToList());

        var falsified = Falsification.Of("the tab headers", Tabs, HeadersAre, inert);

        var only = Assert.Single(falsified.Injections);
        Assert.Equal(Bite.ChangedNothing, only.Outcome);
        Assert.Contains("was never put in front of the check", only.Detail);
        Assert.False(falsified.CanFail);
    }

    [Fact]
    public void A_check_that_throws_against_a_defect_broke_rather_than_went_red()
    {
        // Green on the window as it stands, and indexing off the end the moment a tab goes.
        static AssertionResult Brittle(IReadOnlyList<string> read) =>
            read[2] == "Config"
                ? AssertionResult.Pass("the tab headers", "read")
                : AssertionResult.Fail("the tab headers", "read");

        var falsified = Falsification.Of("the tab headers", Tabs, Brittle, Removed("Config"));

        var only = Assert.Single(falsified.Injections);
        Assert.Equal(Bite.Threw, only.Outcome);
        Assert.Contains("rather than reporting a red", only.Detail);
        Assert.False(falsified.CanFail);
    }

    [Fact]
    public void A_check_that_throws_on_the_honest_window_is_left_to_the_run_that_owns_it()
    {
        // Not caught here on purpose: a check throwing against the real window is a break the run
        // already reports, and swallowing it into a falsifiability sentence would bury it.
        static AssertionResult Brittle(IReadOnlyList<string> read) =>
            read[3] == "Logs" ? AssertionResult.Pass("the tab headers", "read") : AssertionResult.Fail("x", "y");

        Assert.ThrowsAny<Exception>(
            () => Falsification.Of("the tab headers", Tabs, Brittle, Removed("Config")));
    }

    [Fact]
    public void An_injection_that_itself_throws_is_reported_as_the_injection_breaking()
    {
        var broken = new Injection<IReadOnlyList<string>>(
            "'Config' removed", _ => throw new InvalidOperationException("the window was gone"));

        var falsified = Falsification.Of("the tab headers", Tabs, HeadersAre, broken);

        var only = Assert.Single(falsified.Injections);
        Assert.Equal(Bite.Threw, only.Outcome);
        Assert.Contains("injecting ''Config' removed' threw InvalidOperationException", only.Detail);
    }

    [Fact]
    public void A_check_already_red_leaves_the_question_unsettled_rather_than_failed()
    {
        var wrong = new List<string> { "Panes", "Status" };

        var falsified = Falsification.Of("the tab headers", (IReadOnlyList<string>)wrong, HeadersAre, Removed("Panes"));

        Assert.False(falsified.WasGreen);
        Assert.False(falsified.CanFail);

        // Not a red: an injection turning a red check red proves neither of them, and reporting
        // that as a failure would put the tab defect's own name on a second, invented failure.
        var result = falsified.AsAssertion();
        Assert.Equal(AssertionOutcome.Unchecked, result.Outcome);
        Assert.True(result.DidNotRun);
        Assert.Contains("nothing here says whether it can fail", result.Detail);
    }

    [Fact]
    public void Declaring_no_defect_is_refused_because_every_one_of_none_was_caught()
    {
        var refused = Assert.Throws<ArgumentException>(
            () => Falsification.Of("the tab headers", Tabs, HeadersAre));

        Assert.Contains("caught every defect it named caught nothing at all", refused.Message);
    }

    [Fact]
    public void The_honest_verdict_is_kept_so_the_run_reads_the_window_once()
    {
        var falsified = Falsification.Of("the tab headers", Tabs, HeadersAre, Removed("Config"));

        Assert.Equal(AssertionOutcome.Passed, falsified.Honest.Outcome);
        Assert.Contains("read Panes, Status, Config", falsified.Honest.Detail);
    }

    [Fact]
    public void A_check_reporting_a_hole_under_the_defect_did_not_go_red_either()
    {
        static AssertionResult Skips(IReadOnlyList<string> read) =>
            read.Count == 3
                ? AssertionResult.Pass("the tab headers", "read")
                : AssertionResult.Unchecked("the tab headers", Fixtures.RegisteredProfile);

        var falsified = Falsification.Of("the tab headers", Tabs, Skips, Removed("Config"));

        var only = Assert.Single(falsified.Injections);
        Assert.Equal(Bite.LeftItGreen, only.Outcome);
        Assert.Contains("reported a hole rather than a red", only.Detail);
    }
}

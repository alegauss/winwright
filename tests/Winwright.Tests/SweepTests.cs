using Winwright.Verdicts;

using Xunit;

using static Winwright.Tests.Fixtures;

namespace Winwright.Tests;

/// <summary>
/// WW4. claude-tray's sweep walks one submenu per sampled mode, and an assertion absent in all
/// three modes was counted three times and read as three holes. One hole, three places.
/// </summary>
public class SweepTests
{
    private static readonly string[] Modes = ["light", "dark", "high contrast"];

    private static SweepVerdict AbsentInEveryMode() => SweepVerdict.Over(Modes.Select(mode =>
        new EnvironmentRun(mode, RunVerdict.Over([
            AssertionResult.Pass("the window is titled Claude"),
            AssertionResult.Unchecked("the tray menu opens", FreeNotificationArea),
        ]))));

    [Fact]
    public void One_hole_in_three_environments_is_tallied_once()
    {
        var sweep = AbsentInEveryMode();

        Assert.Single(sweep.Unchecked);
        Assert.Equal("the tray menu opens", sweep.Unchecked[0].Name);
        Assert.Equal(3, sweep.UncheckedOccurrences);
    }

    [Fact]
    public void The_line_still_prints_at_every_occurrence()
    {
        var lines = SweepSummary.Detail(AbsentInEveryMode());

        Assert.Equal(3, lines.Count);
        Assert.Contains("[light] the tray menu opens", lines[0]);
        Assert.Contains("[dark] the tray menu opens", lines[1]);
        Assert.Contains("[high contrast] the tray menu opens", lines[2]);
    }

    [Fact]
    public void The_headline_says_one_hole_and_says_where_it_landed()
    {
        Assert.Equal(
            "DEGRADED (exit 2) - 3 environments: 0 failed, 1 unchecked (in 3 of them) (all unclassified)",
            SweepSummary.Headline(AbsentInEveryMode()));
    }

    [Fact]
    public void A_hole_in_one_environment_only_says_so_without_a_count()
    {
        var sweep = SweepVerdict.Over([
            new EnvironmentRun("light", RunVerdict.Over([AssertionResult.Pass("the window is titled Claude")])),
            new EnvironmentRun("dark", RunVerdict.Over([
                AssertionResult.Unchecked("the tray menu opens", FreeNotificationArea),
            ])),
        ]);

        Assert.Equal("DEGRADED (exit 2) - 2 environments: 0 failed, 1 unchecked (all unclassified)", SweepSummary.Headline(sweep));
        Assert.Single(SweepSummary.Detail(sweep));
    }

    [Fact]
    public void Two_different_assertions_are_two_holes()
    {
        var sweep = SweepVerdict.Over([
            new EnvironmentRun("light", RunVerdict.Over([
                AssertionResult.Unchecked("the tray menu opens", FreeNotificationArea),
            ])),
            new EnvironmentRun("dark", RunVerdict.Over([
                AssertionResult.Unchecked("the report renders", RegisteredProfile),
            ])),
        ]);

        Assert.Equal(2, sweep.Unchecked.Count);
        Assert.Equal(2, sweep.UncheckedOccurrences);
    }

    [Fact]
    public void A_failure_anywhere_is_the_sweeps_reading()
    {
        var sweep = SweepVerdict.Over([
            new EnvironmentRun("light", RunVerdict.Over([AssertionResult.Pass("the window is titled Claude")])),
            new EnvironmentRun("dark", RunVerdict.Over([
                AssertionResult.Fail("the window is titled Claude", "read back 'Claude (2)'"),
            ])),
            new EnvironmentRun("high contrast", RunVerdict.Over([
                AssertionResult.Unchecked("the tray menu opens", FreeNotificationArea),
            ])),
        ]);

        Assert.Equal(RunOutcome.Failed, sweep.Outcome);
        Assert.Equal(1, sweep.ExitCode);
        Assert.Single(sweep.Failures);
        Assert.Single(sweep.Unchecked);
    }

    [Fact]
    public void A_clean_sweep_is_zero()
    {
        var sweep = SweepVerdict.Over(Modes.Select(mode =>
            new EnvironmentRun(mode, RunVerdict.Over([AssertionResult.Pass("the window is titled Claude")]))));

        Assert.Equal(RunOutcome.Passed, sweep.Outcome);
        Assert.Equal("PASSED (exit 0) - 3 environments: 0 failed, 0 unchecked", SweepSummary.Render(sweep));
    }

    [Fact]
    public void The_occurrences_carry_the_environments_in_sweep_order()
    {
        Assert.Equal(Modes, AbsentInEveryMode().Unchecked[0].Environments);
    }

    [Fact]
    public void An_environment_walked_twice_is_refused()
    {
        var refusal = Assert.Throws<ArgumentException>(() => SweepVerdict.Over([
            new EnvironmentRun("dark", RunVerdict.Over([AssertionResult.Pass("the window is titled Claude")])),
            new EnvironmentRun("dark", RunVerdict.Over([AssertionResult.Pass("the window is titled Claude")])),
        ]));

        Assert.Contains("walked twice", refusal.Message);
    }

    [Fact]
    public void A_sweep_that_walked_nothing_is_refused()
    {
        Assert.Throws<ArgumentException>(() => SweepVerdict.Over([]));
    }
}

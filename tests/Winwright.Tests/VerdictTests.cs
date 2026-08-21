using Winwright.Verdicts;

using Xunit;

using static Winwright.Tests.Fixtures;

namespace Winwright.Tests;

/// <summary>
/// WW1. Three outcomes and three exit codes, and the one case the whole project turns on: a run
/// whose assertions all passed but where one never ran is neither 0 nor 1.
/// </summary>
public class VerdictTests
{
    [Fact]
    public void Everything_that_ran_and_passed_is_zero()
    {
        var verdict = RunVerdict.Over([
            AssertionResult.Pass("the window is titled Claude"),
            AssertionResult.Pass("the tray icon is present"),
        ]);

        Assert.Equal(RunOutcome.Passed, verdict.Outcome);
        Assert.Equal(0, verdict.ExitCode);
        Assert.Empty(verdict.Unchecked);
        Assert.Equal(2, verdict.Ran);
    }

    [Fact]
    public void One_assertion_that_did_not_hold_is_one()
    {
        var verdict = RunVerdict.Over([
            AssertionResult.Pass("the window is titled Claude"),
            AssertionResult.Fail("the tray icon is present", "no icon under the Claude process"),
        ]);

        Assert.Equal(RunOutcome.Failed, verdict.Outcome);
        Assert.Equal(1, verdict.ExitCode);
    }

    [Fact]
    public void Everything_that_ran_passed_and_one_could_not_run_is_two()
    {
        var verdict = RunVerdict.Over([
            AssertionResult.Pass("the window is titled Claude"),
            AssertionResult.Unchecked("the tray menu opens", FreeNotificationArea),
        ]);

        Assert.Equal(RunOutcome.Degraded, verdict.Outcome);
        Assert.Equal(2, verdict.ExitCode);
    }

    [Fact]
    public void A_hole_never_reads_as_a_pass()
    {
        var verdict = RunVerdict.Over([
            AssertionResult.Unchecked("the report renders", RegisteredProfile),
        ]);

        Assert.NotEqual(RunOutcome.Passed, verdict.Outcome);
        Assert.Equal(0, verdict.Ran);
    }

    [Fact]
    public void A_failure_outranks_a_hole()
    {
        var verdict = RunVerdict.Over([
            AssertionResult.Fail("the report renders", "the file was never written"),
            AssertionResult.Unchecked("the tray menu opens", FreeNotificationArea),
        ]);

        Assert.Equal(RunOutcome.Failed, verdict.Outcome);
        Assert.Equal(1, verdict.ExitCode);
        Assert.Single(verdict.Unchecked);
    }

    [Fact]
    public void The_exit_code_is_the_outcome_and_not_a_second_mapping()
    {
        Assert.Equal(0, (int)RunOutcome.Passed);
        Assert.Equal(1, (int)RunOutcome.Failed);
        Assert.Equal(2, (int)RunOutcome.Degraded);
    }

    [Fact]
    public void A_verdict_over_nothing_is_refused()
    {
        var refusal = Assert.Throws<ArgumentException>(() => RunVerdict.Over([]));
        Assert.Contains("no hole to name", refusal.Message);
    }

    [Fact]
    public void An_unnamed_assertion_is_refused()
    {
        Assert.Throws<ArgumentException>(() => AssertionResult.Unchecked("  ", RegisteredProfile));
    }

    [Fact]
    public void A_hole_with_no_reason_is_refused()
    {
        Assert.Throws<ArgumentException>(() => Precondition.Absent("a registered profile", "  "));
    }

    [Fact]
    public void A_precondition_this_machine_has_does_not_explain_a_hole()
    {
        var refusal = Assert.Throws<ArgumentException>(
            () => AssertionResult.Unchecked("the report renders", Precondition.Met("a registered profile")));

        Assert.Contains("does not explain why", refusal.Message);
    }

    [Fact]
    public void A_hole_carries_the_precondition_that_was_absent()
    {
        var hole = AssertionResult.Unchecked("the report renders", RegisteredProfile);

        Assert.Equal("a registered profile", hole.Missing!.Name);
        Assert.Equal("no profile registered", hole.Detail);
        Assert.True(hole.DidNotRun);
    }
}

using Winwright.Tracing;
using Winwright.Verdicts;

using Xunit;

using static Winwright.Tests.Fixtures;

namespace Winwright.Tests;

/// <summary>
/// WW7. A pattern that throws, an assembly that will not load, a locator that cannot be parsed —
/// none of these is a statement about the code under test, and reporting one as a failed
/// assertion sends whoever reads it to the wrong repository.
/// </summary>
public class HarnessErrorTests
{
    private static HarnessError Threw() =>
        HarnessError.At(5, "click #save", new InvalidOperationException("the Invoke pattern is not supported here"));

    [Fact]
    public void A_thrown_exception_is_its_own_outcome_and_its_own_exit_code()
    {
        var verdict = RunVerdict.Over([AssertionResult.Pass("the window is titled Claude")], [Threw()]);

        Assert.Equal(RunOutcome.Broken, verdict.Outcome);
        Assert.Equal(3, verdict.ExitCode);
    }

    [Fact]
    public void A_broken_harness_is_never_reported_as_a_broken_build()
    {
        var verdict = RunVerdict.Over(
            [AssertionResult.Fail("the report renders", "the file was never written")], [Threw()]);

        Assert.Equal(RunOutcome.Broken, verdict.Outcome);
        Assert.NotEqual(RunOutcome.Failed, verdict.Outcome);
        Assert.Single(verdict.Failures);
    }

    [Fact]
    public void The_outcome_carries_the_step_it_came_from_and_the_exception()
    {
        var error = Threw();

        Assert.Equal(5, error.Step);
        Assert.Equal("click #save", error.Where);
        Assert.Equal("InvalidOperationException", error.ExceptionType);
        Assert.Equal("the Invoke pattern is not supported here", error.Message);
        Assert.Equal("[step 5] click #save - InvalidOperationException: the Invoke pattern is not supported here",
            error.ToString());
    }

    [Fact]
    public void Breaking_before_any_step_ran_is_reported_without_an_ordinal()
    {
        var error = HarnessError.At(0, "loading the scenario", new FileNotFoundException("tray.json is not there"));

        Assert.StartsWith("loading the scenario - FileNotFoundException:", error.ToString());
    }

    [Fact]
    public void A_run_that_broke_before_asserting_anything_is_still_a_verdict()
    {
        var verdict = RunVerdict.Over([], [HarnessError.At(0, "loading the scenario", "FormatException", "bad locator")]);

        Assert.Equal(RunOutcome.Broken, verdict.Outcome);
        Assert.Empty(verdict.Results);
    }

    [Fact]
    public void A_run_with_neither_results_nor_errors_is_still_refused()
    {
        Assert.Throws<ArgumentException>(() => RunVerdict.Over([], []));
    }

    [Fact]
    public void The_summary_gives_the_break_its_own_line_and_its_own_word()
    {
        var verdict = RunVerdict.Over([AssertionResult.Pass("the window is titled Claude")], [Threw()]);

        var summary = VerdictSummary.Render(verdict);

        Assert.StartsWith("BROKEN (exit 3) - 1 assertion: 1 passed, 0 failed, 0 unchecked; the harness broke once",
            summary);
        Assert.Contains("  threw      [step 5] click #save - InvalidOperationException:", summary);
    }

    [Fact]
    public void The_sentence_leads_with_the_break_because_that_says_which_repository_to_open()
    {
        var verdict = RunVerdict.Over([AssertionResult.Unchecked("the tray menu opens", FreeNotificationArea)], [Threw()]);

        Assert.StartsWith("the harness broke at [step 5] click #save", VerdictSummary.Sentence(verdict));
        Assert.Contains("1 never ran: the tray menu opens.", VerdictSummary.Sentence(verdict));
    }

    [Fact]
    public void A_broken_run_never_earns_the_word_and_the_refusal_says_the_harness_broke()
    {
        var verdict = RunVerdict.Over([AssertionResult.Pass("the window is titled Claude")], [Threw()]);

        Assert.False(Coverage.EarnsEvery(verdict));

        var refusal = Assert.Throws<UnearnedGreenException>(() => Coverage.RequireEvery(verdict));

        Assert.Single(refusal.Broke);
        Assert.Contains("the harness broke at [step 5] click #save", refusal.Message);
        Assert.DoesNotContain("never ran", refusal.Message);
    }

    [Fact]
    public void A_sweep_that_broke_anywhere_is_broken_and_says_where()
    {
        var sweep = SweepVerdict.Over([
            new EnvironmentRun("light", RunVerdict.Over([AssertionResult.Pass("the window is titled Claude")])),
            new EnvironmentRun("dark", RunVerdict.Over([AssertionResult.Pass("the window is titled Claude")], [Threw()])),
        ]);

        Assert.Equal(RunOutcome.Broken, sweep.Outcome);
        Assert.Equal(3, sweep.ExitCode);
        Assert.Equal("dark", sweep.Broke[0].Environment);
        Assert.Contains("  threw      [dark] [step 5] click #save", SweepSummary.Render(sweep));
        Assert.StartsWith("the harness broke at [dark] [step 5] click #save", SweepSummary.Sentence(sweep));
    }

    [Fact]
    public void The_trace_tells_a_throw_apart_from_a_failure_too()
    {
        var line = TraceFormat.Line(new TraceStep
        {
            Verb = "click",
            Locator = "#save",
            Verdict = StepVerdict.Threw,
            Detail = "InvalidOperationException: the Invoke pattern is not supported here",
        });

        Assert.Contains("\"verdict\":\"threw\"", line);
        Assert.Equal(StepVerdict.Threw, TraceFormat.Parse(line).Verdict);
    }

    [Fact]
    public void A_harness_error_with_no_step_named_is_refused()
    {
        Assert.Throws<ArgumentException>(() => HarnessError.At(1, "  ", new InvalidOperationException("x")));
    }
}

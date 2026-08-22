using Winwright.Tracing;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW108. Block A shipped both halves and joined neither: a summary line says an assertion failed
/// and the trace says step 7 waited 240 ms and polled three times, and reading one against the
/// other is a person matching prose to prose.
/// <para>
/// That matching is the re-run this block exists to make unnecessary, so the join is one call and
/// not two — a runner left to make it itself would make it one way, and the next runner another.
/// </para>
/// </summary>
public sealed class JoinTests
{
    private static TraceStep Asserting(string locator = "#status") => new()
    {
        Verb = "assert",
        Locator = locator,
        Verdict = StepVerdict.Failed,
        ReadBack = "",
    };

    [Fact]
    public void Settling_a_step_joins_it_to_the_result_in_both_directions()
    {
        using var trace = new StringWriter();
        using var writer = TraceWriter.To(trace);

        writer.Write(new TraceStep { Verb = "launch", Locator = "(none)", Verdict = StepVerdict.Ok });
        var settled = writer.Settled(Asserting(), AssertionResult.Fail("the report renders", "the file was never written"));

        // The result names the step, and the step names the assertion. Neither half can be
        // written without the other, which is the whole of why this is one call.
        Assert.Equal(2, settled.Step);
        Assert.True(settled.Traced);

        var line = TraceFormat.Parse(trace.ToString().Split('\n')[1]);
        Assert.Equal("the report renders", line.Asserted);
        Assert.Equal(2, line.Step);
    }

    [Fact]
    public void A_failure_in_the_summary_names_the_step_that_settled_it()
    {
        using var trace = new StringWriter();
        using var writer = TraceWriter.To(trace);

        var settled = writer.Settled(Asserting(), AssertionResult.Fail("the report renders", "the file was never written"));

        // One grep away, which is what the criterion about diagnosing from the record asks for.
        Assert.Contains("step 1", VerdictSummary.Line(settled));
        Assert.Contains("the report renders - the file was never written", VerdictSummary.Line(settled));
    }

    [Fact]
    public void A_result_nothing_joined_says_nothing_rather_than_naming_step_zero()
    {
        var alone = AssertionResult.Fail("the report renders", "the file was never written");

        // A step number nobody assigned would send a reader to whatever line happened to be there,
        // which is worse than sending them nowhere.
        Assert.Equal(0, alone.Step);
        Assert.False(alone.Traced);
        Assert.DoesNotContain("step", VerdictSummary.Line(alone), StringComparison.Ordinal);
    }

    [Fact]
    public void A_join_to_step_zero_is_refused_because_it_reads_exactly_like_a_real_one()
    {
        var result = AssertionResult.Fail("the report renders", "the file was never written");

        Assert.Throws<ArgumentOutOfRangeException>(() => result.At(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.At(-3));
    }

    [Fact]
    public void An_unchecked_result_is_joined_the_same_way_a_failure_is()
    {
        using var trace = new StringWriter();
        using var writer = TraceWriter.To(trace);

        var hole = AssertionResult.Unchecked("the menu opens", Fixtures.FreeNotificationArea);
        var settled = writer.Settled(Asserting("#tray") with { Verdict = StepVerdict.Unchecked }, hole);

        // A hole is the reading most worth tracing to: it says nothing was observed, and the step
        // is where a reader finds out what the run tried before giving up.
        Assert.Equal(1, settled.Step);
        Assert.Contains("step 1", VerdictSummary.Line(settled));
        Assert.Equal(AssertionOutcome.Unchecked, settled.Outcome);
    }

    [Fact]
    public void Joining_changes_nothing_else_about_the_result()
    {
        var before = AssertionResult.Fail("the report renders", "the file was never written");

        var after = before.At(4);

        Assert.Equal(before.Name, after.Name);
        Assert.Equal(before.Outcome, after.Outcome);
        Assert.Equal(before.Detail, after.Detail);
        Assert.Equal(before, after with { }, EqualityOnEverythingButTheStep.Instance);
    }

    [Fact]
    public void A_step_that_settled_nothing_carries_no_assertion_name()
    {
        using var trace = new StringWriter();
        using var writer = TraceWriter.To(trace);

        var click = writer.Write(new TraceStep { Verb = "click", Locator = "#save", Verdict = StepVerdict.Ok });

        Assert.Null(click.Asserted);
        Assert.DoesNotContain("asserted", TraceFormat.Line(click), StringComparison.Ordinal);
    }

    [Fact]
    public void The_ordinal_comes_from_the_writer_and_never_from_the_caller()
    {
        using var trace = new StringWriter();
        using var writer = TraceWriter.To(trace);

        // Numbered at the moment of writing, so a caller that kept its own count could not get it
        // wrong on a retry — which is the reason the writer assigns it in the first place.
        var first = writer.Settled(Asserting(), AssertionResult.Fail("one", "because"));
        var second = writer.Settled(Asserting() with { Step = 99 }, AssertionResult.Fail("two", "because"));

        Assert.Equal(1, first.Step);
        Assert.Equal(2, second.Step);
    }

    /// <summary>Compares two results on everything the join does not touch.</summary>
    private sealed class EqualityOnEverythingButTheStep : IEqualityComparer<AssertionResult>
    {
        internal static EqualityOnEverythingButTheStep Instance { get; } = new();

        public bool Equals(AssertionResult? left, AssertionResult? right) =>
            left is not null && right is not null
            && left.Name == right.Name
            && left.Outcome == right.Outcome
            && left.Detail == right.Detail
            && Equals(left.Missing, right.Missing);

        public int GetHashCode(AssertionResult one) => HashCode.Combine(one.Name, one.Outcome, one.Detail);
    }
}

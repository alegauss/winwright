using Winwright.Asserting;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW48. An assertion is a boolean, so a failure says nothing about what was there instead.
/// <para>
/// The first test is the defect this task exists for, reproduced: claude-tray reported no status
/// line after 25 seconds while the status line had been up the whole 25 seconds saying it was
/// computing. What is asserted there is not only that the reading is in the sentence but that the
/// sentence does <em>not</em> claim nothing answered — because that claim is what sent the
/// afternoon after a timing problem that did not exist.
/// </para>
/// </summary>
public sealed class ExpectationTests
{
    [Fact]
    public void A_subject_that_answered_the_wrong_thing_throughout_is_not_reported_as_absent()
    {
        var expectation = Expect.That("the status line", "Ready", () => "Computing...", 120, 10);

        var said = expectation.Sentence();

        Assert.False(expectation.Held);
        Assert.True(expectation.EverSaw);
        Assert.Equal(expectation.Polls, expectation.PollsThatSaw);
        // The whole wording, not a fragment of it: the sentence is what this task delivers, so
        // it is pinned here against the expectation's own numbers rather than sampled.
        Assert.Equal(
            $"expected the status line to read 'Ready'; it read 'Computing...' in "
                + $"{expectation.PollsThatSaw} of {expectation.Polls} polls over {expectation.WaitedMs}ms, "
                + "and never anything else.",
            said);

        // Every poll saw the subject, so nothing about this failure is about waiting, and the
        // words are not allowed to suggest it was. That suggestion is what cost the afternoon.
        Assert.DoesNotContain("nothing answered", said);
    }

    [Fact]
    public void A_subject_that_never_answered_says_so_and_is_the_only_one_that_sounds_like_timing()
    {
        var expectation = Expect.That("the status line", "Ready", () => null, 120, 10);

        Assert.False(expectation.Held);
        Assert.False(expectation.EverSaw);
        Assert.Equal(0, expectation.PollsThatSaw);
        Assert.Equal(
            $"expected the status line to read 'Ready'; nothing answered to it in {expectation.Polls} polls "
                + $"over {expectation.WaitedMs}ms.",
            expectation.Sentence());
        Assert.Null(expectation.LastRead);
    }

    [Fact]
    public void The_two_failures_are_told_apart_by_a_number_and_not_by_reading_the_prose()
    {
        var absent = Expect.That("the pane", "open", () => null, 80, 10);
        var wrong = Expect.That("the pane", "open", () => "shut", 80, 10);

        Assert.False(absent.Held);
        Assert.False(wrong.Held);

        // Both are red and a boolean would stop here. PollsThatSaw is what a report can branch on.
        Assert.Equal(0, absent.PollsThatSaw);
        Assert.True(wrong.PollsThatSaw > 0);
    }

    [Fact]
    public void A_value_that_arrives_late_passes_and_says_how_long_it_took()
    {
        var readings = 0;

        var expectation = Expect.That("the field", "done", () => ++readings < 3 ? "working" : "done", 500, 10);

        Assert.True(expectation.Held, expectation.Sentence());
        Assert.Equal("done", expectation.LastRead);
        Assert.Equal(3, expectation.Polls);
        Assert.Contains("read 'done' after", expectation.Sentence());
        Assert.Contains("3 polls", expectation.Sentence());
    }

    [Fact]
    public void Every_change_of_reading_is_kept_and_repeats_are_not()
    {
        var polls = 0;

        // Answers 'a' three times, then 'b' three times, then stops changing. Six polls, three
        // readings: the changes are the story and the repeats are one fact each.
        var expectation = Expect.That("the field", "never", () => ++polls <= 3 ? "a" : polls <= 6 ? "b" : "c", 200, 5);

        Assert.Equal(["a", "b", "c"], expectation.Readings.Select(reading => reading.Read));
        Assert.True(expectation.Polls > expectation.Readings.Count, expectation.Sentence());
        Assert.Contains("then", expectation.Sentence());
        Assert.Contains("saw it over", expectation.Sentence());
    }

    [Fact]
    public void A_reading_that_flaps_is_elided_and_the_elision_is_counted()
    {
        var polls = 0;

        var expectation = Expect.That("the field", "never", () => (++polls).ToString(), 400, 1);

        Assert.True(expectation.Readings.Count > Expectation.MostReadings, expectation.Sentence());
        Assert.Contains("further change", expectation.Sentence());
        Assert.Contains($"{expectation.Readings.Count - Expectation.MostReadings} further change",
            expectation.Sentence());
    }

    [Fact]
    public void A_subject_that_appears_partway_through_keeps_the_absence_in_the_record()
    {
        var polls = 0;

        var expectation = Expect.That("the toast", "up", () => ++polls < 3 ? null : "down", 200, 5);

        Assert.False(expectation.Held);
        Assert.True(expectation.EverSaw);
        Assert.Equal([null, "down"], expectation.Readings.Select(reading => reading.Read));
        Assert.Contains("nothing", expectation.Sentence());
        Assert.Contains("'down'", expectation.Sentence());
    }

    [Fact]
    public void The_result_a_verdict_counts_carries_the_same_sentence()
    {
        var failed = Expect.That("the status line", "Ready", () => "Computing...", 80, 10).AsAssertion();
        var passed = Expect.Now("the status line", "Ready", () => "Ready").AsAssertion();

        Assert.Equal(AssertionOutcome.Failed, failed.Outcome);
        Assert.Contains("'Computing...'", failed.Detail);
        Assert.Equal(AssertionOutcome.Passed, passed.Outcome);
        Assert.Equal("the status line", passed.Name);
    }

    [Fact]
    public void The_trace_step_carries_the_wait_and_the_polls_rather_than_a_bare_red()
    {
        var step = Expect.That("the status line", "Ready", () => "Computing...", 80, 10).AsTraceStep();

        Assert.Equal("expect", step.Verb);
        Assert.Equal("the status line", step.Locator);
        Assert.Equal("Computing...", step.ReadBack);
        Assert.Equal(Winwright.Tracing.StepVerdict.Failed, step.Verdict);
        Assert.True(step.Polls > 1);
        Assert.True(step.WaitedMs >= 80);
        Assert.Contains("Computing...", step.Detail);
    }

    [Fact]
    public void A_single_look_sleeps_never_and_is_reached_by_name()
    {
        var polls = 0;

        var expectation = Expect.Now("the field", "done", () => { polls++; return "working"; });

        Assert.Equal(1, polls);
        Assert.Equal(1, expectation.Polls);
        Assert.False(expectation.Held);
        Assert.Contains("'working'", expectation.Sentence());

        // The deadline has no zero that means this, so nobody reaches a single look by accident.
        var refused = Assert.Throws<ArgumentOutOfRangeException>(
            () => Expect.That("the field", "done", () => null, 0));
        Assert.Contains("Expect.Now", refused.Message);
    }

    [Fact]
    public void An_expectation_with_no_name_and_no_value_is_refused()
    {
        Assert.Throws<ArgumentException>(() => Expect.That(" ", "done", () => null, 50));
        Assert.Throws<ArgumentNullException>(() => Expect.That("the field", null!, () => null, 50));
        Assert.Throws<ArgumentNullException>(() => Expect.That("the field", "done", null!, 50));
        Assert.Throws<ArgumentOutOfRangeException>(() => Expect.That("the field", "done", () => null, 50, 0));
        Assert.Throws<ArgumentException>(() => Expect.Now(" ", "done", () => null));
    }

    [Fact]
    public void An_empty_string_is_a_value_and_nothing_is_not()
    {
        var empty = Expect.Now("the field", "", () => "");
        var missing = Expect.Now("the field", "", () => null);

        Assert.True(empty.Held);
        Assert.True(empty.EverSaw);

        // A control reading the empty string and a control that is not there both look like
        // falsehood to a boolean. They are different findings and only one of them is about the
        // element being absent.
        Assert.False(missing.Held);
        Assert.False(missing.EverSaw);
        Assert.Contains("nothing answered to it", missing.Sentence());
    }
}

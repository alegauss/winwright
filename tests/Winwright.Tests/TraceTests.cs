using Winwright.Tracing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW3. The trace carries what the run observed, one line per step, so a failure is read from the
/// record rather than reproduced on a desk that would answer a different question.
/// </summary>
public class TraceTests
{
    private static TraceStep Resolved(string locator) => new()
    {
        Verb = "click",
        Locator = locator,
        Resolved = "Button 'Save' (AutomationId=save)",
        Pattern = "Invoke",
        ReadBack = "Save",
        WaitedMs = 240,
        Polls = 3,
        Verdict = StepVerdict.Ok,
    };

    [Fact]
    public void A_step_carries_the_locator_what_it_resolved_to_and_what_was_read_back()
    {
        var line = TraceFormat.Line(Resolved("#save"));

        Assert.Contains("\"locator\":\"#save\"", line);
        Assert.Contains("\"resolved\":\"Button 'Save' (AutomationId=save)\"", line);
        Assert.Contains("\"pattern\":\"Invoke\"", line);
        Assert.Contains("\"readBack\":\"Save\"", line);
        Assert.Contains("\"verdict\":\"ok\"", line);
    }

    [Fact]
    public void The_wait_and_the_polls_are_recorded_because_a_re_run_would_not_reproduce_them()
    {
        var line = TraceFormat.Line(Resolved("#save"));

        Assert.Contains("\"waitedMs\":240", line);
        Assert.Contains("\"polls\":3", line);
    }

    [Fact]
    public void A_step_is_one_line_and_never_more()
    {
        var line = TraceFormat.Line(Resolved("#save"));

        Assert.DoesNotContain('\n', line);
        Assert.DoesNotContain('\r', line);
    }

    [Fact]
    public void What_was_not_observed_is_left_out_rather_than_written_as_null()
    {
        var line = TraceFormat.Line(new TraceStep
        {
            Verb = "click",
            Locator = "#save",
            Verdict = StepVerdict.Failed,
            Detail = "nothing matched within 2000 ms",
        });

        Assert.DoesNotContain("resolved", line);
        Assert.DoesNotContain("readBack", line);
        Assert.Contains("\"detail\":\"nothing matched within 2000 ms\"", line);
    }

    [Fact]
    public void A_step_survives_the_round_trip_whole()
    {
        var step = Resolved("#save") with { Step = 7 };

        Assert.Equal(step, TraceFormat.Parse(TraceFormat.Line(step)));
    }

    [Fact]
    public void The_writer_numbers_the_steps_and_the_caller_never_does()
    {
        var text = new StringWriter();
        using var trace = TraceWriter.To(text);

        Assert.Equal(1, trace.Write(Resolved("#save")).Step);
        Assert.Equal(2, trace.Write(Resolved("#cancel")).Step);
        Assert.Equal(2, trace.Count);

        var steps = TraceLog.Read(new StringReader(text.ToString()));
        Assert.Equal([1, 2], steps.Select(s => s.Step));
        Assert.Equal(["#save", "#cancel"], steps.Select(s => s.Locator));
    }

    [Fact]
    public void Every_step_before_the_one_that_broke_is_in_the_record()
    {
        var text = new StringWriter();
        using (var trace = TraceWriter.To(text))
        {
            trace.Write(Resolved("#open"));
            trace.Write(Resolved("#save"));
            trace.Write(new TraceStep
            {
                Verb = "assert",
                Locator = "#status",
                Resolved = "Text 'Saving...'",
                ReadBack = "Saving...",
                Verdict = StepVerdict.Failed,
                Detail = "expected 'Saved', read back 'Saving...'",
            });
        }

        var steps = TraceLog.Read(new StringReader(text.ToString()));

        Assert.Equal(3, steps.Count);
        Assert.All(steps.Take(2), step => Assert.Equal(StepVerdict.Ok, step.Verdict));
        Assert.Equal("expected 'Saved', read back 'Saving...'", steps[^1].Detail);
    }

    [Fact]
    public void A_blank_line_from_a_truncated_run_is_skipped_and_a_broken_one_is_not()
    {
        var steps = TraceLog.Read(new StringReader(TraceFormat.Line(Resolved("#save")) + "\n\n"));
        Assert.Single(steps);

        Assert.ThrowsAny<Exception>(() => TraceLog.Read(new StringReader("{\"verb\":")));
    }

    [Fact]
    public void The_trace_is_flushed_per_line_so_a_run_that_dies_keeps_what_it_saw()
    {
        var path = Path.Combine(Path.GetTempPath(), $"winwright-trace-{Guid.NewGuid():N}", "run.trace.jsonl");
        try
        {
            using (var trace = TraceWriter.ToFile(path))
            {
                trace.Write(Resolved("#open"));

                // Read it back while the writer is still open: this is the harness dying mid-run.
                using var reading = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(reading);
                Assert.Single(TraceLog.Read(reader));
            }

            Assert.Single(TraceLog.ReadFile(path));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }
}

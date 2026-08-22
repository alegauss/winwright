using Winwright.Capturing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW45. An animation has no observable, so a transition ships unlooked-at.
/// <para>
/// The discriminating test is the one about a slow capture. Every other property here — the
/// numbering, the count, the refusals — would hold just as well for a run that accumulated its
/// sleeps; only the recovery after an overrun tells the two implementations apart, and it is the
/// difference between a sequence whose timings mean something and one whose timings drift by the
/// length of whatever the slowest frame happened to be.
/// </para>
/// </summary>
public sealed class FrameRunTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), $"winwright-frames-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
    }

    [Fact]
    public void A_slow_capture_costs_its_own_frame_and_not_every_frame_after_it()
    {
        const int overrun = 60;

        // Frame two takes three intervals to capture. Under accumulation every later frame is
        // pushed by that overrun and the run ends 60ms late; held against a clock, the frames
        // whose slots passed during it are late and the rest are back where they belong.
        var sequence = FrameRun.At(50, 10, folder, path =>
        {
            File.WriteAllText(path, "frame");
            if (path.EndsWith("0002.png", StringComparison.Ordinal))
                Thread.Sleep(overrun);
        });

        Assert.True(
            sequence.Frames[^1].Drift < overrun,
            $"the last frame drifted {sequence.Frames[^1].Drift}ms, which is the overrun carried to the end: "
                + sequence.Sentence());

        // The slots themselves never move, whatever the captures did.
        Assert.Equal([0, 20, 40, 60, 80, 100, 120, 140, 160, 180], sequence.Frames.Select(frame => frame.DueMs));
    }

    [Fact]
    public void The_run_that_fell_behind_says_so_instead_of_reporting_a_count()
    {
        var sequence = FrameRun.At(50, 4, folder, path =>
        {
            File.WriteAllText(path, "frame");
            Thread.Sleep(40);
        });

        Assert.False(sequence.KeptUp);
        Assert.True(sequence.LostSlots > 0, sequence.Sentence());
        Assert.Contains("past the next slot by up to", sequence.Sentence());
        Assert.Contains("could not keep up", sequence.Sentence());
    }

    [Fact]
    public void A_run_the_capture_keeps_up_with_says_every_frame_was_in_its_slot()
    {
        var sequence = FrameRun.At(20, 4, folder, path => File.WriteAllText(path, "frame"));

        // Not "drifted by nothing": a scheduler that deschedules the thread for a few
        // milliseconds is a fact about this machine, and asserting it away would make the test
        // flaky rather than make the run accurate. What is claimed is that no slot was lost.
        Assert.True(sequence.KeptUp, sequence.Sentence());
        Assert.True(sequence.WorstDriftMs < sequence.IntervalMs, sequence.Sentence());
        Assert.Contains("every one inside its slot", sequence.Sentence());
        Assert.DoesNotContain("could not keep up", sequence.Sentence());
    }

    [Fact]
    public void The_frame_count_is_a_number_an_assertion_can_be_written_against()
    {
        var sequence = FrameRun.At(100, 7, folder, path => File.WriteAllText(path, "frame"));

        Assert.Equal(7, sequence.Frames.Count);
        Assert.Equal(7, Directory.GetFiles(folder, "*.png").Length);
        Assert.Equal(Enumerable.Range(1, 7), sequence.Frames.Select(frame => frame.Ordinal));
        Assert.All(sequence.Frames, frame => Assert.True(File.Exists(frame.Path)));
    }

    [Fact]
    public void The_names_sort_in_order_and_an_encoder_takes_the_whole_run_by_pattern()
    {
        var sequence = FrameRun.At(100, 3, folder, path => File.WriteAllText(path, "frame"));

        var written = Directory.GetFiles(folder, "*.png").Select(Path.GetFileName).Order(StringComparer.Ordinal);
        Assert.Equal(["frame-0001.png", "frame-0002.png", "frame-0003.png"], written);

        // The pattern and the names come off the same width, so an encoder handed one finds the
        // other. Written out twice they would agree until somebody asked for 10,000 frames.
        Assert.Equal("frame-%04d.png", FrameRun.PatternFor(3));
        Assert.Equal(sequence.Frames.Count, Directory.GetFiles(folder, "*.png").Length);
    }

    [Fact]
    public void A_longer_run_widens_the_number_and_the_pattern_with_it()
    {
        Assert.Equal("frame-0001.png", FrameRun.NameOf(1, 9999));
        Assert.Equal("frame-00001.png", FrameRun.NameOf(1, 10000));
        Assert.Equal("frame-12345.png", FrameRun.NameOf(12345, 99999));
        Assert.Equal("frame-%05d.png", FrameRun.PatternFor(10000));
        Assert.Equal("frame-0007.webp", FrameRun.NameOf(7, 100, "webp"));
    }

    [Fact]
    public void A_rate_or_a_length_that_names_no_run_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FrameRun.At(0, 3, folder, _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => FrameRun.At(-5, 3, folder, _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => FrameRun.At(25, 0, folder, _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FrameRun.At(FrameRun.FastestRate + 1, 3, folder, _ => { }));
        Assert.Throws<ArgumentNullException>(() => FrameRun.At(25, 3, folder, null!));
        Assert.Throws<ArgumentException>(() => FrameRun.At(25, 3, " ", _ => { }));

        // Nothing was written by any of them, which is what makes a refusal cheap to recover from.
        Assert.False(Directory.Exists(folder));
    }

    [Fact]
    public void An_empty_sequence_says_nothing_was_taken_rather_than_claiming_it_kept_up()
    {
        var empty = new FrameSequence([], 25, 40);

        Assert.Equal("no frames were taken.", empty.Sentence());
        Assert.Equal(0, empty.WorstDriftMs);
        Assert.Equal(0, empty.LostSlots);
    }

    [Fact]
    public void A_frame_that_drifted_inside_its_slot_did_not_cost_the_run_one()
    {
        var nudged = new CapturedFrame(3, "x.png", 80, 85);
        var overrun = new CapturedFrame(3, "x.png", 80, 120);

        Assert.Equal(5, nudged.Drift);
        Assert.False(nudged.LostASlot(40));
        Assert.True(overrun.LostASlot(40));
    }

    [Fact]
    public void A_frame_prints_its_slot_and_says_when_it_missed_one()
    {
        Assert.Equal("frame 3 at 80ms", new CapturedFrame(3, "x.png", 80, 80).ToString());
        Assert.Equal("frame 3 due at 80ms, taken at 95ms (15ms late)", new CapturedFrame(3, "x.png", 80, 95).ToString());
    }
}

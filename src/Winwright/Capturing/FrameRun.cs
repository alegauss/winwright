using System.Diagnostics;
using System.Globalization;

namespace Winwright.Capturing;

/// <summary>One frame of a sequence: where it went, when it was due and when it was taken.</summary>
/// <param name="Ordinal">Its number in the sequence, counting from one.</param>
/// <param name="Path">The file it was written to, named so an encoder can take the whole run.</param>
/// <param name="DueMs">When it should have been taken, measured from the first frame.</param>
/// <param name="AtMs">When it was taken.</param>
public sealed record CapturedFrame(int Ordinal, string Path, int DueMs, int AtMs)
{
    /// <summary>How far off its slot it landed. Never negative: a frame is not taken early.</summary>
    public int Drift => AtMs - DueMs;

    /// <summary>
    /// Whether it lost the sequence a slot, which needs the interval and so is asked of the
    /// sequence rather than answered here. Drifting by a millisecond is not the same claim, and
    /// a frame on its own has no way to tell the two apart.
    /// </summary>
    /// <param name="intervalMs">The gap between one slot and the next.</param>
    public bool LostASlot(int intervalMs) => Drift >= intervalMs;

    /// <summary>The one line a summary shows.</summary>
    public override string ToString() =>
        Drift > 0 ? $"frame {Ordinal} due at {DueMs}ms, taken at {AtMs}ms ({Drift}ms late)"
            : $"frame {Ordinal} at {DueMs}ms";
}

/// <summary>
/// What a run of frames recorded, which is when as well as what.
/// </summary>
/// <param name="Frames">Every frame, in order.</param>
/// <param name="Rate">The rate that was asked for, in frames per second.</param>
/// <param name="IntervalMs">The gap between one frame's slot and the next.</param>
public sealed record FrameSequence(IReadOnlyList<CapturedFrame> Frames, int Rate, int IntervalMs)
{
    /// <summary>
    /// How many frames cost the sequence a slot — landing at or after the moment the next frame
    /// was due, which is when the run has actually lost cadence.
    /// <para>
    /// The threshold is the interval and not zero, and that is derived rather than chosen: a
    /// frame that lands before the next slot has taken nothing from the sequence. Measured, a
    /// thread the scheduler descheduled for 5ms in a 50ms slot read as a capture that could not
    /// keep up, which is a sentence about the machine dressed up as one about the tool.
    /// </para>
    /// </summary>
    public int LostSlots => Frames.Count(frame => frame.LostASlot(IntervalMs));

    /// <summary>The worst a frame drifted, reported whether or not a slot was lost.</summary>
    public int WorstDriftMs => Frames.Count == 0 ? 0 : Frames.Max(frame => frame.Drift);

    /// <summary>Whether the run held its cadence.</summary>
    public bool KeptUp => LostSlots == 0;

    /// <summary>
    /// What was recorded, said out loud — including the drift, which is the half a caller cannot
    /// see by opening the files. A sequence that fell behind is still a sequence, and printing
    /// only the count would let a run at half the rate asked for read as one that kept up.
    /// </summary>
    public string Sentence()
    {
        if (Frames.Count == 0)
            return "no frames were taken.";

        var span = Frames[^1].AtMs;
        var many = Frames.Count == 1 ? "1 frame" : $"{Frames.Count} frames";
        var kept = $"{many} at {Rate}/s over {span}ms";

        // The drift is printed either way, because it is a fact about the run and a reader
        // comparing two sequences wants it. Only the verdict on it depends on the interval.
        return KeptUp
            ? $"{kept}, every one inside its slot (worst drift {WorstDriftMs}ms)."
            : $"{kept}, {LostSlots} of them past the next slot by up to {WorstDriftMs}ms: the capture "
                + "could not keep up with the rate that was asked for.";
    }
}

/// <summary>
/// Frames at a fixed rate into a numbered sequence.
/// <para>
/// An entrance, a fill or a confetti burst has no observable and ships unlooked-at. This is the
/// observable: a numbered run of files an encoder takes whole, and a frame count an assertion can
/// be written against instead of a picture somebody has to open.
/// </para>
/// <para>
/// Each frame's slot is computed from the first one — <c>n × 1000 / rate</c> — and held against a
/// stopwatch, never accumulated out of sleeps. The difference is the whole point of the task: a
/// capture that takes 40ms in a 40ms slot pushes every later frame by 40ms under accumulation, so
/// a one-second run at 25 frames a second finishes two seconds later and nothing in the output
/// says the timings are wrong. Held against a clock, that one frame is late and the next is back
/// in its slot, and <see cref="FrameSequence.LostSlots"/> says how many were.
/// </para>
/// <para>
/// It takes the capture rather than performing one. The off-screen render is WW34's, and the
/// cadence is testable without it — which is also why the timing here can be proven with a
/// deliberately slow capture rather than hoped about.
/// </para>
/// </summary>
public static class FrameRun
{
    /// <summary>
    /// The fastest rate this will run. Not a display limit — it is the point past which the
    /// gap between frames is under the granularity of the clock the wait is held against, so a
    /// sequence would report a cadence it cannot actually have kept.
    /// </summary>
    public const int FastestRate = 1000;

    /// <summary>
    /// Take <paramref name="frames"/> frames at <paramref name="rate"/> a second, writing each
    /// one through <paramref name="take"/>.
    /// </summary>
    /// <param name="rate">Frames per second.</param>
    /// <param name="frames">How many to take.</param>
    /// <param name="into">The directory the sequence is written to. Created where it is missing.</param>
    /// <param name="take">What actually captures one frame, given the path to write it to.</param>
    /// <param name="extension">The file extension, without the dot.</param>
    public static FrameSequence At(int rate, int frames, string into, Action<string> take, string extension = "png")
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rate);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(rate, FastestRate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frames);
        ArgumentException.ThrowIfNullOrWhiteSpace(into);
        ArgumentNullException.ThrowIfNull(take);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        Directory.CreateDirectory(into);

        var interval = 1000 / rate;
        var taken = new List<CapturedFrame>(frames);
        var clock = Stopwatch.StartNew();

        for (var ordinal = 1; ordinal <= frames; ordinal++)
        {
            // From the first frame every time, so a slow capture costs its own frame and not
            // every frame after it. This one expression is the task.
            var due = (int)((long)(ordinal - 1) * 1000 / rate);
            WaitUntil(clock, due);

            var path = System.IO.Path.Combine(into, NameOf(ordinal, frames, extension));
            var at = (int)clock.ElapsedMilliseconds;
            take(path);

            taken.Add(new CapturedFrame(ordinal, path, due, at));
        }

        return new FrameSequence(taken, rate, interval);
    }

    /// <summary>
    /// What one frame is called: zero-padded to a fixed width, so the run sorts in order and an
    /// encoder's own numbering pattern takes the whole sequence with no list of files.
    /// </summary>
    /// <param name="ordinal">Which frame, counting from one.</param>
    /// <param name="frames">How many there are in total, which sets the width.</param>
    /// <param name="extension">The file extension, without the dot.</param>
    public static string NameOf(int ordinal, int frames, string extension = "png")
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ordinal);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frames);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        var width = Math.Max(4, frames.ToString(CultureInfo.InvariantCulture).Length);
        return $"frame-{ordinal.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0')}.{extension}";
    }

    /// <summary>
    /// The pattern an encoder is given for a sequence of this length, matching
    /// <see cref="NameOf"/> — derived from the same width rather than written out twice.
    /// </summary>
    /// <param name="frames">How many frames the sequence holds.</param>
    /// <param name="extension">The file extension, without the dot.</param>
    public static string PatternFor(int frames, string extension = "png")
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frames);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        var width = Math.Max(4, frames.ToString(CultureInfo.InvariantCulture).Length);
        return $"frame-%0{width.ToString(CultureInfo.InvariantCulture)}d.{extension}";
    }

    private static void WaitUntil(Stopwatch clock, int dueMs)
    {
        // Already past it: the frame before this one overran. It is taken now and reported late,
        // rather than the run sleeping a negative interval or skipping the frame silently.
        while (true)
        {
            var left = dueMs - clock.ElapsedMilliseconds;
            if (left <= 0)
                return;

            // Sleep gives the millisecond back to the machine but is only accurate to about
            // fifteen of them, so the last stretch is spun rather than slept: at 25 frames a
            // second a fifteen-millisecond overshoot is more than a third of the interval.
            if (left > 16)
                Thread.Sleep((int)(left - 16));
            else
                Thread.SpinWait(200);
        }
    }
}

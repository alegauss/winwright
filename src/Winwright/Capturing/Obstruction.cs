using Winwright.Tracing;
using Winwright.Verdicts;
using Winwright.Windowing;

namespace Winwright.Capturing;

/// <summary>One window standing over the region a capture was about, and how much of it it takes.</summary>
/// <param name="Window">The window in the way, named rather than counted.</param>
/// <param name="Overlap">The part of the region it stands over, in screen coordinates.</param>
public sealed record Intruder(TopLevelWindow Window, WindowBounds Overlap)
{
    /// <summary>The one line a report names it by.</summary>
    public override string ToString()
    {
        var called = Window.Title.Length > 0 ? $"'{Window.Title}'" : $"a {Window.ClassName}";
        return $"{called} (pid {Window.Pid}) over {Overlap}";
    }
}

/// <summary>
/// What stands between a region and whoever is photographing it.
/// <para>
/// WW38. This used to be nine sampled points, and the capture taken to verify one task passed all
/// nine while carrying two windows of another process across its lower-right corner. More points
/// only move the threshold — the number that finally covers a window is the number of pixels in it —
/// so the question is asked about the region instead.
/// </para>
/// <para>
/// The z order above the window is enumerated and each frame intersected with the region, which
/// answers for the whole area in one pass. And it names the intruder rather than merely refusing:
/// what a reader handed a covered capture needs is which window to move, and a boolean sends them
/// to a screenshot to find out.
/// </para>
/// </summary>
public sealed record Obstruction
{
    private Obstruction(WindowBounds region, IReadOnlyList<Intruder> over, long covered, string absence)
    {
        Region = region;
        Over = over;
        Covered = covered;
        Absence = absence;
    }

    /// <summary>What this reading is called wherever it is reported.</summary>
    public const string PreconditionName = "nothing stands over the region being captured";

    /// <summary>The region that was asked about.</summary>
    public WindowBounds Region { get; }

    /// <summary>Every window above it that takes any of it, topmost first.</summary>
    public IReadOnlyList<Intruder> Over { get; }

    /// <summary>
    /// How much of the region is taken, in pixels, counting an area twice covered once. Two
    /// intruders that overlap each other cover less than the sum of what each covers, and a sum
    /// would report a region as more than wholly covered — which is a number nobody can act on.
    /// </summary>
    public long Covered { get; }

    /// <summary>Why the reading was not taken, where it was not. Empty where it was.</summary>
    public string Absence { get; }

    /// <summary>Whether the reading was taken at all.</summary>
    public bool Was => Absence.Length == 0;

    /// <summary>Whether the region is wholly the window's own.</summary>
    public bool Clear => Was && Over.Count == 0;

    /// <summary>What fraction of the region is taken, from nothing to all of it.</summary>
    public double Fraction => Region.Area == 0 ? 0 : (double)Covered / Region.Area;

    /// <summary>What was read, said either way.</summary>
    public string Sentence()
    {
        if (!Was)
            return $"nothing could be read about what stands over {Region}: {Absence}.";

        if (Over.Count == 0)
            return $"nothing stands over {Region}.";

        var named = string.Join(", ", Over.Select(one => one.ToString()));
        return $"{Over.Count} window(s) stand over {Region}, taking {Covered} of its "
            + $"{Region.Area} pixel(s): {named}.";
    }

    /// <inheritdoc cref="Sentence" />
    public override string ToString() => Sentence();

    /// <summary>
    /// The result a verdict counts. A window somebody else put over the region is a fact about the
    /// desk and not a defect in the code under test, so it is a hole — this block's neighbour
    /// criterion says nothing about the desk is reported as a defect in the code.
    /// </summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult AsAssertion(string named) => Clear
        ? AssertionResult.Pass(named, Sentence())
        : AssertionResult.Unchecked(named, Precondition.Absent(PreconditionName, Sentence()));

    /// <summary>The step a trace records.</summary>
    public TraceStep AsTraceStep(string named) => new()
    {
        Verb = "read what stands over the region",
        Locator = named,
        Resolved = Region.ToString(),
        Pattern = "the z order above the window",
        ReadBack = Was ? $"{Over.Count} over, {Covered} of {Region.Area} pixel(s)" : null,
        Verdict = Clear ? StepVerdict.Ok : StepVerdict.Unchecked,
        Detail = Clear ? null : Sentence(),
    };

    /// <summary>
    /// Read what stands over a region, from the top of the z order down to the window that owns it.
    /// <para>
    /// <c>EnumWindows</c> walks the z order topmost first, so everything before the window under
    /// test is above it and everything after is below. Stopping at the window is what makes this a
    /// question about occlusion rather than about the desktop: a window behind the one being
    /// photographed takes none of the picture.
    /// </para>
    /// </summary>
    /// <param name="window">The window being photographed, whose own frame is not an intruder.</param>
    /// <param name="region">The part of the screen the capture is about.</param>
    public static Obstruction Reading(nint window, WindowBounds region)
    {
        if (window == 0)
            return new Obstruction(region, [], 0, "no window was named, and nothing is above nothing");

        if (region.Width <= 0 || region.Height <= 0)
            return new Obstruction(region, [], 0, $"{region} has no area, so nothing can stand over it");

        var above = new List<Intruder>();
        var reached = false;

        Win32.EnumWindows(
            (candidate, _) =>
            {
                if (candidate == window)
                {
                    reached = true;
                    return false;
                }

                if (!Win32.IsWindowVisible(candidate) || Cloaking.Of(candidate) != Cloak.NotCloaked)
                    return true;

                if (!Win32.GetWindowRect(candidate, out var rectangle))
                    return true;

                var bounds = new WindowBounds(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
                var overlap = Meeting(bounds, region);
                if (overlap.Area == 0)
                    return true;

                Win32.GetWindowThreadProcessId(candidate, out var owner);
                above.Add(new Intruder(
                    new TopLevelWindow(
                        candidate,
                        (int)owner,
                        Win32.TextOf(candidate),
                        Win32.ClassOf(candidate),
                        bounds,
                        true,
                        Win32.GetWindow(candidate, Win32.GwOwner),
                        Cloak.NotCloaked),
                    overlap));

                return true;
            },
            0);

        // A window the walk never reached is one that has gone, or one no longer on the desktop's
        // own list. Everything collected is then above something that is not there, which is not a
        // reading about occlusion and is reported as no reading rather than as a clear region.
        if (!reached)
            return new Obstruction(region, [], 0, "the window was not on the desktop's z order, so nothing above it means anything");

        return new Obstruction(region, above, Union(above.Select(one => one.Overlap).ToList()), "");
    }

    /// <summary>Where two rectangles meet, or an empty one where they do not.</summary>
    private static WindowBounds Meeting(WindowBounds left, WindowBounds right)
    {
        var meeting = new WindowBounds(
            Math.Max(left.Left, right.Left),
            Math.Max(left.Top, right.Top),
            Math.Min(left.Right, right.Right),
            Math.Min(left.Bottom, right.Bottom));

        return meeting.Width <= 0 || meeting.Height <= 0 ? new WindowBounds(0, 0, 0, 0) : meeting;
    }

    /// <summary>
    /// How much area a set of rectangles covers between them, counting twice-covered area once.
    /// <para>
    /// Compressed coordinates rather than a pixel grid: the edges are the only places coverage can
    /// change, so the answer is exact and costs the number of windows rather than the number of
    /// pixels. Summing the parts instead would report a region as more than wholly covered the
    /// moment two intruders overlapped each other, which is the arithmetic a reader cannot use.
    /// </para>
    /// </summary>
    private static long Union(IReadOnlyList<WindowBounds> parts)
    {
        if (parts.Count == 0)
            return 0;

        var xs = parts.SelectMany(one => new[] { one.Left, one.Right }).Distinct().Order().ToList();
        var ys = parts.SelectMany(one => new[] { one.Top, one.Bottom }).Distinct().Order().ToList();

        long covered = 0;
        for (var x = 0; x + 1 < xs.Count; x++)
        {
            for (var y = 0; y + 1 < ys.Count; y++)
            {
                var cell = new WindowBounds(xs[x], ys[y], xs[x + 1], ys[y + 1]);
                if (parts.Any(one => Meeting(one, cell).Area == cell.Area && cell.Area > 0))
                    covered += cell.Area;
            }
        }

        return covered;
    }
}

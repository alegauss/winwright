using Winwright.Tracing;
using Winwright.Verdicts;
using Winwright.Windowing;

namespace Winwright.Capturing;

/// <summary>How a reported surface sits against the rectangle a capture read.</summary>
public enum Sits
{
    /// <summary>Wholly inside. The only answer that makes the capture evidence of anything.</summary>
    Inside,

    /// <summary>Partly inside: the copy clipped it, which is a rectangle that is too small.</summary>
    Clipped,

    /// <summary>Nowhere near it: the copy is of something else, which is a different repair.</summary>
    Elsewhere,

    /// <summary>The application reported a rectangle of no area, which no copy can contain.</summary>
    Nothing,
}

/// <summary>
/// Whether the capture contains the surface it was taken for.
/// <para>
/// Verifying one task in claude-tray cost three captures and a full-screen grab, and none of the
/// three failed: the script reported success, named the right window each time, and the file
/// simply did not contain the note the flag exists to show. Nothing was checking that the capture
/// contained the surface it was taken for, and nothing could — only the application knows what it
/// drew and where.
/// </para>
/// <para>
/// Clipped and elsewhere are told apart because they are two repairs. A surface clipped by eleven
/// rows is a copy rectangle that is too small; a surface nowhere near the copy is a capture of the
/// wrong window, and reading one as the other sends somebody adjusting a margin for an afternoon.
/// </para>
/// <para>
/// Being outside is not by itself a defect in the application: a popup is its own top-level window
/// and a correct copy of the main window can honestly not contain it. Which is exactly why this is
/// asserted rather than inferred — the case names the surface it meant, and the answer is about
/// that surface and no other.
/// </para>
/// </summary>
public sealed record Containment
{
    private Containment(ReportedSurface surface, WindowBounds copy, Sits sits)
    {
        Surface = surface;
        Copy = copy;
        Sits = sits;
    }

    /// <summary>The surface the application reported.</summary>
    public ReportedSurface Surface { get; }

    /// <summary>The rectangle the capture read, in the same physical pixels.</summary>
    public WindowBounds Copy { get; }

    /// <summary>How the two sit against each other.</summary>
    public Sits Sits { get; }

    /// <summary>Whether the capture contains the whole of it.</summary>
    public bool Contains => Sits == Sits.Inside;

    /// <summary>Columns of the surface left of the copy. Zero where none are.</summary>
    public int OverLeft => Math.Max(0, Copy.Left - Surface.Bounds.Left);

    /// <summary>Rows of it above the copy.</summary>
    public int OverTop => Math.Max(0, Copy.Top - Surface.Bounds.Top);

    /// <summary>Columns of it right of the copy.</summary>
    public int OverRight => Math.Max(0, Surface.Bounds.Right - Copy.Right);

    /// <summary>Rows of it below the copy.</summary>
    public int OverBottom => Math.Max(0, Surface.Bounds.Bottom - Copy.Bottom);

    /// <summary>Read one surface against one copy rectangle.</summary>
    /// <param name="copy">What the capture read, in physical pixels.</param>
    /// <param name="surface">What the application said it drew.</param>
    public static Containment Of(WindowBounds copy, ReportedSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        var drawn = surface.Bounds;
        if (drawn.Width <= 0 || drawn.Height <= 0)
            return new Containment(surface, copy, Sits.Nothing);

        if (copy.Left <= drawn.Left && copy.Top <= drawn.Top
            && copy.Right >= drawn.Right && copy.Bottom >= drawn.Bottom)
        {
            return new Containment(surface, copy, Sits.Inside);
        }

        // Any overlap at all is a clip; none is a capture of something else. The two are told
        // apart on the rectangles rather than on how far out it is, because a surface one pixel
        // outside and a surface on another display are both "outside" and neither is the same bug.
        var overlaps = copy.Left < drawn.Right && drawn.Left < copy.Right
            && copy.Top < drawn.Bottom && drawn.Top < copy.Bottom;

        return new Containment(surface, copy, overlaps ? Sits.Clipped : Sits.Elsewhere);
    }

    /// <summary>The whole reading, in the sentence a red step carries.</summary>
    public string Sentence() => Sits switch
    {
        Sits.Inside => $"the capture contains '{Surface.Name}': {Surface.Bounds} is inside {Copy}.",
        Sits.Nothing => $"the application reported '{Surface.Name}' as {Surface.Bounds}, which has no area — "
            + "no copy contains a rectangle nothing occupies.",
        Sits.Elsewhere => $"the capture does not contain '{Surface.Name}': {Surface.Bounds} does not touch "
            + $"{Copy} at all, so this is a picture of something else.",
        _ => $"the capture clips '{Surface.Name}': {Surface.Bounds} sticks out of {Copy} by {Sides()}.",
    };

    /// <summary>The result a verdict counts, under the name the case gives the check.</summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult AsAssertion(string named) =>
        Contains ? AssertionResult.Pass(named, Sentence()) : AssertionResult.Fail(named, Sentence());

    /// <summary>
    /// The step a trace records. WW163: where a surface sat against the copy taken of it is the
    /// reading behind the verdict, and a record that kept only the verdict sends a reader back to
    /// the picture to work out what was measured.
    /// </summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public TraceStep AsTraceStep(string named) => new()
    {
        Verb = "contain",
        Locator = named,
        Resolved = Surface.Name,
        ReadBack = Copy.ToString(),
        Verdict = Contains ? StepVerdict.Ok : StepVerdict.Failed,
        Detail = Contains ? null : Sentence(),
    };

    private string Sides()
    {
        var parts = new List<string>();
        if (OverLeft > 0)
            parts.Add($"left {OverLeft}");
        if (OverTop > 0)
            parts.Add($"top {OverTop}");
        if (OverRight > 0)
            parts.Add($"right {OverRight}");
        if (OverBottom > 0)
            parts.Add($"bottom {OverBottom}");

        return string.Join(", ", parts);
    }
}

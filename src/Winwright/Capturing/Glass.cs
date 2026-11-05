using System.Runtime.InteropServices;

using Winwright.Tracing;
using Winwright.Verdicts;

namespace Winwright.Capturing;

/// <summary>What the compositor is doing with a window's background.</summary>
public enum SystemBackdrop
{
    /// <summary>The compositor would not say. Not a backdrop — an answer this run did not get.</summary>
    Unread = -1,

    /// <summary>
    /// The window never asked, so the compositor decides — and for an ordinary window it decides
    /// none. Told apart from <see cref="None" /> because asking for nothing and never having asked
    /// are two facts, and a build that accepts the call and reports this is the difference.
    /// </summary>
    Auto = 0,

    /// <summary>Asked for, and asked for nothing.</summary>
    None = 1,

    /// <summary>Mica. The desktop wallpaper, tinted, through the window.</summary>
    Mica = 2,

    /// <summary>Acrylic. Whatever is behind the window, blurred, through the window.</summary>
    Acrylic = 3,

    /// <summary>Tabbed. Mica's other spelling, for a window with a tab strip.</summary>
    Tabbed = 4,
}

/// <summary>
/// Whether a window's own glass is carrying what is behind it into the picture.
/// <para>
/// WW41. Measured in freewilly: with nothing overlapping, a copy still carried a blurred image of
/// the desktop behind the window — another application's content legible through the frame —
/// because a Fluent window's backdrop composites what is behind it by design.
/// </para>
/// <para>
/// Z-order reasoning cannot answer for that, and neither can <see cref="Obstruction" />: the
/// intruder is not in front of the window, it is showing through it. So the compositor is asked
/// directly, which makes the refusal positive evidence rather than a name — the window says which
/// backdrop it opted into, and a run reports what it was told rather than what it inferred.
/// </para>
/// <para>
/// A printed warning was the first response and was not enough. A warning is not a refusal, and
/// the file gets written either way.
/// </para>
/// </summary>
public sealed record Glass
{
    private Glass(SystemBackdrop backdrop, string absence)
    {
        Backdrop = backdrop;
        Absence = absence;
    }

    /// <summary>What this reading is called wherever it is reported.</summary>
    public const string PreconditionName = "the window's own glass carries nothing from behind it";

    /// <summary>DWMWA_SYSTEMBACKDROP_TYPE. The one attribute that says what the glass is doing.</summary>
    private const int SystemBackdropType = 38;

    /// <summary>Which backdrop the window opted into, as the compositor reports it.</summary>
    public SystemBackdrop Backdrop { get; }

    /// <summary>Why nothing could be read, where nothing could. Empty where it could.</summary>
    public string Absence { get; }

    /// <summary>Whether the compositor answered at all.</summary>
    public bool Was => Absence.Length == 0;

    /// <summary>
    /// Whether the glass carries what is behind the window into a copy of it. Mica, acrylic and
    /// tabbed all do; asked-for-nothing and never-asked do not.
    /// </summary>
    public bool Transmits => Backdrop is SystemBackdrop.Mica or SystemBackdrop.Acrylic or SystemBackdrop.Tabbed;

    /// <summary>What was read, said either way.</summary>
    public string Sentence()
    {
        if (!Was)
            return $"what the window's glass is doing could not be read: {Absence}.";

        if (!Transmits)
            return Backdrop == SystemBackdrop.Auto
                ? "the window never asked for a backdrop, so its glass carries nothing from behind it."
                : "the window asked for no backdrop, so its glass carries nothing from behind it.";

        return $"the window opted into the {Backdrop.ToString().ToLowerInvariant()} backdrop, so a copy of it "
            + "carries whatever is behind it through the glass.";
    }

    /// <inheritdoc cref="Sentence" />
    public override string ToString() => Sentence();

    /// <summary>
    /// The result a verdict counts. A window that opted into a backdrop is a fact about the window
    /// and not about the desk, but it is still not something the run failed to do — the picture was
    /// never taken, so it is a hole, and the caller refusing on it is what stops the file existing.
    /// </summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult AsAssertion(string named) => Was && !Transmits
        ? AssertionResult.Pass(named, Sentence())
        : AssertionResult.Unchecked(named, Precondition.Absent(PreconditionName, Sentence()));

    /// <summary>The step a trace records.</summary>
    public TraceStep AsTraceStep(string named) => new()
    {
        Verb = "read the window's backdrop",
        Locator = named,
        Pattern = "DWMWA_SYSTEMBACKDROP_TYPE",
        ReadBack = Was ? Backdrop.ToString() : null,
        Verdict = Was && !Transmits ? StepVerdict.Ok : StepVerdict.Unchecked,
        Detail = Was && !Transmits ? null : Sentence(),
    };

    /// <summary>
    /// Ask the compositor what a window's glass is doing.
    /// <para>
    /// The attribute is refused on a build that does not have it, and that is reported as a reading
    /// not taken rather than as a window with no backdrop. A copy taken on such a build may still be
    /// carrying the desktop through the glass; what is different is that nothing here knows.
    /// </para>
    /// </summary>
    /// <param name="window">The window to ask about.</param>
    public static Glass Of(nint window)
    {
        if (window == 0)
            return new Glass(SystemBackdrop.Unread, "no window was named");

        var read = 0;
        var answered = DwmGetWindowAttribute(window, SystemBackdropType, out read, sizeof(int));
        if (answered != 0)
            return new Glass(SystemBackdrop.Unread, $"the compositor refused the attribute (0x{answered:x8})");

        return Enum.IsDefined(typeof(SystemBackdrop), read) && read >= 0
            ? new Glass((SystemBackdrop)read, "")
            : new Glass(SystemBackdrop.Unread, $"the compositor answered {read}, which is not a backdrop this knows");
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);
}

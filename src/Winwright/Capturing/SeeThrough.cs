using System.Runtime.InteropServices;

using Winwright.Tracing;
using Winwright.Verdicts;

namespace Winwright.Capturing;

/// <summary>How a layered window's pixels reach the screen.</summary>
public enum Layering
{
    /// <summary>Nothing could be read. Not a layer — an answer this run did not get.</summary>
    Unread = -1,

    /// <summary>Not layered at all, which is what an ordinary window is.</summary>
    None,

    /// <summary>Layered, and set to full alpha with no colour key: composited, and opaque anyway.</summary>
    Opaque,

    /// <summary>Layered at an alpha below full, so everything behind it shows through everywhere.</summary>
    Translucent,

    /// <summary>Layered with a colour key, so wherever the window drew that colour, the desktop is.</summary>
    Keyed,

    /// <summary>
    /// Layered with an alpha per pixel, which is what <c>UpdateLayeredWindow</c> makes and what
    /// every soft-edged overlay is. How much shows through is a property of the pixels and there is
    /// no attribute to read, so this is composited and nothing can say how much.
    /// </summary>
    PerPixel,
}

/// <summary>
/// Whether a window's own pixels are composited with what is behind them. WW334.
/// <para>
/// <see cref="Glass" /> asks the compositor which system backdrop a window opted into, and answers
/// that question correctly. Layering is the other way the same thing happens, and it answers
/// <see cref="SystemBackdrop.Auto" /> to the first question while being entirely see-through.
/// </para>
/// <para>
/// Measured beside freewilly's menu. The <c>SysShadow</c> window Windows draws behind a
/// drop-shadowed popup is WS_POPUP with no caption, owned by nothing, and two pixels larger on
/// every side — so it is a popup by every test the route has, and a copy of its rectangle is a copy
/// of whatever the menu is standing in front of. It reads <c>ex=0x000800A8</c>: layered, and
/// <c>GetLayeredWindowAttributes</c> refuses, which is what a per-pixel alpha answers.
/// </para>
/// <para>
/// Its own reading and not a second field on <see cref="Glass" />, because the two are refused
/// differently. A backdrop is exempt on a menu — a menu on this shell has acrylic by design, and
/// refusing on it would refuse every capture the copy route exists to take. Layering is exempt on
/// nothing: the shadow behind that menu is a popup too, and it is the one surface beside it that
/// must never be photographed.
/// </para>
/// </summary>
public sealed record SeeThrough
{
    private SeeThrough(Layering Layers, byte alpha, string absence)
    {
        this.Layers = Layers;
        Alpha = alpha;
        Absence = absence;
    }

    /// <summary>What this reading is called wherever it is reported.</summary>
    public const string PreconditionName = "the window's own pixels carry nothing from behind it";

    /// <summary>GWL_EXSTYLE, which is where WS_EX_LAYERED is.</summary>
    private const int ExtendedStyle = -20;

    /// <summary>WS_EX_LAYERED.</summary>
    private const long LayeredStyle = 0x0008_0000L;

    /// <summary>LWA_COLORKEY: the colour the window draws where the desktop should show.</summary>
    private const uint ByColourKey = 0x0000_0001;

    /// <summary>LWA_ALPHA: one alpha for the whole window.</summary>
    private const uint ByAlpha = 0x0000_0002;

    /// <summary>Full alpha, which is a layered window that hides what is behind it after all.</summary>
    private const byte Solid = 255;

    /// <summary>How this window's pixels reach the screen.</summary>
    public Layering Layers { get; }

    /// <summary>
    /// The alpha the window is set to, where one whole-window alpha was read. <see cref="Solid" />
    /// where none was — which is not a claim that the window is opaque, only that this field is not
    /// what says otherwise.
    /// </summary>
    public byte Alpha { get; }

    /// <summary>Why nothing could be read, where nothing could. Empty where it could.</summary>
    public string Absence { get; }

    /// <summary>Whether anything answered at all.</summary>
    public bool Was => Absence.Length == 0;

    /// <summary>
    /// Whether a copy of this window's rectangle is partly a copy of what is behind it.
    /// <para>
    /// A layered window at full alpha with no colour key is not: it is composited and it composites
    /// to itself. Every other layered window is, and the per-pixel one most of all — its edges are
    /// the desktop by construction.
    /// </para>
    /// </summary>
    public bool Transmits => Layers is Layering.Translucent or Layering.Keyed or Layering.PerPixel;

    /// <summary>What was read, said whichever way it came out.</summary>
    public string Sentence() => Layers switch
    {
        Layering.Unread => $"how this window's pixels reach the screen could not be read: {Absence}.",
        Layering.None => "the window is not layered, so its pixels are its own.",
        Layering.Opaque => "the window is layered at full alpha with no colour key, so it composites to itself.",
        Layering.Translucent =>
            $"the window is layered at an alpha of {Alpha} of {Solid}, so a copy of it carries what is behind it.",
        Layering.Keyed =>
            "the window is layered with a colour key, so wherever it drew that colour a copy of it is the desktop.",
        _ => "the window is layered with an alpha per pixel, so how much of it is the desktop is a "
            + "property of the pixels and there is no attribute that says.",
    };

    /// <inheritdoc cref="Sentence" />
    public override string ToString() => Sentence();

    /// <summary>
    /// The result a verdict counts. A window somebody made see-through is a fact about the window
    /// and not something the run failed to do, so it is a hole — the same way
    /// <see cref="Glass.AsAssertion" /> answers, and for the same reason: the picture was never
    /// taken, and what stops the file existing is the caller refusing on this.
    /// </summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult AsAssertion(string named) => Was && !Transmits
        ? AssertionResult.Pass(named, Sentence())
        : AssertionResult.Unchecked(named, Precondition.Absent(PreconditionName, Sentence()));

    /// <summary>The step a trace records.</summary>
    public TraceStep AsTraceStep(string named) => new()
    {
        Verb = "read whether the window is see-through",
        Locator = named,
        Pattern = "WS_EX_LAYERED",
        ReadBack = Was ? Layers.ToString() : null,
        Verdict = Was && !Transmits ? StepVerdict.Ok : StepVerdict.Unchecked,
        Detail = Was && !Transmits ? null : Sentence(),
    };

    /// <summary>
    /// Ask a window how its pixels reach the screen.
    /// <para>
    /// Two calls and three outcomes. The style bit says whether the window is layered at all; the
    /// attributes say how, and refuse for a window that set its layer per pixel — which is the
    /// answer rather than a failure, and the one the shadow behind a menu gives.
    /// </para>
    /// </summary>
    /// <param name="window">The window to ask about.</param>
    public static SeeThrough Of(nint window)
    {
        if (window == 0)
            return new SeeThrough(Layering.Unread, Solid, "no window was named");

        var style = (long)Windowing.Win32.GetWindowLongPtrW(window, ExtendedStyle);
        if ((style & LayeredStyle) == 0)
            return new SeeThrough(Layering.None, Solid, "");

        if (!GetLayeredWindowAttributes(window, out _, out var alpha, out var how))
        {
            // Measured on the shadow behind freewilly's menu: layered, and this refuses. A window
            // whose layer is per pixel has no whole-window attribute to answer with, so the refusal
            // is the answer — and it is the one that matters, because a per-pixel layer is soft
            // edges and soft edges are the desktop.
            return new SeeThrough(Layering.PerPixel, Solid, "");
        }

        if ((how & ByColourKey) != 0)
            return new SeeThrough(Layering.Keyed, alpha, "");

        if ((how & ByAlpha) != 0 && alpha < Solid)
            return new SeeThrough(Layering.Translucent, alpha, "");

        return new SeeThrough(Layering.Opaque, alpha, "");
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLayeredWindowAttributes(nint window, out uint key, out byte alpha, out uint flags);
}

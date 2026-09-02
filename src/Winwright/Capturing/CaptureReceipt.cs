using Winwright.Processes;
using Winwright.Tracing;
using Winwright.Verdicts;
using Winwright.Windowing;

namespace Winwright.Capturing;

/// <summary>Raised where the facts of a capture do not describe the capture that was asked for.</summary>
public sealed class WrongCaptureException : InvalidOperationException
{
    /// <summary>Say what was photographed and why it is not what the run was driving.</summary>
    public WrongCaptureException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// The same, saying which of the ways a capture can be wrong this one is.
    /// <para>
    /// WW188. A refusal is what a reader meets, and a reader meets an arm rather than a type. This
    /// one was five when that was written — another process's window, a window nothing is drawing, a
    /// region another window stood over, a window whose glass transmits, and a picture of exactly
    /// one colour — and the catalogue that pairs every refusal with something that provokes it
    /// counted them as one, so four were invisible to it. WW195 added the sixth, which arrived
    /// paired because of this.
    /// </para>
    /// </summary>
    /// <param name="arm">Which way this capture is wrong.</param>
    /// <param name="message">What was photographed and why it is not what the run was driving.</param>
    public WrongCaptureException(WrongCapture arm, string message)
        : base(message)
    {
        Arm = arm;
    }

    /// <summary>Unused. Present because an exception with no default shapes is awkward to catch.</summary>
    public WrongCaptureException()
        : base("the capture does not describe what this run was driving")
    {
    }

    /// <summary>Unused. Present for the same reason.</summary>
    public WrongCaptureException(string message, Exception inner)
        : base(message, inner)
    {
    }

    /// <summary>
    /// Which way this capture is wrong. <see cref="WrongCapture.Unsaid" /> where it was thrown
    /// without saying — which is a refusal nothing can pair, and the check says so.
    /// </summary>
    public WrongCapture Arm { get; } = WrongCapture.Unsaid;
}

/// <summary>
/// The ways a capture can be wrong, each of which a file on disk looks exactly the same as.
/// <para>
/// WW188. Named here rather than told apart by the sentence they carry: a case matching a phrase is
/// one that starts matching a different arm the day somebody rewords a message, and a catalogue
/// keyed on the type could only ever count every refusal it has as one.
/// </para>
/// </summary>
public enum WrongCapture
{
    /// <summary>Thrown without saying which. Pairs with nothing, and the suite refuses it.</summary>
    Unsaid,

    /// <summary>The window belongs to a process this run is not driving.</summary>
    AnotherProcess,

    /// <summary>Nothing was drawing the window, so the picture is of something cloaked.</summary>
    NothingDrawing,

    /// <summary>Another window stood over the region while it was copied.</summary>
    RegionCovered,

    /// <summary>
    /// The region was clear when the capture started and covered when it finished, so a window
    /// arrived inside the take. WW195: distinct from <see cref="RegionCovered" /> because the
    /// reading that would have refused was taken before the intruder existed.
    /// </summary>
    DeskChanged,

    /// <summary>The window's own glass carries what is behind it into the copy.</summary>
    GlassTransmits,

    /// <summary>
    /// The window's own pixels are composited with what is behind them, because it is layered.
    /// WW334: the other way a window is see-through, and the one no route exempts — the shadow
    /// behind a menu is a popup exactly as the menu is, and it is the desktop.
    /// </summary>
    LayerTransmits,

    /// <summary>The picture is one flat colour, which is not a picture of a window.</summary>
    OneFlatColour,
}

/// <summary>
/// What a capture says about itself.
/// <para>
/// The assertions decide whether a file is written; this decides whether a wrong one reports
/// itself. The failure this whole project started over was caught by a person reading the
/// picture, and a capture that names the window, the process and the arguments behind it is a
/// capture somebody can disbelieve — which is the property a silent success does not have.
/// </para>
/// <para>
/// Every field is read off something that was already established rather than passed in beside
/// the file name: the window comes from the enumeration, the process and the arguments from the
/// target this run reached. So the line cannot describe a capture other than the one taken, which
/// is the failure mode a hand-written success message has.
/// </para>
/// </summary>
public sealed record CaptureReceipt
{
    private CaptureReceipt(
        string path,
        TopLevelWindow window,
        AppTarget target,
        PaintedFrame? frame,
        CaptureRoute? route,
        RegionThroughout? over,
        Glass? glass,
        ColourCheck? colours)
    {
        Path = path;
        Window = window;
        Target = target;
        Frame = frame;
        Route = route;
        Over = over;
        Glass = glass;
        Colours = colours;
    }

    /// <summary>
    /// How the window's own pixels reach the screen, where a caller asked. WW334, and null where
    /// nobody did — which is not the same as a window that is not layered, for the reason every
    /// other reading here is null rather than reassuring.
    /// </summary>
    public SeeThrough? Layers { get; init; }

    /// <summary>
    /// What counting the picture's colours said, where a caller counted. Null where nobody did,
    /// which is not the same as a picture with more than one colour in it.
    /// </summary>
    public ColourCheck? Colours { get; }

    /// <summary>
    /// What the window's own glass was doing, where a caller asked. Null where nobody asked, which
    /// is not the same as a window carrying nothing through it.
    /// </summary>
    public Glass? Glass { get; }

    /// <summary>
    /// What stood over the region, where a caller read it. Null where none did — and null is not
    /// "nothing stood over it": a receipt that could say either from one value would be claiming an
    /// emptiness nobody looked for, which is the shape this project keeps withdrawing.
    /// </summary>
    public RegionThroughout? Over { get; }

    /// <summary>The file that was written.</summary>
    public string Path { get; }

    /// <summary>The window it is a picture of, as the enumeration found it.</summary>
    public TopLevelWindow Window { get; }

    /// <summary>How this run reached the application, which is where the process and pid come from.</summary>
    public AppTarget Target { get; }

    /// <summary>What was copied against what the window owns, where that was read.</summary>
    public PaintedFrame? Frame { get; }

    /// <summary>
    /// Which way this picture was got and why, where the caller said. Null on a capture that did
    /// not route itself — and a null here reads as unrecorded, never as the default having been
    /// taken, because a screen copy nobody wrote down is exactly the one worth writing down.
    /// </summary>
    public CaptureRoute? Route { get; }

    /// <summary>
    /// Whether the arguments behind the picture are knowable. Met on a launch, absent on an
    /// attach — and absent is reported rather than printed as an empty string, because a capture
    /// claiming no arguments and one that cannot know them are different claims.
    /// </summary>
    public Precondition Arguments => Target.LaunchArguments;

    /// <summary>
    /// Take a capture and compose its receipt, asking the questions a capture has to be asked
    /// rather than leaving them to a caller.
    /// <para>
    /// WW187. WW38, WW41 and WW42 gave this block three readings a capture needs, and WW40 gave the
    /// receipt the refusals that fire on them — all as optional arguments, so a caller who passed
    /// none got a receipt that refused nothing and recorded honestly that nobody asked. Recording it
    /// honestly is the part that worked. What did not is that nothing asked.
    /// </para>
    /// <para>
    /// The argument is this repository's own, from <c>Preamble.Of</c>: a reading reached by its own
    /// call is one a runner is free to forget, and the forgotten one stops being measured while
    /// every assertion that needed it starts passing. So the composition lives here, the way WW170
    /// put the run's in one place — and <see cref="Of" /> is still there for a caller who wants the
    /// pieces.
    /// </para>
    /// <para>
    /// Which questions apply is the route's business and not the caller's. A window standing over
    /// the region and a backdrop transmitting through it are both about a copy of the screen; an
    /// off-screen render draws the visual tree with the compositor not involved and neither can
    /// reach it, which is WW194. The colour count applies to whatever was written, because a flat
    /// rectangle is not a picture of a window however it was got.
    /// </para>
    /// </summary>
    /// <param name="path">Where the capture is to be written.</param>
    /// <param name="window">The window being photographed.</param>
    /// <param name="target">How this run reached the application.</param>
    /// <param name="take">What writes the file. Given the path, and called between the readings.</param>
    /// <param name="frame">What is being copied against what the window owns, where it was read.</param>
    /// <param name="route">Which way the picture is being got, and why.</param>
    /// <exception cref="WrongCaptureException">
    /// Where any of the questions answers wrongly. The file is written either way, because a
    /// picture nobody may trust is still evidence about what went wrong — what the refusal withdraws
    /// is the claim that it is a capture.
    /// </exception>
    public static CaptureReceipt Taking(
        string path,
        TopLevelWindow window,
        AppTarget target,
        Action<string> take,
        PaintedFrame? frame = null,
        CaptureRoute? route = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(take);

        var copied = route?.Renders is not true;

        // WW195. Both sides of the take, and this used to be the instant before it alone. That is
        // the closest observable instant and it is not the instant the picture was taken: a window
        // arriving between the reading and the write is in the copy and in nothing else, and the
        // receipt said the region was clear because it was, a moment earlier.
        var glass = copied ? Glass.Of(window.Handle) : null;

        // WW334. The other way the window's own pixels are the desktop's, asked beside the first and
        // on the same route: a render draws the visual tree with the compositor not involved, so
        // there is no layer for it to have been composited through.
        var layers = copied ? SeeThrough.Of(window.Handle) : null;

        RegionThroughout? over = null;
        if (copied)
            over = RegionThroughout.Around(window.Handle, frame?.Painted ?? window.Bounds, () => take(path));
        else
            take(path);

        // And after, because this one is about the file. A capture that was never written is not a
        // flat one, and Colours refuses rather than answering — so the absence reaches the caller
        // as itself instead of as a picture of one colour.
        var colours = File.Exists(path) ? Capturing.Colours.In(path) : null;

        return Of(path, window, target, frame, route, over, glass, colours, layers);
    }

    /// <summary>
    /// Compose a receipt, refusing where the facts disagree with each other.
    /// </summary>
    /// <param name="path">The file that was written.</param>
    /// <param name="window">The window that was photographed.</param>
    /// <param name="target">How this run reached the application.</param>
    /// <param name="frame">What was copied against what the window owns, where it was read.</param>
    /// <param name="route">Which way the picture was got, and why.</param>
    /// <param name="over">
    /// What stood over the region while it was taken, where a caller read it.
    /// <para>
    /// WW40. An overlap fails rather than crops. Since the copied rectangle is the painted frame
    /// there is no invisible border left for a foreign window to hide in, so an overlap is inside
    /// real content — and a file quietly trimmed to dodge one is a picture of something nobody
    /// asked for. Left null by a caller that did not read it, because a receipt claiming nothing
    /// stood over a region nobody looked at is the third kind of wrong capture.
    /// </para>
    /// </param>
    /// <param name="glass">
    /// What the window's own backdrop was doing, where a caller asked.
    /// <para>
    /// WW41. A window with a system backdrop transmits what is behind it through the glass, and
    /// z-order reasoning cannot answer for that: the intruder is not in front of the window, it is
    /// showing through it. Left null by a caller that did not ask, for the same reason
    /// <paramref name="over" /> is.
    /// </para>
    /// </param>
    /// <param name="colours">
    /// What counting the picture's distinct colours said, where a caller counted.
    /// <para>
    /// WW42. A copy of exactly one colour is not a picture of a window, and it is the reading the
    /// alpha scan cannot take: a screen copy has no alpha channel. Left null by a caller that did
    /// not count, for the same reason the other two readings are.
    /// </para>
    /// </param>
    /// <exception cref="WrongCaptureException">
    /// Where the window belongs to a process this run is not driving, where nothing was drawing
    /// it, where another window stood over the region, where one arrived inside the take, where its
    /// own glass is carrying what is behind it, or where the picture is one flat colour. Every one
    /// is a wrong capture that a file on disk looks exactly the same as.
    /// </exception>
    /// <param name="layers">
    /// How the window's own pixels reach the screen, where a caller asked.
    /// <para>
    /// WW334. A layered window is composited with what is behind it, and no route exempts that: the
    /// backdrop refusal above lets a menu through because a menu on this shell has acrylic by
    /// design, and the shadow Windows draws behind that same menu is a popup by every test the route
    /// has and is nothing but the desktop. Left null by a caller that did not ask, for the reason
    /// the three above are.
    /// </para>
    /// </param>
    public static CaptureReceipt Of(
        string path,
        TopLevelWindow window,
        AppTarget target,
        PaintedFrame? frame = null,
        CaptureRoute? route = null,
        RegionThroughout? over = null,
        Glass? glass = null,
        ColourCheck? colours = null,
        SeeThrough? layers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(target);

        // The check the picture cannot make for itself. A capture of somebody else's window is a
        // perfectly good file, and the only thing that ever caught one was a person looking.
        if (window.Pid != target.Pid)
            throw new WrongCaptureException(
                WrongCapture.AnotherProcess,
                $"the capture is of {window} in pid {window.Pid}, and this run is driving pid {target.Pid}.");

        if (window.Cloak != Cloak.NotCloaked)
            throw new WrongCaptureException(
                WrongCapture.NothingDrawing,
                $"the capture is of {window}, which nothing is drawing: {Cloaking.Because(window.Cloak)}.");

        // WW40, and named rather than merely refused: "something else was in the way" is not
        // actionable and a title with a pid is. The reading already carries both, so the refusal
        // hands over its sentence rather than composing a second, thinner one.
        if (over is { WasCovered: true })
            throw new WrongCaptureException(
                WrongCapture.RegionCovered, $"the capture is of {window}, and {over.Sentence()}");

        // WW195, and its own arm rather than the one above. Clear when the capture started and
        // covered when it finished is a window that arrived inside the take, which the reading
        // WW40 refuses on was taken too early to see. Second, so a region already covered is
        // reported as that rather than as the desk having moved.
        if (over is { Changed: true })
            throw new WrongCaptureException(
                WrongCapture.DeskChanged, $"the capture is of {window}, and {over.Sentence()}");

        // WW41, and refused rather than warned about: a warning is not a refusal and the file gets
        // written either way. Not for a popup, which is the one thing the screen copy exists for —
        // a menu on this shell has acrylic by design, so refusing on it would refuse every capture
        // the copy route was built to take.
        //
        // WW194. On the route taken and never on the reach. This read Reach == Renderable, which an
        // off-screen render has by definition — and a render is the one capture a backdrop cannot
        // touch, because it draws the application's own visual tree with no window shown and the
        // compositor not involved. So the default route was refused for a hazard it does not have.
        // A forced copy of a renderable window is what the condition was reaching for, and that is
        // a copy: Renders is the field that separates the two.
        if (glass is { Transmits: true } && route?.Renders is not true && route?.Reach is null or OutOfReach.Renderable)
            throw new WrongCaptureException(
                WrongCapture.GlassTransmits, $"the capture is of {window}, and {glass.Sentence()}");

        // WW334, and the arm above it is the reason this one has no exemption. A menu is let through
        // the backdrop refusal because a menu on this shell has acrylic by design; the shadow drawn
        // behind that menu is a popup by every test the route has, is layered per pixel, and is
        // nothing but a rectangle of whatever the menu is standing in front of. Exempting a popup
        // here would exempt the one surface beside a menu that must never be photographed.
        if (layers is { Transmits: true } && route?.Renders is not true)
            throw new WrongCaptureException(
                WrongCapture.LayerTransmits, $"the capture is of {window}, and {layers.Sentence()}");

        // WW42. A flat rectangle is not a picture of a window, and the session that produced one
        // had everything present and nothing rendering — so the file was written and the run exited
        // zero. Counted rather than scanned for ink: a screen copy has no alpha channel, and the
        // reading that answers "did anything draw" cannot answer for it at all.
        if (colours is { Counted: true, IsFlat: true })
            throw new WrongCaptureException(
                WrongCapture.OneFlatColour, $"the capture is of {window}, and {colours.Sentence()}");

        return new CaptureReceipt(path, window, target, frame, route, over, glass, colours) { Layers = layers };
    }

    /// <summary>
    /// The success line: what was photographed, out of which process, started how. Printed on a
    /// pass and not only on a failure, because a run nobody reads until it goes red is a run
    /// whose wrong captures are all still there.
    /// </summary>
    public string Sentence()
    {
        var said = $"captured {Window} from pid {Target.Pid} to {Path}; {Target.Sentence()}";
        if (Route is not null)
            said = $"{said} {Route.Sentence()}";

        return Frame is null ? said : $"{said} {Frame.Sentence()}";
    }

    /// <summary>The step a trace keeps, addressed by the window rather than by the file.</summary>
    public TraceStep AsTraceStep() => new()
    {
        Verb = Route is null ? "capture" : $"capture ({Route})",
        Locator = Window.Handle == 0 ? "(no window)" : $"0x{Window.Handle:X}",
        Resolved = Window.ToString(),
        ReadBack = Path,
        Verdict = StepVerdict.Ok,
        Detail = Arguments.Satisfied ? null : Arguments.Absence,
    };
}

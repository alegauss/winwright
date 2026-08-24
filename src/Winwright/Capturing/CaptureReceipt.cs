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
        Obstruction? over)
    {
        Path = path;
        Window = window;
        Target = target;
        Frame = frame;
        Route = route;
        Over = over;
    }

    /// <summary>
    /// What stood over the region, where a caller read it. Null where none did — and null is not
    /// "nothing stood over it": a receipt that could say either from one value would be claiming an
    /// emptiness nobody looked for, which is the shape this project keeps withdrawing.
    /// </summary>
    public Obstruction? Over { get; }

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
    /// <exception cref="WrongCaptureException">
    /// Where the window belongs to a process this run is not driving, where nothing was drawing
    /// it, or where another window stood over the region. All three are wrong captures that a file
    /// on disk looks exactly the same as.
    /// </exception>
    public static CaptureReceipt Of(
        string path,
        TopLevelWindow window,
        AppTarget target,
        PaintedFrame? frame = null,
        CaptureRoute? route = null,
        Obstruction? over = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(target);

        // The check the picture cannot make for itself. A capture of somebody else's window is a
        // perfectly good file, and the only thing that ever caught one was a person looking.
        if (window.Pid != target.Pid)
            throw new WrongCaptureException(
                $"the capture is of {window} in pid {window.Pid}, and this run is driving pid {target.Pid}.");

        if (window.Cloak != Cloak.NotCloaked)
            throw new WrongCaptureException(
                $"the capture is of {window}, which nothing is drawing: {Cloaking.Because(window.Cloak)}.");

        // WW40, and named rather than merely refused: "something else was in the way" is not
        // actionable and a title with a pid is. The reading already carries both, so the refusal
        // hands over its sentence rather than composing a second, thinner one.
        if (over is { Was: true, Clear: false })
            throw new WrongCaptureException($"the capture is of {window}, and {over.Sentence()}");

        return new CaptureReceipt(path, window, target, frame, route, over);
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

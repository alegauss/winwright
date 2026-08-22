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
    private CaptureReceipt(string path, TopLevelWindow window, AppTarget target, PaintedFrame? frame)
    {
        Path = path;
        Window = window;
        Target = target;
        Frame = frame;
    }

    /// <summary>The file that was written.</summary>
    public string Path { get; }

    /// <summary>The window it is a picture of, as the enumeration found it.</summary>
    public TopLevelWindow Window { get; }

    /// <summary>How this run reached the application, which is where the process and pid come from.</summary>
    public AppTarget Target { get; }

    /// <summary>What was copied against what the window owns, where that was read.</summary>
    public PaintedFrame? Frame { get; }

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
    /// <exception cref="WrongCaptureException">
    /// Where the window belongs to a process this run is not driving, or where nothing was
    /// drawing it. Both are wrong captures that a file on disk looks exactly the same as.
    /// </exception>
    public static CaptureReceipt Of(string path, TopLevelWindow window, AppTarget target, PaintedFrame? frame = null)
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

        return new CaptureReceipt(path, window, target, frame);
    }

    /// <summary>
    /// The success line: what was photographed, out of which process, started how. Printed on a
    /// pass and not only on a failure, because a run nobody reads until it goes red is a run
    /// whose wrong captures are all still there.
    /// </summary>
    public string Sentence()
    {
        var said = $"captured {Window} from pid {Target.Pid} to {Path}; {Target.Sentence()}";
        return Frame is null ? said : $"{said} {Frame.Sentence()}";
    }

    /// <summary>The step a trace keeps, addressed by the window rather than by the file.</summary>
    public TraceStep AsTraceStep() => new()
    {
        Verb = "capture",
        Locator = Window.Handle == 0 ? "(no window)" : $"0x{Window.Handle:X}",
        Resolved = Window.ToString(),
        ReadBack = Path,
        Verdict = StepVerdict.Ok,
        Detail = Arguments.Satisfied ? null : Arguments.Absence,
    };
}

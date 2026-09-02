using System.Runtime.InteropServices;

using Winwright.Tracing;
using Winwright.Verdicts;

namespace Winwright.Capturing;

/// <summary>
/// What came of asking an application to render its own tree. WW349.
/// </summary>
/// <param name="Answered">Whether the application drew the picture and said so.</param>
/// <param name="Absence">Why it did not, where it did not. Empty where it did.</param>
public sealed record RenderAsked(bool Answered, string Absence)
{
    /// <summary>What this reading is called wherever it is reported.</summary>
    public const string PreconditionName = "an application that renders its own tree when asked";

    /// <summary>What was asked and what came back, said either way.</summary>
    public string Sentence() => Answered
        ? "the application rendered its own tree into the file it was given."
        : $"the application did not render its own tree: {Absence}.";

    /// <inheritdoc cref="Sentence" />
    public override string ToString() => Sentence();

    /// <summary>The result a verdict counts. An application that does not answer is a hole.</summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult AsAssertion(string named) => Answered
        ? AssertionResult.Pass(named, Sentence())
        : AssertionResult.Unchecked(named, Precondition.Absent(PreconditionName, Sentence()));

    /// <summary>The step a trace records.</summary>
    public TraceStep AsTraceStep(string named) => new()
    {
        Verb = "ask the application to render its own tree",
        Locator = named,
        Verdict = Answered ? StepVerdict.Ok : StepVerdict.Unchecked,
        Detail = Answered ? null : Sentence(),
    };
}

/// <summary>
/// Asking an application to draw the picture this engine cannot draw. WW349.
/// <para>
/// The off-screen render is this block's default and is the safer picture: it draws a visual tree
/// with no window shown, so there is no foreground, no z order and no second instance to be confused
/// with. It is also the one route the engine cannot take. A render needs the application's own tree,
/// and nothing outside that process has one — so a capture step against an ordinary window, which is
/// most windows, answered a hole naming the in-app half rather than taking a picture.
/// </para>
/// <para>
/// So the run asks and the application answers. <c>WM_COPYDATA</c> carries the path across the
/// process boundary with Windows doing the marshalling, and the reply is the application's own word
/// that the file is there — which is what makes this a reading rather than a wait on a file that may
/// arrive.
/// </para>
/// <para>
/// The two candidates the design named were both worse on the property this project keeps. A
/// directory the in-app half watches puts a polling thread inside a shipped product, which is what
/// makes <see cref="Winwright.Capturing" />'s two existing channels careful to do nothing at all
/// unless a harness named a file; and a verb the run starts the application with photographs a fresh
/// process rather than the window a case has driven to the state it means to photograph. A message
/// needs no thread — every application under test already runs a message loop — and it arrives at
/// the window that is showing what the run drove it to.
/// </para>
/// <para>
/// Sent rather than posted, and with a budget. The answer is the whole point, so the call has to wait
/// for it; and an application wedged inside its own message loop must come back as a reading this run
/// can report rather than as a run that stopped.
/// </para>
/// </summary>
public static class OwnRender
{
    /// <summary>
    /// The name both halves register, which is how a message meant for this is told from an
    /// application's own <c>WM_COPYDATA</c>.
    /// <para>
    /// Registered rather than a constant somebody picked: <c>RegisterWindowMessage</c> answers the
    /// same number in every process for the same string and a number nobody else can collide with,
    /// which is exactly the promise a magic constant cannot make.
    /// </para>
    /// </summary>
    public const string Registered = "Winwright.OwnRender";

    /// <summary>
    /// The variable an application reads before it answers at all, named here as well because a
    /// refusal that could not say what to set would be the shape WW347 was about.
    /// <para>
    /// Spelled rather than referenced: the engine carries no reference to the in-app half and never
    /// will, so the two halves agree on this the way they agree on every other name between them —
    /// by a case that reads both, which is where a drift of one letter goes red.
    /// </para>
    /// </summary>
    public const string RendersInto = "WINWRIGHT_RENDERS";

    /// <summary>How long an application is given to draw it, unless the caller says otherwise.</summary>
    public const int DefaultBudgetMs = 8000;

    /// <summary>
    /// Ask the application owning <paramref name="window" /> to render that window's tree into
    /// <paramref name="path" />.
    /// </summary>
    /// <param name="window">The window whose tree is wanted. It is what the application looks up.</param>
    /// <param name="path">Where the picture goes. The application writes it, so it is its own to refuse.</param>
    /// <param name="withinMs">How long to wait for the answer.</param>
    /// <returns>Whether it drew one, and why not where it did not.</returns>
    public static RenderAsked Into(nint window, string path, int withinMs = DefaultBudgetMs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(withinMs);

        if (window == 0)
            return new RenderAsked(false, "no window was named");

        var full = System.IO.Path.GetFullPath(path.Trim());
        var wanted = full + "\0";
        var bytes = System.Text.Encoding.Unicode.GetBytes(wanted);

        var buffer = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, buffer, bytes.Length);
            var carried = new Winwright.Windowing.Win32.CopyData
            {
                Data = (nint)Winwright.Windowing.Win32.RegisterWindowMessageW(Registered),
                Size = bytes.Length,
                Buffer = buffer,
            };

            // The window's own thread runs the render, so the wait is on that thread finishing it.
            // Zero back is the send itself failing — a window that is gone, or one whose thread is
            // not answering inside the budget — and the two are told apart by the error, because a
            // run told only "it did not answer" cannot tell a wedged application from a closed one.
            var sent = Winwright.Windowing.Win32.SendMessageTimeoutW(
                window,
                Winwright.Windowing.Win32.WmCopyData,
                0,
                ref carried,
                Winwright.Windowing.Win32.AbortIfHung,
                (uint)withinMs,
                out var answer);
            if (sent == 0)
            {
                var why = Marshal.GetLastWin32Error();
                return new RenderAsked(
                    false,
                    why == 0
                        ? $"its window did not answer inside {withinMs}ms, which is a thread busy in its own loop"
                        : $"its window could not be reached (0x{why:X})");
            }

            if (answer == 0)
            {
                return new RenderAsked(
                    false,
                    "it answered and drew nothing, so it does not take this message — an application "
                        + "takes it by calling Winwright.InApp's Renders.Answer, and that does nothing "
                        + $"unless {RendersInto} names a directory it may write into");
            }

            // Its word that the file is there, checked rather than believed. The application is
            // being asked to do the one thing this engine cannot verify by doing it itself, so the
            // one fact that can be verified from here is.
            return File.Exists(full)
                ? new RenderAsked(true, "")
                : new RenderAsked(false, $"it answered that it had drawn one and {full} is not there");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

}

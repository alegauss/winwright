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
    /// The same, for the ask that names one popup instead of the window's own tree. WW359.
    /// <para>
    /// A second message and not a second field in the first one's payload, which was the obvious
    /// design and is the unsafe one. That payload is a NUL-terminated path read back as
    /// <c>PtrToStringUni</c> over the whole buffer, so an in-app half older than the harness driving
    /// it would take <c>path\0name</c> for a path and hand it to <c>Path.GetFullPath</c>, which
    /// raises on an embedded NUL — out of a window procedure, in a shipped application this run is
    /// only supposed to photograph. Version skew across the two halves is ordinary here: the in-app
    /// half is what an adopter ships, and it reaches them by a release rather than by this commit.
    /// </para>
    /// <para>
    /// A registered name nobody else answers skews the other way. An old half compares the id, does
    /// not match, leaves the message unhandled and answers zero — which this already reports as an
    /// application that does not take the message, naming the verb that would make it.
    /// </para>
    /// </summary>
    public const string RegisteredPopup = "Winwright.OwnRender.Popup";

    /// <summary>
    /// The name for the ask that answers why a render did not happen. WW362.
    /// <para>
    /// A third message for the reason there was a second: an application older than this reads none
    /// of it, answers nothing, and gets the sentence that was always right about it. What it buys is
    /// that the two faults collapsed into "it does not take this message" — no half at all, and a
    /// half told nowhere to write — stop reading alike, which matters most at the attach door, where
    /// only one of them is ever the truth and the run had been printing the other.
    /// </para>
    /// </summary>
    public const string RegisteredWhy = "Winwright.OwnRender.Why";

    /// <summary>
    /// What the why ask answers with, spelled here as numbers for the reason the popup answers are:
    /// the engine holds no reference to the in-app half, and a case reads both lists. WW362.
    /// <para>
    /// Grouped rather than laid beside the popup answers, because the two messages number their own
    /// answers and one of the words is in both lists at different values — a flat <c>PathRefused</c>
    /// could only ever be one of them, and the one it was not would be silently wrong.
    /// </para>
    /// </summary>
    public static class Refusals
    {
        /// <summary>Nothing is wrong, which after a failed render is a race and not an answer.</summary>
        public const int WouldDraw = 1;

        /// <summary>The half is there and the process was started with nowhere to write.</summary>
        public const int ToldNowhere = 2;

        /// <summary>It has somewhere, and the file asked for is not inside it.</summary>
        public const int PathRefused = 3;

        /// <summary>The window is not one that application's presentation stack owns.</summary>
        public const int NotOurWindow = 4;

        /// <summary>It is, and it has laid out to nothing.</summary>
        public const int NothingToDraw = 5;
    }

    /// <summary>
    /// What the popup ask answers with. WW359.
    /// <para>
    /// Spelled here as numbers rather than shared as a type, for the reason
    /// <see cref="RendersInto" /> is spelled twice: the engine holds no reference to the in-app half.
    /// The in-app half names the same numbers in an enum of its own and a case reads both, which is
    /// where a drift of one goes red.
    /// </para>
    /// <para>
    /// Distinct answers and not the one bit the window ask carries, because this ask has refusals
    /// that ask has not, and they want different things done about them. A name matching nothing is
    /// a case naming a popup that is not there; a name matching two is a case that would otherwise
    /// get a picture of the wrong surface, which is the failure this block refuses everywhere else.
    /// Collapsed into one bit they would read alike, and only one of them is the harness's to fix.
    /// </para>
    /// </summary>
    public const int Drawn = 1;

    /// <inheritdoc cref="Drawn" />
    public const int NoSuchPopup = 2;

    /// <inheritdoc cref="Drawn" />
    public const int MoreThanOnePopup = 3;

    /// <inheritdoc cref="Drawn" />
    public const int PopupHoldsNothing = 4;

    /// <inheritdoc cref="Drawn" />
    public const int PathRefused = 5;

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
        var refused = Asked(window, Registered, full + "\0", withinMs, out var answer);
        if (refused is not null)
            return refused;

        if (answer == 0)
            return Why(window, full, withinMs);

        return Landed(full);
    }

    /// <summary>
    /// Ask the application why it drew nothing, and say what it answers. WW362.
    /// <para>
    /// A render that came back zero used to have one sentence, and it named two different faults: an
    /// application carrying no in-app half, and one carrying it that was started without
    /// <see cref="RendersInto" />. The remedies are opposite — a line somebody adds to the product,
    /// against the environment it was launched in — and at the attach door only the second can ever
    /// apply, because that door launched nothing and has no moment left at which it could have set
    /// anything. So the run stopped guessing and asked.
    /// </para>
    /// <para>
    /// Only here, on the path that already failed. An ask that ran every time would put a second
    /// round trip on every capture in every run to answer a question almost none of them have.
    /// </para>
    /// </summary>
    /// <param name="window">The window that was asked about.</param>
    /// <param name="full">The file that was asked for.</param>
    /// <param name="withinMs">How long to wait for the answer.</param>
    private static RenderAsked Why(nint window, string full, int withinMs)
    {
        const string Unheard =
            "it answered and drew nothing, so it does not take this message — an application takes "
                + "it by calling Winwright.InApp's Renders.Answer, and that does nothing unless "
                + RendersInto + " names a directory it may write into";

        // A send that fails here is not worth a sentence of its own: the render's own failure is
        // what the caller asked about, and this is only the part that would have named it better.
        if (Asked(window, RegisteredWhy, full + "\0", withinMs, out var answer) is not null)
            return new RenderAsked(false, Unheard);

        return (int)answer switch
        {
            Refusals.ToldNowhere => new RenderAsked(
                false,
                "it has the in-app half and was started without "
                    + $"{RendersInto}, so it may write nowhere — a run that launches the application "
                    + "sets that from the project's 'captures', and a run attached to one already up "
                    + "cannot: that process has to have been started with it"),
            Refusals.PathRefused => new RenderAsked(
                false,
                $"it refused to write {full}, which is not inside the directory {RendersInto} named "
                    + "in that process — the two runs disagree about where pictures go"),
            Refusals.NotOurWindow => new RenderAsked(
                false,
                "that window is not one its presentation stack owns, so the application has no tree "
                    + "behind it to draw"),
            Refusals.NothingToDraw => new RenderAsked(
                false,
                "the window has laid out to nothing, so there is no picture to take rather than an "
                    + "empty one to write"),

            // WouldDraw, and anything this engine has no name for. Both are an application saying
            // something a run cannot act on, so what it gets is the sentence about the ask it made.
            _ => new RenderAsked(false, Unheard),
        };
    }

    /// <summary>
    /// Ask the application owning <paramref name="window" /> to render the tree held by the popup
    /// called <paramref name="named" /> into <paramref name="path" />. WW359.
    /// <para>
    /// The surface WW347 is about, and the one an outside process cannot reach at all. An open popup
    /// is its own layered top-level window whose soft edge is a strip of whatever it stands in front
    /// of, so a copy of the screen is refused; a closed one has no window to copy. The child is an
    /// ordinary element in a tree the application owns either way, which is why a preview of a
    /// flyout nobody has clicked is a picture this can ask for and no copy ever could.
    /// </para>
    /// <para>
    /// By name, and a name matching more than one popup is refused rather than resolved. That was
    /// the open question the design carried, and walk order was the alternative: it is unique and it
    /// is not stable, so a popup added above another silently repoints every case counting past it.
    /// A name is the author's own word for the surface and the ambiguity is theirs to remove, which
    /// makes the refusal one a person can act on — where a wrong picture is not.
    /// </para>
    /// </summary>
    /// <param name="window">The window whose tree holds the popup. It is what the application walks.</param>
    /// <param name="named">The popup's name, as the application's own author spelled it.</param>
    /// <param name="path">Where the picture goes. The application writes it, so it is its own to refuse.</param>
    /// <param name="withinMs">How long to wait for the answer.</param>
    /// <returns>Whether it drew one, and which refusal it made where it did not.</returns>
    public static RenderAsked PopupInto(nint window, string named, string path, int withinMs = DefaultBudgetMs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(named);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(withinMs);

        // A name carrying the separator could not come back as one field, and a caller who wrote one
        // meant something this cannot ask for. Refused here rather than sent, because the half at
        // the other end would read it as a shorter name and photograph whatever that matched.
        if (named.Contains('\0', StringComparison.Ordinal))
            return new RenderAsked(false, "a popup's name may not carry a NUL, which is what separates the ask");

        if (window == 0)
            return new RenderAsked(false, "no window was named");

        var full = System.IO.Path.GetFullPath(path.Trim());
        var refused = Asked(window, RegisteredPopup, full + "\0" + named + "\0", withinMs, out var answer);
        if (refused is not null)
            return refused;

        return (int)answer switch
        {
            Drawn => Landed(full),
            NoSuchPopup => new RenderAsked(false, $"it holds no popup called {named} under that window"),
            MoreThanOnePopup => new RenderAsked(
                false,
                $"more than one popup under that window is called {named}, so which one was meant is "
                    + "the application's to say — a picture of either would be a picture this run "
                    + "could not prove was the right surface"),
            PopupHoldsNothing => new RenderAsked(
                false,
                $"the popup called {named} is holding nothing that can be drawn, so there is no tree "
                    + "to photograph"),
            PathRefused => new RenderAsked(
                false,
                $"it refused to write {full}, which is not inside the directory {RendersInto} named"),
            _ => new RenderAsked(
                false,
                "it answered and drew nothing, so it does not take this message — an application "
                    + "takes it by calling Winwright.InApp's Renders.Answer, and a half older than "
                    + "this one answers no popup ask at all"),
        };
    }

    /// <summary>
    /// Carry one ask across and hand back the answer, or the reading that says it never arrived.
    /// <para>
    /// Shared by both asks because the marshalling and the two send-level failures are the same for
    /// each, and a second copy is the copy that goes on freeing the buffer after the first one stops.
    /// What differs between them is the registered name and what the payload says, both passed in.
    /// </para>
    /// </summary>
    /// <param name="window">Where to send it.</param>
    /// <param name="registered">The name identifying which ask this is.</param>
    /// <param name="payload">The fields, each NUL-terminated.</param>
    /// <param name="withinMs">How long to wait.</param>
    /// <param name="answer">What the application returned, where it answered at all.</param>
    /// <returns>Null where the send landed, and the reading to report where it did not.</returns>
    private static RenderAsked? Asked(
        nint window, string registered, string payload, int withinMs, out nint answer)
    {
        answer = 0;
        var bytes = System.Text.Encoding.Unicode.GetBytes(payload);

        var buffer = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, buffer, bytes.Length);
            var carried = new Winwright.Windowing.Win32.CopyData
            {
                Data = (nint)Winwright.Windowing.Win32.RegisterWindowMessageW(registered),
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
                out answer);
            if (sent != 0)
                return null;

            var why = Marshal.GetLastWin32Error();
            return new RenderAsked(
                false,
                why == 0
                    ? $"its window did not answer inside {withinMs}ms, which is a thread busy in its own loop"
                    : $"its window could not be reached (0x{why:X})");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>
    /// Its word that the file is there, checked rather than believed. The application is being asked
    /// to do the one thing this engine cannot verify by doing it itself, so the one fact that can be
    /// verified from here is.
    /// </summary>
    /// <param name="full">The file it said it wrote.</param>
    private static RenderAsked Landed(string full) => File.Exists(full)
        ? new RenderAsked(true, "")
        : new RenderAsked(false, $"it answered that it had drawn one and {full} is not there");
}

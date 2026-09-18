using System.Collections.ObjectModel;

namespace Winwright.Tests;

/// <summary>Why a verb that touches the desk is not one a case has to excuse.</summary>
internal enum Touching
{
    /// <summary>It answers a fact about the machine rather than about the desk as it stands: how
    /// many monitors there are, whether a desk exists at all, what this process is running on.</summary>
    AboutTheMachine,

    /// <summary>It is the primitive's own wrapper, and the verb a case calls is the one above it.</summary>
    ThePlumbing,

    /// <summary>It reads a window a caller already has, rather than looking for one on the desk.</summary>
    AWindowInHand,

    /// <summary>It sweeps the desk and then keeps only what the caller named, so what else happens
    /// to be open cannot change the answer.</summary>
    FilteredToWhatTheCallerNamed,

    /// <summary>
    /// It puts back what an act of this engine took and claims nothing. WW330: a case calls one
    /// after it has read everything it came for, and a desk that refuses the tidying leaves the run
    /// exactly where it would have been without the call — so there is no verdict for the desk to
    /// have decided.
    /// </summary>
    PuttingItBack,

    /// <summary>
    /// It answers a verdict that reports the desk itself. WW462: the runner's own answer is where a
    /// hole is written down — an assertion nothing could evaluate comes back <em>unchecked</em> with
    /// the absence on it — so asking a case to excuse the desk around one of these is asking it to
    /// excuse the thing that does the excusing. What a case still owes is to read the verdict it was
    /// given rather than only its outcome, which is a different rule and not this list's.
    /// </summary>
    AnsweredInTheVerdict,
}

/// <summary>One engine verb that reaches the desk and is not in <see cref="DeskAsks.Calls" />.</summary>
/// <param name="Named">The verb, as <c>Type.Method</c>.</param>
/// <param name="Kind">Why a case calling it owes no excuse.</param>
/// <param name="Because">The sentence a reader needs.</param>
internal sealed record DeskVerb(string Named, Touching Kind, string Because)
{
    public override string ToString() => $"{Kind,-16} {Named}: {Because}";
}

/// <summary>
/// WW208. <c>DeskAsks.Calls</c> is the judgement WW190 said it was, and the judgement was typed. The
/// filing is checked — every entry names a condition <c>DeskFacts</c> declares — and nothing checked
/// the other end, so a reading the list had never heard of was a reading no case was ever asked to
/// excuse.
/// <para>
/// Found by a guest run rather than by anything here. <c>Traversal.WhoHasFocus</c> reads what holds
/// the focus anywhere on the desk, was absent from the list, and a case asserting on it went red
/// twice on a machine slowed by its own antivirus — saying the focus was not on the control this
/// suite had just put it on, which is the misattribution WW190 exists to stop.
/// </para>
/// <para>
/// So the surface is read rather than remembered, and read at the place the dependence actually
/// lives: a handful of primitives that ask the desk what is on it right now. A verb that reaches one
/// of those, directly or through something beside it in the same file, is desk-dependent whatever
/// its return type says — which is why a rule keyed on return types would have missed the very call
/// this task is about, since <c>WhoHasFocus</c> answers plain element facts.
/// </para>
/// </summary>
internal static class DeskVerbs
{
    /// <summary>
    /// The calls that ask the desk what is on it at this moment.
    /// <para>
    /// Narrow on purpose, and each narrowing is a judgement. <c>GetWindowRect</c> and
    /// <c>IsWindowVisible</c> read a window a caller already holds; <c>GetSystemMetrics</c> answers
    /// a fact about the machine. What is here either looks for something on the desk without being
    /// told where — the foreground, the focus, a window by class, the whole z order — or puts
    /// something onto it.
    /// </para>
    /// </summary>
    internal static IReadOnlyList<string> Primitives { get; } = new ReadOnlyCollection<string>(
    [
        "GetForegroundWindow",
        "FocusedElement",
        "GetLastInputInfo",
        "GetCursorPos",
        "EnumWindows",
        "FindWindowW",
        "SendInputRaw",
        "OpenInputDesktop",
    ]);

    /// <summary>The verbs that reach one and are not calls a case has to excuse, with why.</summary>
    internal static IReadOnlyList<DeskVerb> Excused { get; } = new ReadOnlyCollection<DeskVerb>(
    [
        new("Desk.Read", Touching.AboutTheMachine,
            "it asks whether there is an interactive desk at all — a window station, an input "
                + "desktop, a compositor — which is a fact about the session this run is in and not "
                + "about what happens to be on it. A case cannot excuse the absence of a desk on the "
                + "grounds of the desk, and DeskGateTests asserts it either way"),

        // WW210 found both of these, and found them by repairing the walk rather than by asking a
        // better question. OfProcess is two overloads: the one that enumerates, and the convenience
        // one beside it. The reading kept whichever came last and threw the other away — so the
        // overload that calls EnumWindows was invisible, and Largest, which calls it, with it.
        new("TopLevelWindows.OfProcess", Touching.FilteredToWhatTheCallerNamed,
            "it walks every top-level window there is and keeps the ones belonging to a pid the "
                + "caller named. A desk crowded with somebody else's windows returns the same list, "
                + "and a window this run's application has not drawn yet is what a deadline is for "
                + "rather than what an excuse is for"),
        new("TopLevelWindows.Largest", Touching.FilteredToWhatTheCallerNamed,
            "the same walk, answering the largest of them. It reaches the desk only through "
                + "OfProcess and inherits the whole of its argument"),

        // WW462 gave this its right name. It is declared on `TrayMenu` and called as
        // `menu.PutBack()`, and the sweep keyed it to the file it sits in — `NotificationArea` —
        // which is a type that has no such member. The reading answers the declaring type now, so
        // the two agree and a reader looking this up finds it.
        new("TrayMenu.PutBack", Touching.PuttingItBack,
            "WW330. It shuts the flyout the act it belongs to opened and gives the desktop back to "
                + "whatever held it, which is housekeeping and not a reading: the verb that took "
                + "both is the one a case excuses, and this one is called after the case has "
                + "asserted everything it came for. A shell that refuses either leaves the taskbar "
                + "the way it already was, which is the state this exists to improve on rather than "
                + "a verdict it could get wrong"),

        // --- WW462, the composites the one-file sweep could not see -------------------------------
        //
        // Each of these reaches the desk through the verbs above it, and each answers a verdict
        // rather than a reading: the run says which assertions could not be evaluated and why, in
        // the shape a report and a trace both carry. That is the third verdict doing its job, and
        // a case excusing the desk around it would be excusing the mechanism that reports it.
        new("Suite.Run", Touching.AnsweredInTheVerdict,
            "it runs the cases a scenario declares and answers a suite verdict, where every hole is "
                + "already named against the assertion that could not run — which is the whole of "
                + "what a run of this engine is for, and is reported whether or not a case reads it"),
        new("Suite.Launch", Touching.AnsweredInTheVerdict,
            "the same run with the launch in front of it, and the launch's own refusals reach the "
                + "verdict the same way"),
        new("CaseRun.Of", Touching.AnsweredInTheVerdict,
            "one case rather than a suite of them, answering the verdict the suite collects"),
        new("Preamble.Of", Touching.AnsweredInTheVerdict,
            "WW170's composition: the five readings a run takes before it claims anything, kept in "
                + "one place so a sixth is this file rather than an audit of every runner. It is the "
                + "reading a report prints, and a run whose desk was refused says so through it"),
        new("Preamble.Around", Touching.AnsweredInTheVerdict,
            "the same composition taken either side of a run, which is how a report says what "
                + "changed on the machine while the cases were going"),
        new("Preamble.Closing", Touching.AnsweredInTheVerdict,
            "the second half of that composition, called on its own where a runner opened the "
                + "reading itself. WW471 gave it a desk to reach: the foreground is read again and "
                + "joined as a finding, so what the report prints about the desk is a pair rather "
                + "than one look — and a case excusing the desk around it would be excusing the "
                + "thing that reports it"),
        new("CaptureReceipt.Taking", Touching.AnsweredInTheVerdict,
            "it takes the three readings a screen copy owes around the take and refuses where any of "
                + "them answers wrongly — so the desk fact does not reach a caller as a picture that "
                + "passed, it reaches it as a WrongCaptureException naming which question failed"),

        new("InstanceCheck.Of", Touching.FilteredToWhatTheCallerNamed,
            "it walks the processes running one executable the caller named and reads what each is "
                + "showing. A desk crowded with somebody else's windows answers the same list, and "
                + "the only thing that changes the answer is another copy of the application under "
                + "test — which is a finding about the machine the run was asked to drive, not about "
                + "what happens to be open on it"),
    ]);

    /// <summary>Every public verb of the engine that reaches a desk primitive.</summary>
    internal static IReadOnlyList<string> Reaching() => reaching.Value;

    /// <summary>The reading a person gets: the count first, then a line each.</summary>
    internal static IReadOnlyList<string> Render() => new ReadOnlyCollection<string>(
    [
        $"{Reaching().Count} engine verb(s) reach the desk, of which {DeskAsks.Calls.Count} are "
            + $"calls a case has to excuse and {Excused.Count} are not",
        .. Excused.Select(one => $"  {one}"),
    ]);

    private static readonly Lazy<IReadOnlyList<string>> reaching = new(Sweep);

    /// <summary>
    /// Every public verb that reaches one of the primitives, all the way down and across files.
    /// <para>
    /// WW462. This walked one file at a time, and the scoping was deliberate: a bare <c>Member(</c>
    /// matched across the whole engine would let any private helper called <c>Run</c> stand in for
    /// <c>Pointer.Run</c>. What it cost is a verb reaching the desk through a call into another
    /// file — <c>Menu.Enter</c>, <c>Menu.Expand</c> and <c>Menu.To</c> each ask
    /// <c>Foreground.Check</c>, which lives in <c>Foreground.cs</c>, and the rule never knew they
    /// exist. Found by accident: WW457 put a primitive directly in <c>Menu.cs</c> for a moment and
    /// all three appeared.
    /// </para>
    /// <para>
    /// The walk moved to <see cref="Checkout.Reaching" /> rather than growing a second copy here,
    /// which is WW210's argument one sweep over: <c>Synthesising</c> had already written the
    /// cross-file rule and shipped it, so two sweeps over the same sources answered differently
    /// about the same member. The qualification is what makes crossing safe — an edge on
    /// <c>Owner.Member(</c> anywhere, and on a bare <c>Member(</c> only inside the declaring file.
    /// </para>
    /// </summary>
    private static IReadOnlyList<string> Sweep() =>
        // The primitives themselves live in Win32.cs, and every one of them touches the desk by
        // definition. Excusing eight declarations one at a time would be writing down that a
        // P/Invoke is a P/Invoke; what a case calls is always the verb above them.
        Checkout.Reaching(Checkout.Engine, Primitives, except: "Win32.cs")
            .Where(one => one.IsPublic)
            .Select(one => one.Named)
            .ToList();
}

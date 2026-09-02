using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Tracing;
using Winwright.Verdicts;
using Winwright.Windowing;

namespace Winwright.Acting;

/// <summary>Which button a synthesized click presses.</summary>
public enum MouseButton
{
    /// <summary>The primary button.</summary>
    Left,

    /// <summary>The secondary one, which is what opens a context menu.</summary>
    Right,

    /// <summary>The wheel, pressed.</summary>
    Middle,
}

/// <summary>
/// Why a scenario reached for the pointer rather than for a pattern.
/// <para>
/// The count was the cheap half of the question. A reader deciding whether a scenario can run
/// unattended wanted to know that this one is a bare border with no automation peer, that one a
/// notification-area icon, and the third a segment of a custom template — three different futures,
/// the first of which may get a peer and the second of which never will, flattened by a list of
/// locators into one number.
/// </para>
/// </summary>
public enum PointerReason
{
    /// <summary>No automation peer at all, so nothing to ask. The one that may change.</summary>
    NoAutomationPeer,

    /// <summary>A notification-area icon: drawn by the shell, exposed by nobody. It never will.</summary>
    NotificationArea,

    /// <summary>A segment of a custom template, drawn without a peer of its own.</summary>
    CustomTemplate,

    /// <summary>The act is about the pointer itself, so no pattern would express it anyway.</summary>
    PointerIsTheAct,

    /// <summary>The control offers the pattern and it did not take; this is the escalation.</summary>
    PatternDidNotTake,
}

/// <summary>
/// One pointer act as a scenario declares it. It is a separate kind from the pattern acts on
/// purpose: what needs a real desktop is then countable by reading the file, rather than
/// discovered on the run where the desktop was busy — and each one carries the reason it is one,
/// stated where somebody chose it rather than inferred later from a locator.
/// </summary>
/// <param name="Verb">What the act is, as the scenario names it.</param>
/// <param name="Locator">What it addresses.</param>
/// <param name="Because">Why the pattern route was unavailable.</param>
/// <param name="Note">What is specific about this one, or empty to let the reason speak alone.</param>
/// <param name="Button">Which button it presses.</param>
/// <param name="Clicks">How many times, which is how a double click is said.</param>
public sealed record PointerAct(
    string Verb,
    Locator Locator,
    PointerReason Because,
    string Note = "",
    MouseButton Button = MouseButton.Left,
    int Clicks = 1)
{
    /// <summary>The reason as a person says it, with this act's own note where it has one.</summary>
    public string Reason => string.IsNullOrWhiteSpace(Note)
        ? Pointer.Worded(Because)
        : $"{Pointer.Worded(Because)} ({Note.Trim()})";

    /// <summary>The one line a report names it by.</summary>
    public override string ToString() =>
        $"{Verb} {Locator} ({Clicks} {Button.ToString().ToLowerInvariant()} click{(Clicks == 1 ? "" : "s")})";
}

/// <summary>What a pointer act did, or the precondition that stopped it before it did anything.</summary>
public sealed record PointerResult
{
    internal PointerResult(
        PointerAct act, ElementFacts? element, Precondition foreground, WindowBounds at, PatternValues after)
    {
        Act = act;
        Element = element;
        Foreground = foreground;
        At = at;
        After = after;
    }

    /// <summary>The act as it was declared.</summary>
    public PointerAct Act { get; }

    /// <summary>What it addressed, or null where nothing resolved.</summary>
    public ElementFacts? Element { get; }

    /// <summary>
    /// Whether the window under test owned the keyboard and the pointer. Absent means nothing was
    /// synthesized: input sent to somebody else's window is not a weaker version of this act, it
    /// is a different act against a window nobody asked about.
    /// </summary>
    public Precondition Foreground { get; }

    /// <summary>Where the click landed, in screen coordinates. Empty where nothing was sent.</summary>
    public WindowBounds At { get; }

    /// <summary>What the element read afterwards, where there was one.</summary>
    public PatternValues After { get; }

    /// <summary>Whether input was actually synthesized.</summary>
    public bool Landed => Foreground.Satisfied && Element is not null;

    /// <summary>
    /// The result a verdict counts. A desk that refused the foreground is a <em>hole</em> and never
    /// a failure.
    /// <para>
    /// WW133. Input synthesised into somebody else's window is not a weaker version of this act, it
    /// is a different act against a window nobody asked about — so nothing is sent, and what a case
    /// then reports has to be that the check did not run rather than that the application is wrong.
    /// This block's criterion says it outright: nothing about the desk is reported as a defect in
    /// the code.
    /// </para>
    /// </summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult AsAssertion(string named)
    {
        if (!Foreground.Satisfied)
            return AssertionResult.Unchecked(named, Foreground);

        return Landed
            ? AssertionResult.Pass(named, $"{Act} landed on {Element} at {At}")
            : AssertionResult.Fail(named, $"{Act} reached nothing: no element resolved");
    }

    /// <summary>The step a trace records, unchecked where the desktop was not ours.</summary>
    public TraceStep AsTraceStep() => new()
    {
        Verb = Act.Verb,
        Locator = Act.Locator.Text,
        Resolved = Element?.ToString(),
        Pattern = "synthesized input",
        ReadBack = Landed ? After.Reading() : null,
        Verdict = Landed ? StepVerdict.Ok : StepVerdict.Unchecked,
        Detail = Landed ? null : Foreground.Absence,
    };
}

/// <summary>A stated reason the tree disagrees with.</summary>
/// <param name="Act">The act that was checked.</param>
/// <param name="Element">What its locator resolved to.</param>
/// <param name="Offered">What that element does offer, in alphabetical order.</param>
public sealed record DisputedReason(
    PointerAct Act, ElementFacts Element, IReadOnlyList<string> Offered)
{
    /// <summary>The sentence the author has to act on, with the claim and the tree both named.</summary>
    public string Because =>
        $"{Act.Verb} {Act.Locator} says {Act.Reason}, and {Element} offers "
        + string.Join(", ", Offered);
}

/// <summary>
/// What checking the declared reasons found: the ones the tree disputes, the ones nothing here
/// could read, and the ones it agrees with.
/// </summary>
/// <param name="Disputed">Acts whose control offers a pattern after all.</param>
/// <param name="Unchecked">Acts whose reason claims nothing about this tree, or whose locator is not in it.</param>
/// <param name="Agreed">Acts whose control does offer nothing, as they said.</param>
public sealed record ReasonsChecked(
    IReadOnlyList<DisputedReason> Disputed,
    IReadOnlyList<PointerAct> Unchecked,
    IReadOnlyList<PointerAct> Agreed)
{
    /// <summary>Whether the tree disagrees with anything the file says.</summary>
    public bool Disputes => Disputed.Count > 0;

    /// <summary>What this run calls the reading, so a preamble names it the same way every time.</summary>
    public const string Named = "the pointer acts say why they are pointer acts";

    /// <summary>
    /// This reading as the preamble carries it.
    /// <para>
    /// WW136. The check shipped and nothing called it, which is this project's founding defect
    /// wearing a new subject: a reason recorded in the file and never read back is the sort of
    /// comment that is true the day it is written and quietly false a year later, and a report
    /// keeps printing it as though somebody had checked.
    /// </para>
    /// <para>
    /// A finding rather than a precondition, because a disputed reason is not a failure of the run
    /// — the act still works, that being the point of a pointer — and nothing may be excused by it.
    /// </para>
    /// </summary>
    public Finding AsFinding() => new(Named, !Disputes, Sentence());

    /// <summary>
    /// The reading in one sentence, which never says every reason was checked while any was not.
    /// </summary>
    public string Sentence()
    {
        var total = Disputed.Count + Unchecked.Count + Agreed.Count;
        if (total == 0)
            return "no pointer act declares a reason to check.";

        var clauses = new List<string>();
        if (Disputed.Count > 0)
            clauses.Add($"{Disputed.Count} disputed: {string.Join("; ", Disputed.Select(one => one.Because))}");
        if (Unchecked.Count > 0)
            clauses.Add(
                $"{Unchecked.Count} not checked, their reasons claiming nothing this tree can answer: "
                + string.Join(", ", Unchecked.Select(one => one.Locator.ToString())));

        return clauses.Count == 0
            ? $"all {total} stated reasons are what the tree says too."
            : $"{Agreed.Count} of {total} reasons agreed with the tree; " + string.Join("; ", clauses) + ".";
    }
}

/// <summary>
/// Synthesized pointer input, for the controls that have no pattern at all: a bare border with no
/// automation peer, a notification-area icon, a segment of a custom template.
/// <para>
/// Reaching for the mouse there is right. Doing it silently is not, because the act then carries a
/// precondition the file never mentions — so nothing in this project falls back to a pointer, and
/// a scenario that needs one says so. <see cref="Summarise"/> is what makes the set countable.
/// </para>
/// </summary>
public static class Pointer
{
    /// <summary>Press once with the primary button.</summary>
    /// <param name="subject">What to press.</param>
    /// <param name="because">Why the pattern route was unavailable. Stated, never inferred.</param>
    /// <param name="button">Which button.</param>
    /// <param name="clicks">How many presses.</param>
    /// <param name="note">What is specific about this one, where the reason alone is not enough.</param>
    public static PointerResult Click(
        Subject subject,
        PointerReason because,
        MouseButton button = MouseButton.Left,
        int clicks = 1,
        string note = "")
    {
        ArgumentNullException.ThrowIfNull(subject);
        return Run(new PointerAct("click", subject.Locator, because, note, button, clicks), subject);
    }

    /// <summary>Press twice, which some templates need and no pattern expresses.</summary>
    /// <param name="subject">What to press.</param>
    /// <param name="because">Why the pattern route was unavailable.</param>
    /// <param name="button">Which button.</param>
    /// <param name="note">What is specific about this one.</param>
    public static PointerResult DoubleClick(
        Subject subject, PointerReason because, MouseButton button = MouseButton.Left, string note = "") =>
        Click(subject, because, button, clicks: 2, note: note);

    /// <summary>One reason as a person says it, rather than as the enum spells it.</summary>
    /// <param name="because">The reason.</param>
    public static string Worded(PointerReason because) => because switch
    {
        PointerReason.NoAutomationPeer => "the control has no automation peer, so there is nothing to ask",
        PointerReason.NotificationArea => "it is a notification-area icon, drawn by the shell and exposed by nobody",
        PointerReason.CustomTemplate => "it is a segment of a custom template, drawn without a peer of its own",
        PointerReason.PointerIsTheAct => "the act is about the pointer itself, so no pattern would express it",
        _ => "the control offers the pattern and it did not take, so this is the escalation",
    };

    /// <summary>
    /// Whether a peer could one day make this act unnecessary. The three futures the count used to
    /// flatten: a bare border may get a peer, and a notification-area icon never will.
    /// </summary>
    /// <param name="because">The reason.</param>
    public static bool MayGetAPeer(PointerReason because) =>
        because is PointerReason.NoAutomationPeer or PointerReason.CustomTemplate;

    /// <summary>
    /// Run a declared act against its subject. The element must be actionable and the window must
    /// own the foreground; without the second, nothing is sent and the result says why.
    /// </summary>
    /// <exception cref="NotActionableException">Where the element cannot take a click at all.</exception>
    public static PointerResult Run(PointerAct act, Subject subject)
    {
        ArgumentNullException.ThrowIfNull(act);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(act.Clicks);

        // A pointer act needs no pattern — that is the whole reason it exists — but it does need
        // the element to be there, on screen and enabled, which is the rest of actionability. The
        // admission is how it is reached at all, so the check cannot be the line somebody drops.
        var admitted = Admitted.To(subject);
        var facts = admitted.Facts;

        var foreground = Foreground.Check(admitted.Window).AsPrecondition();
        if (!foreground.Satisfied)
            return new PointerResult(act, facts, foreground, default, PatternValues.None);

        var at = facts.Bounds;
        Send(at.Left + (at.Width / 2), at.Top + (at.Height / 2), act.Button, act.Clicks);

        // WW353. The send is left alone before this reads what it did, which is the one thing every
        // other synthesised verb was doing and this one was not: typing pauses, and press, nudge and
        // both picker walks poll until the reading moves. This sent and read on the next line, so
        // whatever was there at that instant was returned as the click's answer.
        //
        // A pause and not a poll, and the difference is what a click is. Its neighbours each wait
        // for a named thing to happen — the text to arrive, the focus to move, the range to change —
        // and a click has no such thing: it is as often a navigation whose effect is somewhere else
        // entirely, and `ActVerb` says so, so a poll until the reading moves would spend the whole
        // act budget on every click that correctly changes nothing.
        //
        // The number is the one WW329 measured for the same provocation, which is why this reaches
        // for it rather than choosing one: SendInput returns when the events are queued, and the
        // fault is a read arriving in the target's thread while its packets are still draining.
        //
        // What this does not claim is a fault removed. WW341 built the observable and read 1800
        // rounds of click, press and nudge on the guest with nothing late and nothing lost, which
        // bounds the click's rate under about 1% rather than showing it absent. So this is a shape
        // brought into line with its neighbours, and the arm that would notice if it mattered is
        // still there to be run.
        //
        // What it costs was measured rather than assumed, on the guest at 150 rounds each way:
        // <b>382ms a click round without it and 455ms with</b>. The difference is not the whole 73,
        // because press and nudge moved 234 to 268 and 196 to 235 between the same two runs without
        // being touched at all — so about 36 of it is the guest, and the pause is the fifty it says
        // it is, on a round that was already the longest of the three.
        Thread.Sleep(Keys.FirstLookMs);

        return new PointerResult(act, facts, foreground, at, subject.Read().Values);
    }

    /// <summary>
    /// What in a scenario needs a real desktop, said out loud. This is the point of declaring
    /// them: the cost lands where the reader is instead of on the run where it was discovered.
    /// <para>
    /// Grouped by reason rather than listed by locator. Three clicks is a number; two of them
    /// because nothing drew a peer and one because the shell owns the icon is a decision — the
    /// first pair may go away when the application grows one, and the third never will.
    /// </para>
    /// </summary>
    /// <param name="acts">Every pointer act the scenario declares.</param>
    public static IReadOnlyList<string> Reasons(IReadOnlyList<PointerAct> acts)
    {
        ArgumentNullException.ThrowIfNull(acts);
        if (acts.Count == 0)
            return [];

        return acts
            .GroupBy(one => one.Reason, StringComparer.Ordinal)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
                $"  {group.Count()} because {group.Key}: {string.Join(", ", group.Select(one => one.ToString()))}")
            .ToList();
    }

    /// <summary>The whole cost in one block: the count, then one line per reason.</summary>
    /// <param name="acts">Every pointer act the scenario declares.</param>
    public static string Summarise(IReadOnlyList<PointerAct> acts)
    {
        ArgumentNullException.ThrowIfNull(acts);
        if (acts.Count == 0)
            return "no act here needs a real desktop.";

        var reasons = Reasons(acts);
        var reachable = acts.Count(one => MayGetAPeer(one.Because));
        var future = reachable == 0
            ? ""
            : $" {reachable} of them would go away if the application drew a peer.";

        return $"{acts.Count} {(acts.Count == 1 ? "act needs" : "acts need")} a real desktop, "
            + $"for {reasons.Count} reason{(reasons.Count == 1 ? "" : "s")}.{future}\n"
            + string.Join('\n', reasons);
    }

    /// <summary>
    /// Every declared reason, checked against what the controls actually offer.
    /// <para>
    /// The other half worth wiring: a reason stated in the file is an assertion about the tree,
    /// and the tree is right here. An act that says its control has no peer, against a control
    /// offering Invoke, is a declaration that was true once — and reading it back at the one
    /// moment there is something to read it against is cheaper than believing it for a year.
    /// </para>
    /// </summary>
    /// <param name="root">The window under test.</param>
    /// <param name="acts">Every pointer act the scenario declares.</param>
    public static ReasonsChecked Check(AutomationElement root, IEnumerable<PointerAct> acts)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(acts);

        var disputed = new List<DisputedReason>();
        var unchecked_ = new List<PointerAct>();
        var agreed = new List<PointerAct>();

        foreach (var act in acts)
        {
            // Only two of the five claim anything about the tree. The rest are claims about what
            // somebody wanted, and a check that pretended to have read them would be the green
            // this project exists to withdraw.
            if (act.Because is not (PointerReason.NoAutomationPeer or PointerReason.CustomTemplate))
            {
                unchecked_.Add(act);
                continue;
            }

            var facts = Resolve.Once(root, act.Locator).Facts;
            if (facts is null)
            {
                unchecked_.Add(act);
                continue;
            }

            if (facts.Patterns.Count == 0)
            {
                agreed.Add(act);
                continue;
            }

            disputed.Add(new DisputedReason(
                act, facts, facts.Patterns.OrderBy(name => name, StringComparer.Ordinal).ToList()));
        }

        return new ReasonsChecked(disputed, unchecked_, agreed);
    }

    private static void Send(int x, int y, MouseButton button, int clicks)
    {
        var (down, up) = button switch
        {
            MouseButton.Right => (Win32.MouseRightDown, Win32.MouseRightUp),
            MouseButton.Middle => (Win32.MouseMiddleDown, Win32.MouseMiddleUp),
            _ => (Win32.MouseLeftDown, Win32.MouseLeftUp),
        };

        var left = Win32.GetSystemMetrics(Win32.VirtualScreenX);
        var top = Win32.GetSystemMetrics(Win32.VirtualScreenY);
        var width = Math.Max(1, Win32.GetSystemMetrics(Win32.VirtualScreenWidth));
        var height = Math.Max(1, Win32.GetSystemMetrics(Win32.VirtualScreenHeight));

        var inputs = new List<Win32.Input>
        {
            Mouse(Win32.MouseMove | Win32.MouseAbsolute | Win32.MouseVirtualDesk,
                (x - left) * 65535 / width, (y - top) * 65535 / height),
        };

        for (var press = 0; press < clicks; press++)
        {
            inputs.Add(Mouse(down, 0, 0));
            inputs.Add(Mouse(up, 0, 0));
        }

        Win32.SendInput((uint)inputs.Count, [.. inputs], System.Runtime.InteropServices.Marshal.SizeOf<Win32.Input>());
    }

    private static Win32.Input Mouse(uint flags, int dx, int dy) => new()
    {
        Type = Win32.InputMouse,
        Payload = new Win32.InputPayload
        {
            Mouse = new Win32.MouseInput { Dx = dx, Dy = dy, Flags = flags },
        },
    };
}

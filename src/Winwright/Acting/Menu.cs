using System.Collections.ObjectModel;
using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Tracing;
using Winwright.Verdicts;
using Winwright.Windowing;

namespace Winwright.Acting;

/// <summary>Where a walk through a menu got to, and what it passed on the way.</summary>
public sealed record MenuWalk
{
    internal MenuWalk(
        string what,
        string? wanted,
        string? highlighted,
        IReadOnlyList<string> passed,
        Precondition foreground,
        FocusReading focus)
    {
        What = what;
        Wanted = wanted;
        Highlighted = highlighted;
        Passed = passed;
        Foreground = foreground;
        Focus = focus;
    }

    /// <summary>What was asked of the menu — entered, walked to something, expanded.</summary>
    public string What { get; }

    /// <summary>The entry that was being looked for, where one was.</summary>
    public string? Wanted { get; }

    /// <summary>What is highlighted now.</summary>
    public string? Highlighted { get; }

    /// <summary>
    /// The entry itself, where the walk read one. WW457, and it is what a step compares against: the
    /// verb used to hand back whatever held the focus, which for a drop-down is the window the menu
    /// was raised from and never an entry at all.
    /// </summary>
    public ElementFacts? Entry { get; init; }

    /// <summary>Every entry highlighted on the way, in order.</summary>
    public IReadOnlyList<string> Passed { get; }

    /// <summary>Whether the window owned the desktop. Absent means no key was sent.</summary>
    public Precondition Foreground { get; }

    /// <summary>
    /// Whether the focus was still this application's when the walk stopped.
    /// <para>
    /// WW155. The foreground is checked once, before the keys are sent, so <see cref="Sent"/> is
    /// honest about the moment the act began — and the walk then polls for up to two seconds,
    /// during which the desk is free to change hands. This is the second guard, read at the end.
    /// </para>
    /// </summary>
    public FocusReading Focus { get; }

    /// <summary>Whether a key was sent at all.</summary>
    public bool Sent => Foreground.Satisfied;

    /// <summary>Whether this walk can say anything about the application at all.</summary>
    public bool Observed => Sent && Focus.Inside;

    /// <summary>Whether the walk reached what it was after.</summary>
    public bool Reached =>
        Observed && (Wanted is null || string.Equals(Highlighted, Wanted, StringComparison.Ordinal));

    /// <summary>How many entries were highlighted getting here.</summary>
    public int Hops => Passed.Count;

    /// <summary>What happened, with the route in it.</summary>
    /// <summary>
    /// The result a verdict counts. A desk that refused the foreground is a <em>hole</em> and never
    /// a failure: nothing was sent, so nothing about the application was checked at all.
    /// <para>
    /// WW155: and so is a focus that left the application while the walk was polling. An element
    /// belonging to somebody else's window compared against a wanted entry is a red about this
    /// application that nobody can reproduce, which is worse than no answer.
    /// </para>
    /// </summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult AsAssertion(string named)
    {
        if (!Foreground.Satisfied)
            return AssertionResult.Unchecked(named, Foreground);

        if (!Focus.Inside)
            return AssertionResult.Unchecked(named, Focus.AsPrecondition());

        return Reached
            ? AssertionResult.Pass(named, ToString())
            : AssertionResult.Fail(named, ToString());
    }

    public override string ToString()
    {
        if (!Sent)
            return $"{What} was not sent: {Foreground.Absence}.";

        var route = Passed.Count == 0 ? "nothing" : string.Join(" -> ", Passed);
        if (!Focus.Inside)
            return $"{What} walked {route} and then the focus left this application: {Focus.Because}.";

        return Reached
            ? $"{What} reached \"{Highlighted}\" through {route}."
            : $"{What} did not reach \"{Wanted}\"; it walked {route} and stopped on \"{Highlighted}\".";
    }

    /// <summary>The step a trace records.</summary>
    public TraceStep AsTraceStep() => new()
    {
        Verb = What,
        Locator = Wanted ?? "the menu",
        Resolved = Highlighted,
        Pattern = "synthesized keyboard",
        ReadBack = Highlighted,
        Polls = Hops,
        Verdict = Observed ? (Reached ? StepVerdict.Ok : StepVerdict.Failed) : StepVerdict.Unchecked,
        Detail = Reached ? null : ToString(),
    };
}

/// <summary>
/// A menu, driven the way a keyboard user drives one.
/// <para>
/// Down to the item, Right to expand, and <em>never invoke</em> — in claude-tray one entry
/// launches a terminal and another ends the run. There is no invoke on this surface at all, and
/// that is the whole answer to invoking a destructive entry by accident: a scenario that genuinely
/// means to press one reaches for <see cref="Act.Invoke"/> by name, which is a different call a
/// reader can see in the file.
/// </para>
/// <para>
/// The submenu appearing is an event, so it is polled to a deadline rather than slept at for a
/// fixed interval that is either too short on the day it matters or paid on every run. And nothing
/// here presses anything to reset between attempts: Left on a top-level entry dismisses the whole
/// menu, and retrying after one walked a menu that was no longer there and failed all three times.
/// </para>
/// </summary>
public static class Menu
{
    /// <summary>A backstop on the walk, so a menu that never repeats cannot spin forever.</summary>
    public const int MostEntries = 64;

    /// <summary>
    /// What is highlighted in this application's menu right now.
    /// <para>
    /// WW155. This took no window and answered the focused element of the whole desktop, so what a
    /// case asserted on was whatever held the desk and the menu was only implied. It takes the
    /// window now, and an element belonging to another application is not an answer about this
    /// menu — <see cref="Traversal.WhoHasFocus"/> is still there for a caller who wants the desk.
    /// </para>
    /// </summary>
    /// <param name="window">Any window of the application whose menu this is about.</param>
    public static string? Highlighted(nint window) => Entry(window)?.Says;

    /// <summary>
    /// The entry a menu is highlighting: the focus where the focus is an entry, and the menu's own
    /// reading of itself where it is not. WW457.
    /// <para>
    /// Every Win32 menu answers the first. A drop-down answers none of it — the window it was
    /// raised from keeps the focus — and this read that window's name as the highlight: measured on
    /// the guest as <c>Text 'winwright tray owner'</c>, a Static window in the menu's own process,
    /// which the process-scoped focus reading is right to admit and which is not an entry. The walk
    /// then had one name that never changed, so it stopped after one Down and Right expanded
    /// whichever entry the menu had opened on.
    /// </para>
    /// <para>
    /// So the focus counts only where it is an entry, and where it is not the menu is asked about
    /// itself. Selected or focused, because the two frameworks answer different halves of the same
    /// question: a Win32 popup's entry takes the focus, and a <c>ToolStripMenuItem</c> reports the
    /// selection its own container keeps.
    /// </para>
    /// <para>
    /// The last match and not the first, which is what a submenu needs: a menu whose submenu is open
    /// still shows the parent entry as selected, and the walk is inside the deeper one.
    /// </para>
    /// </summary>
    /// <param name="window">Any window of the application whose menu this is about.</param>
    private static ElementFacts? Entry(nint window)
    {
        if (Focus.Held(window) is { ControlType: MenuEntry } focused && focused.Says is not null)
            return focused;

        return Selected(window);
    }

    /// <summary>What UI Automation calls a menu's entries, spelled once. WW457.</summary>
    private const string MenuEntry = "MenuItem";

    /// <summary>
    /// The entry this menu shows as its own, read out of its subtree rather than off the desk.
    /// WW457.
    /// </summary>
    /// <param name="window">The window the menu is drawn in.</param>
    private static ElementFacts? Selected(nint window)
    {
        if (window == 0)
            return null;

        try
        {
            var entries = AutomationElement.FromHandle(window).FindAll(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem),
                    new OrCondition(
                        new PropertyCondition(AutomationElement.HasKeyboardFocusProperty, true),
                        new PropertyCondition(SelectionItemPattern.IsSelectedProperty, true))));

            return entries.Count == 0 ? null : ElementFacts.Of(entries[entries.Count - 1]);
        }
        catch (Exception unreachable)
            when (unreachable is ElementNotAvailableException or ArgumentException)
        {
            // A menu that went away while it was being read is not an entry, which is the same
            // direction the walk's own stopping rule already falls in.
            return null;
        }
    }

    /// <summary>Enter the menu bar, the way F10 does for a keyboard user.</summary>
    /// <param name="window">The window whose menu bar F10 is sent at.</param>
    /// <param name="desk">
    /// How long to wait for that window to hold the foreground, or <see cref="DeskWait.Once" />.
    /// WW470.
    /// </param>
    /// <param name="settleMs">How long to wait for the highlight to move, which says the key landed.</param>
    /// <param name="pollMs">How often to look again while waiting for it.</param>
    public static MenuWalk Enter(nint window, DeskWait desk, int settleMs = 2000, int pollMs = 25)
    {
        var foreground = Reaches(window, desk);
        if (!foreground.Satisfied)
            return new MenuWalk("enter the menu", null, Highlighted(window), [], foreground, Focus.In(window));

        var before = Highlighted(window);
        Keys.SendMenuBar();
        Attempt.UntilTrue(() => Highlighted(window) is { } now && now != before, settleMs, pollMs);

        // Read at the end and carried, so the answer and the reading behind it cannot be two
        // different moments — which is the whole shape of the defect this fixed.
        //
        // WW457: two readings rather than one, because they answer two questions. The focus says
        // whether this run is entitled to talk about what it found; the highlight says what the menu
        // is showing, which a drop-down does not answer through the focus at all.
        var focus = Focus.In(window);
        var entry = Entry(window);
        var landed = entry?.Says;

        return new MenuWalk(
            "enter the menu", null, landed, landed is null ? [] : [landed], foreground, focus)
        {
            Entry = entry,
        };
    }

    /// <summary>
    /// Walk down until <paramref name="entry"/> is highlighted. Nothing is pressed to normalise
    /// first, and the walk stops when an entry comes round again rather than when a counter says
    /// so — a menu that has been walked once has shown everything it holds.
    /// </summary>
    /// <param name="window">The window whose open menu is walked.</param>
    /// <param name="entry">The entry to stop on.</param>
    /// <param name="desk">
    /// How long to wait for that window to hold the foreground, or <see cref="DeskWait.Once" />.
    /// WW470.
    /// </param>
    /// <param name="settleMs">How long to wait for the highlight to move, which says the key landed.</param>
    /// <param name="pollMs">How often to look again while waiting for it.</param>
    public static MenuWalk To(nint window, string entry, DeskWait desk, int settleMs = 2000, int pollMs = 25)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entry);

        var foreground = Reaches(window, desk);
        if (!foreground.Satisfied)
            return new MenuWalk("walk to", entry, Highlighted(window), [], foreground, Focus.In(window));

        var passed = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var here = Highlighted(window);
        if (here is not null)
        {
            passed.Add(here);
            seen.Add(here);
            if (string.Equals(here, entry, StringComparison.Ordinal))
            {
                return new MenuWalk(
                    "walk to", entry, here, new ReadOnlyCollection<string>(passed), foreground, Focus.In(window));
            }
        }

        for (var hop = 0; hop < MostEntries; hop++)
        {
            var was = here;
            Keys.Send(TraversalKey.Down);
            Attempt.UntilTrue(() => Highlighted(window) is { } now && now != was, settleMs, pollMs);

            // The walk stops where the focus leaves the application as readily as where the menu
            // comes round: an entry read off somebody else's window is not an entry this walked.
            here = Highlighted(window);
            if (here is null || !seen.Add(here))
                break;

            passed.Add(here);
            if (string.Equals(here, entry, StringComparison.Ordinal))
                break;
        }

        return new MenuWalk(
            "walk to", entry, here, new ReadOnlyCollection<string>(passed), foreground, Focus.In(window));
    }

    /// <summary>
    /// Expand what is highlighted, and wait for the submenu to arrive. An entry with no submenu
    /// is not an error here: the deadline passes, the highlight has not moved, and the answer says
    /// which entry it was.
    /// </summary>
    /// <param name="window">The window whose highlighted entry is expanded.</param>
    /// <param name="desk">
    /// How long to wait for that window to hold the foreground, or <see cref="DeskWait.Once" />.
    /// WW470.
    /// </param>
    /// <param name="settleMs">How long to wait for the submenu to arrive.</param>
    /// <param name="pollMs">How often to look again while waiting for it.</param>
    public static MenuWalk Expand(nint window, DeskWait desk, int settleMs = 2000, int pollMs = 25)
    {
        var foreground = Reaches(window, desk);
        var opening = Highlighted(window);
        if (!foreground.Satisfied)
            return new MenuWalk("expand", opening, opening, [], foreground, Focus.In(window));

        Keys.Send(TraversalKey.Right);
        Attempt.UntilTrue(() => Highlighted(window) is { } now && now != opening, settleMs, pollMs);

        // WW457. The highlight and not the focus, for the reason `Entry` gives: the window a
        // drop-down was raised from holds the focus the whole time, so reading the answer off it
        // reported that window's own name as the entry this expanded onto.
        var focus = Focus.In(window);
        var entry = Entry(window);
        var landed = entry?.Says;
        var moved = landed is not null && landed != opening;

        return new MenuWalk(
            "expand",
            moved ? landed : opening,
            landed,
            moved ? [opening ?? "", landed!] : [],
            foreground,
            focus)
        {
            Entry = entry,
        };
    }

    /// <summary>
    /// Back out of the menu. This is not a reset between attempts and is never used as one — it
    /// is how a case leaves the window the way it found it, once it is done with the menu.
    /// </summary>
    public static void Dismiss(int times = 2)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(times);
        for (var each = 0; each < times; each++)
            Keys.SendEscape();
    }

    private static nint Top(nint window) => window == 0 ? 0 : Win32.GetAncestor(window, Win32.GaRoot);

    /// <summary>
    /// Whether a key would reach this menu, asked of the desk rather than of the foreground alone.
    /// WW457.
    /// <para>
    /// The foreground reading is right about a window and wrong about a tracked popup. A
    /// <c>TrackPopupMenu</c> requires the foreground on its <em>owner</em> before it will track at
    /// all, so a menu that is up and working has the desk on a different window of the same process
    /// — which <see cref="ForegroundState.SameProcess" /> calls an intruder, correctly for every
    /// other act and wrongly for this one.
    /// </para>
    /// <para>
    /// Measured rather than reasoned. WW453 drove <c>open submenu</c> against a launched tray and
    /// the Win32 arm was excused reading <c>another window of the same process owns it: 'winwright
    /// tray owner'</c> — which is the window the shell had just been told to put the foreground on,
    /// and the reason the menu was on the screen at all.
    /// </para>
    /// <para>
    /// Asked of Windows and never inferred from ownership, which is the whole of why this is narrow:
    /// <c>GUI_INMENUMODE</c> is the system saying a menu is being worked right now, and
    /// <c>hwndMenuOwner</c> is which window is working it. An owned popup that is not a menu answers
    /// false, so nothing else widens by this.
    /// </para>
    /// <para>
    /// The foreground thread's and never a thread this engine picked: a menu is worked by whoever
    /// holds the desk, so the state worth reading is the state of the thread that holds it. A
    /// process that is not in the foreground can have a menu standing and no key would reach it,
    /// which is the same thing the reading being corrected here already says.
    /// </para>
    /// </summary>
    /// <param name="window">The menu a key is about to be sent at.</param>
    /// <returns>The owner working a menu, or zero where the desk is not in one.</returns>
    public static nint MenuOwner(nint window)
    {
        if (window == 0)
            return 0;

        var info = new Win32.GuiThreadInfo { Size = System.Runtime.InteropServices.Marshal.SizeOf<Win32.GuiThreadInfo>() };
        if (!Win32.GetGUIThreadInfo(0, ref info) || (info.Flags & Win32.GuiInMenuMode) == 0 || info.MenuOwner == 0)
            return 0;

        // The menu being worked is this one, or another window of the same process is working it.
        //
        // The second half is the repair and the first is what it replaced. A `#32768` popup is created
        // by the system, and the window `TrackPopupMenu` was handed is not its `GW_OWNER` — measured
        // on the guest, where this answered zero for a tray menu that was up and being worked, and
        // the case went on being excused on the absence it was written to remove.
        //
        // The process is what links them, and it is the same link the refusal itself draws: the
        // reading being corrected is `SameProcess`, which is another window of this process holding
        // the desk. Where that window is working a menu, the key reaches the menu.
        if (info.MenuOwner == window)
            return info.MenuOwner;

        Win32.GetWindowThreadProcessId(info.MenuOwner, out var working);
        Win32.GetWindowThreadProcessId(window, out var mine);

        return working != 0 && working == mine ? info.MenuOwner : 0;
    }

    /// <summary>
    /// The window holding the desk while this menu stands, where the two are one thread's. WW457.
    /// <para>
    /// The same arrangement <see cref="MenuOwner" /> reads, one framework over and invisible to it. A
    /// <c>ContextMenuStrip</c> is not a Win32 menu and sets no menu mode, so Windows says nothing
    /// about it at all; what WinForms does instead is put the foreground on a window of its own
    /// before showing the drop-down, which is the documented way a tray menu is raised. The menu and
    /// that window are two windows of one thread, and a synthesised key goes to the foreground
    /// thread's queue — the queue the drop-down's own message filter is reading.
    /// </para>
    /// <para>
    /// The thread and never the process, which is the whole of what keeps this narrow. An
    /// application's own dialog taking the desk from its own window is two threads or two queues, and
    /// a key sent then really does land elsewhere: <c>MenuTests</c> drives that arm with a second
    /// pumped dialog and holds it as a hole.
    /// </para>
    /// <para>
    /// And the control type is the other half of it, asked of the tree rather than of the class name:
    /// a drop-down's window class is its framework's and only the tree calls it a menu. An ordinary
    /// window with a sibling on its thread is not a menu and is not admitted here.
    /// </para>
    /// </summary>
    /// <param name="window">The menu a key is about to be sent at.</param>
    /// <returns>The window holding the foreground on this menu's own thread, or zero.</returns>
    public static nint RaisedFrom(nint window)
    {
        if (window == 0 || !AMenu(window))
            return 0;

        // The engine's own reading and not the primitive, which is what keeps the desk sweep honest:
        // a verb of this class reaches the foreground through `Foreground`, where every other one does.
        var holder = Foreground.Now().Window;
        if (holder == 0)
            return 0;

        var raising = Win32.GetWindowThreadProcessId(holder, out _);
        var drawn = Win32.GetWindowThreadProcessId(window, out _);

        return raising != 0 && raising == drawn ? holder : 0;
    }

    /// <summary>
    /// Whether the window a key is aimed at is a menu at all. WW457, and asked of UI Automation
    /// because that is the one reading both kinds answer: <c>#32768</c> and a framework's own
    /// drop-down share no window class and are both a <c>Menu</c> in the tree.
    /// </summary>
    /// <param name="window">The window to ask about.</param>
    private static bool AMenu(nint window)
    {
        try
        {
            return AutomationElement.FromHandle(window).Current.ControlType == ControlType.Menu;
        }
        catch (Exception unreachable)
            when (unreachable is ElementNotAvailableException or ArgumentException)
        {
            // A handle the tree cannot reach is not a menu this run may send a key into, which is
            // the same direction every other reading here falls in.
            return false;
        }
    }

    /// <summary>
    /// The foreground reading a menu act turns on: the ordinary one, unless the desk says a menu is
    /// up and this is it. WW457.
    /// <para>
    /// Two widenings and both are narrow, each with its reason where it is read:
    /// <see cref="MenuOwner" /> is a Win32 menu being worked, which Windows says outright, and
    /// <see cref="RaisedFrom" /> is the same arrangement one framework over, which it says nothing
    /// about at all. Neither is inferred from ownership, and a window that is not a menu is admitted
    /// by neither.
    /// </para>
    /// <para>
    /// Measured from the adopter's side. claude-tray's submenu step read `another window of the same
    /// process owns it: ClaudeTray (pid 6092) (untitled), and the window under test is ClaudeTray
    /// (pid 6092) (untitled)` — two untitled windows of one application, which is a drop-down raised
    /// from its own hidden window described from outside.
    /// </para>
    /// <para>
    /// WW470. The widenings are asked before the wait and again after it, and the order is the whole
    /// of what keeps a menu walk affordable: a drop-down being worked is the case they exist for, so
    /// a wait in front of them would spend the resolve budget on every submenu step before arriving
    /// at an answer that was already true — twice per step, since a walk and the expansion after it
    /// are two calls. Asked again afterwards because the wait is time in which a menu can be raised.
    /// </para>
    /// </summary>
    /// <param name="window">The menu a key is about to be sent at.</param>
    /// <param name="desk">How long to wait for it to come forward before concluding. WW470.</param>
    private static Precondition Reaches(nint window, DeskWait desk)
    {
        if (Widened(window))
            return Precondition.Met(Foreground.PreconditionName);

        var foreground = Foreground.Waited(Top(window), desk).AsPrecondition();
        if (foreground.Satisfied)
            return foreground;

        return Widened(window) ? Precondition.Met(Foreground.PreconditionName) : foreground;
    }

    /// <summary>
    /// Whether the desk says a menu is up and this is it, which is the reading WW457 added beside
    /// the ordinary one.
    /// </summary>
    private static bool Widened(nint window) => MenuOwner(window) != 0 || RaisedFrom(window) != 0;
}

using System.Collections.ObjectModel;

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
    public static string? Highlighted(nint window) =>
        Focus.Held(window)?.Name is { Length: > 0 } name ? name : null;

    /// <summary>Enter the menu bar, the way F10 does for a keyboard user.</summary>
    public static MenuWalk Enter(nint window, int settleMs = 2000, int pollMs = 25)
    {
        var foreground = Reaches(window);
        if (!foreground.Satisfied)
            return new MenuWalk("enter the menu", null, Highlighted(window), [], foreground, Focus.In(window));

        var before = Highlighted(window);
        Keys.SendMenuBar();
        Attempt.UntilTrue(() => Highlighted(window) is { } now && now != before, settleMs, pollMs);

        // Read once at the end and carried, so the answer and the reading behind it cannot be two
        // different moments — which is the whole shape of the defect this fixed.
        var focus = Focus.In(window);
        var landed = focus.Held?.Name is { Length: > 0 } name ? name : null;

        return new MenuWalk(
            "enter the menu", null, landed, landed is null ? [] : [landed], foreground, focus);
    }

    /// <summary>
    /// Walk down until <paramref name="entry"/> is highlighted. Nothing is pressed to normalise
    /// first, and the walk stops when an entry comes round again rather than when a counter says
    /// so — a menu that has been walked once has shown everything it holds.
    /// </summary>
    public static MenuWalk To(nint window, string entry, int settleMs = 2000, int pollMs = 25)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entry);

        var foreground = Reaches(window);
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
    public static MenuWalk Expand(nint window, int settleMs = 2000, int pollMs = 25)
    {
        var foreground = Reaches(window);
        var opening = Highlighted(window);
        if (!foreground.Satisfied)
            return new MenuWalk("expand", opening, opening, [], foreground, Focus.In(window));

        Keys.Send(TraversalKey.Right);
        Attempt.UntilTrue(() => Highlighted(window) is { } now && now != opening, settleMs, pollMs);

        var focus = Focus.In(window);
        var landed = focus.Held?.Name is { Length: > 0 } name ? name : null;
        var moved = landed is not null && landed != opening;

        return new MenuWalk(
            "expand",
            moved ? landed : opening,
            landed,
            moved ? [opening ?? "", landed!] : [],
            foreground,
            focus);
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
    /// The foreground reading a menu act turns on: the ordinary one, unless the desk says a menu is
    /// up and this is it. WW457.
    /// </summary>
    /// <param name="window">The menu a key is about to be sent at.</param>
    private static Precondition Reaches(nint window)
    {
        var foreground = Foreground.Check(Top(window)).AsPrecondition();
        if (foreground.Satisfied || MenuOwner(window) == 0)
            return foreground;

        return Precondition.Met(Foreground.PreconditionName);
    }
}

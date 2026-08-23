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
        string what, string? wanted, string? highlighted, IReadOnlyList<string> passed, Precondition foreground)
    {
        What = what;
        Wanted = wanted;
        Highlighted = highlighted;
        Passed = passed;
        Foreground = foreground;
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

    /// <summary>Whether a key was sent at all.</summary>
    public bool Sent => Foreground.Satisfied;

    /// <summary>Whether the walk reached what it was after.</summary>
    public bool Reached => Sent && (Wanted is null || string.Equals(Highlighted, Wanted, StringComparison.Ordinal));

    /// <summary>How many entries were highlighted getting here.</summary>
    public int Hops => Passed.Count;

    /// <summary>What happened, with the route in it.</summary>
    /// <summary>
    /// The result a verdict counts. A desk that refused the foreground is a <em>hole</em> and never
    /// a failure: nothing was sent, so nothing about the application was checked at all.
    /// </summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult AsAssertion(string named)
    {
        if (!Foreground.Satisfied)
            return AssertionResult.Unchecked(named, Foreground);

        return Reached
            ? AssertionResult.Pass(named, ToString())
            : AssertionResult.Fail(named, ToString());
    }

    public override string ToString()
    {
        if (!Sent)
            return $"{What} was not sent: {Foreground.Absence}.";

        var route = Passed.Count == 0 ? "nothing" : string.Join(" -> ", Passed);
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
        Verdict = !Sent ? StepVerdict.Unchecked : Reached ? StepVerdict.Ok : StepVerdict.Failed,
        Detail = Sent && Reached ? null : ToString(),
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

    /// <summary>What is highlighted in the menu right now, which is what holds the focus.</summary>
    public static string? Highlighted() => Traversal.WhoHasFocus()?.Name is { Length: > 0 } name ? name : null;

    /// <summary>Enter the menu bar, the way F10 does for a keyboard user.</summary>
    public static MenuWalk Enter(nint window, int settleMs = 2000, int pollMs = 25)
    {
        var foreground = Foreground.Check(Top(window)).AsPrecondition();
        if (!foreground.Satisfied)
            return new MenuWalk("enter the menu", null, Highlighted(), [], foreground);

        var before = Highlighted();
        Keys.SendMenuBar();
        Attempt.UntilTrue(() => Highlighted() is { } now && now != before, settleMs, pollMs);

        var landed = Highlighted();
        return new MenuWalk("enter the menu", null, landed, landed is null ? [] : [landed], foreground);
    }

    /// <summary>
    /// Walk down until <paramref name="entry"/> is highlighted. Nothing is pressed to normalise
    /// first, and the walk stops when an entry comes round again rather than when a counter says
    /// so — a menu that has been walked once has shown everything it holds.
    /// </summary>
    public static MenuWalk To(nint window, string entry, int settleMs = 2000, int pollMs = 25)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entry);

        var foreground = Foreground.Check(Top(window)).AsPrecondition();
        if (!foreground.Satisfied)
            return new MenuWalk("walk to", entry, Highlighted(), [], foreground);

        var passed = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var here = Highlighted();
        if (here is not null)
        {
            passed.Add(here);
            seen.Add(here);
            if (string.Equals(here, entry, StringComparison.Ordinal))
                return new MenuWalk("walk to", entry, here, new ReadOnlyCollection<string>(passed), foreground);
        }

        for (var hop = 0; hop < MostEntries; hop++)
        {
            var was = here;
            Keys.Send(TraversalKey.Down);
            Attempt.UntilTrue(() => Highlighted() is { } now && now != was, settleMs, pollMs);

            here = Highlighted();
            if (here is null || !seen.Add(here))
                break;

            passed.Add(here);
            if (string.Equals(here, entry, StringComparison.Ordinal))
                break;
        }

        return new MenuWalk("walk to", entry, here, new ReadOnlyCollection<string>(passed), foreground);
    }

    /// <summary>
    /// Expand what is highlighted, and wait for the submenu to arrive. An entry with no submenu
    /// is not an error here: the deadline passes, the highlight has not moved, and the answer says
    /// which entry it was.
    /// </summary>
    public static MenuWalk Expand(nint window, int settleMs = 2000, int pollMs = 25)
    {
        var foreground = Foreground.Check(Top(window)).AsPrecondition();
        var opening = Highlighted();
        if (!foreground.Satisfied)
            return new MenuWalk("expand", opening, opening, [], foreground);

        Keys.Send(TraversalKey.Right);
        Attempt.UntilTrue(() => Highlighted() is { } now && now != opening, settleMs, pollMs);

        var landed = Highlighted();
        var moved = landed is not null && landed != opening;
        return new MenuWalk(
            "expand",
            moved ? landed : opening,
            landed,
            moved ? [opening ?? "", landed!] : [],
            foreground);
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
}

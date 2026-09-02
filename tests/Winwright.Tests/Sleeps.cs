using System.Collections.ObjectModel;

using Xunit;

namespace Winwright.Tests;

/// <summary>What a sleep is doing, where one is not a wait.</summary>
internal enum Sleeping
{
    /// <summary>The interval between looks. This is what a deadline is made of.</summary>
    Machinery,

    /// <summary>The interval is the resolution of a measurement, so a deadline would delete it.</summary>
    Resolution,

    /// <summary>The sleep is the thing being reproduced, not a way of waiting for it.</summary>
    UnderTest,

    /// <summary>
    /// A wait, and it stays one. There is nothing observable to wait on, and a condition that goes
    /// true too early is worse than the sleep it replaced because it reads as a wait that was
    /// proved. Counted here, and never silently.
    /// </summary>
    StillAWait,

    /// <summary>
    /// Parked on a signal another thread sets, with a deadline. WW198: the opposite of a sleep and
    /// counted anyway, because the point of this catalogue is to see every way of parking a thread
    /// and then say which is which — an unseen one cannot be called right.
    /// </summary>
    OnASignal,

    /// <summary>
    /// The looking is what disturbs the thing being looked at, so the interval is not a wait for a
    /// condition — it is the condition's own precondition. WW329, and it is the arm this catalogue
    /// did not have: a deadline that polled from the first millisecond would be the defect rather
    /// than the way of avoiding one.
    /// </summary>
    Undisturbed,
}

/// <summary>
/// One file that sleeps, and why its sleeping is not a scenario waiting.
/// </summary>
/// <param name="File">The source file, by name.</param>
/// <param name="Sleeps">How many times it calls it.</param>
/// <param name="Kind">What the sleeping is doing.</param>
/// <param name="Because">Why it is that rather than a wait.</param>
internal sealed record Sleep(string File, int Sleeps, Sleeping Kind, string Because)
{
    public override string ToString() => $"{File,-22} {Sleeps}x {Kind,-11} {Because}";
}

/// <summary>
/// WW184. Block C's second criterion says no scenario carries a sleep: every wait is a deadline on
/// a condition, and how long it took is in the trace for whoever wants to tune it. Nothing checked
/// it, and seven files call <c>Thread.Sleep</c>.
/// <para>
/// Several are certainly right, and their being right is the point. <c>Attempt</c> sleeps between
/// polls, which is the deadline machinery itself. <c>FrameRun</c> paces frames, and WW143 argued
/// that case in writing: the interval is the resolution of the measurement, so turning it into a
/// deadline would delete the observation. The fixture sleeps because a thread parked without
/// pumping is the defect <c>--pump=none</c> exists to reproduce.
/// </para>
/// <para>
/// One is a wait and stays one, with the measurement written beside it. That entry is the reason
/// this is a catalogue rather than a ban: a rule that admitted no exceptions would be answered by
/// somebody spelling the sleep differently, and then nothing would know about it at all.
/// </para>
/// <para>
/// The call and never the words. <c>Thread.Sleep(</c> with its bracket, because two files discuss
/// sleeping in prose — this one and <c>Waits</c> — and a scan that counted those would report a
/// criterion broken by a comment explaining why it is not.
/// </para>
/// </summary>
internal static class Sleeps
{
    /// <summary>
    /// Every way of parking a thread this reading knows, matched in the sources exactly as written.
    /// <para>
    /// WW198. This was one spelling, and the doc above argued for a catalogue over a ban in these
    /// words: a rule admitting no exceptions "would be answered by somebody spelling the sleep
    /// differently, and then nothing would know about it at all". It was answered from inside.
    /// <c>FrameRun</c> parks twice — <c>Thread.Sleep</c> for the bulk of the interval and
    /// <c>Thread.SpinWait</c> for the last sixteen milliseconds — and the count said one.
    /// </para>
    /// <para>
    /// Six now, of which three are in the tree and three are what a reader reaches for next. The
    /// empty ones are the cheap half: a spelling written down before anybody uses it costs nothing,
    /// and the one that was not written down cost a count being quietly wrong.
    /// </para>
    /// </summary>
    internal static IReadOnlyList<string> Spellings { get; } = new ReadOnlyCollection<string>(
    [
        "Thread.Sleep(",
        "Thread.SpinWait(",
        "Task.Delay(",
        "SpinWait.SpinUntil(",
        ".WaitOne(",
        ".Wait(",
    ]);

    internal static IReadOnlyList<Sleep> Known { get; } = new ReadOnlyCollection<Sleep>(
    [
        // --- the engine ---------------------------------------------------------------------------
        new("Attempt.cs", 2, Sleeping.Machinery,
            "the interval between one look and the next, in both the sighting deadline and the "
                + "condition one — a deadline that did not sleep would be a spin"),
        new("Expectation.cs", 1, Sleeping.Machinery,
            "the same interval, in the poll an expectation takes while it watches a subject"),
        new("FrameRun.cs", 2, Sleeping.Resolution,
            "frames are paced, and WW143 wrote the argument down: the interval is the resolution of "
                + "the measurement, and the last sixteen milliseconds are spun because the scheduler "
                + "overshoots by more than a third of an interval at twenty-five frames a second. "
                + "WW198: two, and the second is that spin — it was described here and counted "
                + "nowhere, which is the whole of what that task was about"),
        new("Program.cs", 1, Sleeping.UnderTest,
            "the fixture parks a thread without pumping, which is the whole of what --pump=none "
                + "reproduces: a single-threaded apartment that blocks any other way keeps answering "
                + "messages and is not dead at all"),
        new("Pointer.cs", 1, Sleeping.Undisturbed,
            "WW353, and the same measurement as the entry below rather than a second one: the click "
                + "was the one synthesised verb that sent and read on the next line, where its "
                + "neighbours each wait for a named thing to happen. It has no such thing to wait "
                + "for — a click is as often a navigation whose effect is elsewhere — so it takes "
                + "the pause WW329 priced instead of a poll that would spend the act budget on "
                + "every click that correctly changes nothing. Priced with WW341's arm at 150 "
                + "rounds each way on the guest: 382ms a click round without it and 455ms with, of "
                + "which about 36 is the guest — press and nudge moved 234 to 268 and 196 to 235 "
                + "between the same two runs without being touched. Both read 0 late of 150, so "
                + "what this bought is the shape and not a fault removed"),
        new("Keyboard.cs", 1, Sleeping.Undisturbed,
            "WW329, and the one entry here where the sleeping is the repair. The engine's own poll "
                + "was provoking the substitution it then resent: SendInput returns once the events "
                + "are queued rather than processed, so the first look landed in the window's thread "
                + "while its packets were still being translated. Measured on the guest over 1200 "
                + "rounds each — 31 substitutions with no pause, none at 50ms, none at 150ms — and "
                + "there is nothing to wait on instead, because what would end the wait is the look"),
        new("Finished.cs", 1, Sleeping.Machinery,
            "WW218's move into place needs delete access to the destination, and a harness polling "
                + "that file has it open — so this is the interval between one attempt and the next, "
                + "the same machinery a deadline is made of, against a collision that clears in "
                + "milliseconds because the thing holding the file is a poll"),

        // --- the suite ----------------------------------------------------------------------------
        new("FixtureTests.cs", 1, Sleeping.Resolution,
            "the sampler that watches an animation, where the interval is again the resolution of "
                + "what is being measured rather than a wait for it"),
        new("FrameRunTests.cs", 2, Sleeping.UnderTest,
            "a writer made deliberately slow so the capture falls behind — the sleeping is the "
                + "condition under test and waiting for it would be waiting for the case's own hand"),
        // --- parked on a signal, which WW198 widened the reading to see ------------------------------
        new("ApartmentTests.cs", 2, Sleeping.UnderTest,
            "an event nobody ever sets, waited on twice so the apartment's own deadline has something "
                + "that genuinely never finishes to time out on — the parking is the condition under "
                + "test, and a signal that arrived would delete the case"),
        new("PumpedDialog.cs", 1, Sleeping.OnASignal,
            "the thread that owns the window sets this once the window exists, so the constructor "
                + "hands back a dialog that is really there rather than one that will be shortly — "
                + "with a ten-second deadline, so a thread that never starts is a refusal and not a hang"),
        new("PumpedFlyout.cs", 1, Sleeping.OnASignal,
            "WW347: the same shape for a WPF popup, and set in a finally so a flyout that would not "
                + "open is reported as the refusal it is rather than parking the case for the whole "
                + "deadline — which is the difference between a red naming what WPF said and a hang"),
        new("AnsweringWindow.cs", 1, Sleeping.OnASignal,
            "WW349: the same shape again, for the window that answers a render. The thread has to be "
                + "pumping before the send goes out, so what this parks on is the hook being on — a "
                + "send to a window that is not yet answering would spend the whole ask being declined"),
        new("FinishedTests.cs", 3, Sleeping.OnASignal,
            "two signals and one delay, and the shape is the point: a reader takes the destination "
                + "and parks until it is told to let go, so the case knows the file really is open "
                + "rather than about to be — and the letting go is delayed rather than triggered by "
                + "the move, because a collision that had already cleared would not be one"),
        new("Injections.cs", 1, Sleeping.OnASignal,
            "the hook's own thread signals once SetWindowsHookEx has answered, either way, so the "
                + "window is never typed at by a run whose recorder is still being installed — with "
                + "a five-second deadline, because a thread that never starts has to be reported "
                + "rather than hang the fixture on the failure this parking exists to catch"),
        new("TrayIconFixture.cs", 1, Sleeping.OnASignal,
            "the same shape for the icon: the shell is asked and the add is signalled, which is what "
                + "lets every case look straight after Add rather than waiting a guessed interval"),

        new("TraversalTests.cs", 1, Sleeping.StillAWait,
            "there is nothing out here to observe: the state a traversal settles into is the one "
                + "after the change, and a condition true too early reads as a wait that was proved. "
                + "It stays a sleep, with the measurement, until there is something to wait on"),
    ]);

    /// <summary>The ones that are still waits, counted rather than left off.</summary>
    internal static IReadOnlyList<Sleep> Waiting() =>
        new ReadOnlyCollection<Sleep>(Known.Where(one => one.Kind == Sleeping.StillAWait).ToList());

    /// <summary>Every file that calls it, and how many times, read out of the sources.</summary>
    internal static IReadOnlyList<Sleep> Found() => scanned.Value;

    private static readonly Lazy<IReadOnlyList<Sleep>> scanned = new(Scan);

    private static IReadOnlyList<Sleep> Scan()
    {
        var found = new List<Sleep>();

        // WW193. Checkout's walk, so the exclusions are settled in one place — and its own note on
        // leaving a catalogue's own file out is the same reason this one had.
        foreach (var file in Checkout.Sources(Checkout.Everything, except: $"{nameof(Sleeps)}.cs"))
        {
            // WW198. Code and never prose, which this file's own doc has claimed since WW184 and now
            // does: the call with its bracket was chosen so a comment about sleeping was not counted,
            // and stripping the comment says the same thing without relying on how it was spelled.
            var calls = File.ReadLines(file)
                .Select(Checkout.Code)
                .Sum(line => Spellings.Sum(one => Occurrences(line, one)));

            if (calls > 0)
                found.Add(new Sleep(Path.GetFileName(file), calls, Sleeping.Machinery, ""));
        }

        return found.OrderBy(one => one.File, StringComparer.Ordinal).ToList();
    }

    private static int Occurrences(string text, string what)
    {
        var count = 0;
        var at = text.IndexOf(what, StringComparison.Ordinal);
        while (at >= 0)
        {
            count++;
            at = text.IndexOf(what, at + what.Length, StringComparison.Ordinal);
        }

        return count;
    }

}

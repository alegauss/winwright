using System.Diagnostics;
using System.Runtime.InteropServices;

using Winwright.Locating;

namespace Winwright.Typing;

/// <summary>
/// WW342. Which half of a cross-process read provokes the fault: the call, or the pumping it forces
/// on the thread it calls into.
/// <para>
/// WW329 measured a repair and not a mechanism. A UI Automation read against the window under test
/// is two things at once — a call out of this process, and a message loop run on <em>that</em>
/// process's own thread to answer it — and delaying the read removes both. So the fifty
/// milliseconds are a floor found by sweeping rather than a duration anything derived, and any other
/// reader of that window inherits a question nothing can answer: a case watching a caption while a
/// send is in flight is doing whatever the first look was doing, and there is no rule to tell it
/// apart from one that is not.
/// </para>
/// <para>
/// Four arms, and they are a two-by-two. <c>quiet</c> does nothing while the queue drains, which is
/// WW312's control and read no fault in six hundred rounds. <c>read</c> is the engine's own shape
/// and read one in a hundred and fifty. The two new ones take the pair apart: <c>poke</c> makes the
/// window's thread pump and reads nothing from it, and <c>peek</c> touches the window without its
/// thread doing any work at all.
/// </para>
/// <para>
/// <c>poke</c> is <c>SendMessageTimeout</c> with <c>WM_NULL</c>. It blocks until the target thread
/// dispatches it, which is precisely the pumping a read forces and nothing else — no property
/// crosses back. <c>peek</c> is <c>GetWindowRect</c>, which USER answers out of the window's own
/// kernel-side record without the owning thread running at all; it is not a read of the control and
/// is not meant to be, it is the call that touches the window and leaves its thread alone.
/// </para>
/// <para>
/// So the reading is which arms fault. Both <c>poke</c> and <c>read</c> and not <c>peek</c> says it
/// is the pumping, and then the interval is about how long the queue needs rather than about the
/// reader — and any reader can be judged by whether it pumps. <c>read</c> alone says the fault needs
/// the automation call itself, and a cheaper reader might avoid it. <c>peek</c> faulting says the
/// mechanism is neither and the model is wrong, which is the answer worth most.
/// </para>
/// <para>
/// What it read, on the guest on 2026-09-02, 400 rounds an arm: quiet 0, peek 0, poke 0, read 8 —
/// 2.00%, beside the 2.58% WW329 measured on the same shape. So it is <c>read</c> alone, and the
/// pumping is acquitted by 4800 dispatched messages that provoked nothing. Whatever the automation
/// call does to that thread, a message loop turning over is not it: the provider runs on WPF's own
/// dispatcher and does work of a different kind, and that work is what the fifty milliseconds are
/// waiting out. Every substitution had a clean injection, so the fault is still made after the send.
/// </para>
/// <para>
/// What that opens is WW355: the pause is paid by every send this engine makes, and it may be
/// paying for the provider rather than for the queue. A reader that asks the provider for less —
/// or asks something other than the provider — would be a repair with no interval in it at all.
/// </para>
/// </summary>
internal static class Disturbance
{
    /// <summary>What is done to the window while its queue drains.</summary>
    private enum Arm
    {
        /// <summary>Nothing at all. WW312's control.</summary>
        Quiet,

        /// <summary>Its rectangle read, which its own thread never learns about.</summary>
        Peek,

        /// <summary>Its thread made to dispatch a message that says nothing.</summary>
        Poke,

        /// <summary>Its control read through automation, which is the engine's own first look.</summary>
        Read,
    }

    /// <summary>The arms, in the order the report reads them: nothing, then each half, then both.</summary>
    private static readonly Arm[] Arms = [Arm.Quiet, Arm.Peek, Arm.Poke, Arm.Read];

    /// <summary>
    /// How long the disturbance runs for, and it is WW312's number rather than a new one. The whole
    /// value of this arm is that it differs from that sweep in one dimension — what is done during
    /// the drain — so the drain itself is the same length it was there.
    /// </summary>
    private const int DrainMs = 300;

    /// <summary>How often the disturbance repeats, which is the engine's own poll interval.</summary>
    private const int PollMs = 25;

    /// <summary>
    /// How long <c>poke</c> waits for the thread to dispatch. Long enough that a busy thread is
    /// waited for rather than given up on, short enough that a hung one does not stop the run — and
    /// a round where it timed out is counted, because a poke that was never dispatched disturbed
    /// nothing and would otherwise be reported as a quiet round wearing another arm's name.
    /// </summary>
    private const int PokeMs = 200;

    /// <summary>Run the four arms and print what each disturbed.</summary>
    /// <param name="box">The text box under test.</param>
    /// <param name="arrived">The caption the arriving characters are written to.</param>
    /// <param name="packets">The caption the injected code units are written to.</param>
    /// <param name="window">The window under test, for the two arms that do not go through automation.</param>
    /// <param name="rounds">How many rounds each arm types.</param>
    public static void Run(Subject box, Subject arrived, Subject packets, nint window, int rounds)
    {
        Console.WriteLine(
            $"WW342: what a read does to a send, taken apart. {rounds} round(s) on each of"
                + $" {Arms.Length} arms. Every round erases and sends the way the engine does — one"
                + " SendInput carrying the whole string — and then does its arm's thing to the window"
                + $" every {PollMs}ms for {DrainMs}ms while the queue drains. `quiet` does nothing."
                + " `peek` reads the window's rectangle, which USER answers without the window's own"
                + " thread running. `poke` makes that thread dispatch a WM_NULL and reads nothing"
                + " back. `read` is the engine's first look, a UI Automation read of the control."
                + " `substituted` is what the window received differing from what was sent.");

        var faults = new Dictionary<Arm, int>();
        var ran = new Dictionary<Arm, int>();

        foreach (var arm in Arms)
        {
            var measured = Measure(box, arrived, packets, window, rounds, arm);
            faults[arm] = measured.Substituted;
            ran[arm] = measured.Ran;
        }

        Console.WriteLine(Verdict(faults, ran));
    }

    /// <summary>What one arm's rounds came to.</summary>
    /// <param name="Ran">How many rounds produced a reading at both ends.</param>
    /// <param name="Substituted">How many of those arrived differing from what was sent.</param>
    private readonly record struct Measured(int Ran, int Substituted);

    /// <summary>
    /// Run one arm, print its row, and answer what it read.
    /// </summary>
    /// <param name="box">The text box under test.</param>
    /// <param name="arrived">The caption the arriving characters are written to.</param>
    /// <param name="packets">The caption the injected code units are written to.</param>
    /// <param name="window">The window under test.</param>
    /// <param name="rounds">How many rounds to type.</param>
    /// <param name="arm">What to do while the queue drains.</param>
    private static Measured Measure(
        Subject box, Subject arrived, Subject packets, nint window, int rounds, Arm arm)
    {
        var substituted = 0;
        var dirty = 0;
        var unread = 0;
        var ran = 0;
        var undispatched = 0;
        var examples = new List<string>();

        // The focus once and never per round, for the reason WW312's whole sweep takes it once: an
        // act of the engine's own between rounds drains the queue, which is the one thing every arm
        // here is defined by not doing.
        Winwright.Acting.Keyboard.Type(box, "");
        var standing = 0;

        var clock = Stopwatch.StartNew();
        for (var round = 1; round <= rounds; round++)
        {
            var typing = $"WW249-{round}";

            Spaced.Clear(standing);
            standing = typing.Length;
            Spaced.Batch(typing);

            undispatched += Disturb(arm, box, window);

            // Read after the drain and never during it, whatever the arm was. These are captions on
            // the same window, so reading them is itself a cross-process read — and taking it while
            // the queue drains would put the engine's own first look into all four arms and make
            // them one arm measured four times.
            Sweep.Drain(box, typing);

            var got = Sweep.Tail(arrived, typing.Length);
            var sent = Sweep.Tail(packets, typing.Length);

            if (got is null || sent is null)
            {
                unread++;
                continue;
            }

            ran++;
            if (string.Equals(got, typing, StringComparison.Ordinal))
                continue;

            substituted++;
            if (!string.Equals(sent, typing, StringComparison.Ordinal))
                dirty++;

            if (examples.Count < 4)
                examples.Add($"sent {typing}, injected {sent}, arrived {got}");
        }

        clock.Stop();

        var rate = ran == 0 ? 0 : (double)substituted / ran;
        var stalled = undispatched == 0 ? "" : $", {undispatched} poke(s) never dispatched";
        Console.WriteLine(
            $"  {arm.ToString().ToLowerInvariant(),-5}  {substituted,3} substituted of {ran}"
                + $" ({rate:P2}), {dirty} with a dirty injection, {unread} unread{stalled},"
                + $" {clock.Elapsed.TotalSeconds:F0}s");

        foreach (var one in examples)
            Console.WriteLine($"        {one}");

        return new Measured(ran, substituted);
    }

    /// <summary>
    /// Do the arm's thing to the window for the whole drain, and answer how many pokes the thread
    /// never got to.
    /// </summary>
    /// <param name="arm">Which disturbance.</param>
    /// <param name="box">The control, for the arm that reads it.</param>
    /// <param name="window">The window, for the two that do not.</param>
    private static int Disturb(Arm arm, Subject box, nint window)
    {
        if (arm == Arm.Quiet)
        {
            Thread.Sleep(DrainMs);
            return 0;
        }

        var undispatched = 0;
        var until = Stopwatch.StartNew();
        while (until.ElapsedMilliseconds < DrainMs)
        {
            switch (arm)
            {
                case Arm.Peek:
                    _ = GetWindowRect(window, out _);
                    break;

                case Arm.Poke:
                    // Zero is the failure, and a timeout is one of the ways it fails. Counted rather
                    // than ignored: a poke the thread never dispatched left it alone, and a round
                    // built of those is a quiet round filed under this arm's name.
                    if (SendMessageTimeoutW(window, WmNull, 0, 0, SmtoAbortIfHung, PokeMs, out _) == 0)
                        undispatched++;

                    break;

                case Arm.Read:
                    box.ReadOnce();
                    break;

                default:
                    break;
            }

            Thread.Sleep(PollMs);
        }

        return undispatched;
    }

    /// <summary>
    /// What the arms come to, said as which of the two halves the fault needs.
    /// <para>
    /// The control leads and decides whether anything else is readable. An arm set where the engine's
    /// own shape faulted nothing has no rate for the halves to have inherited, and a sentence about
    /// the mechanism written off that is a conclusion about the desk wearing a finding's words.
    /// </para>
    /// </summary>
    /// <param name="faults">How many rounds substituted on each arm.</param>
    /// <param name="ran">How many rounds each arm read at both ends.</param>
    private static string Verdict(IReadOnlyDictionary<Arm, int> faults, IReadOnlyDictionary<Arm, int> ran)
    {
        var quiet = faults.GetValueOrDefault(Arm.Quiet);
        var peek = faults.GetValueOrDefault(Arm.Peek);
        var poke = faults.GetValueOrDefault(Arm.Poke);
        var read = faults.GetValueOrDefault(Arm.Read);

        var counted = string.Join(
            ", ",
            Arms.Select(one => $"{one.ToString().ToLowerInvariant()} {faults.GetValueOrDefault(one)} of {ran.GetValueOrDefault(one)}"));

        if (read == 0)
        {
            return $"The engine's own shape faulted nothing: {counted}. So this run has no rate for"
                + " the halves to have inherited and says nothing about the mechanism — whatever the"
                + " arms did, they did it to a desk that was not producing the fault. Run it longer,"
                + " or on the desk WW312 read it on.";
        }

        if (quiet > 0)
        {
            return $"The control faulted too: {counted}. A round that does nothing while the queue"
                + " drains is the thing every other arm is measured against, so an arm set with a"
                + " dirty control cannot attribute anything. Something else on this desk was"
                + " disturbing the send.";
        }

        if (poke > 0 && peek == 0)
        {
            return $"Making the window's thread pump provokes it and touching the window without"
                + $" waking that thread does not: {counted}. So it is the pumping and not the call —"
                + " which means the fifty milliseconds are about how long the queue needs to drain"
                + " rather than about who is reading, and any reader can be judged by one question:"
                + " does it make that thread run? A cached read is not exempt if it does.";
        }

        if (poke == 0 && read > 0)
        {
            return $"The engine's read provokes it and a bare pump does not: {counted}. So the"
                + " automation call is doing something a dispatched message is not — more work on"
                + " that thread, or work of a different kind — and a cheaper reader is worth"
                + " measuring, because the pause may be paying for the provider rather than for the"
                + " queue.";
        }

        if (peek > 0)
        {
            return $"The arm that leaves the window's thread alone faulted anyway: {counted}. Nothing"
                + " here is doing what this arm assumed — GetWindowRect is answered without the"
                + " owning thread running — so the model behind WW329's pause is wrong somewhere and"
                + " this is the reading that says so.";
        }

        return $"The arms did not separate: {counted}. Read the rows rather than this sentence.";
    }

    /// <summary>The message that says nothing, so a send of it is a dispatch and no more.</summary>
    private const uint WmNull = 0x0000;

    /// <summary>Give up rather than block where the target thread is already hung.</summary>
    private const uint SmtoAbortIfHung = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint window, out Rect rect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SendMessageTimeoutW(
        nint window, uint message, nuint wide, nint far, uint flags, uint timeoutMs, out nuint answer);

    /// <summary>The rectangle <c>peek</c> reads and never looks at.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}

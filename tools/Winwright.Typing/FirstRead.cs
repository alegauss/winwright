using System.Diagnostics;

using Winwright.Acting;
using Winwright.Locating;

namespace Winwright.Typing;

/// <summary>
/// WW329. What the engine's first look costs, and whether waiting before it takes the fault away.
/// <para>
/// WW312 swept the same send quiet and watched — identical rounds, identical wall time, differing
/// only in whether anything read the box while the queue drained. Six hundred quiet rounds faulted
/// nowhere; watched, three of a hundred and fifty, which is the rate the engine has measured on
/// itself all along. So the engine provokes the fault it repairs: <c>SendInput</c> returns once the
/// events are queued rather than processed, and <c>Settled</c> starts polling the instant
/// <c>Send</c> returns, which puts a cross-process read into the window's thread while its packets
/// are still being translated.
/// </para>
/// <para>
/// That makes a repair available which the resend is not, and this is the arm that prices it. Three
/// resends cost a failing send three more of itself and leave the fault at its rate. A pause before
/// the first look costs every send a fixed interval and may leave nothing to repair. Neither number
/// was known: the drain measures 2 to 5ms a character once it starts, and what a first read owes is
/// that pause plus the drain — which nothing had measured, because nothing had waited.
/// </para>
/// <para>
/// The send is the engine's own — one call carrying every code unit — and the erase is in the same
/// act in front of it, which is the shape WW312 found reproduces WW249. Everything after the pause
/// is <c>Settled</c>: the same interval, the same early exit. So the only thing swept is the pause.
/// </para>
/// <para>
/// What this cannot say is <em>why</em> the read provokes it — the cross-process call, or the
/// pumping it forces on the window's thread. Anything else pumping that thread would do as well.
/// Delaying the look repairs it either way, and changing how the look is taken would need to know
/// which, so that question stays open behind whatever this measures.
/// </para>
/// </summary>
internal static class FirstRead
{
    /// <summary>
    /// The pauses, and three of them rather than five.
    /// <para>
    /// The fault runs near 1% on this guest — WW312 read 1 in 150 on this exact arm — so what
    /// decides this measurement is rounds per cell and not cells. Three cells at a thousand rounds
    /// says more than five at six hundred, and the two ends are what the question is: nothing waited,
    /// against a wait comfortably past a nine-character drain.
    /// </para>
    /// <para>
    /// 50ms is the middle and it is the one worth having: a repair that works at 150 and costs 150ms
    /// on every keystroke this engine sends is not obviously better than three resends on the 1% that
    /// fail, and a repair that works at 50 is.
    /// </para>
    /// </summary>
    private static readonly int[] Pauses = [0, 50, 150];

    /// <summary>
    /// Run the arm and print what each pause did.
    /// </summary>
    /// <param name="box">The text box under test.</param>
    /// <param name="arrived">The caption the arriving characters are written to.</param>
    /// <param name="packets">The caption the injected code units are written to.</param>
    /// <param name="rounds">How many rounds each pause types.</param>
    public static void Run(Subject box, Subject arrived, Subject packets, int rounds)
    {
        Console.WriteLine(
            $"WW329: the pause before the first look, {rounds} round(s) at each of"
                + $" {string.Join("ms, ", Pauses)}ms. Every round erases and sends the way the engine does"
                + " — one SendInput carrying the whole string, straight after the backspaces — then waits"
                + " that long before reading the box at all, and polls on the engine's own interval"
                + " after that. `substituted` is what the window received differing from what was sent."
                + " The milliseconds beside it are what the pause costs a round, which is the half of"
                + " this that decides anything.");

        var faults = new Dictionary<int, int>();
        var costs = new Dictionary<int, double>();

        foreach (var pause in Pauses)
        {
            var substituted = 0;
            var dirty = 0;
            var unread = 0;
            var examples = new List<string>();

            // The focus once, for the reason WW312's whole arm takes it once: an act of the engine's
            // own between rounds drains the queue, which is the thing this arm exists not to do.
            Keyboard.Type(box, "");
            var standing = 0;

            var clock = Stopwatch.StartNew();
            for (var round = 1; round <= rounds; round++)
            {
                var typing = $"WW249-{round}";

                Spaced.Clear(standing);
                standing = typing.Length;
                Spaced.Batch(typing);

                if (pause > 0)
                    Thread.Sleep(pause);

                Sweep.Drain(box, typing);

                var got = Sweep.Tail(arrived, typing.Length);
                var sent = Sweep.Tail(packets, typing.Length);

                if (got is null || sent is null)
                {
                    unread++;
                    continue;
                }

                if (string.Equals(got, typing, StringComparison.Ordinal))
                    continue;

                substituted++;
                if (!string.Equals(sent, typing, StringComparison.Ordinal))
                    dirty++;

                if (examples.Count < 4)
                    examples.Add($"sent {typing}, injected {sent}, arrived {got}");
            }

            clock.Stop();

            faults[pause] = substituted;
            costs[pause] = rounds == 0 ? 0 : clock.Elapsed.TotalMilliseconds / rounds;

            var rate = rounds == 0 ? 0 : (double)substituted / rounds;
            Console.WriteLine(
                $"  {pause,3}ms  {substituted,3} substituted of {rounds} ({rate:P2}),"
                    + $" {dirty} with a dirty injection, {unread} unread,"
                    + $" {costs[pause]:F0}ms a round, {clock.Elapsed.TotalSeconds:F0}s");

            foreach (var one in examples)
                Console.WriteLine($"        {one}");
        }

        Console.WriteLine(Verdict(faults, costs, rounds));
    }

    /// <summary>
    /// What the arm says, which turns on whether it saw the fault at all before it turns on
    /// anything else.
    /// <para>
    /// Three outcomes, for the reason WW312's verdict has three: an arm that never faulted has no
    /// rate for a pause to have taken away, and a sentence about the pause written off an empty set
    /// is a conclusion about the desk wearing the words of a finding.
    /// </para>
    /// </summary>
    /// <param name="faults">How many rounds substituted at each pause.</param>
    /// <param name="costs">What a round cost at each pause, in milliseconds.</param>
    /// <param name="rounds">How many rounds each pause ran.</param>
    private static string Verdict(
        IReadOnlyDictionary<int, int> faults, IReadOnlyDictionary<int, double> costs, int rounds)
    {
        var control = faults.GetValueOrDefault(0);
        if (control == 0)
        {
            return $"Nothing faulted with no pause at all, so this run has no rate to take away:"
                + $" {rounds} rounds of the shape WW312 measured the fault in produced none of it."
                + " Whatever the pauses did, they did it to nothing.";
        }

        var waited = faults.Where(one => one.Key > 0).ToList();
        var quiet = waited.Where(one => one.Value == 0).Select(one => one.Key).ToList();

        // What the pause costs against what the resends cost, said as the two numbers rather than as
        // a recommendation: three resends are paid by the faulting sends alone and a pause is paid by
        // every send, so the comparison is a rate against an interval and the choice is the engine's.
        var priced = string.Join(
            ", ",
            costs.Where(one => one.Key > 0)
                .Select(one => $"{one.Key}ms adds {one.Value - costs.GetValueOrDefault(0):F0}ms a round"));

        if (quiet.Count == 0)
        {
            return $"The fault survives every pause: {string.Join(", ", faults.Select(one => $"{one.Key}ms: {one.Value}"))}"
                + $" of {rounds} each. So the first look is not the whole of what provokes it, and"
                + " waiting is not the repair the resend is being compared with. " + priced + ".";
        }

        return $"The fault is {control} of {rounds} with no pause and none at {string.Join("ms, ", quiet)}ms."
            + " That is the repair WW329 is about, and what it costs is the other half: " + priced
            + ". Three resends are paid only by the sends that fault and leave the rate where it is;"
            + " a pause is paid by every send this engine ever makes.";
    }
}

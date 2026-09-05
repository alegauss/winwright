using System.Diagnostics;
using System.Globalization;
using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Locating;

namespace Winwright.Typing;

/// <summary>
/// WW341. Whether the other synthesised acts are read too early, and the observable that can say so.
/// <para>
/// WW329 measured one act. <c>SendInput</c> returns once the events are queued rather than
/// processed, and typing's <c>Settled</c> polled from the instant <c>Send</c> returned — which put a
/// cross-process read into the window's thread while its packets were still being translated. Four
/// more verbs synthesise input and each reads back the moment the send returns: click, press, nudge
/// and the two picker walks.
/// </para>
/// <para>
/// The reason nobody had counted them is that a typed string arrives wrong in a way a case can see,
/// character for character, and an act that lands late does not. A click read too early reports the
/// value from before it, which is the same reading as a click that never landed — so the rate has
/// been invisible rather than absent. The observable is the whole task, and it is one comparison:
/// the reading the engine's own act produced, against a reading taken afterwards with time to
/// settle. Late is the engine's reading stale and the settled one moved. Lost is both stale.
/// </para>
/// <para>
/// What that separation shows is not the same for all four, and the difference is the finding.
/// <c>Traversal.Press</c> and <c>Traversal.Nudge</c> poll until the reading moves, so a look taken
/// too early costs a poll and the answer is right anyway — their late rate is what the poll is
/// paying for. <c>Pointer.Run</c> polled nothing and read on the instant the send returned, so its
/// late rate was a rate of wrong answers, and this is the first thing to count it. WW353 gave it the
/// pause WW329 measured — it still polls nothing, because a click has no named thing to poll toward,
/// so this arm stays the one that would price a desk slow enough to need more.
/// </para>
/// <para>
/// The picker walks are the same shape as press — <c>Keys.Send</c> then <c>Attempt.UntilTrue</c> —
/// and are not driven here. Reading them needs a picker and a menu open on the fixture, and the
/// three below already carry both shapes: the one that polls and the one that does not.
/// </para>
/// <para>
/// What it read, on the guest on 2026-09-02: two runs of 300 rounds each at all three verbs, 1800
/// rounds in all, with nothing late and nothing lost. That bounds the rate under about 1% and the
/// rate WW329 took off typing was 2.58%, so the provocation does not reach these three at anything
/// like the size it reached the send. The click's shape was not thereby acquitted, and WW353 took
/// the half of it that could be taken: 150 rounds each way on the guest read 382ms a click round
/// without the pause and 455ms with, of which about 36 is the guest — press and nudge moved by that
/// much between the same two runs without being touched.
/// </para>
/// </summary>
internal static class Landing
{
    /// <summary>
    /// How long a settled reading is allowed to wait for the act to arrive.
    /// <para>
    /// Generous on purpose, and it decides what "lost" means. Anything shorter makes a slow arrival
    /// indistinguishable from an act that never landed, which is the confusion this arm exists to
    /// take apart — so the deadline is well past anything a desk under no load would need, and a
    /// round that spends it is reported as lost rather than quietly counted as late.
    /// </para>
    /// </summary>
    private const int SettleMs = 3000;

    /// <summary>How often the settled reading looks.</summary>
    private const int PollMs = 20;

    /// <summary>How many faulted rounds have their two readings printed, per verb.</summary>
    private const int MostExamples = 6;

    /// <summary>
    /// The rate the same provocation ran at in the one act that had been measured: 2.58% of sends,
    /// read on this guest over 1200 rounds before WW329's pause. It is what turns a clean run here
    /// into a bound rather than a shrug — this many rounds could not have missed a rate like that.
    /// </summary>
    private const double Typed = 0.0258;

    /// <summary>
    /// Run the arm and print what each verb's own reading was worth.
    /// </summary>
    /// <param name="run">The fixture this arm measures, and how many rounds it was asked for.</param>
    public static void Run(TypingRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var (root, rounds) = (run.Root, run.Rounds);

        Console.WriteLine(
            $"WW341: what each synthesised act's own reading was worth, {rounds} round(s) each."
                + " Every round reads the control, acts, keeps the reading the engine's own act"
                + " produced, and then reads again with up to "
                + SettleMs.ToString(CultureInfo.InvariantCulture)
                + "ms to settle. `late` is the engine's reading stale where the settled one moved —"
                + " the act landed and was read too early. `lost` is both stale, which is an act that"
                + " did not arrive at all. `clean` is the engine's own reading already right.");

        var readings = new List<Reading>
        {
            Clicking(root, rounds),
            Pressing(root, rounds),
            Nudging(root, rounds),
        };

        foreach (var reading in readings)
        {
            Console.WriteLine($"  {reading}");
            foreach (var one in reading.Examples)
                Console.WriteLine($"        {one}");
        }

        Console.WriteLine(Verdict(readings));
    }

    /// <summary>
    /// The click, which is the one with nothing behind it. <c>Pointer.Run</c> sends and then reads
    /// once, so its late rate is not a cost — it is the rate at which a click reports the state from
    /// before itself.
    /// </summary>
    /// <param name="root">The fixture's window.</param>
    /// <param name="rounds">How many rounds to run.</param>
    private static Reading Clicking(AutomationElement root, int rounds)
    {
        var box = On(root, "CheckBox#verbose");
        var reading = new Reading("click", "Pointer.Run pauses before its look and polls nothing");

        for (var round = 1; round <= rounds; round++)
        {
            var before = box.ReadOnce().Values.Toggle;
            if (before is null)
            {
                reading.Unread();
                continue;
            }

            var clicked = Pointer.Click(box, PointerReason.PointerIsTheAct);
            if (!clicked.Landed)
            {
                reading.Unread();
                continue;
            }

            var said = clicked.After.Toggle;
            var settled = Settled(() => box.ReadOnce().Values.Toggle, before);

            reading.Saw(round, before, said, settled);
        }

        return reading;
    }

    /// <summary>
    /// The traversal key, which polls. What its late rate says is what the poll is buying: a first
    /// look this often comes back before the focus has moved, and the verb pays a poll for it rather
    /// than answering wrong.
    /// </summary>
    /// <param name="root">The fixture's window.</param>
    /// <param name="rounds">How many rounds to run.</param>
    private static Reading Pressing(AutomationElement root, int rounds)
    {
        var reading = new Reading("press", "Traversal.Press polls until the focus moves");

        for (var round = 1; round <= rounds; round++)
        {
            var before = Focused();
            if (before is null)
            {
                reading.Unread();
                continue;
            }

            var pressed = Traversal.Press(root, TraversalKey.Tab, settleMs: SettleMs, pollMs: PollMs);
            if (!pressed.Sent)
            {
                reading.Unread();
                continue;
            }

            // The verb's own answer is whether it saw the focus move within its poll, so a reading
            // that stayed put is what "stale" means here. The settled reading is taken the same way
            // as the other two: the poll may already have paid for it, which is the point.
            var said = pressed.Moved && pressed.After is { } after
                ? $"{after.AutomationId}/{after.Name}"
                : before;
            var settled = Settled(Focused, before);

            reading.Saw(round, before, said, settled);
        }

        return reading;
    }

    /// <summary>
    /// The nudge, which polls like press and reads a number rather than a name. Present because it
    /// is a third arrival and the cheapest one to be wrong about: a range that moves by one step is
    /// the smallest change a reading can miss.
    /// </summary>
    /// <param name="root">The fixture's window.</param>
    /// <param name="rounds">How many rounds to run.</param>
    private static Reading Nudging(AutomationElement root, int rounds)
    {
        var reading = new Reading("nudge", "Traversal.Nudge polls until the range moves");

        // The pane the launch asked for, picked before anything under it is looked for: WPF builds a
        // tab's content when it is selected, so a slider on an unpicked tab is in no tree at all. A
        // pattern act, because it needs no foreground and this arm has not taken one yet.
        if (!Act.Select(On(root, "TabItem#rangesPane")).Attempted)
            return reading;

        var slider = On(root, "Slider#roomEitherWay");

        for (var round = 1; round <= rounds; round++)
        {
            var before = slider.ReadOnce().Values.Range;
            if (before is null)
            {
                reading.Unread();
                continue;
            }

            var nudged = Traversal.Nudge(slider);
            if (!nudged.Sent)
            {
                reading.Unread();
                continue;
            }

            var was = Spelled(before);
            var said = Spelled(nudged.After);
            var settled = Settled(() => Spelled(slider.ReadOnce().Values.Range), was);

            reading.Saw(round, was, said, settled);
        }

        return reading;
    }

    /// <summary>
    /// A reading taken with time to settle: poll until it differs from what was there before the
    /// act, and answer whatever is there when the deadline runs out.
    /// <para>
    /// The deadline running out is not an error here. It is the reading that says the act never
    /// arrived, which is the half of the separation that could not be made before — and answering it
    /// as the last value read is what lets a lost round be told from a late one at all.
    /// </para>
    /// </summary>
    /// <param name="read">How to take the reading.</param>
    /// <param name="before">What was there before the act.</param>
    private static string? Settled(Func<string?> read, string? before)
    {
        Attempt.UntilTrue(
            () => read() is { } now && !string.Equals(now, before, StringComparison.Ordinal),
            SettleMs,
            PollMs);

        return read();
    }

    /// <summary>A range as a reading, so all three verbs are compared the same way.</summary>
    /// <param name="value">The range value, or null where nothing answered.</param>
    private static string? Spelled(double? value) =>
        value?.ToString("R", CultureInfo.InvariantCulture);

    /// <summary>What holds the focus, by the name a reading of it carries.</summary>
    private static string? Focused()
    {
        try
        {
            return AutomationElement.FocusedElement is { } now
                ? $"{now.Current.AutomationId}/{now.Current.Name}"
                : null;
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
    }

    /// <summary>
    /// What the arm says, which turns on the click before it turns on anything else.
    /// <para>
    /// The two shapes answer different questions and a verdict that averaged them would say neither.
    /// A polled verb's late rate is a price it already pays; the click's is a rate of wrong answers,
    /// and that is the only number here that is a defect rather than a measurement.
    /// </para>
    /// </summary>
    /// <param name="readings">What each verb read.</param>
    private static string Verdict(IReadOnlyList<Reading> readings)
    {
        var clicking = readings.First(one => one.Verb == "click");
        var polling = readings.Where(one => one.Verb != "click").ToList();

        var polled = string.Join(
            ", ",
            polling.Select(one => $"{one.Verb} {one.Late} late of {one.Ran}"));

        if (clicking.Ran == 0)
            return "The click never ran, so this arm measured nothing it exists to measure.";

        if (clicking.Late == 0 && clicking.Lost == 0)
        {
            // What a clean run rules out, rather than a clean run reported as a shrug. Nothing seen
            // in n rounds puts the rate under about 3/n with 95% confidence, and the number worth
            // comparing it against is typing's own: 2.58% before WW329's pause, which is a rate this
            // many rounds could not have missed.
            var bound = clicking.Ran == 0 ? 1 : 3.0 / clicking.Ran;
            var missed = clicking.Ran * Typed;

            return $"The click read right every one of {clicking.Ran} rounds, and so did the polled"
                + $" verbs: {polled}. What that rules out is the size of it — nothing in"
                + $" {clicking.Ran} rounds puts the rate under about {bound:P1}, and the rate WW329"
                + $" took off typing was {Typed:P2}, which over this many rounds would have been"
                + $" about {missed:F0} of them. So whatever the first look does to a send, it does"
                + " not reach these three at anything like the rate it reached typing.\nWW353 gave"
                + " the click the pause WW329 measured, so it no longer reads on the instant the"
                + " send returns — it still polls nothing, because a click has no named thing to"
                + " poll toward, and this arm is what would notice if that mattered on a desk"
                + " slower than the one it just ran on.";
        }

        var rate = (double)clicking.Late / clicking.Ran;
        return $"The click reported the state from before itself {clicking.Late} times in"
            + $" {clicking.Ran} ({rate:P1}), and lost {clicking.Lost}. Nothing repairs those: the"
            + " verb sends, reads once and returns that reading, so a late arrival is a wrong answer"
            + $" and not a retry. The polled verbs paid {polled} for the same provocation, which is"
            + " the difference a poll makes and the argument for giving the click one.";
    }

    private static Subject On(AutomationElement root, string locator) =>
        Subject.Unguarded(root, Locator.Parse(locator), deadlineMs: 8000, pollMs: 25);

    /// <summary>
    /// What one verb's rounds came to, and the rounds worth a person's eyes.
    /// <para>
    /// Kept per verb rather than totalled, because the two act shapes here are not comparable: a
    /// late reading costs a polled verb a poll and costs the click its answer.
    /// </para>
    /// </summary>
    private sealed class Reading(string verb, string because)
    {
        private readonly List<string> examples = [];
        private readonly Stopwatch clock = Stopwatch.StartNew();

        /// <summary>The verb, as the vocabulary spells it.</summary>
        public string Verb { get; } = verb;

        /// <summary>How many rounds produced a reading at all.</summary>
        public int Ran { get; private set; }

        /// <summary>How many of them the engine's own reading was already right about.</summary>
        public int Clean { get; private set; }

        /// <summary>How many read stale where the act had in fact landed.</summary>
        public int Late { get; private set; }

        /// <summary>How many never arrived, settled reading and all.</summary>
        public int Lost { get; private set; }

        /// <summary>How many rounds could not be read at either end.</summary>
        public int Unreadable { get; private set; }

        /// <summary>The faulted rounds, both readings each.</summary>
        public IReadOnlyList<string> Examples => examples;

        /// <summary>Record a round nothing could be read in.</summary>
        public void Unread() => Unreadable++;

        /// <summary>Record one round's two readings.</summary>
        /// <param name="round">Which round, for the examples.</param>
        /// <param name="before">What was there before the act.</param>
        /// <param name="said">What the engine's own act reported.</param>
        /// <param name="settled">What was there once it had time to arrive.</param>
        public void Saw(int round, string? before, string? said, string? settled)
        {
            Ran++;

            var moved = !string.Equals(settled, before, StringComparison.Ordinal);
            var read = !string.Equals(said, before, StringComparison.Ordinal);

            if (!moved)
            {
                Lost++;
            }
            else if (read)
            {
                Clean++;
                return;
            }
            else
            {
                Late++;
            }

            if (examples.Count < MostExamples)
                examples.Add($"round {round}: was {Or(before)}, act said {Or(said)}, settled {Or(settled)}");
        }

        /// <summary>A reading, or the words for one that was never taken.</summary>
        private static string Or(string? reading) => reading ?? "<nothing>";

        /// <summary>The verb's line in the report.</summary>
        public override string ToString()
        {
            var rate = Ran == 0 ? "" : $" ({(double)Late / Ran:P1} late)";
            var took = Ran == 0 ? "" : $", {clock.Elapsed.TotalMilliseconds / Ran:F0}ms a round";
            var unread = Unreadable == 0 ? "" : $", {Unreadable} unreadable";

            return $"{Verb,-6} {Clean,4} clean {Late,4} late {Lost,4} lost of {Ran}{rate}{took}"
                + $"{unread} — {because}";
        }
    }
}

using System.Diagnostics;
using System.Windows.Automation;

using Winwright.Locating;

namespace Winwright.Typing;

/// <summary>
/// WW368. What the arm does not carry over to the act, one difference at a time.
/// <para>
/// WW355 measured four cheap readers against a draining queue and every one read zero over eight
/// hundred rounds. The engine then took the last of them — one <c>ValuePattern</c> round-trip on
/// an element resolved before the send — and read 1 of 1200 with the pause taken out, where the
/// pause reads 0. Thirty-one times better than WW329's 2.58% and not the zero the arm predicted,
/// so the arm is not the act and nothing said where they part.
/// </para>
/// <para>
/// This is that list, walked. Each rung is the one above it plus exactly one thing the real act
/// does, so the rung where a rate appears is the difference that carries it. The read is the same
/// on all four — the value the engine now asks for — because a ladder that changed the reader as
/// well would be measuring two things per step.
/// </para>
/// <para>
/// One candidate was struck off by reading rather than by running. WW368 was opened suspecting
/// <c>Admitted.Do</c>, which the act reads through and the arm reaches past; it is a method that
/// invokes its argument against a field and adds nothing at all, so the two make the identical
/// call and there is no rung for it here.
/// </para>
/// <para>
/// What the rungs cannot do is resolve a rate of one in twelve hundred out of a few hundred
/// rounds: at 400 an arm the expected count is a third of a fault, so a row of zeros would say
/// nothing. The count is the argument, and 1200 is the one WW355 measured the act at.
/// </para>
/// <para>
/// What it read, on the guest on 2026-09-04, 1200 rounds a rung: arm 0, focus 1, split 0, settle
/// 1. The control reproduced WW355's zero, and <c>settle</c> — the engine's own round with the
/// pause at zero — reproduced the act's 1 in 1200 exactly. So the act's residual rate is now
/// reachable outside the engine, which is the thing this ladder was built for.
/// </para>
/// <para>
/// It enters at <c>focus</c>, and the rungs above it inherit it: three rungs that take the focus
/// every round read 2 of 3600, against 0 of 1200 here and 0 of 3200 across WW355's four readers.
/// <c>split</c> reading 0 does not acquit the send shape so much as show the resolution — one
/// fault in 1200 has an expectation of one, and 0 and 1 are the same measurement at this size.
/// What separates cleanly is the rung below them all.
/// </para>
/// <para>
/// Which moves the suspect to the other side of the send. WW342 acquitted the pumping and WW355
/// acquitted every cheap read <em>during</em> the drain; this says the call that provokes is the
/// one <em>before</em> the keys go in — <c>SetFocus</c> is a provider round-trip on the element,
/// issued on the line above the send, and the fifty milliseconds this engine pays are spent after
/// it. A pause that guarded the focus rather than the read would be a different repair, and this
/// is the reading that makes it a candidate rather than a guess.
/// </para>
/// <para>
/// The rungs also priced themselves: 396, 400 and 401ms a round for the three that drain for a
/// fixed 300ms, and 144ms for the one that stops on a match. The last is the act's own round, and
/// it agrees with the 91-95ms WW355 measured with the walk taken out.
/// </para>
/// <para>
/// WW381 puts the candidate on the same ladder. If what provokes is the focus, the fifty
/// milliseconds this engine pays are on the wrong side of the send, and the repair is to move them
/// rather than to buy more: <c>guard</c> is <c>settle</c> with that interval spent between the
/// focus and the keys instead of after them. It is the one rung that is not a difference the real
/// act has — every other adds something the act does, and this takes the act somewhere it has
/// never been — and it is here because the two placements cost the same and only a reading can say
/// which one buys anything.
/// </para>
/// <para>
/// Which makes the pair, and not the rung, the thing to read. <c>settle</c> is the act with the
/// pause at zero and <c>guard</c> is the same round with it above the send, so what separates them
/// is where the interval is spent — and they are compared against each other in the run they were
/// both measured in, because this fault's rate is a property of the desk on the evening and not a
/// number to carry across from WW368's row.
/// </para>
/// <para>
/// It is the count that decides whether that pair says anything, and the ladder cannot help with
/// it: <c>settle</c> reads about 1 in 1200, so 1200 rounds expects one substitution and a
/// <c>guard</c> that reads zero beside it has separated nothing. <see cref="Verdict"/> works the
/// expectation out from what <c>settle</c> read in that same run and says which of the two answers
/// the run earned, rather than reading a zero as a repair.
/// </para>
/// <para>
/// What the rung read, on the guest on 2026-09-05, 1200 rounds a rung twice: nothing, and the tool
/// said so both times. The control faulted on each run — <c>arm</c> 1 of 1200, where WW355 read 0
/// of 3200 and WW368 read 0 of 1200 — so the guard clause in front of every verdict refused to
/// attribute anything. It was a desk faulting everywhere at once: 1, 0, 2, 0, 3 up the ladder on
/// the first run and 1, 2, 0, 4, 1 on the second, with the control, the act and the repair all
/// inside the same handful. A row that moves like that is the evening and not the rungs.
/// </para>
/// <para>
/// So the pair is still owed a reading, and what the two runs did settle is what the rung costs:
/// 261 and 271ms a round against <c>settle</c>'s 145 and 231 on those same runs. The 116 and the
/// 40 do not agree with each other either, which is the same evening moving under both rather than
/// a price for the placement — and the first thing a quiet guest would measure properly.
/// </para>
/// <para>
/// WW397 asked whether the guest was the evening, and it is not. A third run on 2026-09-06, after
/// the VM had been powered off and cold-booted, read <b>arm 2 of 1200</b> with focus, split and
/// settle at zero and guard at 1. Pooled with the two before it the control is <b>4 of 3600</b>,
/// against WW355's 0 of 3200 and WW368's 0 of 1200 — so this machine's floor rose from under 1 in
/// 3200 to about 1 in 900 between 2026-09-04 and 2026-09-05, and a reboot did not put it back.
/// </para>
/// <para>
/// What that costs is every reading this ladder can take at these counts. The control is inside the
/// same handful as the rungs above it, so nothing separates, and the refusal below is right to
/// attribute nothing. What it is not is transient: three runs across two days and a power cycle
/// have said the same thing, so <em>run it again</em> is advice that has already been taken.
/// </para>
/// </summary>
internal static class Transfer
{
    /// <summary>
    /// One step of the walk from the arm to the act, and then the one step past it. WW381's rung is
    /// declared here with the rest because it is a shape of the same round; what keeps it out of the
    /// walk is <see cref="Walk"/> and not this list.
    /// </summary>
    private enum Rung
    {
        /// <summary>
        /// What <c>provoke</c>'s <c>value</c> arm does, and the control: the focus taken once before
        /// every round, End and the backspaces in one call, the text in another, and the value read
        /// every poll for a fixed drain however the box reads. WW355 read this zero.
        /// </summary>
        Arm,

        /// <summary>
        /// Plus the focus taken before every round, which is what the act does — it admits the
        /// element and calls <c>SetFocus</c> on the way into every send, where every arm this
        /// project has run took it once and then typed hundreds of rounds.
        /// </summary>
        Focus,

        /// <summary>
        /// Plus the send split the way the act splits it. <c>MoveToTheEnd</c> and <c>Erase</c> are
        /// two <c>SendInput</c> calls a line apart in the engine, so a real round puts three arrays
        /// into the queue where every arm here has put two.
        /// </summary>
        Split,

        /// <summary>
        /// Plus the act's own reading shape: no fixed drain, but a poll that stops the moment the
        /// box reads what was sent. It is the difference that most wants explaining — the act reads
        /// fewer times than the arm and faults more — and by this rung the round is the engine's
        /// with the pause at zero.
        /// </summary>
        Settle,

        /// <summary>
        /// Plus the pause, taken after the focus rather than after the send. WW381, and the one rung
        /// that walks away from the act instead of towards it: the engine spends this interval below
        /// the keys and WW368 put the provoking call above them, so this is the third placement and
        /// the only one nobody has measured. It is last because it is <c>settle</c> repaired rather
        /// than a step that gets nearer the act, and it inherits every difference below it so the
        /// two of them differ in the placement and in nothing else.
        /// </summary>
        Guard,
    }

    /// <summary>
    /// The rung the walk starts from, which is the one a rate cannot enter at. WW368: it is
    /// WW355's own reading, and a run where it faults has no clean floor for the rungs above it to
    /// have departed from — so it is the control rather than a step, and the verdict says so.
    /// </summary>
    private const Rung Control = Rung.Arm;

    /// <summary>
    /// Each step of the walk and what it adds, paired. WW394.
    /// <para>
    /// The sentence used to be a switch in <see cref="Verdict" /> over two rungs and a default, and
    /// the default happened to name the third. It was right by arithmetic: the search runs over the
    /// walk, and <c>settle</c> was the only rung the default could reach. A rung joining the walk
    /// tomorrow would have arrived through it and been reported under its own name carrying
    /// <c>settle</c>'s sentence — the row above saying one thing and the verdict below another, and
    /// the verdict is the line a person reads.
    /// </para>
    /// <para>
    /// Paired rather than checked, which is the stronger of the two repairs available: the walk is
    /// derived from this list, so a rung joins it by having a sentence and there is no way to add
    /// one without writing what it adds. WW354's shape, one file over.
    /// </para>
    /// </summary>
    private static readonly (Rung Rung, string Adds)[] Climbing =
    [
        (Rung.Focus, "taking the focus before every round"),
        (Rung.Split, "sending End in a call of its own, so the queue sees three arrays and not two"),
        (Rung.Settle, "stopping the read the moment the box says what was sent, which is the act's own shape"),
    ];

    /// <summary>
    /// The walk from the arm to the act, which is what a rate appearing on a rung attributes. WW368,
    /// and derived from <see cref="Climbing" /> since WW394 so the two cannot disagree.
    /// </summary>
    private static readonly Rung[] Walk = [Control, .. Climbing.Select(one => one.Rung)];

    /// <summary>
    /// The rungs, in the order they are climbed: the walk, and then the one that departs from it.
    /// <para>
    /// Written off <see cref="Walk"/> rather than beside it, because the two lists are one fact. The
    /// verdict searches the walk for where a rate enters, and WW381's rung inside that search would
    /// be a repair reported as a difference the act already has.
    /// </para>
    /// </summary>
    private static readonly Rung[] Rungs = [.. Walk, Rung.Guard];

    /// <summary>
    /// How long the three fixed-drain rungs read for, which is WW312's number and Disturbance's.
    /// The last rung takes it out, because taking it out is what that rung is.
    /// </summary>
    private const int DrainMs = 300;

    /// <summary>How often the value is read, which is the engine's own poll interval.</summary>
    private const int PollMs = 25;

    /// <summary>How long the settling rung waits before giving up on a reading that never arrives.</summary>
    private const int SettleMs = 2000;

    /// <summary>
    /// How long the guarding rung waits between the focus and the keys. WW381, and it is
    /// <c>Keys.FirstLookMs</c> — the interval the engine already spends on the other side of the
    /// send.
    /// <para>
    /// Spelled here for the reason <c>Spaced</c>'s interop is spelled here: the engine's copy is
    /// internal to it. The same number and not a swept one, because what this rung varies is where
    /// the interval is spent and a second difference in the length would make the pair unreadable —
    /// the question is the placement, and the price of answering it has to be one the engine is
    /// already paying.
    /// </para>
    /// </summary>
    private const int GuardMs = 50;

    /// <summary>
    /// How many substitutions a rung has to expect before a zero on it means anything. WW381.
    /// <para>
    /// The bare run's number, for its reason: below about five expected events a clean rung is
    /// something a quiet evening produces, so a verdict read off one would call every short run a
    /// repair. It is the count that decides this, and the ladder cannot make the fault likelier —
    /// so the honest answer to a run too small is the number of rounds that would not be.
    /// </para>
    /// </summary>
    private const double Resolvable = 5;

    /// <summary>
    /// The automation id of the box every rung types into, spelled here for the reason Disturbance
    /// spells it: the element is resolved without the engine so the read under test is one pattern
    /// round-trip and not a walk.
    /// </summary>
    private const string BoxId = "profile";

    /// <summary>Climb the rungs and print what each one read.</summary>
    /// <param name="run">The fixture this arm measures, and how many rounds it was asked for.</param>
    public static void Run(TypingRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var (box, arrived, packets, window, rounds) =
            (run.Box, run.Arrived, run.Injected, run.Window, run.Rounds);

        Console.WriteLine(
            $"WW368: what does not carry from the arm to the act, {rounds} round(s) on each of"
                + $" {Rungs.Length} rungs. Every rung reads the same value the engine now reads — one"
                + " ValuePattern round-trip on an element resolved before the send — and each of the"
                + $" first {Walk.Length} adds exactly one thing the real act does. `arm` is WW355's"
                + " reading, which read zero:"
                + " the focus taken once, End and the backspaces in one SendInput, the text in"
                + " another, and the value read every poll for a fixed 300ms. `focus` takes the focus"
                + " before every round the way the act does. `split` sends End in a call of its own,"
                + " so a round is three arrays into the queue rather than two, which is the engine's"
                + " shape. `settle` stops reading the moment the box says what was sent instead of"
                + " draining for a fixed time, which makes the round the act's with the pause at"
                + " zero. `guard` is WW381's and the only rung that is not something the act does:"
                + $" it is `settle` with {GuardMs}ms spent between the focus and the keys instead of"
                + " after them, which is the engine's own interval on the other side of the send."
                + " `substituted` is what the window received differing from what was sent.");

        // Resolved once and before the rungs, for WW355's reason: the walk is most of what the
        // engine's old look asked the provider for, and a rung that re-resolved every poll would be
        // measuring the walk again under a name that says it does not.
        var element = AutomationElement.FromHandle(window).FindFirst(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.AutomationIdProperty, BoxId));

        if (element is null)
        {
            Console.Error.WriteLine(
                $"no control with the automation id '{BoxId}' is under that window, so every rung"
                    + " would read nothing and report it as clean");
            return;
        }

        var read = new Dictionary<Rung, Measured>();
        foreach (var rung in Rungs)
            read[rung] = Measure(box, arrived, packets, rounds, rung, element);

        Console.WriteLine(Enough.Concluded(rounds, () => Verdict(read)));
    }

    /// <summary>What one rung's rounds came to.</summary>
    /// <param name="Ran">How many rounds produced a reading at both ends.</param>
    /// <param name="Substituted">How many of those arrived differing from what was sent.</param>
    private readonly record struct Measured(int Ran, int Substituted);

    /// <summary>Run one rung, print its row, and answer what it read.</summary>
    /// <param name="box">The text box under test.</param>
    /// <param name="arrived">The caption the arriving characters are written to.</param>
    /// <param name="packets">The caption the injected code units are written to.</param>
    /// <param name="rounds">How many rounds to type.</param>
    /// <param name="rung">Which step of the walk.</param>
    /// <param name="element">The box, resolved before the rounds.</param>
    private static Measured Measure(
        Subject box, Subject arrived, Subject packets, int rounds, Rung rung, AutomationElement element)
    {
        var substituted = 0;
        var dirty = 0;
        var unread = 0;
        var ran = 0;
        var examples = new List<string>();

        // The focus once before the rounds on every rung, because the control rung is defined by
        // taking it only here — the rungs above add a second one inside the round rather than
        // moving this one.
        Winwright.Acting.Keyboard.Type(box, "");
        var standing = 0;

        var clock = Stopwatch.StartNew();
        for (var round = 1; round <= rounds; round++)
        {
            var typing = $"WW249-{round}";

            Typed(rung, element, standing, typing);
            standing = typing.Length;

            // Read after the rung's own reading and never during it, for Disturbance's reason:
            // these are captions on the same window, so reading them is itself a cross-process read
            // and taking it while the queue drains would put one more reader into every rung.
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
        Console.WriteLine(
            $"  {rung.ToString().ToLowerInvariant(),-6}  {substituted,3} substituted of {ran}"
                + $" ({rate:P2}), {dirty} with a dirty injection, {unread} unread,"
                + $" {clock.Elapsed.TotalSeconds:F0}s, {clock.Elapsed.TotalMilliseconds / Math.Max(rounds, 1):F0}ms a round");

        foreach (var one in examples)
            Console.WriteLine($"        {one}");

        return new Measured(ran, substituted);
    }

    /// <summary>
    /// One round of the rung's own shape: what it does before the send, the send itself, and how it
    /// reads afterwards. WW368, and it is one method because a rung <em>is</em> these three answers
    /// — reading them apart in the loop is what made the loop the thing to read instead of the row.
    /// </summary>
    /// <param name="rung">Which step of the walk.</param>
    /// <param name="element">The box, resolved before the rounds.</param>
    /// <param name="standing">How many characters the box is holding.</param>
    /// <param name="typing">What this round sends.</param>
    private static void Typed(Rung rung, AutomationElement element, int standing, string typing)
    {
        if (rung >= Rung.Focus)
            Focused(element);

        // WW381. Between the focus and the keys, which is the one placement this ladder has never
        // tried: the engine spends the same interval below the send, and WW368 put the call that
        // provokes above it. Nothing else about the round changes, so the two rungs either side of
        // this line are the same act with the pause in two places and not two experiments.
        if (rung >= Rung.Guard)
            Thread.Sleep(GuardMs);

        if (rung >= Rung.Split)
        {
            Spaced.End();
            Spaced.Erase(standing);
        }
        else
        {
            Spaced.Clear(standing);
        }

        Spaced.Batch(typing);

        if (rung >= Rung.Settle)
            Settling(element, typing);
        else
            Draining(element);
    }

    /// <summary>
    /// Take the focus the way the act takes it: <c>SetFocus</c> on the element the admission already
    /// holds. WW368.
    /// </summary>
    /// <param name="element">The box, resolved before the rounds.</param>
    private static void Focused(AutomationElement element)
    {
        try
        {
            element.SetFocus();
        }
        catch (Exception refused) when (refused is InvalidOperationException or ElementNotAvailableException)
        {
            // A refused focus is the desk and not the fault. The round goes on: what it measures is
            // what the send did, and a round the box would not take is caught by the captions.
        }
    }

    /// <summary>Read the value every poll for the whole drain, however the box reads. WW355's shape.</summary>
    /// <param name="element">The box, resolved before the rounds.</param>
    private static void Draining(AutomationElement element)
    {
        var until = Stopwatch.StartNew();
        while (until.ElapsedMilliseconds < DrainMs)
        {
            _ = Valued(element);
            Thread.Sleep(PollMs);
        }
    }

    /// <summary>
    /// Read the value every poll and stop the moment it says what was sent. WW368, and the act's own
    /// shape with the pause at zero: the engine polls to a deadline and returns on the first reading
    /// that matches, so it reads fewer times than an arm draining for a fixed span.
    /// </summary>
    /// <param name="element">The box, resolved before the rounds.</param>
    /// <param name="expected">What the box should come to say.</param>
    private static void Settling(AutomationElement element, string expected)
    {
        var until = Stopwatch.StartNew();
        while (until.ElapsedMilliseconds < SettleMs)
        {
            if (string.Equals(Valued(element), expected, StringComparison.Ordinal))
                return;

            Thread.Sleep(PollMs);
        }
    }

    /// <summary>
    /// The box's value through its own pattern, which is the read the engine settles with. Null
    /// where the element went, because a control that closed under a round is the desk rather than
    /// the fault.
    /// </summary>
    /// <param name="element">The box, resolved before the rounds.</param>
    private static string? Valued(AutomationElement element)
    {
        try
        {
            return element.GetCurrentPattern(ValuePattern.Pattern) is ValuePattern pattern
                ? pattern.Current.Value
                : null;
        }
        catch (Exception gone) when (gone is ElementNotAvailableException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// What the rungs come to, said as which step the rate appeared on — and then what the two
    /// placements of the pause came to, which is a second question off the same run.
    /// <para>
    /// The control leads, as it does in Disturbance and for the same reason: a control rung that
    /// faulted has no clean baseline for the rungs above it to have departed from, and a sentence
    /// about where the difference enters written off that is a conclusion about the desk.
    /// </para>
    /// <para>
    /// WW381's rung is read after the climb and never inside it. Where a rate enters is a fact about
    /// the act, and <c>guard</c> is not the act — a search that found it first would report the
    /// repair as the difference, which is the one sentence this ladder must not produce.
    /// </para>
    /// </summary>
    /// <param name="read">What each rung read.</param>
    private static string Verdict(IReadOnlyDictionary<Rung, Measured> read)
    {
        var counted = string.Join(
            ", ",
            Rungs.Select(one => $"{one.ToString().ToLowerInvariant()} {read[one].Substituted} of {read[one].Ran}"));

        var rounds = read[Control].Ran;

        if (read[Control].Substituted > 0)
        {
            return $"The control faulted: {counted}. `arm` is WW355's own reading, which read zero"
                + " over eight hundred rounds, so a run where it faults is a desk doing something"
                + " else and nothing here attributes anything."
                + " WW397 measured what that something is on the guest this project uses: 4 of 3600"
                + " across three runs on two days, one of them after a cold boot, where WW355 read 0"
                + " of 3200 and WW368 read 0 of 1200. So a machine whose control faults may not be"
                + " having a bad evening — this one has had a floor of about 1 in 900 since"
                + " 2026-09-05, and running it again there answers the same way. What separates at"
                + " that floor is a quieter machine, or a count several times this one.";
        }

        // The first step above the control that faulted, and what it adds, taken together off the
        // one list. WW394: they were two lookups a switch apart, and the switch's default named a
        // rung rather than refusing to answer for one it had not heard of.
        var entered = Climbing.Where(one => read[one.Rung].Substituted > 0).ToList();
        if (entered.Count == 0)
        {
            return $"Nothing separated: {counted}. Every rung read clean, including the one that is"
                + " the act's own shape with the pause at zero — so either the difference is not in"
                + $" this list, or {rounds} rounds a rung cannot see it. WW355 read the act at 1 in"
                + " 1200, so a rung of a few hundred expects a fraction of a fault and a row of"
                + " zeros is the count being too small rather than the rungs being clean."
                + Placement(read);
        }

        var (first, added) = entered[0];

        return $"It enters at `{first.ToString().ToLowerInvariant()}`: {counted}. Every rung below it"
            + $" read clean, and what that one adds is {added} — so that is what the arm was not"
            + " doing, and it is where the rate the act still carries comes from."
            + Placement(read);
    }

    /// <summary>
    /// What the two placements of the pause came to. WW381.
    /// <para>
    /// Read as a pair and against each other, never against a number from another evening. WW368
    /// read <c>settle</c> at 1 in 1200 and WW313 measured this fault's rate moving within a single
    /// run, so what a clean <c>guard</c> has to beat is what <c>settle</c> read beside it — in the
    /// same rounds, on the same desk, an hour apart at most.
    /// </para>
    /// <para>
    /// Which makes the expectation the sentence rather than the counts. At the rate the act carries,
    /// a rung of a few hundred rounds expects a fraction of a substitution, and two rungs that both
    /// read zero there have agreed about nothing at all — so a run too small is told what it would
    /// take rather than given a verdict it did not earn.
    /// </para>
    /// </summary>
    /// <param name="read">What each rung read.</param>
    private static string Placement(IReadOnlyDictionary<Rung, Measured> read)
    {
        var settle = read[Rung.Settle];
        var guard = read[Rung.Guard];

        var said = $"{Environment.NewLine}The placement: `settle` spends nothing and read"
            + $" {settle.Substituted} of {settle.Ran}; `guard` spends the engine's own {GuardMs}ms"
            + $" between the focus and the keys and read {guard.Substituted} of {guard.Ran}.";

        if (settle.Substituted == 0 || settle.Ran == 0)
        {
            return said + " There is no rate here to have moved: `settle` is the act with the pause"
                + " at zero and it faulted nowhere in this run, so a clean `guard` is two clean rungs"
                + " and not a repair. The pair says something only where the rung below it faults.";
        }

        var rate = (double)settle.Substituted / settle.Ran;
        var predicted = rate * guard.Ran;

        if (predicted < Resolvable)
        {
            return said + $" That does not separate them: at `settle`'s own rate in this run,"
                + $" {guard.Ran} rounds carry about {predicted:F1} substitutions, and a rung"
                + $" expecting fewer than {Resolvable:F0} reads clean on a quiet evening whatever the"
                + $" pause is doing. It would take about {Math.Ceiling(Resolvable / rate):F0} rounds"
                + " a rung to ask this properly.";
        }

        if (guard.Substituted == 0)
        {
            return said + $" The fault follows the focus: this length expects about {predicted:F0}"
                + " with the pause below the keys and `guard` carried none. So the interval moves"
                + " rather than doubles — the same milliseconds the engine already pays, spent above"
                + " the send instead of after it.";
        }

        return guard.Substituted * 2 <= predicted
            ? said + $" It moves most of it and not all: this length expects about {predicted:F0} and"
                + $" `guard` carried {guard.Substituted}. Halving is the crude reading a count this"
                + " size supports, and what it says is that the focus is part of what provokes and"
                + " that the send is not acquitted by moving the pause off it."
            : said + $" The fault does not follow the focus: this length expects about {predicted:F0}"
                + $" and `guard` carried {guard.Substituted}, so a pause above the keys is not the"
                + " pause below them. Both calls would need one, which is a worse answer than the"
                + " move and a true one.";
    }
}

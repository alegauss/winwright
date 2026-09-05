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
/// </summary>
internal static class Transfer
{
    /// <summary>One step of the walk from the arm to the act.</summary>
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
    }

    /// <summary>The rungs, in the order they are climbed.</summary>
    private static readonly Rung[] Rungs = [Rung.Arm, Rung.Focus, Rung.Split, Rung.Settle];

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
                + " ValuePattern round-trip on an element resolved before the send — and each adds"
                + " exactly one thing the real act does. `arm` is WW355's reading, which read zero:"
                + " the focus taken once, End and the backspaces in one SendInput, the text in"
                + " another, and the value read every poll for a fixed 300ms. `focus` takes the focus"
                + " before every round the way the act does. `split` sends End in a call of its own,"
                + " so a round is three arrays into the queue rather than two, which is the engine's"
                + " shape. `settle` stops reading the moment the box says what was sent instead of"
                + " draining for a fixed time, which makes the round the act's with the pause at"
                + " zero. `substituted` is what the window received differing from what was sent.");

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

        Console.WriteLine(Verdict(read));
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
    /// What the rungs come to, said as which step the rate appeared on.
    /// <para>
    /// The control leads, as it does in Disturbance and for the same reason: a control rung that
    /// faulted has no clean baseline for the rungs above it to have departed from, and a sentence
    /// about where the difference enters written off that is a conclusion about the desk.
    /// </para>
    /// </summary>
    /// <param name="read">What each rung read.</param>
    private static string Verdict(IReadOnlyDictionary<Rung, Measured> read)
    {
        var counted = string.Join(
            ", ",
            Rungs.Select(one => $"{one.ToString().ToLowerInvariant()} {read[one].Substituted} of {read[one].Ran}"));

        var rounds = read[Rung.Arm].Ran;

        if (read[Rung.Arm].Substituted > 0)
        {
            return $"The control faulted: {counted}. `arm` is WW355's own reading, which read zero"
                + " over eight hundred rounds, so a run where it faults is a desk doing something"
                + " else and nothing here attributes anything. Run it again on a quiet guest.";
        }

        var first = Rungs.Skip(1).FirstOrDefault(one => read[one].Substituted > 0);
        if (first == default)
        {
            return $"Nothing separated: {counted}. Every rung read clean, including the one that is"
                + " the act's own shape with the pause at zero — so either the difference is not in"
                + $" this list, or {rounds} rounds a rung cannot see it. WW355 read the act at 1 in"
                + " 1200, so a rung of a few hundred expects a fraction of a fault and a row of"
                + " zeros is the count being too small rather than the rungs being clean.";
        }

        var added = first switch
        {
            Rung.Focus => "taking the focus before every round",
            Rung.Split => "sending End in a call of its own, so the queue sees three arrays and not two",
            _ => "stopping the read the moment the box says what was sent, which is the act's own shape",
        };

        return $"It enters at `{first.ToString().ToLowerInvariant()}`: {counted}. Every rung below it"
            + $" read clean, and what that one adds is {added} — so that is what the arm was not"
            + " doing, and it is where the rate the act still carries comes from.";
    }
}

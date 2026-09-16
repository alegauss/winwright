using System.Diagnostics;

using Winwright.Locating;
using Winwright.Projects;
using Winwright.Tracing;
using Winwright.Verdicts;

namespace Winwright.Asserting;

/// <summary>One value the subject held, and when it was first seen holding it.</summary>
/// <param name="Read">What it read. Null where the subject answered nothing at all.</param>
/// <param name="AtMs">How far into the wait it first read that.</param>
public sealed record Reading(string? Read, int AtMs)
{
    /// <summary>The reading as a sentence prints it, with nothing told apart from an empty string.</summary>
    public override string ToString() => Read is null ? "nothing" : $"'{Read}'";
}

/// <summary>
/// What an expectation saw while it waited.
/// <para>
/// The first version of a timed-out read in claude-tray reported no panes and no status line after
/// 25 seconds, while the status line had been up for the whole 25 seconds saying it was computing
/// — and the real fault was elsewhere entirely, a missing template part. The message pointed at
/// timing, and getting past it cost a throwaway script. A boolean cannot tell those two apart, so
/// nothing here is a boolean.
/// </para>
/// <para>
/// The distinction that would have saved that afternoon is between never seeing the subject and
/// seeing it hold the wrong value the whole time. Both are a red; only one of them is about
/// timing, and <see cref="PollsThatSaw"/> is which.
/// </para>
/// </summary>
public sealed record Expectation
{
    /// <summary>
    /// How many changes of reading are printed before the rest are counted. A value that flaps
    /// would otherwise put a hundred entries in a sentence nobody then reads.
    /// </summary>
    public const int MostReadings = 6;

    internal Expectation(
        string name, string wanted, IReadOnlyList<Reading> readings, int waitedMs, int polls, int pollsThatSaw)
    {
        Name = name;
        Wanted = wanted;
        Readings = readings;
        WaitedMs = waitedMs;
        Polls = polls;
        PollsThatSaw = pollsThatSaw;
    }

    /// <summary>What was expected, as the scenario names it.</summary>
    public string Name { get; }

    /// <summary>The value it was waiting for.</summary>
    public string Wanted { get; }

    /// <summary>
    /// Every value the subject was seen holding, in order, one entry per change rather than one
    /// per poll — a hundred polls of the same string is one fact, and the changes are the story.
    /// </summary>
    public IReadOnlyList<Reading> Readings { get; }

    /// <summary>How long it waited, in milliseconds.</summary>
    public int WaitedMs { get; }

    /// <summary>How many times it looked.</summary>
    public int Polls { get; }

    /// <summary>
    /// The control view that explains this, where it went red and something had a window to read.
    /// Null on a green, and null where the expectation was built from a reading nobody could point
    /// at a window.
    /// <para>
    /// WW166. The type that builds this view is bounded, budgeted and thoroughly tested, and
    /// outside its own tests nothing in the engine called it — a capability with no caller, which
    /// is a criterion met by a type existing rather than by a run. What was missing is a verb that
    /// holds both halves at once: <see cref="Expect.That(string, string, Func{string}, int, int)" />
    /// takes a function and never sees a window, so it could not have attached one.
    /// <see cref="Expect.Of(Winwright.Locating.Subject, string, string, Func{Winwright.Locating.PatternValues, string},
    /// int)" /> is the verb that does.
    /// </para>
    /// </summary>
    public Diagnosis? Explains { get; private init; }

    /// <summary>
    /// The same expectation with the view attached. Refused on anything but a red, by the
    /// diagnosis itself: a dump under every green is a report nobody reaches the end of.
    /// </summary>
    /// <param name="view">The control view as it stood when this went red.</param>
    public Expectation Explaining(Diagnosis view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return this with { Explains = view };
    }

    /// <summary>
    /// Why the subject never answered, where the locator itself was diagnosed. WW456.
    /// <para>
    /// The one arm of <see cref="Sentence" /> that used to end without a reason. A subject that was
    /// never seen at all has no reading to print and no control view worth the room — the tree under
    /// a window nothing resolved in is not what the reader needs — so the sentence said how long it
    /// waited and stopped there, which sounds like timing and is usually not.
    /// </para>
    /// <para>
    /// Measured across three tasks and three guest runs. An adopter's step reported <c>nothing
    /// answered to it in 22 polls over 6177ms</c> one line after the same run had reported the menu
    /// it read back, and each run afterwards ruled out one thing that sentence could have carried:
    /// whether anything was standing, whether more than one was, whether it held entries, and what
    /// type they were. <see cref="LocatorMiss" /> had every one of those and reached nobody.
    /// </para>
    /// </summary>
    public LocatorMiss? Missed { get; private init; }

    /// <summary>The same expectation carrying the diagnosed miss. WW456.</summary>
    /// <param name="miss">Why the locator found nothing.</param>
    public Expectation Missing(LocatorMiss miss)
    {
        ArgumentNullException.ThrowIfNull(miss);
        return this with { Missed = miss };
    }

    /// <summary>
    /// How many of those looks found the subject answering at all. This is the number that
    /// separates "it was never there" from "it was there and it never said what was wanted", and
    /// it is the whole reason this type exists.
    /// </summary>
    public int PollsThatSaw { get; }

    /// <summary>What it read last, which is what was there when the wait gave up.</summary>
    public string? LastRead => Readings.Count == 0 ? null : Readings[^1].Read;

    /// <summary>Whether the subject ever answered.</summary>
    public bool EverSaw => PollsThatSaw > 0;

    /// <summary>Whether the expectation held.</summary>
    public bool Held => string.Equals(LastRead, Wanted, StringComparison.Ordinal) && EverSaw;

    /// <summary>
    /// What happened, in the words that decide between a re-run and a hunt.
    /// </summary>
    public string Sentence()
    {
        if (Held)
            return $"{Name} read '{Wanted}' after {WaitedMs}ms and {Times(Polls)}.";

        // Never answered. This used to be the only sentence that sounds like it is about timing,
        // and WW456 is why it no longer is left at that: it sounds like timing and it is usually
        // about the tree, so the miss's own diagnosis follows it where there is one.
        if (!EverSaw)
        {
            return $"expected {Name} to read '{Wanted}'; nothing answered to it in {Times(Polls)} over {WaitedMs}ms."
                + (Missed is null ? "" : $" {Missed.Sentence()}");
        }

        // Answered throughout and never said it. The sentence claude-tray needed and did not get.
        if (Readings.Count == 1)
            return $"expected {Name} to read '{Wanted}'; it read {Readings[0]} in "
                + $"{PollsThatSaw} of {Times(Polls)} over {WaitedMs}ms, and never anything else.";

        var shown = Readings.Take(MostReadings).Select(reading => $"{reading} at {reading.AtMs}ms");
        var rest = Readings.Count - MostReadings;
        var more = rest > 0 ? $", and {rest} further change{(rest == 1 ? "" : "s")} not shown" : "";

        return $"expected {Name} to read '{Wanted}'; it read {string.Join(", then ", shown)}{more} — "
            + $"{PollsThatSaw} of {Times(Polls)} saw it over {WaitedMs}ms.";
    }

    /// <summary>The result a verdict counts, carrying this sentence as its detail.</summary>
    public AssertionResult AsAssertion() =>
        Held ? AssertionResult.Pass(Name, Sentence()) : AssertionResult.Fail(Name, Sentence());

    /// <summary>The step a trace keeps, with the wait and the poll count already on it.</summary>
    public TraceStep AsTraceStep() => new()
    {
        Verb = "expect",
        Locator = Name,
        ReadBack = LastRead,
        WaitedMs = WaitedMs,
        Polls = Polls,
        Verdict = Held ? StepVerdict.Ok : StepVerdict.Failed,
        Detail = Held ? null : Sentence(),
    };

    private static string Times(int polls) => polls == 1 ? "1 poll" : $"{polls} polls";
}

/// <summary>
/// Waiting for a value to be what it should be, and reporting what it was instead.
/// <para>
/// Two doors for the same reason <see cref="Locating.Attempt"/> has two: <see cref="Now"/> looks
/// once and never sleeps, <see cref="That(string, string, Func{string}, int, int)"/> takes a
/// required deadline. Neither is reachable by leaving an argument off the other, so nobody
/// inherits a wait they did not ask for or loses the resolution of one they did.
/// </para>
/// </summary>
public static class Expect
{
    /// <summary>
    /// Wait for <paramref name="read"/> to answer <paramref name="wanted"/>, recording every
    /// change of reading on the way.
    /// </summary>
    /// <param name="name">What is being expected, as the scenario names it.</param>
    /// <param name="wanted">The value it should read.</param>
    /// <param name="read">What to read. Null means the subject answered nothing at all.</param>
    /// <param name="deadlineMs">How long to wait.</param>
    /// <param name="pollMs">How often to look.</param>
    /// <exception cref="ArgumentOutOfRangeException">Where the deadline is not positive.</exception>
    public static Expectation That(string name, string wanted, Func<string?> read, int deadlineMs, int pollMs = 25)
    {
        Named(name, wanted, read);
        if (deadlineMs <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(deadlineMs), deadlineMs, "a deadline of nothing is a single look, which is Expect.Now");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pollMs);

        var clock = Stopwatch.StartNew();
        var readings = new List<Reading>();
        var polls = 0;
        var saw = 0;

        while (true)
        {
            polls++;
            var value = read();
            if (value is not null)
                saw++;

            // One entry per change and not one per poll: a hundred polls of the same string is
            // one fact, and what a reader needs is where it changed.
            if (readings.Count == 0 || !string.Equals(readings[^1].Read, value, StringComparison.Ordinal))
                readings.Add(new Reading(value, (int)clock.ElapsedMilliseconds));

            if (string.Equals(value, wanted, StringComparison.Ordinal))
                return new Expectation(name, wanted, readings, (int)clock.ElapsedMilliseconds, polls, saw);

            var left = deadlineMs - (int)clock.ElapsedMilliseconds;
            if (left <= 0)
                return new Expectation(name, wanted, readings, (int)clock.ElapsedMilliseconds, polls, saw);

            Thread.Sleep(Math.Min(pollMs, left));
        }
    }

    /// <summary>
    /// Watch a subject for a value, and where it never arrives, attach the control view that
    /// explains it.
    /// <para>
    /// WW166. This is the verb that holds both halves at once. Every other spelling takes a
    /// function and never sees a window, so a red came back with a sentence and the reading had to
    /// be done twice — once by the harness, and once by a person writing a throwaway script to ask
    /// the same question again. Here the subject is in hand, so the tree is read at the instant it
    /// went red rather than by somebody else afterwards.
    /// </para>
    /// <para>
    /// The view is attached on a red only, and it marks the subject's own element in the tree
    /// rather than the root: a budget that drops the one line the reader came for is worse than no
    /// dump at all.
    /// </para>
    /// </summary>
    /// <param name="subject">What to watch, which is also what to diagnose against.</param>
    /// <param name="name">What is expected, as the scenario names it.</param>
    /// <param name="wanted">The value it is waiting for.</param>
    /// <param name="read">Which of the subject's patterns to read.</param>
    /// <param name="budget">How many elements the view may show.</param>
    public static Expectation Of(
        Subject subject,
        string name,
        string wanted,
        Func<PatternValues, string?> read,
        int budget = Diagnosis.DefaultBudget)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(read);

        var last = default(ElementFacts);
        var missed = default(LocatorMiss);
        var watched = That(
            name,
            wanted,
            () =>
            {
                var look = subject.ReadOnce();
                last = look.Facts;
                missed = look.Miss;
                return look.Found ? read(look.Values) : null;
            },
            subject.DeadlineMs,
            subject.PollMs);

        if (watched.Held)
            return watched;

        // WW456. The last look's diagnosis, kept only where the subject was never seen — which is
        // the arm whose sentence ends without a reason. Where it answered and read the wrong thing,
        // the readings are the reason and a miss from some earlier poll would be a second story.
        var carrying = !watched.EverSaw && missed is not null ? watched.Missing(missed) : watched;

        // Read now rather than kept from the poll: what a reader wants is the window as it stood
        // when the deadline ran out, and a tree captured earlier is a page about a moment that had
        // not failed yet.
        return carrying.Explaining(Diagnosis.OfWindow(carrying.AsAssertion(), subject.Window, last, budget));
    }

    /// <summary>The same, with the deadline and the poll interval read from what the project declared.</summary>
    public static Expectation That(
        string name, string wanted, Func<string?> read, Timeouts timeouts, string named = "resolve")
    {
        ArgumentNullException.ThrowIfNull(timeouts);
        return That(name, wanted, read, timeouts.For(named), timeouts.For("poll"));
    }

    /// <summary>
    /// Look exactly once. No poll and no sleep — which is what asking whether something is
    /// <em>already</em> so actually needs, and what keeps the answer's timing the application's.
    /// </summary>
    public static Expectation Now(string name, string wanted, Func<string?> read)
    {
        Named(name, wanted, read);

        var clock = Stopwatch.StartNew();
        var value = read();
        clock.Stop();

        return new Expectation(
            name, wanted, [new Reading(value, 0)], (int)clock.ElapsedMilliseconds, 1, value is null ? 0 : 1);
    }

    private static void Named(string name, string wanted, Func<string?> read)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(wanted);
        ArgumentNullException.ThrowIfNull(read);
    }
}

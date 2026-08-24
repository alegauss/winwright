using System.Collections.ObjectModel;

using Winwright.Asserting;
using Winwright.Processes;
using Winwright.Projects;
using Winwright.Windowing;

namespace Winwright.Verdicts;

/// <summary>One thing a run measured about the machine before it started.</summary>
/// <param name="Name">What was measured, under the name the assertions refer to it by.</param>
/// <param name="Condition">What it turned out to be, or null where the measurement was not taken.</param>
/// <param name="Sentence">The reading, or why it was not taken.</param>
public sealed record Measured(string Name, Precondition? Condition, string Sentence)
{
    /// <summary>Whether this run measured it at all.</summary>
    public bool Was => Condition is not null;

    /// <summary>Whether it was measured and this machine has it.</summary>
    public bool Held => Condition?.Satisfied == true;

    /// <summary>The one line the preamble shows.</summary>
    public override string ToString() => Was
        ? $"  {(Held ? "ok      " : "absent  ")}{Name}: {Sentence}"
        : $"  not read {Name}: {Sentence}";
}

/// <summary>
/// One thing a run read about what the scenario declared, rather than about the machine.
/// <para>
/// WW136. A precondition is a fact about the desk that an assertion may be excused by; a finding is
/// not. A pointer act declaring that its control has no automation peer, against a control offering
/// Invoke, is a file that has gone stale — the act still works, so nothing is excused and nothing
/// goes red, and a reader who never hears about it keeps quoting a reason nobody checked.
/// </para>
/// </summary>
/// <param name="Named">What was read.</param>
/// <param name="Holds">
/// Whether what the file said is what the application says, and null where this run could not read
/// it at all.
/// <para>
/// WW151. The third state is the one the store reading needed: a run that took no fingerprint
/// because no project declared a store has nothing to say, and a run that took one and found it
/// moved has something to say. Reporting them the same way is the shape this project keeps
/// refusing, and two states could only ever report them the same way.
/// </para>
/// </param>
/// <param name="Sentence">The reading, or why it was not taken.</param>
public sealed record Finding(string Named, bool? Holds, string Sentence)
{
    /// <summary>Whether this run read it at all.</summary>
    public bool Was => Holds is not null;

    /// <summary>The one line the preamble shows.</summary>
    public override string ToString() => Was
        ? $"  {(Holds == true ? "agrees  " : "differs ")}{Named}: {Sentence}"
        : $"  not read {Named}: {Sentence}";
}

/// <summary>
/// Everything a run measured about the machine, in one reading taken once.
/// <para>
/// The five measurements shipped and none of them was joined: staleness, the running binary, the
/// foreground, the launch arguments and the resolved language each answer with a precondition and
/// a sentence, and each is reached by its own call on its own type. Nothing listed them, so a
/// run's claim about which binary it drove was met three times over by three sentences — which is
/// to say it was not met once, and a reader got whichever the caller remembered to print.
/// </para>
/// <para>
/// The half that matters more: a runner assembling the precondition set by hand is one day edited
/// by somebody who does not know all five are there, and the forgotten one stops being measured
/// while every assertion that needed it silently starts passing. That is this project's founding
/// defect with a different subject, and here it is closed by there being one list.
/// </para>
/// <para>
/// A measurement this run could not take is <em>recorded as not taken</em> rather than left out.
/// An absent line and a missing line read the same to somebody skimming, and only one of them is
/// a statement.
/// </para>
/// </summary>
public sealed record Preamble
{
    private Preamble(
        Desk machine,
        IReadOnlyList<Measured> measurements,
        IReadOnlyList<Finding> findings,
        StoreFingerprint? store,
        string storeAbsence)
    {
        Machine = machine;
        Measurements = measurements;
        Findings = findings;
        Store = store;
        StoreAbsence = storeAbsence;
    }

    /// <summary>
    /// What this run read about the desk itself, as opposed to about the application on it.
    /// <para>
    /// WW156. Kept whole beside the flattened measurements, because the two are asked different
    /// questions. Every other reading here excuses one assertion; these six say whether anything
    /// could have been observed at all, and that is a statement about the run rather than about a
    /// check in it.
    /// </para>
    /// </summary>
    public Desk Machine { get; }

    /// <summary>Every measurement, taken or not, in the order a preamble prints them.</summary>
    public IReadOnlyList<Measured> Measurements { get; }

    /// <summary>
    /// What this run read about the scenario's own declarations. Never a precondition: a finding
    /// excuses nothing, and an assertion may not claim to have been checked against one.
    /// </summary>
    public IReadOnlyList<Finding> Findings { get; }

    /// <summary>
    /// The store as it read before the run, or null where no project declared one to fingerprint.
    /// <para>
    /// WW151. Taken here rather than by whoever remembers, which is what this block's criterion
    /// asked for and what nothing did: the type that takes a fingerprint is thorough and outside
    /// its own tests nothing called it, so the promise held exactly as often as an author wrote
    /// both halves — and the half that gets forgotten is the second one, when the run is over, the
    /// assertions passed and nobody is looking.
    /// </para>
    /// </summary>
    public StoreFingerprint? Store { get; }

    /// <summary>Why no fingerprint was taken, where none was. Empty where one was.</summary>
    public string StoreAbsence { get; }

    /// <summary>The findings the application disagrees with.</summary>
    public IReadOnlyList<Finding> Differing => new ReadOnlyCollection<Finding>(
        Findings.Where(one => one.Holds == false).ToList());

    /// <summary>The findings this run could not read at all.</summary>
    public IReadOnlyList<Finding> Unfound => new ReadOnlyCollection<Finding>(
        Findings.Where(one => !one.Was).ToList());

    /// <summary>
    /// Read the store again and answer what moved, as a finding to be joined with
    /// <see cref="Including" />.
    /// <para>
    /// A finding and not an assertion: the application did what it was driven to do, so nothing
    /// failed. A finding and not a precondition either: nothing may be excused by it. What it is is
    /// a thing a reader has to be told, and the whole defect was that nobody was.
    /// </para>
    /// <para>
    /// Where no store was declared this answers a reading that was not taken, which is a different
    /// sentence from one taken and clean. A run with nothing to say and a run with something to say
    /// are not reported the same way.
    /// </para>
    /// </summary>
    public Finding LeftAsFound()
    {
        if (Store is null)
            return new Finding(StoreChange.Named, null, StoreAbsence);

        try
        {
            // Asked to read itself again, so the after reading cannot be of a different list than
            // the before one — a comparison across two lists reports files appearing and going.
            var change = Store.Against(Store.Again());
            return new Finding(StoreChange.Named, change.Untouched, change.Sentence());
        }
        catch (Exception unreadable) when (unreadable is IOException or UnauthorizedAccessException)
        {
            return new Finding(StoreChange.Named, null, $"the store could not be read again: {unreadable.Message}");
        }
    }

    /// <summary>
    /// The same reading, carrying what was read about the declarations too.
    /// <para>
    /// Joined here because this is the one place a run's readings gather, and joined this way round
    /// because the thing that produces a finding knows about acts and this does not — a preamble
    /// that reached into the acting half would be a cycle rather than a composition.
    /// </para>
    /// </summary>
    /// <param name="findings">What was read about the file's claims.</param>
    public Preamble Including(params Finding[] findings)
    {
        ArgumentNullException.ThrowIfNull(findings);
        return new Preamble(
            Machine,
            Measurements,
            new ReadOnlyCollection<Finding>([.. Findings, .. findings.Where(one => one is not null)]),
            Store,
            StoreAbsence);
    }

    /// <summary>
    /// The verdict a run gets where the desk it was handed cannot observe anything, and null where
    /// it can and the run is free to proceed.
    /// <para>
    /// WW156. Delegated to <see cref="Windowing.Desk.Refusal(string)"/> rather than decided again
    /// here: the desk is the subject, and a second copy of the rule is a second thing to keep in
    /// step. This is the spelling a runner reaches, because a runner already holds a preamble.
    /// </para>
    /// </summary>
    /// <param name="run">What the run would have checked, named as a whole.</param>
    public RunVerdict? Refusal(string run) => Machine.Refusal(run);

    /// <summary>
    /// The conditions the assertions are resolved against. Only the ones actually measured: a
    /// precondition nobody read is not one an assertion may claim to have been checked against.
    /// </summary>
    public IReadOnlyList<Precondition> Conditions => new ReadOnlyCollection<Precondition>(
        Measurements.Where(one => one.Was).Select(one => one.Condition!).ToList());

    /// <summary>The ones this machine does not have.</summary>
    public IReadOnlyList<Precondition> Absent => new ReadOnlyCollection<Precondition>(
        Conditions.Where(one => !one.Satisfied).ToList());

    /// <summary>The ones this run could not read at all.</summary>
    public IReadOnlyList<Measured> Unread => new ReadOnlyCollection<Measured>(
        Measurements.Where(one => !one.Was).ToList());

    /// <summary>Whether everything was measured and everything held.</summary>
    public bool Clear => Unread.Count == 0 && Absent.Count == 0;

    /// <summary>One measurement by name, or null where this preamble carries none such.</summary>
    /// <param name="named">The precondition's name.</param>
    public Measured? Find(string named)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(named);
        return Measurements.FirstOrDefault(one => string.Equals(one.Name, named.Trim(), StringComparison.Ordinal));
    }

    /// <summary>
    /// Take the reading.
    /// </summary>
    /// <param name="target">How this run reached the application.</param>
    /// <param name="declaration">The project, where one was loaded.</param>
    /// <param name="window">The window under test, where one is known.</param>
    /// <param name="ours">The pids this run owns, so its own processes are not other instances.</param>
    /// <remarks>
    /// The composition lives here and nowhere else. That is the whole point: adding a sixth
    /// measurement is this file and not an audit of every runner that forgot to call it.
    /// </remarks>
    public static Preamble Of(
        AppTarget target,
        ProjectDeclaration? declaration = null,
        nint window = 0,
        IEnumerable<int>? ours = null)
    {
        ArgumentNullException.ThrowIfNull(target);

        // WW156. The desk first, and in its own order, because these six stop the rest mattering:
        // a machine with no input desktop has nothing to say about whether the binary it would
        // have driven was stale. Read here rather than by a runner, for the reason this whole type
        // exists - a reading reached by its own call is one a runner is free to forget, and the
        // forgotten one stops being measured while every assertion that needed it starts passing.
        var machine = Desk.Read();
        var taken = new List<Measured>(
            machine.Conditions.Select(one => new Measured(
                one.Name,
                one,
                one.Satisfied ? "this machine has it" : one.Absence)))
        {
            new(AppTarget.LaunchArgumentsPreconditionName, target.LaunchArguments, target.Sentence()),
        };

        taken.Add(Read(RunningBinary.PreconditionName, "no project declared the executable to compare against",
            () => Named(RunningBinary.Check(Executable(declaration), target.Pid))));

        taken.Add(Read(Staleness.PreconditionName, "no project declared a source root to compare against",
            () => Named(Staleness.Of(Require(declaration)))));

        taken.Add(Read(ResolvedLanguage.PreconditionName, "no project declared any language files",
            () => Language(Require(declaration))));

        taken.Add(Read(Foreground.PreconditionName, "no window was under test when this was read",
            () => window == 0
                ? throw new InvalidOperationException("no window")
                : Named(Foreground.Check(window))));

        taken.Add(Read(InstanceCheck.OverrideName, "no project declared the executable to look for",
            () => Instances(Executable(declaration), ours)));

        // WW157. Beside the desk's six rather than among them: a desk with a person at it can
        // still be observed, so this excuses an assertion and never refuses a run.
        var alone = ForeignInput.Read();
        taken.Add(new Measured(ForeignInput.PreconditionName, alone.AsPrecondition(), alone.Sentence()));

        // WW151. The before reading, taken here for the reason every other reading is taken here: a
        // reading reached by its own call is one a runner is free to forget, and this is the one
        // that gets forgotten, because its other half falls due when the run is already over.
        var (store, absence) = Fingerprinted(declaration);

        return new Preamble(
            machine, new ReadOnlyCollection<Measured>(taken), new ReadOnlyCollection<Finding>([]), store, absence);
    }

    /// <summary>
    /// The store as it reads now, or why it was not read. Never thrown from: a project that
    /// declared no store is an ordinary project, and a store this run cannot read is a fact about
    /// the machine rather than an error in the run.
    /// </summary>
    /// <param name="declaration">The project, where one was loaded.</param>
    private static (StoreFingerprint? Store, string Absence) Fingerprinted(ProjectDeclaration? declaration)
    {
        if (declaration is null || !declaration.Declares("fingerprintStore"))
            return (null, "no project declared a store this run must leave as it found it");

        try
        {
            return (StoreFingerprint.Of([declaration.FingerprintStore], []), "");
        }
        catch (Exception unreadable)
            when (unreadable is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return (null, $"the declared store could not be read: {unreadable.Message}");
        }
    }

    /// <summary>
    /// The same reading with the store read again and what moved joined into it — the reading a run
    /// ends with rather than the one it started with.
    /// <para>
    /// WW170. <see cref="LeftAsFound" /> was written, bounded and thoroughly tested, and outside its
    /// own tests nothing called it. The reason is the one this type's own comment gives for taking
    /// the before half here: a reading reached by its own call is one a runner is free to forget,
    /// and this is the half that gets forgotten, because it falls due when the run is already over.
    /// </para>
    /// <para>
    /// Idempotent on the reading and not on the store: calling it twice reads the store twice and
    /// joins two findings, which is a report saying the same thing twice rather than a wrong one.
    /// <see cref="Around" /> is the spelling that cannot be called twice by accident.
    /// </para>
    /// </summary>
    public Preamble Closing() => Including(LeftAsFound());

    /// <summary>
    /// Whether the closing reading has been taken — the store read again and what moved joined in.
    /// <para>
    /// WW177. A reading that opened a fingerprint and never closed it is a reading from the middle
    /// of a run, and printing one under a final verdict shows a reader the machine as it was before
    /// the run touched it while the verdict beside it is about what happened after.
    /// </para>
    /// </summary>
    public bool Closed => Findings.Any(one => one.Named == StoreChange.Named);

    /// <summary>
    /// Take the reading, run the case, and close the reading — so the half that falls due after the
    /// run has a moment that exists, rather than a call somebody has to remember.
    /// <para>
    /// WW170. Shaped after <see cref="Asserting.Untouched.Around(IReadOnlyList{string}, Action)" />
    /// and for the same reason it gives: the close wraps the case rather than living in a disposer,
    /// so an exception from the case propagates untouched and no closing reading is taken. There is
    /// nothing to say about what a run left behind when the run did not finish.
    /// </para>
    /// </summary>
    /// <param name="target">What the run is driving.</param>
    /// <param name="run">The run.</param>
    /// <param name="declaration">The project, where one was loaded. Its store is what gets closed.</param>
    /// <param name="window">The window the foreground is read against, where there is one.</param>
    /// <param name="ours">The pids this run owns.</param>
    public static Preamble Around(
        AppTarget target,
        Action run,
        ProjectDeclaration? declaration = null,
        nint window = 0,
        IEnumerable<int>? ours = null)
    {
        ArgumentNullException.ThrowIfNull(run);

        var opened = Of(target, declaration, window, ours);
        run();
        return opened.Closing();
    }

    /// <summary>The preamble a summary opens with: one line per measurement, taken or not.</summary>
    public IReadOnlyList<string> Render()
    {
        var lines = new List<string> { Sentence() };
        lines.AddRange(Measurements.Select(one => one.ToString()));
        lines.AddRange(Findings.Select(one => one.ToString()));
        return new ReadOnlyCollection<string>(lines);
    }

    /// <summary>What the machine turned out to be, in the one sentence a reader skims.</summary>
    public string Sentence()
    {
        // A finding never makes the machine unclear and is never left out of the sentence either:
        // absent and checked read the same to whoever skims, which is the whole reason it is here.
        var about = new List<string>();
        if (Differing.Count > 0)
        {
            about.Add(
                $"{Differing.Count} of {Findings.Count - Unfound.Count} declared reading(s) differ from the "
                    + $"application: {string.Join("; ", Differing.Select(one => one.Sentence))}");
        }

        // WW151: and a reading nobody took is said as well, for the reason the not-read measurements
        // are. A run with nothing to say and a run with something to say are different runs, and a
        // sentence that mentions neither reads as the second one.
        if (Unfound.Count > 0)
            about.Add($"{Unfound.Count} reading(s) not taken: {string.Join(", ", Unfound.Select(one => one.Named))}");

        var found = about.Count == 0 ? "" : " " + string.Join(" ", about);

        if (Clear)
            return $"this run measured all {Measurements.Count} conditions and every one of them held.{found}";

        var parts = new List<string>();
        if (Absent.Count > 0)
            parts.Add($"{Absent.Count} absent: {string.Join(", ", Absent.Select(one => one.Name))}");

        if (Unread.Count > 0)
            parts.Add($"{Unread.Count} not read: {string.Join(", ", Unread.Select(one => one.Name))}");

        return $"this run measured {Measurements.Count} conditions, {string.Join("; ", parts)}.{found}";
    }

    /// <summary>The whole reading as a block of text.</summary>
    public override string ToString() => string.Join('\n', Render());

    /// <summary>
    /// Take one measurement, recording what stopped it rather than leaving it out. Anything the
    /// reading throws is the reading not being available, which is a fact about this run and not
    /// an error in it: a project that declared no language files is an ordinary project.
    /// </summary>
    private static Measured Read(string named, string absent, Func<Measured> reading)
    {
        try
        {
            return reading();
        }
        catch (Exception unavailable)
            when (unavailable is InvalidOperationException or ArgumentException or IOException)
        {
            return new Measured(named, null, absent);
        }
    }

    private static ProjectDeclaration Require(ProjectDeclaration? declaration) =>
        declaration ?? throw new InvalidOperationException("no project was declared");

    private static string Executable(ProjectDeclaration? declaration) => Require(declaration).Executable;

    private static Measured Named(RunningBinary read) =>
        new(RunningBinary.PreconditionName, read.AsPrecondition(), read.Sentence());

    private static Measured Named(Staleness read) =>
        new(Staleness.PreconditionName, read.AsPrecondition(), read.Sentence());

    private static Measured Named(Foreground read) =>
        new(Foreground.PreconditionName, read.AsPrecondition(), read.Sentence());

    private static Measured Language(ProjectDeclaration declaration)
    {
        var read = ResolvedLanguage.Resolve(declaration);
        return new Measured(ResolvedLanguage.PreconditionName, read.Matching(declaration.LanguageFallback), read.Sentence());
    }

    private static Measured Instances(string executable, IEnumerable<int>? ours)
    {
        var read = InstanceCheck.Of(executable, ours);
        var named = "this run is the only one showing the application";

        return new Measured(
            named,
            read.Windowed.Count == 0
                ? Precondition.Met(named)
                : Precondition.Absent(named, read.Sentence()),
            read.Sentence());
    }
}

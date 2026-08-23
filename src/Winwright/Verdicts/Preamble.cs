using System.Collections.ObjectModel;

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
/// <param name="Holds">Whether what the file said is what the application says.</param>
/// <param name="Sentence">The reading, in the line a preamble prints.</param>
public sealed record Finding(string Named, bool Holds, string Sentence)
{
    /// <summary>The one line the preamble shows.</summary>
    public override string ToString() => $"  {(Holds ? "agrees  " : "differs ")}{Named}: {Sentence}";
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
    private Preamble(IReadOnlyList<Measured> measurements, IReadOnlyList<Finding> findings)
    {
        Measurements = measurements;
        Findings = findings;
    }

    /// <summary>Every measurement, taken or not, in the order a preamble prints them.</summary>
    public IReadOnlyList<Measured> Measurements { get; }

    /// <summary>
    /// What this run read about the scenario's own declarations. Never a precondition: a finding
    /// excuses nothing, and an assertion may not claim to have been checked against one.
    /// </summary>
    public IReadOnlyList<Finding> Findings { get; }

    /// <summary>The findings the application disagrees with.</summary>
    public IReadOnlyList<Finding> Differing => new ReadOnlyCollection<Finding>(
        Findings.Where(one => !one.Holds).ToList());

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
            Measurements,
            new ReadOnlyCollection<Finding>([.. Findings, .. findings.Where(one => one is not null)]));
    }

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

        var taken = new List<Measured>
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

        return new Preamble(new ReadOnlyCollection<Measured>(taken), new ReadOnlyCollection<Finding>([]));
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
        var found = Differing.Count == 0
            ? ""
            : $" {Differing.Count} of {Findings.Count} declared reading(s) differ from the application: "
                + string.Join("; ", Differing.Select(one => one.Sentence));

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

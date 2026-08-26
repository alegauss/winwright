using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Winwright.Scenarios;

/// <summary>
/// What a case is launched against: the arguments, the environment variables, the sampled
/// environment, and whether the window may be lent.
/// <para>
/// WW60. The states a menu exists to report are the ones where the environment disagrees with the
/// application, and on a developer's machine it never does — so without a sampled environment those
/// assertions are only ever unchecked. The refusal below is the whole task: <em>one</em> declaration
/// decides both what the application is launched with and what the expectations are read from, so
/// the two cannot be given different modes and a sampled menu is never compared against a real
/// environment.
/// </para>
/// <para>
/// That is enforced by there being one field. <see cref="Environment"/> is what the launch carries
/// and what a report says the expectations were read against; a fixture naming an environment that
/// reaches the launch nowhere is refused, and so is one that names it twice — an argument spelling
/// the environment flag by hand beside the field is two places deciding one thing, and the second
/// one silently wins.
/// </para>
/// <para>
/// <see cref="Shareable"/> is WW62's half and is a fact about the window rather than about the run:
/// this application leaves a window in a state the next case would accept. Whether a run actually
/// lends it is opted into per invocation, because a case run alone still owning its process is the
/// property that keeps it worth running alone.
/// </para>
/// </summary>
public sealed record FixtureDeclaration
{
    private FixtureDeclaration(
        string name,
        string environment,
        string flag,
        IReadOnlyList<string> arguments,
        IReadOnlyDictionary<string, string> variables,
        bool shareable)
    {
        Name = name;
        Environment = environment;
        Flag = flag;
        Arguments = arguments;
        Variables = variables;
        Shareable = shareable;
    }

    /// <summary>What the fixture is called, and what a report names the launch by.</summary>
    public string Name { get; }

    /// <summary>
    /// The sampled environment this fixture is, or empty where it samples nothing and the
    /// application is launched as it comes. The one field both halves read.
    /// </summary>
    public string Environment { get; }

    /// <summary>
    /// The argument the environment reaches the application through, without its value — the
    /// <c>--language</c> of <c>--language=pt-BR</c>. Empty where the environment travels as a
    /// variable instead, or where there is no environment.
    /// </summary>
    public string Flag { get; }

    /// <summary>Everything else the launch carries, in declared order.</summary>
    public IReadOnlyList<string> Arguments { get; }

    /// <summary>The environment variables the launch sets, by name.</summary>
    public IReadOnlyDictionary<string, string> Variables { get; }

    /// <summary>Whether this window may be lent to a case that only reads it.</summary>
    public bool Shareable { get; }

    /// <summary>Whether this fixture samples an environment at all.</summary>
    public bool Samples => Environment.Length > 0;

    /// <summary>The application as it comes: no arguments, no variables, nothing sampled.</summary>
    public static FixtureDeclaration Plain { get; } =
        new("as it comes", "", "", [], new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()), false);

    /// <summary>
    /// Declare one.
    /// </summary>
    /// <param name="name">What to call it.</param>
    /// <param name="environment">The sampled environment, where it samples one.</param>
    /// <param name="flag">The argument the environment reaches the application through.</param>
    /// <param name="arguments">Everything else the launch carries.</param>
    /// <param name="variables">The environment variables it sets. A value carrying the environment counts as carrying it.</param>
    /// <param name="shareable">That this window may be lent to a case that only reads it.</param>
    /// <exception cref="ScenarioRefusedException">
    /// Where the environment reaches the launch nowhere, or reaches it twice.
    /// </exception>
    public static FixtureDeclaration Of(
        string name,
        string? environment = null,
        string? flag = null,
        IEnumerable<string>? arguments = null,
        IReadOnlyDictionary<string, string>? variables = null,
        bool shareable = false)
    {
        var called = string.IsNullOrWhiteSpace(name) ? "<unnamed fixture>" : name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ScenarioRefusedException(called, "a fixture is named, because a report says which one a case ran against");

        var sampled = environment?.Trim() ?? "";
        var through = flag?.Trim() ?? "";

        var rest = new List<string>();
        foreach (var argument in arguments ?? [])
        {
            if (string.IsNullOrWhiteSpace(argument))
                throw new ScenarioRefusedException(called, "one of its arguments is blank, and a blank argument says nothing");

            rest.Add(argument.Trim());
        }

        var set = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var variable in variables ?? new Dictionary<string, string>())
        {
            if (string.IsNullOrWhiteSpace(variable.Key))
                throw new ScenarioRefusedException(called, "one of its variables has no name");

            if (!set.TryAdd(variable.Key.Trim(), variable.Value ?? ""))
                throw new ScenarioRefusedException(called, $"it sets '{variable.Key.Trim()}' twice");
        }

        if (through.Length > 0 && sampled.Length == 0)
            throw new ScenarioRefusedException(called, $"it passes '{through}' and names no environment to pass through it");

        if (sampled.Length > 0)
        {
            // The refusal WW60 exists for, read the other way round. An argument spelling the flag
            // by hand beside the field is two places deciding one thing, and whichever the
            // application reads last is the one that decides — so the expectations describe the
            // field's environment and the window renders the argument's.
            var doubled = rest.Find(one => Names(one, through));
            if (doubled is not null)
            {
                throw new ScenarioRefusedException(
                    called,
                    $"'{doubled}' decides the environment a second time, and the expectations read only the first");
            }

            if (through.Length == 0 && !set.Values.Any(value => value.Contains(sampled, StringComparison.Ordinal)))
            {
                throw new ScenarioRefusedException(
                    called,
                    $"it samples '{sampled}' and nothing carries it to the launch, so the expectations would "
                    + "describe one environment and the window would render another");
            }
        }

        return new FixtureDeclaration(
            called, sampled, through, new ReadOnlyCollection<string>(rest), new ReadOnlyDictionary<string, string>(set), shareable);
    }

    /// <summary>
    /// Every argument the launch carries, the sampled environment among them. Derived rather than
    /// stored, so the launch and <see cref="Environment"/> cannot come apart.
    /// </summary>
    public IReadOnlyList<string> Launching()
    {
        var all = new List<string>(Arguments);
        if (Flag.Length > 0)
            all.Add($"{Flag}={Environment}");

        return new ReadOnlyCollection<string>(all);
    }

    /// <summary>How to start the application under test with this fixture in force.</summary>
    /// <param name="executable">The application, usually the project's own.</param>
    public ProcessStartInfo Starting(string executable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);

        var start = new ProcessStartInfo(executable) { UseShellExecute = false };
        foreach (var argument in Launching())
            start.ArgumentList.Add(argument);

        foreach (var variable in Variables)
            start.Environment[variable.Key] = variable.Value;

        return start;
    }

    /// <summary>What the expectations were read against, in the words a report prints.</summary>
    public string Sentence()
    {
        var sampled = Samples ? $"sampling {Environment}" : "the application as it comes";
        var lent = Shareable ? ", shareable" : "";
        return $"{Name}: {sampled}{lent}.";
    }

    /// <summary>The one line a listing shows.</summary>
    public override string ToString() => Sentence();

    /// <summary>Whether <paramref name="argument"/> is that flag, given with a value or not.</summary>
    private static bool Names(string argument, string flag)
    {
        if (flag.Length == 0)
            return false;

        return string.Equals(argument, flag, StringComparison.OrdinalIgnoreCase)
            || argument.StartsWith($"{flag}=", StringComparison.OrdinalIgnoreCase);
    }
}

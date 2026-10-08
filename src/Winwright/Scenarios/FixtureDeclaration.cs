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
        bool shareable,
        string language,
        bool resident,
        string workingDirectory)
    {
        Name = name;
        Environment = environment;
        Flag = flag;
        Arguments = arguments;
        Variables = variables;
        Shareable = shareable;
        Language = language;
        Resident = resident;
        WorkingDirectory = workingDirectory;
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

    /// <summary>
    /// The language tag the window this launches is in, or empty where nothing said.
    /// <para>
    /// WW240. A derived set used to refuse a project declaring more than one strings file, so an
    /// application shipping five languages had to declare one and pretend the other four were not
    /// there. The answer was always a line above: claude-tray's fixtures launch with `--lang en`, and
    /// the project-wide declaration that made the sweep work happened to agree with them.
    /// </para>
    /// <para>
    /// A fact about the window rather than about the run, which is <see cref="Shareable"/>'s shape and
    /// not <see cref="Environment"/>'s. The environment field is refused where nothing carries it to
    /// the launch, because it decides what the application is started with; this decides nothing —
    /// it says what the arguments produced, so that expectations are read out of the strings the
    /// window is actually showing.
    /// </para>
    /// </summary>
    public string Language { get; }

    /// <summary>The language this window is in, or null where the fixture named none.</summary>
    /// <exception cref="ScenarioRefusedException">Where the tag is not a language.</exception>
    public System.Globalization.CultureInfo? Speaking =>
        Language.Length == 0 ? null : Culture(Name, Language);

    /// <summary>
    /// Whether this launch draws no window of its own, and is held as a process instead.
    /// <para>
    /// WW257. The wait after a launch refuses where no window arrives, which is the right answer for
    /// every fixture that meant to draw one: nothing about the case was observed, so nothing about the
    /// application is being reported. A tray is the counter-example. claude-tray's `--second-tray`
    /// puts an icon in the notification area and draws nothing, and the window there is what a click
    /// on the icon is supposed to <em>produce</em> — so refusing the fixture makes the one thing being
    /// asserted a reason not to run.
    /// </para>
    /// <para>
    /// Said by the fixture and never inferred from the wait timing out, because that is the same
    /// refusal read backwards: a launch that was supposed to draw a window and did not is a failure,
    /// and a run that quietly carried on would report it as a case about the desktop. What a resident
    /// fixture's locators resolve against is the desktop, because that is where a tray icon lives.
    /// </para>
    /// </summary>
    public bool Resident { get; }

    /// <summary>
    /// The directory the application is started in, as the fixture declares it, or empty where it
    /// starts in the project's own root. Relative paths resolve against that root. WW508.
    /// <para>
    /// Found by quickshell. A fixture wanting the dialog to look the same on every desk passed the
    /// client a session file committed beside the cases — <c>cases/fixtures/MobaXterm.ini</c> — and
    /// that path meant nothing to the launched application, because the launch set no working
    /// directory and inherited whatever one the test runner happened to be in. The same argument
    /// resolved somewhere different under <c>run-tests.cmd</c>, under <c>dotnet test</c> and in the
    /// guest.
    /// </para>
    /// <para>
    /// The default is the fix and this field is the exception to it: an adopter who declares nothing
    /// gets the directory <c>winwright.json</c> sits in, which is what every other path the project
    /// declares already resolves against. The two workarounds that field was worth removing were
    /// setting the runner process's own current directory before a launch, which is global state in a
    /// test assembly, and committing an absolute path into a file read on other machines.
    /// </para>
    /// </summary>
    public string WorkingDirectory { get; }

    /// <summary>Whether this fixture samples an environment at all.</summary>
    public bool Samples => Environment.Length > 0;

    /// <summary>The application as it comes: no arguments, no variables, nothing sampled.</summary>
    public static FixtureDeclaration Plain { get; } =
        new("as it comes", "", "", [], new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()), false, "", false, "");

    /// <summary>
    /// Declare one.
    /// </summary>
    /// <param name="name">What to call it.</param>
    /// <param name="environment">The sampled environment, where it samples one.</param>
    /// <param name="flag">The argument the environment reaches the application through.</param>
    /// <param name="arguments">Everything else the launch carries.</param>
    /// <param name="variables">The environment variables it sets. A value carrying the environment counts as carrying it.</param>
    /// <param name="shareable">That this window may be lent to a case that only reads it.</param>
    /// <param name="language">The language tag the window it launches is in.</param>
    /// <param name="resident">That this launch draws no window, so the run holds it as a process.</param>
    /// <param name="workingDirectory">The directory to start it in, resolved against the project's root.</param>
    /// <exception cref="ScenarioRefusedException">
    /// Where the environment reaches the launch nowhere, or reaches it twice, or the language is not one.
    /// </exception>
    public static FixtureDeclaration Of(
        string name,
        string? environment = null,
        string? flag = null,
        IEnumerable<string>? arguments = null,
        IReadOnlyDictionary<string, string>? variables = null,
        bool shareable = false,
        string? language = null,
        bool resident = false,
        string? workingDirectory = null)
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

        // WW240. Judged here rather than where a set is derived: a tag that is not a language is
        // wrong on every machine, and discovering it on the run that was going to sweep with it costs
        // a launch to learn what the file already said.
        var speaking = language?.Trim() ?? "";
        if (speaking.Length > 0)
            Culture(called, speaking);

        // WW508. Trimmed and carried as written, never resolved here: a declaration is read without
        // a filesystem, and the root it resolves against belongs to the project rather than to the
        // file this fixture was declared in. An absolute path is left alone by the resolve below,
        // which is the rule every other path the project declares already follows.
        var starting = workingDirectory?.Trim() ?? "";

        return new FixtureDeclaration(
            called,
            sampled,
            through,
            new ReadOnlyCollection<string>(rest),
            new ReadOnlyDictionary<string, string>(set),
            shareable,
            speaking,
            resident,
            starting);
    }

    /// <summary>
    /// The culture a tag names, or a refusal saying it names none.
    /// <para>
    /// <c>predefinedOnly</c>, for the reason the label reader gives: without it .NET manufactures a
    /// culture for any string at all, so a typo becomes a language this ships no strings for and the
    /// refusal arrives about the wrong thing.
    /// </para>
    /// </summary>
    /// <param name="called">The fixture, for the refusal.</param>
    /// <param name="tag">The language tag.</param>
    /// <exception cref="ScenarioRefusedException">Where it names no language.</exception>
    private static System.Globalization.CultureInfo Culture(string called, string tag)
    {
        try
        {
            return System.Globalization.CultureInfo.GetCultureInfo(tag, predefinedOnly: true);
        }
        catch (System.Globalization.CultureNotFoundException)
        {
            throw new ScenarioRefusedException(
                called, $"it says its window is in '{tag}', which is not a language tag such as en or pt-BR");
        }
    }

    /// <summary>
    /// The one argument that puts a process on this fixture's sampled environment, or empty where it
    /// samples nothing. WW473.
    /// <para>
    /// Named rather than spelled twice, because the launch is no longer the only thing that has to
    /// carry it. A read-out is a launch of the application too, and until WW473 it was composed out
    /// of what the project declares and nothing the fixture said — so a window drawn on a sampled
    /// environment was compared against an application asked about the real machine, which is the
    /// failure this type's own note says the one field exists to prevent.
    /// </para>
    /// </summary>
    public string Sampling => Flag.Length > 0 ? $"{Flag}={Environment}" : "";

    /// <summary>
    /// Every argument the launch carries, the sampled environment among them. Derived rather than
    /// stored, so the launch and <see cref="Environment"/> cannot come apart.
    /// </summary>
    public IReadOnlyList<string> Launching()
    {
        var all = new List<string>(Arguments);
        if (Sampling.Length > 0)
            all.Add(Sampling);

        return new ReadOnlyCollection<string>(all);
    }

    /// <summary>
    /// The directory this launch starts in, resolved. WW508.
    /// <para>
    /// <c>Path.GetFullPath(path, root)</c> with the environment expanded, which is the rule
    /// <see cref="Projects.ProjectDeclaration"/> already applies to every path it declares — so a
    /// relative directory is relative to the same place, and one that is already absolute is left
    /// where it is.
    /// </para>
    /// </summary>
    /// <param name="root">The project's root, the directory its declaration sits in.</param>
    public string StartsIn(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);

        return WorkingDirectory.Length == 0
            ? System.IO.Path.GetFullPath(root)
            : System.IO.Path.GetFullPath(System.Environment.ExpandEnvironmentVariables(WorkingDirectory), root);
    }

    /// <summary>How to start the application under test with this fixture in force.</summary>
    /// <param name="executable">The application, usually the project's own.</param>
    /// <param name="root">
    /// The project's root. Required rather than defaulted, because the thing WW508 fixed is a launch
    /// that set no directory and inherited the runner's — and a default here would be that launch,
    /// spelled as a choice nobody made.
    /// </param>
    public ProcessStartInfo Starting(string executable, string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);

        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            WorkingDirectory = StartsIn(root),
        };
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
        var held = Resident ? ", resident" : "";

        // WW508, and as declared rather than resolved: the resolved path is this machine's, and a
        // report that two people compare is a report that has to say the same thing on both. Said
        // only where the fixture named one, which is Shareable's rule and Resident's — a line that
        // says "the project root" on every fixture is the mark that marks nothing.
        var started = WorkingDirectory.Length > 0 ? $", starting in {WorkingDirectory}" : "";
        return $"{Name}: {sampled}{lent}{held}{started}.";
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

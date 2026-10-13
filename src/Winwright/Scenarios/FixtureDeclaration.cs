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
        string workingDirectory,
        IReadOnlyList<StagedFile> files,
        IReadOnlyList<string> reaches)
    {
        Reaches = reaches;
        Files = files;
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

    /// <summary>
    /// The token a <see cref="Variables"/> value or a <see cref="WorkingDirectory"/> may name to mean
    /// the directory this fixture's <see cref="Files"/> were staged into. WW509.
    /// <para>
    /// A token rather than resolving every value as a path, which is what WW509's first draft asked
    /// for: <c>WINWRIGHT_ROLE=reader</c> would become a directory, and a value is not a path for
    /// being a string. This substitutes exactly where an author wrote it, and a value carrying any
    /// other brace — a line of JSON, say — is left alone.
    /// </para>
    /// </summary>
    public const string Staged = "{files}";

    /// <summary>
    /// The files copied into a directory of this launch's own, each a path relative to the project's
    /// root, in declared order. Empty where the fixture declares none. WW509.
    /// <para>
    /// Found adopting a case for quickshell's QS217. The client keeps saved sessions in a file under
    /// the user's AppData, and a case searching that list needs a store holding known sessions. A
    /// variable only reaches an application that reads one, so the case needed bytes where the
    /// application already looks — and the two workarounds were a flag that exists only for the
    /// harness, which the application's users then see, and writing into the real user profile before
    /// launching, which the next case tramples.
    /// </para>
    /// <para>
    /// With <see cref="Staged"/> a Windows application needs no flag at all: the fixture points
    /// <c>APPDATA</c> at the staged directory and the application finds a store this case put there.
    /// Declaring files and naming the token nowhere is refused, for WW60's reason one field over —
    /// files that reach the launch nowhere are a declaration that decided nothing.
    /// </para>
    /// </summary>
    public IReadOnlyList<StagedFile> Files { get; }

    /// <summary>
    /// The variables that name a folder Windows also answers as a known folder. WW519.
    /// <para>
    /// Found by quickshell's QS217. Its fixture pointed <c>APPDATA</c> at <see cref="Staged"/>, the
    /// launch was clean and the file was staged, and the client read the real profile's store anyway:
    /// <c>GetFolderPath(SpecialFolder.ApplicationData)</c> is <c>SHGetKnownFolderPath</c>, resolved
    /// from the registry and never from the variable. So did most Windows applications, which makes
    /// the variable a store only for an application that reads it — and a case about the wrong store
    /// is green.
    /// </para>
    /// <para>
    /// Said and not refused, because the engine cannot tell which kind of application it launched:
    /// one that reads the variable gets exactly the store the fixture asked for, and refusing that is
    /// refusing the case that works. What it can do is never let the fixture read as though the
    /// redirect covered both.
    /// </para>
    /// </summary>
    public static IReadOnlyList<string> ProfileVariables { get; } =
        new ReadOnlyCollection<string>(["APPDATA", "LOCALAPPDATA", "USERPROFILE", "HOME"]);

    /// <summary>
    /// The <see cref="ProfileVariables"/> this fixture sets, as declared, in that list's order. Empty
    /// where it sets none. WW519.
    /// </summary>
    public IReadOnlyList<string> VariableOnly =>
        new ReadOnlyCollection<string>(ProfileVariables.Where(Variables.ContainsKey).ToList());

    /// <summary>
    /// The clause a report adds where this fixture sets a <see cref="ProfileVariables"/> name, or
    /// empty where it sets none. WW519.
    /// </summary>
    public string VariableOnlyClause =>
        VariableOnly.Count == 0
            ? ""
            : $", setting {string.Join(" and ", VariableOnly)} as a variable only — an application asking "
                + "Windows for that known folder still reads the real profile";

    /// <summary>
    /// What the application connects to outside itself, each as <c>host:port</c> or as an
    /// environment variable naming one, in declared order. Empty where it reaches nothing. WW522.
    /// <para>
    /// Probed before the launch, every time, and never started: the server is the project's to run.
    /// Unreachable, every check on this fixture is a hole naming the address. Reachable, each is
    /// offered to the arguments and the variables as <c>{reach0}</c>, <c>{reach1}</c> and on, so the
    /// launch names the server the probe found rather than a second spelling of it.
    /// </para>
    /// <para>
    /// Probed per launch and not once per run, which was the open choice: a server that goes away
    /// halfway through a suite is the case where a run-wide answer would be a pass about a connection
    /// that was no longer there.
    /// </para>
    /// </summary>
    public IReadOnlyList<string> Reaches { get; }

    /// <summary>The token the arguments and variables name <see cref="Reaches"/>' first address by. WW522.</summary>
    public const string FirstReach = "{reach0}";

    /// <summary>
    /// Probe what this fixture reaches. Null where it reaches nothing or everything answered, and the
    /// absence that names the address that did not where something did not. WW522.
    /// </summary>
    /// <param name="timeoutMs">How long each connect may take.</param>
    public Verdicts.Precondition? Unreachable(int timeoutMs)
    {
        if (Reaches.Count == 0)
            return null;

        var probed = Processes.Reaching.Probe(Reached(), timeoutMs);
        return probed.Satisfied ? null : probed;
    }

    /// <summary>The addresses with their variables expanded, which is what is probed and what is offered.</summary>
    private IReadOnlyList<string> Reached() =>
        Reaches.Select(one => System.Environment.ExpandEnvironmentVariables(one)).ToList();

    /// <summary><paramref name="value"/> with every <c>{reachN}</c> swapped for that address.</summary>
    private string Offered(string value)
    {
        if (Reaches.Count == 0 || !value.Contains("{reach", StringComparison.Ordinal))
            return value;

        var reached = Reached();
        for (var at = 0; at < reached.Count; at++)
            value = value.Replace($"{{reach{at}}}", reached[at], StringComparison.Ordinal);

        return value;
    }

    /// <summary>Whether this fixture samples an environment at all.</summary>
    public bool Samples => Environment.Length > 0;

    /// <summary>
    /// Whether this fixture's launch depends on the staged directory existing — because it stages
    /// files, or because something it declares names <see cref="Staged"/>. WW515.
    /// </summary>
    public bool NeedsStaging =>
        Files.Count > 0
        || WorkingDirectory.Contains(Staged, StringComparison.Ordinal)
        || Variables.Values.Any(value => value.Contains(Staged, StringComparison.Ordinal));

    /// <summary>The application as it comes: no arguments, no variables, nothing sampled.</summary>
    public static FixtureDeclaration Plain { get; } =
        new("as it comes", "", "", [], new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()), false, "", false, "", [], []);

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
    /// <param name="files">The files to stage for this launch, each relative to the project's root.</param>
    /// <param name="reaches">What the application connects to, each <c>host:port</c> or a variable naming one.</param>
    /// <exception cref="ScenarioRefusedException">
    /// Where the environment reaches the launch nowhere, or reaches it twice, or the language is not
    /// one, or files are declared that reach the launch nowhere.
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
        string? workingDirectory = null,
        IEnumerable<StagedFile>? files = null,
        IEnumerable<string>? reaches = null)
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

        var staging = new List<StagedFile>();
        var landing = new Dictionary<string, StagedFile>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files ?? [])
        {
            if (string.IsNullOrWhiteSpace(file?.From))
                throw new ScenarioRefusedException(called, "one of its files is blank, and a blank path names nothing");

            var staged = file with { From = file.From.Trim(), To = file.To?.Trim() };
            var lands = Landing(called, staged);

            // WW524. Two entries landing on one path is the silent overwrite this used to be — two
            // plain files sharing a name from different folders among them — and which one the
            // application then reads is whichever was declared last.
            if (landing.TryGetValue(lands, out var already))
            {
                throw new ScenarioRefusedException(
                    called, $"'{already}' and '{staged}' both land on '{lands}', so one would overwrite the other");
            }

            landing[lands] = staged;
            staging.Add(staged);
        }

        // WW509, and WW60's refusal one field over: a declaration that reaches the launch nowhere
        // decided nothing. Files staged into a directory no variable and no working directory names
        // are files the application cannot be looking at, and the case that was written to read them
        // reads whatever the real machine had instead — which is green, and about the wrong store.
        if (staging.Count > 0
            && !starting.Contains(Staged, StringComparison.Ordinal)
            && !set.Values.Any(value => value.Contains(Staged, StringComparison.Ordinal)))
        {
            throw new ScenarioRefusedException(
                called,
                $"it stages {staging.Count} file(s) and nothing names '{Staged}', so the launch cannot "
                + "be looking at them");
        }

        // WW522. An address written out is judged here, because one that is not a host and a port is
        // wrong on every machine. One naming a variable is judged when it is probed, since what it
        // expands to is a fact about the machine running it.
        var reaching = new List<string>();
        foreach (var address in reaches ?? [])
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ScenarioRefusedException(called, "one of the addresses it reaches is blank, and a blank address names nothing");

            var written = address.Trim();
            if (!written.Contains('%', StringComparison.Ordinal) && Processes.Reaching.Parsed(written) is null)
            {
                throw new ScenarioRefusedException(
                    called, $"it reaches '{written}', which is not a host:port such as 127.0.0.1:2222 or a %VARIABLE% naming one");
            }

            reaching.Add(written);
        }

        return new FixtureDeclaration(
            called,
            sampled,
            through,
            new ReadOnlyCollection<string>(rest),
            new ReadOnlyDictionary<string, string>(set),
            shareable,
            speaking,
            resident,
            starting,
            new ReadOnlyCollection<StagedFile>(staging),
            new ReadOnlyCollection<string>(reaching));
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
            : System.IO.Path.GetFullPath(
                System.Environment.ExpandEnvironmentVariables(Resolved(WorkingDirectory, root)), root);
    }

    /// <summary>
    /// The directory this fixture's <see cref="Files"/> are staged into. Derived and never stored, so
    /// the path <see cref="Staged"/> resolves to and the path <see cref="Stage"/> writes cannot come
    /// apart. Creates nothing. WW509.
    /// <para>
    /// Under the system temp rather than the checkout, which is the decision worth stating: an engine
    /// that writes into an adopter's working tree by default is an engine that turns up in their
    /// <c>git status</c>, and a store seeded for a case is not a thing anybody meant to commit.
    /// </para>
    /// <para>
    /// Per project and per fixture, because that is what a launch is keyed by: two fixtures are two
    /// launches with two stores, and two cases sharing one fixture share the window already. The
    /// project's root goes through a hash because a path is not a directory name, and the root's own
    /// folder name rides along so a person looking in temp can tell whose it is.
    /// </para>
    /// </summary>
    /// <param name="root">The project's root.</param>
    public string StagedInto(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);

        var full = System.IO.Path.GetFullPath(root);
        var hashed = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(full.ToLowerInvariant()));

        return System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "winwright",
            $"{Legible(new System.IO.DirectoryInfo(full).Name)}-{Convert.ToHexString(hashed)[..8].ToLowerInvariant()}",
            Legible(Name));
    }

    /// <summary>
    /// Put this fixture's files in that directory, emptied first, and hand back where they went.
    /// <para>
    /// Emptied, because a launch that inherits what the last one left is the global state this field
    /// exists to remove — the same defect as writing into the real user profile, moved somewhere
    /// tidier. A fixture declaring no files still gets the directory, so a variable naming
    /// <see cref="Staged"/> with nothing staged is an empty store of this launch's own, which is a
    /// thing worth asking for rather than a mistake.
    /// </para>
    /// </summary>
    /// <param name="root">The project's root, which the declared paths are relative to.</param>
    /// <exception cref="ScenarioRefusedException">Where a declared file is not there to copy.</exception>
    public StagedFixture Stage(string root)
    {
        var into = StagedInto(root);
        if (System.IO.Directory.Exists(into))
            System.IO.Directory.Delete(into, recursive: true);

        System.IO.Directory.CreateDirectory(into);

        foreach (var file in Files)
        {
            var from = System.IO.Path.GetFullPath(System.Environment.ExpandEnvironmentVariables(file.From), root);
            if (!System.IO.File.Exists(from))
            {
                // Named as the fixture wrote it and as it resolved, because the two are the whole
                // question: a reader who sees only one of them cannot tell a missing file from a
                // path that meant something else.
                throw new ScenarioRefusedException(
                    Name, $"it stages '{file.From}', which is not a file — it resolved to {from}");
            }

            // WW524. Where it lands, under the staged directory: the top under its own name for a
            // plain path, or the folder the fixture named. Its folders are made here, because the
            // whole point of naming one is that the application looks one folder down.
            var to = System.IO.Path.Combine(into, Landing(Name, file));
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(to)!);
            System.IO.File.Copy(from, to, overwrite: true);

            if (file.Fill)
                Filled(file, to, root);
        }

        // WW515. A type rather than the path, so the thing a launch is composed from is a fixture
        // that has been staged, and the root it resolves against is the root its files went under
        // by construction. Two calls each taking a root could be handed two.
        return new StagedFixture(this, root, into);
    }

    /// <summary>
    /// Where a staged file lands, relative to the staged directory and normalised, or a refusal where
    /// it would land outside it. WW524.
    /// <para>
    /// Refused absolute and refused climbing out, because the staged directory is emptied before
    /// every launch: a destination outside it is a file this run writes into somebody's machine and
    /// never cleans, which is the global state staging exists to remove.
    /// </para>
    /// </summary>
    private static string Landing(string called, StagedFile file)
    {
        var to = file.Lands.Replace('/', System.IO.Path.DirectorySeparatorChar);
        if (to.Length == 0 || System.IO.Path.IsPathRooted(to))
        {
            throw new ScenarioRefusedException(
                called, $"it stages '{file}', and 'to' has to be a path under the staged directory, such as quickshell/sessions.json");
        }

        var parts = to.Split(System.IO.Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Any(part => part == ".."))
        {
            throw new ScenarioRefusedException(
                called, $"it stages '{file}', and '..' climbs out of the staged directory, which is emptied before every launch");
        }

        return string.Join(System.IO.Path.DirectorySeparatorChar, parts.Where(part => part != "."));
    }

    /// <summary>
    /// Fill a staged copy's tokens in place: <c>{reach0}</c> and on with the addresses this fixture
    /// reaches, and <see cref="Staged"/> with where the files went. WW527.
    /// <para>
    /// Read as strict UTF-8 and written back with the byte-order mark it came with, so a file the
    /// application parses is changed in the tokens and nowhere else. A file that is not UTF-8 is
    /// refused naming it, because filling it would rewrite bytes nobody asked to change.
    /// </para>
    /// </summary>
    private void Filled(StagedFile file, string staged, string root)
    {
        var bytes = System.IO.File.ReadAllBytes(staged);
        var marked = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        var strict = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: marked, throwOnInvalidBytes: true);

        string text;
        try
        {
            text = strict.GetString(bytes, marked ? 3 : 0, bytes.Length - (marked ? 3 : 0));
        }
        catch (System.Text.DecoderFallbackException)
        {
            throw new ScenarioRefusedException(
                Name, $"it asks to fill '{file.From}', which is not UTF-8 text, so filling it would rewrite bytes nobody asked to change");
        }

        var filled = Offered(Resolved(text, root));
        if (!string.Equals(filled, text, StringComparison.Ordinal))
            System.IO.File.WriteAllText(staged, filled, strict);
    }

    /// <summary><paramref name="value"/> with <see cref="Staged"/> swapped for where the files went.</summary>
    private string Resolved(string value, string root) =>
        value.Contains(Staged, StringComparison.Ordinal)
            ? value.Replace(Staged, StagedInto(root), StringComparison.Ordinal)
            : value;

    /// <summary>A name a directory can be called, for the one in temp a person has to recognise.</summary>
    private static string Legible(string name)
    {
        var letters = name
            .Select(one => System.IO.Path.GetInvalidFileNameChars().Contains(one) || one == ' ' ? '-' : one)
            .ToArray();

        return new string(letters);
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
        ArgumentException.ThrowIfNullOrWhiteSpace(root);

        // WW515. The order WW509 left to whoever called these two. A fixture that names the staged
        // directory is a fixture whose launch resolves a token to a path, and a path that was never
        // staged is a directory that may not exist or may hold what an earlier run left — the
        // application then reports a store it cannot read and the red is about the application.
        //
        // Asked of the fixtures that depend on it and not of every fixture, because that is where
        // the dependency is: one that never mentions the staged directory does not care whether it
        // exists, and a launch that demanded staging from all of them would make `Suite` the only
        // caller that could compose one at all.
        if (NeedsStaging && !System.IO.Directory.Exists(StagedInto(root)))
        {
            throw new ScenarioRefusedException(
                Name,
                $"it names '{Staged}' or stages files, and nothing has staged it — {nameof(Stage)} makes the "
                    + "directory a launch resolves that to");
        }

        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            WorkingDirectory = StartsIn(root),
        };
        // WW522. The addresses this fixture reaches are offered here too, so an argument naming the
        // server is the one the probe found.
        foreach (var argument in Launching())
            start.ArgumentList.Add(Offered(argument));

        // WW509. The token resolves here and not in the declaration: where the files go is a fact
        // about this machine, and a declaration read on one machine is the same declaration on the
        // next. A value naming no token is handed over exactly as written.
        foreach (var variable in Variables)
            start.Environment[variable.Key] = Offered(Resolved(variable.Value, root));

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

        // WW509. Counted rather than listed: a reader of a red wants to know this launch had a store
        // of its own, and which files were in it is the declaration's business.
        var staged = Files.Count > 0 ? $", staging {Files.Count} file(s)" : "";

        // WW522. Named rather than counted: which server a launch needed is what a reader of a hole
        // has to go and start.
        if (Reaches.Count > 0)
            staged += $", reaching {string.Join(", ", Reaches)}";

        // WW519. Last, because it qualifies the store the clauses before it describe.
        return $"{Name}: {sampled}{lent}{held}{started}{staged}{VariableOnlyClause}.";
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

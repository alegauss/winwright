using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Winwright.Projects;

/// <summary>
/// One key <c>winwright.json</c> may carry: what it declares, what its absence means, and what it
/// refuses. WW490.
/// <para>
/// The file is the first thing an adopting repository writes, and what described it was an example.
/// An example is a good start and a bad reference: it cannot say which keys exist beside the ones it
/// shows, what each one falls back to when it is absent, or which of them refuse a value that looks
/// perfectly reasonable — and two of those refusals cost an afternoon each if they are met rather
/// than read.
/// </para>
/// <para>
/// A record rather than prose, for the reason <see cref="Scenarios.Field"/> is one: a description a
/// tool can carry is a description the run enforces, and prose about a key is what a reader reads
/// and a generator cannot. The site's documentation area publishes this list; nothing there is
/// typed.
/// </para>
/// </summary>
/// <param name="Name">The key, spelled as the file spells it.</param>
/// <param name="Under">The object it sits in, or empty where it is at the top level.</param>
/// <param name="Holds">What kind of value it takes, in the words a reader of JSON uses.</param>
/// <param name="Means">What declaring it does.</param>
/// <param name="Absent">What happens when it is not declared, which is never nothing.</param>
/// <param name="Refuses">The value it turns away, or empty where it turns away none.</param>
public sealed record DeclaredKey(
    string Name,
    string Under,
    string Holds,
    string Means,
    string Absent,
    string Refuses)
{
    /// <summary>How this file addresses it: the key, under its object where it has one.</summary>
    public string Addressed => Under.Length > 0 ? $"{Under}.{Name}" : Name;

    /// <summary>The one line a listing of the declaration shows.</summary>
    public override string ToString() =>
        $"{Addressed} ({Holds}): {Means}. Absent: {Absent}."
            + (Refuses.Length > 0 ? $" Refuses: {Refuses}." : "");
}

/// <summary>
/// What is true of a project rather than of a case: the executable, the source root the staleness
/// check compares against, the language files, the default timeouts and the store to fingerprint.
/// A scenario carrying one of these is a scenario that runs on exactly one checkout, which is how
/// a harness becomes unmovable and then unowned — so they are declared once, in <c>winwright.json</c>
/// at the project root, and every relative path in it resolves against that file's own directory.
/// </summary>
public sealed class ProjectDeclaration
{
    /// <summary>The file a project declares itself in, looked for by walking up from a directory.</summary>
    public const string FileName = "winwright.json";

    /// <summary>
    /// Every key this build reads out of <c>winwright.json</c>, in the order a reader meets them:
    /// what the application is, what it ships, how long to wait, and what a run must not do quietly.
    /// <para>
    /// WW490. Held against <see cref="Shape"/> in both directions by the suite, so a key this build
    /// reads and this list does not name is a red rather than a row missing from a page — which is
    /// the same arrangement <c>Cooperating</c> makes for the verbs and for the same reason: a
    /// catalogue nobody fails over is a catalogue that falls behind.
    /// </para>
    /// <para>
    /// Every key is optional, which is what makes an incomplete declaration safe to start from: a
    /// reading that needs one this file does not declare is recorded as <em>not taken</em> rather
    /// than skipped, and the four paths refuse at the moment something asks for them instead of at
    /// load. So <c>Absent</c> is never "nothing happens" — it is what the run does instead.
    /// </para>
    /// </summary>
    public static IReadOnlyList<DeclaredKey> Keys { get; } = new ReadOnlyCollection<DeclaredKey>(
    [
        new(
            "executable",
            "",
            "a path",
            "the binary a launch starts, resolved against this file's own directory",
            "asking for it refuses, naming the key and that it was launching the application under "
                + "test; a run that attaches to a process already running never asks",
            ""),
        new(
            "sourceRoot",
            "",
            "a path",
            "the source a staleness check compares the built binary against",
            "the staleness reading is recorded as not taken, so a run cannot report on a build from "
                + "last week and say nothing about it",
            ""),
        new(
            "fingerprintStore",
            "",
            "a path",
            "the region of the machine a run must leave exactly as it found it — where the "
                + "application keeps the settings and caches it owns",
            "nothing is fingerprinted, so a run that drove a path writing a real setting finishes "
                + "quietly",
            ""),
        new(
            "captures",
            "",
            "a path to a directory",
            "where a picture a case asks for is written; the case's own name is the folder inside it, "
                + "so two cases asking for 'the menu' do not answer each other",
            "a capture step refuses at the door rather than after launching the application",
            ""),
        new(
            "languageFiles",
            "",
            "an array of paths",
            "the strings this application ships, which is what lets a locator say {a.key} and an "
                + "expectation derive a label instead of typing words a translation rewrites",
            "there is no well to derive from, and every claim that reads one is refused where it is "
                + "written rather than at run time",
            ""),
        new(
            "loading",
            "",
            "an array of keys",
            "the keys of the strings shown while a page is still computing, so a page still saying it "
                + "is loading is a failure rather than a photograph",
            "no page is held to having finished computing",
            "a key none of the languageFiles carries — a check that silently matches nothing reports "
                + "every page as finished forever"),
        new(
            "sourceIgnore",
            "",
            "an array of directory names",
            "what the staleness walk steps past, by simple name at any depth",
            "the names DefaultSourceIgnore lists stand — build output and tooling state, which is the "
                + "set that matters: with bin counted as source the binary is always newer than itself",
            ""),
        new(
            "timeouts",
            "",
            "an object of names to milliseconds",
            "how long this project is willing to wait, by name, declared once rather than typed into "
                + "the case that needed it",
            "the names Timeouts.Defaults seeds stand, and a declared name nothing seeds is simply "
                + "this project's own",
            "a value that is not a positive number"),
        new(
            "language",
            "",
            "an object",
            "how the run works out which language the application is actually in",
            "the display language is the whole of the resolution",
            ""),
        new(
            "attempts",
            "",
            "a whole number",
            "how many times a flaky act may be attempted — a fact about this project rather than "
                + "about a case",
            "Retry.DefaultCap stands",
            "a number outside Retry's own bounds, named against this file rather than thrown once "
                + "per step about an argument out of range"),
        new(
            "destructive",
            "",
            "an array of {\"id\"} or {\"key\"} entries",
            "the entries that end the run, which no step may touch without saying it meant to",
            "nothing is destructive, and no step has to say it meant it",
            "a bare name where the project ships more than one language, a name being exactly the "
                + "field a translation rewrites"),
        new(
            "reportedSets",
            "",
            "an object of names to argument arrays",
            "the sets the application reports about itself, each with the arguments that make it "
                + "print one per line — for a set that is this machine's data rather than a string "
                + "the product ships",
            "'covers' has only the language files to derive from",
            "an entry with no name, or one with no arguments, since nothing would then say how the "
                + "application is asked"),
        new(
            "reportedValues",
            "",
            "an object of names to argument arrays",
            "the single values the application reports about itself, for a fact about this machine "
                + "that no case may type",
            "'expectReported' has no well to ask",
            "an entry with no name, or one with no arguments"),
        new(
            "preferenceFile",
            "language",
            "a path",
            "the JSON file this application saves the user's chosen language in",
            "the display language is the whole of the resolution",
            ""),
        new(
            "preferenceKey",
            "language",
            "a key, dotted for a nested one",
            "where inside that file the chosen language sits",
            "the preference file is not read",
            ""),
        new(
            "fallback",
            "language",
            "a language tag",
            "the language the application itself falls back to when it ships no strings for the one "
                + "the machine is in",
            "there is no fallback to make, and reading a label in a language nobody declared is "
                + "refused rather than answered in English",
            ""),
    ]);

    /// <summary>What a project gets without declaring anything: build output and tooling state.</summary>
    public static IReadOnlyList<string> DefaultSourceIgnore { get; } =
        new ReadOnlyCollection<string>(["bin", "obj", ".git", ".vs", ".idea", "node_modules", "TestResults", ".roadkeep"]);

    private static readonly JsonSerializerOptions ReadAs = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string? executable;
    private readonly string? sourceRoot;
    private readonly string? fingerprintStore;
    private readonly string? captures;

    private ProjectDeclaration(string path, Shape shape)
    {
        Path = path;
        Root = System.IO.Path.GetDirectoryName(path)!;
        executable = Resolve(shape.Executable);
        sourceRoot = Resolve(shape.SourceRoot);
        fingerprintStore = Resolve(shape.FingerprintStore);
        captures = Resolve(shape.Captures);
        LanguageFiles = new ReadOnlyCollection<string>(
            (shape.LanguageFiles ?? []).Select(Resolve).OfType<string>().ToList());
        Loading = new ReadOnlyCollection<string>(
            (shape.Loading ?? []).Select(one => one?.Trim() ?? "").Where(one => one.Length > 0).ToList());
        SourceIgnore = new ReadOnlyCollection<string>(
            (shape.SourceIgnore ?? DefaultSourceIgnore)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .ToList());
        LanguagePreferenceFile = Resolve(shape.Language?.PreferenceFile);
        LanguagePreferenceKey = string.IsNullOrWhiteSpace(shape.Language?.PreferenceKey)
            ? null
            : shape.Language.PreferenceKey.Trim();
        LanguageFallback = string.IsNullOrWhiteSpace(shape.Language?.Fallback)
            ? null
            : shape.Language.Fallback.Trim();
        Attempts = shape.Attempts is { } declared ? Capped(declared, path) : Acting.Retry.DefaultCap;
        Timeouts = Timeouts.Declared(shape.Timeouts, path);
        Destructive = Destructive.Of(shape.Destructive, LanguageFiles, path);

        // WW260, and WW294 beside it. Both wells are a name and the arguments that answer it, so both
        // are read by one thing: a second spelling of that would be a second set of rules to keep in
        // step. What differs between them is what comes back, which is the reader's business and not
        // the declaration's.
        ReportedSets = new ReadOnlyDictionary<string, IReadOnlyList<string>>(
            Asked(shape.ReportedSets, "reportedSet", path));

        ReportedValues = new ReadOnlyDictionary<string, IReadOnlyList<string>>(
            Asked(shape.ReportedValues, "reportedValue", path));
    }

    /// <summary>
    /// One well of things the application reports, by name, with the arguments that ask for each.
    /// WW294: written once because the set well and the value well differ in what they read back and
    /// in nothing about how they are declared.
    /// </summary>
    /// <param name="declared">What the file said, or null where it said nothing.</param>
    /// <param name="what">What one of them is called, as a refusal spells it.</param>
    /// <param name="path">The declaration file, for the refusal.</param>
    private static Dictionary<string, IReadOnlyList<string>> Asked(
        Dictionary<string, IReadOnlyList<string>>? declared, string what, string path)
    {
        var asked = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var (name, arguments) in declared ?? [])
        {
            var called = name?.Trim() ?? "";
            if (called.Length == 0)
                throw new ArgumentException($"{path} declares a {what} with no name", nameof(declared));

            if (arguments is null || arguments.Count == 0)
            {
                throw new ArgumentException(
                    $"{path} declares the {what} '{called}' with no arguments, so nothing says how the "
                        + "application is asked for it",
                    nameof(declared));
            }

            asked[called] = new ReadOnlyCollection<string>([.. arguments]);
        }

        return asked;
    }

    /// <summary>The declaration file that was read.</summary>
    public string Path { get; }

    /// <summary>The directory it sits in, which every relative path in it is resolved against.</summary>
    public string Root { get; }

    /// <summary>The language files this project ships, resolved. Empty where none are declared.</summary>
    public IReadOnlyList<string> LanguageFiles { get; }

    /// <summary>
    /// The keys of the strings this application shows while a page is still computing.
    /// <para>
    /// WW43. Keys and never the text: a phrase typed here is one a translation rewrites, and a
    /// check comparing against it starts matching nothing on the day somebody ships another
    /// language. The text is read from <see cref="LanguageFiles" /> for whichever language the run
    /// resolved, and a key none of them carries refuses rather than matching nothing.
    /// </para>
    /// </summary>
    public IReadOnlyList<string> Loading { get; }

    /// <summary>
    /// Directory names the staleness check walks past, by simple name at any depth. Build output
    /// is the one that matters: with `bin` counted as source, the binary is always newer than
    /// itself and nothing is ever stale, which is the check quietly answering nothing.
    /// </summary>
    public IReadOnlyList<string> SourceIgnore { get; }

    /// <summary>How long this project waits, by name, with the engine's defaults folded under it.</summary>
    public Timeouts Timeouts { get; }

    /// <summary>
    /// The entries that end the run, named here because which one quits is a fact about the
    /// application. Empty where the project declares none, and then nothing is refused.
    /// </summary>
    public Destructive Destructive { get; }

    /// <summary>
    /// The JSON file this application saves the user's chosen language in, resolved. Null where
    /// the project declares none, and then the display language is the whole of the resolution.
    /// </summary>
    public string? LanguagePreferenceFile { get; }

    /// <summary>The key inside that file, dotted for a nested one. Null where none is declared.</summary>
    public string? LanguagePreferenceKey { get; }

    /// <summary>
    /// The language the application itself falls back to when it ships no strings for the one the
    /// machine is in. Null where the project declares none, and then there is no fallback to make
    /// — reading a label in a language nobody declared is refused rather than answered in English.
    /// </summary>
    public string? LanguageFallback { get; }

    /// <summary>
    /// How many times a flaky act may be attempted. A number about this project rather than about
    /// a case, so it is declared once here and never typed into the scenario that needed it.
    /// <para>
    /// Bounded at both ends by <see cref="Acting.Retry"/>'s own limits, and both ends checked here.
    /// WW216: only the lower one was. A project writing nine loaded, said nothing, and then threw
    /// once per step when the engine handed the number to the bounded retry — so a six-step case
    /// reported six breakages about an argument out of range, none of which named the file the nine
    /// was typed into. Two rules about one value in two places, and the weaker one ran where a
    /// person could act on it.
    /// </para>
    /// </summary>
    public int Attempts { get; }

    /// <summary>The application under test.</summary>
    /// <exception cref="DeclarationMissingException">Where the project declares none.</exception>
    public string Executable => Require(executable, "executable", "launching the application under test");

    /// <summary>
    /// The sets the application reports about itself, by name, each holding the arguments that make it
    /// print one per line.
    /// <para>
    /// WW260. The second well a derived set can come from, and it exists because the first one is the
    /// wrong place for some sets. `covers` derives from the language files, which is right for every
    /// tab header the strings declare and wrong for what claude-tray's menu case counts: profiles are
    /// this machine's data, the number is whatever this machine has, and neither half is in a strings
    /// file. Typing it is the defect `covers` exists to refuse, one well over — a case asserting two
    /// profile entries goes on asserting two after a third is added.
    /// </para>
    /// <para>
    /// Declared here rather than named in a case for the same reason a strings file is: how the
    /// application is asked is the project's business, and a case naming the flag would be a case that
    /// runs on one checkout.
    /// </para>
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ReportedSets { get; }

    /// <summary>
    /// The single values the application reports about itself, by name, each holding the arguments
    /// that make it print one.
    /// <para>
    /// WW294. The scalar beside <see cref="ReportedSets"/>, and it exists because that one is the
    /// wrong shape for most of what an application knows about itself. Measured reading claude-tray's
    /// check script: it pulls eight facts out of one report, and only the first is a set — the rest
    /// are single values, like which profile the icon follows and which one the environment selects.
    /// </para>
    /// <para>
    /// <c>label</c> is the near miss and answers a different question: that derives a value from the
    /// project's <em>strings</em>, which is right for a word the product ships and wrong for a fact
    /// about this machine. A case can type neither — an account name passes on the desk it was written
    /// on and fails on every other.
    /// </para>
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ReportedValues { get; }

    /// <summary>The source root a staleness check compares the built binary against.</summary>
    /// <exception cref="DeclarationMissingException">Where the project declares none.</exception>
    public string SourceRoot => Require(sourceRoot, "sourceRoot", "checking whether the binary is stale");

    /// <summary>
    /// The region of the machine a run must leave exactly as it found it — where an application keeps
    /// the settings and caches it owns.
    /// <para>
    /// WW83 corrected the sentence this used to carry, which said image fingerprints and a capture:
    /// nothing has ever read it for that. <see cref="Verdicts.Preamble" /> is its one reader, and what
    /// it does with it is take a fingerprint before the run and again after, so a run that drove a
    /// path which writes a real setting says so instead of finishing quietly.
    /// </para>
    /// </summary>
    /// <exception cref="DeclarationMissingException">Where the project declares none.</exception>
    public string FingerprintStore =>
        Require(fingerprintStore, "fingerprintStore", "reading whether a run left the machine as it found it");

    /// <summary>
    /// Where a picture a case asks for is written.
    /// <para>
    /// WW336. The project's and never the case's, which is the whole of why the verb waited for a
    /// task of its own: every other field a case carries is derived precisely so a case means the
    /// same thing on the next machine, and a path typed into one is the plainest way to break that.
    /// A case names what to call the picture; where the pictures go is a fact about the checkout.
    /// </para>
    /// <para>
    /// A directory rather than a file, because a case takes as many as it means to and the run has
    /// to keep them apart — the case's own name is the folder inside this one, so two cases asking
    /// for "the menu" do not answer each other.
    /// </para>
    /// </summary>
    /// <exception cref="DeclarationMissingException">Where the project declares none.</exception>
    public string Captures =>
        Require(captures, "captures", "writing a picture a case asked for");

    /// <summary>Whether the project declared a value for that key at all, without refusing.</summary>
    public bool Declares(string key) => key switch
    {
        "captures" => captures is not null,
        "executable" => executable is not null,
        "sourceRoot" => sourceRoot is not null,
        "fingerprintStore" => fingerprintStore is not null,
        "languageFiles" => LanguageFiles.Count > 0,
        "loading" => Loading.Count > 0,
        "language.fallback" => LanguageFallback is not null,
        "destructive" => Destructive.Any,
        _ => Timeouts.All.ContainsKey(key.StartsWith("timeouts.", StringComparison.Ordinal) ? key[9..] : key),
    };

    /// <summary>Read one declaration file.</summary>
    /// <exception cref="DeclarationMissingException">Where the file is not there.</exception>
    public static ProjectDeclaration Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var full = System.IO.Path.GetFullPath(path);
        if (!File.Exists(full))
            throw new DeclarationMissingException(MissingDeclaration.NotAtThePathNamed, FileName, full, "every scenario in this project");

        var shape = JsonSerializer.Deserialize<Shape>(File.ReadAllText(full), ReadAs)
            ?? throw new JsonException($"{full} is empty, and an empty declaration declares nothing");

        return new ProjectDeclaration(full, shape);
    }

    /// <summary>
    /// Walk up from <paramref name="startingAt"/> until a declaration turns up. This is what lets
    /// a scenario be moved to another checkout unchanged: it names what it drives, and where that
    /// lives is answered by whichever project the file happens to be sitting in.
    /// </summary>
    /// <exception cref="DeclarationMissingException">Where no ancestor directory declares one.</exception>
    public static ProjectDeclaration Find(string startingAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startingAt);

        var directory = new DirectoryInfo(System.IO.Path.GetFullPath(startingAt));
        for (var walking = directory; walking is not null; walking = walking.Parent)
        {
            var candidate = System.IO.Path.Combine(walking.FullName, FileName);
            if (File.Exists(candidate))
                return Load(candidate);
        }

        throw new DeclarationMissingException(
            MissingDeclaration.NotUpTheTree, FileName, $"{directory.FullName} and every directory above it", "every scenario in this project");
    }

    /// <summary>
    /// The declared cap, or a refusal naming this file and the limit it broke.
    /// <para>
    /// WW216. Both ends, and both of <see cref="Acting.Retry"/>'s own numbers rather than a second
    /// pair written here: a limit transcribed is a limit that drifts, and the reason for the upper
    /// one lives on the type that enforces it — past a handful a cap stops being a cap and becomes
    /// the loop that type exists to refuse.
    /// </para>
    /// </summary>
    private static int Capped(int declared, string path)
    {
        if (declared <= 0)
        {
            throw new ArgumentException(
                $"{path} allows {declared} attempts, and an act nobody may attempt is not an act", nameof(declared));
        }

        if (declared > Acting.Retry.MostAttempts)
        {
            throw new ArgumentException(
                $"{path} allows {declared} attempts, which is not a cap: past {Acting.Retry.MostAttempts} an act is "
                    + "one nobody will ever see fail",
                nameof(declared));
        }

        return declared;
    }

    private string? Resolve(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var expanded = System.Environment.ExpandEnvironmentVariables(path.Trim());
        return System.IO.Path.GetFullPath(expanded, Root);
    }

    private string Require(string? value, string key, string wanted) =>
        value ?? throw new DeclarationMissingException(MissingDeclaration.KeyNotDeclared, key, Path, wanted);

    private sealed record Shape
    {
        [JsonPropertyName("executable")] public string? Executable { get; init; }

        [JsonPropertyName("sourceRoot")] public string? SourceRoot { get; init; }

        [JsonPropertyName("fingerprintStore")] public string? FingerprintStore { get; init; }

        [JsonPropertyName("captures")] public string? Captures { get; init; }

        [JsonPropertyName("languageFiles")] public IReadOnlyList<string>? LanguageFiles { get; init; }

        [JsonPropertyName("loading")] public IReadOnlyList<string>? Loading { get; init; }

        [JsonPropertyName("sourceIgnore")] public IReadOnlyList<string>? SourceIgnore { get; init; }

        [JsonPropertyName("timeouts")] public Dictionary<string, int>? Timeouts { get; init; }

        [JsonPropertyName("language")] public LanguageShape? Language { get; init; }

        [JsonPropertyName("attempts")] public int? Attempts { get; init; }

        [JsonPropertyName("destructive")] public IReadOnlyList<System.Text.Json.JsonElement>? Destructive { get; init; }

        [JsonPropertyName("reportedSets")] public Dictionary<string, IReadOnlyList<string>>? ReportedSets { get; init; }

        [JsonPropertyName("reportedValues")] public Dictionary<string, IReadOnlyList<string>>? ReportedValues { get; init; }
    }

    private sealed record LanguageShape
    {
        [JsonPropertyName("preferenceFile")] public string? PreferenceFile { get; init; }

        [JsonPropertyName("preferenceKey")] public string? PreferenceKey { get; init; }

        [JsonPropertyName("fallback")] public string? Fallback { get; init; }
    }
}

using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Winwright.Projects;

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

    private ProjectDeclaration(string path, Shape shape)
    {
        Path = path;
        Root = System.IO.Path.GetDirectoryName(path)!;
        executable = Resolve(shape.Executable);
        sourceRoot = Resolve(shape.SourceRoot);
        fingerprintStore = Resolve(shape.FingerprintStore);
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

    /// <summary>The source root a staleness check compares the built binary against.</summary>
    /// <exception cref="DeclarationMissingException">Where the project declares none.</exception>
    public string SourceRoot => Require(sourceRoot, "sourceRoot", "checking whether the binary is stale");

    /// <summary>Where image fingerprints are kept between runs.</summary>
    /// <exception cref="DeclarationMissingException">Where the project declares none.</exception>
    public string FingerprintStore => Require(fingerprintStore, "fingerprintStore", "comparing a capture with the last one");

    /// <summary>Whether the project declared a value for that key at all, without refusing.</summary>
    public bool Declares(string key) => key switch
    {
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

        [JsonPropertyName("languageFiles")] public IReadOnlyList<string>? LanguageFiles { get; init; }

        [JsonPropertyName("loading")] public IReadOnlyList<string>? Loading { get; init; }

        [JsonPropertyName("sourceIgnore")] public IReadOnlyList<string>? SourceIgnore { get; init; }

        [JsonPropertyName("timeouts")] public Dictionary<string, int>? Timeouts { get; init; }

        [JsonPropertyName("language")] public LanguageShape? Language { get; init; }

        [JsonPropertyName("attempts")] public int? Attempts { get; init; }

        [JsonPropertyName("destructive")] public IReadOnlyList<System.Text.Json.JsonElement>? Destructive { get; init; }
    }

    private sealed record LanguageShape
    {
        [JsonPropertyName("preferenceFile")] public string? PreferenceFile { get; init; }

        [JsonPropertyName("preferenceKey")] public string? PreferenceKey { get; init; }

        [JsonPropertyName("fallback")] public string? Fallback { get; init; }
    }
}

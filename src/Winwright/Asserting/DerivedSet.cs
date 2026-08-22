using System.Collections.ObjectModel;
using System.Text.Json;

using Winwright.Projects;
using Winwright.Verdicts;

namespace Winwright.Asserting;

/// <summary>Raised where a set cannot be derived, so nothing is asserted against a guess.</summary>
public sealed class UnderivableSetException : InvalidOperationException
{
    /// <summary>Say what could not be derived and from where.</summary>
    public UnderivableSetException(string message)
        : base(message)
    {
    }

    /// <summary>Unused. Present because an exception with no default shapes is awkward to catch.</summary>
    public UnderivableSetException()
        : base("the expected set could not be derived from the project's own strings")
    {
    }

    /// <summary>Unused. Present for the same reason.</summary>
    public UnderivableSetException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>What a set derived from the project's strings turned out to be, compared with what was read.</summary>
/// <param name="Set">The set that was expected, and where it came from.</param>
/// <param name="Matched">Values that were expected and read, in the set's own order.</param>
/// <param name="Missing">Values the strings declare that nothing read.</param>
/// <param name="Unexpected">Values that were read and the strings do not declare.</param>
public sealed record SetComparison(
    DerivedSet Set,
    IReadOnlyList<string> Matched,
    IReadOnlyList<string> Missing,
    IReadOnlyList<string> Unexpected)
{
    /// <summary>Whether everything the strings declare was read, and nothing else was.</summary>
    public bool Held => Missing.Count == 0 && Unexpected.Count == 0;

    /// <summary>
    /// What was expected, what was read, and which way they differ — never the word <em>all</em>
    /// while anything is missing, since that is the sentence claude-tray printed against a window
    /// carrying one more tab than the expectation had ever heard of.
    /// </summary>
    public string Sentence()
    {
        if (Held)
            return $"{Set.Named}: all {Matched.Count} of {Listed(Matched)} were read, {Set.Source}.";

        var parts = new List<string>();
        if (Missing.Count > 0)
            parts.Add($"{Listed(Missing)} {(Missing.Count == 1 ? "is" : "are")} declared and was not read");

        if (Unexpected.Count > 0)
            parts.Add($"{Listed(Unexpected)} {(Unexpected.Count == 1 ? "was" : "were")} read and is declared nowhere");

        return $"{Set.Named}: {string.Join("; ", parts)} — {Matched.Count} of {Set.Expected.Count} matched, {Set.Source}.";
    }

    /// <summary>The result a verdict counts, carrying this sentence as its detail.</summary>
    public AssertionResult AsAssertion() =>
        Held ? AssertionResult.Pass(Set.Named, Sentence()) : AssertionResult.Fail(Set.Named, Sentence());

    private static string Listed(IReadOnlyList<string> values) => string.Join(", ", values.Select(value => $"'{value}'"));
}

/// <summary>
/// An expected set read out of the project's own strings.
/// <para>
/// A hardcoded expected set silently stops covering the thing it was written for. claude-tray's
/// panes case named three tab headers by hand and the window had carried four for some time, so it
/// reported all three tab headers read against a four-tab window and the newest pane had never
/// been asked whether it was in the tree at all.
/// </para>
/// <para>
/// The set is derived from the strings and <em>not</em> from the tree, and that is the whole
/// design rather than a convenience: the tree is what is being asserted, so an expectation read
/// out of it agrees with whatever is there and could never notice a header that had gone missing.
/// There is deliberately no way to build one of these from what was read — the only source is a
/// file, and <see cref="Against"/> is the only door readings come in by.
/// </para>
/// </summary>
public sealed record DerivedSet
{
    private DerivedSet(string named, string source, IReadOnlyList<string> keys, IReadOnlyList<string> expected)
    {
        Named = named;
        Source = source;
        Keys = keys;
        Expected = expected;
    }

    /// <summary>What this is a set of, as the scenario names it.</summary>
    public string Named { get; }

    /// <summary>Where it was derived from, as a sentence prints it. Named apart from the factory
    /// above so the file a set came from and the door it came through never read as one thing.</summary>
    public string Source { get; }

    /// <summary>The keys it came from, in the order the file spells them.</summary>
    public IReadOnlyList<string> Keys { get; }

    /// <summary>The values, in the same order.</summary>
    public IReadOnlyList<string> Expected { get; }

    /// <summary>
    /// Derive one from a language file, taking every string under <paramref name="under"/>.
    /// </summary>
    /// <param name="named">What the set is of, as the scenario names it.</param>
    /// <param name="languageFile">The JSON file the project ships its strings in.</param>
    /// <param name="under">The key the values sit under, dotted for a nested one.</param>
    /// <exception cref="UnderivableSetException">
    /// Where the file is missing or unreadable, or where the key yields nothing — an empty
    /// expected set passes against an empty window, which is the failure this type exists to stop
    /// and not one it is allowed to reintroduce by deriving nothing quietly.
    /// </exception>
    public static DerivedSet From(string named, string languageFile, string under)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(named);
        ArgumentException.ThrowIfNullOrWhiteSpace(languageFile);
        ArgumentException.ThrowIfNullOrWhiteSpace(under);

        var full = Path.GetFullPath(languageFile);
        if (!File.Exists(full))
            throw new UnderivableSetException($"{named} is derived from {full}, which is not there");

        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(
                File.ReadAllText(full),
                new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            root = document.RootElement.Clone();
        }
        catch (JsonException broken)
        {
            throw new UnderivableSetException($"{full} is not readable JSON: {broken.Message}");
        }

        var key = under.Trim();
        var found = Nested(root, key) ?? Flat(root, key);
        var where = $"derived from '{key}' in {Path.GetFileName(full)}";

        if (found is null || found.Count == 0)
            throw new UnderivableSetException(
                $"{named}: '{key}' in {full} declares no strings, and an empty expected set is met by an "
                    + "empty window — which is the hole this set exists to close");

        return new DerivedSet(
            named.Trim(),
            where,
            new ReadOnlyCollection<string>(found.Select(pair => pair.Key).ToList()),
            new ReadOnlyCollection<string>(found.Select(pair => pair.Value).ToList()));
    }

    /// <summary>
    /// The same, from the one language file the project declares. Refused where it declares
    /// several: which of them the application is showing is a question this cannot answer, and
    /// picking the first would derive an expectation in a language nobody is looking at.
    /// </summary>
    public static DerivedSet From(string named, ProjectDeclaration declaration, string under)
    {
        ArgumentNullException.ThrowIfNull(declaration);

        return declaration.LanguageFiles.Count switch
        {
            0 => throw new UnderivableSetException(
                $"{named} is derived from the project's strings and {declaration.Path} declares no languageFiles"),
            1 => From(named, declaration.LanguageFiles[0], under),
            _ => throw new UnderivableSetException(
                $"{named}: {declaration.Path} declares {declaration.LanguageFiles.Count} language files, and which "
                    + "one the application is showing is not answerable here — name the file"),
        };
    }

    /// <summary>
    /// Compare the set with what was read. The only door readings come in by, which is what keeps
    /// the expectation from being derived from the thing it is asserting.
    /// </summary>
    /// <param name="read">What the tree actually held.</param>
    public SetComparison Against(IEnumerable<string> read)
    {
        ArgumentNullException.ThrowIfNull(read);

        var seen = read.Where(value => value is not null).ToList();
        var wanted = new HashSet<string>(Expected, StringComparer.Ordinal);
        var found = new HashSet<string>(seen, StringComparer.Ordinal);

        return new SetComparison(
            this,
            new ReadOnlyCollection<string>(Expected.Where(found.Contains).ToList()),
            new ReadOnlyCollection<string>(Expected.Where(value => !found.Contains(value)).ToList()),
            new ReadOnlyCollection<string>(seen.Where(value => !wanted.Contains(value)).Distinct(StringComparer.Ordinal).ToList()));
    }

    private static List<KeyValuePair<string, string>>? Nested(JsonElement root, string key)
    {
        var element = root;
        foreach (var step in key.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(step, out element))
                return null;
        }

        if (element.ValueKind != JsonValueKind.Object)
            return null;

        return element.EnumerateObject()
            .Where(property => property.Value.ValueKind == JsonValueKind.String)
            .Select(property => new KeyValuePair<string, string>(
                $"{key}.{property.Name}", property.Value.GetString()!))
            .ToList();
    }

    private static List<KeyValuePair<string, string>>? Flat(JsonElement root, string key)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return null;

        // The other shape a strings file comes in: one flat object with dotted names. Reached
        // only when the nested walk found nothing, so a file using both is read the way it nests.
        var prefix = $"{key}.";
        return root.EnumerateObject()
            .Where(property => property.Value.ValueKind == JsonValueKind.String
                && property.Name.StartsWith(prefix, StringComparison.Ordinal))
            .Select(property => new KeyValuePair<string, string>(property.Name, property.Value.GetString()!))
            .ToList();
    }
}

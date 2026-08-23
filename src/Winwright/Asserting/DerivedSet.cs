using System.Collections.ObjectModel;
using System.Text.Json;

using Winwright.Projects;
using Winwright.Tracing;
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

/// <summary>Why a string under the key is not a member of the set derived from it.</summary>
public enum LeftOutBecause
{
    /// <summary>It carries a placeholder, so no exact read could ever match it.</summary>
    CarriesAPlaceholder,

    /// <summary>
    /// It is a note rather than a string. JSON has no comments, so a strings file that wants one
    /// writes a key nobody reads — and the derivation took it as something a window should show.
    /// </summary>
    IsANote,
}

/// <summary>A string the strings declare that the derived set does not take.</summary>
/// <param name="Key">The key it sits under.</param>
/// <param name="Value">What it says, placeholder and all.</param>
/// <param name="Where">The file and line it is declared on.</param>
/// <param name="Why">Which of the two reasons it was left out for.</param>
public sealed record LeftOut(string Key, string Value, Provenance Where, LeftOutBecause Why)
{
    /// <summary>The one line a source or a refusal names it by.</summary>
    public override string ToString() =>
        Where.Known ? $"'{Key}' = '{Value}' ({Where})" : $"'{Key}' = '{Value}'";
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

        // The missing ones carry their line and the unexpected ones cannot: a value the strings
        // declare has somewhere to be looked at, and one that was only ever read has nowhere.
        if (Missing.Count > 0)
            parts.Add($"{Traced(Missing)} {(Missing.Count == 1 ? "is" : "are")} declared and was not read");

        if (Unexpected.Count > 0)
            parts.Add($"{Listed(Unexpected)} {(Unexpected.Count == 1 ? "was" : "were")} read and is declared nowhere");

        return $"{Set.Named}: {string.Join("; ", parts)} — {Matched.Count} of {Set.Expected.Count} matched, {Set.Source}.";
    }

    /// <summary>The result a verdict counts, carrying this sentence as its detail.</summary>
    public AssertionResult AsAssertion() =>
        Held ? AssertionResult.Pass(Set.Named, Sentence()) : AssertionResult.Fail(Set.Named, Sentence());

    /// <summary>
    /// The step a trace records. WW163: a set derived from the project's own strings is a reading a
    /// run took, and a verdict is the conclusion of readings — one that can answer only the
    /// conclusion leaves a reader with nothing to reach the observation by.
    /// </summary>
    public TraceStep AsTraceStep() => new()
    {
        Verb = "derive",
        Locator = Set.Named,
        Resolved = Set.Source,
        ReadBack = $"{Matched.Count} matched, {Missing.Count} missing, {Unexpected.Count} unexpected",
        From = Set.Origin,
        Verdict = Held ? StepVerdict.Ok : StepVerdict.Failed,
        Detail = Held ? null : Sentence(),
    };

    private static string Listed(IReadOnlyList<string> values) => string.Join(", ", values.Select(value => $"'{value}'"));

    /// <summary>The same, each value followed by the file and line the strings declare it on.</summary>
    private string Traced(IReadOnlyList<string> values) => string.Join(
        ", ",
        values.Select(value =>
        {
            var from = Set.Whence(value);
            return from.Known ? $"'{value}' ({from})" : $"'{value}'";
        }));
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
    private DerivedSet(
        string named,
        string source,
        IReadOnlyList<string> keys,
        IReadOnlyList<string> expected,
        Provenance origin,
        IReadOnlyList<Provenance> origins,
        IReadOnlyList<LeftOut> excluded)
    {
        Named = named;
        Source = source;
        Keys = keys;
        Expected = expected;
        Origin = origin;
        Origins = origins;
        Excluded = excluded;
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
    /// The file and line the whole set was derived from, as a field rather than as prose. It is
    /// what lets a reader check that the expectation came from anywhere at all without opening the
    /// strings file to find out.
    /// </summary>
    public Provenance Origin { get; }

    /// <summary>
    /// Where each expected value is declared, in the same order as <see cref="Expected"/>. A value
    /// whose line could not be numbered still names its file, since the point of the reading is
    /// that no value in the set is one nobody can trace.
    /// </summary>
    public IReadOnlyList<Provenance> Origins { get; }

    /// <summary>
    /// The strings under this key that carry a placeholder, left out of the expectation and
    /// recorded rather than dropped.
    /// <para>
    /// A tree holding <c>Profile: Alexandre</c> can never equal <c>Profile: {name}</c>, so such a
    /// value in the set is a member nothing reads: an unfixable red on every run, or worse a green
    /// where some control happens to render the literal braces. Refusing the whole derivation was
    /// the other reading and it was measured against a real strings file: the fixture's own
    /// <c>labels</c> holds two ordinary strings and one templated one, and refusing would make
    /// that key underivable for the sake of a value nobody could have asserted anyway.
    /// </para>
    /// <para>
    /// So they are excluded and said out loud — in <see cref="Source"/>, which every verdict
    /// sentence carries, so the exclusion appears under each run rather than in this comment. A
    /// count that is not silent is not the defect this project exists about.
    /// </para>
    /// </summary>
    public IReadOnlyList<LeftOut> Excluded { get; }

    /// <summary>The ones left out for carrying a placeholder.</summary>
    public IReadOnlyList<LeftOut> Templated => new ReadOnlyCollection<LeftOut>(
        Excluded.Where(one => one.Why == LeftOutBecause.CarriesAPlaceholder).ToList());

    /// <summary>The ones left out for being a note rather than a string.</summary>
    public IReadOnlyList<LeftOut> Notes => new ReadOnlyCollection<LeftOut>(
        Excluded.Where(one => one.Why == LeftOutBecause.IsANote).ToList());

    /// <summary>Where one expected value came from, or <see cref="Provenance.Unknown"/> for a value not in the set.</summary>
    public Provenance Whence(string value)
    {
        var at = Expected.ToList().IndexOf(value);
        return at < 0 ? Provenance.Unknown : Origins[at];
    }

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

        var declared = found.Select(pair => pair.Key).ToList();

        // One pass over the file for every key at once: a lookup per value would read a strings
        // file as many times as it has strings, which is the cost this whole rule exists to avoid.
        var lines = JsonSource.LinesOf(full, [key, .. declared]);

        var excluded = found
            .Where(pair => Why(pair.Key, pair.Value) is not null)
            .Select(pair => new LeftOut(
                pair.Key,
                pair.Value,
                Provenance.InFile(full, lines.GetValueOrDefault(pair.Key), pair.Key),
                Why(pair.Key, pair.Value)!.Value))
            .ToList();

        var kept = found.Where(pair => Why(pair.Key, pair.Value) is null).ToList();
        if (kept.Count == 0)
        {
            // The same rule as an empty key, said as what it is: nothing under here is a string a
            // window could show, so the set that survives is empty and an empty set is met by an
            // empty window. Named apart from "declares no strings" because the remedy differs.
            throw new UnderivableSetException(
                $"{named}: nothing under '{key}' in {full} is a string an exact read could match "
                    + $"({string.Join("; ", excluded.Select(one => one.ToString()))})");
        }

        var keys = kept.Select(pair => pair.Key).ToList();

        return new DerivedSet(
            named.Trim(),
            Derivation(where, excluded),
            new ReadOnlyCollection<string>(keys),
            new ReadOnlyCollection<string>(kept.Select(pair => pair.Value).ToList()),
            Provenance.InFile(full, lines.GetValueOrDefault(key), key),
            new ReadOnlyCollection<Provenance>(
                keys.Select(one => Provenance.InFile(full, lines.GetValueOrDefault(one), one)).ToList()),
            new ReadOnlyCollection<LeftOut>(excluded));
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

    /// <summary>
    /// Why one string is not a member, or null where it is one.
    /// <para>
    /// Two reasons and no third. A placeholder can never be matched by an exact read; a note is not
    /// a string the application shows at all — JSON has no comments, so a strings file that wants
    /// one writes a key nobody reads, and the derivation took both notes in this repository's own
    /// fixture as things a window should display.
    /// </para>
    /// </summary>
    private static LeftOutBecause? Why(string key, string value)
    {
        if (IsANote(key))
            return LeftOutBecause.IsANote;

        return Labels.CarriesAPlaceholder(value) ? LeftOutBecause.CarriesAPlaceholder : null;
    }

    /// <summary>
    /// Whether a key is the convention for a comment rather than a name for a string.
    /// <para>
    /// Read off the last segment, since a nested key arrives here dotted. Four spellings of one
    /// convention: <c>//</c> is the common one and <c>//2</c> the way a file carrying two of them
    /// writes the second, and the underscore and dollar forms are the same idea from other
    /// ecosystems. A project that genuinely ships a label under a key called <c>//</c> does not
    /// exist, and one that did would be told by the source sentence rather than left guessing.
    /// </para>
    /// </summary>
    private static bool IsANote(string key)
    {
        var last = key.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) is { Length: > 0 } steps
            ? steps[^1]
            : key.Trim();

        return last.StartsWith("//", StringComparison.Ordinal)
            || string.Equals(last, "_comment", StringComparison.OrdinalIgnoreCase)
            || string.Equals(last, "$comment", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Where the set came from, with what it left out and why. In the source rather than in a
    /// comment: every verdict prints this sentence, so the exclusion is read under each run.
    /// </summary>
    private static string Derivation(string where, IReadOnlyList<LeftOut> excluded)
    {
        if (excluded.Count == 0)
            return where;

        var parts = new List<string>();
        foreach (var why in new[] { LeftOutBecause.CarriesAPlaceholder, LeftOutBecause.IsANote })
        {
            var these = excluded.Where(one => one.Why == why).ToList();
            if (these.Count == 0)
                continue;

            var said = why == LeftOutBecause.CarriesAPlaceholder ? "carrying a placeholder" : "a note and not a string";
            parts.Add($"{these.Count} {said} ({string.Join("; ", these.Select(one => $"'{one.Key}'"))})");
        }

        return $"{where}, less {string.Join(" and ", parts)}";
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

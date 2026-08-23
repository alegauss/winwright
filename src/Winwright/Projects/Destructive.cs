using System.Collections.ObjectModel;
using System.Text.Json;

namespace Winwright.Projects;

/// <summary>How a project spelled a destructive entry, which decides what a translation can do to it.</summary>
public enum DeclaredBy
{
    /// <summary>The automation id: the one field the application controls and nobody translates.</summary>
    Id,

    /// <summary>A key in the project's own strings, resolved to whatever each language shows.</summary>
    Key,

    /// <summary>The displayed name. Refused where the project ships more than one language.</summary>
    Name,
}

/// <summary>One entry a project declares destructive, and what it matches.</summary>
/// <param name="Declared">The entry as the project spelled it.</param>
/// <param name="By">Which field it addresses.</param>
/// <param name="Shows">
/// The texts a key resolves to, one per language file that carries it. Empty for the other two.
/// </param>
public sealed record DestructiveEntry(string Declared, DeclaredBy By, IReadOnlyList<string> Shows)
{
    /// <summary>Whether a translation can move it out from under the guard.</summary>
    public bool SurvivesTranslation => By != DeclaredBy.Name;

    /// <summary>The one phrase a report names it by.</summary>
    public override string ToString() => By switch
    {
        DeclaredBy.Id => $"#{Declared}",
        DeclaredBy.Key => Shows.Count == 0
            ? $"'{Declared}' (a key the strings do not carry)"
            : $"'{Declared}' showing {string.Join(", ", Shows.Select(one => $"\"{one}\""))}",
        _ => $"\"{Declared}\"",
    };
}

/// <summary>
/// The entries this project says end the run, named once beside the executable and the timeouts.
/// <para>
/// The acting block's criterion said destructive entries are named in the scenario and reached only
/// by traversal. The second half shipped and the route beside it stayed wide open: the general
/// invoke pressed a menu item called Quit exactly as willingly as one called Open.
/// </para>
/// <para>
/// WW134: and then the guard spoke one language. A declared name is the field a translation
/// rewrites, so a project declaring "Quit" was guarded on an English desk and unguarded the moment
/// the same application came up in pt-BR showing "Sair" — unguarded silently, because a name that
/// matched nothing looks exactly like a name that was never dangerous. The failure mode is the
/// worst available: the run presses the entry that ends the run, on the machine where somebody was
/// least expecting it.
/// </para>
/// <para>
/// So an entry is declared by something a translation cannot move — the automation id, or a key the
/// project's own strings resolve — and a bare name is refused where the project ships more than one
/// language. The rule underneath is worth keeping: a safety check compared against text a person
/// sees is a safety check with an expiry date, and the expiry is whenever somebody translates the
/// application.
/// </para>
/// </summary>
public sealed class Destructive
{
    private readonly IReadOnlyList<DestructiveEntry> entries;

    private Destructive(IReadOnlyList<DestructiveEntry> entries)
    {
        this.entries = entries;
    }

    /// <summary>A project that declares none. Nothing is refused, and nothing pretends otherwise.</summary>
    public static Destructive None { get; } = new(new ReadOnlyCollection<DestructiveEntry>([]));

    /// <summary>Every entry named, in the order the project named them.</summary>
    public IReadOnlyList<DestructiveEntry> Entries => entries;

    /// <summary>Whether this project names any at all.</summary>
    public bool Any => entries.Count > 0;

    /// <summary>
    /// Read a declared list.
    /// </summary>
    /// <param name="declared">
    /// The entries. A string addresses the automation id or the displayed name; an object says
    /// which — <c>{"id": …}</c>, <c>{"key": …}</c> or <c>{"name": …}</c>.
    /// </param>
    /// <param name="languageFiles">
    /// The project's strings, which is what a key is resolved against and what decides whether a
    /// name may be used at all.
    /// </param>
    /// <param name="declaredIn">The declaration file, so a refusal says where to go.</param>
    /// <exception cref="ArgumentException">
    /// Where an entry could only ever be matched by a name and the project ships more than one
    /// language, so the guard would hold in one of them and quietly stop holding in the rest.
    /// </exception>
    public static Destructive Of(
        IEnumerable<JsonElement>? declared,
        IReadOnlyList<string>? languageFiles = null,
        string declaredIn = "the project declaration")
    {
        if (declared is null)
            return None;

        var files = languageFiles ?? [];
        var read = new List<DestructiveEntry>();

        foreach (var element in declared)
        {
            var entry = One(element, files, declaredIn);
            if (entry is null)
                continue;

            if (read.Exists(one => string.Equals(one.Declared, entry.Declared, StringComparison.OrdinalIgnoreCase)))
                continue;

            read.Add(entry);
        }

        return read.Count == 0 ? None : new Destructive(new ReadOnlyCollection<DestructiveEntry>(read));
    }

    /// <summary>The same, from plain names. For a caller with no project and no languages behind it.</summary>
    /// <param name="declared">The names or ids.</param>
    public static Destructive Of(IEnumerable<string>? declared)
    {
        if (declared is null)
            return None;

        var read = declared
            .Where(one => !string.IsNullOrWhiteSpace(one))
            .Select(one => one.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(one => new DestructiveEntry(one, DeclaredBy.Name, []))
            .ToList();

        return read.Count == 0 ? None : new Destructive(new ReadOnlyCollection<DestructiveEntry>(read));
    }

    /// <summary>
    /// The declared entry this element is, or null where it is none of them.
    /// <para>
    /// An id is compared exactly, being a token the application chose. A name is compared without
    /// case, because a menu writes <c>Quit</c> and a declaration writes <c>quit</c> and neither
    /// author is wrong — and a key is compared against every language it resolves in, so the guard
    /// holds whichever one the application is showing rather than the one it was written on.
    /// </para>
    /// </summary>
    /// <param name="name">What the element is called.</param>
    /// <param name="automationId">Its automation id, or empty where it has none.</param>
    public string? Matched(string? name, string? automationId)
    {
        foreach (var entry in entries)
        {
            if (Matches(entry, name, automationId))
                return entry.Declared;
        }

        return null;
    }

    /// <summary>What this project refuses, in the one sentence a report prints.</summary>
    public string Sentence() => Any
        ? $"{entries.Count} entr{(entries.Count == 1 ? "y is" : "ies are")} declared destructive: "
            + string.Join(", ", entries.Select(one => one.ToString())) + "."
        : "this project declares no destructive entry, so nothing here is refused.";

    /// <summary>The list as a report names it.</summary>
    public override string ToString() => Sentence();

    private static bool Matches(DestructiveEntry entry, string? name, string? automationId) => entry.By switch
    {
        DeclaredBy.Id => !string.IsNullOrEmpty(automationId)
            && string.Equals(entry.Declared, automationId, StringComparison.Ordinal),
        DeclaredBy.Key => !string.IsNullOrEmpty(name)
            && entry.Shows.Any(shown => string.Equals(shown, name.Trim(), StringComparison.OrdinalIgnoreCase)),
        _ => Named(entry.Declared, name, automationId),
    };

    /// <summary>
    /// A bare declaration matches either field, which is what it meant before there was a way to
    /// say which — and is why it is refused outright once a second language is in play.
    /// </summary>
    private static bool Named(string declared, string? name, string? automationId) =>
        (!string.IsNullOrEmpty(automationId) && string.Equals(declared, automationId, StringComparison.Ordinal))
        || (!string.IsNullOrEmpty(name) && string.Equals(declared, name.Trim(), StringComparison.OrdinalIgnoreCase));

    private static DestructiveEntry? One(JsonElement element, IReadOnlyList<string> files, string declaredIn)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            var bare = element.GetString()?.Trim() ?? "";
            if (bare.Length == 0)
                return null;

            // Refused rather than resolved: a bare entry may be matched by name, and a name is what
            // a translation rewrites. One language and it is unambiguous enough to keep working;
            // two and the guard would hold in whichever language it was written on.
            if (files.Count > 1)
            {
                throw new ArgumentException(
                    $"{declaredIn} declares \"{bare}\" destructive and ships {files.Count} languages, so a "
                        + "name that matches today stops matching in the others — write it as "
                        + "{\"id\": \"…\"} or {\"key\": \"…\"}",
                    nameof(declaredIn));
            }

            return new DestructiveEntry(bare, DeclaredBy.Name, []);
        }

        if (element.ValueKind != JsonValueKind.Object)
            return null;

        if (Text(element, "id") is { Length: > 0 } id)
            return new DestructiveEntry(id, DeclaredBy.Id, []);

        if (Text(element, "key") is { Length: > 0 } key)
            return new DestructiveEntry(key, DeclaredBy.Key, Resolved(key, files));

        if (Text(element, "name") is { Length: > 0 } named)
        {
            if (files.Count > 1)
            {
                throw new ArgumentException(
                    $"{declaredIn} declares the name \"{named}\" destructive and ships {files.Count} languages, "
                        + "so the guard would hold in one of them and quietly stop holding in the rest — write it "
                        + "as {\"id\": \"…\"} or {\"key\": \"…\"}",
                    nameof(declaredIn));
            }

            return new DestructiveEntry(named, DeclaredBy.Name, []);
        }

        return null;
    }

    /// <summary>
    /// What one key shows, in every language the project ships. Every one and not the resolved one:
    /// which language the application is showing is a fact about the run, and a guard that only
    /// held in the current one would be the same defect measured a moment later.
    /// </summary>
    private static IReadOnlyList<string> Resolved(string key, IReadOnlyList<string> files)
    {
        var shown = new List<string>();
        foreach (var file in files)
        {
            string? text;
            try
            {
                text = JsonSource.Value(file, key);
            }
            catch (Exception unreadable) when (unreadable is JsonException or IOException)
            {
                // A strings file that cannot be read is not this list's problem to report; the
                // label reader and the language precondition both say so where it matters.
                continue;
            }

            if (!string.IsNullOrWhiteSpace(text))
                shown.Add(text.Trim());
        }

        return new ReadOnlyCollection<string>(shown.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static string? Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim()
            : null;
}

using System.Collections.ObjectModel;
using System.Text.Json;

using Winwright.Projects;
using Winwright.Verdicts;

namespace Winwright.Scenarios;

/// <summary>
/// Whether the cases a run ran are the ones the last run over the same selection ran, and whether
/// every claim that run made is still made. WW532, WW533.
/// <para>
/// Raised in a comment on the article about the run that went from 374 to 352. The commenter
/// reproduced it in pytest: four tests passed, then one became a skip and one was renamed, and the
/// run still exited 0 with the line people read unchanged. A count catches 374 becoming 352. It
/// cannot catch a suite that keeps its total and changes its contents, because the check that stopped
/// being made has no test left to fail.
/// </para>
/// <para>
/// Half of that was closed already: <see cref="SuiteVerdict.Skipped"/> names every case a selection
/// left alone, so there is no silent skip here. What every run lacked was a memory, so this keeps
/// one — each case that ran with the defect it says it <c>catches</c>, per selection, in the file the
/// project declares as <c>lastRun</c> — and the next run over the same selection reads it back.
/// </para>
/// <para>
/// WW533 is why the claims are kept beside the names. A diff of names treats every rename as a
/// change, and a diff that is mostly noise is one people learn to wave through. What a substitution
/// actually loses is a claim: <c>catches</c> survives a rename untouched, so this holds when every
/// claim the last run made is still made by some case that ran, names or no names, and does not hold
/// when one is made by none. Two cases may make the same claim, so a claim one of them dropped while
/// the other still makes it is no loss. A case that vanished having claimed nothing is the one thing
/// this cannot judge, and it is named as that rather than read as harmless.
/// </para>
/// <para>
/// A finding and never a failure. It is neither the application failing nor the desk refusing, and a
/// red that cleared itself on the next run would teach everybody to wave it through. The record is
/// rewritten by every run, so a deliberate change is named on the run that made it and is the
/// baseline after that.
/// </para>
/// </summary>
public sealed class RanBefore
{
    /// <summary>What this reading is called wherever it is reported.</summary>
    public const string Named = "every claim the last run over the same selection made is still made by a case that ran";

    /// <summary>The project key the record's path is declared under.</summary>
    public const string Key = "lastRun";

    private static readonly JsonSerializerOptions Written = new() { WriteIndented = true };

    private RanBefore(bool asked, string? record, string selection, int ran)
    {
        Asked = asked;
        Record = record;
        Selection = selection;
        Count = ran;
    }

    /// <summary>The reading of a run whose project keeps no record, which is a reading not taken.</summary>
    public static RanBefore NotAsked { get; } = new(false, null, "", 0);

    /// <summary>Whether the project declared a record at all, without which nothing was compared.</summary>
    public bool Asked { get; }

    /// <summary>Whether there was no earlier run over this selection on record to compare against.</summary>
    public bool First { get; private init; }

    /// <summary>The file the record is kept in, or null where none is declared.</summary>
    public string? Record { get; }

    /// <summary>The selection the record is kept under, as the run reports it.</summary>
    public string Selection { get; }

    /// <summary>How many cases this run ran.</summary>
    public int Count { get; }

    /// <summary>Every case this run ran that the last run over the same selection did not, by name.</summary>
    public IReadOnlyList<string> Appeared { get; private init; } = [];

    /// <summary>Every case the last run over the same selection ran that this one did not, by name.</summary>
    public IReadOnlyList<string> Vanished { get; private init; } = [];

    /// <summary>
    /// Every claim the last run made that no case this run ran makes any more, as the cases wrote
    /// them. WW533: the loss a substitution actually costs.
    /// </summary>
    public IReadOnlyList<string> Lost { get; private init; } = [];

    /// <summary>
    /// Every case that vanished having said nothing about what it catches, by name — the vanishing
    /// this reading cannot judge. WW533.
    /// </summary>
    public IReadOnlyList<string> Unclaimed { get; private init; } = [];

    /// <summary>Why the record on disk could not be read, where it could not. Null otherwise.</summary>
    public string? Unreadable { get; private init; }

    /// <summary>Whether the same cases ran as last time, by name.</summary>
    public bool Same => Asked && !First && Appeared.Count == 0 && Vanished.Count == 0;

    /// <summary>Whether every claim the last run made is still made, which is the claim above.</summary>
    public bool Kept => Asked && !First && Lost.Count == 0 && Unclaimed.Count == 0;

    /// <summary>What was read, said in whichever way it went.</summary>
    public string Sentence()
    {
        if (!Asked)
        {
            return "nothing remembers which cases the last run ran, so a case swapped for another at the same "
                + $"total would not show; declaring '{Key}' keeps that record.";
        }

        if (First)
        {
            var why = Unreadable is null ? "no earlier run over that selection is on record" : $"the record could not be read ({Unreadable})";
            return $"{why} at {Record}, so this run over {Selection} is compared against nothing and is "
                + "what the next one is compared against.";
        }

        var plural = Count == 1 ? "" : "s";
        var names = Same
            ? $"the same {Count} case{plural} ran as the last run over {Selection}"
            : $"the cases that ran are not the ones the last run over {Selection} ran: {Changes()}";

        return $"{names}; {Claims()}.";
    }

    /// <inheritdoc cref="Sentence" />
    public override string ToString() => Sentence();

    /// <summary>The reading as a finding: held, not held, or never taken.</summary>
    public Finding AsFinding() => new(Named, Asked && !First ? Kept : null, Sentence());

    /// <summary>
    /// The same, for a runner that knows only the names it ran: every case is read as claiming
    /// nothing, so a vanishing among them is one this cannot judge, and says so.
    /// </summary>
    /// <param name="asked">The selection, which the record is kept under.</param>
    /// <param name="ran">The name of every case that ran.</param>
    /// <param name="project">The declaration that names the record, or names none.</param>
    public static RanBefore Of(Selection asked, IEnumerable<string> ran, ProjectDeclaration project)
    {
        ArgumentNullException.ThrowIfNull(ran);
        return Of(asked, ran.Select(name => (name, "")), project);
    }

    /// <summary>
    /// Compare the cases that ran, and what each says it catches, with the record the project declares,
    /// and write this run's back as the record the next one is compared against. <c>Suite</c> calls
    /// this at the end of both of its doors.
    /// </summary>
    /// <param name="asked">The selection, which the record is kept under.</param>
    /// <param name="ran">Every case that ran, by name, with its <c>catches</c> or empty where it has none.</param>
    /// <param name="project">The declaration that names the record, or names none.</param>
    public static RanBefore Of(
        Selection asked, IEnumerable<(string Name, string Catches)> ran, ProjectDeclaration project)
    {
        ArgumentNullException.ThrowIfNull(asked);
        ArgumentNullException.ThrowIfNull(ran);
        ArgumentNullException.ThrowIfNull(project);

        if (!project.Declares(Key))
            return NotAsked;

        var record = project.LastRun;
        var selection = asked.Asked;

        var now = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, catches) in ran)
            now.TryAdd(name, catches?.Trim() ?? "");

        var (kept, unreadable) = Read(record);
        var before = kept.TryGetValue(selection, out var was) ? was : null;

        kept[selection] = new Dictionary<string, string>(now, StringComparer.Ordinal);
        Directory.CreateDirectory(Path.GetDirectoryName(record)!);
        File.WriteAllText(record, JsonSerializer.Serialize(kept, Written));

        if (before is null)
            return new RanBefore(true, record, selection, now.Count) { First = true, Unreadable = unreadable };

        var vanished = before.Keys.Where(name => !now.ContainsKey(name)).Order(StringComparer.Ordinal).ToList();
        var claimedNow = new HashSet<string>(now.Values.Where(one => one.Length > 0), StringComparer.Ordinal);

        return new RanBefore(true, record, selection, now.Count)
        {
            Appeared = new ReadOnlyCollection<string>(now.Keys.Where(name => !before.ContainsKey(name)).ToList()),
            Vanished = new ReadOnlyCollection<string>(vanished),
            Lost = new ReadOnlyCollection<string>(
                before.Values.Where(one => one.Length > 0 && !claimedNow.Contains(one))
                    .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList()),
            Unclaimed = new ReadOnlyCollection<string>(vanished.Where(name => before[name].Length == 0).ToList()),
        };
    }

    /// <summary>
    /// The record on disk, per selection, each a map of case name to claim. A selection kept as a
    /// plain list of names — the shape WW532 wrote — reads as names that claimed nothing.
    /// </summary>
    private static (Dictionary<string, Dictionary<string, string>> Kept, string? Unreadable) Read(string record)
    {
        var kept = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        if (!File.Exists(record))
            return (kept, null);

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(record));
            foreach (var selection in document.RootElement.EnumerateObject())
                kept[selection.Name] = Cases(selection.Value);

            return (kept, null);
        }
        catch (Exception wrong) when (wrong is JsonException or InvalidOperationException)
        {
            // Rewritten rather than refused: the record is this tool's own file and nothing a person
            // keeps, so a damaged one costs one comparison and is said, not a run.
            return ([], wrong.Message);
        }
    }

    /// <summary>One selection's cases as the record holds them: a map to claims, or a list of names.</summary>
    private static Dictionary<string, string> Cases(JsonElement held)
    {
        var cases = new Dictionary<string, string>(StringComparer.Ordinal);
        if (held.ValueKind == JsonValueKind.Array)
        {
            foreach (var name in held.EnumerateArray())
                cases.TryAdd(name.GetString() ?? "", "");

            return cases;
        }

        foreach (var one in held.EnumerateObject())
            cases[one.Name] = one.Value.ValueKind == JsonValueKind.String ? one.Value.GetString()! : "";

        return cases;
    }

    private string Changes()
    {
        var parts = new List<string>();
        if (Appeared.Count > 0)
            parts.Add($"{Appeared.Count} appeared ({Quoted(Appeared)})");
        if (Vanished.Count > 0)
            parts.Add($"{Vanished.Count} vanished ({Quoted(Vanished)})");

        return string.Join(", and ", parts);
    }

    private string Claims()
    {
        var parts = new List<string>();
        if (Lost.Count > 0)
            parts.Add($"{Lost.Count} claim{(Lost.Count == 1 ? "" : "s")} no case makes any more ({Quoted(Lost)})");
        if (Unclaimed.Count > 0)
        {
            parts.Add($"{Unclaimed.Count} that vanished said nothing about what it catches ({Quoted(Unclaimed)}), "
                + "so whether a claim went with it cannot be told");
        }

        return parts.Count == 0 ? "every claim the last run made is still made" : string.Join(", and ", parts);
    }

    private static string Quoted(IEnumerable<string> names) => string.Join(", ", names.Select(one => $"'{one}'"));
}

using System.Collections.ObjectModel;
using System.Text.Json;

using Winwright.Projects;
using Winwright.Verdicts;

namespace Winwright.Scenarios;

/// <summary>
/// Whether the cases a run ran are the ones the last run over the same selection ran. WW532.
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
/// one — the names of the cases that ran, per selection, in the file the project declares as
/// <c>lastRun</c> — and the next run over the same selection names each case that appeared and each
/// that vanished.
/// </para>
/// <para>
/// A finding and never a failure, which is the answer to what verdict a difference earns. It is
/// neither the application failing nor the desk refusing, and the colour of the build stays with the
/// checks that ran. The record is rewritten by every run, so a deliberate rename is named on the run
/// that made it and is the baseline after that — once, and not a line everybody learns to skip.
/// </para>
/// </summary>
public sealed class RanBefore
{
    /// <summary>What this reading is called wherever it is reported.</summary>
    public const string Named = "the cases this run ran are the ones the last run over the same selection ran";

    /// <summary>The project key the record's path is declared under.</summary>
    public const string Key = "lastRun";

    private static readonly JsonSerializerOptions Written = new() { WriteIndented = true };

    private RanBefore(
        bool asked,
        string? record,
        string selection,
        int ran,
        IReadOnlyList<string>? appeared,
        IReadOnlyList<string>? vanished,
        string? unreadable = null)
    {
        Asked = asked;
        Record = record;
        Selection = selection;
        Count = ran;
        Appeared = appeared ?? [];
        Vanished = vanished ?? [];
        First = asked && appeared is null;
        Unreadable = unreadable;
    }

    /// <summary>The reading of a run whose project keeps no record, which is a reading not taken.</summary>
    public static RanBefore NotAsked { get; } = new(false, null, "", 0, [], []);

    /// <summary>Whether the project declared a record at all, without which nothing was compared.</summary>
    public bool Asked { get; }

    /// <summary>Whether there was no earlier run over this selection on record to compare against.</summary>
    public bool First { get; }

    /// <summary>The file the record is kept in, or null where none is declared.</summary>
    public string? Record { get; }

    /// <summary>The selection the record is kept under, as the run reports it.</summary>
    public string Selection { get; }

    /// <summary>How many cases this run ran.</summary>
    public int Count { get; }

    /// <summary>Every case this run ran that the last run over the same selection did not, by name.</summary>
    public IReadOnlyList<string> Appeared { get; }

    /// <summary>Every case the last run over the same selection ran that this one did not, by name.</summary>
    public IReadOnlyList<string> Vanished { get; }

    /// <summary>Why the record on disk could not be read, where it could not. Null otherwise.</summary>
    public string? Unreadable { get; }

    /// <summary>Whether the same cases ran as last time, which is the claim above.</summary>
    public bool Same => Asked && !First && Appeared.Count == 0 && Vanished.Count == 0;

    /// <summary>What was read, said in whichever of the four ways it went.</summary>
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

        if (Same)
            return $"the same {Count} case{(Count == 1 ? "" : "s")} ran as the last run over {Selection}.";

        var parts = new List<string>();
        if (Appeared.Count > 0)
            parts.Add($"{Appeared.Count} appeared ({Quoted(Appeared)})");
        if (Vanished.Count > 0)
            parts.Add($"{Vanished.Count} vanished ({Quoted(Vanished)})");

        return $"the cases that ran are not the ones the last run over {Selection} ran: {string.Join(", and ", parts)}.";
    }

    /// <inheritdoc cref="Sentence" />
    public override string ToString() => Sentence();

    /// <summary>The reading as a finding: held, not held, or never taken.</summary>
    public Finding AsFinding() => new(Named, Asked && !First ? Same : null, Sentence());

    /// <summary>
    /// Compare the cases that ran with the record the project declares, and write this run's set back
    /// as the record the next one is compared against. <c>Suite</c> calls this at the end of both of
    /// its doors; a runner of its own calls it with the names it ran.
    /// </summary>
    /// <param name="asked">The selection, which the record is kept under.</param>
    /// <param name="ran">The name of every case that ran.</param>
    /// <param name="project">The declaration that names the record, or names none.</param>
    public static RanBefore Of(Selection asked, IEnumerable<string> ran, ProjectDeclaration project)
    {
        ArgumentNullException.ThrowIfNull(asked);
        ArgumentNullException.ThrowIfNull(ran);
        ArgumentNullException.ThrowIfNull(project);

        if (!project.Declares(Key))
            return NotAsked;

        var record = project.LastRun;
        var selection = asked.Asked;
        var now = ran.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

        Dictionary<string, List<string>> kept;
        string? unreadable = null;
        try
        {
            kept = File.Exists(record)
                ? JsonSerializer.Deserialize<Dictionary<string, List<string>>>(File.ReadAllText(record)) ?? []
                : [];
        }
        catch (JsonException wrong)
        {
            // Rewritten rather than refused: the record is this tool's own file and nothing a person
            // keeps, so a damaged one costs one comparison and is said, not a run.
            kept = [];
            unreadable = wrong.Message;
        }

        var before = kept.TryGetValue(selection, out var was) ? was : null;

        kept[selection] = now;
        Directory.CreateDirectory(Path.GetDirectoryName(record)!);
        File.WriteAllText(record, JsonSerializer.Serialize(kept, Written));

        if (before is null)
            return new RanBefore(true, record, selection, now.Count, null, null, unreadable);

        var appeared = now.Except(before, StringComparer.Ordinal).ToList();
        var vanished = before.Except(now, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        return new RanBefore(
            true,
            record,
            selection,
            now.Count,
            new ReadOnlyCollection<string>(appeared),
            new ReadOnlyCollection<string>(vanished));
    }

    private static string Quoted(IEnumerable<string> names) => string.Join(", ", names.Select(one => $"'{one}'"));
}

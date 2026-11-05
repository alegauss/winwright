using System.Text;

namespace Winwright.Verdicts;

/// <summary>
/// A sweep as a person reads it. The headline counts assertions once, because one hole is one
/// hole however many environments met it; the lines under it print every occurrence, because
/// where it did not run is part of the reading. Two properties, two places, neither collapsed.
/// </summary>
public static class SweepSummary
{
    /// <summary>Render the whole summary, headline first, as lines joined by newlines.</summary>
    public static string Render(SweepVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);

        var text = new StringBuilder(Headline(verdict));
        foreach (var line in Detail(verdict))
            text.Append('\n').Append(line);

        return text.ToString();
    }

    /// <summary>
    /// The deduped reading: how many environments were walked, and how many distinct assertions
    /// failed or never ran, each followed by the number of places it happened where that is more
    /// than one — so a count of holes and a count of occurrences can never be read for each other.
    /// <para>
    /// WW192, in the same commit as the single run's, and that is deliberate. WW177 joined the
    /// reading to the verdict for one run, the sweep did not get it, and WW185 was the repair — a
    /// division shipped for one summary and not the other would be that split again. A sweep is
    /// where whose-fault matters most: a hole in two of five environments is a question about those
    /// two machines, and only if the hole is theirs.
    /// </para>
    /// <para>
    /// Divided over the distinct assertions rather than over the occurrences, which is the rule this
    /// headline already follows: one hole is one hole however many environments met it.
    /// </para>
    /// </summary>
    public static string Headline(SweepVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);

        var word = verdict.Outcome.ToString().ToUpperInvariant();
        var broke = verdict.Broke.Count == 0
            ? ""
            : $"; the harness broke {VerdictSummary.Times(verdict.Broke.Count)}";
        var whose = VerdictSummary.Whose(verdict.Unchecked.Select(one => one.Occurrences[0].Result));
        return $"{word} (exit {verdict.ExitCode}) - "
            + $"{VerdictSummary.Plural(verdict.Environments.Count, "environment")}: "
            + $"{Counted(verdict.Failures.Count, verdict.FailureOccurrences, "failed")}, "
            + $"{Counted(verdict.Unchecked.Count, verdict.UncheckedOccurrences, "unchecked")}{whose}{broke}";
    }

    /// <summary>
    /// The sweep in one sentence. As with a single run, the word <em>every</em> is earned: a
    /// sweep that met a hole anywhere names it instead, deduped, so the sentence stays a reading
    /// of the whole walk rather than of whichever environment happened to be clean.
    /// </summary>
    public static string Sentence(SweepVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);

        var walked = VerdictSummary.Plural(verdict.Environments.Count, "environment");
        if (Coverage.EarnsEvery(verdict))
            return $"every assertion passed in {walked}.";

        var clauses = new List<string>();
        if (verdict.Broke.Count > 0)
            clauses.Add("the harness broke at "
                + string.Join(", ", verdict.Broke.Select(at => $"[{at.Environment}] {at.Error}")));

        clauses.Add($"{walked} walked");
        if (verdict.Failures.Count > 0)
            clauses.Add($"{verdict.Failures.Count} failed: {string.Join(", ", Coverage.Failed(verdict))}");
        if (verdict.Unchecked.Count > 0)
            clauses.Add($"{verdict.Unchecked.Count} never ran: {string.Join(", ", Coverage.NotRun(verdict))}");

        return string.Join("; ", clauses) + ".";
    }

    /// <summary>
    /// One line per occurrence — failures first, holes after them — each carrying the environment
    /// it happened in, so three sightings of one hole read as one hole in three places.
    /// </summary>
    public static IReadOnlyList<string> Detail(SweepVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);

        var lines = new List<string>();
        foreach (var at in verdict.Broke)
            lines.Add($"  threw      [{at.Environment}] {at.Error}");
        foreach (var tally in verdict.Failures)
            foreach (var occurrence in tally.Occurrences)
                lines.Add(VerdictSummary.Line(occurrence.Result, occurrence.Environment));
        foreach (var tally in verdict.Unchecked)
            foreach (var occurrence in tally.Occurrences)
                lines.Add(VerdictSummary.Line(occurrence.Result, occurrence.Environment));

        // WW185. One sentence per machine that has something to explain, and only where a reading
        // was taken. A sweep is read to find out which environment behaved differently, and the
        // answer used to be a name — the full page for five environments would be sixty-five lines
        // of measurements, which is why this is the sentence a reader skims and not the page.
        foreach (var run in verdict.Environments.Where(one => one.Described && one.Verdict.Outcome != RunOutcome.Passed))
            lines.Add($"  on         [{run.Environment}] {run.Reading!.Sentence()}");

        // And the environments nobody read, named rather than counted — but only where the sweep
        // read some of them. A sweep that described none claimed nothing, and there are no "on"
        // lines above for a reader to mistake for the whole set; a sweep that described four of
        // five is the one where the fifth reads as a machine like the others.
        if (verdict.Environments.Any(one => one.Described) && !verdict.Describes)
            lines.Add($"  not read   {verdict.Undescribed.Count} environment(s): {string.Join(", ", verdict.Undescribed)}");

        return lines;
    }

    private static string Counted(int distinct, int occurrences, string word) =>
        occurrences > distinct
            ? $"{distinct} {word} (in {occurrences} of them)"
            : $"{distinct} {word}";
}

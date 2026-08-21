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
    /// </summary>
    public static string Headline(SweepVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);

        var word = verdict.Outcome.ToString().ToUpperInvariant();
        return $"{word} (exit {verdict.ExitCode}) - "
            + $"{VerdictSummary.Plural(verdict.Environments.Count, "environment")}: "
            + $"{Counted(verdict.Failures.Count, verdict.FailureOccurrences, "failed")}, "
            + $"{Counted(verdict.Unchecked.Count, verdict.UncheckedOccurrences, "unchecked")}";
    }

    /// <summary>
    /// One line per occurrence — failures first, holes after them — each carrying the environment
    /// it happened in, so three sightings of one hole read as one hole in three places.
    /// </summary>
    public static IReadOnlyList<string> Detail(SweepVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);

        var lines = new List<string>();
        foreach (var tally in verdict.Failures)
            foreach (var occurrence in tally.Occurrences)
                lines.Add(VerdictSummary.Line(occurrence.Result, occurrence.Environment));
        foreach (var tally in verdict.Unchecked)
            foreach (var occurrence in tally.Occurrences)
                lines.Add(VerdictSummary.Line(occurrence.Result, occurrence.Environment));

        return lines;
    }

    private static string Counted(int distinct, int occurrences, string word) =>
        occurrences > distinct
            ? $"{distinct} {word} (in {occurrences} of them)"
            : $"{distinct} {word}";
}

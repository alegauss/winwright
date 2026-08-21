using System.Text;

namespace Winwright.Verdicts;

/// <summary>
/// The verdict as a person reads it. One headline carrying the outcome, its exit code and the
/// tally, then one line per assertion that failed or never ran — named, because a hole reported
/// only as a count is a hole nobody can go and look at.
/// </summary>
public static class VerdictSummary
{
    /// <summary>Render the whole summary, headline first, as lines joined by newlines.</summary>
    public static string Render(RunVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);

        var text = new StringBuilder(Headline(verdict));
        foreach (var line in Detail(verdict))
            text.Append('\n').Append(line);

        return text.ToString();
    }

    /// <summary>
    /// The one line CI shows: the outcome, the exit code it is, and how the assertions divided.
    /// The counts are stated separately so that a green with a hole in it cannot read as a green.
    /// </summary>
    public static string Headline(RunVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);

        var word = verdict.Outcome.ToString().ToUpperInvariant();
        var passed = verdict.Results.Count - verdict.Failures.Count - verdict.Unchecked.Count;
        return $"{word} (exit {verdict.ExitCode}) - {Plural(verdict.Results.Count, "assertion")}: "
            + $"{passed} passed, {verdict.Failures.Count} failed, {verdict.Unchecked.Count} unchecked";
    }

    /// <summary>
    /// One line per assertion that did not pass, failures first and holes after them, each naming
    /// the assertion and the sentence that explains it.
    /// </summary>
    public static IReadOnlyList<string> Detail(RunVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);

        var lines = new List<string>();
        foreach (var failure in verdict.Failures)
            lines.Add($"  failed     {failure.Name} - {failure.Detail}");
        foreach (var hole in verdict.Unchecked)
            lines.Add($"  unchecked  {hole.Name} - {hole.Detail}");

        return lines;
    }

    private static string Plural(int count, string noun) =>
        count == 1 ? $"1 {noun}" : $"{count} {noun}s";
}

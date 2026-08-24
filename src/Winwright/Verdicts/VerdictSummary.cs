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
    /// The whole page: what the run read about the machine, then the verdict that reading makes
    /// legible.
    /// <para>
    /// WW177. These were two pages that never met. This file mentioned <see cref="Preamble" />
    /// nowhere and nothing joined them, so a run measured thirteen things about the machine and
    /// closed its store fingerprint, and a person handed an exit code got none of it. This block's
    /// first criterion is that a degraded run is legible without reading the log — and an exit 2
    /// names the assertions that never ran while the reason each could not run sat on the page
    /// nothing printed.
    /// </para>
    /// <para>
    /// The reading leads, because it is what makes the verdict underneath it legible: a reader who
    /// has just been told four assertions never ran wants the absent precondition first, not after.
    /// </para>
    /// </summary>
    /// <param name="verdict">What the run concluded.</param>
    /// <param name="reading">What it read about the machine, opened before it and closed after.</param>
    /// <exception cref="ArgumentException">
    /// Where the reading declared a store and never closed it. That is a reading from the middle of
    /// a run, and printing one under a final verdict shows the machine as it was before the run
    /// touched it beside a verdict about what happened after.
    /// </exception>
    public static string Render(RunVerdict verdict, Preamble reading)
    {
        ArgumentNullException.ThrowIfNull(verdict);
        ArgumentNullException.ThrowIfNull(reading);

        if (reading.Store is not null && !reading.Closed)
        {
            throw new ArgumentException(
                "this reading opened a store fingerprint and never closed it, so it is from the middle "
                    + "of a run and not the end of one — call Preamble.Closing, or Preamble.Around to "
                    + "have both halves taken for you",
                nameof(reading));
        }

        var text = new StringBuilder();
        foreach (var line in reading.Render())
            text.Append(line).Append('\n');

        return text.Append(Render(verdict)).ToString();
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
        var broke = verdict.Broke.Count == 0 ? "" : $"; the harness broke {Times(verdict.Broke.Count)}";
        return $"{word} (exit {verdict.ExitCode}) - {Plural(verdict.Results.Count, "assertion")}: "
            + $"{passed} passed, {verdict.Failures.Count} failed, {verdict.Unchecked.Count} unchecked{broke}";
    }

    /// <summary>
    /// The run in one sentence, for whoever prints a banner rather than a table. The word
    /// <em>every</em> appears here only where <see cref="Coverage.EarnsEvery(RunVerdict)"/> says
    /// it was earned; otherwise what did not run is named, not counted, because a count is what a
    /// dropped assertion rides out in.
    /// </summary>
    public static string Sentence(RunVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);

        if (Coverage.EarnsEvery(verdict))
            return $"every assertion passed ({verdict.Results.Count} of {verdict.Results.Count}).";

        var passed = verdict.Results.Count - verdict.Failures.Count - verdict.Unchecked.Count;
        return Composed(
            passed, verdict.Results.Count, Coverage.Failed(verdict), Coverage.NotRun(verdict), verdict.Broke);
    }

    /// <summary>
    /// One line per assertion that did not pass, failures first and holes after them, each naming
    /// the assertion and the sentence that explains it.
    /// </summary>
    public static IReadOnlyList<string> Detail(RunVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);

        var lines = new List<string>();
        foreach (var error in verdict.Broke)
            lines.Add($"  threw      {error}");
        foreach (var failure in verdict.Failures)
            lines.Add(Line(failure));
        foreach (var hole in verdict.Unchecked)
            lines.Add(Line(hole));

        return lines;
    }

    /// <summary>
    /// One assertion as its own line. <paramref name="environment"/> is what a sweep puts in
    /// front of it, so the same assertion appearing in three environments reads as three places
    /// rather than as three assertions.
    /// </summary>
    public static string Line(AssertionResult result, string? environment = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Outcome == AssertionOutcome.Passed)
            throw new ArgumentException(
                $"'{result.Name}' passed, and a summary that lists what passed is one nobody reads to the end",
                nameof(result));

        var word = result.Outcome == AssertionOutcome.Failed ? "failed    " : "unchecked ";
        var place = string.IsNullOrWhiteSpace(environment) ? "" : $"[{environment.Trim()}] ";
        var why = result.Missing is null ? result.Detail : $"'{result.Missing.Name}' absent: {result.Detail}";

        // The ordinal where there is one, so a failure named here is one grep away from the line
        // recording what was read back. Left out entirely where nothing joined them, because a
        // step number nobody assigned would send a reader to whatever line happened to be there.
        var at = result.Traced ? $"step {result.Step}  " : "";
        return $"  {word} {at}{place}{result.Name} - {why}";
    }

    /// <summary>Count and noun, agreeing. Used by every headline this project renders.</summary>
    public static string Plural(int count, string noun) =>
        count == 1 ? $"1 {noun}" : $"{count} {noun}s";

    /// <summary>
    /// The unearned sentence: what passed as a fraction, then what failed and what never ran, each
    /// named. Shared with the sweep, so one hole reads the same way whichever summary printed it.
    /// </summary>
    internal static string Composed(
        int passed,
        int total,
        IReadOnlyList<string> failed,
        IReadOnlyList<string> notRun,
        IReadOnlyList<HarnessError> broke)
    {
        var clauses = new List<string>();

        // The break leads, because it is the clause that says which repository to open.
        if (broke.Count > 0)
            clauses.Add($"the harness broke at {string.Join(", ", broke.Select(error => error.ToString()))}");

        clauses.Add($"{passed} of {Plural(total, "assertion")} passed");
        if (failed.Count > 0)
            clauses.Add($"{failed.Count} failed: {string.Join(", ", failed)}");
        if (notRun.Count > 0)
            clauses.Add($"{notRun.Count} never ran: {string.Join(", ", notRun)}");

        return string.Join("; ", clauses) + ".";
    }

    /// <summary>Once, or a count. Small, and it keeps "broke 1 times" out of a headline.</summary>
    internal static string Times(int count) => count == 1 ? "once" : $"{count} times";
}

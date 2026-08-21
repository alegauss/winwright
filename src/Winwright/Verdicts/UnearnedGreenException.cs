namespace Winwright.Verdicts;

/// <summary>
/// Something asked to say that every check passed, on a run that did not earn it. The refusal
/// names what stands in the way rather than counting it, because a count is what a dropped
/// assertion rides out in — an info line saying "1 skipped" is the line nobody reads.
/// </summary>
public sealed class UnearnedGreenException : Exception
{
    /// <param name="unearned">The claim that was asked for, so the refusal reads as an answer to it.</param>
    /// <param name="notRun">The assertions that never ran.</param>
    /// <param name="failed">The assertions that ran and did not hold.</param>
    /// <param name="broke">Everywhere the harness itself threw.</param>
    public UnearnedGreenException(
        string unearned,
        IReadOnlyList<string> notRun,
        IReadOnlyList<string> failed,
        IReadOnlyList<HarnessError> broke)
        : base($"'{unearned}' is not this run: {Because(notRun, failed, broke)}")
    {
        Unearned = unearned;
        NotRun = notRun;
        Failed = failed;
        Broke = broke;
    }

    /// <summary>The claim that was refused.</summary>
    public string Unearned { get; }

    /// <summary>The assertions that did not run, in the order the run recorded them.</summary>
    public IReadOnlyList<string> NotRun { get; }

    /// <summary>The assertions that ran and did not hold.</summary>
    public IReadOnlyList<string> Failed { get; }

    /// <summary>Everywhere the harness itself threw.</summary>
    public IReadOnlyList<HarnessError> Broke { get; }

    private static string Because(
        IReadOnlyList<string> notRun, IReadOnlyList<string> failed, IReadOnlyList<HarnessError> broke)
    {
        var clauses = new List<string>();
        if (broke.Count > 0)
            clauses.Add($"the harness broke at {string.Join(", ", broke.Select(error => error.ToString()))}");
        if (failed.Count > 0)
            clauses.Add($"{string.Join(", ", failed)} failed");
        if (notRun.Count > 0)
            clauses.Add($"{string.Join(", ", notRun)} never ran");

        return clauses.Count > 0 ? string.Join("; ", clauses) : "it did not earn the word";
    }
}

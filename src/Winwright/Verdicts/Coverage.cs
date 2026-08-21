namespace Winwright.Verdicts;

/// <summary>
/// The gate in front of the word <em>every</em>. A run where an assertion could not be evaluated
/// is not the same run as one where all of them passed, and printing the same green line for both
/// is how a timing assertion got dropped into an info line nobody reads. So the word is earned:
/// anything about to claim full coverage — this project's own summary, or an adopting project's
/// banner — asks here first, and is refused with the names while the unchecked list is non-empty.
/// </summary>
public static class Coverage
{
    /// <summary>Whether this run is entitled to say that every check passed.</summary>
    public static bool EarnsEvery(RunVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);
        return verdict.Outcome == RunOutcome.Passed;
    }

    /// <summary>Whether this sweep is, which takes every environment it walked being entitled too.</summary>
    public static bool EarnsEvery(SweepVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);
        return verdict.Outcome == RunOutcome.Passed;
    }

    /// <summary>The assertions that did not run, by name and deduped, in the order they were recorded.</summary>
    public static IReadOnlyList<string> NotRun(RunVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);
        return verdict.Unchecked.Select(result => result.Name).Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>The same, across a sweep: one hole is one name however many environments met it.</summary>
    public static IReadOnlyList<string> NotRun(SweepVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);
        return verdict.Unchecked.Select(tally => tally.Name).ToList();
    }

    /// <summary>The assertions that ran and did not hold, by name and deduped.</summary>
    public static IReadOnlyList<string> Failed(RunVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);
        return verdict.Failures.Select(result => result.Name).Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>The same, across a sweep.</summary>
    public static IReadOnlyList<string> Failed(SweepVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);
        return verdict.Failures.Select(tally => tally.Name).ToList();
    }

    /// <summary>
    /// Pass through <paramref name="claim"/> where the run earned it, and refuse it where it did
    /// not. This is the call an adopting project's own reporting makes before printing a green.
    /// </summary>
    /// <exception cref="UnearnedGreenException">Where an assertion did not run.</exception>
    public static string RequireEvery(RunVerdict verdict, string claim = "every check passed")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claim);
        return EarnsEvery(verdict) ? claim : throw new UnearnedGreenException(claim, NotRun(verdict), Failed(verdict));
    }

    /// <summary>The same gate in front of a sweep's own green.</summary>
    /// <exception cref="UnearnedGreenException">Where an assertion did not run anywhere in it.</exception>
    public static string RequireEvery(SweepVerdict verdict, string claim = "every check passed")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claim);
        return EarnsEvery(verdict) ? claim : throw new UnearnedGreenException(claim, NotRun(verdict), Failed(verdict));
    }
}

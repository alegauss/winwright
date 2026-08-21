using System.Collections.ObjectModel;

namespace Winwright.Verdicts;

/// <summary>
/// The reading of a whole run: the assertion results it is derived from, the outcome that follows
/// from them, and the exit code that outcome is. Nothing here is set by a caller — the verdict is
/// derived, so a run cannot report a green it did not earn.
/// </summary>
public sealed class RunVerdict
{
    private RunVerdict(IReadOnlyList<AssertionResult> results)
    {
        Results = results;
        Failures = Where(results, AssertionOutcome.Failed);
        Unchecked = Where(results, AssertionOutcome.Unchecked);
        Outcome = Failures.Count > 0 ? RunOutcome.Failed
            : Unchecked.Count > 0 ? RunOutcome.Degraded
            : RunOutcome.Passed;
    }

    /// <summary>Every assertion the run produced a result for, in the order it produced them.</summary>
    public IReadOnlyList<AssertionResult> Results { get; }

    /// <summary>The assertions that ran and did not hold.</summary>
    public IReadOnlyList<AssertionResult> Failures { get; }

    /// <summary>The assertions that never ran, each carrying the precondition that was absent.</summary>
    public IReadOnlyList<AssertionResult> Unchecked { get; }

    /// <summary>The one of three readings this run earned.</summary>
    public RunOutcome Outcome { get; }

    /// <summary>The process exit code, which is the outcome itself and not a second mapping.</summary>
    public int ExitCode => (int)Outcome;

    /// <summary>How many assertions ran at all — the denominator a pass is entitled to claim.</summary>
    public int Ran => Results.Count - Unchecked.Count;

    /// <summary>
    /// Read a verdict off the results of a run. A failure outranks a hole, because a run that
    /// broke is a stronger statement than a run that could not look; a hole with nothing broken
    /// is the third reading and never a pass.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Where there are no results at all. A verdict over nothing would be exactly the green this
    /// project exists to refuse, and it has no assertion to name in exchange.
    /// </exception>
    public static RunVerdict Over(IEnumerable<AssertionResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var collected = new List<AssertionResult>(results);
        if (collected.Count == 0)
            throw new ArgumentException(
                "a run with no assertion results has nothing to report as passed, and no hole to name",
                nameof(results));

        return new RunVerdict(new ReadOnlyCollection<AssertionResult>(collected));
    }

    private static IReadOnlyList<AssertionResult> Where(IReadOnlyList<AssertionResult> results, AssertionOutcome outcome)
    {
        var matched = new List<AssertionResult>();
        foreach (var result in results)
            if (result.Outcome == outcome)
                matched.Add(result);

        return new ReadOnlyCollection<AssertionResult>(matched);
    }
}

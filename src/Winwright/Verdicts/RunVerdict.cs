using System.Collections.ObjectModel;

namespace Winwright.Verdicts;

/// <summary>
/// The reading of a whole run: the assertion results it is derived from, whatever the harness
/// threw, the outcome that follows from them, and the exit code that outcome is. Nothing here is
/// set by a caller — the verdict is derived, so a run cannot report a green it did not earn.
/// </summary>
public sealed class RunVerdict
{
    private RunVerdict(IReadOnlyList<AssertionResult> results, IReadOnlyList<HarnessError> broke)
    {
        Results = results;
        Broke = broke;
        Failures = Where(results, AssertionOutcome.Failed);
        Unchecked = Where(results, AssertionOutcome.Unchecked);
        Outcome = broke.Count > 0 ? RunOutcome.Broken
            : Failures.Count > 0 ? RunOutcome.Failed
            : Unchecked.Count > 0 ? RunOutcome.Degraded
            : RunOutcome.Passed;
    }

    /// <summary>Every assertion the run produced a result for, in the order it produced them.</summary>
    public IReadOnlyList<AssertionResult> Results { get; }

    /// <summary>Every place the harness itself threw, in the order it threw.</summary>
    public IReadOnlyList<HarnessError> Broke { get; }

    /// <summary>The assertions that ran and did not hold.</summary>
    public IReadOnlyList<AssertionResult> Failures { get; }

    /// <summary>The assertions that never ran, each carrying the precondition that was absent.</summary>
    public IReadOnlyList<AssertionResult> Unchecked { get; }

    /// <summary>The one of four readings this run earned.</summary>
    public RunOutcome Outcome { get; }

    /// <summary>The process exit code, which is the outcome itself and not a second mapping.</summary>
    public int ExitCode => (int)Outcome;

    /// <summary>How many assertions ran at all — the denominator a pass is entitled to claim.</summary>
    public int Ran => Results.Count - Unchecked.Count;

    /// <summary>
    /// Read a verdict off the results of a run. A broken harness outranks everything, because
    /// nothing past the throw was observed and the reader is opening this repository rather than
    /// the one under test; then a failure outranks a hole, because a run that broke is a stronger
    /// statement than a run that could not look; and a hole with nothing broken is never a pass.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Where there are neither results nor errors. A verdict over nothing would be exactly the
    /// green this project exists to refuse, and it has no assertion to name in exchange.
    /// </exception>
    public static RunVerdict Over(IEnumerable<AssertionResult> results) => Over(results, []);

    /// <summary>The same, for a run where the harness threw as well as, or instead of, asserting.</summary>
    /// <exception cref="ArgumentException">Where there are neither results nor errors.</exception>
    public static RunVerdict Over(IEnumerable<AssertionResult> results, IEnumerable<HarnessError> broke)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(broke);

        var collected = new List<AssertionResult>(results);
        var errors = new List<HarnessError>(broke);
        if (collected.Count == 0 && errors.Count == 0)
            throw new ArgumentException(
                "a run with no assertion results has nothing to report as passed, and no hole to name",
                nameof(results));

        return new RunVerdict(
            new ReadOnlyCollection<AssertionResult>(collected),
            new ReadOnlyCollection<HarnessError>(errors));
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

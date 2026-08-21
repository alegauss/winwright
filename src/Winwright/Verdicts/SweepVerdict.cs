using System.Collections.ObjectModel;

namespace Winwright.Verdicts;

/// <summary>One environment a sweep walked, and the reading that environment earned.</summary>
/// <param name="Environment">What was varied — the sampled mode, the theme, the DPI.</param>
/// <param name="Verdict">That environment's own reading, unchanged by the sweep around it.</param>
public sealed record EnvironmentRun(string Environment, RunVerdict Verdict);

/// <summary>One harness error and the environment it was thrown in.</summary>
/// <param name="Environment">The environment the sweep was walking when it broke.</param>
/// <param name="Error">What was thrown, carrying the step it came from.</param>
public sealed record BrokenAt(string Environment, HarnessError Error);

/// <summary>One assertion in one environment: where it happened, and what happened.</summary>
/// <param name="Environment">The environment this occurrence belongs to.</param>
/// <param name="Result">The assertion result recorded there.</param>
public sealed record Occurrence(string Environment, AssertionResult Result);

/// <summary>
/// One assertion across a whole sweep: the name it is counted under once, and every place it
/// actually happened. Both halves are kept because they are two different properties — collapsing
/// them either hides an occurrence or inflates the count.
/// </summary>
/// <param name="Name">The assertion's name, which is what the tally dedupes by.</param>
/// <param name="Occurrences">Every environment it happened in, in the order the sweep walked them.</param>
public sealed record AssertionTally(string Name, IReadOnlyList<Occurrence> Occurrences)
{
    /// <summary>The environments this assertion turned up in, in sweep order.</summary>
    public IEnumerable<string> Environments => Occurrences.Select(occurrence => occurrence.Environment);
}

/// <summary>
/// The reading of a sweep — one scenario walked across several environments. An assertion absent
/// in all three sampled modes is one hole that happened three times, not three holes: the tally
/// dedupes by name, and the detail still prints at every occurrence, because where it did not run
/// is part of the reading.
/// </summary>
public sealed class SweepVerdict
{
    private SweepVerdict(
        IReadOnlyList<EnvironmentRun> environments,
        IReadOnlyList<AssertionTally> failures,
        IReadOnlyList<AssertionTally> unchecked_)
    {
        Environments = environments;
        Failures = failures;
        Unchecked = unchecked_;
        Broke = new ReadOnlyCollection<BrokenAt>(environments
            .SelectMany(run => run.Verdict.Broke.Select(error => new BrokenAt(run.Environment, error)))
            .ToList());
        Outcome = Broke.Count > 0 ? RunOutcome.Broken
            : failures.Count > 0 ? RunOutcome.Failed
            : unchecked_.Count > 0 ? RunOutcome.Degraded
            : RunOutcome.Passed;
    }

    /// <summary>Every environment walked, in the order the sweep walked them.</summary>
    public IReadOnlyList<EnvironmentRun> Environments { get; }

    /// <summary>The distinct assertions that failed somewhere, each with every place it failed.</summary>
    public IReadOnlyList<AssertionTally> Failures { get; }

    /// <summary>The distinct assertions that never ran somewhere, each with every place they did not.</summary>
    public IReadOnlyList<AssertionTally> Unchecked { get; }

    /// <summary>Every place the harness threw, with the environment it threw in.</summary>
    public IReadOnlyList<BrokenAt> Broke { get; }

    /// <summary>The one of four readings the sweep as a whole earned.</summary>
    public RunOutcome Outcome { get; }

    /// <summary>The process exit code, which is the outcome itself and not a second mapping.</summary>
    public int ExitCode => (int)Outcome;

    /// <summary>How many times a hole happened at all, which is never the number of holes.</summary>
    public int UncheckedOccurrences => Unchecked.Sum(tally => tally.Occurrences.Count);

    /// <summary>How many times a failure happened at all.</summary>
    public int FailureOccurrences => Failures.Sum(tally => tally.Occurrences.Count);

    /// <summary>
    /// Read a verdict off a walked sweep. The worst reading in any environment is the sweep's,
    /// for the same reason it is a run's: a hole somewhere is a hole, wherever else it was clean.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Where no environment was walked, or where one was walked twice — two readings of one
    /// environment cannot both be that environment's.
    /// </exception>
    public static SweepVerdict Over(IEnumerable<EnvironmentRun> environments)
    {
        ArgumentNullException.ThrowIfNull(environments);

        var walked = new List<EnvironmentRun>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var run in environments)
        {
            ArgumentNullException.ThrowIfNull(run);
            if (string.IsNullOrWhiteSpace(run.Environment))
                throw new ArgumentException(
                    "an environment is reported under a name, and this one has none", nameof(environments));
            if (!seen.Add(run.Environment))
                throw new ArgumentException(
                    $"'{run.Environment}' was walked twice, and the two readings cannot both be its own",
                    nameof(environments));

            walked.Add(run);
        }

        if (walked.Count == 0)
            throw new ArgumentException(
                "a sweep that walked no environment has nothing to report and no hole to name", nameof(environments));

        return new SweepVerdict(
            new ReadOnlyCollection<EnvironmentRun>(walked),
            Tally(walked, AssertionOutcome.Failed),
            Tally(walked, AssertionOutcome.Unchecked));
    }

    private static IReadOnlyList<AssertionTally> Tally(IReadOnlyList<EnvironmentRun> walked, AssertionOutcome outcome)
    {
        var order = new List<string>();
        var byName = new Dictionary<string, List<Occurrence>>(StringComparer.Ordinal);

        foreach (var run in walked)
            foreach (var result in run.Verdict.Results)
            {
                if (result.Outcome != outcome)
                    continue;

                if (!byName.TryGetValue(result.Name, out var occurrences))
                {
                    occurrences = [];
                    byName[result.Name] = occurrences;
                    order.Add(result.Name);
                }

                occurrences.Add(new Occurrence(run.Environment, result));
            }

        var tallies = order
            .Select(name => new AssertionTally(name, new ReadOnlyCollection<Occurrence>(byName[name])))
            .ToList();

        return new ReadOnlyCollection<AssertionTally>(tallies);
    }
}

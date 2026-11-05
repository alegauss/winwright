namespace Winwright.Locating;

/// <summary>
/// A step matched more than one element and said nothing about which it meant.
/// <para>
/// It is refused rather than answered with whichever came first, because whichever came first is
/// the tree's own order — the order the application happened to create things in — and a scenario
/// resting on that runs against the other one the day somebody reorders a panel, and is green.
/// The refusal lists what matched, each as the locator step that would address it, so the choice
/// is made in the file where the next reader can see it was made.
/// </para>
/// </summary>
public sealed class AmbiguousLocatorException : Exception
{
    /// <param name="step">The step that matched several.</param>
    /// <param name="candidates">What it matched, each named the way a locator names one.</param>
    public AmbiguousLocatorException(LocatorStep step, IReadOnlyList<string> candidates)
        : base($"{step} matches {candidates.Count} elements and does not say which:\n"
            + string.Join("\n", candidates.Select(candidate => "  " + candidate))
            + "\nAdd [order=left|right|top|bottom], or [index=n] to take the nth in tree order.")
    {
        Step = step;
        Candidates = candidates;
    }

    /// <summary>The step that matched several.</summary>
    public LocatorStep Step { get; }

    /// <summary>What it matched.</summary>
    public IReadOnlyList<string> Candidates { get; }
}

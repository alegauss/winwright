namespace Winwright.Verdicts;

/// <summary>
/// What became of a whole run. The member values are the process exit codes, deliberately: a
/// mapping written twice is a mapping that drifts, and CI reads the number rather than the word.
/// </summary>
public enum RunOutcome
{
    /// <summary>Every assertion ran and every one of them held.</summary>
    Passed = 0,

    /// <summary>At least one assertion ran and did not hold.</summary>
    Failed = 1,

    /// <summary>
    /// Everything that ran passed, and something could not be evaluated at all. Collapsing this
    /// into either of the other two is the non-goal this project's central finding rests on.
    /// </summary>
    Degraded = 2,
}

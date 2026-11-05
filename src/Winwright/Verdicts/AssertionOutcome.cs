namespace Winwright.Verdicts;

/// <summary>
/// What became of one assertion. The third member is the one that does not exist in xUnit and is
/// the reason this vocabulary is written out: an assertion that could not be evaluated at all is
/// neither a pass nor a failure, and reporting it as either loses the fact that nobody looked.
/// </summary>
public enum AssertionOutcome
{
    /// <summary>It ran, and what it claimed was true.</summary>
    Passed,

    /// <summary>It ran, and what it claimed was false.</summary>
    Failed,

    /// <summary>
    /// It did not run: a precondition this machine does not satisfy was absent, so there was
    /// nothing to evaluate. Never a pass — that is the whole finding this project was started over.
    /// </summary>
    Unchecked,
}

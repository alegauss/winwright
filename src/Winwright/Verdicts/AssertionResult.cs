namespace Winwright.Verdicts;

/// <summary>
/// One assertion's outcome, with the name it is reported under and the sentence that explains it.
/// The name is required by construction because a degraded run's whole obligation is to name each
/// assertion that did not run, and an unnamed one cannot be named.
/// </summary>
/// <param name="Name">What the assertion claimed, as the scenario spells it.</param>
/// <param name="Outcome">Whether it passed, failed, or was never evaluated.</param>
/// <param name="Detail">
/// Why: what was read back on a failure, which precondition was absent on an unchecked one.
/// Empty is allowed only on a pass, where there is nothing to explain.
/// </param>
public sealed record AssertionResult(string Name, AssertionOutcome Outcome, string Detail)
{
    /// <summary>It ran and it held. <paramref name="detail"/> is what was read back, if anything.</summary>
    public static AssertionResult Pass(string name, string detail = "") =>
        new(Named(name), AssertionOutcome.Passed, detail ?? "");

    /// <summary>It ran and it did not hold. <paramref name="detail"/> says what was read instead.</summary>
    public static AssertionResult Fail(string name, string detail) =>
        new(Named(name), AssertionOutcome.Failed, Explained(detail, nameof(detail)));

    /// <summary>
    /// It never ran. <paramref name="reason"/> names the precondition that was absent, because a
    /// hole reported without one is a hole nobody can act on.
    /// </summary>
    public static AssertionResult Unchecked(string name, string reason) =>
        new(Named(name), AssertionOutcome.Unchecked, Explained(reason, nameof(reason)));

    /// <summary>True where this assertion never ran.</summary>
    public bool DidNotRun => Outcome == AssertionOutcome.Unchecked;

    private static string Named(string name) =>
        string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("an assertion is reported under a name, and this one has none", nameof(name))
            : name.Trim();

    private static string Explained(string detail, string parameter) =>
        string.IsNullOrWhiteSpace(detail)
            ? throw new ArgumentException("an outcome that is not a pass carries the sentence that explains it", parameter)
            : detail.Trim();
}

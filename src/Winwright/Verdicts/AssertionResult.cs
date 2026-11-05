namespace Winwright.Verdicts;

/// <summary>
/// One assertion's outcome, with the name it is reported under and the sentence that explains it.
/// The name is required by construction because a degraded run's whole obligation is to name each
/// assertion that did not run, and an unnamed one cannot be named.
/// </summary>
public sealed record AssertionResult
{
    private AssertionResult(string name, AssertionOutcome outcome, string detail, Precondition? missing)
    {
        Name = name;
        Outcome = outcome;
        Detail = detail;
        Missing = missing;
    }

    /// <summary>What the assertion claimed, as the scenario spells it.</summary>
    public string Name { get; }

    /// <summary>Whether it passed, failed, or was never evaluated.</summary>
    public AssertionOutcome Outcome { get; }

    /// <summary>
    /// Why: what was read back on a failure, which precondition was absent on an unchecked one.
    /// Empty is allowed only on a pass, where there is nothing to explain.
    /// </summary>
    public string Detail { get; }

    /// <summary>
    /// The precondition this machine did not have, on an unchecked result and nowhere else. It is
    /// what separates a hole from a check that could never have run: a hole names what was absent.
    /// </summary>
    public Precondition? Missing { get; }

    /// <summary>It ran and it held. <paramref name="detail"/> is what was read back, if anything.</summary>
    public static AssertionResult Pass(string name, string detail = "") =>
        new(Named(name), AssertionOutcome.Passed, (detail ?? "").Trim(), null);

    /// <summary>It ran and it did not hold. <paramref name="detail"/> says what was read instead.</summary>
    public static AssertionResult Fail(string name, string detail) =>
        new(Named(name), AssertionOutcome.Failed, Explained(detail, nameof(detail)), null);

    /// <summary>
    /// It never ran, because <paramref name="missing"/> was absent on this machine. A precondition
    /// this machine *has* is refused here: an assertion whose preconditions were all met and which
    /// still did not run is a defect in the harness, not a hole in the environment.
    /// </summary>
    public static AssertionResult Unchecked(string name, Precondition missing)
    {
        ArgumentNullException.ThrowIfNull(missing);
        if (missing.Satisfied)
            throw new ArgumentException(
                $"'{missing.Name}' was met on this machine, so it does not explain why '{name}' did not run",
                nameof(missing));

        return new AssertionResult(Named(name), AssertionOutcome.Unchecked, missing.Absence, missing);
    }

    /// <summary>
    /// The ordinal of the trace step that settled this, counted from 1. Zero where nothing joined
    /// them — and zero is a real answer rather than a default: a result that names no step is one
    /// a reader has to find by matching prose to prose, which is the re-run this block exists to
    /// make unnecessary.
    /// </summary>
    public int Step { get; private init; }

    /// <summary>Whether a reader can go straight from this line to the step that produced it.</summary>
    public bool Traced => Step > 0;

    /// <summary>
    /// The same result, joined to the step that settled it.
    /// </summary>
    /// <param name="step">The ordinal the trace writer assigned, which is always at least one.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Where the ordinal is not one a trace ever assigns. A join to step zero is a join to
    /// nothing, and it would read in a summary exactly like a real one.
    /// </exception>
    public AssertionResult At(int step)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(step, 1);
        return this with { Step = step };
    }

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

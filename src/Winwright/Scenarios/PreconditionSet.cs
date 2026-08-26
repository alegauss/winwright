using Winwright.Verdicts;

namespace Winwright.Scenarios;

/// <summary>
/// What this machine turned out to have, answered once per run and read by every assertion that
/// declared a requirement. It is the boundary the two states are separated across: a name in here
/// that came back absent produces a hole, and a name that is in no set at all is a declaration
/// nothing could ever satisfy, refused at load.
/// </summary>
public sealed class PreconditionSet
{
    private readonly Dictionary<string, Precondition> byName;

    private PreconditionSet(Dictionary<string, Precondition> byName) => this.byName = byName;

    /// <summary>Every precondition this run measured, absent ones included.</summary>
    public IReadOnlyCollection<Precondition> All => byName.Values;

    /// <summary>Collect what was measured. A name measured twice is refused rather than resolved.</summary>
    public static PreconditionSet Of(params Precondition[] preconditions)
    {
        var collected = new Dictionary<string, Precondition>(StringComparer.Ordinal);
        foreach (var precondition in preconditions ?? [])
        {
            ArgumentNullException.ThrowIfNull(precondition);
            if (!collected.TryAdd(precondition.Name, precondition))
                throw new ArgumentException(
                    $"'{precondition.Name}' was measured twice, and the two answers cannot both be this machine's",
                    nameof(preconditions));
        }

        return new PreconditionSet(collected);
    }

    /// <summary>Whether this run measured that precondition at all.</summary>
    public bool Measured(string name) => byName.ContainsKey(name);

    /// <summary>
    /// The first precondition <paramref name="declaration"/> needs that this machine does not
    /// have, or null where it has all of them and the assertion is free to run.
    /// </summary>
    /// <exception cref="ScenarioRefusedException">
    /// Where the declaration requires something no run measures. That is not a hole — a hole is
    /// this machine falling short of a condition somebody can check — it is a requirement nothing
    /// could ever satisfy, so the assertion would be unchecked on every desk forever.
    /// </exception>
    public Precondition? FirstAbsent(AssertionDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        return FirstAbsent(declaration.Name, declaration.Requires);
    }

    /// <summary>
    /// The same, for a caller holding names rather than an assertion — a case declares its
    /// requirements the same way an assertion does, and one set of rules is what keeps the two
    /// answers the same.
    /// </summary>
    /// <param name="named">What the refusal should call the thing that requires these.</param>
    /// <param name="required">The names, in declared order.</param>
    /// <exception cref="ScenarioRefusedException">Where one is something no run measures.</exception>
    public Precondition? FirstAbsent(string named, IEnumerable<string> required)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(named);
        ArgumentNullException.ThrowIfNull(required);

        foreach (var one in required)
        {
            if (!byName.TryGetValue(one, out var precondition))
                throw new ScenarioRefusedException(
                    named,
                    $"it requires '{one}', which nothing measures, so it could never run on any machine");

            if (!precondition.Satisfied)
                return precondition;
        }

        return null;
    }
}

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

        foreach (var required in declaration.Requires)
        {
            if (!byName.TryGetValue(required, out var precondition))
                throw new ScenarioRefusedException(
                    declaration.Name,
                    $"it requires '{required}', which nothing measures, so it could never run on any machine");

            if (!precondition.Satisfied)
                return precondition;
        }

        return null;
    }
}

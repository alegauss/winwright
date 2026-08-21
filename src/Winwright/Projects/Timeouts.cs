using System.Collections.ObjectModel;

namespace Winwright.Projects;

/// <summary>
/// How long this project is willing to wait, by name. Three are seeded here so a declaration that
/// says nothing still works everywhere, and anything else a project needs it declares once — a
/// timeout typed into the case that needs it is a number nobody can tune without reading every case.
/// </summary>
public sealed class Timeouts
{
    /// <summary>What every project gets without declaring anything.</summary>
    public static IReadOnlyDictionary<string, int> Defaults { get; } =
        new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["resolve"] = 5000,
            ["act"] = 2000,
            ["launch"] = 15000,
        });

    private readonly IReadOnlyDictionary<string, int> byName;
    private readonly string declaredIn;

    private Timeouts(IReadOnlyDictionary<string, int> byName, string declaredIn)
    {
        this.byName = byName;
        this.declaredIn = declaredIn;
    }

    /// <summary>Every timeout in force, the seeded ones and the declared ones together.</summary>
    public IReadOnlyDictionary<string, int> All => byName;

    /// <summary>
    /// Fold what a project declared over the defaults. A declared value replaces the seeded one;
    /// a name nothing seeds is simply this project's own.
    /// </summary>
    /// <exception cref="ArgumentException">Where a declared timeout is not a positive number.</exception>
    public static Timeouts Declared(IReadOnlyDictionary<string, int>? declared, string declaredIn)
    {
        var folded = new Dictionary<string, int>(Defaults, StringComparer.Ordinal);
        foreach (var (name, milliseconds) in declared ?? new Dictionary<string, int>())
        {
            if (milliseconds <= 0)
                throw new ArgumentException(
                    $"{declaredIn} declares timeout '{name}' as {milliseconds} ms, and a wait of nothing is not a wait",
                    nameof(declared));

            folded[name] = milliseconds;
        }

        return new Timeouts(new ReadOnlyDictionary<string, int>(folded), declaredIn);
    }

    /// <summary>How long to wait for <paramref name="name"/>, in milliseconds.</summary>
    /// <exception cref="DeclarationMissingException">
    /// Where nothing declares it. A default invented here would be a number that differs between
    /// two checkouts of the same scenario, which is the whole thing this declaration prevents.
    /// </exception>
    public int For(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return byName.TryGetValue(name, out var milliseconds)
            ? milliseconds
            : throw new DeclarationMissingException($"timeouts.{name}", declaredIn, "a step waiting on it");
    }
}

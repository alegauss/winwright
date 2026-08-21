namespace Winwright.Verdicts;

/// <summary>
/// A condition an assertion needs in place before there is anything to observe: a second profile
/// registered, a foreground this run owns, a free notification area. It is the only thing that
/// entitles a run to report a hole — unchecked means it should have been checked and was not, so
/// the absence has a name, and a hole without one is a hole nobody can go and fix.
/// </summary>
public sealed record Precondition
{
    private Precondition(string name, bool satisfied, string absence)
    {
        Name = name;
        Satisfied = satisfied;
        Absence = absence;
    }

    /// <summary>What the assertion needs, as the scenario names it.</summary>
    public string Name { get; }

    /// <summary>Whether this machine has it.</summary>
    public bool Satisfied { get; }

    /// <summary>Why this machine does not have it. Empty where it does.</summary>
    public string Absence { get; }

    /// <summary>This machine has it, so every assertion needing it is free to run.</summary>
    public static Precondition Met(string name) => new(Named(name), true, "");

    /// <summary>
    /// This machine does not have it. <paramref name="absence"/> says what was looked for and
    /// what was there instead, because that sentence is the whole content of a degraded reading.
    /// </summary>
    public static Precondition Absent(string name, string absence)
    {
        if (string.IsNullOrWhiteSpace(absence))
            throw new ArgumentException(
                "an absent precondition says what was missing, or the hole it explains explains nothing",
                nameof(absence));

        return new Precondition(Named(name), false, absence.Trim());
    }

    private static string Named(string name) =>
        string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("a precondition is referred to by name, and this one has none", nameof(name))
            : name.Trim();
}

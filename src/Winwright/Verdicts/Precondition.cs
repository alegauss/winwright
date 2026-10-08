namespace Winwright.Verdicts;

/// <summary>
/// A condition an assertion needs in place before there is anything to observe: a second profile
/// registered, a foreground this run owns, a free notification area. It is the only thing that
/// entitles a run to report a hole — unchecked means it should have been checked and was not, so
/// the absence has a name, and a hole without one is a hole nobody can go and fix.
/// </summary>
public sealed record Precondition
{
    private Precondition(string name, bool satisfied, string absence, string presence = "")
    {
        Name = name;
        Satisfied = satisfied;
        Absence = absence;
        Presence = presence;
    }

    /// <summary>What the assertion needs, as the scenario names it.</summary>
    public string Name { get; }

    /// <summary>Whether this machine has it.</summary>
    public bool Satisfied { get; }

    /// <summary>Why this machine does not have it. Empty where it does.</summary>
    public string Absence { get; }

    /// <summary>
    /// How this machine has it, where saying so tells two satisfied readings apart. Empty on the
    /// ones where being met says everything there is to say. WW517.
    /// <para>
    /// The mirror of <see cref="Absence"/>, and it exists because the two were not mirrored. Every
    /// absence names both sides, which is WW245's rule and cost two runs to arrive at; a met reading
    /// said only that it was met — so a foreground owned by the window under test and one judged ours
    /// because an ancestor shared a root with the holder left the same record, and a keystroke that
    /// was delivered could not be told from one that may not have been.
    /// </para>
    /// <para>
    /// Empty on most conditions, deliberately. A second profile registered is registered, and a
    /// sentence about how would be the mark that marks nothing.
    /// </para>
    /// </summary>
    public string Presence { get; }

    /// <summary>This machine has it, so every assertion needing it is free to run.</summary>
    public static Precondition Met(string name) => new(Named(name), true, "");

    /// <summary>
    /// The same, saying how. WW517: for a condition whose satisfied readings are not all the same
    /// reading, where which one it was belongs in the record rather than in a reader's inference.
    /// </summary>
    /// <param name="name">What the assertion needs, as the scenario names it.</param>
    /// <param name="presence">How this machine has it.</param>
    public static Precondition Met(string name, string presence)
    {
        if (string.IsNullOrWhiteSpace(presence))
        {
            throw new ArgumentException(
                "a met precondition that says how says something, or it is the plain Met and should say so",
                nameof(presence));
        }

        return new(Named(name), true, "", presence.Trim());
    }

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

namespace Winwright.Scenarios;

/// <summary>
/// The scenario did not load. Thrown before a run starts, never during one, because everything it
/// reports is true of the file rather than of this machine: a refusal at load is the same refusal
/// on every desk, which is exactly what tells it apart from a hole.
/// </summary>
public sealed class ScenarioRefusedException : Exception
{
    /// <summary>What was refused, named the way the scenario names it.</summary>
    public string Subject { get; }

    /// <param name="subject">The assertion, step or declaration the refusal is about.</param>
    /// <param name="because">Why it cannot load, in the sentence the author has to act on.</param>
    public ScenarioRefusedException(string subject, string because)
        : base($"{subject}: {because}")
    {
        Subject = subject;
        Because = because;
    }

    /// <summary>Why it cannot load.</summary>
    public string Because { get; }
}

namespace Winwright.Scenarios;

/// <summary>
/// The scenario did not load. Thrown before a run starts and never during one, because what it
/// reports is settled before any assertion runs: either something true of the file alone, or
/// something true of the file read against the application it names — an act reaching for a
/// pattern that build's control does not carry. Either way it is the same refusal for anyone
/// holding the same two, which is exactly what tells it apart from a hole.
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

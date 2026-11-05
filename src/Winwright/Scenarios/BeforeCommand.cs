namespace Winwright.Scenarios;

/// <summary>
/// One command a fixture runs before its application launches. WW582.
/// <para>
/// Found by Modelado (MOD65): its CLI sends a job to a cloud service and records the spend in a ledger
/// the window's About reads, and the claim is that a job sent from the CLI shows in About. A fixture
/// could stage files, variables and arguments, and could not run the CLI itself in its staged folder
/// with its variables, so the case staged a hand-written ledger and proved only that About reads a file.
/// </para>
/// </summary>
/// <param name="Run">The program, relative to the project's root or naming a token, as a fixture's <c>application</c> is.</param>
/// <param name="Arguments">What it is given, with the fixture's tokens filled.</param>
/// <param name="Input">What it reads on its standard input, or null where it reads nothing.</param>
/// <param name="ExpectExit">The exit code that says it did its part.</param>
public sealed record BeforeCommand(string Run, IReadOnlyList<string> Arguments, string? Input = null, int ExpectExit = 0)
{
    /// <summary>The command as a report names it.</summary>
    public override string ToString() =>
        Arguments.Count == 0 ? Run : $"{Run} {string.Join(' ', Arguments)}";
}

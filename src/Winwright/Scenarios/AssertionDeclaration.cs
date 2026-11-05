using System.Collections.ObjectModel;

using Winwright.Verdicts;

namespace Winwright.Scenarios;

/// <summary>
/// One assertion as the scenario declares it: what it claims, what it observes to find out, and
/// the preconditions this machine has to have before there is anything to observe at all.
/// <para>
/// The subject is required, and that requirement is the whole of WW2. An assertion naming nothing
/// to observe matches nothing on any machine, which means it never fails anywhere and reads green
/// forever — worse than the absent check it was written instead of. That is a property of the
/// file, not of the desk it runs on, so it is refused when the scenario loads rather than counted
/// as a hole at run time.
/// </para>
/// </summary>
public sealed record AssertionDeclaration
{
    private AssertionDeclaration(string name, string subject, IReadOnlyList<string> requires)
    {
        Name = name;
        Subject = subject;
        Requires = requires;
    }

    /// <summary>What the assertion claims, and the name every report refers to it by.</summary>
    public string Name { get; }

    /// <summary>What is observed to settle the claim — the element, window or file it reads.</summary>
    public string Subject { get; }

    /// <summary>The preconditions this assertion needs present, by name and in declared order.</summary>
    public IReadOnlyList<string> Requires { get; }

    /// <summary>Declare one, refusing at load whatever could never be observed on any machine.</summary>
    /// <exception cref="ScenarioRefusedException">Where the declaration covers nothing.</exception>
    public static AssertionDeclaration Of(string name, string subject, params string[] requires)
    {
        var named = Require(name, "an assertion is reported under a name, and this one has none", "<unnamed assertion>");

        if (string.IsNullOrWhiteSpace(subject))
            throw new ScenarioRefusedException(
                named,
                "it names nothing to observe, so it matches nothing on any machine and would read green forever");

        var collected = new List<string>();
        foreach (var required in requires ?? [])
        {
            var precondition = Require(required, "a precondition is referred to by name, and this one has none", named);
            if (collected.Contains(precondition, StringComparer.Ordinal))
                throw new ScenarioRefusedException(named, $"it requires '{precondition}' twice");

            collected.Add(precondition);
        }

        return new AssertionDeclaration(named, subject.Trim(), new ReadOnlyCollection<string>(collected));
    }

    /// <summary>
    /// The result to report where <paramref name="missing"/> is one of this assertion's own
    /// preconditions and this machine does not have it. A precondition this assertion never
    /// declared is refused: a hole is explained by something the check actually needed.
    /// </summary>
    public AssertionResult Unchecked(Precondition missing)
    {
        ArgumentNullException.ThrowIfNull(missing);
        if (!Requires.Contains(missing.Name, StringComparer.Ordinal))
            throw new ArgumentException(
                $"'{Name}' never declared '{missing.Name}', so its absence does not explain why this did not run",
                nameof(missing));

        return AssertionResult.Unchecked(Name, missing);
    }

    private static string Require(string? value, string because, string subject) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ScenarioRefusedException(subject, because)
            : value.Trim();
}

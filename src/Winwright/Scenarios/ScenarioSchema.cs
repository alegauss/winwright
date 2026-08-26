using System.Collections.ObjectModel;

namespace Winwright.Scenarios;

/// <summary>
/// One field of the scenario format: its name, whether it has to be there, what it is for, and the
/// closed list of what it accepts where it has one.
/// </summary>
/// <param name="Name">The key, spelled as the file spells it.</param>
/// <param name="Required">Whether a case or a step without it is refused.</param>
/// <param name="Means">What it is for, in the sentence a refusal can be read beside.</param>
/// <param name="OneOf">Everything it accepts, or empty where it takes free text.</param>
public sealed record Field(string Name, bool Required, string Means, IReadOnlyList<string> OneOf)
{
    /// <summary>The one line a listing of the format shows.</summary>
    public override string ToString()
    {
        var of = OneOf.Count == 0 ? "" : $" — one of: {string.Join(", ", OneOf)}";
        return $"{Name}{(Required ? "" : " (optional)")}: {Means}{of}";
    }
}

/// <summary>
/// The scenario format, as data.
/// <para>
/// WW58. A format that lives only in a loader is a convention: the author writes a file, the loader
/// reports afterwards, and the report arrives once the prose exists. The saving this block is about
/// is the analysis rather than the characters — so the format has to be readable <em>before</em> a
/// file is written, which means it has to be something other than the code that reads one.
/// </para>
/// <para>
/// It is also what <see cref="ScenarioFile"/> lists in a refusal. A key nobody recognises is
/// refused with the keys there are, and the two lists cannot drift because there is only one.
/// </para>
/// </summary>
public static class ScenarioSchema
{
    /// <summary>The key the file's cases are under.</summary>
    public const string Cases = "cases";

    /// <summary>The key a case's steps are under.</summary>
    public const string Steps = "steps";

    /// <summary>The key a case's tags are under.</summary>
    public const string Tags = "tags";

    /// <summary>The key a case's preconditions are under.</summary>
    public const string Needs = "needs";

    /// <summary>The key a case's justification is under.</summary>
    public const string Catches = "catches";

    /// <summary>The key a file's fixtures are under.</summary>
    public const string Fixtures = "fixtures";

    /// <summary>
    /// What the file itself may say. Here for the same reason the other three lists are: a
    /// misspelled <c>"fixtres"</c> that loads is every case in the file silently launched against
    /// the application as it comes, with expectations describing an environment nothing set up.
    /// </summary>
    public static IReadOnlyList<Field> File { get; } = new ReadOnlyCollection<Field>(
    [
        new(Cases, true, "the cases the file holds, as an array", []),
        new(Fixtures, false, "what to launch them against, as an array", []),
    ]);

    /// <summary>What a case may say.</summary>
    public static IReadOnlyList<Field> Case { get; } = new ReadOnlyCollection<Field>(
    [
        new("name", true, "what the case is called, and what a run of it is reported under", []),
        new(Steps, true, "its steps, in the order they are performed", []),
        new(Tags, false, "what selects it besides its name, as an array of words", []),
        new(Needs, false, "what this machine has to have before it can observe anything, as an array of names", []),
        new(Catches, false, "the defect it exists to catch — what went wrong without it", []),
        new("filed", false, "the task it was filed under", []),
        new("fixture", false, $"which of this file's '{Fixtures}' to launch it against", []),
        new("onlyReads", false, "that it leaves the window as it found it, so a window may be lent to it", ["true", "false"]),
    ]);

    /// <summary>What a fixture may say.</summary>
    public static IReadOnlyList<Field> Fixture { get; } = new ReadOnlyCollection<Field>(
    [
        new("name", true, "what to call it, and what a case names to be launched against it", []),
        new("environment", false, "the sampled environment it is — the one field both the launch and the expectations read", []),
        new("flag", false, "the argument the environment reaches the application through, without its value", []),
        new("arguments", false, "everything else the launch carries, as an array", []),
        new("variables", false, "the environment variables it sets, as an object", []),
        new("shareable", false, "that this window may be lent to a case that only reads it", ["true", "false"]),
    ]);

    /// <summary>What a step may say.</summary>
    public static IReadOnlyList<Field> Step { get; } = new ReadOnlyCollection<Field>(
    [
        new("locator", true, "what to act on, in the locator grammar", []),
        new("act", true, "what to do to it", ActVerb.All.Select(verb => verb.Name).ToList()),
        new("with", false, "what the act needs said, where it needs anything", []),
        new("expect", false, "what the reading should be once the act has landed", []),
        new("reads", false, "which reading the expectation is about", ReadBack.All.Select(one => one.Name).ToList()),
        new("meansIt", false, "that this step means a destructive entry it names", ["true", "false"]),
        new("named", false, "what a report should call it, where the act and the locator will not do", []),
    ]);

    /// <summary>The format as a tool or an agent is told it, before anything is written.</summary>
    public static IReadOnlyList<string> Render()
    {
        var lines = new List<string>
        {
            $"A scenario file is an object with '{Cases}': an array of cases, and optionally "
                + $"'{Fixtures}': an array of what to launch them against.",
            "A case:",
        };

        lines.AddRange(Case.Select(field => $"  {field}"));
        lines.Add("A step:");
        lines.AddRange(Step.Select(field => $"  {field}"));
        lines.Add("A fixture:");
        lines.AddRange(Fixture.Select(field => $"  {field}"));
        return new ReadOnlyCollection<string>(lines);
    }

    /// <summary>The keys of <paramref name="fields"/>, as a refusal lists them.</summary>
    internal static string Spelled(IReadOnlyList<Field> fields) =>
        string.Join(", ", fields.Select(field => field.Name));

    /// <summary>Whether <paramref name="fields"/> knows <paramref name="key"/>.</summary>
    internal static bool Knows(IReadOnlyList<Field> fields, string key)
    {
        foreach (var field in fields)
            if (string.Equals(field.Name, key, StringComparison.Ordinal))
                return true;

        return false;
    }
}

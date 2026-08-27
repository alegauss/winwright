using System.Collections.ObjectModel;
using System.Text.Json.Nodes;

namespace Winwright.Scenarios;

/// <summary>
/// What kind of value a field holds.
/// <para>
/// WW66. The prose beside a field already said this — "as an array", "as an object" — and prose is
/// what a reader reads and a tool cannot. A tool carrying the format as its input schema needs the
/// kind as data, and the loader needs it to be the <em>same</em> data: a schema that says a field is
/// text where the loader reads an array is a tool that accepts what the run refuses, which is a
/// guess dressed as a constraint.
/// </para>
/// </summary>
public enum Taking
{
    /// <summary>Text.</summary>
    Text,

    /// <summary>True or false.</summary>
    Truth,

    /// <summary>An array of text.</summary>
    Words,

    /// <summary>An object whose every value is text.</summary>
    Pairs,

    /// <summary>An array of cases.</summary>
    Cases,

    /// <summary>An array of steps.</summary>
    Steps,

    /// <summary>An array of fixtures.</summary>
    Fixtures,
}

/// <summary>
/// One field of the scenario format: its name, whether it has to be there, what it holds, what it is
/// for, and the closed list of what it accepts where it has one.
/// </summary>
/// <param name="Name">The key, spelled as the file spells it.</param>
/// <param name="Required">Whether a case or a step without it is refused.</param>
/// <param name="Holds">What kind of value it takes, which is what the loader reads it as.</param>
/// <param name="Means">What it is for, in the sentence a refusal can be read beside.</param>
/// <param name="OneOf">
/// Everything it accepts, or empty where it takes free text. Empty on a <see cref="Taking.Truth"/>
/// field too: what a boolean accepts is already said by what it holds, and saying it twice is the
/// second spelling that goes on saying the old thing after the first moves.
/// </param>
public sealed record Field(string Name, bool Required, Taking Holds, string Means, IReadOnlyList<string> OneOf)
{
    /// <summary>The one line a listing of the format shows.</summary>
    public override string ToString()
    {
        var of = (OneOf.Count, Holds) switch
        {
            ( > 0, _) => $" — one of: {string.Join(", ", OneOf)}",
            (_, Taking.Truth) => " — one of: true, false",
            _ => "",
        };

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
/// <para>
/// WW66 made that one list say enough for a tool to carry it. <see cref="AsJsonSchema"/> is the
/// format as a tool's input schema, so the fields arrive already named, already typed and already
/// constrained rather than typed from memory — and <see cref="Of"/> is what the loader asks before
/// reading a field, so the kind the schema publishes is the kind the run enforces.
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
        new(Cases, true, Taking.Cases, "the cases the file holds, as an array", []),
        new(Fixtures, false, Taking.Fixtures, "what to launch them against, as an array", []),
    ]);

    /// <summary>What a case may say.</summary>
    public static IReadOnlyList<Field> Case { get; } = new ReadOnlyCollection<Field>(
    [
        new("name", true, Taking.Text, "what the case is called, and what a run of it is reported under", []),
        new(Steps, true, Taking.Steps, "its steps, in the order they are performed", []),
        new(Tags, false, Taking.Words, "what selects it besides its name, as an array of words", []),
        new(Needs, false, Taking.Words, "what this machine has to have before it can observe anything, as an array of names", []),
        new(Catches, false, Taking.Text, "the defect it exists to catch — what went wrong without it", []),
        new("filed", false, Taking.Text, "the task it was filed under", []),
        new("fixture", false, Taking.Text, $"which of this file's '{Fixtures}' to launch it against", []),
        new("onlyReads", false, Taking.Truth, "that it leaves the window as it found it, so a window may be lent to it", []),
    ]);

    /// <summary>What a fixture may say.</summary>
    public static IReadOnlyList<Field> Fixture { get; } = new ReadOnlyCollection<Field>(
    [
        new("name", true, Taking.Text, "what to call it, and what a case names to be launched against it", []),
        new("environment", false, Taking.Text, "the sampled environment it is — the one field both the launch and the expectations read", []),
        new("flag", false, Taking.Text, "the argument the environment reaches the application through, without its value", []),
        new("arguments", false, Taking.Words, "everything else the launch carries, as an array", []),
        new("variables", false, Taking.Pairs, "the environment variables it sets, as an object", []),
        new("shareable", false, Taking.Truth, "that this window may be lent to a case that only reads it", []),
        new("language", false, Taking.Text, "the language tag the window it launches is in, so a derived set reads the strings that window is actually showing", []),
    ]);

    /// <summary>What a step may say.</summary>
    public static IReadOnlyList<Field> Step { get; } = new ReadOnlyCollection<Field>(
    [
        new("locator", true, Taking.Text, "what to act on, in the locator grammar", []),
        new("act", true, Taking.Text, "what to do to it", ActVerb.All.Select(verb => verb.Name).ToList()),
        new("with", false, Taking.Text, "what the act needs said, where it needs anything", []),
        new("expect", false, Taking.Text, "what the reading should be once the act has landed", []),
        new("reads", false, Taking.Text, "which reading the expectation is about", ReadBack.All.Select(one => one.Name).ToList()),
        new("moves", false, Taking.Truth, "that the reading should end up different, where the case cannot know what it will be", []),
        new("answers", false, Taking.Truth, "that the reading it names should say something rather than nothing, where the case cannot know what", []),
        new("matches", false, Taking.Text, "the regular expression the reading should match, where the case cannot name the value but can name its shape", []),
        new("discloses", false, Taking.Truth, "that the act put something under the locator that was not in the tree before it", []),
        new("sameAs", false, Taking.Text, "the 'named' of an earlier step in this case whose reading this one claims to be back to, for the round trip whose value no case can name", []),
        new("never", false, Taking.Text, "the key whose string must not be showing anywhere in the window at any moment while this step waits for its locator — a key and never the text, like the project's own loading strings", []),
        new("covers", false, Taking.Text, "the key whose every string must be read somewhere the locator matches, without its trailing dot — derived from the project's own strings and never listed here", []),
        new("meansIt", false, Taking.Truth, "that this step means a destructive entry it names", []),
        new("named", false, Taking.Text, "what a report should call it, where the act and the locator will not do", []),
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

    /// <summary>
    /// The format as JSON Schema, which is what a tool carries as its input schema.
    /// <para>
    /// WW66. <see cref="Render"/> is the format as prose, and prose is what an agent reads and then
    /// types a key from memory. This is the same list as a constraint: the wrong kind of value and
    /// the misspelled key are things the caller cannot express, rather than things the loader gets
    /// to explain afterwards.
    /// </para>
    /// <para>
    /// <c>additionalProperties</c> is false at every level for exactly the reason
    /// <see cref="ScenarioFile"/> refuses a key nobody recognises: <c>"expects"</c> beside
    /// <c>"expect"</c> is a check the author wrote and the run never made, and a schema that shrugs
    /// at it hands that green back.
    /// </para>
    /// </summary>
    public static JsonObject AsJsonSchema() => Shape(File);

    /// <summary>
    /// The field <paramref name="fields"/> calls <paramref name="key"/>, where it holds
    /// <paramref name="holds"/>.
    /// <para>
    /// What makes the published kind and the enforced kind one thing. The loader asks this before
    /// reading a field, so a schema saying <c>tags</c> is text while the loader reads an array
    /// cannot survive a single load — and the suite loads scenarios everywhere.
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Where the schema has no such field, or says it holds something else. A harness error and
    /// never a <see cref="ScenarioRefusedException"/>: nothing about the author's file is wrong.
    /// </exception>
    public static Field Of(IReadOnlyList<Field> fields, string key, Taking holds)
    {
        foreach (var field in fields)
        {
            if (!string.Equals(field.Name, key, StringComparison.Ordinal))
                continue;

            return field.Holds == holds
                ? field
                : throw new InvalidOperationException(
                    $"the schema says '{key}' holds {field.Holds} and it is being read as {holds}");
        }

        throw new InvalidOperationException(
            $"the schema has no '{key}', so nothing can read one; there is {Spelled(fields)}");
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

    /// <summary>One shape — the file, a case, a step or a fixture — as a JSON Schema object.</summary>
    private static JsonObject Shape(IReadOnlyList<Field> fields)
    {
        var properties = new JsonObject();
        var required = new JsonArray();
        foreach (var field in fields)
        {
            properties[field.Name] = Described(field);
            if (field.Required)
                required.Add(field.Name);
        }

        var shape = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["additionalProperties"] = false,
        };

        if (required.Count > 0)
            shape["required"] = required;

        return shape;
    }

    /// <summary>One field as a JSON Schema value, off what it holds and what it accepts.</summary>
    private static JsonObject Described(Field field)
    {
        var described = field.Holds switch
        {
            Taking.Truth => new JsonObject { ["type"] = "boolean" },
            Taking.Words => new JsonObject
            {
                ["type"] = "array",
                ["items"] = new JsonObject { ["type"] = "string" },
            },
            Taking.Pairs => new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = new JsonObject { ["type"] = "string" },
            },
            Taking.Cases => ArrayOf(Case),
            Taking.Steps => ArrayOf(Step),
            Taking.Fixtures => ArrayOf(Fixture),
            _ => new JsonObject { ["type"] = "string" },
        };

        described["description"] = field.Means;
        if (field.OneOf.Count > 0)
        {
            var accepted = new JsonArray();
            foreach (var one in field.OneOf)
                accepted.Add(one);

            described["enum"] = accepted;
        }

        return described;
    }

    private static JsonObject ArrayOf(IReadOnlyList<Field> of) => new()
    {
        ["type"] = "array",
        ["items"] = Shape(of),
    };
}

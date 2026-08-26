using System.Collections.ObjectModel;
using System.Text.Json;

namespace Winwright.Scenarios;

/// <summary>
/// A file of cases, read field by field and refused at the first one that is wrong.
/// <para>
/// WW58, which is roadkeep's first law applied to a scenario instead of to a line. A linter reports
/// after the text exists: the author has written the case, and what arrives is a request to delete
/// it. This refuses at the point of insertion — the first key it does not recognise, the first
/// value of the wrong kind, the first act that is not an act — and it stops there rather than
/// collecting a list, because a list is a second analysis and the analysis is what the saving was.
/// </para>
/// <para>
/// A refusal names its address as a path into the file rather than as a line. Provenance by line
/// exists in this tree, and it exists for a strings file with four hundred keys in it, where naming
/// the file is naming nothing. <c>cases[2].steps[1].act</c> is already tighter than a line: it says
/// which case, which step, and which field, and nothing about it goes stale when the file is
/// reformatted.
/// </para>
/// <para>
/// A key nobody recognises is refused rather than ignored. Ignoring it is what makes a format a
/// convention: <c>"expects"</c> beside <c>"expect"</c> loads, runs, checks nothing and reads green,
/// which is the unearned green arriving through a typo.
/// </para>
/// </summary>
public sealed class ScenarioFile
{
    /// <summary>What a scenario file is called where a project does not say otherwise.</summary>
    public const string Extension = ".cases.json";

    private ScenarioFile(string path, IReadOnlyList<CaseDeclaration> cases)
    {
        Path = path;
        Cases = cases;
    }

    /// <summary>The file that was read, as a full path.</summary>
    public string Path { get; }

    /// <summary>Its cases, in file order.</summary>
    public IReadOnlyList<CaseDeclaration> Cases { get; }

    /// <summary>
    /// The fixtures its cases actually name, in the order they are first named. Derived from the
    /// cases rather than kept beside them, so a fixture declared and used by nothing never reaches a
    /// report as though something ran against it.
    /// </summary>
    public IReadOnlyList<FixtureDeclaration> Fixtures => new ReadOnlyCollection<FixtureDeclaration>(
        Cases.Select(one => one.Fixture)
            .Where(one => !ReferenceEquals(one, FixtureDeclaration.Plain))
            .Distinct()
            .ToList());

    /// <summary>Read one, refusing at the first field that is wrong.</summary>
    /// <param name="path">The file.</param>
    /// <param name="suite">
    /// What the rest of the suite declares, where a caller has it. A case may then name a fixture
    /// another file declared — which is WW214, and the reason a launch three files need is written
    /// once. Null resolves against this file alone.
    /// </param>
    /// <exception cref="ScenarioRefusedException">Where it is absent, is not JSON, or has a wrong field.</exception>
    public static ScenarioFile Load(string path, FixtureSet? suite = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var full = System.IO.Path.GetFullPath(path.Trim());
        return new ScenarioFile(full, Read(full, Text(full), suite));
    }

    private static string Text(string full)
    {
        try
        {
            return File.ReadAllText(full);
        }
        catch (Exception unreadable) when (unreadable is IOException or UnauthorizedAccessException)
        {
            throw new ScenarioRefusedException(full, $"it could not be read — {unreadable.Message}");
        }
    }

    /// <summary>
    /// Every scenario file under a directory, in path order, and the cases they hold.
    /// <para>
    /// Two passes, because the fixtures are the suite's and the cases are each file's. The first
    /// collects every fixture every file declares, so the second can resolve a case against a launch
    /// declared next door — and a name two files declare is refused in the first pass, naming both,
    /// before any case has resolved against either of them.
    /// </para>
    /// <para>
    /// A case name declared in two files is refused too. WW59 runs a case by name, and a name that
    /// selects two cases across a suite is the same ambiguity a name declared twice in one file is —
    /// it just costs a second file to see.
    /// </para>
    /// </summary>
    /// <param name="directory">Where to look, walked recursively.</param>
    /// <exception cref="ScenarioRefusedException">
    /// Where the directory is absent, a file will not load, or two files declare one case or fixture name.
    /// </exception>
    public static IReadOnlyList<ScenarioFile> LoadAll(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        var root = System.IO.Path.GetFullPath(directory.Trim());
        if (!Directory.Exists(root))
            throw new ScenarioRefusedException(root, "there is no such directory, so there are no cases under it");

        var found = Directory.GetFiles(root, $"*{Extension}", SearchOption.AllDirectories);
        Array.Sort(found, StringComparer.OrdinalIgnoreCase);

        var suite = FixtureSet.Across(found.Select(path => new ScenarioSource(path, Text(path))));
        var loaded = new List<ScenarioFile>();
        var whose = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in found)
        {
            var file = Load(path, suite);
            foreach (var one in file.Cases)
            {
                if (whose.TryGetValue(one.Name, out var already))
                {
                    throw new ScenarioRefusedException(
                        one.Name,
                        $"it is declared in {already} and again in {file.Path}, so its name selects two cases");
                }

                whose[one.Name] = file.Path;
            }

            loaded.Add(file);
        }

        return new ReadOnlyCollection<ScenarioFile>(loaded);
    }

    /// <summary>Every case in <paramref name="files"/>, in file order then declared order.</summary>
    /// <param name="files">What <see cref="LoadAll"/> read.</param>
    public static IReadOnlyList<CaseDeclaration> Across(IEnumerable<ScenarioFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        return new ReadOnlyCollection<CaseDeclaration>(files.SelectMany(file => file.Cases).ToList());
    }

    /// <summary>Read cases out of JSON that is already in hand, under a name for the refusals.</summary>
    /// <param name="named">What the refusals should call it.</param>
    /// <param name="json">The text.</param>
    /// <param name="suite">
    /// What the rest of the suite declares. This file's own fixtures are folded in on top, and a
    /// name another file already declared is the drift WW214 refuses.
    /// </param>
    /// <exception cref="ScenarioRefusedException">Where it is not JSON, or has a wrong field.</exception>
    public static IReadOnlyList<CaseDeclaration> Read(string named, string json, FixtureSet? suite = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(named);
        ArgumentNullException.ThrowIfNull(json);

        using var document = Parsed(named, json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new ScenarioRefusedException(named, $"a scenario file is an object with '{ScenarioSchema.Cases}' in it");

        Unknown(named, root, ScenarioSchema.File);

        ScenarioSchema.Of(ScenarioSchema.File, ScenarioSchema.Cases, Taking.Cases);
        if (!root.TryGetProperty(ScenarioSchema.Cases, out var cases))
            throw new ScenarioRefusedException(named, $"it declares no '{ScenarioSchema.Cases}', so it holds no case");

        if (cases.ValueKind != JsonValueKind.Array)
            throw new ScenarioRefusedException($"{named} {ScenarioSchema.Cases}", "it is not an array of cases");

        var fixtures = (suite ?? FixtureSet.Empty).With(named, Fixtured(named, root));
        var read = new List<CaseDeclaration>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;
        foreach (var one in cases.EnumerateArray())
        {
            var at = $"{named} {ScenarioSchema.Cases}[{index}]";
            var declared = OneCase(at, one, fixtures);

            // Refused rather than kept: WW59 runs a case by name, and a name selecting two cases is
            // a filter whose answer depends on which one the loader happened to reach first.
            if (!seen.Add(declared.Name))
                throw new ScenarioRefusedException(at, $"'{declared.Name}' is declared twice in this file");

            read.Add(declared);
            index++;
        }

        if (read.Count == 0)
            throw new ScenarioRefusedException($"{named} {ScenarioSchema.Cases}", "it holds no cases, so there is nothing to run");

        return new ReadOnlyCollection<CaseDeclaration>(read);
    }

    private static JsonDocument Parsed(string named, string json)
    {
        try
        {
            return JsonDocument.Parse(
                json,
                new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        }
        catch (JsonException unparseable)
        {
            throw new ScenarioRefusedException(named, $"it is not JSON — {unparseable.Message}");
        }
    }

    /// <summary>
    /// The fixtures one file declares, without reading its cases.
    /// <para>
    /// The first of WW214's two passes. A suite's fixtures have to be collected before any case
    /// resolves against them, or a case naming a launch declared next door is refused for naming
    /// something that exists.
    /// </para>
    /// </summary>
    /// <param name="named">What refusals should call the file.</param>
    /// <param name="json">Its text.</param>
    /// <exception cref="ScenarioRefusedException">Where it is not JSON, or a fixture in it is wrong.</exception>
    internal static IReadOnlyList<FixtureDeclaration> FixturesIn(string named, string json)
    {
        using var document = Parsed(named, json);
        return document.RootElement.ValueKind == JsonValueKind.Object
            ? Fixtured(named, document.RootElement)
            : [];
    }

    /// <summary>
    /// The fixtures this file declares, in declared order. Declared at the file rather than on each
    /// case so that several cases can name one — which is what makes WW62's lending expressible at
    /// all, and what stops the same launch being written out three times and drifting on the second.
    /// </summary>
    private static IReadOnlyList<FixtureDeclaration> Fixtured(string named, JsonElement root)
    {
        var read = new List<FixtureDeclaration>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ScenarioSchema.Of(ScenarioSchema.File, ScenarioSchema.Fixtures, Taking.Fixtures);
        if (!root.TryGetProperty(ScenarioSchema.Fixtures, out var fixtures) || fixtures.ValueKind == JsonValueKind.Null)
            return read;

        if (fixtures.ValueKind != JsonValueKind.Array)
            throw new ScenarioRefusedException($"{named} {ScenarioSchema.Fixtures}", "it is not an array of fixtures");

        var index = 0;
        foreach (var one in fixtures.EnumerateArray())
        {
            var at = $"{named} {ScenarioSchema.Fixtures}[{index}]";
            if (one.ValueKind != JsonValueKind.Object)
                throw new ScenarioRefusedException(at, "a fixture is an object");

            Unknown(at, one, ScenarioSchema.Fixture);

            var name = Text(at, one, ScenarioSchema.Fixture, "name");
            var declared = Addressed(at, () => FixtureDeclaration.Of(
                name!,
                Text(at, one, ScenarioSchema.Fixture, "environment"),
                Text(at, one, ScenarioSchema.Fixture, "flag"),
                Words(at, one, ScenarioSchema.Fixture, "arguments"),
                Pairs(at, one, ScenarioSchema.Fixture, "variables"),
                Truth(at, one, ScenarioSchema.Fixture, "shareable")));

            if (!seen.Add(declared.Name))
                throw new ScenarioRefusedException(at, $"'{declared.Name}' is declared twice, so a case naming it names two");

            read.Add(declared);
            index++;
        }

        return read;
    }

    private static CaseDeclaration OneCase(string at, JsonElement one, FixtureSet fixtures)
    {
        if (one.ValueKind != JsonValueKind.Object)
            throw new ScenarioRefusedException(at, "a case is an object");

        Unknown(at, one, ScenarioSchema.Case);

        var name = Text(at, one, ScenarioSchema.Case, "name");
        ScenarioSchema.Of(ScenarioSchema.Case, ScenarioSchema.Steps, Taking.Steps);
        if (!one.TryGetProperty(ScenarioSchema.Steps, out var steps))
            throw new ScenarioRefusedException($"{at}.{ScenarioSchema.Steps}", "a case declares its steps, and this one declares none");

        if (steps.ValueKind != JsonValueKind.Array)
            throw new ScenarioRefusedException($"{at}.{ScenarioSchema.Steps}", "it is not an array of steps");

        var declared = new List<StepDeclaration>();
        var index = 0;
        foreach (var step in steps.EnumerateArray())
        {
            declared.Add(OneStep($"{at}.{ScenarioSchema.Steps}[{index}]", step));
            index++;
        }

        var tags = Words(at, one, ScenarioSchema.Case, ScenarioSchema.Tags);
        var needs = Words(at, one, ScenarioSchema.Case, ScenarioSchema.Needs);
        var catches = Text(at, one, ScenarioSchema.Case, ScenarioSchema.Catches);
        var filed = Text(at, one, ScenarioSchema.Case, "filed");
        var against = Against(at, one, fixtures);
        var onlyReads = Truth(at, one, ScenarioSchema.Case, "onlyReads");

        return Addressed(at, () => CaseDeclaration.Declared(
            name!, declared, tags, needs, catches, filed, against, onlyReads));
    }

    /// <summary>
    /// The fixture this case names, resolved against everything the suite declares. A name nothing
    /// declares is refused with the ones there are: a case launched against a fixture that does not
    /// exist would otherwise silently get the application as it comes, and its expectations describe
    /// an environment nothing put the window into.
    /// </summary>
    private static FixtureDeclaration? Against(string at, JsonElement one, FixtureSet fixtures)
    {
        if (Text(at, one, ScenarioSchema.Case, "fixture") is not { } named)
            return null;

        return fixtures.Named(named)
            ?? throw new ScenarioRefusedException(
                $"{at}.fixture", $"no fixture is called '{named}'; {fixtures.Spelled()}");
    }

    private static StepDeclaration OneStep(string at, JsonElement step)
    {
        if (step.ValueKind != JsonValueKind.Object)
            throw new ScenarioRefusedException(at, "a step is an object");

        Unknown(at, step, ScenarioSchema.Step);

        // Every field is read before anything is declared, so a refusal about a field's kind wears
        // that field's own address and never the step's with the field's in brackets after it.
        var locator = Text(at, step, ScenarioSchema.Step, "locator");
        var act = Text(at, step, ScenarioSchema.Step, "act");
        var with = Text(at, step, ScenarioSchema.Step, "with");
        var expect = Text(at, step, ScenarioSchema.Step, "expect");
        var reads = Text(at, step, ScenarioSchema.Step, "reads");
        var moves = Truth(at, step, ScenarioSchema.Step, "moves");
        var meansIt = Truth(at, step, ScenarioSchema.Step, "meansIt");
        var named = Text(at, step, ScenarioSchema.Step, "named");

        return Addressed(at, () => StepDeclaration.Of(locator!, act!, with, expect, reads, meansIt, named, moves));
    }

    /// <summary>
    /// Refuse a key the format does not have. This is the whole difference between a format and a
    /// convention: a misspelled optional key that loads is a check the author wrote and the run
    /// never made.
    /// </summary>
    private static void Unknown(string at, JsonElement element, IReadOnlyList<Field> fields)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!ScenarioSchema.Knows(fields, property.Name))
            {
                throw new ScenarioRefusedException(
                    $"{at}.{property.Name}",
                    $"there is no such field; there is {ScenarioSchema.Spelled(fields)}");
            }
        }
    }

    /// <summary>
    /// Read a text field, asking the schema what it holds and whether it has to be there.
    /// <para>
    /// WW66. Whether a field is text and whether it is required used to be said twice — once in the
    /// schema a tool publishes and once at the call site that reads it — and the copy that drifts is
    /// the published one, because only the call site is exercised. Asking makes a mismatch a harness
    /// error on the first load rather than a tool that accepts what the run refuses.
    /// </para>
    /// </summary>
    private static string? Text(string at, JsonElement element, IReadOnlyList<Field> fields, string key)
    {
        var field = ScenarioSchema.Of(fields, key, Taking.Text);
        if (!element.TryGetProperty(key, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return field.Required
                ? throw new ScenarioRefusedException($"{at}.{key}", "it is not there, and it has to be")
                : null;
        }

        if (value.ValueKind != JsonValueKind.String)
            throw new ScenarioRefusedException($"{at}.{key}", $"it is {Kind(value)} and it has to be text");

        return value.GetString();
    }

    private static IReadOnlyList<string> Words(string at, JsonElement element, IReadOnlyList<Field> fields, string key)
    {
        ScenarioSchema.Of(fields, key, Taking.Words);
        if (!element.TryGetProperty(key, out var value) || value.ValueKind == JsonValueKind.Null)
            return [];

        if (value.ValueKind != JsonValueKind.Array)
            throw new ScenarioRefusedException($"{at}.{key}", $"it is {Kind(value)} and it has to be an array of words");

        var read = new List<string>();
        var index = 0;
        foreach (var word in value.EnumerateArray())
        {
            if (word.ValueKind != JsonValueKind.String)
                throw new ScenarioRefusedException($"{at}.{key}[{index}]", $"it is {Kind(word)} and it has to be text");

            read.Add(word.GetString()!);
            index++;
        }

        return read;
    }

    private static IReadOnlyDictionary<string, string> Pairs(
        string at, JsonElement element, IReadOnlyList<Field> fields, string key)
    {
        ScenarioSchema.Of(fields, key, Taking.Pairs);
        var read = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!element.TryGetProperty(key, out var value) || value.ValueKind == JsonValueKind.Null)
            return read;

        if (value.ValueKind != JsonValueKind.Object)
            throw new ScenarioRefusedException($"{at}.{key}", $"it is {Kind(value)} and it has to be an object");

        foreach (var pair in value.EnumerateObject())
        {
            if (pair.Value.ValueKind != JsonValueKind.String)
            {
                throw new ScenarioRefusedException(
                    $"{at}.{key}.{pair.Name}", $"it is {Kind(pair.Value)} and it has to be text");
            }

            read[pair.Name] = pair.Value.GetString()!;
        }

        return read;
    }

    private static bool Truth(string at, JsonElement element, IReadOnlyList<Field> fields, string key)
    {
        ScenarioSchema.Of(fields, key, Taking.Truth);
        if (!element.TryGetProperty(key, out var value) || value.ValueKind == JsonValueKind.Null)
            return false;

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new ScenarioRefusedException($"{at}.{key}", $"it is {Kind(value)} and it has to be true or false"),
        };
    }

    /// <summary>
    /// Run a declaration's own refusal and put the file address in front of it. The judgements stay
    /// in one place — a loader that re-checked the fields would be a second set of rules that drifts
    /// from the first — and what the loader adds is where in the file to go.
    /// </summary>
    private static T Addressed<T>(string at, Func<T> declaring)
    {
        try
        {
            return declaring();
        }
        catch (ScenarioRefusedException refused)
        {
            throw new ScenarioRefusedException($"{at} ({refused.Subject})", refused.Because);
        }
    }

    private static string Kind(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Number => "a number",
        JsonValueKind.Array => "an array",
        JsonValueKind.Object => "an object",
        JsonValueKind.True or JsonValueKind.False => "true or false",
        _ => "not text",
    };
}

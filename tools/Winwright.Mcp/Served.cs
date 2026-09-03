using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;

using Winwright.Projects;
using Winwright.Scenarios;

namespace Winwright.Mcp;

/// <summary>What a tool answered: the text, and whether it is a refusal rather than an answer.</summary>
/// <param name="Text">What the caller is told.</param>
/// <param name="Refused">
/// Whether this is a refusal. Reported as a tool error rather than as prose, because a caller that
/// has to read the sentence to find out whether it worked is a caller that will stop reading.
/// </param>
public sealed record Answer(string Text, bool Refused = false);

/// <summary>One tool this server offers.</summary>
/// <param name="Name">What a call names.</param>
/// <param name="Summary">What it is for, as the listing says it.</param>
/// <param name="Schema">Its input schema.</param>
/// <param name="Answering">What it does with the arguments it was given.</param>
public sealed record Offered(string Name, string Summary, JsonObject Schema, Func<JsonObject, Answer> Answering);

/// <summary>
/// The tools the plugin carries, and what one JSON-RPC message gets back.
/// <para>
/// WW66. A case is a data file, and until now the only way to learn its format was to read prose and
/// then type a key from memory. A guess costs a refusal and a retry at best; at worst it is
/// <c>"expects"</c> beside <c>"expect"</c>, which loads under a schema that shrugs and reads green.
/// <see cref="ScenarioSchema.AsJsonSchema"/> is the format as a constraint, so the fields arrive
/// already named, already typed and already closed — the misspelling is not something the caller can
/// express.
/// </para>
/// <para>
/// The dispatch is a function from a message to a message and never a read off a pipe, so the suite
/// exercises every answer without launching anything. <see cref="Program"/> is the pipe and nothing
/// else, which is the whole of what cannot be tested in process.
/// </para>
/// </summary>
public static class Served
{
    /// <summary>The protocol revision this speaks.</summary>
    public const string Protocol = "2025-06-18";

    /// <summary>What this server calls itself, which is the name the plugin wires.</summary>
    public const string Named = "winwright";

    /// <summary>What a message naming a method nobody has exits with, as JSON-RPC spells it.</summary>
    public const int NoSuchMethod = -32601;

    /// <summary>What a message whose parameters could not be used exits with.</summary>
    public const int Unusable = -32602;

    /// <summary>What text that is not JSON exits with.</summary>
    public const int Unparseable = -32700;

    /// <summary>
    /// Every tool, in the order a listing shows them: the format, then the vocabulary, then the one
    /// that reads a file back before it exists.
    /// </summary>
    public static IReadOnlyList<Offered> Tools { get; } = new ReadOnlyCollection<Offered>(
    [
        new(
            "winwright_format",
            "The scenario format: every field of a file, a case, a step and a fixture, whether it is "
                + "required, and the closed list of what it accepts. Read this instead of opening a "
                + "case file to work out what a key is called.",
            Nothing(),
            _ => new Answer(string.Join('\n', ScenarioSchema.Render()))),

        new(
            "winwright_vocabulary",
            "What a step may do and what it may read back: every act, what each one needs said "
                + "beside it, and whether the engine may repeat it.",
            Nothing(),
            _ => new Answer(Vocabulary())),

        new(
            "winwright_check",
            "Read a scenario file back before it is written. Send the file as this tool's arguments "
                + "and it answers either the first thing wrong with it, addressed as a path into "
                + "the file, or what the loader read — which is what a run of it would do. Name a "
                + "'project' beside it and it also answers what the door of a run would refuse; "
                + "without one it says so rather than implying the file would run.",
            CheckSchema(),
            Checking),

        new(
            "winwright_run",
            "Run the cases a selection asks for and answer the verdict: the sentence, a line per case "
                + "that ran and per case it left alone, the exit code, and what outlived the run. A "
                + "desk that cannot observe answers a hole rather than a red.",
            Running.Schema(),
            arguments => Running.Over(arguments, Winwright.Windowing.Desk.Read())),
    ]);

    /// <summary>The version of the engine this server is calling.</summary>
    public static string Version => Engine.Running().Version ?? "unpinnable";

    /// <summary>
    /// Answer one message.
    /// </summary>
    /// <param name="request">The message, already parsed.</param>
    /// <returns>
    /// What to write back, or null where there is nothing to write — a notification carries no id,
    /// and JSON-RPC says a reply to one is an error rather than a courtesy.
    /// </returns>
    public static JsonObject? To(JsonObject request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var method = request["method"]?.GetValue<string>();
        var id = request["id"];
        if (id is null)
            return null;

        // Deep-cloned because a node still parented to the request cannot be handed to a reply, and
        // the id is the one field of the request a reply has to carry back verbatim.
        var answering = id.DeepClone();
        return method switch
        {
            "initialize" => Result(answering, Initialized()),
            "tools/list" => Result(answering, Listed()),
            "tools/call" => Called(answering, request["params"] as JsonObject),
            "ping" => Result(answering, new JsonObject()),
            _ => Wrong(answering, NoSuchMethod, $"there is no '{method}'; there is initialize, tools/list, tools/call, ping"),
        };
    }

    /// <summary>A reply carrying a result.</summary>
    public static JsonObject Result(JsonNode? id, JsonNode result) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id,
        ["result"] = result,
    };

    /// <summary>A reply carrying an error, which is not the same as a tool that refused.</summary>
    public static JsonObject Wrong(JsonNode? id, int code, string message) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id,
        ["error"] = new JsonObject { ["code"] = code, ["message"] = message },
    };

    /// <summary>
    /// What <c>winwright_check</c> takes: the file, and beside it the project to hold it against.
    /// WW360.
    /// <para>
    /// The format's own schema with one key added, rather than a hand-written copy of it. The file
    /// half has to stay exactly what the loader reads — that is what
    /// <c>ScenarioSchema.AsJsonSchema</c> is for, and a second spelling here is the one that would
    /// go on describing last month's format.
    /// </para>
    /// <para>
    /// Beside the file and not inside it, because it is not a field of the file: no scenario carries
    /// a project, and the loader refuses the key. So it is stripped before the file is read, and
    /// what a caller sends is the file plus one argument about where to judge it.
    /// </para>
    /// <para>
    /// Optional, and that is the decision WW360 turned on. Required, it would refuse every check of
    /// a file whose project is not written yet — which is the state a scenario is usually first
    /// checked in, so the tool would be closed exactly when it is most wanted. Optional costs
    /// something real instead: the tool answers two different questions, so the answer has to say
    /// which one it answered, and it does.
    /// </para>
    /// </summary>
    private static JsonObject CheckSchema()
    {
        var schema = ScenarioSchema.AsJsonSchema();
        if (schema["properties"] is JsonObject properties)
        {
            properties["project"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = $"optional: the {ProjectDeclaration.FileName} to hold these cases "
                    + "against, or a directory to find it from. Given one, the answer also covers "
                    + "what the door of a run would refuse; without one it checks the file alone "
                    + "and says so.",
            };
        }

        return schema;
    }

    /// <summary>An input schema for a tool that takes nothing, spelled so it refuses anything.</summary>
    private static JsonObject Nothing() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject(),
        ["additionalProperties"] = false,
    };

    private static JsonObject Initialized() => new()
    {
        ["protocolVersion"] = Protocol,
        ["capabilities"] = new JsonObject { ["tools"] = new JsonObject() },
        ["serverInfo"] = new JsonObject { ["name"] = Named, ["version"] = Version },
    };

    private static JsonObject Listed()
    {
        var listed = new JsonArray();
        foreach (var tool in Tools)
        {
            listed.Add(new JsonObject
            {
                ["name"] = tool.Name,
                ["description"] = tool.Summary,
                ["inputSchema"] = tool.Schema.DeepClone(),
            });
        }

        return new JsonObject { ["tools"] = listed };
    }

    private static JsonObject Called(JsonNode? id, JsonObject? parameters)
    {
        var named = parameters?["name"]?.GetValue<string>();
        var tool = Tools.FirstOrDefault(one => string.Equals(one.Name, named, StringComparison.Ordinal));
        if (tool is null)
        {
            return Wrong(
                id,
                Unusable,
                $"there is no tool called '{named}'; there is {string.Join(", ", Tools.Select(one => one.Name))}");
        }

        var arguments = parameters?["arguments"] as JsonObject ?? new JsonObject();
        var answer = tool.Answering(arguments);
        return Result(id, new JsonObject
        {
            ["content"] = new JsonArray
            {
                new JsonObject { ["type"] = "text", ["text"] = answer.Text },
            },
            ["isError"] = answer.Refused,
        });
    }

    /// <summary>
    /// Read the file the caller sent, and answer either the refusal or what a run would do.
    /// <para>
    /// The refusal is the loader's own, addressed as the loader addresses it —
    /// <c>cases[2].steps[1].act</c> — because a second sentence written here is the one that goes on
    /// saying the old thing after the rules move.
    /// </para>
    /// </summary>
    private static Answer Checking(JsonObject arguments)
    {
        // The project is this tool's argument and never the file's. Stripped rather than tolerated,
        // because the loader refuses a key it does not know and the format has no such key — so the
        // thing handed on has to be the file the caller would actually write.
        var where = arguments["project"]?.GetValue<string>()?.Trim() is { Length: > 0 } said ? said : null;
        var file = arguments.DeepClone().AsObject();
        file.Remove("project");

        try
        {
            var read = ScenarioFile.Read("what you sent", file.ToJsonString());
            var lines = new List<string> { $"{read.Count} case{(read.Count == 1 ? "" : "s")}, and the loader accepts them:" };
            lines.AddRange(read.Select(one =>
                $"  {one.Name} — {one.Steps.Count} step{(one.Steps.Count == 1 ? "" : "s")}, "
                    + $"against {Against(one.Fixture)}"));

            // Read once and asked once. Loading it a second time to count what the first found would
            // be two readings of a file that can change between them.
            var project = where is null ? null : Loaded(where);
            var gaps = project is null
                ? Array.Empty<Suite.UndeclaredNeed>()
                : Suite.Undeclared(read, project);

            lines.Add("");
            lines.AddRange(Held(project, gaps));

            return new Answer(string.Join('\n', lines), Refused: gaps.Count > 0);
        }
        catch (ScenarioRefusedException refused)
        {
            return new Answer($"{refused.Subject}: {refused.Because}", Refused: true);
        }
        catch (DeclarationMissingException missing)
        {
            return new Answer(missing.Message, Refused: true);
        }
    }

    /// <summary>
    /// What the file was held against, and what that leaves unanswered. WW360.
    /// <para>
    /// The whole cost of making the project optional is here. A tool that answers two questions has
    /// to say which one it answered, or the shorter answer reads as the longer one — and "the loader
    /// accepts them" reading as "this would run" is the second telling that costs a run, which is
    /// the thing this task was opened about.
    /// </para>
    /// </summary>
    /// <param name="project">The project the caller named, or null where it named none.</param>
    /// <param name="gaps">What holding the file against it found.</param>
    private static IReadOnlyList<string> Held(
        ProjectDeclaration? project, IReadOnlyList<Suite.UndeclaredNeed> gaps)
    {
        if (project is null)
        {
            return
            [
                "No project was named, so this is the file alone. A step whose act needs a "
                    + $"declaration — {Needing()} — would still be refused at the door of a run by a "
                    + $"{ProjectDeclaration.FileName} that does not carry it. Send 'project' to have "
                    + "that answered here.",
            ];
        }

        if (gaps.Count == 0)
            return [$"Held against {project.Path}, which declares everything these steps need."];

        var lines = new List<string>
        {
            $"Held against {project.Path}, and a run would refuse "
                + $"{gaps.Count} step{(gaps.Count == 1 ? "" : "s")} at its door:",
        };

        // Every one of them and not the first, which is the difference between a door and a report:
        // an author told about one missing key at a time pays a round trip per key.
        lines.AddRange(gaps.Select(one => $"  {one.Case}: {one.Because(project.Path)}"));
        return lines;
    }

    /// <summary>
    /// The project, found from a directory or read from a file — the same either-way the run tool
    /// takes, because an author naming one for a check and one for a run should not have to spell
    /// them differently.
    /// </summary>
    /// <param name="where">What the caller named.</param>
    private static ProjectDeclaration Loaded(string where) =>
        Directory.Exists(where) ? ProjectDeclaration.Find(where) : ProjectDeclaration.Load(where);

    /// <summary>
    /// The acts that need a declaration, read off the vocabulary. Named rather than listed here, so
    /// a second one added to <c>ActVerb.All</c> reaches this sentence without anybody remembering.
    /// </summary>
    private static string Needing()
    {
        var needing = ActVerb.All
            .Where(one => one.Needs.Length > 0)
            .Select(one => $"'{one.Name}' needs '{one.Needs}'")
            .ToList();

        return needing.Count == 0 ? "none do today" : string.Join(", ", needing);
    }

    /// <summary>What a case is launched against, named the way a report would name it.</summary>
    private static string Against(FixtureDeclaration fixture) =>
        ReferenceEquals(fixture, FixtureDeclaration.Plain)
            ? "the application as it comes"
            : $"'{fixture.Name}'";

    private static string Vocabulary()
    {
        var lines = new List<string> { "An act:" };
        lines.AddRange(ActVerb.All.Select(verb =>
            $"  {verb.Name} — {Needs(verb)}, and {(verb.Repeatable ? "the engine may repeat it" : "the engine attempts it once")}"
                + (verb.Reads ? ", and it reads without acting" : "")

                // WW225. Both read off the verb rather than described here. The closed list is what
                // makes a wrong reason a refusal at the point of insertion, and whether an act
                // synthesises input is what decides it can come back not attempted at all.
                + (verb.Synthesises ? ", and it synthesises input so it needs the foreground" : "")
                + (verb.Accepts.Count > 0 ? $" — one of: {string.Join(", ", verb.Accepts)}" : "")));

        lines.Add("A reading:");
        lines.AddRange(ReadBack.All.Select(one => $"  {one.Name}"));
        return string.Join('\n', lines);
    }

    private static string Needs(ActVerb verb) => verb.Wants switch
    {
        Takes.Text => "needs text in 'with'",
        Takes.Number => "needs a number in 'with'",
        Takes.Position => "needs a position in 'with', counted from 0",
        _ => "needs nothing said beside it",
    };

    /// <summary>Parse one line off the pipe, or say what is wrong with it.</summary>
    /// <param name="line">What was read.</param>
    /// <param name="complaint">Why it is not a message, where it is not. Empty where it is.</param>
    public static JsonObject? Parsed(string line, out string complaint)
    {
        complaint = "";
        try
        {
            if (JsonNode.Parse(line) is JsonObject read)
                return read;

            complaint = "a JSON-RPC message is an object";
            return null;
        }
        catch (JsonException unparseable)
        {
            complaint = $"it is not JSON — {unparseable.Message}";
            return null;
        }
    }
}

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
                + "the file, or what the loader read — which is what a run of it would do.",
            ScenarioSchema.AsJsonSchema(),
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
        try
        {
            var read = ScenarioFile.Read("what you sent", arguments.ToJsonString());
            var lines = new List<string> { $"{read.Count} case{(read.Count == 1 ? "" : "s")}, and the loader accepts them:" };
            lines.AddRange(read.Select(one =>
                $"  {one.Name} — {one.Steps.Count} step{(one.Steps.Count == 1 ? "" : "s")}, "
                    + $"against {Against(one.Fixture)}"));

            return new Answer(string.Join('\n', lines));
        }
        catch (ScenarioRefusedException refused)
        {
            return new Answer($"{refused.Subject}: {refused.Because}", Refused: true);
        }
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

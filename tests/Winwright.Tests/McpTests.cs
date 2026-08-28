using System.Text.Json;
using System.Text.Json.Nodes;

using Winwright.Mcp;
using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW66. The format used to be readable only as prose, so the way a key arrived was that somebody
/// typed it from memory. A guess costs a refusal and a retry at best; at worst it is
/// <c>"expects"</c> beside <c>"expect"</c>, which a shrugging schema loads and a run reads green.
/// <para>
/// What is checked here is that the schema a tool publishes and the format the loader enforces are
/// one thing. Two lists would agree on the day they were written and diverge on the first field
/// added to one of them, and the divergence is invisible: the tool goes on accepting what the run
/// refuses, which is the shape of a constraint that is really a suggestion.
/// </para>
/// </summary>
public sealed class McpTests
{
    [Fact]
    public void The_input_schema_is_the_format_the_loader_reads_and_not_a_second_copy_of_it()
    {
        // Field for field, kind for kind, off the one list. A field added to the schema and not to
        // this walk is still checked, because the walk is the schema.
        Shaped(ScenarioSchema.AsJsonSchema(), ScenarioSchema.File);
    }

    [Fact]
    public void A_key_the_loader_would_refuse_is_a_key_the_schema_cannot_express()
    {
        // The whole difference between a format and a convention, carried into the tool: 'expects'
        // beside 'expect' has to be unwritable rather than explained after the fact.
        var schema = ScenarioSchema.AsJsonSchema();
        var step = schema["properties"]!["cases"]!["items"]!["properties"]!["steps"]!["items"]!;

        Assert.False(schema["additionalProperties"]!.GetValue<bool>());
        Assert.False(step["additionalProperties"]!.GetValue<bool>());
        Assert.Null(step["properties"]!["expects"]);
        Assert.NotNull(step["properties"]!["expect"]);
    }

    [Fact]
    public void What_a_step_may_do_arrives_as_a_closed_list_off_the_vocabulary()
    {
        var step = ScenarioSchema.AsJsonSchema()["properties"]!["cases"]!["items"]!["properties"]!["steps"]!["items"]!;

        Assert.Equal(
            ActVerb.All.Select(verb => verb.Name),
            step["properties"]!["act"]!["enum"]!.AsArray().Select(one => one!.GetValue<string>()));
        Assert.Equal(
            ReadBack.All.Select(one => one.Name),
            step["properties"]!["reads"]!["enum"]!.AsArray().Select(one => one!.GetValue<string>()));
    }

    [Fact]
    public void The_schema_and_the_reader_cannot_disagree_about_what_a_field_holds()
    {
        // The negative control for the whole arrangement, provoked rather than waited for: reading a
        // field as the wrong kind is a harness error on the spot, so a schema that says one thing
        // while the loader does another cannot survive a single load.
        Assert.Equal("tags", ScenarioSchema.Of(ScenarioSchema.Case, "tags", Taking.Words).Name);

        var wrong = Assert.Throws<InvalidOperationException>(
            () => ScenarioSchema.Of(ScenarioSchema.Case, "tags", Taking.Text));
        Assert.Contains("holds Words and it is being read as Text", wrong.Message, StringComparison.Ordinal);

        var absent = Assert.Throws<InvalidOperationException>(
            () => ScenarioSchema.Of(ScenarioSchema.Case, "tagz", Taking.Words));
        Assert.Contains("the schema has no 'tagz'", absent.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_tool_the_listing_offers_carries_an_input_schema_and_says_what_it_is_for()
    {
        var listed = Answered(Served.To(Message(1, "tools/list"))!)["tools"]!.AsArray();

        Assert.Equal(Served.Tools.Count, listed.Count);
        foreach (var tool in listed)
        {
            Assert.StartsWith("winwright_", tool!["name"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.NotEmpty(tool["description"]!.GetValue<string>());
            Assert.Equal("object", tool["inputSchema"]!["type"]!.GetValue<string>());
        }

        // The one tool that exists to make a guess unnecessary carries the format itself.
        var checking = listed.Single(one => one!["name"]!.GetValue<string>() == "winwright_check");
        Assert.NotNull(checking!["inputSchema"]!["properties"]!["cases"]);
    }

    [Fact]
    public void The_format_and_the_vocabulary_are_answers_rather_than_prose_somebody_loads()
    {
        var format = Said(Served.To(Message(2, "tools/call", Calling("winwright_format")))!);
        var vocabulary = Said(Served.To(Message(3, "tools/call", Calling("winwright_vocabulary")))!);

        // WW258. The same rendering ScenarioFileTests pins, asserted again here because this is the
        // answer a tool actually receives: 'locator' now says which group it belongs to, since a step
        // needs it or a 'tray' and neither "required" nor "optional" is true of either.
        Assert.Contains(
            $"locator (one of the {Winwright.Scenarios.ScenarioSchema.Subject}): what to act on",
            format,
            StringComparison.Ordinal);

        Assert.Contains($"tray (one of the {Winwright.Scenarios.ScenarioSchema.Subject}):", format, StringComparison.Ordinal);
        Assert.Contains("one of: true, false", format, StringComparison.Ordinal);

        foreach (var verb in ActVerb.All)
            Assert.Contains($"  {verb.Name} — ", vocabulary, StringComparison.Ordinal);

        Assert.Contains("set value — needs text in 'with'", vocabulary, StringComparison.Ordinal);
        Assert.Contains("toggle — needs nothing said beside it, and the engine attempts it once", vocabulary, StringComparison.Ordinal);
        Assert.Contains("read — needs nothing said beside it, and the engine may repeat it, and it reads without acting", vocabulary, StringComparison.Ordinal);
    }

    [Fact]
    public void Checking_a_file_answers_the_refusal_the_loader_would_give_at_the_address_it_gives_it()
    {
        // The saving this is about is the analysis: the author hears about the misspelling before
        // the file exists, addressed as a path into it rather than as a line.
        var reply = Served.To(Message(4, "tools/call", Calling("winwright_check", """
            {
              "cases": [
                { "name": "a", "steps": [ { "locator": "Button#save", "act": "invoke", "expects": "Saved" } ] }
              ]
            }
            """)))!;

        Assert.True(Answered(reply)["isError"]!.GetValue<bool>());
        Assert.Contains("cases[0].steps[0].expects", Said(reply), StringComparison.Ordinal);
        Assert.Contains("there is no such field", Said(reply), StringComparison.Ordinal);
    }

    [Fact]
    public void Checking_a_file_that_loads_answers_what_a_run_of_it_would_do()
    {
        var reply = Served.To(Message(5, "tools/call", Calling("winwright_check", """
            {
              "fixtures": [ { "name": "in Portuguese", "environment": "pt-BR", "flag": "--culture" } ],
              "cases": [
                {
                  "name": "the field takes a name",
                  "fixture": "in Portuguese",
                  "steps": [
                    { "locator": "Edit#profileName", "act": "set value", "with": "Ada", "expect": "Ada", "reads": "value" },
                    { "locator": "Button#save", "act": "invoke", "named": "save the profile" }
                  ]
                }
              ]
            }
            """)))!;

        Assert.False(Answered(reply)["isError"]!.GetValue<bool>());
        Assert.Contains("1 case", Said(reply), StringComparison.Ordinal);
        Assert.Contains("the field takes a name — 2 steps, against 'in Portuguese'", Said(reply), StringComparison.Ordinal);
    }

    [Fact]
    public void A_case_naming_no_fixture_is_reported_as_the_application_as_it_comes()
    {
        var reply = Served.To(Message(6, "tools/call", Calling("winwright_check", """
            { "cases": [ { "name": "a", "steps": [ { "locator": "Text#status", "act": "read", "expect": "Saved", "reads": "text" } ] } ] }
            """)))!;

        Assert.Contains("against the application as it comes", Said(reply), StringComparison.Ordinal);
    }

    [Fact]
    public void A_call_naming_no_tool_names_the_tools_there_are_rather_than_going_quiet()
    {
        var reply = Served.To(Message(7, "tools/call", Calling("winwright_screenshot")))!;

        Assert.Equal(Served.Unusable, reply["error"]!["code"]!.GetValue<int>());
        foreach (var tool in Served.Tools)
            Assert.Contains(tool.Name, reply["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_method_nobody_has_is_an_error_and_never_an_empty_result()
    {
        var reply = Served.To(Message(8, "tools/eval"))!;

        Assert.Equal(Served.NoSuchMethod, reply["error"]!["code"]!.GetValue<int>());
        Assert.Contains("there is no 'tools/eval'", reply["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    [Fact]
    public void The_handshake_names_the_protocol_the_server_and_the_engine_it_is_calling()
    {
        var result = Answered(Served.To(Message(9, "initialize"))!);

        Assert.Equal(Served.Protocol, result["protocolVersion"]!.GetValue<string>());
        Assert.Equal(Served.Named, result["serverInfo"]!["name"]!.GetValue<string>());
        Assert.NotNull(result["capabilities"]!["tools"]);

        // The version is read off the assembly actually loaded, so the tools cannot report one
        // version while the engine answering them is another.
        Assert.Equal(Winwright.Projects.Engine.Running().Version, result["serverInfo"]!["version"]!.GetValue<string>());
    }

    [Fact]
    public void A_notification_gets_no_reply_and_text_that_is_not_a_message_gets_an_error()
    {
        // A notification carries no id, and JSON-RPC says answering one is an error rather than a
        // courtesy. Text that is not a message is answered and the pipe stays open: a server that
        // exits on the first bad line takes the session's tools with it.
        Assert.Null(Served.To(new JsonObject { ["jsonrpc"] = "2.0", ["method"] = "notifications/initialized" }));

        Assert.Null(Served.Parsed("{ not json", out var unparseable));
        Assert.Contains("it is not JSON", unparseable, StringComparison.Ordinal);

        Assert.Null(Served.Parsed("[1, 2]", out var notAnObject));
        Assert.Contains("a JSON-RPC message is an object", notAnObject, StringComparison.Ordinal);
    }

    [Fact]
    public void The_plugin_wires_the_server_through_the_launcher_that_can_say_it_is_not_built()
    {
        // WW221. The wiring used to be `dotnet exec` on a path under bin\Release, so a fresh clone
        // showed the server as failed with a .NET assembly error for its whole explanation. The
        // launcher is what turns that into a sentence naming the build.
        var repository = Repository();
        using var wiring = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(repository, ".claude-plugin", "mcp.json")));

        var server = wiring.RootElement.GetProperty("mcpServers").GetProperty("winwright");
        var command = server.GetProperty("command").GetString()!;

        Assert.StartsWith("${CLAUDE_PLUGIN_ROOT}/", command, StringComparison.Ordinal);
        Assert.EndsWith("/tools/winwright-mcp.cmd", command, StringComparison.Ordinal);

        var launcher = Path.Combine(repository, "tools", "winwright-mcp.cmd");
        Assert.True(File.Exists(launcher), $"the wiring names {command} and there is no {launcher}");

        // The launcher and the build cannot drift on a renamed assembly or a moved framework: both
        // halves are read off this tree rather than retyped here.
        var said = File.ReadAllText(launcher);
        Assert.Contains("Winwright.Mcp.dll", said, StringComparison.Ordinal);
        Assert.Contains(Framework(repository), said, StringComparison.Ordinal);

        // And the manifest points at the wiring, or it is a file nothing reads.
        using var manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(repository, ".claude-plugin", "plugin.json")));
        Assert.Equal("./.claude-plugin/mcp.json", manifest.RootElement.GetProperty("mcpServers").GetString());
    }

    /// <summary>One shape and every shape under it, against the fields it was generated from.</summary>
    private static void Shaped(JsonNode shape, IReadOnlyList<Field> fields)
    {
        Assert.Equal("object", shape["type"]!.GetValue<string>());
        Assert.False(shape["additionalProperties"]!.GetValue<bool>());

        var properties = shape["properties"]!.AsObject();
        Assert.Equal(fields.Select(field => field.Name), properties.Select(pair => pair.Key));

        var required = shape["required"]?.AsArray().Select(one => one!.GetValue<string>()) ?? [];
        Assert.Equal(fields.Where(field => field.Required).Select(field => field.Name), required);

        foreach (var field in fields)
        {
            var property = properties[field.Name]!;
            Assert.Equal(field.Means, property["description"]!.GetValue<string>());

            switch (field.Holds)
            {
                case Taking.Text:
                    Assert.Equal("string", property["type"]!.GetValue<string>());
                    break;
                case Taking.Truth:
                    Assert.Equal("boolean", property["type"]!.GetValue<string>());
                    break;
                case Taking.Words:
                    Assert.Equal("array", property["type"]!.GetValue<string>());
                    Assert.Equal("string", property["items"]!["type"]!.GetValue<string>());
                    break;
                case Taking.Pairs:
                    Assert.Equal("object", property["type"]!.GetValue<string>());
                    Assert.Equal("string", property["additionalProperties"]!["type"]!.GetValue<string>());
                    break;
                default:
                    Assert.Equal("array", property["type"]!.GetValue<string>());
                    Shaped(property["items"]!, Under(field.Holds));
                    break;
            }

            Assert.Equal(
                field.OneOf,
                property["enum"]?.AsArray().Select(one => one!.GetValue<string>()).ToList() ?? []);
        }
    }

    private static IReadOnlyList<Field> Under(Taking holds) => holds switch
    {
        Taking.Cases => ScenarioSchema.Case,
        Taking.Steps => ScenarioSchema.Step,
        Taking.Fixtures => ScenarioSchema.Fixture,
        _ => throw new InvalidOperationException($"{holds} is not a shape"),
    };

    private static JsonObject Message(int id, string method, JsonObject? parameters = null)
    {
        var message = new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["method"] = method };
        if (parameters is not null)
            message["params"] = parameters;

        return message;
    }

    private static JsonObject Calling(string name, string? arguments = null)
    {
        var parameters = new JsonObject { ["name"] = name };
        if (arguments is not null)
            parameters["arguments"] = JsonNode.Parse(arguments);

        return parameters;
    }

    private static JsonObject Answered(JsonObject reply) => reply["result"]!.AsObject();

    private static string Said(JsonObject reply) =>
        Answered(reply)["content"]!.AsArray()[0]!["text"]!.GetValue<string>();

    /// <summary>The framework every project in this tree targets, read off the file that says so.</summary>
    private static string Framework(string repository)
    {
        var props = File.ReadAllText(Path.Combine(repository, "Directory.Build.props"));
        var opening = props.IndexOf("<TargetFramework>", StringComparison.Ordinal) + "<TargetFramework>".Length;
        var closing = props.IndexOf("</TargetFramework>", opening, StringComparison.Ordinal);

        Assert.True(closing > opening, "Directory.Build.props declares no TargetFramework");
        return props[opening..closing].Trim();
    }

    /// <summary>The repository root, found by walking up to the file that declares the version.</summary>
    private static string Repository()
    {
        var walking = new DirectoryInfo(AppContext.BaseDirectory);
        while (walking is not null && !File.Exists(Path.Combine(walking.FullName, "Directory.Build.props")))
            walking = walking.Parent;

        Assert.NotNull(walking);
        return walking.FullName;
    }
}

using System.Text.Json.Nodes;

using Winwright.Mcp;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW222. The tools answered whether a file would load and stopped there, so the question a session
/// actually has — <em>did it pass</em> — went back to a shell: build, <c>dotnet test</c>, read a trx.
/// That is the script-shaped path WW67 exists to deny, reached by the door WW67 left open.
/// <para>
/// The reading this has to get right is the third one. A desk with no interactive session, no display
/// or no automation observed nothing, so it has no verdict — not a red, and emphatically not a green.
/// None of the six conditions is forceable in process, which is what <see cref="Desk.Blocked"/> is
/// for, and it is the only way this path is reachable on a machine anybody has.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class RunningTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-running-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private static Desk Locked() => Desk.Blocked(
        Precondition.Absent(Desk.InputDesktop, "the input desktop would not open, which is a locked workstation"));

    [Fact]
    public void A_desk_that_cannot_observe_answers_a_hole_and_says_it_is_not_a_failure()
    {
        // The whole reason this is filed apart from WW66: a schema-shaped tool cannot be wrong about
        // the desk, and a verdict-shaped one can be wrong about it in the one direction that matters.
        var answered = Running.Over(Arguments(), Locked());

        Assert.Contains("nothing ran", answered.Text, StringComparison.Ordinal);
        Assert.Contains(Desk.InputDesktop, answered.Text, StringComparison.Ordinal);
        Assert.Contains("locked workstation", answered.Text, StringComparison.Ordinal);
        Assert.Contains("a hole and not a failure", answered.Text, StringComparison.Ordinal);

        // No verdict at all, rather than one of the two that would be read as an outcome.
        Assert.DoesNotContain("exit code", answered.Text, StringComparison.Ordinal);
        Assert.True(answered.Refused);
    }

    [Fact]
    public void The_desk_is_read_before_anything_is_launched_or_even_loaded()
    {
        // Arguments naming nothing that exists. A blocked desk still answers about the desk, because
        // a run that could observe nothing never got as far as the declaration being wrong.
        var nowhere = new JsonObject { ["project"] = "D:/nothing/at/all", ["cases"] = "D:/nor/here" };

        Assert.Contains(Desk.InputDesktop, Running.Over(nowhere, Locked()).Text, StringComparison.Ordinal);
    }

    [Fact]
    public void A_declaration_that_is_not_there_is_refused_and_names_itself()
    {
        var desk = Desk.Read();
        if (!desk.CanObserve)
            return;

        var answered = Running.Over(
            new JsonObject { ["project"] = Path.Combine(root, "winwright.json"), ["cases"] = root },
            desk);

        Assert.True(answered.Refused);
        Assert.Contains("winwright.json", answered.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void A_directory_holding_no_cases_is_refused_rather_than_passing_over_none()
    {
        // The refusal Suite already owns, carried through: a run of no cases has no failure in it and
        // reads as a pass, which is the shape this whole project started over.
        var desk = Desk.Read();
        if (!desk.CanObserve)
            return;

        var project = Path.Combine(root, Winwright.Projects.ProjectDeclaration.FileName);
        File.WriteAllText(
            project,
            $$"""{ "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}} }""");

        var empty = Directory.CreateDirectory(Path.Combine(root, "none")).FullName;
        var answered = Running.Over(
            new JsonObject { ["project"] = project, ["cases"] = empty }, desk);

        Assert.True(answered.Refused);
        Assert.Contains("nothing to run", answered.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void The_arguments_a_run_cannot_do_without_are_refused_by_name()
    {
        var desk = Desk.Read();
        if (!desk.CanObserve)
            return;

        Assert.Contains(
            "no winwright.json was named",
            Running.Over(new JsonObject { ["cases"] = root }, desk).Text,
            StringComparison.Ordinal);

        Assert.Contains(
            "no directory of cases was named",
            Running.Over(new JsonObject { ["project"] = root }, desk).Text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_case_written_as_a_file_is_run_and_the_verdict_comes_back_as_the_answer()
    {
        // WW222's whole claim, end to end and against the real fixture: a data file on disk, a
        // launch, a run, and the sentence — without a build, a `dotnet test` or a trx anywhere in it.
        var desk = Desk.Read();
        if (!desk.CanObserve)
            return;

        var project = Declaring();
        File.WriteAllText(
            Path.Combine(root, $"profiles{Winwright.Scenarios.ScenarioFile.Extension}"),
            """
            {
              "fixtures": [ { "name": "the names pane", "arguments": ["--names"] } ],
              "cases": [
                {
                  "name": "the profile box takes a name",
                  "catches": "an editable control the pane draws and no case ever writes to",
                  "fixture": "the names pane",
                  "steps": [
                    { "locator": "Edit#profileBox", "act": "set value", "with": "beta", "expect": "beta", "reads": "value" }
                  ]
                }
              ]
            }
            """);

        var answered = Running.Over(new JsonObject { ["project"] = project, ["cases"] = root }, desk);

        Assert.False(answered.Refused, answered.Text);
        Assert.Contains("the profile box takes a name", answered.Text, StringComparison.Ordinal);
        Assert.Contains("exit code 0", answered.Text, StringComparison.Ordinal);

        // And the run says what it left on the desk, beside the verdict rather than inside it.
        Assert.Contains("outlived the run", answered.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void A_selector_that_matches_no_case_is_refused_rather_than_passing_over_nothing()
    {
        var desk = Desk.Read();
        if (!desk.CanObserve)
            return;

        var project = Declaring();
        File.WriteAllText(
            Path.Combine(root, $"profiles{Winwright.Scenarios.ScenarioFile.Extension}"),
            """
            {
              "cases": [
                {
                  "name": "the pane is selected",
                  "steps": [ { "locator": "TabItem#namesPane", "act": "select", "expect": "selected", "reads": "selected" } ]
                }
              ]
            }
            """);

        var answered = Running.Over(
            new JsonObject { ["project"] = project, ["cases"] = root, ["case"] = "a case nobody wrote" },
            desk);

        // A run of no cases has no failure in it and reads as a pass, so the typo has to stop it.
        Assert.True(answered.Refused, answered.Text);
        Assert.DoesNotContain("exit code 0", answered.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void What_the_tool_takes_is_a_selection_and_where_to_select_from()
    {
        var schema = Running.Schema();
        var properties = schema["properties"]!.AsObject();

        Assert.Equal(
            ["project", "cases", "case", "tag", "sharing"],
            properties.Select(pair => pair.Key));
        Assert.Equal(
            ["project", "cases"],
            schema["required"]!.AsArray().Select(one => one!.GetValue<string>()));
        Assert.False(schema["additionalProperties"]!.GetValue<bool>());

        // The tool is offered under its own name and carries this schema, or the listing and the
        // implementation are two things that agree today.
        var offered = Served.Tools.Single(one => one.Name == "winwright_run");
        Assert.Equal(schema.ToJsonString(), offered.Schema.ToJsonString());
    }

    /// <summary>A declaration whose executable is the built fixture, which is the application under test.</summary>
    private string Declaring()
    {
        var path = Path.Combine(root, Winwright.Projects.ProjectDeclaration.FileName);
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "resolve": 4000, "act": 4000, "poll": 25, "launch": 20000 }
            }
            """);

        return path;
    }

    private static JsonObject Arguments() => new()
    {
        ["project"] = Path.Combine(Path.GetTempPath(), "winwright.json"),
        ["cases"] = Path.GetTempPath(),
    };
}

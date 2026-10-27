using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW582. Commands a fixture runs once its files are staged and before its application launches.
/// <para>
/// Found by Modelado (MOD65): the claim was that a job its CLI sent shows in the window's About, and
/// no fixture could run the CLI in its staged folder with its variables, so the case staged a
/// hand-written ledger and proved only that About reads a file. Here the command is cmd itself,
/// writing into the staged directory with a variable the fixture sets and with what it was given on
/// its standard input, and a file step reads what it wrote.
/// </para>
/// <para>
/// The control is a command that exits otherwise than it said: the case is a hole naming it and the
/// code, and nothing is launched.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class BeforeCommandTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-before-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_fixture_reads_its_commands_and_refuses_one_that_runs_nothing()
    {
        var fixture = Assert.Single(Fixtures("""
            { "name": "a ledger", "before": [
              { "run": "tools/cli.exe", "arguments": ["send", "{files}/m.glb"], "input": "key", "expectExit": 2 },
              { "run": "tools/cli.exe" } ] }
            """));

        Assert.Equal(2, fixture.Before.Count);
        Assert.Equal(("tools/cli.exe", "key", 2), (fixture.Before[0].Run, fixture.Before[0].Input, fixture.Before[0].ExpectExit));
        Assert.Equal(["send", "{files}/m.glb"], fixture.Before[0].Arguments);
        Assert.Equal(0, fixture.Before[1].ExpectExit);
        Assert.True(fixture.NeedsStaging);
        Assert.Contains("running 2 command(s) first", fixture.Sentence(), StringComparison.Ordinal);

        var nothing = Assert.Throws<ScenarioRefusedException>(() => Fixtures("""{ "name": "x", "before": [ { "arguments": ["a"] } ] }"""));
        Assert.EndsWith(".before[0].run", nothing.Subject, StringComparison.Ordinal);
        Assert.Contains("a command runs something", nothing.Because, StringComparison.Ordinal);
        Assert.Contains(
            "no such field",
            Assert.Throws<ScenarioRefusedException>(() => Fixtures("""{ "name": "x", "before": [ { "run": "a.exe", "wait": 3 } ] }""")).Because,
            StringComparison.Ordinal);
        Assert.Contains(
            "whole number",
            Assert.Throws<ScenarioRefusedException>(() => Fixtures("""{ "name": "x", "before": [ { "run": "a.exe", "expectExit": "0" } ] }""")).Because,
            StringComparison.Ordinal);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_file_staged_for_a_command_is_one_somebody_reads()
    {
        // WW509 refuses staged files nothing reads; a command naming the staged directory reads them.
        var fixture = Assert.Single(Fixtures("""
            { "name": "a photo", "files": ["data/photo.png"], "before": [ { "run": "cli.exe", "arguments": ["{files}/photo.png"] } ] }
            """));

        Assert.Single(fixture.Files);
    }

    [Fact]
    public void What_a_command_writes_before_the_launch_is_there_for_the_case()
    {
        var verdict = Run(
            """
            { "run": "C:/Windows/System32/cmd.exe", "arguments": ["/d", "/c", "echo %MARK%>{files}\\made.txt"] },
            { "run": "C:/Windows/System32/cmd.exe", "arguments": ["/d", "/c", "sort > {files}\\in.txt"], "input": "piped" }
            """,
            """
            { "file": "{files}/made.txt", "act": "read", "sameBytesAs": "data/made.txt", "named": "the command wrote with the fixture's variable" },
            { "file": "{files}/in.txt", "act": "read", "sameBytesAs": "data/in.txt", "named": "the command read its input" }
            """);
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_command_that_exits_otherwise_leaves_the_case_a_hole_naming_it()
    {
        var verdict = Run(
            """{ "run": "C:/Windows/System32/cmd.exe", "arguments": ["/d", "/c", "echo failing & exit 3"] }""",
            """{ "file": "{files}/made.txt", "act": "read", "sameBytesAs": "data/made.txt" }""");
        if (verdict is null)
            return;

        var said = Said(verdict);
        Assert.NotEqual(RunOutcome.Passed, verdict.Outcome);
        Assert.Contains("exited 3 where 0 was expected", said, StringComparison.Ordinal);
        Assert.Contains("failing", said, StringComparison.Ordinal);
    }

    private static IReadOnlyList<FixtureDeclaration> Fixtures(string fixture)
    {
        var read = ScenarioFile.Read("before.cases.json", $$"""
            { "fixtures": [ {{fixture}} ],
              "cases": [ { "name": "a case", "fixture": {{System.Text.Json.JsonSerializer.Serialize(System.Text.Json.JsonDocument.Parse(fixture).RootElement.GetProperty("name").GetString())}},
                "steps": [ { "file": "{files}/out.txt", "act": "read", "sameBytesAs": "data/out.txt" } ] } ] }
            """);

        return [read[0].Fixture];
    }

    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>
    /// Launch the fixture app behind <paramref name="before"/>, then run <paramref name="steps"/>; or
    /// null where this desk cannot observe.
    /// </summary>
    private SuiteVerdict? Run(string before, string steps)
    {
        if (!Desk.Read().CanObserve)
            return null;

        Directory.CreateDirectory(Path.Combine(root, "data"));
        File.WriteAllText(Path.Combine(root, "data", "made.txt"), "hello\r\n");
        File.WriteAllText(Path.Combine(root, "data", "in.txt"), "piped\r\n");

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "launch": 30000, "resolve": 4000, "act": 4000, "poll": 50 }
            }
            """);

        var cases = $$"""
            {
              "fixtures": [ { "name": "a window after its CLI", "arguments": ["--status"], "variables": { "MARK": "hello" }, "before": [ {{before}} ] } ],
              "cases": [
                {
                  "name": "what the CLI wrote is there",
                  "catches": "a CLI's output no case could produce before the window launched",
                  "fixture": "a window after its CLI",
                  "steps": [ {{steps}} ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(ScenarioFile.Read("before.cases.json", cases), Selection.All, register, project);
        register.StopAll();
        return verdict;
    }
}

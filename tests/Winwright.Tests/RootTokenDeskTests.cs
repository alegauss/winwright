using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW542, in a real window: a case types <c>{root}</c> into an edit and reads back the checkout's own
/// path, which is the thing quickshell's case had to spell whole for one desk.
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class RootTokenDeskTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-root-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Theory]
    [InlineData("{root}\\cases", false)]
    [InlineData("{root/}/cases", true)]
    public void A_case_types_the_project_root_and_reads_back_this_checkout(string typed, bool slashed)
    {
        if (!Desk.Read().CanObserve)
            return;

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "launch": 30000, "resolve": 4000, "act": 4000, "poll": 25 }
            }
            """);

        var project = ProjectDeclaration.Load(declaration);
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(project.Root));
        var expected = slashed ? "/" + full.Replace('\\', '/') + "/cases" : full + "\\cases";

        var cases = $$"""
            {
              "fixtures": [ { "name": "an edit to type into", "arguments": ["--names", "--show"] } ],
              "cases": [
                {
                  "name": "the root reaches the edit",
                  "catches": "a case that had to spell one desk's checkout path whole to reach a file in it",
                  "fixture": "an edit to type into",
                  "steps": [
                    {
                      "locator": "Edit#profileBox",
                      "act": "set value",
                      "with": {{System.Text.Json.JsonSerializer.Serialize(typed)}},
                      "reads": "value",
                      "expect": {{System.Text.Json.JsonSerializer.Serialize(expected)}}
                    }
                  ]
                }
              ]
            }
            """;

        using var register = ProcessRegister.For(project);
        var verdict = Suite.Launch(ScenarioFile.Read("root.cases.json", cases), Selection.All, register, project);
        register.StopAll();
        if (BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, string.Join(Environment.NewLine, verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))));
    }
}

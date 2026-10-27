using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW575, its first slice. A fixture that launches another executable than the project's.
/// <para>
/// Found by Modelado (MOD31): the update's evidence is a click in the copy an installer put in
/// <c>%LOCALAPPDATA%\Programs</c>, not the build tree's that <c>winwright.json</c> names, and no case
/// could launch that one. Here the project names an executable that is not there and the fixture
/// names the real one, so the window that opens can only be the fixture's.
/// </para>
/// <para>
/// The control is the same case with no <c>application</c>: it launches the project's missing
/// executable and is refused, so a run that ignored the field and passed anyway cannot pass both.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ApplicationFixtureTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-application-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void The_executable_a_fixture_names_is_resolved_the_way_the_project_resolves_its_own()
    {
        var project = Path.GetFullPath(Path.GetTempPath());
        var fixture = FixtureDeclaration.Of("installed", application: @"%LOCALAPPDATA%\Programs\App\App.exe");
        var relative = FixtureDeclaration.Of("built", application: @"bin\App.exe");
        var plain = FixtureDeclaration.Of("as declared");

        Assert.Equal(
            Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA")!, "Programs", "App", "App.exe"),
            fixture.Launches(@"C:\declared\App.exe", project));
        Assert.Equal(Path.Combine(project, "bin", "App.exe"), relative.Launches(@"C:\declared\App.exe", project));
        Assert.Equal(@"C:\declared\App.exe", plain.Launches(@"C:\declared\App.exe", project));

        // And a report says which binary the launch drove, where it was not the project's.
        Assert.Contains(@"launching %LOCALAPPDATA%\Programs\App\App.exe", fixture.Sentence(), StringComparison.Ordinal);
        Assert.DoesNotContain("launching", plain.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_fixture_naming_the_real_executable_launches_it_over_the_projects_missing_one()
    {
        var verdict = Run(application: true);
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void The_same_fixture_without_it_launches_the_projects_and_is_refused()
    {
        if (!Desk.Read().CanObserve)
            return;

        // Refused and not red: an executable that cannot be started is the declaration's fault on
        // every machine, which is the refusal WW567 made of it.
        var refused = Assert.Throws<ScenarioRefusedException>(() => Run(application: false));

        Assert.Contains("not-built.exe' could not be started", refused.Because, StringComparison.Ordinal);
        Assert.Contains("the executable in", refused.Because, StringComparison.Ordinal);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>
    /// Read the fixture's heading under a project whose executable is not there, with or without a
    /// fixture naming the real one; or null where this desk cannot observe.
    /// </summary>
    private SuiteVerdict? Run(bool application)
    {
        if (!Desk.Read().CanObserve)
            return null;

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            """
            {
              "executable": "not-built.exe",
              "timeouts": { "launch": 30000, "resolve": 4000, "act": 4000, "poll": 25 }
            }
            """);

        var naming = application
            ? $"\"application\": {System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}, "
            : "";

        var cases = $$"""
            {
              "fixtures": [ { {{naming}}"name": "the installed copy", "arguments": ["--status"] } ],
              "cases": [
                {
                  "name": "the installed copy opens",
                  "catches": "an installed copy no case could launch, because the project names the build tree's",
                  "fixture": "the installed copy",
                  "steps": [
                    { "locator": "Text#heading", "act": "read", "reads": "name", "expect": "winwright fixture", "named": "the window is the fixture's" }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        SuiteVerdict verdict;
        try
        {
            verdict = Suite.Launch(ScenarioFile.Read("application.cases.json", cases), Selection.All, register, project);
        }
        finally
        {
            register.StopAll();
        }

        return verdict;
    }
}

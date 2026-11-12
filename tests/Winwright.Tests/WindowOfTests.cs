using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW620. A fixture whose launch is a launcher, and whose window is drawn by a process it starts.
/// <para>
/// Found by Modelado (MOD79): its packaged window is reached only through a CLI alias that starts the
/// app as a child, and the wait for a window asked the launched process alone. Here the launcher is
/// <c>cmd /c start</c>, which starts this repository's fixture and leaves at once, so the window can
/// only be found by following the image the fixture names.
/// </para>
/// <para>
/// The control is the same launch without <c>windowOf</c>: it is refused for drawing no window, so a
/// run that followed children by default could not pass both.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class WindowOfTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-window-of-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private static string Image => Path.GetFileName(Fixture.Executable());

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_fixture_following_an_image_says_so_and_names_it_as_an_executable()
    {
        var fixture = FixtureDeclaration.Of("through the alias", windowOf: "Modelado");

        Assert.Equal("Modelado.exe", fixture.WindowOf);
        Assert.Contains("reading the window Modelado.exe draws", fixture.Sentence(), StringComparison.Ordinal);
        Assert.Equal("", FixtureDeclaration.Of("plain").WindowOf);
    }

    [Theory]
    [Trait(NoDesk.Key, NoDesk.Free)]
    [InlineData("  ", false, "names none")]
    [InlineData(@"C:\apps\Modelado.exe", false, "is a path")]
    [InlineData("Modelado.exe", true, "waits for no window")]
    public void A_blank_image_a_path_or_a_resident_launch_is_refused(string windowOf, bool resident, string said)
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => FixtureDeclaration.Of("through the alias", resident: resident, windowOf: windowOf));

        Assert.Contains(said, refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_case_file_reads_the_field()
    {
        var cases = ScenarioFile.Read(
            "window-of.cases.json",
            """
            {
              "fixtures": [ { "name": "via the alias", "application": "cli.exe", "windowOf": "Modelado.exe" } ],
              "cases": [ { "name": "c", "fixture": "via the alias", "steps": [ { "locator": "Text#heading", "act": "read", "reads": "name", "expect": "x", "named": "s" } ] } ]
            }
            """);

        Assert.Equal("Modelado.exe", Assert.Single(cases).Fixture.WindowOf);
    }

    [Fact]
    public void A_window_a_launchers_child_draws_is_followed_and_the_launcher_leaving_is_no_early_exit()
    {
        var verdict = Run(Image);
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void The_same_launcher_without_the_image_is_refused_for_drawing_no_window()
    {
        if (!Desk.Read().CanObserve)
            return;

        var refused = Assert.Throws<ScenarioRefusedException>(() => Run(windowOf: null));

        Assert.Contains("drew no window", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void An_image_the_launch_never_started_is_refused_naming_what_it_did_start()
    {
        if (!Desk.Read().CanObserve)
            return;

        var refused = Assert.Throws<ScenarioRefusedException>(() => Run("nothing-draws-this.exe"));

        Assert.Contains("follows the window nothing-draws-this.exe draws, and none drew one", refused.Because, StringComparison.Ordinal);
        // The launcher has gone by the deadline; the child it started is still there to be named.
        Assert.Contains(Image, refused.Because, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>
    /// Read the fixture's heading through <c>cmd /c start</c>, following <paramref name="windowOf"/>
    /// where one is named; or null where this desk cannot observe.
    /// </summary>
    private SuiteVerdict? Run(string? windowOf)
    {
        if (!Desk.Read().CanObserve)
            return null;

        // Unquoted by the launch only where it holds no space, and a quoted first argument is the
        // title `start` reads rather than the program it starts.
        Assert.DoesNotContain(' ', Fixture.Executable());

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            """
            {
              "executable": "not-built.exe",
              "timeouts": { "launch": 15000, "resolve": 4000, "act": 4000, "poll": 25 }
            }
            """);

        static string json(string value) => System.Text.Json.JsonSerializer.Serialize(value);
        var following = windowOf is null ? "" : $"\"windowOf\": {json(windowOf)}, ";
        var cases = $$"""
            {
              "fixtures": [
                {
                  {{following}}"name": "through a launcher",
                  "application": {{json(Path.Combine(Environment.SystemDirectory, "cmd.exe"))}},
                  "arguments": ["/c", "start", "/b", {{json(Fixture.Executable())}}, "--status"]
                }
              ],
              "cases": [
                {
                  "name": "the window a launcher's child draws",
                  "catches": "a window drawn by the process a launcher starts, which the wait never asked about",
                  "fixture": "through a launcher",
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
            verdict = Suite.Launch(ScenarioFile.Read("window-of.cases.json", cases), Selection.All, register, project);
        }
        finally
        {
            register.StopAll();
        }

        return verdict;
    }
}

using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW608. A claim that an act's handler showed a declared string, watched from before the act.
/// <para>
/// Found by claude-tray (T435): a rescan's invoke returns before its handler runs, so a read after it
/// could not tell a rescan that kept the selection from one that had not started, and passed on the
/// build that broke it. The page's <c>Scanning…</c> line is the evidence, and it is gone before
/// anything after the act can look. Driven against the fixture's drags pane, whose rescan shows the
/// line for 150ms after the invoke returns and whose quiet rescan never shows it.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class PassesTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-passes-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void A_line_the_handler_shows_after_the_invoke_returns_is_seen()
    {
        var verdict = Run("Button#rescanButton");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void An_act_whose_handler_never_shows_the_line_fails_naming_it()
    {
        // The control: the same end state, and the line never shown. A claim that passed on the end
        // state would pass here, and this is red.
        var verdict = Run("Button#quietButton");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        var said = Said(verdict);
        Assert.Equal(RunOutcome.Failed, verdict.Outcome);
        Assert.Contains("scan.scanning", said, StringComparison.Ordinal);
        Assert.Contains("not seen", said, StringComparison.Ordinal);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void It_is_a_claim_of_its_own_and_rides_only_on_an_act()
    {
        var step = Wrote.Step("Button#rescanButton", "invoke", ("passes", "scan.scanning"));
        Assert.True(step.Checkable);
        Assert.Equal("scan.scanning", step.Passes);

        Assert.Contains(
            "which starts nothing",
            Assert.Throws<ScenarioRefusedException>(() => Wrote.Step("Text#scanSaid", "read", ("passes", "scan.scanning"))).Because,
            StringComparison.Ordinal);
        Assert.Contains(
            "also makes another claim",
            Assert.Throws<ScenarioRefusedException>(
                () => Wrote.Step("Button#rescanButton", "invoke", ("passes", "scan.scanning"), ("never", "scan.scanning"))).Because,
            StringComparison.Ordinal);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>
    /// Invoke <paramref name="pressed"/> claiming the scanning line passes, then read that the scan is
    /// done; or null where this desk cannot observe.
    /// </summary>
    private SuiteVerdict? Run(string pressed)
    {
        if (!Desk.Read().CanObserve)
            return null;

        File.WriteAllText(
            Path.Combine(root, "strings.en.json"),
            $$"""
            { "scan.scanning": "Scanning the projects" }
            """);

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "languageFiles": ["strings.en.json"],
              "language": { "fallback": "en" },
              "timeouts": { "launch": 30000, "resolve": 4000, "act": 2000, "poll": 25 }
            }
            """);

        var cases = $$"""
            {
              "fixtures": [ { "name": "a page that rescans", "arguments": ["--drags", "--show"] } ],
              "cases": [
                {
                  "name": "a rescan shows that it ran",
                  "catches": "a rescan whose invoke returned before it ran, read as done by the step after it",
                  "fixture": "a page that rescans",
                  "steps": [
                    { "locator": "{{pressed}}", "act": "invoke", "passes": "scan.scanning", "named": "the rescan shows it is scanning" },
                    { "locator": "Text#scanSaid", "act": "read", "reads": "name", "expect": "scanned", "named": "the rescan is done" }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(ScenarioFile.Read("passes.cases.json", cases), Selection.All, register, project);
        register.StopAll();
        return verdict;
    }
}

using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW548. A step claims its element covers the whole of the monitor it is on.
/// <para>
/// Found by quickshell (QS257): F11 took the frame away, and a case could read only that it had gone,
/// so a full screen leaving a border uncovered passed. Driven against the fixture's <c>--screen</c>
/// shape: a terminal covering its monitor to the pixel, and one stopping eight pixels short of the
/// right and bottom edges — the red has to name both rectangles and the two edges it left.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class FillsMonitorTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-fills-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void A_fills_monitor_claim_loads_as_one_claim_on_a_read()
    {
        var step = Wrote.Step("Edit#screenTerminal", "read", ("fillsMonitor", true));

        Assert.True(step.FillsMonitor);
        Assert.Equal("fillsMonitor", Assert.Single(step.Claims).Field);
    }

    [Theory]
    [InlineData("invoke", null, "the act is 'read'")]
    [InlineData("read", "name", "'reads' says nothing here")]
    public void A_fills_monitor_claim_beside_an_act_or_a_reading_is_refused(string act, string? reads, string because)
    {
        var refused = Assert.Throws<ScenarioRefusedException>(() => reads is null
            ? Wrote.Step("Edit#screenTerminal", act, ("fillsMonitor", true))
            : Wrote.Step("Edit#screenTerminal", act, ("fillsMonitor", true), ("reads", reads)));

        Assert.Contains(because, refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_fills_monitor_claim_beside_a_within_claim_is_refused_as_two_claims()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => Wrote.Step("Edit#screenTerminal", "read", ("fillsMonitor", true), ("within", "Pane#page")));

        Assert.Contains("fillsMonitor", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_terminal_in_full_screen_covers_its_monitor()
    {
        var verdict = Run("fill");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
        Assert.Contains("covers its monitor", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_terminal_short_of_its_monitor_fails_naming_the_edges_it_left()
    {
        var verdict = Run("short");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        var said = Said(verdict);
        Assert.True(verdict.Outcome == RunOutcome.Failed, said);
        Assert.Contains("does not cover its monitor", said, StringComparison.Ordinal);
        Assert.Contains("8px at the right, 8px at the bottom left uncovered", said, StringComparison.Ordinal);
    }

    /// <summary>One fillsMonitor claim against the screen shape, or null where this desk cannot observe.</summary>
    private SuiteVerdict? Run(string screen)
    {
        if (!Desk.Read().CanObserve)
            return null;

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "launch": 30000, "resolve": 2000, "act": 4000, "poll": 25 }
            }
            """);

        var cases = $$"""
            {
              "fixtures": [ { "name": "a terminal in full screen", "arguments": ["--screen={{screen}}"] } ],
              "cases": [
                {
                  "name": "the terminal covers its monitor",
                  "catches": "a full screen that leaves a border or a strip of the monitor uncovered",
                  "fixture": "a terminal in full screen",
                  "steps": [ { "locator": "Edit#screenTerminal", "act": "read", "fillsMonitor": true } ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(ScenarioFile.Read("fills.cases.json", cases), Selection.All, register, project);

        register.StopAll();
        return verdict;
    }

    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));
}

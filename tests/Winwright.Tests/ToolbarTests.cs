using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW571. A toolbar button moved into the toolbar's overflow, and the refusal that says so.
/// <para>
/// Found by PhotoBoost (PB65): on the guest, whose window was narrower than the host's, the editor's
/// toolbar moved its last buttons into its overflow, and the step pressing one was refused as nothing
/// matched in 46 looks — which read as a misspelled id. Driven against the fixture's
/// <c>--toolbar</c> pane, whose toolbar is as wide as the window, sized by WW570's <c>resize</c>.
/// </para>
/// <para>
/// The control is the same step in a wide window, where the button is on the bar and the press
/// lands: a refusal naming the overflow whatever the width would pass the first case and fail this.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ToolbarTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-toolbar-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void A_button_in_the_overflow_is_refused_naming_the_toolbar_that_holds_it()
    {
        var verdict = Run(
            """
            { "locator": "ToolBar#editorToolbar", "act": "resize", "with": "360x420", "named": "the window is narrowed" },
            { "locator": "Button#rotateRight", "act": "invoke", "named": "Rotate right is pressed" },
            { "locator": "Text#toolbarSaid", "act": "read", "reads": "name", "expect": "pressed: Rotate right", "named": "the press landed" }
            """);
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        var said = Said(verdict);
        Assert.True(verdict.Outcome != RunOutcome.Passed, said);
        Assert.Contains("ToolBar #editorToolbar (opened by Button #OverflowButton) holds items in its overflow", said, StringComparison.Ordinal);
    }

    [Fact]
    public void The_same_button_on_a_wide_window_is_on_the_bar_and_takes_the_press()
    {
        var verdict = Run(
            """
            { "locator": "ToolBar#editorToolbar", "act": "resize", "with": "1600x420", "named": "the window is widened" },
            { "locator": "Button#rotateRight", "act": "invoke", "named": "Rotate right is pressed" },
            { "locator": "Text#toolbarSaid", "act": "read", "reads": "name", "expect": "pressed: Rotate right", "named": "the press landed" }
            """);
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>Run <paramref name="steps"/> against the toolbar pane, or null where this desk cannot observe.</summary>
    private SuiteVerdict? Run(string steps)
    {
        if (!Desk.Read().CanObserve)
            return null;

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "launch": 30000, "resolve": 3000, "act": 6000, "poll": 50 }
            }
            """);

        var cases = $$"""
            {
              "fixtures": [
                { "name": "the toolbar pane", "arguments": ["--toolbar"] }
              ],
              "cases": [
                {
                  "name": "a toolbar button is pressed",
                  "catches": "a toolbar button moved into the overflow on a narrower desk, refused as a misspelled id",
                  "fixture": "the toolbar pane",
                  "steps": [
                    {{steps}}
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(ScenarioFile.Read("toolbar.cases.json", cases), Selection.All, register, project);

        register.StopAll();
        return verdict;
    }
}

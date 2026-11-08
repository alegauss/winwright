using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW610. A case puts the keyboard focus on an element, so the press after it lands there.
/// <para>
/// Found by claude-tray (T438): Sessions rows open on Enter or Space once focused, and a press sends its
/// key at the window, so it went wherever focus already was. Driven against the fixture's drags pane,
/// whose two buttons each say they were pressed: a focus that did not land, or a press that ignored it,
/// reads as the other button.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class FocusTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-focus-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void A_key_pressed_after_a_focus_lands_on_the_element_focused()
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

        // B then A, so each press has to land somewhere other than where the last one did.
        var cases = """
            {
              "fixtures": [ { "name": "two buttons a key can press", "arguments": ["--drags", "--show"] } ],
              "cases": [
                {
                  "name": "the keyboard opens what has the focus",
                  "catches": "a row opened by Enter or Space once focused, with no step that could put the focus on it",
                  "fixture": "two buttons a key can press",
                  "steps": [
                    { "locator": "Button#keyB", "act": "focus", "reads": "focused", "expect": "focused", "named": "B has the focus" },
                    { "locator": "Button#keyB", "act": "press", "with": "Enter", "named": "Enter is pressed" },
                    { "locator": "Text#keySaid", "act": "read", "reads": "name", "expect": "pressed: B", "named": "Enter pressed B" },
                    { "locator": "Button#keyA", "act": "focus", "reads": "focused", "expect": "focused", "named": "A has the focus" },
                    { "locator": "Button#keyA", "act": "press", "with": "Space", "named": "Space is pressed" },
                    { "locator": "Text#keySaid", "act": "read", "reads": "name", "expect": "pressed: A", "named": "Space pressed A" }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(ScenarioFile.Read("focus.cases.json", cases), Selection.All, register, project);
        register.StopAll();
        if (BusyDesk.Excused(verdict))
            return;

        Assert.True(
            verdict.Outcome == RunOutcome.Passed,
            string.Join(
                Environment.NewLine,
                verdict.Render()
                    .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
                    .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
                    .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString())))));
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_focus_step_loads_and_takes_nothing_in_with()
    {
        var step = Wrote.Step("Button#keyB", "focus", ("reads", "focused"), ("expect", "focused"));
        Assert.Equal("focus", step.Verb.Name);
        Assert.False(step.Verb.Synthesises);

        Assert.Throws<ScenarioRefusedException>(() => Wrote.Step("Button#keyB", "focus", ("with", "Enter")));
    }
}

using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW539. A step claims its element is drawn inside another.
/// <para>
/// Found by quickshell (QS312): a tab's close button was 22 by 22 in the tree and painted nothing,
/// and every case passed. Driven against the fixture's <c>--layout</c> pane: a button with room, one
/// spilling past a box too narrow for it, and one laid out zero wide — the two reds are the controls,
/// and each has to name the rectangles it measured.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class WithinTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-within-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void A_within_claim_loads_as_one_claim_on_a_read()
    {
        var step = Wrote.Step("Button#fitsButton", "read", ("within", "Group#roomyBox"));

        Assert.Equal("Group#roomyBox", step.Within!.Text);
        Assert.Equal("within", Assert.Single(step.Claims).Field);
    }

    [Theory]
    [InlineData("invoke", null, "the act is 'read'")]
    [InlineData("read", "name", "'reads' says nothing here")]
    public void A_within_claim_beside_an_act_or_a_reading_is_refused(string act, string? reads, string because)
    {
        var refused = Assert.Throws<ScenarioRefusedException>(() => reads is null
            ? Wrote.Step("Button#fitsButton", act, ("within", "Group#roomyBox"))
            : Wrote.Step("Button#fitsButton", act, ("within", "Group#roomyBox"), ("reads", reads)));

        Assert.Contains(because, refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_button_with_room_is_drawn_within_its_box()
    {
        var verdict = Run("Button#fitsButton", "Group#roomyBox");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_button_spilling_out_of_a_narrow_box_fails_naming_both_rectangles()
    {
        var verdict = Run("Button#spillsButton", "Group#narrowBox");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Failed, Said(verdict));
        Assert.Contains("is not within Group#narrowBox", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_button_laid_out_to_nothing_fails_as_empty()
    {
        var verdict = Run("Button#emptyButton", "Group#squeezedBox");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Failed, Said(verdict));
        Assert.Contains("is drawn empty", Said(verdict), StringComparison.Ordinal);
    }

    /// <summary>One within claim against the layout pane, or null where this desk cannot observe.</summary>
    private SuiteVerdict? Run(string element, string container)
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
              "fixtures": [ { "name": "three buttons in three boxes", "arguments": ["--layout", "--show"] } ],
              "cases": [
                {
                  "name": "a button is drawn inside its box",
                  "catches": "a control the layout clipped or left no room, which every case reading its name passed",
                  "fixture": "three buttons in three boxes",
                  "steps": [ { "locator": {{System.Text.Json.JsonSerializer.Serialize(element)}}, "act": "read", "within": {{System.Text.Json.JsonSerializer.Serialize(container)}} } ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(ScenarioFile.Read("within.cases.json", cases), Selection.All, register, project);

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

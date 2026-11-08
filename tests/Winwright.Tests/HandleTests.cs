using Winwright.Acting;
using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW572. A drag between two points of one element, for a handle drawn on a canvas.
/// <para>
/// Found by PhotoBoost (PB67): the crop editor is drawn on a GPU surface, so its handles are pixels
/// and not elements, and resizing the crop presses on a handle and releases elsewhere on the same
/// surface. Driven against the fixture's <c>--handles</c> pane, whose surface is one element and
/// whose crop's top-left handle is taken only by a press near that corner.
/// </para>
/// <para>
/// The control is the same drag pressed in the middle of the crop, away from the handle: it moves
/// the pointer just as far and must leave the crop as it was, so a fixture that moved the corner on
/// any drag, or a drag that ignored where it was told to press, cannot pass both.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class HandleTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-handle-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void A_handle_pressed_at_its_point_and_released_at_another_moves_there()
    {
        var verdict = Run("10%,10%", "40%,40%", "crop: 120,60 150x75");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void The_same_drag_pressed_away_from_the_handle_moves_nothing()
    {
        var verdict = Run("50%,50%", "80%,80%", "crop: 30,15 240x120");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_point_in_pixels_presses_where_its_percentage_would()
    {
        // The handle is 30 by 15 device-independent pixels in from the corner, whatever the display's
        // scale, which is the claim a point in pixels makes.
        var verdict = Run("30,15", "120,60", "crop: 120,60 150x75");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_click_at_a_point_inside_the_crop_lands_there()
    {
        // WW603. wintimeline's WT57: a spot on a canvas is clicked where it is, not at the centre.
        var verdict = Clicked("50%,50%", "clicked: crop");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_click_at_a_point_in_the_margin_answers_differently()
    {
        // The other point of the same surface. The centre is inside the crop, so a click that ignored
        // its point would read 'crop' here and fail.
        var verdict = Clicked("5,5", "clicked: margin");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_click_at_a_point_outside_its_element_is_refused_and_presses_nothing()
    {
        // The surface is 300 wide, so 400 across is past its edge: the act refuses it naming the size,
        // and the read after it still says nothing was clicked.
        var verdict = Clicked("400,10", "nothing clicked");
        if (verdict is null)
            return;

        var said = Said(verdict);
        Assert.NotEqual(RunOutcome.Passed, verdict.Outcome);
        Assert.Contains("'400,10' is outside it", said, StringComparison.Ordinal);
        Assert.Contains("300x150 device-independent pixels", said, StringComparison.Ordinal);
    }

    [Fact]
    public void A_click_at_a_point_is_refused_where_it_is_written_wrongly()
    {
        var step = Wrote.Step("Pane#cropSurface", "click", ("with", "PointerIsTheAct"), ("at", "8.9%,6"));
        Assert.Equal("8.9%,6", step.At!.Text);
        Assert.Equal("click Pane#cropSurface at 8.9%,6", step.Name);
        Assert.Null(Wrote.Step("Pane#cropSurface", "click", ("with", "PointerIsTheAct")).At);

        Assert.Contains(
            "'at' is '8.9%'",
            Assert.Throws<ScenarioRefusedException>(
                () => Wrote.Step("Pane#cropSurface", "click", ("with", "PointerIsTheAct"), ("at", "8.9%"))).Because,
            StringComparison.Ordinal);
        Assert.Contains(
            "a drag says its points in 'from' and 'to'",
            Assert.Throws<ScenarioRefusedException>(
                () => Wrote.Step("Pane#cropSurface", "drag", ("to", "Pane#other"), ("at", "10%,10%"))).Because,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_point_is_read_the_way_a_case_writes_it()
    {
        var percent = ElementPoint.Parse(" 12.5%, 8% ");
        Assert.NotNull(percent);
        Assert.Equal((12.5, true, 8d, true), (percent.X, percent.XIsPercent, percent.Y, percent.YIsPercent));

        var pixels = ElementPoint.Parse("40,30");
        Assert.NotNull(pixels);
        Assert.Equal((40d, false, 30d, false), (pixels.X, pixels.XIsPercent, pixels.Y, pixels.YIsPercent));

        Assert.All(
            ["", "40", "40,30,2", "-4,30", "101%,0%", "40.5,30", "12,5%,8%", "wide,30"],
            written => Assert.Null(ElementPoint.Parse(written)));
    }

    [Fact]
    public void A_drag_inside_its_element_is_refused_where_it_is_written_wrongly()
    {
        var step = Wrote.Step("Pane#cropSurface", "drag", ("from", "10%,10%"), ("to", "40%,30%"));
        Assert.Equal("10%,10%", step.From!.Text);
        Assert.Equal("40%,30%", step.ToPoint!.Text);
        Assert.Null(step.To);

        Assert.Contains(
            "names no 'to'",
            Assert.Throws<ScenarioRefusedException>(() => Wrote.Step("Pane#cropSurface", "drag", ("from", "10%,10%"))).Because,
            StringComparison.Ordinal);
        Assert.Contains(
            "'to' is 'Pane#other'",
            Assert.Throws<ScenarioRefusedException>(
                () => Wrote.Step("Pane#cropSurface", "drag", ("from", "10%,10%"), ("to", "Pane#other"))).Because,
            StringComparison.Ordinal);
        Assert.Contains(
            "'from' is '10%'",
            Assert.Throws<ScenarioRefusedException>(
                () => Wrote.Step("Pane#cropSurface", "drag", ("from", "10%"), ("to", "40%,30%"))).Because,
            StringComparison.Ordinal);
        Assert.Contains(
            "only a 'drag'",
            Assert.Throws<ScenarioRefusedException>(
                () => Wrote.Step("Pane#cropSurface", "click", ("with", "PointerIsTheAct"), ("from", "10%,10%"), ("to", "40%,30%"))).Because,
            StringComparison.Ordinal);
        Assert.Contains(
            "'toDesktop'",
            Assert.Throws<ScenarioRefusedException>(
                () => Wrote.Step("Pane#cropSurface", "drag", ("from", "10%,10%"), ("to", "40%,30%"), ("toDesktop", true))).Because,
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
    /// Click the crop surface at <paramref name="at"/> and read where the click landed expecting
    /// <paramref name="expected"/>, or null where this desk cannot observe. WW603.
    /// </summary>
    private SuiteVerdict? Clicked(string at, string expected)
    {
        if (!Desk.Read().CanObserve)
            return null;

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "launch": 30000, "resolve": 4000, "act": 4000, "poll": 25 }
            }
            """);

        var cases = $$"""
            {
              "fixtures": [ { "name": "a crop drawn on a surface", "arguments": ["--handles", "--show"] } ],
              "cases": [
                {
                  "name": "the surface is clicked at a point",
                  "catches": "a spot drawn on a canvas, with no click that presses anywhere but the element's centre",
                  "fixture": "a crop drawn on a surface",
                  "steps": [
                    { "locator": "Pane#cropSurface", "act": "click", "with": "PointerIsTheAct", "at": "{{at}}", "named": "the surface is clicked" },
                    { "locator": "Text#clickSaid", "act": "read", "reads": "name", "expect": "{{expected}}", "named": "the surface says where" }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(ScenarioFile.Read("click-at.cases.json", cases), Selection.All, register, project);

        register.StopAll();
        return verdict;
    }

    /// <summary>
    /// Drag on the crop surface from <paramref name="from"/> to <paramref name="to"/> and read the
    /// crop expecting <paramref name="expected"/>, or null where this desk cannot observe.
    /// </summary>
    private SuiteVerdict? Run(string from, string to, string expected)
    {
        if (!Desk.Read().CanObserve)
            return null;

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "launch": 30000, "resolve": 4000, "act": 4000, "poll": 25 }
            }
            """);

        var cases = $$"""
            {
              "fixtures": [ { "name": "a crop drawn on a surface", "arguments": ["--handles", "--show"] } ],
              "cases": [
                {
                  "name": "a crop handle is dragged",
                  "catches": "a crop handle drawn on a surface, with no drag that presses at one point of an element and releases at another",
                  "fixture": "a crop drawn on a surface",
                  "steps": [
                    { "locator": "Text#cropSaid", "act": "read", "reads": "name", "expect": "crop: 30,15 240x120", "named": "the crop starts where it was drawn" },
                    { "locator": "Pane#cropSurface", "act": "drag", "from": "{{from}}", "to": "{{to}}", "named": "the surface is dragged on" },
                    { "locator": "Text#cropSaid", "act": "read", "reads": "name", "expect": "{{expected}}", "named": "the crop says where it is" }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(ScenarioFile.Read("handle.cases.json", cases), Selection.All, register, project);

        register.StopAll();
        return verdict;
    }
}

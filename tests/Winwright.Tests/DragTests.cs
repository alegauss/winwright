using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW520. One element dragged onto another.
/// <para>
/// Found adopting quickshell's tab strip (QS250), which reorders a tab when it is dragged across its
/// neighbours. A case could click a tab and read whether it was selected, and had no verb to press,
/// move and release elsewhere, so the gesture a user makes was proven only by an in-process test.
/// </para>
/// <para>
/// Driven against the fixture's <c>--drags</c> strip, which no pattern and no key reorders. The
/// control is a drag that ends where it started: it moves the pointer and must leave the order as it
/// was, so a run that could pass by reading a read-out that always said the reordered thing cannot.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class DragTests : IDisposable
{
    /// <summary>
    /// The fixture's second window's drop zone, from the desktop down — the title is
    /// <c>DragsPane.DropWindowTitle</c>, spelled out because this suite builds the fixture without referencing it.
    /// </summary>
    private const string DropZone = "Window[name=\"winwright drop target\"] > Text#dropZone";

    private readonly string root = Directory.CreateTempSubdirectory("winwright-drag-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void A_tab_dragged_onto_its_neighbour_takes_its_place()
    {
        var verdict = Run("TabItem#secondTab", "Second, First, Third");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_tab_dragged_past_its_neighbour_lands_on_the_one_it_was_released_over()
    {
        var verdict = Run("TabItem#thirdTab", "Second, Third, First");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_drag_that_ends_where_it_started_moves_nothing()
    {
        // The control: the pointer still goes past the threshold and back, so a strip that reordered
        // on any movement at all, or a read-out that said a moved order whatever happened, fails here.
        var verdict = Run("TabItem#firstTab", "First, Second, Third");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_note_dragged_onto_another_window_is_dropped_there()
    {
        // WW530. The destination is another top-level window, found from the desktop by its title,
        // and what it took is reported back in the window under test.
        var verdict = Run(
            "Text#dragNote",
            DropZone,
            "Text#dropSaid",
            "nothing dropped",
            "dropped: A note",
            desktop: true);
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_destination_in_another_window_is_not_found_in_the_window_under_test()
    {
        // The control: the same locator without 'toDesktop' is looked for where the drag starts,
        // where no such window is, so nothing is pressed and nothing is dropped.
        var verdict = Run(
            "Text#dragNote",
            DropZone,
            "Text#dropSaid",
            "nothing dropped",
            "dropped: A note",
            desktop: false);
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_file_dragged_onto_a_folder_the_fixture_opened_lands_there_byte_for_byte()
    {
        // WW535 and WW536 together, which is quickshell's QS188 written as a case: the fixture opens
        // the folder beside the window, the drag drops onto Explorer from the desktop, and a file
        // step reads what landed. ExplorerDropRun did all three in C# because a case could do none.
        if (!Desk.Read().CanObserve)
            return;

        var payload = new byte[64 * 1024];
        Random.Shared.NextBytes(payload);
        Directory.CreateDirectory(Path.Combine(root, "data"));
        File.WriteAllBytes(Path.Combine(root, "data", "report.bin"), payload);

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "launch": 30000, "resolve": 10000, "act": 4000, "poll": 50 }
            }
            """);

        var cases = """
            {
              "fixtures": [
                {
                  "name": "a note that carries a file",
                  "arguments": ["--drags", "--show"],
                  "files": ["data/report.bin"],
                  "variables": { "WINWRIGHT_DRAG_FILE": "{files}/report.bin" },
                  "opens": "{files}/winwright-landing"
                }
              ],
              "cases": [
                {
                  "name": "a file dropped onto an Explorer folder is there byte for byte",
                  "catches": "a drop onto Explorer that a case could neither aim at nor check",
                  "fixture": "a note that carries a file",
                  "steps": [
                    {
                      "locator": "Text#dragNote",
                      "act": "drag",
                      "to": "Window[nameStarts=\"winwright-landing\"] > List[class=UIItemsView]",
                      "toDesktop": true,
                      "named": "the note is dropped on the folder"
                    },
                    {
                      "file": "{files}/winwright-landing/report.bin",
                      "act": "read",
                      "sameBytesAs": "data/report.bin",
                      "named": "the file landed with its bytes"
                    }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(ScenarioFile.Read("explorer.cases.json", cases), Selection.All, register, project);
        var survivors = register.StopAll();
        if (BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));

        // And the folder's window was given back with the case: nothing it opened is still standing.
        Assert.Empty(survivors);
    }

    [Fact]
    public void A_file_dragged_in_from_outside_is_taken_by_the_zone_it_is_dropped_on()
    {
        // WW550. PhotoBoost's PB23 written as a case: the fixture stages the file, the step drops it
        // from outside the application onto the zone, and the zone says what it took.
        var verdict = Dropped("Text#fileZone", "file dropped: photo.png, 4096 bytes");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_file_dropped_where_nothing_takes_it_is_a_hole_that_says_so()
    {
        // The control: the same drop released over a read-out that takes no drop. A drop that read as
        // landed whatever the target answered would pass here, and the hole has to name the answer.
        var verdict = Dropped("Text#dragOrder", "file dropped: photo.png, 4096 bytes");
        if (verdict is null)
            return;

        // The target's hole is the assertion here and never an excuse: BusyDesk refuses it, rightly,
        // as no fact about the desk. Only a run that never got that far is the desk's.
        var refused = verdict.Ran
            .SelectMany(one => one.Verdict.Results)
            .FirstOrDefault(one => one.DidNotRun && one.Missing?.Name == Winwright.Acting.FileDrop.TargetTakesIt);
        if (refused is null && BusyDesk.Excused(verdict))
            return;

        var said = Said(verdict);
        Assert.True(verdict.Outcome != RunOutcome.Passed, said);
        Assert.NotNull(refused);
        Assert.Contains("no effect", refused.Missing!.Absence, StringComparison.Ordinal);
    }

    [Fact]
    public void A_drop_names_its_file_and_nowhere_else_to_end()
    {
        var bare = Assert.Throws<ScenarioRefusedException>(() => Wrote.Step("Text#fileZone", "drop file"));
        Assert.Contains("'drop file'", bare.Because, StringComparison.Ordinal);

        Assert.Throws<ScenarioRefusedException>(
            () => Wrote.Step("Text#fileZone", "drop file", ("with", "photo.png"), ("to", "Text#dragOrder")));
    }

    [Fact]
    public void A_file_staged_only_for_the_drop_is_one_a_step_looks_at()
    {
        // WW509 refuses staged files the launch never names, and a file staged to be dropped is named
        // by the step and never by the launch. The control is the same fixture with no step naming
        // it, which is still refused.
        const string Fixture = """
            "fixtures": [ { "name": "a zone", "arguments": ["--drags"], "files": ["data/photo.png"] } ]
            """;

        const string Read = """{ "locator": "Text#fileSaid", "act": "read", "reads": "name", "expect": "file dropped: photo.png, 4096 bytes" }""";

        var read = ScenarioFile.Read("drop.cases.json", $$"""
            { {{Fixture}}, "cases": [ { "name": "dropped", "catches": "a drop zone with no case", "fixture": "a zone",
              "steps": [ { "locator": "Text#fileZone", "act": "drop file", "with": "{files}/photo.png" }, {{Read}} ] } ] }
            """);
        Assert.Single(read);

        var refused = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("drop.cases.json", $$"""
            { {{Fixture}}, "cases": [ { "name": "dropped", "catches": "a drop zone with no case", "fixture": "a zone",
              "steps": [ { "locator": "Text#fileZone", "act": "drop file", "with": "data/photo.png" }, {{Read}} ] } ] }
            """));
        Assert.Contains("cannot be looking at them", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_dropped_file_is_found_the_way_a_file_step_finds_one()
    {
        var staged = Path.Combine(root, "staged");
        var step = Wrote.Step("Text#fileZone", "drop file", ("with", "data/photo.png"));
        var fromStage = Wrote.Step("Text#fileZone", "drop file", ("with", "{files}/photo.png"));

        Assert.Equal(Path.Combine(root, "data", "photo.png"), step.Rooted(root, staged, null).Argument);
        Assert.Equal(Path.Combine(staged, "photo.png"), fromStage.Rooted(root, staged, null).Argument);
        Assert.True(step.Verb.Synthesises);
        Assert.False(step.Verb.Repeatable);
    }

    [Fact]
    public void A_middle_click_on_a_tab_closes_it()
    {
        // WW538. quickshell's QS310 gesture, on the fixture's strip.
        var verdict = Clicked("middle", "First, Third");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void Alt_tapped_alone_shows_the_menu_bar_and_a_second_tap_hides_it()
    {
        // WW544. quickshell's QS354 gesture: a bar hidden until Alt goes down and up with nothing
        // between. The second tap is the control — a reading that always found the bar would pass the
        // first half and fail this one.
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

        var cases = """
            {
              "fixtures": [ { "name": "a bar shown by Alt", "arguments": ["--drags", "--show"] } ],
              "cases": [
                {
                  "name": "Alt tapped alone shows the bar",
                  "catches": "a menu bar shown the Windows way, by Alt tapped alone, that no press could make",
                  "fixture": "a bar shown by Alt",
                  "steps": [
                    { "locator": "MenuItem#altFile", "act": "read", "absent": true, "named": "the bar starts hidden" },
                    { "locator": "Text#dragOrder", "act": "press", "with": "Alt", "named": "Alt is tapped" },
                    { "locator": "MenuItem#altFile", "act": "read", "reads": "name", "expect": "File", "named": "the bar is shown" },
                    { "locator": "Text#dragOrder", "act": "press", "with": "Alt", "named": "Alt is tapped again" },
                    { "locator": "MenuItem#altFile", "act": "read", "absent": true, "named": "the bar is hidden again" }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(ScenarioFile.Read("alt.cases.json", cases), Selection.All, register, project);
        register.StopAll();
        if (BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_left_click_on_the_same_tab_closes_nothing()
    {
        // The control: the same step on the primary button selects the tab and leaves the strip whole.
        var verdict = Clicked("left", "First, Second, Third");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    /// <summary>
    /// Drop a staged file onto <paramref name="onto"/> from outside the application and read the
    /// zone's read-out expecting <paramref name="expected"/>, or null where this desk cannot observe.
    /// WW550.
    /// </summary>
    private SuiteVerdict? Dropped(string onto, string expected)
    {
        if (!Desk.Read().CanObserve)
            return null;

        Directory.CreateDirectory(Path.Combine(root, "data"));
        File.WriteAllBytes(Path.Combine(root, "data", "photo.png"), new byte[4096]);

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
              "fixtures": [
                { "name": "a zone that takes files", "arguments": ["--drags", "--show"], "files": ["data/photo.png"] }
              ],
              "cases": [
                {
                  "name": "a file is dropped from outside",
                  "catches": "a drop zone that takes files from Explorer, with no step that could start a drag outside the application",
                  "fixture": "a zone that takes files",
                  "steps": [
                    { "locator": "Text#fileSaid", "act": "read", "reads": "name", "expect": "no file dropped", "named": "nothing was dropped yet" },
                    { "locator": {{System.Text.Json.JsonSerializer.Serialize(onto)}}, "act": "drop file", "with": "{files}/photo.png", "named": "the file is dropped" },
                    { "locator": "Text#fileSaid", "act": "read", "reads": "name", "expect": {{System.Text.Json.JsonSerializer.Serialize(expected)}}, "named": "the zone took the file" }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(ScenarioFile.Read("drop.cases.json", cases), Selection.All, register, project);

        register.StopAll();
        return verdict;
    }

    /// <summary>
    /// Click the strip's second tab with <paramref name="button"/> and read the order, or null where
    /// this desk cannot observe.
    /// </summary>
    private SuiteVerdict? Clicked(string button, string expected)
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
              "fixtures": [ { "name": "a strip closed by the middle button", "arguments": ["--drags", "--show"] } ],
              "cases": [
                {
                  "name": "a tab is clicked with one button",
                  "catches": "a tab closed by a middle click, with no step that could press the middle button",
                  "fixture": "a strip closed by the middle button",
                  "steps": [
                    { "locator": "TabItem#secondTab", "act": "click", "with": "PointerIsTheAct", "button": "{{button}}" },
                    { "locator": "Text#dragOrder", "act": "read", "reads": "name", "expect": "{{expected}}" }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(ScenarioFile.Read("click.cases.json", cases), Selection.All, register, project);

        register.StopAll();
        return verdict;
    }

    [Fact]
    public void Looking_from_the_desktop_with_no_destination_is_refused_where_it_is_written()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => Wrote.Step("TabItem#firstTab", "click", ("with", "PointerIsTheAct"), ("toDesktop", true)));

        Assert.Contains("'toDesktop'", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_drag_with_nowhere_to_end_is_refused_where_it_is_written()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(() => Wrote.Step("TabItem#firstTab", "drag"));

        Assert.Contains("names no 'to'", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_destination_beside_an_act_that_ends_where_it_starts_is_refused()
    {
        // A case reading as though it moves something while its act only clicks would be green about
        // a gesture nobody made.
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => Wrote.Step("TabItem#firstTab", "click", ("with", "PointerIsTheAct"), ("to", "TabItem#secondTab")));

        Assert.Contains("'click'", refused.Because, StringComparison.Ordinal);
        Assert.Contains("'drag'", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_destination_is_held_to_the_locator_grammar_and_substituted_like_the_locator()
    {
        Assert.Throws<ScenarioRefusedException>(() => Wrote.Step("TabItem#firstTab", "drag", ("to", "TabItem[")));

        var step = Wrote.Step("TabItem[name=\"{}\"]", "drag", ("to", "Tab#dragStrip > TabItem[name=\"{}\"]"));
        var member = step.For("First");

        Assert.Equal("Tab#dragStrip > TabItem[name=\"First\"]", member.To!.Text);
        Assert.True(step.Verb.Synthesises);
        Assert.False(step.Verb.Repeatable);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>
    /// Drag the first tab onto <paramref name="onto"/> and read the order, or null where this desk
    /// cannot observe.
    /// </summary>
    private SuiteVerdict? Run(string onto, string expected) =>
        Run("TabItem#firstTab", onto, "Text#dragOrder", "First, Second, Third", expected, desktop: false);

    /// <summary>
    /// Read <paramref name="readOut"/> expecting <paramref name="opening"/>, drag
    /// <paramref name="from"/> onto <paramref name="onto"/>, and read it again expecting
    /// <paramref name="expected"/>; or null where this desk cannot observe.
    /// </summary>
    private SuiteVerdict? Run(
        string from, string onto, string readOut, string opening, string expected, bool desktop)
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
              "fixtures": [
                { "name": "a strip reordered by dragging", "arguments": ["--drags", "--show"] }
              ],
              "cases": [
                {
                  "name": "a tab is dragged to a new place",
                  "catches": "a tab strip reordered by dragging, with no verb that presses, moves past the threshold and releases elsewhere",
                  "fixture": "a strip reordered by dragging",
                  "steps": [
                    {
                      "locator": {{System.Text.Json.JsonSerializer.Serialize(readOut)}},
                      "act": "read",
                      "reads": "name",
                      "expect": {{System.Text.Json.JsonSerializer.Serialize(opening)}},
                      "named": "the read-out says what it opens with"
                    },
                    {
                      "locator": {{System.Text.Json.JsonSerializer.Serialize(from)}},
                      "act": "drag",
                      "to": {{System.Text.Json.JsonSerializer.Serialize(onto)}},
                      "toDesktop": {{(desktop ? "true" : "false")}},
                      "named": "it is dragged"
                    },
                    {
                      "locator": {{System.Text.Json.JsonSerializer.Serialize(readOut)}},
                      "act": "read",
                      "reads": "name",
                      "expect": {{System.Text.Json.JsonSerializer.Serialize(expected)}},
                      "named": "the read-out says where it went"
                    }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(
            ScenarioFile.Read("drag.cases.json", cases), Selection.All, register, project);

        register.StopAll();
        return verdict;
    }
}

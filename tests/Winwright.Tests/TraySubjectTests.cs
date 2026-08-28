using System.Text.Json;
using System.Text.Json.Nodes;

using Winwright.Acting;
using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW258. Every locator in a case resolves against the window the fixture launched, and the
/// notification area is in the shell's tree rather than that window's — so no locator reaches it, and
/// an icon has no clickable point either. The engine has found icons by the name the shell gives them
/// since block D and nothing a data file could write got there.
/// <para>
/// The shape settled on is a second kind of subject rather than another verb over the grammar: a step
/// carries a <c>locator</c> or a <c>tray</c>, exactly one of the two. These hold the format half —
/// what loads, what is refused, and that the schema a tool is published says the same thing the run
/// enforces.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class TraySubjectTests
{
    /// <summary>One step, as a file writes it, with whatever fields the case under test needs.</summary>
    private static IReadOnlyList<CaseDeclaration> Read(string fields) => ScenarioFile.Read(
        "tray.cases.json",
        $$"""
        {
          "cases": [
            {
              "name": "the tray icon is there",
              "catches": "a tray that starts and places no icon, which nothing else in a suite would notice",
              "steps": [ { {{fields}} } ]
            }
          ]
        }
        """);

    [Fact]
    public void A_step_can_name_a_tray_icon_instead_of_a_locator()
    {
        var step = Assert.Single(Assert.Single(Read("""
            "tray": "winwright under test", "act": "read", "named": "the icon this run added is showing"
            """)).Steps);

        Assert.Equal("winwright under test", step.Tray);
        Assert.Null(step.Locator);

        // What a trace line and a refusal read, and the one accessor that answers for both kinds of
        // subject — so a report about a tray step never prints an empty locator.
        Assert.Equal("tray icon 'winwright under test'", step.Addressed);
    }

    [Fact]
    public void A_step_that_names_neither_a_locator_nor_a_tray_icon_says_what_it_needs()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(() => Read("""
            "act": "read", "reads": "name", "expect": "anything"
            """));

        Assert.Contains("says what it acts on", refused.Because, StringComparison.Ordinal);
        Assert.Contains("'locator'", refused.Because, StringComparison.Ordinal);
        Assert.Contains("'tray'", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_step_that_names_both_is_refused_because_it_addresses_two_things()
    {
        // The arm a boolean 'required' could not express, and the reason the group exists. Silently
        // honouring whichever the code reads first is how a case ends up asserting about a control
        // nobody meant.
        var refused = Assert.Throws<ScenarioRefusedException>(() => Read("""
            "locator": "Button#icon", "tray": "winwright under test", "act": "read"
            """));

        Assert.Contains("acts on one thing", refused.Because, StringComparison.Ordinal);
        Assert.Contains("'locator'", refused.Because, StringComparison.Ordinal);
        Assert.Contains("'tray'", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_tray_step_carrying_a_claim_about_a_pattern_reading_is_refused_rather_than_ignored()
    {
        // The founding rule of this format pointed at its own newest field. A tray icon is a rectangle
        // and a tooltip — it has no patterns — so a key that loaded and did nothing would be a check
        // the author wrote and the run never made, a dozen times over.
        var refused = Assert.Throws<ScenarioRefusedException>(() => Read("""
            "tray": "winwright under test", "act": "read", "reads": "value", "expect": "anything"
            """));

        Assert.Contains("'expect'", refused.Because, StringComparison.Ordinal);
        Assert.Contains("'reads'", refused.Because, StringComparison.Ordinal);
        Assert.Contains("can be found", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_tray_step_naming_an_act_that_presses_something_is_refused_with_the_ones_it_takes()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(() => Read("""
            "tray": "winwright under test", "act": "invoke"
            """));

        Assert.Contains("'invoke'", refused.Because, StringComparison.Ordinal);
        Assert.Contains("'read'", refused.Because, StringComparison.Ordinal);
        Assert.Contains("'open tray menu'", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_step_can_ask_a_tray_icon_for_its_menu()
    {
        var step = Assert.Single(Assert.Single(Read("""
            "tray": "winwright under test", "act": "open tray menu", "named": "the icon shows a menu"
            """)).Steps);

        Assert.Equal("winwright under test", step.Tray);
        Assert.True(step.OpensTheTrayMenu);
        Assert.True(step.Checkable);
    }

    [Fact]
    public void Reading_the_icon_and_opening_its_menu_are_told_apart_by_the_verb_alone()
    {
        // The two acts a tray takes, and nothing but the verb distinguishes them — no second field,
        // because a flag beside the verb would be a second answer about the same word.
        var found = Assert.Single(Assert.Single(Read("""
            "tray": "winwright under test", "act": "read"
            """)).Steps);

        Assert.False(found.OpensTheTrayMenu);
        Assert.True(found.Verb.Reads);

        Assert.Equal(
            ["read", "open tray menu"],
            ActVerb.All.Where(one => one.OnATray).Select(one => one.Name));
    }

    [Fact]
    public void Opening_a_tray_menu_is_attempted_once_because_the_second_ask_is_a_second_keypress()
    {
        // The rule every synthesised act is under. It also needs the foreground — its route is focus
        // and the application key — so a desk that refused it took the act entirely.
        var verb = ActVerb.Named("open tray menu");

        Assert.False(verb.Repeatable);
        Assert.True(verb.Synthesises);
        Assert.False(verb.Reads);
        Assert.True(verb.OnATray);
    }

    [Fact]
    public void The_schema_a_tool_is_published_says_exactly_one_of_them_too()
    {
        // WW66's rule, and the reason the group lives on the fields rather than in the loader: a
        // caller carrying both, or neither, has to be expressing something the schema refuses — not
        // something the loader explains after the prose exists.
        var schema = ScenarioSchema.AsJsonSchema();
        var step = Walk(schema, "properties", "cases", "items", "properties", "steps", "items");

        Assert.NotNull(step);
        var groups = step!["allOf"]?.AsArray();
        Assert.NotNull(groups);

        var either = Assert.Single(groups!)!["oneOf"]!.AsArray()
            .Select(one => one!["required"]!.AsArray()[0]!.GetValue<string>())
            .ToList();

        Assert.Equal(["locator", "tray"], either);

        // And neither is in `required`, which is what would have contradicted the group.
        var required = step["required"]?.AsArray().Select(one => one!.GetValue<string>()).ToList() ?? [];
        Assert.DoesNotContain("locator", required);
        Assert.DoesNotContain("tray", required);
        Assert.Contains("act", required);
    }

    [Fact]
    public void The_format_a_reader_is_shown_says_which_two_are_the_alternatives()
    {
        // The prose half of the same list. "required" and "optional" are both false of one of these,
        // and printing either would mislead in the direction the reader cannot check.
        var said = string.Join("\n", ScenarioSchema.Render());

        Assert.Contains($"locator (one of the {ScenarioSchema.Subject})", said, StringComparison.Ordinal);
        Assert.Contains($"tray (one of the {ScenarioSchema.Subject})", said, StringComparison.Ordinal);
    }

    [Fact]
    public void A_case_runs_against_a_real_icon_this_run_placed()
    {
        if (!Winwright.Windowing.Desk.Read().CanObserve)
            return;

        // The engine half, and the reason it needs its own case: everything above is about what
        // loads. This drives `Suite.Launch` end to end against an icon this run really placed, so the
        // branch that reaches `NotificationArea.Find` instead of building a subject is exercised
        // rather than assumed — a path nothing runs is the unearned green one level up from a case.
        using var icon = BusyDesk.Built(() => TrayIconFixture.Add("winwright tray subject"));
        if (icon is null)
            return;

        using var register = new Winwright.Processes.ProcessRegister();
        var verdict = Suite.Launch(
            Read($$"""
                "tray": {{JsonSerializer.Serialize(icon.Tip)}}, "act": "read",
                "named": "the icon this run placed is showing"
                """),
            Selection.All,
            register,
            Project());

        var ran = Assert.Single(verdict.Ran);
        var result = Assert.Single(ran.Verdict.Results);

        // A desk that could not be searched is a hole and never a red about the application, which is
        // WW168's rule reaching a case for the first time.
        if (BusyDesk.Excused(result))
            return;

        Assert.Equal(Winwright.Verdicts.RunOutcome.Passed, verdict.Outcome);
        Assert.Contains(icon.Tip, result.Detail, StringComparison.Ordinal);

        // And the trace names the icon rather than an empty locator, which is what `Addressed` is for.
        Assert.Contains(
            ran.Trace,
            one => one.Locator is { } said && said.Contains("winwright tray subject", StringComparison.Ordinal));
    }

    [Fact]
    public void A_tray_step_leaves_the_overflow_the_way_it_found_it()
    {
        if (!Winwright.Windowing.Desk.Read().CanObserve)
            return;

        // WW258, and measured: `Find` opens the overflow to look in it and does not shut it, so the
        // first version of this step walked away leaving a flyout standing — and
        // `TrayPlacementTests.The_fixture_leaves_the_overflow_the_way_it_found_it` went red on it.
        // Read either side rather than asserting it is shut, because a flyout somebody else left
        // standing belongs to them and this step must not shut that one either.
        var before = NotificationArea.Overflow() is not null;

        using var register = new Winwright.Processes.ProcessRegister();
        Suite.Launch(
            Read("""
                "tray": "nothing is called this in any notification area", "act": "read"
                """),
            Selection.All,
            register,
            Project());

        Assert.Equal(before, NotificationArea.Overflow() is not null);
    }

    [Fact]
    public void A_case_naming_an_icon_nothing_placed_fails_rather_than_passing_quietly()
    {
        if (!Winwright.Windowing.Desk.Read().CanObserve)
            return;

        // The negative control, and the half that makes the case above mean something: a tray step
        // whose icon is genuinely absent has to go red. Without this, a step that always passed would
        // look exactly like one that found what it named.
        using var register = new Winwright.Processes.ProcessRegister();
        var verdict = Suite.Launch(
            Read("""
                "tray": "nothing is called this in any notification area", "act": "read"
                """),
            Selection.All,
            register,
            Project());

        var result = Assert.Single(Assert.Single(verdict.Ran).Verdict.Results);
        if (BusyDesk.Excused(result))
            return;

        Assert.Equal(Winwright.Verdicts.AssertionOutcome.Failed, result.Outcome);
    }

    /// <summary>A declaration naming the fixture, which a tray case needs only to be launched at all.</summary>
    private static Winwright.Projects.ProjectDeclaration Project()
    {
        var root = Directory.CreateTempSubdirectory("winwright-tray-").FullName;
        var declaration = Path.Combine(root, Winwright.Projects.ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "resolve": 1500, "act": 2000, "poll": 25, "window": 1500 }
            }
            """);

        return Winwright.Projects.ProjectDeclaration.Load(declaration);
    }

    /// <summary>Walk into the published schema by key, or null where the path is not there.</summary>
    private static JsonNode? Walk(JsonNode? from, params string[] keys)
    {
        var here = from;
        foreach (var key in keys)
            here = here?[key];

        return here;
    }
}

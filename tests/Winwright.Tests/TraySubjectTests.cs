using System.Text.Json;
using System.Text.Json.Nodes;

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
    public void A_tray_step_naming_an_act_that_presses_something_is_refused_with_the_one_it_takes()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(() => Read("""
            "tray": "winwright under test", "act": "invoke"
            """));

        Assert.Contains("'invoke'", refused.Because, StringComparison.Ordinal);
        Assert.Contains("'read'", refused.Because, StringComparison.Ordinal);
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

    /// <summary>Walk into the published schema by key, or null where the path is not there.</summary>
    private static JsonNode? Walk(JsonNode? from, params string[] keys)
    {
        var here = from;
        foreach (var key in keys)
            here = here?[key];

        return here;
    }
}

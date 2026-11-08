using Winwright.Acting;
using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW538. A click may name its button. Found by quickshell's QS310, where a middle click on a tab
/// closes it and the case could not be written, because the click verb always pressed the left one.
/// The gesture itself is driven in <see cref="DragTests"/>, on the fixture's strip; these are what
/// loads and what is refused.
/// </summary>
public sealed class ClickButtonTests
{
    [Fact]
    public void A_click_that_names_no_button_presses_the_left_one_as_every_click_did()
    {
        var step = Wrote.Step("TabItem#secondTab", "click", ("with", "PointerIsTheAct"));

        Assert.Equal(MouseButton.Left, step.Button);
        Assert.Equal("click TabItem#secondTab", step.Name);
    }

    [Theory]
    [InlineData("middle", MouseButton.Middle)]
    [InlineData("right", MouseButton.Right)]
    [InlineData("Middle", MouseButton.Middle)]
    public void A_click_names_the_button_it_presses_and_its_name_says_so(string written, MouseButton pressed)
    {
        var step = Wrote.Step("TabItem#secondTab", "click", ("with", "PointerIsTheAct"), ("button", written));

        Assert.Equal(pressed, step.Button);
        Assert.Equal($"{pressed.ToString().ToLowerInvariant()} click TabItem#secondTab", step.Name);
    }

    [Fact]
    public void A_button_that_is_not_one_is_refused_with_the_buttons_there_are()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => Wrote.Step("TabItem#secondTab", "click", ("with", "PointerIsTheAct"), ("button", "wheel")));

        Assert.Contains("'left', 'right', 'middle'", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_button_beside_any_act_but_a_click_is_refused()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => Wrote.Step("Button#save", "invoke", ("button", "middle")));

        Assert.Contains("no other act presses one", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_click_holds_no_key_unless_it_says_and_its_name_says_the_key_it_holds()
    {
        // WW540. The pointer's half of adding to a selection: Ctrl held across the click.
        Assert.Null(Wrote.Step("ListItem#row", "click", ("with", "PointerIsTheAct")).Holding);

        var step = Wrote.Step("ListItem#row", "click", ("with", "PointerIsTheAct"), ("holding", "ctrl"));

        Assert.Equal(HeldKey.Ctrl, step.Holding);
        Assert.Equal("ctrl click ListItem#row", step.Name);
    }

    [Fact]
    public void A_key_that_is_not_one_or_beside_another_act_is_refused()
    {
        Assert.Contains(
            "a click holds 'ctrl' or 'shift'",
            Assert.Throws<ScenarioRefusedException>(
                () => Wrote.Step("ListItem#row", "click", ("with", "PointerIsTheAct"), ("holding", "alt"))).Because,
            StringComparison.Ordinal);

        Assert.Contains(
            "no other act holds one",
            Assert.Throws<ScenarioRefusedException>(
                () => Wrote.Step("ListItem#row", "select", ("holding", "ctrl"))).Because,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_click_presses_once_unless_it_says_twice_and_its_name_says_double_click()
    {
        // WW602. wintimeline's WT56: a compound clip opens on a double click and on nothing else.
        Assert.Equal(1, Wrote.Step("ListItem#row", "click", ("with", "PointerIsTheAct")).Clicks);

        var step = Wrote.Step("ListItem#row", "click", ("with", "PointerIsTheAct"), ("clicks", 2));

        Assert.Equal(2, step.Clicks);
        Assert.Equal("double click ListItem#row", step.Name);
        Assert.Equal(1, Wrote.Step("ListItem#row", "click", ("with", "PointerIsTheAct"), ("clicks", 1)).Clicks);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(-1)]
    public void A_count_other_than_one_or_two_is_refused(int clicks)
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => Wrote.Step("ListItem#row", "click", ("with", "PointerIsTheAct"), ("clicks", clicks)));

        Assert.Contains("a click presses 1 time or 2", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_count_beside_any_act_but_a_click_is_refused()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => Wrote.Step("Button#save", "invoke", ("clicks", 2)));

        Assert.Contains("no other act presses", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void The_file_format_reads_clicks_as_a_number_and_refuses_it_written_as_text()
    {
        Assert.Equal(2, Cases("2").Steps[0].Clicks);

        var refused = Assert.Throws<ScenarioRefusedException>(() => Cases("\"2\""));
        Assert.Contains("has to be a whole number", refused.Because, StringComparison.Ordinal);

        Assert.Throws<ScenarioRefusedException>(() => Cases("1.5"));

        // And the schema an agent reads says so too.
        Assert.Equal(Taking.Whole, ScenarioSchema.Step.Single(field => field.Name == "clicks").Holds);
    }

    /// <summary>A file holding one case whose click writes <paramref name="clicks"/> as given.</summary>
    private static CaseDeclaration Cases(string clicks) => Assert.Single(ScenarioFile.Read("double.cases.json", $$"""
        {
          "cases": [
            {
              "name": "a row opens on a double click",
              "steps": [
                { "locator": "TabItem#secondTab", "act": "click", "with": "PointerIsTheAct", "clicks": {{clicks}} },
                { "locator": "Text#openedSaid", "act": "read", "reads": "name", "expect": "opened: Second" }
              ]
            }
          ]
        }
        """));

    [Fact]
    public void The_file_format_reads_the_button_and_publishes_the_ones_there_are()
    {
        var step = Assert.Single(Assert.Single(ScenarioFile.Read("click.cases.json", """
            {
              "cases": [
                {
                  "name": "a tab closes on the middle button",
                  "steps": [
                    { "locator": "TabItem#secondTab", "act": "click", "with": "PointerIsTheAct", "button": "middle" },
                    { "locator": "Text#dragOrder", "act": "read", "reads": "name", "expect": "First, Third" }
                  ]
                }
              ]
            }
            """)).Steps.Take(1));

        Assert.Equal(MouseButton.Middle, step.Button);
        Assert.Equal(StepDeclaration.Buttons, ScenarioSchema.Step.Single(field => field.Name == "button").OneOf);

        // The schema spells both lists as literals for the documentation area's reader, so this is
        // what keeps the literals and the step's own vocabularies from drifting apart.
        Assert.Equal(StepDeclaration.HeldKeys, ScenarioSchema.Step.Single(field => field.Name == "holding").OneOf);
    }
}

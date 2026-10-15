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
    }
}

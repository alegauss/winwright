using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW57. A step is fields, and every one of them is judged when it is declared. What is refused
/// here is refused on every machine — an unparseable locator, an act that does not exist, an
/// argument the verb cannot use — which is exactly what tells it apart from a red on one desk.
/// </summary>
public class StepDeclarationTests
{
    [Fact]
    public void A_step_is_a_locator_a_verb_and_what_the_control_should_read()
    {
        var step = StepDeclaration.Of("""Edit[name="Profile"]""", "set value", "beta", expected: "beta");

        Assert.Equal("beta", step.Argument);
        Assert.Equal("beta", step.Expected);
        Assert.Equal("set value", step.Verb.Name);
        Assert.Equal("anything", step.Reads.Name);
        Assert.True(step.Checkable);
    }

    [Fact]
    public void A_step_acting_on_nothing_is_refused()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => StepDeclaration.Of("  ", "invoke"));

        Assert.Contains("names nothing", refusal.Because);
    }

    [Fact]
    public void A_locator_that_does_not_parse_is_refused_at_declaration_and_not_at_run_time()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("""Button[name=""", "invoke"));

        Assert.Contains("does not parse", refusal.Because);
        Assert.Contains("invoke Button[name=", refusal.Subject);
    }

    [Fact]
    public void An_argument_the_verb_cannot_use_is_refused_and_the_step_is_named()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Button", "invoke", "beta", named: "press Save"));

        Assert.Equal("press Save", refusal.Subject);
        Assert.Contains("takes nothing", refusal.Because);
    }

    [Fact]
    public void An_argument_the_verb_needs_and_has_not_got_is_refused()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => StepDeclaration.Of("Slider", "set range"));

        Assert.Contains("acts on a number", refusal.Because);
    }

    [Fact]
    public void A_reading_that_does_not_exist_is_refused_with_the_ones_that_do()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("CheckBox", "toggle", expected: "On", reads: "checked"));

        Assert.Equal("checked", refusal.Subject);
        Assert.Contains("toggle", refusal.Because);
    }

    [Fact]
    public void A_reading_named_with_nothing_expected_of_it_is_refused_as_dead_configuration()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("CheckBox", "toggle", reads: "toggle"));

        Assert.Contains("expects nothing of it", refusal.Because);
    }

    [Fact]
    public void A_step_may_expect_nothing_at_all_and_then_it_is_an_act_and_not_a_check()
    {
        // Opening a node so a later step can read what it contains is a step. It is not a check,
        // and the run counts it as neither passed nor failed.
        var step = StepDeclaration.Of("TreeItem", "expand");

        Assert.Null(step.Expected);
        Assert.False(step.Checkable);
        Assert.False(step.Retryable);
    }

    [Fact]
    public void A_step_is_retryable_only_where_it_waits_for_something_and_the_verb_survives_repeating()
    {
        Assert.True(StepDeclaration.Of("Edit", "set value", "beta", expected: "beta").Retryable);

        // Waiting for a state a second toggle would leave is how a green becomes a red about the
        // opposite state, so the engine gets one attempt whatever the expectation said.
        Assert.False(StepDeclaration.Of("CheckBox", "toggle", expected: "On", reads: "toggle").Retryable);
    }

    [Fact]
    public void A_step_that_means_a_destructive_entry_says_so_in_a_field_a_reviewer_finds()
    {
        var quitting = StepDeclaration.Of(
            "Button[name=\"Quit\"]", "invoke", expected: "gone", reads: "value", meansIt: true, named: "quit the app");

        Assert.True(quitting.MeansIt);
        Assert.False(StepDeclaration.Of("Button", "invoke").MeansIt);
    }

    [Fact]
    public void A_step_nobody_named_is_named_by_what_it_does()
    {
        var step = StepDeclaration.Of("""CheckBox[name="Wrap lines"]""", "toggle", expected: "On", reads: "toggle");

        Assert.Equal("""toggle CheckBox[name="Wrap lines"]""", step.Name);
        Assert.Contains("→ toggle 'On'", step.ToString());
    }
}

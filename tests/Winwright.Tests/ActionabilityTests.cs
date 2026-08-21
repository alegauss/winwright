using System.Windows.Automation;

using Winwright.Locating;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW18. Present in the tree, not offscreen, enabled, and carrying the pattern the act needs. The
/// fourth is the one no browser has to check, and the refusal names which of the four was missing
/// because each has a different remedy.
/// </summary>
public class ActionabilityTests
{
    private static ElementFacts Button(
        bool offscreen = false, bool enabled = true, params string[] patterns) =>
        new("Save", "save", "Button", "Button", offscreen, enabled,
            new Winwright.Windowing.WindowBounds(120, 660, 210, 688),
            patterns.ToHashSet(StringComparer.Ordinal));

    [Fact]
    public void All_four_holding_is_the_only_reading_that_lets_an_act_run()
    {
        var check = ActionabilityCheck.Of(Button(patterns: "Invoke"), "Invoke");

        Assert.True(check.CanAct);
        Assert.Equal(Actionable.Yes, check.State);
        Assert.Empty(check.Missing);
        check.Require("#save");
    }

    [Fact]
    public void Nothing_in_the_tree_is_the_first_of_the_four()
    {
        var check = ActionabilityCheck.Of(null, "Invoke");

        Assert.Equal(Actionable.NotInTree, check.State);
        Assert.Equal("nothing matched, or what matched has gone since.", check.Sentence());
    }

    [Fact]
    public void Offscreen_names_the_remedy_that_is_its_own()
    {
        var check = ActionabilityCheck.Of(Button(offscreen: true, patterns: "Invoke"), "Invoke");

        Assert.Equal(Actionable.Offscreen, check.State);
        Assert.Contains("scroll it into view, or the window is minimised", check.Because);
    }

    [Fact]
    public void Disabled_names_a_different_remedy()
    {
        var check = ActionabilityCheck.Of(Button(enabled: false, patterns: "Invoke"), "Invoke");

        Assert.Equal(Actionable.Disabled, check.State);
        Assert.Contains("the application is not ready for this act yet", check.Because);
    }

    [Fact]
    public void The_missing_pattern_is_the_one_no_browser_has_to_check()
    {
        var check = ActionabilityCheck.Of(Button(false, true, "Value", "ScrollItem"), "Invoke");

        Assert.Equal(Actionable.PatternMissing, check.State);
        Assert.Contains("offers no Invoke pattern; it has ScrollItem, Value", check.Because);
    }

    [Fact]
    public void An_element_offering_nothing_at_all_says_so()
    {
        Assert.Contains("it has none at all", ActionabilityCheck.Of(Button(), "Invoke").Because);
    }

    [Fact]
    public void An_act_that_needs_no_pattern_is_never_refused_for_one()
    {
        Assert.True(ActionabilityCheck.Of(Button()).CanAct);
        Assert.True(ActionabilityCheck.Of(Button(), patternNeeded: null).CanAct);
    }

    [Fact]
    public void Everything_wrong_is_kept_and_the_first_leads()
    {
        var check = ActionabilityCheck.Of(Button(offscreen: true, enabled: false), "Invoke");

        Assert.Equal([Actionable.Offscreen, Actionable.Disabled, Actionable.PatternMissing], check.Missing);
        Assert.Equal(Actionable.Offscreen, check.State);
        Assert.Contains("(also disabled, missing the pattern)", check.Sentence());
    }

    [Fact]
    public void A_pattern_is_never_read_off_an_element_that_was_not_there()
    {
        var check = ActionabilityCheck.Of(null, "Invoke");

        Assert.Single(check.Missing);
        Assert.Equal(Actionable.NotInTree, check.Missing[0]);
    }

    [Fact]
    public void The_refusal_carries_the_locator_and_which_of_the_four_it_was()
    {
        var check = ActionabilityCheck.Of(Button(enabled: false, patterns: "Invoke"), "Invoke");

        var refusal = Assert.Throws<NotActionableException>(() => check.Require("Button#save"));

        Assert.Equal("Button#save", refusal.Locator);
        Assert.Equal(Actionable.Disabled, refusal.Missing);
        Assert.StartsWith("Button#save cannot take this act:", refusal.Message);
    }

    [Fact]
    public void An_element_that_has_gone_reads_as_not_in_the_tree_rather_than_throwing()
    {
        Assert.Null(ElementFacts.Of(null));
        Assert.Equal(Actionable.NotInTree, ActionabilityCheck.Of(ElementFacts.Of(null)).State);
    }

    [Fact]
    public void Facts_are_read_off_a_real_ui_automation_element_with_short_names()
    {
        var facts = ElementFacts.Of(AutomationElement.RootElement);

        Assert.NotNull(facts);
        Assert.Equal("Pane", facts.ControlType);
        Assert.DoesNotContain("ControlType.", facts.ControlType);
        Assert.All(facts.Patterns, pattern => Assert.DoesNotContain("PatternIdentifiers", pattern));
    }

    [Fact]
    public void A_real_elements_pattern_names_are_the_ones_the_locator_grammar_uses()
    {
        var desktop = AutomationElement.RootElement.FindFirst(
            TreeScope.Children, System.Windows.Automation.Condition.TrueCondition);

        var facts = ElementFacts.Of(desktop);

        Assert.NotNull(facts);
        Assert.All(facts.Patterns, pattern => Assert.True(
            UiaVocabulary.IsPattern(pattern), $"'{pattern}' is not a name the locator grammar accepts"));
    }
}

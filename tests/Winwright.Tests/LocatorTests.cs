using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Winwright.Locating;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW16. One grammar, parsed once. The same three automation conditions were being rebuilt at
/// every call site, in PowerShell in one project and in C# in another.
/// </summary>
public class LocatorTests
{
    private static LocatorStep Only(string text) => Assert.Single(Locator.Parse(text).Steps);

    [Fact]
    public void An_automation_id_stands_on_its_own()
    {
        var step = Only("#saveButton");

        Assert.Equal("saveButton", step.AutomationId);
        Assert.Empty(step.ControlTypes);
    }

    [Fact]
    public void A_control_type_stands_on_its_own()
    {
        Assert.Equal(["Button"], Only("Button").ControlTypes);
    }

    [Fact]
    public void A_step_carries_every_field_the_grammar_has()
    {
        var step = Only("""Button#save[name="Save as..."][class=Chrome_WidgetWin_1][pattern=Invoke][index=2]""");

        Assert.Equal(["Button"], step.ControlTypes);
        Assert.Equal("save", step.AutomationId);
        Assert.Equal("Save as...", step.Name);
        Assert.Equal("Chrome_WidgetWin_1", step.ClassName);
        Assert.Equal("Invoke", step.Pattern);
        Assert.Equal(2, step.Index);
    }

    [Fact]
    public void Chaining_is_descendant_at_any_depth()
    {
        var locator = Locator.Parse("Window#main > Pane > Button#save");

        Assert.Equal(3, locator.Steps.Count);
        Assert.Equal(["main", null, "save"], locator.Steps.Select(step => step.AutomationId));
    }

    [Fact]
    public void Whitespace_around_the_operator_is_optional()
    {
        Assert.Equal(
            Locator.Parse("Window#main>Pane").ToString(),
            Locator.Parse("  Window#main   >   Pane  ").ToString());
    }

    [Fact]
    public void A_bare_value_may_hold_spaces_and_a_quoted_one_may_hold_a_bracket()
    {
        Assert.Equal("Save as", Only("[name=Save as]").Name);
        Assert.Equal("""Save ["as"]""", Only("""[name="Save [\"as\"]"]""").Name);
    }

    [Fact]
    public void The_canonical_spelling_parses_back_to_the_same_locator()
    {
        var written = """Button#save[name="Save as..."][class=Chrome_WidgetWin_1][pattern=Invoke][index=2] > Text""";
        var once = Locator.Parse(written);
        var twice = Locator.Parse(once.ToString());

        Assert.Equal(once.ToString(), twice.ToString());
        Assert.Equal(once.Steps, twice.Steps);
    }

    [Fact]
    public void A_control_type_ui_automation_does_not_have_is_refused_with_the_nearest_words()
    {
        var refusal = Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("Buton#save"));

        Assert.Contains("'Buton' is no UI Automation control type", refusal.Because);
        Assert.Contains("Button", refusal.Because);
        Assert.Equal(0, refusal.Position);
    }

    [Fact]
    public void A_pattern_ui_automation_does_not_have_is_refused_with_the_nearest_words()
    {
        var refusal = Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("Button[pattern=Invoked]"));

        Assert.Contains("'Invoked' is no UI Automation pattern", refusal.Because);
        Assert.Contains("Invoke", refusal.Because);
    }

    [Fact]
    public void The_vocabulary_is_ui_automations_own_and_not_a_list_kept_here()
    {
        Assert.Contains("Button", UiaVocabulary.ControlTypes);
        Assert.Contains("SplitButton", UiaVocabulary.ControlTypes);
        Assert.Contains("Invoke", UiaVocabulary.Patterns);
        Assert.Contains("ExpandCollapse", UiaVocabulary.Patterns);
        Assert.DoesNotContain("Buton", UiaVocabulary.ControlTypes);
    }

    [Fact]
    public void An_index_counts_from_one_and_says_so()
    {
        var refusal = Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("Button[index=0]"));

        Assert.Contains("an index counts from one", refusal.Because);
    }

    [Fact]
    public void An_index_that_is_not_a_number_is_refused()
    {
        Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("Button[index=second]"));
    }

    [Fact]
    public void A_key_the_grammar_does_not_have_is_refused_with_the_ones_it_does()
    {
        var refusal = Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("Button[label=Save]"));

        Assert.Contains("the keys are name, nameStarts, class, pattern, order, index", refusal.Because);
    }

    [Fact]
    public void The_same_key_twice_in_one_step_is_two_claims_and_is_refused()
    {
        var refusal = Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("Button[name=Save][name=Cancel]"));

        Assert.Contains("claimed twice in one step", refusal.Because);
    }

    [Fact]
    public void A_step_that_constrains_nothing_is_refused()
    {
        var refusal = Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("Button > "));

        Assert.Contains("addresses everything", refusal.Because);
    }

    [Fact]
    public void An_empty_locator_is_refused()
    {
        Assert.Throws<LocatorSyntaxException>(() => Locator.Parse(""));
    }

    [Fact]
    public void A_predicate_that_is_not_closed_is_refused()
    {
        // WW196: the arm and not only the type. This case used to carry both spellings, and the
        // second is a different refusal — a quote that never closes, which the case below is about.
        Assert.Equal(
            LocatorFault.PredicateNotClosed,
            Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("Button[name=Save")).Arm);
    }

    [Fact]
    public void A_quoted_value_that_is_never_closed_is_refused_as_the_quote()
    {
        // WW196. The bracket inside a quote was never a bracket, so this is not an unclosed
        // predicate: the reader has to add a quote and not a bracket, and that is a different fix.
        var refusal = Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("""Button[name="Save]"""));

        Assert.Equal(LocatorFault.QuoteNotClosed, refusal.Arm);
        Assert.Contains("this quoted value is never closed", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_hash_with_no_id_after_it_is_refused()
    {
        // WW196. A '#' introducing nothing addresses every element rather than one, which is the
        // shape of locator that quietly matches whatever happens to be first.
        var refusal = Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("Button#"));

        Assert.Equal(LocatorFault.EmptyAutomationId, refusal.Arm);
        Assert.Contains("introduces an automation id and this one is empty", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_predicate_that_is_not_key_equals_value_is_refused()
    {
        // WW196. Distinct from a predicate nobody closed: this one is closed and says nothing, so
        // the refusal answers with the keys the grammar has rather than pointing at a bracket.
        var refusal = Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("Button[name]"));

        Assert.Equal(LocatorFault.PredicateMalformed, refusal.Arm);
        Assert.Contains("a predicate reads [key=value]", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void The_refusal_points_at_the_column_it_is_about()
    {
        var refusal = Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("Button#save ! Pane"));

        Assert.Equal(12, refusal.Position);
        Assert.Contains("^ expected '>' or the end", refusal.Message);
    }

    [Fact]
    public void Try_parse_answers_rather_than_throwing()
    {
        Assert.True(Locator.TryParse("Button#save", out var good, out var nothing));
        Assert.NotNull(good);
        Assert.Null(nothing);

        Assert.False(Locator.TryParse("Buton", out var bad, out var because));
        Assert.Null(bad);
        Assert.Contains("no UI Automation control type", because);
    }

    [Fact]
    public void The_signature_makes_the_promise_the_body_keeps()
    {
        // WW364. Both `out`s above are read without a bang, and that is the whole of what this
        // pins: the attributes are what make the throw above a use narrow the locator, and nothing
        // else notices them going. Four sites in StepDeclaration had spelt `parsed!` instead, and
        // WW351 added the fourth — a construction moved above the bang narrowing the rest of the
        // method, and the compiler asked for another bang rather than for the annotation.
        var outs = typeof(Locator)
            .GetMethod(nameof(Locator.TryParse))!
            .GetParameters()
            .Where(one => one.IsOut)
            .ToList();

        Assert.Equal(["locator", "because"], outs.Select(one => one.Name));
        Assert.Equal(
            [true, false],
            outs.Select(one => one.GetCustomAttribute<NotNullWhenAttribute>()?.ReturnValue));
    }

    [Fact]
    public void The_locator_keeps_the_text_it_was_written_as_for_the_trace()
    {
        Assert.Equal("  Button#save  ", Locator.Parse("  Button#save  ").Text);
    }
}

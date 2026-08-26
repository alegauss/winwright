using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW59's selecting half. A filter is the easy part; the reason this is a type is the refusal — a
/// selector matching nothing would otherwise give a run of no cases, and a run of no cases has no
/// failure and no hole in it, so it reads as a pass about nothing.
/// </summary>
public class SelectionTests
{
    private static CaseDeclaration Case(string name, params string[] tags) => CaseDeclaration.WithTags(
        name, tags, StepDeclaration.Of("Edit", "set value", "beta", expected: "beta", reads: "value"));

    private static IReadOnlyList<CaseDeclaration> Three() =>
        [Case("renaming a profile", "smoke"), Case("the tray menu opens", "tray", "smoke"), Case("the report renders")];

    [Fact]
    public void Nothing_asked_for_means_everything()
    {
        Assert.True(Selection.All.Unfiltered);
        Assert.Equal("everything", Selection.All.Asked);
        Assert.Equal(3, Selection.All.Over(Three()).Count);
    }

    [Fact]
    public void One_case_by_name_selects_that_case_and_leaves_the_rest_named()
    {
        var asked = Selection.Case("renaming a profile");
        var over = asked.Over(Three());

        Assert.Equal(["renaming a profile"], over.Select(one => one.Name));
        Assert.Null(asked.Leaves(over[0]));
        Assert.Equal("not selected by case 'renaming a profile'", asked.Leaves(Case("the report renders")));
    }

    [Fact]
    public void A_name_typed_in_another_case_still_selects_it()
    {
        // Typed at a command line from memory of a file. Refusing it for its first letter is a
        // refusal about the typist rather than about the suite.
        Assert.Single(Selection.Case("RENAMING A PROFILE").Over(Three()));
        Assert.Single(Selection.Case("  renaming a profile  ").Over(Three()));
    }

    [Fact]
    public void A_tag_selects_every_case_that_declares_it()
    {
        Assert.Equal(
            ["renaming a profile", "the tray menu opens"],
            Selection.Tag("smoke").Over(Three()).Select(one => one.Name));

        Assert.Equal(["the tray menu opens"], Selection.Tag("TRAY").Over(Three()).Select(one => one.Name));
    }

    [Fact]
    public void Cases_and_tags_together_select_the_union_of_both()
    {
        var asked = Selection.Of(["the report renders"], ["tray"]);

        Assert.Equal(
            ["the tray menu opens", "the report renders"],
            asked.Over(Three()).Select(one => one.Name));

        Assert.Contains("case 'the report renders' and tag tray", asked.Asked);
    }

    [Fact]
    public void A_name_that_selects_no_case_is_refused_with_the_names_there_are()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => Selection.Case("renaming a proflie").Over(Three()));

        Assert.Equal("renaming a proflie", refusal.Subject);
        Assert.Contains("no case is called that", refusal.Because);
        Assert.Contains("'the report renders'", refusal.Because);
    }

    [Fact]
    public void A_tag_that_selects_no_case_is_refused_with_the_tags_there_are()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => Selection.Tag("nightly").Over(Three()));

        Assert.Contains("no case is tagged that", refusal.Because);
        Assert.Contains("smoke", refusal.Because);
        Assert.Contains("tray", refusal.Because);
    }

    [Fact]
    public void A_blank_selector_says_nothing_and_is_refused_for_it()
    {
        Assert.Contains(
            "a blank case selects nothing",
            Assert.Throws<ScenarioRefusedException>(() => Selection.Case("  ")).Because);

        Assert.Contains(
            "a blank tag selects nothing",
            Assert.Throws<ScenarioRefusedException>(() => Selection.Tag("")).Because);
    }

    [Fact]
    public void The_same_selector_asked_for_twice_is_refused()
    {
        Assert.Contains(
            "that case is asked for twice",
            Assert.Throws<ScenarioRefusedException>(() => Selection.Of(["a", "A"])).Because);

        Assert.Contains(
            "that tag is asked for twice",
            Assert.Throws<ScenarioRefusedException>(() => Selection.Of(null, ["smoke", "smoke"])).Because);
    }

    [Fact]
    public void A_case_declares_what_selects_it_besides_its_name()
    {
        var one = Case("renaming a profile", "smoke", "profiles");

        Assert.Equal(["smoke", "profiles"], one.Tags);
        Assert.True(one.Tagged("SMOKE"));
        Assert.False(one.Tagged("tray"));
        Assert.Contains("[smoke profiles]", one.ToString());
    }

    [Fact]
    public void A_tag_declared_twice_or_blank_is_refused_when_the_case_is()
    {
        // The refusal names the second spelling and not the first: that is the one to delete, and a
        // refusal naming the copy the author means to keep sends them to the wrong line.
        Assert.Contains(
            "declares the tag 'SMOKE' twice",
            Assert.Throws<ScenarioRefusedException>(() => Case("a", "smoke", "SMOKE")).Because);

        Assert.Contains(
            "a blank tag selects nothing",
            Assert.Throws<ScenarioRefusedException>(() => Case("a", " ")).Because);
    }

    [Fact]
    public void A_case_declaring_no_tags_is_selected_by_its_name_alone()
    {
        var one = CaseDeclaration.Of("a", StepDeclaration.Of("Edit", "set value", "b", expected: "b"));

        Assert.Empty(one.Tags);
        Assert.DoesNotContain("[", one.ToString());
    }
}

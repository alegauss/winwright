using System.Reflection;

using Winwright.Asserting;
using Winwright.Projects;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW49. A hardcoded expected set silently stops covering what it was written for.
/// <para>
/// The first test is claude-tray's defect reproduced against both shapes at once: a window that
/// grew a fourth tab. Written by hand the expectation had three and reported all three read; here
/// the set comes from the strings, so the fourth is expected before anybody notices it exists and
/// its absence from the tree is a red.
/// </para>
/// </summary>
public sealed class DerivedSetTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-strings-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private string Strings(string name, string json)
    {
        var path = Path.Combine(root, name);
        File.WriteAllText(path, json);
        return path;
    }

    private string FourTabs() => Strings(
        "strings.en.json",
        """
        {
          "tabs": {
            "panes": "Panes",
            "status": "Status",
            "config": "Config",
            "logs": "Logs"
          },
          "buttons": { "close": "Close" }
        }
        """);

    [Fact]
    public void The_one_way_claim_holds_over_values_the_set_does_not_declare()
    {
        // WW275. The claim that had no way to be written: every declared string is read here, and a
        // stranger is allowed. Measured migrating WW84 — a sidebar whose items are the only elements
        // addressable by their words, so the locator has to be `Text`, and the panel beside it is
        // full of Texts that no locator separates from them.
        var set = DerivedSet.From("the sidebar panels", FourTabs(), "tabs");

        var compared = set.Against(
            ["Panes", "Status", "Config", "Logs", "Save", "Cancel", "Refresh interval"], exactly: false);

        Assert.True(compared.Held, compared.Sentence());
        Assert.Empty(compared.Missing);

        // Allowed is not the same as unrecorded: they are still counted and still said, on the pass.
        Assert.Equal(3, compared.Unexpected.Count);
        Assert.Contains("3 other value(s) were read here", compared.Sentence(), StringComparison.Ordinal);
        Assert.Contains("which this claim allows", compared.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void The_one_way_claim_still_fails_on_a_declared_string_nothing_read()
    {
        // The half it does not give up. Allowing strangers is not allowing absences, or the field
        // would be a way of asserting nothing at all.
        var set = DerivedSet.From("the sidebar panels", FourTabs(), "tabs");

        var compared = set.Against(["Panes", "Status", "Config", "Save"], exactly: false);

        Assert.False(compared.Held);
        Assert.Equal(["Logs"], compared.Missing);
    }

    [Fact]
    public void The_exact_claim_is_the_default_and_still_refuses_a_stranger()
    {
        // The control for the two above: nothing about `covers` moved, and a window carrying one more
        // tab than the expectation had heard of is still the defect it exists to catch.
        var set = DerivedSet.From("the tab headers", FourTabs(), "tabs");

        Assert.False(set.Against(["Panes", "Status", "Config", "Logs", "Debug"]).Held);
    }

    [Fact]
    public void The_sentence_agrees_with_itself_when_more_than_one_is_wrong()
    {
        // WW275. Both clauses carried a verb written for the singular, so the sentence read
        // "'a', 'b' were read and is declared nowhere" — and the missing half had the same fault
        // one clause over, which the task did not name.
        var set = DerivedSet.From("the tab headers", FourTabs(), "tabs");

        var compared = set.Against(["Panes", "Status", "Debug", "Trace"]);
        var said = compared.Sentence();

        Assert.Contains("are declared and were not read", said, StringComparison.Ordinal);
        Assert.Contains("were read and are declared nowhere", said, StringComparison.Ordinal);
        Assert.DoesNotContain("were read and is declared", said, StringComparison.Ordinal);
        Assert.DoesNotContain("are declared and was not", said, StringComparison.Ordinal);
    }

    [Fact]
    public void A_tab_the_strings_declare_and_the_window_does_not_show_is_a_red()
    {
        var set = DerivedSet.From("the tab headers", FourTabs(), "tabs");

        // The window as claude-tray's hand-written expectation knew it: three headers. The
        // fourth is in the strings, so it is expected here without anybody having edited a case.
        var compared = set.Against(["Panes", "Status", "Config"]);

        Assert.False(compared.Held);
        Assert.Equal(["Logs"], compared.Missing);
        Assert.Empty(compared.Unexpected);
        Assert.Contains("'Logs' (strings.en.json:6 'tabs.logs') is declared and was not read", compared.Sentence());

        // The sentence a hand-written set printed against the same window, and the one this may
        // never print while anything is missing.
        Assert.DoesNotContain("all 3", compared.Sentence());
    }

    [Fact]
    public void A_set_that_matches_says_how_many_and_where_it_came_from()
    {
        var set = DerivedSet.From("the tab headers", FourTabs(), "tabs");

        var compared = set.Against(["Panes", "Status", "Config", "Logs"]);

        Assert.True(compared.Held);
        Assert.Equal(4, compared.Matched.Count);
        Assert.Equal(
            "the tab headers: all 4 of 'Panes', 'Status', 'Config', 'Logs' were read, "
                + "derived from 'tabs' in strings.en.json.",
            compared.Sentence());
    }

    [Fact]
    public void Something_read_that_the_strings_declare_nowhere_is_reported_too()
    {
        var set = DerivedSet.From("the tab headers", FourTabs(), "tabs");

        var compared = set.Against(["Panes", "Status", "Config", "Logs", "Debug"]);

        Assert.False(compared.Held);
        Assert.Empty(compared.Missing);
        Assert.Equal(["Debug"], compared.Unexpected);
        Assert.Contains("'Debug' was read and is declared nowhere", compared.Sentence());
    }

    [Fact]
    public void Both_directions_are_reported_in_one_sentence()
    {
        var set = DerivedSet.From("the tab headers", FourTabs(), "tabs");

        var compared = set.Against(["Panes", "Status", "Debug"]);

        Assert.Equal(["Config", "Logs"], compared.Missing);
        Assert.Equal(["Debug"], compared.Unexpected);
        Assert.Contains(
            "'Config' (strings.en.json:5 'tabs.config'), 'Logs' (strings.en.json:6 'tabs.logs') are declared",
            compared.Sentence());
        Assert.Contains("'Debug' was read", compared.Sentence());
        Assert.Contains("2 of 4 matched", compared.Sentence());
    }

    [Fact]
    public void The_keys_travel_with_the_values_so_a_red_names_what_to_go_and_look_at()
    {
        var set = DerivedSet.From("the tab headers", FourTabs(), "tabs");

        Assert.Equal(["tabs.panes", "tabs.status", "tabs.config", "tabs.logs"], set.Keys);
        Assert.Equal(["Panes", "Status", "Config", "Logs"], set.Expected);
    }

    [Fact]
    public void A_flat_file_of_dotted_names_derives_the_same_set()
    {
        var flat = Strings(
            "flat.json",
            """
            { "tabs.panes": "Panes", "tabs.logs": "Logs", "buttons.close": "Close" }
            """);

        var set = DerivedSet.From("the tab headers", flat, "tabs");

        Assert.Equal(["Panes", "Logs"], set.Expected);
        Assert.Equal(["tabs.panes", "tabs.logs"], set.Keys);
    }

    [Fact]
    public void A_key_that_declares_nothing_is_refused_rather_than_deriving_an_empty_set()
    {
        // The whole point. An empty expected set is met by an empty window, so a derivation that
        // silently found nothing would reintroduce the defect it was built to close.
        var refused = Assert.Throws<UnderivableSetException>(
            () => DerivedSet.From("the tab headers", FourTabs(), "panels"));

        Assert.Contains("declares no strings", refused.Message);
        Assert.Contains("empty expected set is met by an empty window", refused.Message);
    }

    [Fact]
    public void A_file_that_is_not_there_or_not_readable_is_refused_and_named()
    {
        var missing = Assert.Throws<UnderivableSetException>(
            () => DerivedSet.From("the tab headers", Path.Combine(root, "nope.json"), "tabs"));
        Assert.Contains("nope.json", missing.Message);
        Assert.Contains("not there", missing.Message);

        var broken = Assert.Throws<UnderivableSetException>(
            () => DerivedSet.From("the tab headers", Strings("broken.json", "{ not json"), "tabs"));
        Assert.Contains("not readable JSON", broken.Message);
    }

    [Fact]
    public void A_project_declaring_one_language_file_derives_from_it_and_several_is_refused()
    {
        var one = Path.Combine(root, "one");
        Directory.CreateDirectory(one);
        File.Copy(FourTabs(), Path.Combine(one, "strings.en.json"));
        File.WriteAllText(
            Path.Combine(one, ProjectDeclaration.FileName), """{ "languageFiles": ["strings.en.json"] }""");

        var set = DerivedSet.From("the tab headers", ProjectDeclaration.Find(one), "tabs");
        Assert.Equal(4, set.Expected.Count);

        var two = Path.Combine(root, "two");
        Directory.CreateDirectory(two);
        File.Copy(FourTabs(), Path.Combine(two, "strings.en.json"));
        File.Copy(FourTabs(), Path.Combine(two, "strings.pt.json"));
        File.WriteAllText(
            Path.Combine(two, ProjectDeclaration.FileName),
            """{ "languageFiles": ["strings.en.json", "strings.pt.json"] }""");

        // Which one the application is showing is not answerable here, and picking the first
        // would derive an expectation in a language nobody is looking at.
        //
        // WW240 kept this and made it a smaller claim: it is now what happens when *nothing* said
        // which language the window is in. A fixture that says so gets its set derived from the file
        // for that language, which is FixtureLanguageTests.
        var refused = Assert.Throws<UnderivableSetException>(
            () => DerivedSet.From("the tab headers", ProjectDeclaration.Find(two), "tabs"));
        Assert.Contains("declares 2 language files", refused.Message);
        Assert.Contains("no fixture said which language", refused.Message);
    }

    [Fact]
    public void The_result_a_verdict_counts_carries_the_same_sentence()
    {
        var set = DerivedSet.From("the tab headers", FourTabs(), "tabs");

        var failed = set.Against(["Panes"]).AsAssertion();
        var passed = set.Against(["Panes", "Status", "Config", "Logs"]).AsAssertion();

        Assert.Equal(AssertionOutcome.Failed, failed.Outcome);
        Assert.Equal("the tab headers", failed.Name);
        Assert.Contains("'Status' (strings.en.json:4 'tabs.status')", failed.Detail);
        Assert.Contains("'Logs' (strings.en.json:6 'tabs.logs') are declared", failed.Detail);
        Assert.Equal(AssertionOutcome.Passed, passed.Outcome);
    }

    [Fact]
    public void There_is_no_way_to_derive_a_set_from_the_tree_it_is_asserting()
    {
        // Structural, not a convention. A set built from what was read agrees with whatever is
        // there and could never notice a header that had gone missing, so no door here takes
        // readings except the one that compares against them.
        var doors = typeof(DerivedSet)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == nameof(DerivedSet.From));

        Assert.NotEmpty(doors);
        Assert.All(doors, door => Assert.All(
            door.GetParameters(),
            parameter => Assert.False(
                parameter.ParameterType != typeof(string)
                    && typeof(System.Collections.IEnumerable).IsAssignableFrom(parameter.ParameterType),
                $"DerivedSet.From takes {parameter.ParameterType.Name}, which is a way to derive the set "
                    + "from the readings it is meant to be checking")));
    }
}

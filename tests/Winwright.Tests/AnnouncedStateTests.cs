using Winwright.Locating;
using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW83. What an entry that carries its own state needs, in three pieces that only work together.
/// <para>
/// The migration this is for drives claude-tray's Profile submenu. An entry there is named
/// <c>Pessoal — used 41%  · active now</c>: the label is somebody's account, the reading is what it
/// has consumed and the suffix comes and goes — so <c>name</c>, which matches by equality, addresses
/// no entry on any machine. And whether it carries the check mark is announced as a word in front of
/// a sentence, because the custom accessible object that carries the sentence at all costs the
/// framework's toggle pattern.
/// </para>
/// <para>
/// So: a locator that matches the front of a name, a reading of the sentence beside it, and a claim
/// about the front of that sentence. Each of the three is useless without the other two, which is
/// why they are one task and are tested here on one pane.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class AnnouncedStateTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-announced-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void A_name_that_ends_in_a_reading_is_addressed_by_the_label_it_begins_with()
    {
        var verdict = Run(
            """
            {
              "locator": "Button[nameStarts=\"Pessoal \"]",
              "act": "read",
              "reads": "description",
              "beginsWithLabel": "menu.itemChecked",
              "named": "the entry the icon follows carries the mark"
            }
            """);

        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void The_entry_that_does_not_carry_the_mark_fails_the_same_claim()
    {
        var verdict = Run(
            """
            {
              "locator": "Button[nameStarts=\"Work \"]",
              "act": "read",
              "reads": "description",
              "beginsWithLabel": "menu.itemChecked",
              "named": "the entry nothing follows carries the mark"
            }
            """);

        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));

        // The key and the string both, so a reader of the red can tell a wrong state from a right
        // state announced in the wrong language.
        Assert.Contains("menu.itemChecked", Said(verdict), StringComparison.Ordinal);
        Assert.Contains("Not checked", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_state_named_inside_the_sentence_rather_than_in_front_of_it_is_not_that_state()
    {
        // The whole reason the claim is a prefix. This row is off and its own explanation says what
        // being on would do, so a containment match reports a switch as on because it explained
        // itself.
        var verdict = Run(
            """
            {
              "locator": "Button[nameStarts=\"Follow \"]",
              "act": "read",
              "reads": "description",
              "beginsWithLabel": "menu.itemChecked",
              "named": "the switch whose explanation names the state it is not in"
            }
            """);

        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void An_element_that_says_nothing_beside_its_name_answers_nothing()
    {
        // Not "the empty string", which would satisfy a claim that the reading answered at all.
        var verdict = Run(
            """
            {
              "locator": "Button#silentRow",
              "act": "read",
              "reads": "description",
              "answers": true,
              "named": "the row that announces nothing"
            }
            """);

        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_prefix_of_nothing_is_a_prefix_of_every_name_there_is()
    {
        var refused = Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("Button[nameStarts=\"\"]"));
        Assert.Contains("every name begins with nothing", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_step_naming_both_ways_of_matching_one_string_is_refused()
    {
        var refused = Assert.Throws<LocatorSyntaxException>(
            () => Locator.Parse("Button[name=\"Pessoal\"][nameStarts=\"Pessoal\"]"));

        Assert.Contains("two claims about one string", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void The_grammar_writes_back_what_it_read()
    {
        var once = Locator.Parse("Button[nameStarts=\"Pessoal — \"]");
        var twice = Locator.Parse(once.ToString());

        Assert.Equal(once.ToString(), twice.ToString());
        Assert.Equal(once.Steps, twice.Steps);
        Assert.Equal("Pessoal — ", once.Steps[0].NameStarts);
    }

    [Fact]
    public void Reading_the_name_a_prefix_locator_half_chose_is_refused_like_reading_the_whole_one()
    {
        // WW238's rule, and the prefix is under it for the same reason: the locator fixed the front
        // of the answer, so a claim that it says something holds because the prefix is not empty.
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Button[nameStarts=\"Pessoal \"]", "read", reads: "name", answers: true));

        Assert.Contains("fixed before the act runs", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void Three_ways_of_claiming_one_declared_string_are_three_and_never_two()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Button", "read", label: "a.key", beginsWithLabel: "another.key"));

        Assert.Contains("one declared string claimed different ways", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_step_that_only_claims_the_front_of_a_reading_is_still_a_check()
    {
        Assert.True(StepDeclaration.Of("Button", "read", beginsWithLabel: "menu.itemChecked").Checkable);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>Run one step against the announcing pane, or null where this desk cannot observe.</summary>
    private SuiteVerdict? Run(string step)
    {
        if (!Desk.Read().CanObserve)
            return null;

        // The two states as strings the project declares, which is the whole point of the claim: the
        // word is the application's and a case that typed it would be wrong in every other language.
        File.WriteAllText(
            Path.Combine(root, "strings.en.json"),
            $$"""
            {
              "menu.itemChecked": {{System.Text.Json.JsonSerializer.Serialize(Fixture.AnnouncedChecked)}},
              "menu.itemUnchecked": {{System.Text.Json.JsonSerializer.Serialize(Fixture.AnnouncedUnchecked)}}
            }
            """);

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "languageFiles": ["strings.en.json"],
              "language": { "fallback": "en" },
              "timeouts": { "launch": 30000, "resolve": 4000, "act": 4000, "poll": 25 }
            }
            """);

        var cases = $$"""
            {
              "fixtures": [
                {
                  "name": "a window of rows that announce their state",
                  "arguments": ["--announces"],
                  "language": "en"
                }
              ],
              "cases": [
                {
                  "name": "an entry that carries its own state is read by the front of what it announces",
                  "catches": "a check that matched the state word anywhere in the sentence, which reports a switch as on because its explanation says what turning it on would do",
                  "fixture": "a window of rows that announce their state",
                  "steps": [
                    {{step}}
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(
            ScenarioFile.Read("announced.cases.json", cases),
            Selection.All,
            register,
            project);

        // Nothing left on the desk, whichever way the case went: this launches a real window per
        // fact, and a fixture left running is the next fact reading somebody else's window.
        register.StopAll();
        return verdict;
    }
}

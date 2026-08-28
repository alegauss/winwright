using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW58. roadkeep's first law, transferred: a field is judged where it is inserted, so a refusal
/// costs a retry and never a deletion. What is proved here is that the refusal arrives at the first
/// wrong field, that it says where in the file to go, and that a key nobody recognises is refused
/// rather than ignored — the last one being how a format stays a format instead of a convention.
/// </summary>
public class ScenarioFileTests
{
    private const string One = """
        {
          "cases": [
            {
              "name": "the field takes a name",
              "steps": [
                { "locator": "Edit", "act": "set value", "with": "beta", "expect": "beta", "reads": "value" }
              ]
            }
          ]
        }
        """;

    [Fact]
    public void A_file_of_cases_reads_as_the_cases_it_declares()
    {
        var cases = ScenarioFile.Read("one.cases.json", One);

        var only = Assert.Single(cases);
        Assert.Equal("the field takes a name", only.Name);

        var step = Assert.Single(only.Steps);
        Assert.Equal("set value", step.Verb.Name);
        Assert.Equal("beta", step.Argument);
        Assert.Equal("beta", step.Expected);
        Assert.Equal("value", step.Reads.Name);
    }

    [Fact]
    public void A_key_the_format_does_not_have_is_refused_with_the_keys_it_does()
    {
        // The one that matters. 'expects' beside 'expect' would load, run, check nothing and read
        // green — the unearned green arriving through a typo.
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            {
              "cases": [
                {
                  "name": "the field takes a name",
                  "steps": [ { "locator": "Edit", "act": "set value", "with": "beta", "expects": "beta" } ]
                }
              ]
            }
            """));

        Assert.Equal("one.cases.json cases[0].steps[0].expects", refusal.Subject);
        Assert.Contains("there is no such field", refusal.Because);
        Assert.Contains("expect", refusal.Because);
    }

    [Fact]
    public void A_refusal_says_which_case_and_which_step_to_go_to()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("two.cases.json", """
            {
              "cases": [
                { "name": "a", "steps": [ { "locator": "Edit", "act": "set value", "with": "b", "expect": "b" } ] },
                {
                  "name": "b",
                  "steps": [
                    { "locator": "Edit", "act": "set value", "with": "b", "expect": "b" },
                    { "locator": "Button", "act": "smash" }
                  ]
                }
              ]
            }
            """));

        Assert.StartsWith("two.cases.json cases[1].steps[1]", refusal.Subject);
        Assert.Contains("no such act", refusal.Because);

        // 'smash' and not 'click': WW225 made click a verb, and this case is about the address a
        // refusal carries rather than about which word happens to be outside the vocabulary today.
        Assert.Contains("smash", refusal.Subject);
    }

    [Fact]
    public void A_field_of_the_wrong_kind_names_that_field_and_says_what_it_had_to_be()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            { "cases": [ { "name": "a", "steps": [ { "locator": "Edit", "act": "set range", "with": 42 } ] } ] }
            """));

        Assert.Equal("one.cases.json cases[0].steps[0].with", refusal.Subject);
        Assert.Contains("it is a number and it has to be text", refusal.Because);
    }

    [Fact]
    public void A_flag_that_is_not_a_flag_is_refused_too()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            {
              "cases": [
                {
                  "name": "a",
                  "steps": [ { "locator": "Button", "act": "invoke", "meansIt": "yes" } ]
                }
              ]
            }
            """));

        Assert.Equal("one.cases.json cases[0].steps[0].meansIt", refusal.Subject);
        Assert.Contains("has to be true or false", refusal.Because);
    }

    [Fact]
    public void A_required_field_that_is_not_there_is_refused_where_it_would_have_been()
    {
        // WW258 moved the example off 'locator'. That field is no longer unconditionally required —
        // a step carries it or a 'tray', and a missing one of those is refused by the group with the
        // step's own address, because there is no single field to point at when either would do. The
        // rule this case is about is the other one, so it is asserted through a field that still has
        // to be there on every step.
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            { "cases": [ { "name": "a", "steps": [ { "locator": "Button" } ] } ] }
            """));

        Assert.Equal("one.cases.json cases[0].steps[0].act", refusal.Subject);
        Assert.Contains("it is not there, and it has to be", refusal.Because);
    }

    [Fact]
    public void A_declarations_own_refusal_keeps_its_sentence_and_gains_an_address()
    {
        // The judgements stay in StepDeclaration. What the loader adds is where in the file to go,
        // because a loader re-checking the fields would be a second set of rules that drifts.
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            { "cases": [ { "name": "a", "steps": [ { "locator": "Button", "act": "invoke", "with": "beta" } ] } ] }
            """));

        Assert.StartsWith("one.cases.json cases[0].steps[0] (", refusal.Subject);
        Assert.Contains("takes nothing", refusal.Because);
    }

    [Fact]
    public void A_case_that_could_only_read_green_is_refused_through_the_file_too()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            { "cases": [ { "name": "the tree opens", "steps": [ { "locator": "TreeItem", "act": "expand" } ] } ] }
            """));

        Assert.Contains("cases[0]", refusal.Subject);
        Assert.Contains("can only ever read green", refusal.Because);
    }

    [Fact]
    public void Two_cases_of_one_name_are_refused_because_a_name_has_to_select_one()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            {
              "cases": [
                { "name": "a", "steps": [ { "locator": "Edit", "act": "set value", "with": "b", "expect": "b" } ] },
                { "name": "A", "steps": [ { "locator": "Edit", "act": "set value", "with": "c", "expect": "c" } ] }
              ]
            }
            """));

        Assert.Contains("declared twice in this file", refusal.Because);
    }

    [Fact]
    public void A_file_that_is_not_JSON_is_refused_with_what_the_parser_said()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", "cases:"));

        Assert.Equal("one.cases.json", refusal.Subject);
        Assert.Contains("it is not JSON", refusal.Because);
    }

    [Fact]
    public void A_file_declaring_no_cases_at_all_is_refused()
    {
        Assert.Contains(
            "it declares no 'cases'",
            Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", "{ }")).Because);

        Assert.Contains(
            "there is nothing to run",
            Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """{ "cases": [] }""")).Because);
    }

    [Fact]
    public void A_file_that_is_not_there_is_refused_and_not_thrown_from_somewhere_else()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"winwright-absent-{Guid.NewGuid():N}.cases.json");

        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Load(missing));

        Assert.Contains("could not be read", refusal.Because);
    }

    [Fact]
    public void A_file_on_disk_reads_the_same_as_the_text_of_it()
    {
        var path = Path.Combine(
            Directory.CreateTempSubdirectory("winwright-scenario-").FullName, $"one{ScenarioFile.Extension}");

        try
        {
            File.WriteAllText(path, One);
            var loaded = ScenarioFile.Load(path);

            Assert.Equal(path, loaded.Path);
            Assert.Equal("the field takes a name", Assert.Single(loaded.Cases).Name);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public void The_format_is_readable_before_a_file_is_written()
    {
        // A format that lives only in the loader is a convention. This is what a tool or an agent
        // is told, and it is the same list the refusals are spelled from.
        var rendered = string.Join('\n', ScenarioSchema.Render());

        Assert.Contains("'cases': an array of cases", rendered);
        // WW258: 'locator' now carries which group it belongs to rather than nothing, because neither
        // "required" nor "optional" is true of it — a step needs it or a 'tray', and exactly one.
        Assert.Contains($"locator (one of the {ScenarioSchema.Subject}): what to act on", rendered);
        Assert.Contains("one of: read, invoke, toggle, set value, set range, select, expand, collapse", rendered);
        Assert.Contains("one of: anything, value, range, toggle, selected, picked, expanded, text", rendered);
        Assert.Contains("(optional)", rendered);
    }

    [Fact]
    public void Every_field_the_loader_reads_is_a_field_the_schema_declares()
    {
        // The two lists cannot drift because there is only one, and this is what says so.
        Assert.Equal(["cases", "fixtures"], ScenarioSchema.File.Select(field => field.Name));
        Assert.Equal(
            ["name", "steps", "tags", "needs", "catches", "filed", "fixture", "forEach", "onlyReads"],
            ScenarioSchema.Case.Select(field => field.Name));
        Assert.Equal(
            ["name", "environment", "flag", "arguments", "variables", "shareable", "language", "resident"],
            ScenarioSchema.Fixture.Select(field => field.Name));
        Assert.Equal(
            [
                "locator", "tray", "act", "with", "expect", "reads", "moves", "answers", "matches", "discloses",
                "sameAs", "unlike", "sameCountdownAs", "label", "notLabel", "ownHeader", "eachSpoken", "spoken", "never", "covers",
                "coversAtLeast", "coversWithin", "meansIt", "named",
            ],
            ScenarioSchema.Step.Select(field => field.Name));

        Assert.Equal(
            ActVerb.All.Select(verb => verb.Name),
            ScenarioSchema.Step.Single(field => field.Name == "act").OneOf);
        Assert.Equal(
            ReadBack.All.Select(one => one.Name),
            ScenarioSchema.Step.Single(field => field.Name == "reads").OneOf);
    }
}

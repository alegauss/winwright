using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW61 and WW63, the two fields a case carries that are not about driving anything.
/// <para>
/// <c>needs</c> is the third outcome applied to a whole case: pportal's interaction tests fail
/// rather than skip when no controller is plugged in, because xUnit gave them nowhere else to go.
/// <c>catches</c> is why the case survives: every case in these harnesses carries a sentence about
/// what went wrong without it, and a check nobody can justify is a check nobody dares delete and
/// nobody dares change.
/// </para>
/// </summary>
public class CaseCatchesTests
{
    private static StepDeclaration Step() =>
        Wrote.Step("Edit", "set value", ("with", "beta"), ("expect", "beta"), ("reads", "value"));

    [Fact]
    public void A_case_carries_the_defect_it_exists_to_catch_and_the_task_it_was_filed_under()
    {
        var one = CaseDeclaration.Declared(
            "renaming a profile writes it back",
            [Step()],
            catches: "a rename that updates the list and never the file",
            filed: "WW63");

        Assert.True(one.Justified);
        Assert.Equal("a rename that updates the list and never the file", one.Catches);
        Assert.Equal("WW63", one.Filed);
        Assert.Equal(
            "renaming a profile writes it back: a rename that updates the list and never the file [WW63]",
            one.Sentence());
    }

    [Fact]
    public void A_case_that_says_nothing_about_why_it_exists_says_so_rather_than_reading_justified()
    {
        // Optional on purpose. Asked for a sentence they do not have, an author writes one — and the
        // field stops meaning anything for every case that has a real one.
        var one = CaseDeclaration.Of("something", Step());

        Assert.False(one.Justified);
        Assert.Empty(one.Catches);
        Assert.Contains("nothing says what this catches", one.Sentence());
        Assert.Contains("what deleting it would cost", one.Sentence());
    }

    [Fact]
    public void A_justification_that_is_present_and_blank_is_refused_because_it_reads_as_answered()
    {
        Assert.Contains(
            "reads as answered",
            Assert.Throws<ScenarioRefusedException>(
                () => CaseDeclaration.Declared("a", [Step()], catches: "   ")).Because);

        Assert.Contains(
            "names the task it was filed under and then names none",
            Assert.Throws<ScenarioRefusedException>(
                () => CaseDeclaration.Declared("a", [Step()], filed: " ")).Because);
    }

    [Fact]
    public void A_claim_that_is_its_own_name_again_is_refused_however_it_is_cased_or_punctuated()
    {
        // WW537. Measured by a reader of the 374-to-352 article: a catches filled from the case name
        // makes the claim diff between runs match the name diff line for line.
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => CaseDeclaration.Declared(
                "renaming a profile writes it back", [Step()], catches: "  Renaming a profile — writes it back. "));

        Assert.Contains("which is its own name again", refusal.Because);
        Assert.Contains("say what goes wrong without it", refusal.Because);
    }

    [Theory]
    // WW541. The four shapes a dev.to reader derived from a case name that equality lets through.
    [InlineData("WW1042 login succeeds for a user")]
    [InlineData("Tests. Login succeeds for a user")]
    [InlineData("login succeeds for a user, regression")]
    [InlineData("Regression: login succeeds for a user")]
    public void A_claim_that_begins_or_ends_with_a_long_name_is_named_and_still_loads(string claim)
    {
        var one = CaseDeclaration.Declared("login succeeds for a user", [Step()], catches: claim);

        Assert.True(one.Justified);
        Assert.True(one.EchoesItsName);
        Assert.Equal([one], CaseDeclaration.Echoing([one]));
    }

    [Theory]
    // The honest half of his table: new content, and a name inside a sentence rather than at its edge.
    [InlineData("login succeeds for a user", "a cached session token that accepts a stale token")]
    [InlineData("login succeeds for a user", "a password whose login succeeds for a user only without its trailing space")]
    // Under the floor a name is a word or two, and a claim beginning with a word is ordinary prose.
    [InlineData("save", "save that reports success and writes nothing")]
    public void A_claim_with_its_own_content_or_a_short_name_at_its_edge_is_not_named(string name, string claim)
    {
        var one = CaseDeclaration.Declared(name, [Step()], catches: claim);

        Assert.False(one.EchoesItsName);
    }

    [Fact]
    public void A_case_that_says_nothing_does_not_echo_anything()
    {
        Assert.False(CaseDeclaration.Of("login succeeds for a user", Step()).EchoesItsName);
    }

    [Fact]
    public void A_claim_that_holds_its_name_inside_a_longer_sentence_is_taken()
    {
        // Equality and never containment: a short name stands inside honest claims, and turning one
        // away teaches people to leave the field out.
        var one = CaseDeclaration.Declared("save", [Step()], catches: "a save that reports success and writes nothing");

        Assert.True(one.Justified);
    }

    [Fact]
    public void A_claim_that_is_its_own_name_is_refused_at_the_case_in_the_file()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            {
              "cases": [
                {
                  "name": "the second profile renames",
                  "catches": "The second profile renames",
                  "steps": [ { "locator": "Edit", "act": "set value", "with": "b", "expect": "b" } ]
                }
              ]
            }
            """));

        Assert.StartsWith("one.cases.json cases[0]", refusal.Subject);
        Assert.Contains("its own name again", refusal.Because);
    }

    [Fact]
    public void What_is_unjustified_is_counted_over_a_set_rather_than_asked_case_by_case()
    {
        var declared = new[]
        {
            CaseDeclaration.Declared("a", [Step()], catches: "a defect"),
            CaseDeclaration.Of("b", Step()),
            CaseDeclaration.Of("c", Step()),
        };

        Assert.Equal(["b", "c"], CaseDeclaration.Unjustified(declared).Select(one => one.Name));
    }

    [Fact]
    public void A_case_declares_what_this_machine_has_to_have_before_it_can_observe_anything()
    {
        var one = CaseDeclaration.Declared("a", [Step()], needs: ["a second profile", "a free notification area"]);

        Assert.Equal(["a second profile", "a free notification area"], one.Needs);
        Assert.Contains("(needs a second profile, a free notification area)", one.ToString());
    }

    [Fact]
    public void A_requirement_declared_twice_or_blank_is_refused()
    {
        Assert.Contains(
            "declares the requirement 'A PAD' twice",
            Assert.Throws<ScenarioRefusedException>(
                () => CaseDeclaration.Declared("a", [Step()], needs: ["a pad", "A PAD"])).Because);

        Assert.Contains(
            "a blank requirement is nothing this machine could be asked for",
            Assert.Throws<ScenarioRefusedException>(
                () => CaseDeclaration.Declared("a", [Step()], needs: [" "])).Because);
    }

    [Fact]
    public void Both_fields_come_out_of_the_file_the_case_was_written_in()
    {
        var cases = ScenarioFile.Read("one.cases.json", """
            {
              "cases": [
                {
                  "name": "the second profile renames",
                  "needs": ["a second profile"],
                  "catches": "a rename that writes the first profile when the second is selected",
                  "filed": "WW63",
                  "tags": ["profiles"],
                  "steps": [ { "locator": "Edit", "act": "set value", "with": "b", "expect": "b" } ]
                }
              ]
            }
            """);

        var only = Assert.Single(cases);
        Assert.Equal(["a second profile"], only.Needs);
        Assert.Equal(["profiles"], only.Tags);
        Assert.Equal("WW63", only.Filed);
        Assert.True(only.Justified);
    }

    [Fact]
    public void A_requirement_of_the_wrong_kind_in_the_file_is_refused_at_its_own_address()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            {
              "cases": [
                {
                  "name": "a",
                  "needs": ["a second profile", 4],
                  "steps": [ { "locator": "Edit", "act": "set value", "with": "b", "expect": "b" } ]
                }
              ]
            }
            """));

        Assert.Equal("one.cases.json cases[0].needs[1]", refusal.Subject);
        Assert.Contains("it is a number and it has to be text", refusal.Because);
    }

    [Fact]
    public void A_precondition_set_answers_a_list_of_names_the_way_it_answers_an_assertion()
    {
        // One set of rules, so a case and an assertion cannot disagree about the same machine.
        var measured = PreconditionSet.Of(
            Winwright.Verdicts.Precondition.Met("a display that renders"),
            Winwright.Verdicts.Precondition.Absent("a second profile", "this checkout registers one"));

        Assert.Null(measured.FirstAbsent("a case", ["a display that renders"]));
        Assert.Equal("a second profile", measured.FirstAbsent("a case", ["a second profile"])!.Name);

        Assert.Contains(
            "which nothing measures",
            Assert.Throws<ScenarioRefusedException>(() => measured.FirstAbsent("a case", ["a pad"])).Because);
    }
}

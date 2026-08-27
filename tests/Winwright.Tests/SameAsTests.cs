using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW255. Measured migrating claude-tray's profiles case, which walks a picker 0 → 1 → 0 and asserts
/// that the third stop reads what the first one did. `moves` compares a reading against the same
/// reading a moment earlier, in the same step, across the same act — one shape of <em>changed</em>.
/// A round trip is the other: a value that changed and then came back.
/// <para>
/// It is the field report and not a nicety. The defect it was written for repainted the panes with
/// the profile being left behind, so coming back showed another account's figures while every
/// reading, taken on its own, looked perfectly healthy.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class SameAsTests : IDisposable
{
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint WsTabStop = 0x00010000;

    private readonly string root = Directory.CreateTempSubdirectory("winwright-sameas-").FullName;

    /// <summary>
    /// One edit box, which is all a round trip needs: a value can be written away from what it was
    /// and written back, and `set value` goes through the pattern so nothing here needs the desk.
    /// </summary>
    private readonly PumpedDialog dialog = PumpedDialog.Open(
        "winwright round trip",
        new PumpedDialog.ChildWindow("Edit", "alpha", WsChild | WsVisible | WsTabStop, 20, 20, 200, 24));

    public void Dispose()
    {
        dialog.Dispose();
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void A_reading_that_went_away_and_came_back_holds_against_the_step_it_came_back_to()
    {
        var verdict = Run("""
            {
              "cases": [
                {
                  "name": "the value survives the round trip",
                  "catches": "a value that comes back to something other than what it was, which every reading taken on its own looks healthy for",
                  "steps": [
                    {
                      "locator": "Edit", "act": "set value", "with": "alpha",
                      "expect": "alpha", "reads": "value", "named": "the first stop"
                    },
                    { "locator": "Edit", "act": "set value", "with": "bravo", "expect": "bravo", "reads": "value" },
                    {
                      "locator": "Edit", "act": "set value", "with": "alpha",
                      "reads": "value", "sameAs": "the first stop",
                      "named": "back where it started"
                    }
                  ]
                }
              ]
            }
            """);

        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
        Assert.Equal(3, verdict.Assertions);
    }

    [Fact]
    public void A_reading_that_came_back_somewhere_else_fails_and_the_sentence_names_where_it_was()
    {
        // The whole point of the claim, and the reason it takes no value: nothing in the file below
        // names 'alpha', and the failure has to name it anyway or a reader cannot tell a round trip
        // that landed wrong from one that never ran.
        var verdict = Run("""
            {
              "cases": [
                {
                  "name": "coming back to the wrong value is a failure",
                  "catches": "a round trip that reports success on whatever the last write happened to leave",
                  "steps": [
                    {
                      "locator": "Edit", "act": "set value", "with": "alpha",
                      "expect": "alpha", "reads": "value", "named": "the first stop"
                    },
                    { "locator": "Edit", "act": "set value", "with": "bravo", "expect": "bravo", "reads": "value" },
                    {
                      "locator": "Edit", "act": "set value", "with": "charlie",
                      "reads": "value", "sameAs": "the first stop",
                      "named": "back where it started"
                    }
                  ]
                }
              ]
            }
            """);

        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));

        var said = Said(verdict);
        Assert.Contains("the first stop", said, StringComparison.Ordinal);
        Assert.Contains("alpha", said, StringComparison.Ordinal);
        Assert.Contains("charlie", said, StringComparison.Ordinal);
    }

    [Fact]
    public void It_is_one_claim_like_every_other_one()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of(
                "Edit", "set value", argument: "alpha", expected: "alpha", reads: "value", sameAs: "the first stop"));

        Assert.Contains("also makes another claim", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_step_cannot_claim_it_is_back_to_itself()
    {
        // The easy typo rather than a hypothetical: a round trip's third stop and its first read the
        // same element under the same verb, so a case that left both unnamed would write this by
        // accident — and it holds whatever the window did.
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Edit", "read", reads: "value", named: "the stop", sameAs: "the stop"));

        Assert.Contains("back to itself", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_step_claiming_it_is_back_has_to_say_which_reading()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Edit", "read", named: "the second stop", sameAs: "the first stop"));

        Assert.Contains("does not say which reading", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void Pointing_at_nothing_is_refused_with_the_names_it_could_have_meant()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => CaseDeclaration.Of(
            "a case pointing at a step nobody wrote",
            StepDeclaration.Of("Edit", "set value", argument: "alpha", expected: "alpha", reads: "value", named: "the first stop"),
            StepDeclaration.Of("Edit", "read", reads: "value", sameAs: "the stop before", named: "back where it started")));

        Assert.Contains("no step before it is called that", refusal.Because, StringComparison.Ordinal);
        Assert.Contains("'the first stop'", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void Pointing_forward_is_the_same_nothing_as_pointing_at_a_name_nobody_wrote()
    {
        // A step further down the case is a reading that does not exist yet when this one runs.
        var refusal = Assert.Throws<ScenarioRefusedException>(() => CaseDeclaration.Of(
            "a case pointing at a step that has not run",
            StepDeclaration.Of("Edit", "read", reads: "value", sameAs: "the later stop", named: "the first stop"),
            StepDeclaration.Of("Edit", "set value", argument: "alpha", expected: "alpha", reads: "value", named: "the later stop")));

        Assert.Contains("no step before it is called that", refusal.Because, StringComparison.Ordinal);
        Assert.Contains("it is the first step", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void Two_steps_by_one_name_is_refused_rather_than_resolved_to_the_first()
    {
        // The shape this claim is most likely to meet. A round trip reads the same element with the
        // same verb at every stop, so the default name is the same at all of them — and a pointer
        // that quietly picked the first would be a case that reads correctly and means something else.
        var refusal = Assert.Throws<ScenarioRefusedException>(() => CaseDeclaration.Of(
            "a case with two stops by one name",
            StepDeclaration.Of("Edit", "read", reads: "value", answers: true),
            StepDeclaration.Of("Edit", "read", reads: "value", answers: true),
            StepDeclaration.Of("Edit", "read", reads: "value", sameAs: "read Edit", named: "back where it started")));

        Assert.Contains("2 steps before it are called that", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void Comparing_two_different_readings_is_refused_because_it_says_nothing()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => CaseDeclaration.Of(
            "a case comparing a value to a name",
            StepDeclaration.Of("Edit", "read", reads: "name", answers: true, named: "the first stop"),
            StepDeclaration.Of("Edit", "read", reads: "value", sameAs: "the first stop", named: "back where it started")));

        Assert.Contains("two different values", refusal.Because, StringComparison.Ordinal);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        '\n',
        verdict.Render().Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString()))));

    /// <summary>Run one scenario against the dialog, or null where this desk cannot observe.</summary>
    private SuiteVerdict? Run(string cases)
    {
        var desk = Desk.Read();
        if (!desk.CanObserve)
            return null;

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "resolve": 4000, "act": 4000, "poll": 25, "launch": 20000 }
            }
            """);

        var project = ProjectDeclaration.Load(declaration);
        var declared = ScenarioFile.Read("round-trip.cases.json", cases);

        dialog.BringToFront();
        return Suite.Run(declared, Selection.All, dialog.Root, project);
    }
}

using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW318. Some windows make their argument by what is <em>not</em> in them.
/// <para>
/// Found adopting this in an application whose window has no toolbar, no status bar and no sidebar
/// — and not hidden ones waiting to be switched on, but no elements at all. Through a case that
/// could not be said: a step reads a subject, a locator matching nothing has no subject, and
/// <c>"expect": "absent"</c> came back as <em>nothing answered to it in 109 polls</em>, which is
/// word for word what a genuinely broken read produces.
/// </para>
/// <para>
/// The whole difficulty is the unearned green on the other side. A claim of absence passes hardest
/// against a window that never opened, so the cases below drive both halves: the thing that is
/// really not there, the thing that is, and the region that is missing underneath.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class AbsentTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-absent-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void A_control_the_window_does_not_have_is_read_as_absent_rather_than_as_a_timeout()
    {
        var verdict = Run("Button#thereIsNoSuchButton");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_control_that_is_there_fails_the_claim_and_says_what_it_found()
    {
        // The pane's own title, which is in the tree on every run — so this is the arm that must
        // fail, and the sentence has to name what was found rather than say the claim did not hold.
        var verdict = Run("Text#namesTitle");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));

        // What was found, as the tree describes it — which for a control with an automation id is
        // that id and not its text. A failure saying only that the claim did not hold sends the
        // reader back to the window to find out what is there.
        Assert.Contains("namesTitle", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_region_that_is_not_there_answers_nothing_rather_than_passing()
    {
        // The unearned green this field is easiest to write. `Group#noSuchPane > Button#anything`
        // matches nothing because the pane is missing, and a step that could not tell that from an
        // absent button would pass hardest on a window that never rendered.
        var verdict = Run("Group#noSuchPane > Button#anything");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));
        Assert.Contains("no region to be absent from", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void Claiming_absence_and_anything_about_what_was_matched_is_two_things()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Button#gone", "read", absent: true, answers: true));

        Assert.Contains("no reading of an element that is not there", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void The_claim_the_old_chain_had_never_heard_of_is_refused_like_the_rest()
    {
        // WW378. The rule above used to be a seventeen-term chain over the parameters — one term per
        // claim the format had on the day it was written — and `contains` joined in WW326 and never
        // joined it. So a step claiming absence beside it fell through to the generic multi-claim
        // refusal and was told it made two claims, rather than that a claim about nothing is a claim
        // about something.
        //
        // It asks the claim set now, which a claim joins by being a field. What this case pins is
        // the one behaviour that changed: the sentence a reader is given for the pair.
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of(
                "Button#gone", "read", reads: "name", absent: true, contains: "the earlier stop"));

        Assert.Contains("no reading of an element that is not there", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void Naming_a_reading_of_something_that_is_not_there_is_refused()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Button#gone", "read", absent: true, reads: "name"));

        Assert.Contains("answers no reading", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void Acting_on_what_a_step_says_is_not_there_is_refused_where_it_is_written()
    {
        // It would fail on the very absence it asserts: an act resolves its subject first.
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Button#gone", "invoke", absent: true));

        Assert.Contains("the very absence it is asserting", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_step_that_only_claims_absence_is_still_a_check()
    {
        Assert.True(StepDeclaration.Of("Button#gone", "read", absent: true).Checkable);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>Claim one locator matches nothing, or null where this desk cannot observe.</summary>
    private SuiteVerdict? Run(string locator)
    {
        if (!Desk.Read().CanObserve)
            return null;

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "launch": 30000, "resolve": 1500, "act": 1500, "poll": 25 }
            }
            """);

        var cases = $$"""
            {
              "fixtures": [
                { "name": "a window with a names pane in it", "arguments": ["--names"] }
              ],
              "cases": [
                {
                  "name": "the window does not hold it",
                  "catches": "an expectation of absence reported as a timeout, which reads exactly like a broken read and is the pass",
                  "fixture": "a window with a names pane in it",
                  "steps": [
                    {
                      "locator": {{System.Text.Json.JsonSerializer.Serialize(locator)}},
                      "act": "read",
                      "absent": true,
                      "named": "the window does not hold this"
                    }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(
            ScenarioFile.Read("absent.cases.json", cases), Selection.All, register, project);

        register.StopAll();
        return verdict;
    }
}

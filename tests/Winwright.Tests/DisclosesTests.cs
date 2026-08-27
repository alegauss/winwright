using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW251. A disclosure is not one reading moving.
/// <para>
/// Measured migrating claude-tray's sessions case: clicking a conversation row unfolds the call tree
/// that produced it, and what says so is more elements under the row than there were. <c>moves</c> is
/// one reading of one element and <c>covers</c> is a derived set — neither says <em>there is more here
/// than there was</em>, which is what a tree view, an expander, a details pane and a search that
/// fills a list all are.
/// </para>
/// <para>
/// Driven through a tab and not through an expander, because the fixture has no expander and the tab
/// is the same fact: WPF builds a tab's content on its first visit, which is the sentence claude-tray's
/// own harness wrote about the pane this field was measured on.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class DisclosesTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-discloses-").FullName;
    private readonly Settling settling = Attachable.Settling();

    public void Dispose()
    {
        settling.Dispose();
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void A_step_can_claim_the_tree_under_it_grew()
    {
        var step = StepDeclaration.Of("TabItem#statusPane", "select", discloses: true);

        Assert.True(step.Discloses);
        Assert.True(step.Checkable);
        Assert.Null(step.Expected);
    }

    [Fact]
    public void A_reading_verb_cannot_disclose_anything()
    {
        // Nothing acted, so the claim would be that the window changed while nobody touched it: a race
        // or a lie, and green either way.
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("TabItem", "read", discloses: true));

        Assert.Contains("only reads", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_reading_beside_it_is_refused_because_the_subject_is_the_subtree()
    {
        // It would look like it narrowed the claim and would narrow nothing.
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("TabItem", "select", reads: "selected", discloses: true));

        Assert.Contains("not about what it says", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void One_claim_per_step_holds_for_this_one_too()
    {
        foreach (var both in new Action[]
        {
            () => StepDeclaration.Of("TabItem", "select", expected: "selected", reads: "selected", discloses: true),
            () => StepDeclaration.Of("TabItem", "select", moves: true, discloses: true),
            () => StepDeclaration.Of("TabItem", "select", answers: true, discloses: true),
            () => StepDeclaration.Of("TabItem", "select", covers: "stats.tab", discloses: true),
            () => StepDeclaration.Of("TabItem", "select", matches: "x", discloses: true),
        })
        {
            var refused = Assert.Throws<ScenarioRefusedException>(both);
            Assert.Contains("another claim", refused.Because, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Visiting_a_pane_for_the_first_time_discloses_its_contents()
    {
        if (!Desk.Read().CanObserve)
            return;

        var held = Run(
            """{ "locator": "TabItem#statusPane", "act": "select", "discloses": true, "named": "visiting the pane puts its rows in the tree" }""");

        Assert.True(held!.Outcome == RunOutcome.Passed, Said(held));
    }

    [Fact]
    public void An_act_that_puts_nothing_in_the_tree_does_not_hold()
    {
        if (!Desk.Read().CanObserve)
            return;

        // The assertion that says the claim is a real one. Pressing a button is an act, it lands, and
        // nothing arrives under the button — so a field that held here would be a synonym for
        // 'something happened' and would read as covered on every act in every case.
        var nothing = Run(
            """{ "locator": "Button#save", "act": "invoke", "discloses": true, "named": "pressing save discloses nothing" }""");

        Assert.NotEqual(RunOutcome.Passed, nothing!.Outcome);
        Assert.Contains("more than the", Said(nothing), StringComparison.Ordinal);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    private SuiteVerdict? Run(string step)
    {
        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "resolve": 4000, "act": 4000, "poll": 25 }
            }
            """);

        var cases = $$"""
            {
              "cases": [
                {
                  "name": "the pane discloses its rows",
                  "catches": "a pane that opens and puts nothing in the accessibility tree, which a screen reader reads as an empty page",
                  "steps": [ {{step}} ]
                }
              ]
            }
            """;

        return Suite.Launch(
            ScenarioFile.Read("discloses.cases.json", cases),
            Selection.All,
            settling.Register,
            ProjectDeclaration.Load(declaration));
    }
}

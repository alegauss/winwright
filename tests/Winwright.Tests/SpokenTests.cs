using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW253. `discloses` says there is more under the locator than there was. It does not say that what
/// is under it <em>reads</em>, and that is the claim claude-tray's script made about a conversation
/// row before it ever clicked one: what a screen reader gets from a row is text or it is a picture,
/// and no capture can tell the two apart.
/// <para>
/// The script asserted four or more named descendants, which is the stale literal a derived set
/// exists to refuse — the row grows a column and the case goes on asserting four. So the claim here
/// is two count-free halves, and this drives both ends of each.
/// </para>
/// <para>
/// The names pane is the subject because it carries the whole rule at once: a control with no name, a
/// glyph, an id handed back, a label beside a box, and a button that keeps its text.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class SpokenTests : IDisposable
{
    private readonly Settling settling = Attachable.Settling();
    private readonly AutomationElement fixtureRoot;
    private readonly string root = Directory.CreateTempSubdirectory("winwright-spoken-").FullName;

    public SpokenTests()
    {
        // One pane and not two, which is measured rather than preferred: WPF builds a tab's content
        // on its first visit, so adding a second pane makes it the selected one and leaves this one's
        // controls in no tree at all. Everything below is inside the names pane.
        var launched = settling.Register.Launch(Fixture.Started("--names"));
        var drawn = Attempt.UntilTrue(() => TopLevelWindows.Largest(launched.Pid) is not null, 20000, 25);

        Assert.True(drawn.Happened, $"the fixture drew no window in {drawn.WaitedMs}ms");
        fixtureRoot = AutomationElement.FromHandle(TopLevelWindows.Largest(launched.Pid)!.Handle);
    }

    public void Dispose()
    {
        settling.Dispose();
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void A_subtree_whose_every_speaking_element_says_a_name_holds()
    {
        // The button that keeps its own text. Measured: one descendant, a Text announcing "Save
        // changes" — so everything under it that announces anything announces a label.
        //
        // A Button and not the row above it, which is measured too: a WPF StackPanel has no
        // automation peer, so `#labelledRow` is in no tree and a locator naming it matches nothing.
        // That is its own failure rather than this one — see below.
        var verdict = Run("Button#spoken");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
        Assert.Contains("announce a name", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_glyph_under_it_fails_and_the_sentence_says_it_is_a_glyph()
    {
        // The whole pane, measured as eleven descendants carrying both halves of the defect: a button
        // whose name is one Segoe MDL2 codepoint, and one that hands back its own automation id.
        // Non-empty and silent: each satisfies every check for a name and a screen reader reads
        // nothing, which is what WW175 measured.
        var verdict = Run("#namesPane");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));

        var said = Said(verdict);
        Assert.True(said.Contains("is not a name", StringComparison.Ordinal), said);
        Assert.True(
            said.Contains("font glyph", StringComparison.Ordinal)
                || said.Contains("automation id handed back", StringComparison.Ordinal),
            said);
    }

    [Fact]
    public void A_subtree_where_nothing_speaks_fails_and_says_how_many_it_looked_at()
    {
        // The other end, and the one the script was actually written for: a subtree that is all
        // picture. Measured — the unnamed button draws nothing and has no descendants at all, which
        // is the same answer as a row whose every field is an icon.
        var verdict = Run("Button#unnamed");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));
        Assert.Contains("not one of them says anything", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_locator_that_matched_nothing_fails_about_the_locator_and_not_about_the_names()
    {
        // Found by this class going red on its own first run, against `#labelledRow` — a WPF
        // StackPanel, which has no automation peer and is therefore in no tree. The claim came back
        // as "nothing under it announces a name: 0 element(s)", which is also true of a window that
        // never drew the thing at all, and sends a reader looking at names for a missing element.
        var verdict = Run("#nothingDrewThis");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));
        Assert.Contains("never arrived", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_sweep_over_elements_fails_where_any_one_of_them_is_not_named()
    {
        // WW262, and the other axis: `spoken` is about what sits under one element, this is about
        // every element a locator matches. Measured — the pane's buttons are the whole naming rule at
        // once, so the sweep meets a glyph, an id handed back and one with no name at all.
        var verdict = Run("Button", each: true);
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));

        var said = Said(verdict);
        Assert.True(said.Contains("is not a name", StringComparison.Ordinal), said);

        // "N of M", because a sweep that reported only whether would be the count this whole engine
        // refuses — and M above one is what says it swept many rather than resolved one.
        Assert.True(said.Contains(" of the ", StringComparison.Ordinal), said);
    }

    [Fact]
    public void A_sweep_over_elements_that_are_all_named_holds()
    {
        var verdict = Run("Button#spoken", each: true);
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
        Assert.Contains("announce a name", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_sweep_that_matched_nothing_is_counted_as_a_hole_rather_than_held_or_failed()
    {
        // WW272. A pass would be the check nobody ran reported as one that passed. A red would be a
        // lie about the application: this sweeps the window rather than a declared set, and a page
        // with no rows the rule applies to is a page behaving as designed — claude-tray's About panel
        // holds prose and links and not one settings row.
        var verdict = Run("Button#nothingDrewThis", each: true);
        if (verdict is null)
            return;

        Assert.Equal(RunOutcome.Degraded, verdict.Outcome);

        // Naming the locator, because a hole nobody can go and look at is a hole nobody can close.
        var said = Said(verdict);
        Assert.True(said.Contains("Button#nothingDrewThis", StringComparison.Ordinal), said);
        Assert.True(said.Contains("swept nothing at all", StringComparison.Ordinal), said);
    }

    [Fact]
    public void A_sweep_over_elements_is_a_read_and_one_claim_like_every_other()
    {
        // The same two rules `covers` is under: one act over many elements is not a claim about any
        // of them, and a step answers one thing.
        Assert.Contains(
            "not a claim",
            Assert.Throws<ScenarioRefusedException>(
                () => StepDeclaration.Of("Button", "invoke", eachSpoken: true)).Because,
            StringComparison.Ordinal);

        Assert.Contains(
            "also makes another claim",
            Assert.Throws<ScenarioRefusedException>(
                () => StepDeclaration.Of("Button", "read", answers: true, eachSpoken: true)).Because,
            StringComparison.Ordinal);

        Assert.Contains(
            "which is their name",
            Assert.Throws<ScenarioRefusedException>(
                () => StepDeclaration.Of("Button", "read", reads: "value", eachSpoken: true)).Because,
            StringComparison.Ordinal);
    }

    [Fact]
    public void It_is_one_claim_like_every_other_one()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("#labelledRow", "read", answers: true, spoken: true));

        Assert.Contains("also makes another claim", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void Naming_a_reading_beside_it_narrows_nothing_and_is_refused()
    {
        // The subject is the subtree, so a reading here would look like it narrowed the claim and
        // would narrow nothing: what those elements announce is their name, always.
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("#labelledRow", "read", reads: "value", spoken: true));

        Assert.Contains("which is their name", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_step_that_only_claims_this_is_still_a_check()
    {
        var step = StepDeclaration.Of("#labelledRow", "read", spoken: true);

        Assert.True(step.Checkable);
        Assert.True(step.Spoken);
        Assert.Equal(1, CaseDeclaration.Of("a case that only listens", step).Checks);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>Claim that subtree is spoken, or null where this desk cannot observe.</summary>
    private SuiteVerdict? Run(string locator, bool each = false)
    {
        if (!Desk.Read().CanObserve)
            return null;

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "resolve": 600, "act": 4000, "poll": 25 }
            }
            """);

        var cases = $$"""
            {
              "cases": [
                {
                  "name": "what is under it reads as text rather than as a picture",
                  "catches": "a row a screen reader gets nothing from, which every capture of it looks perfect in",
                  "steps": [
                    {
                      "locator": {{System.Text.Json.JsonSerializer.Serialize(locator)}},
                      "act": "read",
                      {{(each ? "\"eachSpoken\": true" : "\"spoken\": true")}},
                      "named": "everything under it that speaks says a name"
                    }
                  ]
                }
              ]
            }
            """;

        return Suite.Run(
            ScenarioFile.Read("spoken.cases.json", cases),
            Selection.All,
            fixtureRoot,
            ProjectDeclaration.Load(declaration));
    }
}

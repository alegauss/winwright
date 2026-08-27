using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW274. A locator step named one control type, and a rule under test governs a family of controls as
/// often as it governs one.
/// <para>
/// Measured migrating `WW84`. claude-tray's row rule names every control with no content of its own to
/// derive a name from — a ComboBox, a Slider, a TextBox, and a switch — and excludes everything else by
/// <em>what it is</em> rather than by a list of ids, because a Slider's `DecreaseLarge` and a
/// ScrollBar's `PageUp` are plain unnamed Buttons the rule must leave alone. Written as one step per
/// type, most of the steps match nothing on any given panel, `WW272` makes each of those a hole, and
/// the run is a page of holes nobody reads.
/// </para>
/// <para>
/// The union is one search rather than several stitched together: UI Automation takes the tree walk
/// either way, so what it costs is an Or in the condition.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class TypeUnionTests : IDisposable
{
    private readonly Settling settling = Attachable.Settling();
    private readonly AutomationElement fixtureRoot;
    private readonly string root = Directory.CreateTempSubdirectory("winwright-union-").FullName;

    public TypeUnionTests()
    {
        var launched = settling.Register.Launch(Fixture.Started("--rows=paired"));
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
    public void A_step_names_several_control_types_and_spells_them_back()
    {
        var step = Assert.Single(Locator.Parse("ComboBox|Slider|Edit").Steps);

        Assert.Equal(["ComboBox", "Slider", "Edit"], step.ControlTypes);

        // Round-trips, which is the whole claim `inspect` makes about the lines it prints: one that
        // cannot be copied back into a scenario is worse than no line.
        Assert.Equal("ComboBox|Slider|Edit", step.ToString());
        Assert.Equal(["ComboBox", "Slider", "Edit"], Locator.Parse(step.ToString()).Steps[0].ControlTypes);
    }

    [Fact]
    public void A_union_carries_the_predicates_the_grammar_already_had()
    {
        var step = Assert.Single(Locator.Parse("ComboBox|Slider[name=\"Refresh interval\"][order=top]").Steps);

        Assert.Equal(["ComboBox", "Slider"], step.ControlTypes);
        Assert.Equal("Refresh interval", step.Name);
        Assert.Equal(MatchOrder.Top, step.Order);
    }

    [Fact]
    public void A_word_in_a_union_that_is_no_control_type_is_refused_like_any_other()
    {
        var refused = Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("ComboBox|Slidr|Edit"));

        Assert.Equal(LocatorFault.UnknownControlType, refused.Arm);
        Assert.Contains("Slider", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_type_named_twice_in_one_step_is_refused()
    {
        // Not a wider set — a step written twice, and the reader of one is looking for the difference
        // between the two halves that is not there. The same rule a predicate claimed twice is under.
        var refused = Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("ComboBox|Slider|ComboBox"));

        Assert.Equal(LocatorFault.KeyClaimedTwice, refused.Arm);
        Assert.Contains("ComboBox", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_union_with_nothing_after_the_bar_is_refused()
    {
        Assert.Equal(
            LocatorFault.UnknownControlType,
            Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("ComboBox|")).Arm);
    }

    [Fact]
    public void A_sweep_over_a_union_reaches_every_type_in_it_and_no_others()
    {
        // The rows pane carries one of each: a ComboBox announcing its row, a TextBox announcing its
        // row, a Slider announcing its row — and a CheckBox that announces nothing at all, which is
        // the control the rule must leave alone and the one this proves is left alone.
        var verdict = Run("ComboBox|Slider|Edit");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
        Assert.Contains("all 3 element(s)", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void Adding_the_silent_control_to_the_union_is_what_makes_it_red()
    {
        // The other end of the same measurement, so the pass above is a reading and not a coincidence:
        // the union really does decide what is swept, and one more type is one more element.
        var verdict = Run("ComboBox|Slider|Edit|CheckBox");
        if (verdict is null)
            return;

        Assert.Equal(RunOutcome.Failed, verdict.Outcome);
        Assert.Contains("of the 4 element(s)", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_sweep_looks_only_under_what_the_steps_before_its_last_one_name()
    {
        // WW277. The last step is the one the matches are of, and it was being matched against the
        // whole window — so every step before it was decoration, and a case scoping a sweep to one row
        // got a sweep of the page with the row's name written beside it.
        //
        // The language row holds one control. The window holds four, one of which announces nothing.
        var verdict = Run("Group[name=\"Language\"] > ComboBox|Slider|Edit|CheckBox");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
        Assert.Contains("all 1 element(s)", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_route_that_matches_several_is_swept_under_every_one_of_them()
    {
        // The difference between a route and a sweep. Resolving refuses a step that matches several
        // and says nothing about which, because an act would one day land on the other one and be
        // green — and a sweep means the other one too. Demanding this case say *which row* would be
        // demanding it list the rows, which is what deriving the set exists to refuse.
        var verdict = Run("Group > ComboBox|Slider|Edit");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
        Assert.Contains("all 3 element(s)", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_route_that_resolves_to_nothing_sweeps_nothing_rather_than_the_window()
    {
        // The other half, and it is what a bug here would look like from a green: a scope naming a row
        // this page does not draw must not quietly widen to the page.
        var verdict = Run("Group[name=\"Nothing drew this\"] > ComboBox|Slider|Edit");
        if (verdict is null)
            return;

        Assert.Equal(RunOutcome.Degraded, verdict.Outcome);
        Assert.Contains("swept nothing at all", Said(verdict), StringComparison.Ordinal);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>Sweep one locator over the fixture's rows, or null where the desk cannot observe.</summary>
    /// <param name="locator">What to sweep.</param>
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
              "timeouts": { "resolve": 400, "act": 4000, "poll": 25 }
            }
            """);

        var cases = $$"""
            {
              "cases": [
                {
                  "name": "every control the naming rule is responsible for announces a label",
                  "catches": "a contentless control on a page nobody thought to name, which a check written against the three somebody knew about never asks",
                  "steps": [
                    {
                      "locator": {{System.Text.Json.JsonSerializer.Serialize(locator)}},
                      "act": "read",
                      "eachSpoken": true,
                      "named": "every control the rule is responsible for announces a label"
                    }
                  ]
                }
              ]
            }
            """;

        return Suite.Run(
            ScenarioFile.Read("union.cases.json", cases),
            Selection.All,
            fixtureRoot,
            ProjectDeclaration.Load(declaration));
    }
}

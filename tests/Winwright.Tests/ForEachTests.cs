using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW263. Steps are an array and the array is written out, which is right for every case that knows
/// what it drives. claude-tray's names case does not: the panels it visits come from the strings the
/// application ships, so the number of steps is data the file must not carry.
/// <para>
/// Listing them is the defect the derivation exists to refuse — a panel added later is swept by
/// nothing, and the run reports a clean pass over the panels somebody remembered. That is `WW49` one
/// level up: a hardcoded set that silently stops covering what it was written for.
/// </para>
/// <para>
/// The two guards the script wrote by hand are the engine's here. An empty derivation is refused
/// rather than run, because zero members makes every assertion inside run zero times and report
/// nothing at all. And what was visited is counted apart from what was asserted, because a member
/// that was reached and had nothing to check is not one that got away.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ForEachTests : IDisposable
{
    private readonly Settling settling = Attachable.Settling();
    private readonly AutomationElement fixtureRoot;
    private readonly string root = Directory.CreateTempSubdirectory("winwright-foreach-").FullName;

    public ForEachTests()
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
    public void A_case_runs_its_steps_once_for_each_string_the_key_declares()
    {
        // Four rows named by the strings file and never by the case. Adding a fifth string is a fifth
        // run with no edit here, which is the whole of what this is for.
        var verdict = Run("rows.headers");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));

        // WW276. One claim and not four: the case asserts one rule over four rows, and a summary
        // naming four results for one rule says the rule was checked four times.
        Assert.Equal(1, verdict.Assertions);

        var said = Said(verdict);

        // The member reaches the report as well as the locator: twelve steps across four panels
        // reported under four identical names is a trace a reader has to count lines in to use.
        Assert.True(said.Contains("[Working directory]", StringComparison.Ordinal), said);
        Assert.True(said.Contains("across 4 member(s)", StringComparison.Ordinal), said);

        // And what was visited, apart from what was asserted, which is the count the script kept by
        // hand and the half WW263's design asked for and did not get.
        Assert.True(said.Contains("4 of 4 member(s) carried this claim", StringComparison.Ordinal), said);
    }

    [Fact]
    public void A_member_with_nothing_to_check_is_counted_and_named_and_does_not_degrade_the_walk()
    {
        // WW276, and the reason it exists. claude-tray's About panel holds prose and links and not one
        // settings row, so a claim made once per panel holes there on every machine forever — and the
        // suite around it goes red on a page behaving exactly as designed.
        //
        // The same shape here: the four rows are all on the page, and the naming rule governs a
        // picker, a slider and a field — so the row holding a check box has nothing for it to name.
        var verdict = Run("rows.headers", "Group[name=\"{}\"] > ComboBox|Slider|Edit");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));

        // Counted and named, never silent: a row that was reached and had nothing to check is not
        // one that got away, and a reader has to be able to tell which it was.
        var said = Said(verdict);
        Assert.True(said.Contains("3 of 4 member(s) carried this claim", StringComparison.Ordinal), said);
        Assert.True(said.Contains("1 had nothing to check", StringComparison.Ordinal), said);
        Assert.True(said.Contains("[Startup]", StringComparison.Ordinal), said);
    }

    [Fact]
    public void A_walk_where_no_member_carried_the_claim_is_a_hole_and_never_a_pass()
    {
        // The other end, and it is WW263's empty-derivation guard one level in: a walk that visited
        // every member and found nothing to check anywhere ran zero times and reported nothing at all.
        var verdict = Run("rows.headers", "Group[name=\"{}\"] > ProgressBar");
        if (verdict is null)
            return;

        Assert.Equal(RunOutcome.Degraded, verdict.Outcome);
        Assert.Contains("not one of the 4 member(s) had anything to check", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_member_the_window_does_not_draw_is_still_a_red_and_never_one_of_those_holes()
    {
        // WW272's carve-out, kept: the locator's last step is the member itself, so nothing matching
        // is the strings and the window disagreeing rather than a row with nothing in it. The two read
        // alike from a count and are opposite repairs, so this is measured beside the two above.
        var verdict = Run("rows.withAnExtra", "Group[name=\"{}\"]");
        if (verdict is null)
            return;

        Assert.Equal(RunOutcome.Failed, verdict.Outcome);
        Assert.Contains("this window does not draw", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_member_the_window_does_not_have_fails_and_names_which_one()
    {
        // The panel that was never opened, which is the failure the derivation exists to produce: the
        // strings declare a row this page does not draw, and a listed set would never have asked.
        var verdict = Run("rows.withAnExtra");
        if (verdict is null)
            return;

        // WW272. A red and not the hole a sweep matching nothing otherwise gets. The difference is
        // which side the set came from: this locator was built out of a string the project declares,
        // so nothing matching is the file and the window disagreeing, and that is wrong everywhere.
        Assert.Equal(RunOutcome.Failed, verdict.Outcome);

        var said = Said(verdict);
        Assert.True(said.Contains("[Telemetry]", StringComparison.Ordinal), said);
        Assert.True(said.Contains("this window does not draw", StringComparison.Ordinal), said);
    }

    [Fact]
    public void A_key_declaring_nothing_is_refused_rather_than_run_over_no_members()
    {
        // The guard the script wrote by hand before its own walk. Zero members makes every assertion
        // inside run zero times and report nothing at all — a clean run over nothing.
        //
        // Refused rather than failed, which is where a sweep over a key nobody declared already goes:
        // nothing about the application was observed, so nothing about it is being reported.
        if (!Desk.Read().CanObserve)
            return;

        var refusal = Assert.Throws<ScenarioRefusedException>(() => Run("rows.nobodyDeclaredThese"));

        Assert.Contains("rows.nobodyDeclaredThese", refusal.Because, StringComparison.Ordinal);
        Assert.Contains("empty expected set", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_case_repeating_over_a_set_no_step_reaches_is_refused_where_it_was_written()
    {
        // It would drive the same window N times and report N times the confidence for one reading,
        // which is the unearned green arriving as arithmetic.
        var refusal = Assert.Throws<ScenarioRefusedException>(() => CaseDeclaration.Declared(
            "a case that repeats and never says over what",
            [StepDeclaration.Of("Group", "read", eachSpoken: true)],
            forEach: "rows.headers"));

        Assert.Contains("no step's locator names the member", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_locator_that_names_the_member_and_cannot_parse_with_one_is_refused_at_the_locator()
    {
        // Both are facts about the file, so both are judged where the locator was written rather than
        // on whichever member happened to expose it.
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Group[name=\"{}", "read", eachSpoken: true));

        Assert.Contains("does not parse", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_step_carrying_the_member_says_so_and_substitutes_into_a_new_locator()
    {
        var step = StepDeclaration.Of("Group[name=\"{}\"]", "read", eachSpoken: true);

        Assert.True(step.NamesTheMember);

        var one = step.For("Startup");

        // WW258 made `Locator` nullable for the tray subject; this step named one, so `Addressed`
        // answers the locator's text and asserts through the accessor a report actually reads.
        Assert.Equal("Group[name=\"Startup\"]", one.Addressed);
        Assert.Contains("[Startup]", one.Name, StringComparison.Ordinal);
        Assert.False(one.NamesTheMember);

        // WW272. What it was built out of, which is what tells a sweep matching nothing apart from a
        // page that legitimately has none of these.
        Assert.Equal("Startup", one.Carries);

        // And a step that reaches no member is not made a claim about one by being repeated beside a
        // step that does — every step of a repeated case is renamed, and only some of them derive.
        var beside = StepDeclaration.Of("Group", "read", eachSpoken: true).For("Startup");

        Assert.Null(beside.Carries);
        Assert.Contains("[Startup]", beside.Name, StringComparison.Ordinal);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>Repeat one claim over the strings a key declares, or null where the desk cannot observe.</summary>
    /// <param name="key">What to run once for each of.</param>
    /// <param name="locator">What each run sweeps, with the member reaching it through the placeholder.</param>
    private SuiteVerdict? Run(string key, string locator = "Group[name=\"{}\"]")
    {
        if (!Desk.Read().CanObserve)
            return null;

        // The rows the fixture actually draws, plus a key declaring one it does not and a key
        // declaring nothing at all — so every arm below is a fact about these strings.
        File.WriteAllText(
            Path.Combine(root, "strings.en.json"),
            """
            {
              "rows.headers.language": "Language",
              "rows.headers.directory": "Working directory",
              "rows.headers.startup": "Startup",
              "rows.headers.interval": "Refresh interval",

              "rows.withAnExtra.language": "Language",
              "rows.withAnExtra.telemetry": "Telemetry"
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
              "timeouts": { "resolve": 400, "act": 4000, "poll": 25 }
            }
            """);

        var cases = $$"""
            {
              "cases": [
                {
                  "name": "every row the strings declare is on the page and named",
                  "catches": "a panel added later that no case visits, which a listed set reports a clean pass over",
                  "forEach": {{System.Text.Json.JsonSerializer.Serialize(key)}},
                  "steps": [
                    {
                      "locator": {{System.Text.Json.JsonSerializer.Serialize(locator)}},
                      "act": "read",
                      "eachSpoken": true,
                      "named": "the row is on the page and everything in it is named"
                    }
                  ]
                }
              ]
            }
            """;

        return Suite.Run(
            ScenarioFile.Read("foreach.cases.json", cases),
            Selection.All,
            fixtureRoot,
            ProjectDeclaration.Load(declaration));
    }
}

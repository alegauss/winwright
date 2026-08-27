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
        Assert.Equal(4, verdict.Assertions);

        // The member reaches the report as well as the locator: twelve steps across four panels
        // reported under four identical names is a trace a reader has to count lines in to use.
        var said = Said(verdict);
        Assert.True(said.Contains("[Working directory]", StringComparison.Ordinal), said);
        Assert.True(said.Contains("across 4 member(s)", StringComparison.Ordinal), said);
    }

    [Fact]
    public void A_member_the_window_does_not_have_fails_and_names_which_one()
    {
        // The panel that was never opened, which is the failure the derivation exists to produce: the
        // strings declare a row this page does not draw, and a listed set would never have asked.
        var verdict = Run("rows.withAnExtra");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));
        Assert.Contains("[Telemetry]", Said(verdict), StringComparison.Ordinal);
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

        Assert.Equal("Group[name=\"Startup\"]", one.Locator.Text);
        Assert.Contains("[Startup]", one.Name, StringComparison.Ordinal);
        Assert.False(one.NamesTheMember);
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
    private SuiteVerdict? Run(string key)
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
                      "locator": "Group[name=\"{}\"]",
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

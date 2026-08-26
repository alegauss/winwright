using System.Windows.Automation;

using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW236. The engine has derived a set from a project's own strings since block F, and nothing in a
/// data file could name one — so that block's first criterion, that every set a scenario checks
/// against is derived rather than typed, was unfalsifiable of scenarios rather than met.
/// <para>
/// The defect it is for is claude-tray's panes case, which says it in its own comment: it named three
/// tab keys by hand, the window grew a fourth, and the case reported <em>all three tab headers
/// read</em> against a four-tab window. A list stops covering what it was written for and says
/// nothing when it does. Both ends of that are checked here — a window carrying every string holds,
/// and one string added to the file and not to the window is what fails.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class CoversTests : IDisposable
{
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;

    private readonly string root = Directory.CreateTempSubdirectory("winwright-covers-").FullName;

    /// <summary>Three labels, named for the three strings the file below declares.</summary>
    private readonly PumpedDialog dialog = PumpedDialog.Open(
        "winwright covers",
        new PumpedDialog.ChildWindow("Static", "Overview", WsChild | WsVisible, 20, 20, 160, 20),
        new PumpedDialog.ChildWindow("Static", "Sessions", WsChild | WsVisible, 20, 50, 160, 20),
        new PumpedDialog.ChildWindow("Static", "Profiles", WsChild | WsVisible, 20, 80, 160, 20));

    public void Dispose()
    {
        dialog.Dispose();
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void A_step_covering_a_key_is_a_claim_a_run_can_find_false()
    {
        var step = StepDeclaration.Of("Text", "read", covers: "stats.tab");

        Assert.Equal("stats.tab", step.Covers);
        Assert.True(step.Checkable, "a sweep is one claim, and a claim is checkable");
        Assert.Null(step.Expected);
    }

    [Fact]
    public void A_sweep_that_acts_is_refused_because_one_act_over_many_is_not_a_claim()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text", "invoke", covers: "stats.tab"));

        Assert.Contains("one act over many of them is not a claim", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_sweep_and_an_expectation_of_one_reading_are_two_claims()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text", "read", expected: "Overview", covers: "stats.tab"));

        Assert.Contains("a step answers one", refused.Because, StringComparison.Ordinal);

        // And a sweep claiming movement is caught by the rule above it, which is why the sweep's own
        // check does not name 'moves': a read never moved anything, whatever else it says.
        Assert.Contains(
            "reads and never acts",
            Assert.Throws<ScenarioRefusedException>(
                () => StepDeclaration.Of("Text", "read", covers: "stats.tab", moves: true)).Because,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_sweep_naming_a_pattern_reading_is_refused_because_it_compares_names()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text", "read", reads: "value", covers: "stats.tab"));

        Assert.Contains("a pattern reading is not one of them", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_sweep_is_not_refused_by_the_rules_written_for_a_step_about_one_element()
    {
        // Two refusals would otherwise fire first and name the wrong field: a sweep expects nothing of
        // one reading on purpose, so "the reading changes nothing" and "the step does nothing at all"
        // are both false of it — and a refusal that names the wrong field is fixed the wrong way.
        var step = StepDeclaration.Of("Text", "read", covers: "stats.tab");

        Assert.Equal("read", step.Verb.Name);
        Assert.True(step.Checkable);
    }

    [Fact]
    public void A_window_carrying_every_string_the_file_declares_holds()
    {
        var verdict = Run("""{ "stats.tab.overview": "Overview", "stats.tab.sessions": "Sessions", "stats.tab.profiles": "Profiles" }""");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
        Assert.Equal(1, verdict.Assertions);
    }

    [Fact]
    public void A_string_added_to_the_file_and_not_to_the_window_is_what_fails()
    {
        // The defect, provoked rather than waited for. This is the fourth tab: the file grows and the
        // window does not, and a case that had listed three would have gone on saying all three read.
        var verdict = Run("""
            {
              "stats.tab.overview": "Overview",
              "stats.tab.sessions": "Sessions",
              "stats.tab.profiles": "Profiles",
              "stats.tab.claudeCode": "Claude Code"
            }
            """);

        if (verdict is null)
            return;

        Assert.NotEqual(RunOutcome.Passed, verdict.Outcome);

        var said = Said(verdict);
        Assert.Contains("Claude Code", said, StringComparison.Ordinal);

        // Never the word 'all' while anything is missing, which is the sentence the old case printed.
        Assert.DoesNotContain("all 4", said, StringComparison.Ordinal);
    }

    [Fact]
    public void A_set_that_cannot_be_derived_is_a_refusal_and_never_a_failure()
    {
        // Nothing about the application was observed, so nothing about it is reported. The project
        // below declares no language files at all.
        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(declaration, """{ "executable": "nothing.exe" }""");

        var declared = ScenarioFile.Read("covers.cases.json", Cases);
        var verdict = Suite.Run(declared, Selection.All, dialog.Root, ProjectDeclaration.Load(declaration));

        // Broken and never failed, which is where a step's throw lands and is the right rank for it:
        // nothing about the application was observed, so the reader is being sent to the declaration
        // rather than to the window. The sentence names which of the two is missing.
        Assert.Equal(RunOutcome.Broken, verdict.Outcome);
        Assert.Contains("declares no languageFiles", Said(verdict), StringComparison.Ordinal);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    private const string Cases = """
        {
          "cases": [
            {
              "name": "every tab the strings declare is in the tree",
              "catches": "a pane whose header reads and whose body went missing with it",
              "steps": [ { "locator": "Text", "act": "read", "covers": "stats.tab" } ]
            }
          ]
        }
        """;

    /// <summary>Run the sweep against a strings file this test wrote, or null where the desk cannot observe.</summary>
    private SuiteVerdict? Run(string strings)
    {
        if (!Winwright.Windowing.Desk.Read().CanObserve)
            return null;

        var languages = Path.Combine(root, "en.json");
        File.WriteAllText(languages, strings);

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "languageFiles": ["en.json"],
              "timeouts": { "resolve": 4000, "act": 4000, "poll": 25 }
            }
            """);

        var project = ProjectDeclaration.Load(declaration);
        return Suite.Run(ScenarioFile.Read("covers.cases.json", Cases), Selection.All, dialog.Root, project);
    }
}

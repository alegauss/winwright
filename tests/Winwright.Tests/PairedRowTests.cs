using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW264. A sweep proves a control announces something. It cannot prove the something is its own
/// row's header, because it has no idea which row the control is in — and the failure hiding there is
/// worse than the one it catches: a rule that pairs the wrong two things gives several controls one
/// name, and a screen reader reads the same label over each of them.
/// <para>
/// Both pages are driven, and that is the point. A pane that always carried the defect would be one
/// nothing could prove the correct answer against — a claim failing on every row set is
/// indistinguishable from one that fails on everything.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class PairedRowTests : IDisposable
{
    private readonly Settling settling = Attachable.Settling();
    private readonly string root = Directory.CreateTempSubdirectory("winwright-paired-").FullName;

    public void Dispose()
    {
        settling.Dispose();
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void A_page_that_pairs_every_row_correctly_holds_across_all_of_them()
    {
        var verdict = Run("paired", "Group");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));

        // Across the rows and not one of them: a single-row match cannot fail this way by
        // construction, so the number is what says the claim had somewhere to go wrong.
        Assert.Contains("across 4 row(s)", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_control_wearing_the_row_next_doors_label_fails_and_names_both_rows()
    {
        var verdict = Run("borrowed", "Group");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));

        var said = Said(verdict);
        Assert.True(said.Contains("announce another row's header", StringComparison.Ordinal), said);

        // Both rows in the sentence, because "a control is misnamed" sends a reader to look at every
        // control on the page and "the slider in Refresh interval says Language" sends them at one.
        Assert.True(said.Contains("Refresh interval", StringComparison.Ordinal), said);
        Assert.True(said.Contains("Language", StringComparison.Ordinal), said);
    }

    [Fact]
    public void The_defect_is_invisible_to_a_check_that_asks_only_whether_a_name_exists()
    {
        // The whole reason this claim exists. Every name on the borrowed page is non-empty and every
        // one of them is a real label somebody wrote — so the sweep that catches glyphs and echoed
        // ids passes it, and only the pairing does not.
        var verdict = Run("borrowed", "Group", each: true);
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_locator_matching_no_row_fails_rather_than_pairing_nothing()
    {
        var verdict = Run("paired", "Group[name=\"Nothing drew this\"]");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));
        Assert.Contains("met by an empty window", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void The_pairing_is_a_read_and_one_claim_like_every_other()
    {
        Assert.Contains(
            "not a claim",
            Assert.Throws<ScenarioRefusedException>(
                () => StepDeclaration.Of("Group", "invoke", ownHeader: true)).Because,
            StringComparison.Ordinal);

        Assert.Contains(
            "also makes another claim",
            Assert.Throws<ScenarioRefusedException>(
                () => StepDeclaration.Of("Group", "read", eachSpoken: true, ownHeader: true)).Because,
            StringComparison.Ordinal);

        Assert.Contains(
            "which is their name",
            Assert.Throws<ScenarioRefusedException>(
                () => StepDeclaration.Of("Group", "read", reads: "name", ownHeader: true)).Because,
            StringComparison.Ordinal);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>Drive one shape of the rows pane, or null where this desk cannot observe.</summary>
    /// <param name="how">`paired` or `borrowed`.</param>
    /// <param name="locator">What the claim is about.</param>
    /// <param name="each">Make the weaker claim instead, which is what this one is here to beat.</param>
    private SuiteVerdict? Run(string how, string locator, bool each = false)
    {
        if (!Desk.Read().CanObserve)
            return null;

        var launched = settling.Register.Launch(Fixture.Started($"--rows={how}"));
        var drawn = Attempt.UntilTrue(() => TopLevelWindows.Largest(launched.Pid) is not null, 20000, 25);

        Assert.True(drawn.Happened, $"the fixture drew no window in {drawn.WaitedMs}ms");
        var window = AutomationElement.FromHandle(TopLevelWindows.Largest(launched.Pid)!.Handle);

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
                  "name": "every row's controls announce that row",
                  "catches": "a naming rule that pairs the wrong two things, which every check for whether a name exists passes",
                  "steps": [
                    {
                      "locator": {{System.Text.Json.JsonSerializer.Serialize(locator)}},
                      "act": "read",
                      {{(each ? "\"eachSpoken\": true" : "\"ownHeader\": true")}},
                      "named": "no control wears the row next door's label"
                    }
                  ]
                }
              ]
            }
            """;

        return Suite.Run(
            ScenarioFile.Read("paired.cases.json", cases),
            Selection.All,
            window,
            ProjectDeclaration.Load(declaration));
    }
}

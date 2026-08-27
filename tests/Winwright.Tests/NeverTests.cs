using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW256. Every other claim is read once the waiting is over, and one of claude-tray's cannot be.
/// Coming back to a profile seen seconds ago shows its report without ever showing the <em>no
/// readings yet</em> line, because that line means the per-profile cache did not put the report back
/// — and the line is gone by the time anybody reads the end state, which is what passing looks like
/// and also what a switch that flashed one looks like.
/// <para>
/// Measured there at 12ms with the cache and 162ms without it, on the same window a second apart.
/// The assertion is written as <em>the line was never shown</em> and deliberately not as <em>the
/// panes returned within N ms</em>: a deadline is the one claim in that file that would go red on a
/// slow machine for a correct reason.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class NeverTests : IDisposable
{
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;

    /// <summary>The string the strings file below declares, and what the dialog is showing.</summary>
    private const string NoReadings = "No readings for this profile yet";

    private readonly string root = Directory.CreateTempSubdirectory("winwright-never-").FullName;

    /// <summary>
    /// A label that is showing and a control that is there. Which of the two a case names decides
    /// whether the claim holds, and both ends are reachable from one static window: the forbidden
    /// string is either in it or the case named a key for a string that is not.
    /// </summary>
    private readonly PumpedDialog dialog = PumpedDialog.Open(
        "winwright never",
        new PumpedDialog.ChildWindow("Static", NoReadings, WsChild | WsVisible, 20, 20, 260, 20),
        new PumpedDialog.ChildWindow("Static", "Used 41%", WsChild | WsVisible, 20, 50, 260, 20));

    public void Dispose()
    {
        dialog.Dispose();
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void A_string_the_window_never_showed_holds_and_says_how_often_it_looked()
    {
        // The declared key is one the dialog does not carry, so nothing was ever showing it and the
        // locator is there from the first poll.
        var verdict = Run("labels.stale");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));

        // How often it looked, in the sentence. The strength of a negative claim is the number of
        // times somebody looked, and a reader who cannot see it cannot tell this from a claim that
        // held because the wait was over before it started.
        Assert.Contains("look(s)", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_string_that_was_showing_fails_and_names_what_carried_it()
    {
        var verdict = Run("labels.noReadings");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));

        var said = Said(verdict);
        Assert.True(said.Contains(NoReadings, StringComparison.Ordinal), said);
        Assert.True(said.Contains("labels.noReadings", StringComparison.Ordinal), said);
    }

    [Fact]
    public void A_locator_that_never_arrives_fails_rather_than_holding_on_having_seen_nothing()
    {
        // The trap this claim is easiest to write. Nothing arrives, so nothing was ever seen — and
        // reporting that as *the line never showed* would be a green over a window that never got
        // where the case was taking it.
        var verdict = Run("labels.stale", locator: "Text[name=\"Nothing here\"]");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));
        Assert.True(Said(verdict).Contains("never arrived", StringComparison.Ordinal), Said(verdict));
    }

    [Fact]
    public void It_is_one_claim_like_every_other_one()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text", "read", expected: "Used 41%", reads: "name", never: "labels.stale"));

        Assert.Contains("also makes another claim", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void Naming_a_reading_beside_it_narrows_nothing_and_is_refused()
    {
        // The claim is about the window and not about this element: the string may show anywhere, and
        // the locator says when to stop looking rather than what to look at.
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text", "read", reads: "name", never: "labels.stale"));

        Assert.Contains("the claim is about the window", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_step_that_only_claims_this_is_still_a_check()
    {
        // The rule every claim before it is under, and the one a new field is quietly left out of: a
        // case whose only step makes this claim must not be refused as a case that claims nothing.
        var step = StepDeclaration.Of("Text", "read", never: "labels.stale");

        Assert.True(step.Checkable);
        Assert.Equal(1, CaseDeclaration.Of("a case that only watches", step).Checks);
    }

    [Fact]
    public void A_key_the_project_does_not_declare_is_refused_where_it_was_written()
    {
        var verdict = Run("labels.nobodyDeclaredThis");
        if (verdict is null)
            return;

        Assert.True(Said(verdict).Contains("labels.nobodyDeclaredThis", StringComparison.Ordinal), Said(verdict));
        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>Watch for one declared key while waiting for a locator, or null where the desk cannot observe.</summary>
    private SuiteVerdict? Run(string key, string locator = "Text[name=\"Used 41%\"]")
    {
        if (!Desk.Read().CanObserve)
            return null;

        File.WriteAllText(
            Path.Combine(root, "strings.en.json"),
            $$"""
            {
              "labels.noReadings": {{System.Text.Json.JsonSerializer.Serialize(NoReadings)}},
              "labels.stale": "Rates last read"
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
                  "name": "the line was never shown while the report came back",
                  "catches": "a report put back by a cache and one rebuilt from scratch, which read the same once the waiting was over",
                  "steps": [
                    {
                      "locator": {{System.Text.Json.JsonSerializer.Serialize(locator)}},
                      "act": "read",
                      "never": {{System.Text.Json.JsonSerializer.Serialize(key)}},
                      "named": "coming back never showed the not-yet line"
                    }
                  ]
                }
              ]
            }
            """;

        return Suite.Run(
            ScenarioFile.Read("never.cases.json", cases),
            Selection.All,
            dialog.Root,
            ProjectDeclaration.Load(declaration));
    }
}

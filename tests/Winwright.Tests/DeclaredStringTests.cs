using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW261 and WW270. `expect` takes a literal, which for a label is the hardcoded set with one member:
/// it goes stale the day somebody edits the string, and it is wrong in every other language the
/// application ships from the moment it is written. claude-tray's script never typed one — it called
/// the strings file for all nine of its label reads.
/// <para>
/// The mirror is the same mechanism and a different claim. Some states an application has a word for
/// are states it must not be in: a live strip announcing its own <em>throughput unavailable</em> label
/// means the tail was never restarted, and no reading of a value could catch it because the numbers
/// are all present.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class DeclaredStringTests : IDisposable
{
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;

    private readonly string root = Directory.CreateTempSubdirectory("winwright-declared-").FullName;

    /// <summary>Two labels: one the strings file declares, and one it declares as the wrong state.</summary>
    private readonly PumpedDialog dialog = PumpedDialog.Open(
        "winwright declared",
        new PumpedDialog.ChildWindow("Static", "Refresh interval", WsChild | WsVisible, 20, 20, 220, 20),
        new PumpedDialog.ChildWindow("Static", "Live throughput unavailable", WsChild | WsVisible, 20, 50, 260, 20));

    public void Dispose()
    {
        dialog.Dispose();
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void A_control_announcing_the_string_its_key_declares_holds()
    {
        var verdict = Run("Text[name=\"Refresh interval\"]", label: "settings.interval");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));

        // The key and the string, both in the sentence: a reader has to be able to tell a control
        // announcing the wrong label from one announcing the right label in the wrong language.
        var said = Said(verdict);
        Assert.True(said.Contains("settings.interval", StringComparison.Ordinal), said);
        Assert.True(said.Contains("Refresh interval", StringComparison.Ordinal), said);
    }

    [Fact]
    public void A_control_announcing_something_else_fails_and_names_both()
    {
        var verdict = Run("Text[name=\"Live throughput unavailable\"]", label: "settings.interval");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));
        Assert.Contains("Refresh interval", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_reading_that_must_not_be_the_declared_string_holds_where_it_is_not()
    {
        var verdict = Run("Text[name=\"Refresh interval\"]", notLabel: "stats.live.off");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void The_state_the_application_has_a_word_for_is_caught_where_it_is_showing()
    {
        // The defect WW270 exists for: every number on the strip is present and plausible, and the
        // only thing wrong is which string the headline is.
        var verdict = Run("Text[name=\"Live throughput unavailable\"]", notLabel: "stats.live.off");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));
        Assert.Contains("anything but", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_key_the_project_does_not_declare_is_refused_rather_than_read_as_nothing()
    {
        var verdict = Run("Text[name=\"Refresh interval\"]", label: "settings.nobodyDeclaredThis");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));
        Assert.Contains("settings.nobodyDeclaredThis", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void Claiming_it_is_the_string_and_is_not_the_string_is_two_things()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text", "read", label: "a.key", notLabel: "another.key"));

        // WW83 made it three ways rather than two, so the refusal names the fields to choose between
        // rather than saying "these are two" of a family that now has a third member.
        Assert.Contains("one declared string claimed different ways", refusal.Because, StringComparison.Ordinal);
        Assert.Contains("'label'", refusal.Because, StringComparison.Ordinal);
        Assert.Contains("'notLabel'", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void It_is_one_claim_like_every_other_one()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text", "read", expected: "Refresh interval", label: "settings.interval"));

        Assert.Contains("also makes another claim", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_step_that_only_claims_this_is_still_a_check()
    {
        Assert.True(StepDeclaration.Of("Text", "read", label: "settings.interval").Checkable);
        Assert.True(StepDeclaration.Of("Text", "read", notLabel: "stats.live.off").Checkable);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>Read one control against a declared string, or null where this desk cannot observe.</summary>
    private SuiteVerdict? Run(string locator, string? label = null, string? notLabel = null)
    {
        if (!Desk.Read().CanObserve)
            return null;

        File.WriteAllText(
            Path.Combine(root, "strings.en.json"),
            """
            {
              "settings.interval": "Refresh interval",
              "stats.live.off": "Live throughput unavailable"
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

        var claim = label is not null
            ? $"\"label\": {System.Text.Json.JsonSerializer.Serialize(label)}"
            : $"\"notLabel\": {System.Text.Json.JsonSerializer.Serialize(notLabel)}";

        var cases = $$"""
            {
              "cases": [
                {
                  "name": "the control announces what the strings declare",
                  "catches": "a label typed into a case, which goes stale the day the string is edited and is wrong in every other language from the moment it is written",
                  "steps": [
                    {
                      "locator": {{System.Text.Json.JsonSerializer.Serialize(locator)}},
                      "act": "read",
                      "reads": "name",
                      {{claim}},
                      "named": "the control announces the declared string"
                    }
                  ]
                }
              ]
            }
            """;

        return Suite.Run(
            ScenarioFile.Read("declared.cases.json", cases),
            Selection.All,
            dialog.Root,
            ProjectDeclaration.Load(declaration));
    }
}

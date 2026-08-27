using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW250. The claim between naming a value and saying only that there is one.
/// <para>
/// Measured while migrating claude-tray's sessions case. Its list-price note interpolates the date the
/// rate card was read, so no case can name what it says — and a note that has lost that date is the
/// defect, while still answering. So <c>expect</c> could not be written and <c>answers</c> would have
/// read as covered while checking nothing.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class MatchesTests : IDisposable
{
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;

    private readonly string root = Directory.CreateTempSubdirectory("winwright-matches-").FullName;

    /// <summary>A label carrying a date, which is the shape this field exists for.</summary>
    private readonly PumpedDialog dialog = PumpedDialog.Open(
        "winwright matches",
        new PumpedDialog.ChildWindow("Static", "rates read 2026-08-26", WsChild | WsVisible, 20, 20, 260, 20));

    public void Dispose()
    {
        dialog.Dispose();
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void A_step_can_claim_the_shape_of_a_value_it_cannot_name()
    {
        var step = StepDeclaration.Of("Text", "read", reads: "name", matches: @"\d{4}-\d{2}-\d{2}");

        Assert.NotNull(step.Matches);
        Assert.True(step.Checkable);
        Assert.Null(step.Expected);
    }

    [Fact]
    public void A_pattern_that_matches_the_empty_string_is_refused_as_answers_wearing_a_check()
    {
        // The unearned green this field is easiest to write, and the same shape WW237 and WW238 each
        // closed once: a pattern matching the empty string holds for every answer there is.
        foreach (var loose in new[] { ".*", "^.*$", "a?", "" })
        {
            if (loose.Length == 0)
            {
                // An empty pattern is no pattern at all, so the step is simply one that claims nothing
                // — which the rule about a reading with no expectation already refuses.
                Assert.Throws<ScenarioRefusedException>(
                    () => StepDeclaration.Of("Text", "read", reads: "name", matches: loose));
                continue;
            }

            var refused = Assert.Throws<ScenarioRefusedException>(
                () => StepDeclaration.Of("Text", "read", reads: "name", matches: loose));

            Assert.Contains("matches the empty string", refused.Because, StringComparison.Ordinal);
            Assert.Contains("say 'answers'", refused.Because, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void A_pattern_that_does_not_parse_is_refused_where_the_locator_would_be()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text", "read", reads: "name", matches: "([unclosed"));

        Assert.Contains("does not parse", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void One_claim_per_step_holds_for_this_one_too()
    {
        foreach (var both in new Action[]
        {
            () => StepDeclaration.Of("Text", "read", expected: "x", reads: "name", matches: "x"),
            () => StepDeclaration.Of("Slider", "nudge", moves: true, matches: "x"),
            () => StepDeclaration.Of("Text", "read", reads: "name", answers: true, matches: "x"),
            () => StepDeclaration.Of("Text", "read", covers: "stats.tab", matches: "x"),
        })
        {
            var refused = Assert.Throws<ScenarioRefusedException>(both);
            Assert.Contains("another claim", refused.Because, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void A_reading_that_matches_passes_and_one_that_does_not_says_what_it_read()
    {
        if (!Desk.Read().CanObserve)
            return;

        var held = Run(@"rates read \d{4}-\d{2}-\d{2}");
        Assert.True(held!.Outcome == RunOutcome.Passed, Said(held));

        // And the failure names the pattern and the reading, because what a reader needs is what the
        // control actually said rather than that it failed to match.
        var missed = Run(@"rates read \d{4}/\d{2}/\d{2}");
        Assert.NotEqual(RunOutcome.Passed, missed!.Outcome);

        var said = Said(missed);
        Assert.True(said.Contains(@"rates read \d{4}/\d{2}/\d{2}", StringComparison.Ordinal), said);
        Assert.True(said.Contains("rates read 2026-08-26", StringComparison.Ordinal), said);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    private SuiteVerdict? Run(string pattern)
    {
        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "resolve": 2000, "act": 2000, "poll": 20 }
            }
            """);

        var cases = $$"""
            {
              "cases": [
                {
                  "name": "the label carries a date",
                  "catches": "a caption that lost the date its figure depends on, which still answers",
                  "steps": [
                    {
                      "locator": "Text",
                      "act": "read",
                      "reads": "name",
                      "matches": {{System.Text.Json.JsonSerializer.Serialize(pattern)}}
                    }
                  ]
                }
              ]
            }
            """;

        return Suite.Run(
            ScenarioFile.Read("matches.cases.json", cases),
            Selection.All,
            dialog.Root,
            ProjectDeclaration.Load(declaration));
    }
}

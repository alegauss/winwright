using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW326. A dialog quotes the thing it opened for, and equality is false of every one of them.
/// <para>
/// Measured migrating pportal's capture case: it reads the pad's name off the mapping screen,
/// presses a row, and asserts the prompt that comes up quotes it. Neither string can be typed in a
/// case — one is whatever controller is plugged into that desk and the other is built out of it —
/// so the claim is exactly <em>the reading of this step holds the reading of that one</em>, and
/// there was nowhere to write it.
/// </para>
/// <para>
/// The pane it is read against builds the second caption out of the first, so the two cannot
/// disagree. That is what keeps this a claim about the application rather than about two constants
/// that happen to overlap.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ContainsTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-contains-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void A_reading_that_quotes_an_earlier_one_holds_it()
    {
        var verdict = Run("Text#chordEcho", "contains");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void The_same_pair_is_not_equal_which_is_why_the_claim_exists()
    {
        // The control. `sameAs` is the claim that was available, and this is the case it cannot
        // make: the echo holds the read-out and is not it.
        var verdict = Run("Text#chordEcho", "sameAs");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_reading_that_does_not_hold_it_fails_and_names_what_it_read()
    {
        // The title says "Chords" and holds nothing the read-out said, so the claim is false — and
        // the failure has to say what it read rather than only that it did not hold.
        var verdict = Run("Text#chordsTitle", "contains");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));
        Assert.Contains("Chords", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void Claiming_it_holds_one_step_and_compares_with_another_is_two_things()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text", "read", reads: "name", contains: "a step", sameAs: "another"));

        Assert.Contains("a step answers one thing", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_countdown_pointer_at_a_step_nobody_wrote_is_refused_at_last()
    {
        // Not this task's claim, and found by it. The cross-step check read `SameAs ?? Unlike`, so a
        // `sameCountdownAs` naming a step that does not exist reached the run and compared against
        // nothing — the sixth call site WW308 said would be the one to spell it differently.
        var refused = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            {
              "cases": [
                {
                  "name": "a",
                  "steps": [
                    {
                      "locator": "Text#one",
                      "act": "read",
                      "reads": "name",
                      "sameCountdownAs": "a step nobody wrote",
                      "named": "the second stop"
                    }
                  ]
                }
              ]
            }
            """));

        Assert.Contains("no step before it is called that", refused.Because, StringComparison.Ordinal);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>Run a command, then compare a second reading with the first — or null where this desk cannot observe.</summary>
    private SuiteVerdict? Run(string locator, string claim)
    {
        if (!Desk.Read().CanObserve)
            return null;

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "launch": 30000, "resolve": 4000, "act": 4000, "poll": 25 }
            }
            """);

        var cases = $$"""
            {
              "fixtures": [
                { "name": "a window whose captions quote each other", "arguments": ["--chords", "--show"] }
              ],
              "cases": [
                {
                  "name": "the caption quotes what the other one says",
                  "catches": "a dialog asserted by equality against the thing it names, which is false of every dialog that quotes what it opened for",
                  "fixture": "a window whose captions quote each other",
                  "steps": [
                    {
                      "locator": "Text#chordRan",
                      "act": "press",
                      "with": "Ctrl+Shift+I",
                      "named": "the command runs"
                    },
                    {
                      "locator": "Text#chordRan",
                      "act": "read",
                      "reads": "name",
                      "answers": true,
                      "named": "what the read-out says"
                    },
                    {
                      "locator": {{System.Text.Json.JsonSerializer.Serialize(locator)}},
                      "act": "read",
                      "reads": "name",
                      {{System.Text.Json.JsonSerializer.Serialize(claim)}}: "what the read-out says",
                      "named": "the caption beside it"
                    }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(
            ScenarioFile.Read("contains.cases.json", cases), Selection.All, register, project);

        register.StopAll();
        return verdict;
    }
}

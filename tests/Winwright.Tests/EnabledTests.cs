using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW325. The state a form is in before it is filled in.
/// <para>
/// Measured missing on pportal's mapping screen, whose third case is one line — Update stays off
/// until something is bound, asserted on the real control rather than on the view model that
/// decides it. No reading in the vocabulary answered it: <c>toggle</c> is a pattern a Button does
/// not offer, <c>focused</c> is about the desk, and <c>anything</c> walks the patterns a disabled
/// Button answers through none of.
/// </para>
/// <para>
/// The pane it is read against puts the greyed control beside three kinds of absence, which is the
/// distinction that matters: a case that could not tell <em>not there</em> from <em>there and
/// refusing</em> would report a form as broken for the state it is supposed to be in.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class EnabledTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-enabled-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void A_control_that_will_not_take_input_reads_as_not_enabled()
    {
        var verdict = Run("Button#refusing", "not enabled");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_control_beside_it_that_will_reads_as_enabled()
    {
        // The control case. A reading that answered "not enabled" for everything would pass the one
        // above on a page where nothing is greyed at all.
        var verdict = Run("Button#showing", "enabled");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void Expecting_the_wrong_one_fails_and_says_which_it_read()
    {
        var verdict = Run("Button#refusing", "enabled");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));
        Assert.Contains("not enabled", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void Claiming_it_answers_is_refused_because_every_element_answers_it()
    {
        // The unearned green this reading would otherwise be, and the same one `focused` carries:
        // an element that resolved is enabled or is not, so "this reading says something" holds
        // wherever the locator matched — which is existence wearing the words of a reading.
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Button#refusing", "read", reads: "enabled", answers: true));

        Assert.Contains("could never be false", refused.Because, StringComparison.Ordinal);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>Read one control's enabled state, or null where this desk cannot observe.</summary>
    private SuiteVerdict? Run(string locator, string expected)
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
                { "name": "a window with the absences pane in it", "arguments": ["--absences"] }
              ],
              "cases": [
                {
                  "name": "the control says whether it will take input",
                  "catches": "a form asserted through its view model, where the control a person meets stays enabled and nothing reads it",
                  "fixture": "a window with the absences pane in it",
                  "steps": [
                    {
                      "locator": {{System.Text.Json.JsonSerializer.Serialize(locator)}},
                      "act": "read",
                      "reads": "enabled",
                      "expect": {{System.Text.Json.JsonSerializer.Serialize(expected)}},
                      "named": "the control's own answer about taking input"
                    }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(
            ScenarioFile.Read("enabled.cases.json", cases), Selection.All, register, project);

        register.StopAll();
        return verdict;
    }
}

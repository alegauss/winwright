using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW257. The wait after a launch refuses where no window arrives, and that is the right answer for
/// every fixture written before this one: nothing about the case was observed, so nothing about the
/// application is being reported.
/// <para>
/// A tray is the counter-example, and `WW82` was blocked on it. claude-tray's tray puts an icon in
/// the notification area and draws no window at all — and the window there is what a click on the
/// icon is supposed to <em>produce</em>, which is the claim rather than the precondition. Refusing
/// the fixture makes the one thing being asserted a reason not to run.
/// </para>
/// <para>
/// Said by the fixture and never inferred from the wait timing out, because that is the same refusal
/// read backwards. A flag that turned it off everywhere would buy one case and cost the answer on all
/// the others, so both ends are measured here.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ResidentFixtureTests : IDisposable
{
    private readonly Settling settling = Attachable.Settling();
    private readonly string root = Directory.CreateTempSubdirectory("winwright-resident-").FullName;

    public void Dispose()
    {
        settling.Dispose();
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void A_fixture_that_says_it_draws_nothing_is_run_rather_than_refused()
    {
        if (!Desk.Read().CanObserve)
            return;

        // The fixture's `--resident` is a process that runs and shows nothing, which is the ordinary
        // state of a tray application. The step below finds nothing on purpose: what is being measured
        // is that the run reached it at all, so what it reports is about the case rather than about
        // the launch.
        var verdict = Run(resident: true, "Button#nothingIsCalledThisOnAnyDesktop");

        Assert.Equal(RunOutcome.Failed, verdict.Outcome);
        Assert.Contains("nothingIsCalledThisOnAnyDesktop", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void The_same_launch_without_the_word_is_still_refused_for_drawing_no_window()
    {
        if (!Desk.Read().CanObserve)
            return;

        // The half a flag turning this off everywhere would have cost. A launch that was supposed to
        // draw a window and did not is the failure the refusal was written for, and it is the same
        // process here — the only difference is that the fixture did not say so.
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => Run(resident: false, "Button#nothingIsCalledThisOnAnyDesktop"));

        Assert.Contains("drew no window", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_resident_launch_that_died_is_named_by_the_case_rather_than_by_its_locators()
    {
        if (!Desk.Read().CanObserve)
            return;

        // WW279. The same fixture and the same step as the case above, with `--dies` the only
        // difference: the launch exits on startup, so the step finds nothing for a second reason and
        // the reds it produces are identical. What has to differ is the case's own line.
        var verdict = Run(resident: true, "Button#nothingIsCalledThisOnAnyDesktop", "--dies");
        var line = Assert.Single(verdict.Ran).ToString();

        // The number off the fixture's own catalogue and never typed here: WW161, and a case
        // asserting a transcription would go on agreeing with itself after the fixture changed.
        Assert.Contains($"exited with {Fixture.ExitFor("dies")}", line, StringComparison.Ordinal);

        // Before the outcome, which is the whole of what was wrong: a reader who meets the outcome
        // first goes to the locator lines, and every one of those is about a desk the application
        // had already left.
        Assert.True(
            line.IndexOf("exited with", StringComparison.Ordinal)
                < line.IndexOf(RunOutcome.Failed.ToString(), StringComparison.Ordinal),
            line);
    }

    [Fact]
    public void A_resident_launch_the_run_stopped_itself_says_nothing_about_having_exited()
    {
        if (!Desk.Read().CanObserve)
            return;

        // The other half, and it is what makes the clause above mean anything. This launch behaves —
        // it runs, shows nothing, and the register stops it — so a line that named an exit here would
        // name one on every tray in the suite, which is a sentence nobody reads by the third case.
        var verdict = Run(resident: true, "Button#nothingIsCalledThisOnAnyDesktop");
        var ran = Assert.Single(verdict.Ran);

        Assert.Null(ran.Departed);
        Assert.DoesNotContain("exited", ran.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_fixture_says_which_it_is_in_the_line_a_report_prints()
    {
        // A reader of a red wants to know what it was run against, and "resident" is the difference
        // between a window that went missing and one there was never going to be.
        Assert.Contains(
            ", resident.",
            FixtureDeclaration.Of("the tray", resident: true).Sentence(),
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "resident",
            FixtureDeclaration.Of("a window").Sentence(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_case_file_says_it_and_the_declaration_carries_it()
    {
        var declared = ScenarioFile.Read(
            "resident.cases.json",
            """
            {
              "fixtures": [ { "name": "the tray", "arguments": ["--resident"], "resident": true } ],
              "cases": [
                {
                  "name": "the tray is there",
                  "catches": "a tray that exits on startup, which nothing else in the suite would notice",
                  "fixture": "the tray",
                  "steps": [ { "locator": "Button#icon", "act": "read", "reads": "name", "answers": true } ]
                }
              ]
            }
            """);

        Assert.True(Assert.Single(declared).Fixture.Resident);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>Run one case against a launch that draws nothing.</summary>
    /// <param name="resident">Whether the fixture says so.</param>
    /// <param name="locator">What the one step looks for.</param>
    /// <param name="also">Whatever else the launch carries, after <c>--resident</c>.</param>
    private SuiteVerdict Run(bool resident, string locator, params string[] also)
    {
        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "resolve": 400, "act": 2000, "poll": 25, "window": 1500 }
            }
            """);

        var cases = $$"""
            {
              "fixtures": [
                {
                  "name": "the fixture that draws nothing",
                  "arguments": [{{string.Join(", ", new[] { "--resident" }.Concat(also).Select(one => System.Text.Json.JsonSerializer.Serialize(one)))}}],
                  "resident": {{(resident ? "true" : "false")}}
                }
              ],
              "cases": [
                {
                  "name": "a case against a launch with no window of its own",
                  "catches": "a tray whose fixture is refused before the first step, so the click that is supposed to produce a window never happens",
                  "fixture": "the fixture that draws nothing",
                  "steps": [
                    {
                      "locator": {{System.Text.Json.JsonSerializer.Serialize(locator)}},
                      "act": "read",
                      "reads": "name",
                      "answers": true,
                      "named": "the run reached this step at all"
                    }
                  ]
                }
              ]
            }
            """;

        return Suite.Launch(
            ScenarioFile.Read("resident.cases.json", cases),
            Selection.All,
            settling.Register,
            ProjectDeclaration.Load(declaration));
    }
}

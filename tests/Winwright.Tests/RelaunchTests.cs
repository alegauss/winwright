using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW575. A case that follows the application across the exit an update makes and the copy started
/// again after it.
/// <para>
/// Found by Modelado (MOD31): "Update and restart" starts the newer installer and exits, and the
/// installer starts the application again. Driven against the fixture's <c>--relaunch</c> pane, whose
/// restart starts a new copy of the same executable marked <c>--relaunched</c> and exits, so a step
/// after the relaunch can only read "started again" off the new window.
/// </para>
/// <para>
/// The control is the exit with no restart: the exit is claimed green and the relaunch is red, so a
/// step that passed whatever the process did cannot pass both.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class RelaunchTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-relaunch-").FullName;

    /// <summary>What outlived the last run, read when the run stopped everything it held.</summary>
    private IReadOnlyList<Survivor> survivors = [];

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void An_update_that_exits_and_starts_again_is_followed_into_the_new_window()
    {
        var verdict = Run("Button#updateAndRestart");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        var said = Said(verdict);
        Assert.True(verdict.Outcome == RunOutcome.Passed, said);

        // The exit was the claim, so the case's line does not say the application died.
        Assert.DoesNotContain("the application exited", verdict.Ran[0].ToString(), StringComparison.Ordinal);

        // And the copy the case followed was stopped with it: nothing outlived the run.
        Assert.Empty(survivors);
    }

    [Fact]
    public void An_exit_with_no_restart_is_claimed_and_the_relaunch_it_never_made_is_red()
    {
        var verdict = Run("Button#exitOnly");
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        var said = Said(verdict);
        Assert.Equal(RunOutcome.Failed, verdict.Outcome);
        Assert.Contains("exited after", said, StringComparison.Ordinal);
        Assert.Contains("drew a window in", said, StringComparison.Ordinal);
        Assert.Empty(survivors);
    }

    [Theory]
    [Trait(NoDesk.Key, NoDesk.Free)]
    [InlineData("""{ "process": "exits", "act": "invoke" }""", "the one act it takes is 'read'")]
    [InlineData("""{ "process": "crashes", "act": "read" }""", "to 'exits' or to 'relaunches'")]
    [InlineData("""{ "process": "exits", "act": "read", "expect": "x" }""", "the wait is its claim")]
    [InlineData("""{ "process": "exits", "locator": "Button", "act": "read" }""", "a step acts on one thing")]
    public void What_a_process_step_cannot_say_is_refused_where_it_is_written(string step, string because)
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => ScenarioFile.Read("relaunch.cases.json", $$"""{ "cases": [ { "name": "a relaunch", "steps": [ {{step}} ] } ] }"""));

        Assert.Contains(because, refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_case_that_only_reads_cannot_wait_for_the_application_to_go()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("relaunch.cases.json", """
            { "cases": [ { "name": "a relaunch", "onlyReads": true, "steps": [ { "process": "exits", "act": "read" } ] } ] }
            """));

        Assert.Contains("leaves no window for the next case to borrow", refusal.Because, StringComparison.Ordinal);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>
    /// Press <paramref name="pressed"/>, claim the exit, wait for the relaunch and read the new
    /// window; or null where this desk cannot observe. What outlived the run is kept in
    /// <see cref="survivors"/>.
    /// </summary>
    private SuiteVerdict? Run(string pressed)
    {
        if (!Desk.Read().CanObserve)
            return null;

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "launch": 15000, "resolve": 4000, "act": 4000, "poll": 50 }
            }
            """);

        var cases = $$"""
            {
              "fixtures": [ { "name": "an application that updates", "arguments": ["--relaunch"] } ],
              "cases": [
                {
                  "name": "an update restarts the application",
                  "catches": "an update that exits and starts the application again, which no case could follow",
                  "fixture": "an application that updates",
                  "steps": [
                    { "locator": "Text#relaunchSaid", "act": "read", "reads": "name", "expect": "first start", "named": "the first copy is up" },
                    { "locator": "{{pressed}}", "act": "invoke", "named": "the update is asked for" },
                    { "process": "exits", "act": "read", "named": "the application exits to update" },
                    { "process": "relaunches", "act": "read", "named": "the application is started again" },
                    { "locator": "Text#relaunchSaid", "act": "read", "reads": "name", "expect": "started again", "named": "the window is the new copy's" }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(ScenarioFile.Read("relaunch.cases.json", cases), Selection.All, register, project);
        survivors = register.StopAll();
        return verdict;
    }
}

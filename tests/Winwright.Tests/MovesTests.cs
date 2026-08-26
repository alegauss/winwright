using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW229. Measured migrating claude-tray's keyboard case. Its fourth assertion was an arrow key
/// driving a slider — a claim about movement, because the starting value belongs to the
/// application's own settings and no case can know it. `expect` compares a reading to a string, so
/// the migration put the control at a known floor first and expected the one value that could
/// follow: two steps instead of one, a write through the pattern before the key press that was the
/// point, and an expectation that goes stale the day the tick frequency changes.
/// <para>
/// What is checked here is the claim itself, and both ends of it. A control that moves holds; a
/// control that will not budge fails, and the sentence names the value that stayed — which is the
/// half a boolean would have thrown away.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class MovesTests : IDisposable
{
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint WsTabStop = 0x00010000;

    private readonly string root = Directory.CreateTempSubdirectory("winwright-moves-").FullName;

    /// <summary>
    /// WW234. This ran through <c>Suite.Launch</c>, which starts a process, and the driving case
    /// excused itself on every guest run while being read as proof. <c>Suite.Run</c> takes a root that
    /// is already open, which is the door: the window is this thread's, and the case is otherwise the
    /// same data file.
    /// <para>
    /// WW247 corrects the reason. It said a process started by a test host never gets the foreground;
    /// a launched window that opens focused already has it, and WW246 — the window under test reading
    /// as nothing — is what was really stopping this. The dialog stays for WW248's reason rather than
    /// this one, and whether it should is open.
    /// </para>
    /// </summary>
    private readonly PumpedDialog dialog = PumpedDialog.Open(
        "winwright moves",
        new PumpedDialog.ChildWindow("msctls_trackbar32", null, WsChild | WsVisible | WsTabStop, 20, 20, 200, 32));

    public void Dispose()
    {
        dialog.Dispose();
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void A_step_can_claim_the_reading_moved_without_naming_what_it_moved_to()
    {
        var step = StepDeclaration.Of("Slider#roomEitherWay", "nudge", reads: "range", moves: true);

        Assert.True(step.Moves);
        Assert.True(step.Checkable, "a step claiming movement is a step a run can find false");
        Assert.Null(step.Expected);
        Assert.Contains("range moves", step.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Naming_the_value_and_claiming_movement_are_two_claims_and_never_both()
    {
        // Naming the value already says it moved where it was something else, and a step owing two
        // assertion results is a trace line a reader has to take apart.
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Slider#roomEitherWay", "nudge", expected: "10", reads: "range", moves: true));

        Assert.Contains("naming the value says both", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_read_cannot_be_what_moved_a_reading()
    {
        // A read touches nothing, so a read claiming movement is a claim about whatever else is
        // happening on the desk.
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text#status", "read", moves: true));

        Assert.Contains("reads and never acts", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_reading_named_beside_a_movement_claim_is_no_longer_a_reading_that_changes_nothing()
    {
        // 'reads' used to require 'expect', because a reading nothing expects of decides nothing.
        // A movement claim is what it decides now, so the refusal has to stop applying.
        var step = StepDeclaration.Of("Slider#roomEitherWay", "nudge", reads: "range", moves: true);

        Assert.Equal("range", step.Reads.Name);
    }

    [Fact]
    public void A_control_that_moves_holds_and_a_case_says_so_in_one_step()
    {
        // The claim the script was making, in one step instead of two, against a control whose
        // starting value this case never names.
        var verdict = Run("""
            {
              "cases": [
                {
                  "name": "an arrow key moves a range",
                  "catches": "a range control that reports a value and does not respond to a key",
                  "steps": [
                    { "locator": "Slider", "act": "nudge", "reads": "range", "moves": true }
                  ]
                }
              ]
            }
            """);

        if (verdict is null)
            return;

        // Excused where the run never owned the desktop, and counted as the hole it is: a nudge that
        // was never attempted says nothing about the control, which is what WW225's precondition is
        // for. Asserted otherwise, with the whole reading in the message so a red says why.
        var only = Assert.Single(verdict.Ran);
        if (BusyDesk.Excused(Assert.Single(only.Verdict.Results)))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
        Assert.Equal(1, verdict.Assertions);
    }

    [Fact]
    public void A_control_that_will_not_budge_fails_and_the_sentence_names_the_value_that_stayed()
    {
        // The other end, provoked rather than waited for: the pane draws a range with one value in
        // it, so nothing could move it and the claim is false. What the reader gets is the number.
        // Written against the trackbar and not a range with no room: `set range` to the value it
        // already holds is a write that cannot move anything, which is the same false claim reached
        // without needing a control the fixture had to draw.
        var verdict = Run("""
            {
              "cases": [
                {
                  "name": "a value written where it already sits does not move",
                  "catches": "a movement claim that passes on a reading that never changed",
                  "steps": [
                    { "locator": "Slider", "act": "set range", "with": "0", "reads": "range", "moves": true }
                  ]
                }
              ]
            }
            """);

        if (verdict is null)
            return;

        var only = Assert.Single(verdict.Ran);
        if (BusyDesk.Excused(Assert.Single(only.Verdict.Results)))
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));

        // The value that stayed, in the sentence. A boolean would have thrown it away, and the number
        // is what tells a reader whether the control is stuck or the key never arrived.
        Assert.Contains("something other than '0'", Said(verdict), StringComparison.Ordinal);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        '\n',
        verdict.Render().Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString()))));

    /// <summary>Run one scenario against the fixture, or null where this desk cannot observe.</summary>
    private SuiteVerdict? Run(string cases)
    {
        var desk = Desk.Read();
        if (!desk.CanObserve)
            return null;

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "resolve": 4000, "act": 4000, "poll": 25, "launch": 20000 }
            }
            """);

        var project = ProjectDeclaration.Load(declaration);
        var declared = ScenarioFile.Read("moves.cases.json", cases);

        dialog.BringToFront();
        return Suite.Run(declared, Selection.All, dialog.Root, project);
    }
}

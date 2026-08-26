using System.Runtime.InteropServices;
using System.Windows.Automation;

using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Tracing;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW57's engine half. The claim is not that the acts work — <see cref="ActTests"/> proves that.
/// The claim is that a case carrying none of the loop, none of the waits, none of the attempts and
/// none of the verdict runs anyway, and that what it reports is derived from what it observed.
/// <para>
/// So each of these declares a case as data against a dialog built here, and asserts about the
/// verdict, the trace ordinals and the steps a stopped run did not reach — the four things
/// claude-tray's harness rewrote once per case.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class CaseRunTests : IDisposable
{
    private const uint WsPopup = 0x80000000;
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint BsAutoCheckBox = 0x0003;
    private const uint CbsDropDownList = 0x0003;
    private const uint CbAddString = 0x0143;

    private readonly List<nint> created = [];
    private readonly string root = Directory.CreateTempSubdirectory("winwright-caserun-").FullName;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SendMessageW(nint window, uint message, nint wParam, string lParam);

    public void Dispose()
    {
        for (var index = created.Count - 1; index >= 0; index--)
            DestroyWindow(created[index]);

        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void A_case_that_is_only_data_runs_and_earns_a_pass()
    {
        var frame = Dialog();
        var declared = CaseDeclaration.Of(
            "the field takes a name",
            StepDeclaration.Of("Edit", "set value", "beta", expected: "beta", reads: "value"));

        var run = Run(declared, frame);

        Assert.Equal(RunOutcome.Passed, run.Verdict.Outcome);
        Assert.Equal(1, run.Verdict.Ran);
        Assert.True(run.Finished);
        Assert.Empty(run.NotReached);
    }

    [Fact]
    public void A_verb_a_case_declared_is_the_verb_the_trace_names()
    {
        // Not decoration. The vocabulary is a closed list of strings and Act stamps its own; two
        // spellings of the same act would put the case and its trace into different words.
        var frame = Dialog();
        var declared = CaseDeclaration.Of(
            "the box ticks",
            StepDeclaration.Of("""CheckBox[name="Wrap lines"]""", "toggle", expected: "On", reads: "toggle"));

        var run = Run(declared, frame);

        Assert.Equal(RunOutcome.Passed, run.Verdict.Outcome);
        Assert.Equal("toggle", run.Trace[0].Verb);
    }

    [Fact]
    public void The_act_and_what_it_was_waited_for_are_two_trace_steps_and_the_result_points_at_the_second()
    {
        var frame = Dialog();
        var declared = CaseDeclaration.Of(
            "the field takes a name",
            StepDeclaration.Of("Edit", "set value", "beta", expected: "beta", reads: "value"));

        var run = Run(declared, frame);

        Assert.Equal(2, run.Trace.Count);
        Assert.Equal([1, 2], run.Trace.Select(step => step.Step));
        Assert.Equal("set value", run.Trace[0].Verb);
        Assert.Equal("expect", run.Trace[1].Verb);

        var result = Assert.Single(run.Verdict.Results);
        Assert.True(result.Traced);
        Assert.Equal(2, result.Step);
    }

    [Fact]
    public void A_step_that_expects_nothing_records_its_act_and_asserts_nothing()
    {
        var frame = Dialog();
        var declared = CaseDeclaration.Of(
            "the drop-down opens and the field takes a name",
            StepDeclaration.Of("ComboBox", "expand"),
            StepDeclaration.Of("Edit", "set value", "beta", expected: "beta", reads: "value"));

        var run = Run(declared, frame);

        Assert.Equal(RunOutcome.Passed, run.Verdict.Outcome);

        // Three lines for two steps: the unchecked act, then the checked act and its wait.
        Assert.Equal(["expand", "set value", "expect"], run.Trace.Select(step => step.Verb));
        Assert.Single(run.Verdict.Results);
    }

    [Fact]
    public void A_read_back_that_never_arrives_is_a_failure_carrying_what_it_read_instead()
    {
        var frame = Dialog();
        var declared = CaseDeclaration.Of(
            "the field reads gamma",
            StepDeclaration.Of("Edit", "set value", "beta", expected: "gamma", reads: "value"));

        var run = Run(declared, frame);

        Assert.Equal(RunOutcome.Failed, run.Verdict.Outcome);

        var failed = Assert.Single(run.Verdict.Failures);
        Assert.Contains("'beta'", failed.Detail);
        Assert.Contains("gamma", failed.Detail);
    }

    [Fact]
    public void A_step_that_throws_stops_the_case_and_the_run_names_what_it_did_not_reach()
    {
        // The locator parses and matches nothing, so resolving runs out of time and the act throws.
        // Every step after it was written expecting this one to have landed.
        var frame = Dialog();
        var declared = CaseDeclaration.Of(
            "a control nothing draws",
            StepDeclaration.Of("""Button[name="Nothing draws this"]""", "invoke", expected: "x", reads: "value"),
            StepDeclaration.Of("Edit", "set value", "beta", expected: "beta", reads: "value"),
            StepDeclaration.Of("ComboBox", "expand"));

        var run = Run(declared, frame);

        Assert.Equal(RunOutcome.Broken, run.Verdict.Outcome);
        Assert.False(run.Finished);
        Assert.Equal(1, run.Reached);
        Assert.Equal(
            ["""set value Edit""", "expand ComboBox"],
            run.NotReached.Select(step => step.Name));

        var broke = Assert.Single(run.Verdict.Broke);
        Assert.Equal(1, broke.Step);
        Assert.Equal(StepVerdict.Threw, run.Trace[0].Verdict);
    }

    [Fact]
    public void A_broken_step_leaves_no_assertion_behind_and_the_verdict_is_still_derived()
    {
        var frame = Dialog();
        var declared = CaseDeclaration.Of(
            "a control nothing draws",
            StepDeclaration.Of("""Button[name="Nothing draws this"]""", "invoke", expected: "x", reads: "value"));

        var run = Run(declared, frame);

        // RunVerdict refuses a run with neither a result nor an error, which is why a case with no
        // check at all is refused when it is declared rather than when it is run.
        Assert.Empty(run.Verdict.Results);
        Assert.Equal(RunOutcome.Broken, run.Verdict.Outcome);
        Assert.NotEqual(0, run.Verdict.ExitCode);
    }

    [Fact]
    public void A_toggle_whose_state_never_arrives_is_attempted_once_and_not_flipped_back()
    {
        // Toggling is not repeatable, so the engine takes one go. A second attempt would read Off,
        // and the failure would be about the state the retry produced rather than the one the case
        // asked for.
        var frame = Dialog();
        var declared = CaseDeclaration.Of(
            "the box reads a third state",
            StepDeclaration.Of("""CheckBox[name="Wrap lines"]""", "toggle", expected: "Indeterminate", reads: "toggle"));

        var run = Run(declared, frame);

        Assert.Equal(RunOutcome.Failed, run.Verdict.Outcome);
        Assert.Equal(1, run.Trace[0].Attempts);
        Assert.Contains("'On'", Assert.Single(run.Verdict.Failures).Detail);
    }

    [Fact]
    public void A_step_that_reads_touches_nothing_and_records_one_line_rather_than_two()
    {
        // WW213. An act and its expectation are two lines because they can be true separately. A
        // read has no act for it to be separately true of, and the empty pattern is what says so.
        var frame = Dialog();
        var declared = CaseDeclaration.Of(
            "the field says what it was made with",
            StepDeclaration.Of("Edit", "read", expected: "alpha", reads: "value"));

        var run = Run(declared, frame);

        Assert.Equal(RunOutcome.Passed, run.Verdict.Outcome);

        var only = Assert.Single(run.Trace);
        Assert.Equal("read", only.Verb);
        Assert.Equal("Edit", only.Locator);
        Assert.Null(only.Pattern);
        Assert.Equal("alpha", only.ReadBack);
        Assert.Equal("read Edit", only.Asserted);
        Assert.Equal(1, Assert.Single(run.Verdict.Results).Step);
    }

    [Fact]
    public void A_read_leaves_the_control_exactly_as_it_found_it()
    {
        var frame = Dialog();
        var declared = CaseDeclaration.Of(
            "the box is read twice and never flipped",
            StepDeclaration.Of("""CheckBox[name="Wrap lines"]""", "read", expected: "Off", reads: "toggle"),
            StepDeclaration.Of("""CheckBox[name="Wrap lines"]""", "read", expected: "Off", reads: "toggle"));

        var run = Run(declared, frame);

        // A toggle read twice would have been On by the second look. Two Offs is the claim.
        Assert.Equal(RunOutcome.Passed, run.Verdict.Outcome);
        Assert.Equal(2, run.Verdict.Ran);
        Assert.Equal(["read", "read"], run.Trace.Select(step => step.Verb));
    }

    [Fact]
    public void A_read_of_something_nothing_draws_is_a_failure_about_the_locator_and_not_a_throw()
    {
        // The reason a read does not go through Act: an act must have found something to press and
        // a read need not, so what was not there is an expectation nothing answered.
        var frame = Dialog();
        var declared = CaseDeclaration.Of(
            "the status label says Saved",
            StepDeclaration.Of("""Text[name="Saved"]""", "read", expected: "Saved", reads: "text"));

        var run = Run(declared, frame);

        Assert.Equal(RunOutcome.Failed, run.Verdict.Outcome);
        Assert.Empty(run.Verdict.Broke);
        Assert.Contains("nothing answered to it", Assert.Single(run.Verdict.Failures).Detail);
    }

    [Fact]
    public void A_read_is_attempted_once_however_long_it_waits()
    {
        var frame = Dialog();
        var declared = CaseDeclaration.Of(
            "the field says gamma",
            StepDeclaration.Of("Edit", "read", expected: "gamma", reads: "value"));

        var run = Run(declared, frame);

        Assert.Equal(RunOutcome.Failed, run.Verdict.Outcome);
        Assert.Equal(1, Assert.Single(run.Trace).Attempts);
    }

    [Fact]
    public void A_case_of_reads_alone_is_a_case_and_never_the_green_a_case_of_acts_alone_would_be()
    {
        // The pair that makes the refusal legible: reads are checks, so a case of nothing but reads
        // loads, and a case of nothing but acts does not.
        var reading = CaseDeclaration.Of(
            "the field says what it was made with",
            StepDeclaration.Of("Edit", "read", expected: "alpha", reads: "value"));

        Assert.Equal(1, reading.Checks);

        Assert.Contains(
            "can only ever read green",
            Assert.Throws<ScenarioRefusedException>(
                () => CaseDeclaration.Of("the tree opens", StepDeclaration.Of("ComboBox", "expand"))).Because);
    }

    [Fact]
    public void A_case_run_carries_the_case_it_was_a_run_of_and_says_so_in_one_line()
    {
        var frame = Dialog();
        var declared = CaseDeclaration.Of(
            "the field takes a name",
            StepDeclaration.Of("Edit", "set value", "beta", expected: "beta", reads: "value"));

        var run = Run(declared, frame);

        Assert.Same(declared, run.Declared);
        Assert.Contains("the field takes a name: Passed over 1 of 1 check", run.ToString());
    }

    private CaseResult Run(CaseDeclaration declared, nint frame) =>
        CaseRun.Of(declared, AutomationElement.FromHandle(frame), Declared());

    /// <summary>A project whose waits are short, because half of these are waiting for a red.</summary>
    private ProjectDeclaration Declared()
    {
        var path = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Environment.ProcessPath)}},
              "timeouts": { "resolve": 600, "act": 600, "poll": 20 }
            }
            """);

        return ProjectDeclaration.Load(path);
    }

    private nint Create(string className, string? title, uint style, int w, int h, nint parent = 0)
    {
        var window = CreateWindowExW(0, className, title, style, 20, 20, w, h, parent, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
        return window;
    }

    private nint Dialog()
    {
        var frame = Create("Static", "winwright statistics", WsPopup | WsVisible, 480, 320);
        Create("Button", "Wrap lines", WsChild | WsVisible | BsAutoCheckBox, 140, 24, frame);
        Create("Edit", "alpha", WsChild | WsVisible, 200, 24, frame);
        var combo = Create("ComboBox", null, WsChild | WsVisible | CbsDropDownList, 200, 200, frame);
        SendMessageW(combo, CbAddString, 0, "Alpha");
        SendMessageW(combo, CbAddString, 0, "Beta");
        return frame;
    }
}

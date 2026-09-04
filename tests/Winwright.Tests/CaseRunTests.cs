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

    [Fact]
    public void A_case_can_name_a_capture_and_the_picture_lands_where_the_project_says()
    {
        // WW336. The one act a case could not name, and the reason it could not was the file: every
        // other field is derived so a case means the same thing on the next machine, and a path
        // written into one is the plainest way to break that. So the case says what to call the
        // picture and the project says where pictures go.
        //
        // Resolved against the desktop and not against a window of its own, which is what a resident
        // fixture does and is the shape this verb is for: a tray draws no window, so a case that
        // photographs its menu has no main window to be told the menu is not. Here the popup this
        // test made stands in for that menu — WS_POPUP with no caption, which is what the route
        // reads and what sends it to the screen rather than to a render nothing can take.
        _ = Dialog();
        var declared = CaseDeclaration.Of(
            "the dialog is photographed",
            StepDeclaration.Of("Text[name=\"winwright statistics\"]", "capture", "the field as it opens"));

        var run = Run(declared, AutomationElement.RootElement, captures: true);

        // A window somebody stood over the region of is a hole and not a red, which is the whole of
        // what the arm mapping is for — so the case stands down rather than asserting about a desk.
        // Named against the engine's own condition, because a hole this case does not recognise is
        // one it must not stand down for.
        if (run.Verdict.Unchecked.Any(
                one => one.Missing?.Name == Winwright.Capturing.Obstruction.PreconditionName))
        {
            return;
        }

        // The whole reading and not the outcome alone: a capture has six ways of being wrong and a
        // red saying only "Degraded" sends a reader to a debugger to find out which.
        Assert.True(
            run.Verdict.Outcome == RunOutcome.Passed,
            string.Join(
                Environment.NewLine,
                run.Verdict.Unchecked.Select(one => $"  unchecked {one}")
                    .Concat(run.Verdict.Failures.Select(one => $"  failed    {one}"))));

        // The picture is where the project said, in a folder named for the case and a file named
        // for what the case called it — neither of which the case spelled as a path.
        var into = Path.Combine(root, "pictures", "the dialog is photographed", "the field as it opens.png");
        Assert.True(File.Exists(into), $"nothing was written to {into}");

        // And it is a picture rather than a file: the receipt counts the colours of whatever was
        // written, so a flat rectangle would have refused before this line.
        Assert.True(new FileInfo(into).Length > 0);
    }

    [Fact]
    public void A_capture_of_a_window_that_wants_a_render_is_a_hole_naming_the_half_that_can()
    {
        // WW336, and WW349 changed what the hole says without changing that it is one. The engine
        // used to answer that a render is the application's own to take and it could not take one;
        // now it asks, and this window is a bare Win32 frame with no in-app half behind it — so what
        // a reader is told is that the application did not answer, and what to add so it would.
        //
        // Still a hole and never a red, which is the half that did not move: the picture was never
        // taken, nothing about the application was observed, and how the product was built is not
        // something to fail it for.
        var frame = Create("Static", "winwright main", WsVisible | 0x00C00000, 480, 320);
        Create("Edit", "alpha", WsChild | WsVisible, 200, 24, frame);

        var declared = CaseDeclaration.Of(
            "the main window is photographed",
            StepDeclaration.Of("Edit", "capture", "the whole window"));

        var run = Run(declared, frame, captures: true);

        Assert.Equal(RunOutcome.Degraded, run.Verdict.Outcome);

        var hole = Assert.Single(run.Verdict.Unchecked);
        Assert.Contains("Winwright.InApp", hole.Detail, StringComparison.Ordinal);
        Assert.Contains("Renders.Answer", hole.Detail, StringComparison.Ordinal);
        Assert.Equal(Winwright.Capturing.RenderAsked.PreconditionName, hole.Missing?.Name);
    }

    [Fact]
    public void A_capture_with_nowhere_declared_to_put_it_is_a_hole_and_not_a_path_invented_here()
    {
        // WW336. A run that guessed a directory would be a run whose pictures land somewhere nobody
        // asked for, and a case that named one would be a case that means something else on the next
        // machine. Neither, so it says what is missing and which file to add it to.
        var frame = Dialog();
        var declared = CaseDeclaration.Of(
            "the dialog is photographed",
            StepDeclaration.Of("Edit", "capture", "the field"));

        var run = Run(declared, frame);

        Assert.Equal(RunOutcome.Degraded, run.Verdict.Outcome);

        var hole = Assert.Single(run.Verdict.Unchecked);
        Assert.Contains("declares no 'captures'", hole.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void A_capture_step_claims_the_picture_and_may_not_claim_a_reading_beside_it()
    {
        // WW336, and it is WW323's rule reaching the newest act: a capture's claim is the receipt —
        // that the picture is of this window, out of this process, with nothing showing through it —
        // and a reading of the element the locator matched is a second thing to check.
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Edit", "capture", "the field", expected: "beta", reads: "value"));

        Assert.Contains("a capture's claim is the picture", refused.Because, StringComparison.Ordinal);

        // And naming a reading alone is refused too, because a capture is about the window the
        // locator is inside rather than about what that element says.
        var named = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Edit", "capture", "the field", reads: "value"));

        Assert.Contains("about the window the locator is inside", named.Because, StringComparison.Ordinal);

        // A capture with nothing to call it is refused by the verb's own arity, which is where every
        // other missing argument is caught.
        Assert.Throws<ScenarioRefusedException>(() => StepDeclaration.Of("Edit", "capture"));
    }

    [Fact]
    public void A_capture_may_name_the_popup_it_is_of_and_no_other_verb_may()
    {
        // WW372. WW359 built the ask and wired nothing to it, so the surface a scenario most wants a
        // picture of — a flyout nobody has clicked — was the one a scenario could not ask for. It is
        // a field on the capture and not a third kind of subject: the step still addresses a window
        // the ordinary way, and this says which surface inside it the picture is of.
        var step = StepDeclaration.Of("Pane", "capture", "the flyout", popup: "  details  ");

        Assert.Equal("details", step.Popup);

        // Trimmed like every other field, because the name crosses a process boundary and a case
        // that indented its JSON would be asking for a popup called ' details '.
        Assert.Equal("capture", step.Verb.Name);

        // And nowhere else. Every other act reaches an element a locator matched, and a popup nobody
        // has opened is not one — so a step naming it under another verb would load and mean
        // nothing, which is the key that does nothing this format exists to refuse.
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Pane", "read", reads: "name", answers: true, popup: "details"));

        Assert.Contains("a popup is the surface a capture asks", refused.Because, StringComparison.Ordinal);
        Assert.Contains("details", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void The_act_line_says_what_the_verdict_settled_on_where_that_is_not_what_the_act_read()
    {
        // WW366. An act reads once, the moment it returns; the expectation beside it polls after
        // that to a deadline, and it is the second reading the verdict turns on. Both are true and
        // they are about different instants, and the act's line is the one a reader lands on —
        // it names the verb they wrote — so a line reading 'On, ok' above a pass sends them to the
        // control when the answer was elsewhere.
        //
        // Pinned through a reading that differs by projection rather than by timing: a checkbox
        // reads 'On' through its own patterns and 'Wrap lines' through its name, and this fixture
        // has no control that settles late on demand. It is the same fact on the line — the value
        // the verdict used is not the value the act read — which is why the field is shown and
        // never judged.
        var frame = Dialog();
        var declared = CaseDeclaration.Of(
            "the box says what it is called",
            StepDeclaration.Of("CheckBox", "toggle", expected: "Wrap lines", reads: "name"));

        var run = Run(declared, frame);

        Assert.Equal(RunOutcome.Passed, run.Verdict.Outcome);
        Assert.Equal(2, run.Trace.Count);

        // The act's own reading, which is not what was checked.
        Assert.Equal("toggle", run.Trace[0].Verb);
        Assert.NotEqual("Wrap lines", run.Trace[0].ReadBack);

        // And what was: the same value the expectation's own line carries, said on the line above
        // it so a reader never has to hold two lines together to see that they disagree.
        Assert.Equal("Wrap lines", run.Trace[0].Settled);
        Assert.Equal(run.Trace[1].ReadBack, run.Trace[0].Settled);
    }

    [Fact]
    public void The_act_line_says_nothing_extra_where_the_verdict_settled_on_what_it_read()
    {
        // The other half, and the reason the field is nullable: a step whose window had already
        // settled has nothing to add, and a second value on every line is the mark that marks
        // nothing.
        var frame = Dialog();
        var declared = CaseDeclaration.Of(
            "the drop-down opens and the field takes a name",
            StepDeclaration.Of("ComboBox", "expand"),
            StepDeclaration.Of("Edit", "set value", "beta", expected: "beta", reads: "value"));

        var run = Run(declared, frame);

        Assert.Equal(RunOutcome.Passed, run.Verdict.Outcome);

        // A step with no expectation never reaches the question.
        Assert.Equal("expand", run.Trace[0].Verb);
        Assert.Null(run.Trace[0].Settled);

        // And one whose expectation settled on what the act read has nothing to add.
        Assert.Equal("beta", run.Trace[1].ReadBack);
        Assert.Null(run.Trace[1].Settled);
    }

    private CaseResult Run(CaseDeclaration declared, nint frame, bool captures = false) =>
        Run(declared, AutomationElement.FromHandle(frame), captures);

    /// <summary>
    /// The same, against a root that need not be a window. WW336: a resident fixture draws none, so
    /// its cases resolve against the desktop — and the capture verb's own shape turns on that.
    /// </summary>
    /// <param name="declared">The case.</param>
    /// <param name="root">What its locators resolve under.</param>
    /// <param name="captures">Whether the project says where pictures go.</param>
    private CaseResult Run(CaseDeclaration declared, AutomationElement root, bool captures = false) =>
        CaseRun.Of(declared, root, Declared(captures));

    /// <summary>A project whose waits are short, because half of these are waiting for a red.</summary>
    /// <param name="captures">
    /// WW336. Whether it says where pictures go. Off by default and named by the three cases that
    /// need it, because the absence is itself a case: a run with nowhere declared answers a hole
    /// rather than inventing a directory.
    /// </param>
    private ProjectDeclaration Declared(bool captures = false)
    {
        var pictures = captures ? """, "captures": "pictures" """ : "";
        var path = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Environment.ProcessPath)}},
              "timeouts": { "resolve": 600, "act": 600, "poll": 20 }{{pictures}}
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

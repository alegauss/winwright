using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Scenarios;
using Winwright.Tracing;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW225. A case could name eight acts and all eight went through a control's own pattern, so the
/// half of the engine that puts input on the desk had no name a data file could write. WW78's
/// keyboard case would have migrated into `set value` and `set range` — the two pattern routes that
/// passed on the day the windows they were about took no keyboard input at all.
/// <para>
/// What is checked here is the difference itself: real keys reaching a real TextBox, and the result
/// carrying what the act needed of the machine. That second field is the one the shape turned on. An
/// act nothing performed and a control that would not move are indistinguishable from a reading, and
/// reporting the first as the second is a red about the application on a fact about the desk.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class SynthesisedActTests : IDisposable
{
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;

    /// <summary>
    /// Without this, Tab moves nothing. A plain child window is not in a tab order — that is what a
    /// dialog's own message loop arranges — and the first version of this class left it off and
    /// asserted the focus had moved. It had not, and the case said so rather than excusing itself,
    /// which is the difference WW232 bought.
    /// </summary>
    private const uint WsTabStop = 0x00010000;

    /// <summary>
    /// An in-process window, and that is the whole reason this class does not launch the fixture.
    /// <para>
    /// WW232. It did launch it, and every positive case here excused itself on every guest run —
    /// silently, while being reported as proof. What that was blamed on is corrected by WW247: the
    /// sentence written here said a fixture started by a test host can never take the foreground,
    /// inferred from <see cref="Act"/>'s header, which is about Windows refusing to *grant* it. A
    /// launched window that opens focused already has it. The real cause was WW246 — the engine read
    /// the window under test as nothing, because a WPF control carries no handle of its own.
    /// </para>
    /// <para>
    /// The dialog stays for now and its reason is WW248: a window this thread shows takes the desk, so
    /// a class that opens one cannot drive a launched fixture beside it. Whether these cases are
    /// better off back on the fixture is a question about this class rather than about the engine, and
    /// is open.
    /// </para>
    /// <para>
    /// Two edits and a label: something to type into, something for Tab to move the focus to, and
    /// something that resolves while offering no Invoke, which is what a click has to be about.
    /// </para>
    /// </summary>
    private readonly PumpedDialog dialog = PumpedDialog.Open(
        "winwright synthesised",
        new PumpedDialog.ChildWindow("Edit", "alpha", WsChild | WsVisible | WsTabStop, 20, 20, 220, 24),
        new PumpedDialog.ChildWindow("Edit", "beta", WsChild | WsVisible | WsTabStop, 20, 60, 220, 24),
        new PumpedDialog.ChildWindow("Static", "a label", WsChild | WsVisible, 20, 100, 120, 20),
        new PumpedDialog.ChildWindow("msctls_trackbar32", null, WsChild | WsVisible | WsTabStop, 20, 130, 200, 32));

    public void Dispose() => dialog.Dispose();

    private Subject On(string locator) =>
        Subject.Unguarded(dialog.Root, Locator.Parse(locator), deadlineMs: 4000, pollMs: 25);

    [Fact]
    public void Typing_reaches_the_control_with_real_keys_and_reads_back()
    {
        // The observable WW78's first assertion is. `set value` beside this one writes through
        // ValuePattern and cannot fail the way this one can.
        var typed = Synthesised.Type(On("Edit[order=top]"), "beta");

        if (BusyDesk.Excused(typed.Needed!))
            return;

        Assert.True(typed.Attempted, typed.ToString());
        Assert.Equal("beta", typed.After.Value);
        Assert.True(typed.Changed, typed.ToString());
        Assert.Equal("synthesised keyboard", typed.Pattern);
    }

    [Fact]
    public void A_pattern_act_needs_nothing_of_the_machine_and_says_so_by_carrying_nothing()
    {
        // The other half of the shape, and the reason the field is nullable rather than always
        // present: nothing about the desk stops a pattern act, which is why those eight were the
        // whole vocabulary a case could name.
        var written = Act.SetValue(On("Edit[order=top]"), "gamma");

        Assert.Null(written.Needed);
        Assert.True(written.Attempted);
        Assert.Equal(StepVerdict.Ok, written.AsTraceStep().Verdict);
    }

    [Fact]
    public void A_synthesised_act_always_carries_what_it_needed_whichever_way_it_went()
    {
        // Present either way, so a reader never has to know which verbs are which to know whether
        // the answer is about the application.
        var typed = Synthesised.Type(On("Edit[order=top]"), "delta");

        Assert.NotNull(typed.Needed);
        Assert.Equal(typed.Needed!.Satisfied, typed.Attempted);

        // And the trace says the same thing the field does: ok where it ran, unchecked where it did
        // not, because a trace line reading ok over an act nothing performed is the unearned green in
        // miniature.
        Assert.Equal(
            typed.Attempted ? StepVerdict.Ok : StepVerdict.Unchecked,
            typed.AsTraceStep().Verdict);
    }

    [Fact]
    public void Clicking_carries_the_reason_the_case_had_to_write()
    {
        // A label resolves and offers no Invoke, which is why claude-tray's navigation clicked one.
        // The reason is in the route, so a report says why this act was not a pattern act.
        var clicked = Synthesised.Click(On("Text[name=\"a label\"]"), PointerReason.PointerIsTheAct);

        if (BusyDesk.Excused(clicked.Needed!))
            return;

        Assert.Contains("synthesised pointer", clicked.Pattern, StringComparison.Ordinal);
        Assert.Contains(nameof(PointerReason.PointerIsTheAct), clicked.Pattern, StringComparison.Ordinal);
    }

    [Fact]
    public void A_click_naming_no_reason_this_engine_has_is_refused_with_the_ones_it_does()
    {
        // Refused rather than defaulted: a click whose justification defaults is a click nobody had
        // to justify, and then every act quietly escalates to the pointer.
        var refused = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            {
              "cases": [
                {
                  "name": "a",
                  "steps": [
                    {
                      "locator": "Text#profileLabel",
                      "act": "click",
                      "with": "because I felt like it",
                      "expect": "Profile"
                    }
                  ]
                }
              ]
            }
            """));

        Assert.Contains("'click' does not take 'because I felt like it'", refused.Because, StringComparison.Ordinal);
        foreach (var reason in Enum.GetValues<PointerReason>())
            Assert.Contains(reason.ToString(), refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_click_carrying_no_reason_at_all_is_refused_where_the_field_would_have_been()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            {
              "cases": [
                {
                  "name": "a",
                  "steps": [ { "locator": "Text#profileLabel", "act": "click", "expect": "Profile" } ]
                }
              ]
            }
            """));

        Assert.Contains("carries none", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void Tab_moves_the_focus_off_the_box_and_a_case_can_say_so()
    {
        // WW78's second assertion, and the one that could not be written at all: the seven readings
        // were about patterns, and what holds the focus is not one. Written the way a case would —
        // the locator is the element the step is about, the key goes to its window, and the reading
        // asks whether that element still has the focus.
        var typed = Synthesised.Type(On("Edit[order=top]"), "epsilon");
        if (BusyDesk.Excused(typed.Needed!))
            return;

        // Typing focuses it, so the reading before the key is 'focused' and after it is not.
        Assert.Equal("focused", ReadBack.Named("focused").Of(On("Edit[order=top]").Read()));

        var pressed = Synthesised.Press(On("Edit[order=top]"), TraversalKey.Tab);
        if (BusyDesk.Excused(pressed.Needed!))
            return;

        Assert.Equal("not focused", ReadBack.Named("focused").Of(On("Edit[order=top]").Read()));
    }

    [Fact]
    public void The_focus_reading_answers_nothing_where_nothing_resolved()
    {
        // Null and not "not focused", exactly as the seven pattern readings answer: an element that
        // is not there holds no focus and does not fail to hold it either, and answering the second
        // would be an expectation met by an absence.
        Assert.Null(ReadBack.Named("focused").Of(On("Edit#thereIsNoSuchBox").Read()));
    }

    [Fact]
    public void A_key_no_traversal_has_is_refused_where_the_author_wrote_it()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            {
              "cases": [
                {
                  "name": "a",
                  "steps": [
                    {
                      "locator": "Edit#profileBox",
                      "act": "press",
                      "with": "Enter",
                      "expect": "not focused",
                      "reads": "focused"
                    }
                  ]
                }
              ]
            }
            """));

        Assert.Contains("'press' does not take 'Enter'", refused.Because, StringComparison.Ordinal);
        Assert.Contains("Tab", refused.Because, StringComparison.Ordinal);
        Assert.Contains("ShiftTab", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void The_vocabulary_says_which_acts_a_busy_desk_can_take_away()
    {
        // Data rather than a note, so a report can name the acts in a case that needed the machine.
        // WW254: 'pick' is here although it tries the selection pattern first, because the flag
        // answers whether a busy desk can take the act away and for that one it can — the keyboard
        // fallback exists precisely because the pattern route sometimes refuses.
        // WW259: 'open submenu' joins them, and it is the least ambiguous member of the set — the whole
        // act is a keypress at a menu, so a desk that refused the foreground took the gesture entirely.
        // WW258: 'open tray menu' too. Its route is focus and the application key, so a desk that
        // refused the focus took the whole act — and there is no pointer half to fall back to, since
        // a synthesised right-click opens nothing at all on this shell.
        Assert.Equal(
            ["type", "click", "nudge", "press", "pick", "pick at", "open submenu", "open tray menu"],
            ActVerb.All.Where(one => one.Synthesises).Select(one => one.Name));

        Assert.All(
            ActVerb.All.Where(one => !one.Synthesises),
            one => Assert.False(one.Synthesises, $"'{one.Name}' asks the control and needs no foreground"));
    }

    [Fact]
    public void A_synthesised_act_is_attempted_once_because_a_repeat_is_a_second_keystroke()
    {
        // The same rule 'toggle' is not repeatable under: typing twice into a box is not typing once,
        // and a retried click is a second press the case never asked for.
        Assert.All(
            ActVerb.All.Where(one => one.Synthesises),
            one => Assert.False(one.Repeatable, $"'{one.Name}' would be sent twice by a retry"));
    }
}

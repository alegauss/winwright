using System.Collections.ObjectModel;

using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>Why a case that asks a desk-dependent reading needs no excuse for the desk.</summary>
internal enum Unaffected
{
    /// <summary>The verb refuses at the door and the desk is never reached — and that refusal is
    /// the whole assertion.</summary>
    Refused,

    /// <summary>The route under test asks the control, or the reading is answered from what the
    /// case handed in. Nothing is read off the desk at all.</summary>
    NoDesk,

    /// <summary>The case takes the desk away itself, so a desk that refuses is the condition under
    /// test rather than an accident of the machine.</summary>
    Provoked,

    /// <summary>The assertion holds for whatever came back, with both arms written out. A desk that
    /// refused is one of the answers this case is about.</summary>
    Shape,

    /// <summary>The case returns where the desk was not what it needed, before asserting anything
    /// about it — through a reading of its own rather than through <c>BusyDesk</c>.</summary>
    Guarded,

    /// <summary>
    /// The second fact is the same act's, asked a line later. WW462: a case that excuses a menu walk
    /// and then reads what it highlighted is asking about the focus where it excused the foreground,
    /// and WW197 is right that those are two facts — what makes the second safe is that the excuse it
    /// already has covers the act both of them come from. A desk lost between the two lines is
    /// WW155's window, which the walk's own second guard is for and which no excuse here would close.
    /// </summary>
    TheSameReading,
}

/// <summary>
/// One call a case can make that depends on the desk, and which of this engine's desk facts it is.
/// </summary>
/// <param name="Call">The call as the sources spell it, up to and including its bracket.</param>
/// <param name="Fact">The desk fact it can turn on, exactly as <see cref="DeskFacts" /> names it.</param>
/// <param name="Because">Why that call is the desk's and not the application's.</param>
internal sealed record DeskCall(string Call, string Fact, string Because)
{
    public override string ToString() => $"{Call,-34} {Because}";
}

/// <summary>
/// One case that makes such a call and does not excuse the desk, with why it does not have to.
/// </summary>
/// <param name="Case">The case, as <c>TypeTests.Method_name</c>.</param>
/// <param name="Kind">Which way it is unaffected.</param>
/// <param name="Because">Why, in the words the pairing is read in.</param>
internal sealed record DeskAsk(string Case, Unaffected Kind, string Because)
{
    public override string ToString() => $"{Kind,-9} {Case}: {Because}";
}

/// <summary>
/// WW190. <c>BusyDesk</c> is how this suite answers a desk it did not get, and WW172 applied it to
/// every case that needed it at the time. What it could not do is make the next case use it.
/// <para>
/// Measured rather than counted: holding the guest's desk after WW179 shipped produced nine
/// failures and every one was this — a case asking the shell, the foreground or the focus for a
/// verdict and reporting the answer as a defect in the code under test. Three of the nine were
/// written after WW172 by somebody who had just applied the rule elsewhere. The rest predate it and
/// were never reached. Each costs a three-and-a-half-minute run to find, and that is the cheap
/// outcome; the expensive one is the same case passing on a quiet desk for a month.
/// </para>
/// <para>
/// So the same shape <c>Deadlines</c> and <c>Sleeps</c> already use. The sources are read for every
/// case that asks one of the calls below for a verdict; what it excused is subtracted; every fact
/// left over is paired here with why the desk cannot reach it. Both directions, so a pairing for a
/// case that has since been fixed is red as well.
/// </para>
/// <para>
/// WW197 moved the unit. This counted a case as excused where its body mentioned <c>BusyDesk</c>
/// anywhere, which is the wrong unit by exactly the margin WW190 was about — a case that excused its
/// fixture and then asserted a shell fact was credited for both, and one of them went red on a desk
/// somebody had left a flyout open on. An excuse now covers the fact of the reading it names, and
/// nothing else. Moving it found five more cases owing a reason and one owing a repair.
/// </para>
/// <para>
/// The calls are the judgement and are not pretended to be mechanical. What makes the list checkable
/// is the other end: each names a fact <c>DeskFacts</c> declares, so a call classified against a
/// condition this engine does not call the desk's is caught rather than believed.
/// </para>
/// </summary>
internal static class DeskAsks
{
    /// <summary>What a case has to call for the desk to be able to answer it.</summary>
    internal static IReadOnlyList<DeskCall> Calls { get; } = new ReadOnlyCollection<DeskCall>(
    [
        // --- input, which lands where the desk says it lands ---------------------------------------
        new("Pointer.Click(", Winwright.Windowing.Foreground.PreconditionName,
            "synthesised input goes to whoever holds the foreground, and Windows grants that to "
                + "whoever it grants it to"),
        new("Pointer.DoubleClick(", Winwright.Windowing.Foreground.PreconditionName,
            "the same act twice over, and the same question about where it lands"),
        new("Pointer.Run(", Winwright.Windowing.Foreground.PreconditionName,
            "the door both of the above go through, called directly where a case builds the act"),
        new("Keyboard.Type(", Winwright.Acting.Keyboard.FocusPreconditionName,
            "a key goes where the focus is, and the focus is the desk's to give"),
        new("Keyboard.Run(", Winwright.Acting.Keyboard.FocusPreconditionName,
            "the same, where a case builds the act rather than asking for the shorthand"),
        new("Pick.Value(", Winwright.Windowing.Foreground.PreconditionName,
            "the keyboard route walks the list with real keystrokes, so it needs the desktop the "
                + "pattern route does not"),
        new("Pick.At(", Winwright.Windowing.Foreground.PreconditionName,
            "the same walk counted by position rather than by value, and the same keystrokes under it"),
        new("Selecting.Confirmed(", Winwright.Windowing.Foreground.PreconditionName,
            "it may escalate to a click, which its own parameter makes a caller say yes to — and a "
                + "declared act needing a real desktop is one the desk can refuse"),

        // --- WW462, and the reason this whole block was short by fifteen. The sweep that keeps this
        // list honest walked one file at a time, so every verb below reached the desk through a call
        // into another file and was invisible to it: the synthesised acts go through `Keys` and
        // `Pointer`, and the menu walk asks `Foreground.Check`. Each is the same claim as the six
        // above — input lands where the desk says it lands — arrived at one indirection later.
        new("Synthesised.Type(", Winwright.Acting.Keyboard.FocusPreconditionName,
            "typing through the act a case names, which is the keyboard route with a read-back on it"),
        new("Synthesised.Press(", Winwright.Acting.Keyboard.FocusPreconditionName,
            "a chord sent at whatever holds the focus, which is the desk's to give"),
        new("Synthesised.Nudge(", Winwright.Acting.Keyboard.FocusPreconditionName,
            "an arrow key at the focused control, and an arrow key is a real keystroke"),
        new("Synthesised.Click(", Winwright.Windowing.Foreground.PreconditionName,
            "the pointer act a case names, carrying the reason the pattern route was not taken"),
        new("Synthesised.Pick(", Winwright.Windowing.Foreground.PreconditionName,
            "picking by value, which walks the list with keys where the pattern will not answer"),
        new("Synthesised.PickAt(", Winwright.Windowing.Foreground.PreconditionName,
            "the same by position, and the same walk under it"),
        new("Synthesised.ExpandMenu(", Winwright.Windowing.Foreground.PreconditionName,
            "Right at an open menu, which goes to whoever holds the foreground — and WW457 widened "
                + "what counts as holding it for a menu without widening who grants it"),
        new("Traversal.Nudge(", Winwright.Acting.Keyboard.FocusPreconditionName,
            "a traversal key by another name, and `Traversal.Press` beside it has said so since WW208"),
        new("Menu.Enter(", Winwright.Windowing.Foreground.PreconditionName,
            "F10 at the window under test, which is the keystroke a menu bar opens to"),
        new("Menu.To(", Winwright.Windowing.Foreground.PreconditionName,
            "Down until an entry is highlighted, each one a key the desk has to deliver"),
        new("Menu.Expand(", Winwright.Windowing.Foreground.PreconditionName,
            "Right at the highlighted entry, and the walk answers a hole where the desk refused"),

        // --- the readings that are about the desk itself ------------------------------------------
        new("Menu.Highlighted(", Winwright.Acting.FocusReading.Named,
            "what a menu is showing as highlighted, which WW457 made the menu's own reading and is "
                + "still the focus where the focus is an entry"),
        new("Menu.RaisedFrom(", Winwright.Windowing.Foreground.PreconditionName,
            "which window of a menu's own thread holds the desk, which is a reading of the desk "
                + "however narrow the question"),
        new("Foreground.Now(", Winwright.Windowing.Foreground.PreconditionName,
            "who holds the keyboard right now, which on a locked or blank session is nobody"),
        new("Focus.In(", Winwright.Acting.FocusReading.Named,
            "what holds the focus, which may be another application's or may be nothing at all"),
        new("Focus.Held(", Winwright.Acting.FocusReading.Named,
            "the same reading with the sentence dropped"),
        new("Traversal.WhoHasFocus(", Winwright.Acting.FocusReading.Named,
            "what holds the focus anywhere on the desk, which this list did not know about until a "
                + "case asserting on it went red twice on a loaded guest"),
        new("Traversal.Press(", Winwright.Acting.Keyboard.FocusPreconditionName,
            "a traversal key is a real keystroke, so where it lands is the focus's business — and "
                + "WW208's reading of the engine found this missing here too, on the same day"),
        new("ForeignInput.Read(", Winwright.Windowing.ForeignInput.PreconditionName,
            "whether somebody was using the machine, which is the machine's business"),
        new("Obstruction.Reading(", Winwright.Capturing.Obstruction.PreconditionName,
            "what is standing over a rectangle, which anything on the desk may be"),
        new("RegionThroughout.Around(", Winwright.Capturing.Obstruction.PreconditionName,
            "the same reading twice, either side of a take"),
        new("Foreground.Check(", Winwright.Windowing.Foreground.PreconditionName,
            "whether a named window holds the keyboard, which on a locked session nothing does"),
        new("Preamble.CameForward(", Winwright.Windowing.Foreground.PreconditionName,
            "WW471. The closing look at the same condition, which is a reading and not a "
                + "composition: what it answers is whose the desk was when the run ended"),
        new("Foreground.Waited(", Winwright.Windowing.Foreground.PreconditionName,
            "the same question waited out, which WW470 gave the acts — a desk that never comes is "
                + "the same absence arrived at later, so a case asking this owes the same excuse"),

        // --- the shell, which is another process and always there until it is not ------------------
        new("NotificationArea.Tray(", Winwright.Acting.TraySearch.PreconditionName,
            "the taskbar belongs to the shell, and a shell that is restarting has none"),
        new("NotificationArea.Showing(", Winwright.Acting.TraySearch.PreconditionName,
            "what is on the taskbar, which is whatever this desk happens to be running"),
        // WW197 moved this one. What is in the overflow is empty until the shell has opened it, so
        // the fact it turns on is the flyout and never the search — and a case that excused the
        // flyout and then read its icons was being asked to excuse a second fact for one reading.
        new("NotificationArea.Hidden(", Winwright.Acting.OverflowState.PreconditionName,
            "what is in the overflow, which is empty until the shell has opened it"),
        new("NotificationArea.Chevron(", Winwright.Acting.TraySearch.PreconditionName,
            "the button that opens the overflow, absent on a taskbar something is covering"),
        new("NotificationArea.Overflow(", Winwright.Acting.OverflowState.PreconditionName,
            "the flyout, which is on the screen only while the shell is showing it"),
        new("NotificationArea.OpenOverflow(", Winwright.Acting.OverflowState.PreconditionName,
            "the shell decides whether the flyout opens, and a covered taskbar has no chevron to "
                + "open it with"),
        new("NotificationArea.CloseOverflow(", Winwright.Acting.OverflowState.PreconditionName,
            "the same decision, the other way round"),
        new("NotificationArea.Find(", Winwright.Acting.TraySearch.PreconditionName,
            "a search that could not open the overflow looked at the taskbar alone, which is a "
                + "different answer from the icon being absent"),
        new("NotificationArea.Reachable(", Winwright.Acting.TraySearch.PreconditionName,
            "whether there is a notification area on this desk at all, which WW190 added and which "
                + "is the reading most of the tray cases excuse on"),
        new("NotificationArea.Placing(", Winwright.Acting.TraySearch.PreconditionName,
            "whether this shell is placing icons at all, which WW217 added for the case that has "
                + "just failed to find its own — and a shell that places none is the same absence "
                + "Reachable names, asked one question wider"),
        new("NotificationArea.ElementFor(", Winwright.Acting.TraySearch.PreconditionName,
            "the element behind an icon, which the shell may have moved between the two calls"),
        new("NotificationArea.OpenMenu(", Winwright.Acting.TrayMenu.PreconditionName,
            "the route to a tray menu is focus and then the application key, and a desk that gives "
                + "neither stops the act before it starts"),
    ]);

    /// <summary>Every case that asks and does not excuse, with why the desk cannot reach it.</summary>
    internal static IReadOnlyList<DeskAsk> Known { get; } = new ReadOnlyCollection<DeskAsk>(
    [
        // --- the verb refuses at the door ----------------------------------------------------------
        new("AdmissionTests.Every_verb_that_acts_refuses_the_same_control_for_the_same_reason",
            Unaffected.Refused,
            "all six verbs are asked of a disabled control and all six throw before any of them "
                + "reaches the desk — which is the claim: one door, and no verb with a way round it"),
        new("DestructiveEntryTests.Every_verb_is_refused_because_the_guard_is_the_door_and_not_the_verb",
            Unaffected.Refused,
            "the same shape one guard over: a button declared destructive is refused by every verb, "
                + "and a refusal that happened is not a refusal the desk could have prevented"),
        new("KeyboardTests.A_control_that_reports_no_value_is_refused_rather_than_typed_into",
            Unaffected.Refused,
            "a label has no value to read back, so the act is refused for that and no keystroke is "
                + "ever sent anywhere"),
        new("KeyboardTests.Typing_something_that_is_not_there_is_refused_as_not_in_the_tree",
            Unaffected.Refused,
            "the locator matches nothing, so there is no element to hold the focus this would need"),
        new("PickTests.A_value_the_picker_does_not_hold_is_refused_with_the_ones_it_does",
            Unaffected.Refused,
            "the picker is asked for a value it does not have and refuses with the ones it does, "
                + "which is read off the control and never off the screen"),
        new("ShutPickerTests.A_value_a_shut_picker_does_not_hold_is_refused_with_the_ones_it_does",
            Unaffected.Refused,
            "the same refusal against a WPF picker, which WW265 opens first so the list it refuses "
                + "with is the five values it holds rather than the nothing a shut one shows"),
        new("PointerTests.A_click_count_of_nothing_is_refused",
            Unaffected.Refused,
            "zero clicks is refused as an argument, before the act has a foreground to want"),
        new("PointerTests.A_pointer_act_still_needs_the_element_to_be_there_and_on_screen",
            Unaffected.Refused,
            "the locator matches nothing, so the refusal is about the tree and the desk was never "
                + "asked for the desktop"),

        // --- nothing is read off the desk ----------------------------------------------------------

        // WW265's two, and they are NoDesk by assertion rather than by hope. The selection pattern
        // needs no foreground, and each case asserts the route it took — so a fall-through to the
        // keyboard is a red naming the route rather than a reading blamed on the control.
        new("ShutPickerTests.A_shut_picker_is_walked_and_left_shut",
            Unaffected.NoDesk,
            "the walk takes the selection pattern once the picker has been opened, which asks the "
                + "control; the case asserts that route, so nothing here is answered by the desk"),
        new("ShutPickerTests.A_picker_already_open_is_left_open",
            Unaffected.NoDesk,
            "the same pattern route against a picker that was already dropped down, and what is "
                + "asserted beside it is that the walk left it that way"),

        new("CheckoutTests.A_line_is_read_as_code_and_never_as_what_somebody_wrote_about_it",
            Unaffected.NoDesk,
            "it names a tray call as text and never makes one — and it is here rather than invisible "
                + "because the line proving that an unpaired quote is left whole is itself a line "
                + "with an unpaired quote, which the stripper leaves whole. That is the trade-off "
                + "written down: reading too much is a red somebody answers, and this is somebody "
                + "answering it"),
        new("ThroughoutTests.A_reading_that_was_never_taken_does_not_refuse_a_capture_either",
            Unaffected.NoDesk,
            "the window handle is zero, so both readings are refused for that before anything looks "
                + "at what is standing anywhere"),
        new("ThroughoutTests.The_take_runs_between_the_two_readings_and_not_before_them",
            Unaffected.NoDesk,
            "the same handle of zero, and what is asserted is the order the take ran in — which is "
                + "about the door and not about the desk it would have read"),
        new("FixtureTests.A_reading_that_was_never_taken_does_not_refuse_a_capture_either",
            Unaffected.NoDesk,
            "the region handed in has no area, so the reading is refused for that and never looks "
                + "at what is standing anywhere"),
        new("FixtureTests.A_region_with_no_area_and_a_window_that_is_nothing_are_readings_not_taken",
            Unaffected.NoDesk,
            "the two absences, both of them in the arguments: a region of no area and a window "
                + "handle of zero. Neither reaches the desk to be affected by it"),
        new("FocusScopeTests.A_window_that_names_no_process_is_said_rather_than_read_as_agreement",
            Unaffected.NoDesk,
            "the handle is zero, which names no process, so the reading answers that and never asks "
                + "what holds the focus"),
        new("PickTests.The_pattern_route_is_one_switch_and_the_answer_says_so",
            Unaffected.NoDesk,
            "the pattern route asks the control to select and never synthesises a keystroke, which "
                + "is the whole reason it is the default"),

        // --- the case takes the desk away itself ---------------------------------------------------
        new("ActTests.A_toggle_lands_while_the_foreground_belongs_to_another_window",
            Unaffected.Provoked,
            "the foreground is given away on purpose and then asserted to be elsewhere, which is the "
                + "premise of the case: a pattern act lands without it"),
        new("FocusScopeTests.A_walk_whose_focus_left_the_application_is_unchecked_rather_than_red",
            Unaffected.Provoked,
            "the reading it leaves unexcused is taken against the desktop window on purpose, so it "
                + "is never inside the application whatever the desk is doing — the walk's own "
                + "reading, which the desk can refuse, is excused"),
        new("FixtureTests.A_capture_whose_region_was_stood_over_is_refused_and_names_the_intruder",
            Unaffected.Provoked,
            "--intrude puts a window of this run's own over the rectangle, so the obstruction is "
                + "arranged rather than waited for"),
        new("FixtureTests.An_intruder_in_the_way_of_nothing_takes_none_of_the_region",
            Unaffected.Provoked,
            "the same flag placing the intruder well clear, and the assertion is about that one "
                + "handle — anything else the desk is holding up is somebody else's window"),
        new("FixtureTests.The_area_it_answers_with_is_the_area_the_intruder_actually_takes",
            Unaffected.Provoked,
            "the overlap is worked out from the two rectangles the case placed, and the bounds "
                + "either side of it hold whatever else is on the desk"),
        new("FixtureTests.The_region_check_names_the_intruder_rather_than_answering_whether",
            Unaffected.Provoked,
            "the intruder is this run's, named by pid, so a desk with other windows over the region "
                + "adds to the reading and never removes the one being asserted"),
        new("FocusScopeTests.A_focus_that_is_not_this_applications_is_a_hole_and_never_a_failure",
            Unaffected.Provoked,
            "the reading is taken against the desktop window on purpose, so it is never inside the "
                + "application under test whatever the desk is doing"),
        new("PickTests.The_pattern_route_needs_no_desktop_and_the_keyboard_one_says_when_it_had_none",
            Unaffected.Provoked,
            "a decoy takes the desktop away before the keyboard route is asked, and the assertion "
                + "is that the route says it had none"),
        new("PointerTests.Input_sent_nowhere_is_a_hole_in_the_trace_rather_than_a_step_that_ran",
            Unaffected.Provoked,
            "the same decoy, and the trace step is asserted to be a hole — which is the answer a "
                + "refused desk produces"),
        new("RefusedForegroundTests.A_hole_carries_the_precondition_rather_than_a_sentence_about_it",
            Unaffected.Provoked,
            "the decoy again, and what is asserted is that the hole carries the condition rather "
                + "than a sentence about it"),
        new("RefusedForegroundTests.A_traversal_that_could_not_be_sent_is_a_hole",
            Unaffected.Provoked,
            "the decoy once more, for a traversal key rather than a click or a type — and what is "
                + "asserted is the hole it produces, naming the foreground as the reason"),
        new("RefusedForegroundTests.Typing_that_could_not_be_sent_is_a_hole_too",
            Unaffected.Provoked,
            "the decoy for the keyboard rather than the pointer, asserting the same hole"),

        // --- both arms are written out -------------------------------------------------------------
        new("ForegroundTests.A_window_that_cannot_hold_the_foreground_reads_as_not_holding_it",
            Unaffected.Shape,
            "the window is created hidden and never activated, so it cannot be the foreground on any "
                + "desk — what is asserted is a negative that holds whoever does hold it"),
        new("ForegroundTests.Once_is_the_single_look_this_type_has_always_taken",
            Unaffected.Shape,
            "WW470. The same hidden window, and the same negative: whoever holds the desk, this one "
                + "does not — what is asserted is that asking with `Once` cost no wait at all"),
        new("ForegroundTests.A_desk_that_never_comes_still_holes_and_the_absence_says_how_long_it_waited",
            Unaffected.Shape,
            "WW470. The hidden window again, so the wait is certain to run out whoever is at the "
                + "machine — which is the arm this case is about: a wait that turned that hole into "
                + "a pass would be the defect, and the absence says how long it stood"),
        new("NotificationAreaTests.Whether_this_desk_places_icons_at_all_is_a_different_question_from_whether_one_is_there",
            Unaffected.Shape,
            "WW217. The reading itself is what this case is about, and both arms are written out: a "
                + "desk called placing must hold an icon somewhere, and one that is not says which "
                + "part refused. Excusing on the fact it exists to read would leave nothing asserted"),
        new("NotificationAreaTests.Putting_the_desk_back_answers_for_the_desktop_and_not_only_the_flyout",
            Unaffected.Shape,
            "WW344. The foreground is read to check the verb's own reading of it against an "
                + "independent look, so both sides move together: a desk that refused the restore "
                + "makes the reading say it did, and the assertion is that the two agree. A desk "
                + "that would not give the desktop back is one of the answers this case is about, "
                + "and excusing it would leave the half nobody hears about unasserted again"),
        new("NotificationAreaTests.Asking_whether_the_desk_places_icons_leaves_the_taskbar_as_it_found_it",
            Unaffected.Shape,
            "the flyout is read either side and the two are compared, so whatever the shell was "
                + "showing is what the reading has to leave it showing — which holds on a desk that "
                + "would not open it at all, and is the same claim WW197 made literal one case down"),
        new("TraySubjectTests.A_tray_step_leaves_the_overflow_the_way_it_found_it",
            Unaffected.Shape,
            "WW258, and the same claim as the two above it: the flyout is read either side and the "
                + "two are compared, so a desk that would not open one at all still satisfies it. The "
                + "step under test opened one and walked away, which is what this now refuses"),
        new("TrayPlacementTests.The_fixture_leaves_the_overflow_the_way_it_found_it",
            Unaffected.Shape,
            "WW197 made the claim literal: the flyout is read either side and the two are compared, "
                + "so whatever the shell was showing is what the fixture has to leave it showing. It "
                + "asserted the flyout was shut until then, and went red on a desk where it was not"),
        new("FixtureTests.A_person_can_ask_for_the_window_to_come_forward",
            Unaffected.Shape,
            "what is asserted about the foreground is a negative — the quiet launch is not holding "
                + "it — and that is true of every desk, including one holding nothing"),
        new("ForeignInputTests.A_reading_that_is_alone_offers_no_elapsed_time_to_quote",
            Unaffected.Shape,
            "the claim is made only where the reading came back alone, which is the arm it is about"),
        new("ForeignInputTests.The_absence_names_the_person_and_never_the_foreground",
            Unaffected.Shape,
            "the word that must not appear is asserted absent either way, and the naming is asserted "
                + "only on the arm that has somebody to name"),
        new("ForeignInputTests.The_precondition_agrees_with_the_reading_that_produced_it",
            Unaffected.Shape,
            "an equality between the flag and the condition, which holds whichever they are, and a "
                + "sentence demanded only where the condition is absent"),
        new("ForeignInputTests.The_sentence_and_the_flag_never_disagree",
            Unaffected.Shape,
            "one equality over both arms: whatever the desk was doing, the two halves of the reading "
                + "say the same thing about it"),
        new("ForeignInputTests.Watching_again_forgets_what_came_before",
            Unaffected.Shape,
            "the typing is there to leave something behind and its verdict is never asserted on — "
                + "what is asserted is that watching again forgot it, which a refused desk did too"),
        new("NotificationAreaTests.A_window_taking_the_foreground_shuts_the_flyout_under_whoever_was_looking_in_it",
            Unaffected.Provoked,
            "WW288. The foreground moving is the provocation this measures rather than a condition "
                + "it needs: the case takes the desk itself, reads who ended up with it, and returns "
                + "naming the desk where the dialog did not take it — so an excuse would be an "
                + "excuse for the event under test. WW411: what it does excuse is the other side, "
                + "the flyout gone before anything provoked it, which is a measurement that never "
                + "started rather than the one this makes"),
        new("NotificationAreaTests.A_shell_that_will_not_work_the_flyout_is_a_hole_naming_what_it_was",
            Unaffected.Shape,
            "the case exists for the refused arm: it asserts the pass where the flyout opened and "
                + "the hole where it did not, and excusing either would delete half of it"),
        new("ThroughoutTests.A_region_read_once_says_so_rather_than_standing_for_both_ends",
            Unaffected.Shape,
            "every claim here is about the shape of a reading taken once — that it did not look "
                + "twice, that it will not say the region held still, that its Finding observed "
                + "nothing — and all of that is true of whatever the one reading found"),
        new("RefusedForegroundTests.The_verdict_tracks_the_desk_rather_than_the_other_way_round",
            Unaffected.Shape,
            "stated as the invariant rather than by insisting the window takes the foreground — the "
                + "verdict is a hole exactly where the desk was elsewhere, and checked either way"),

        // --- the case reads the desk and returns ---------------------------------------------------
        new("FixtureTests.A_capture_whose_region_was_clear_carries_the_reading_that_says_so",
            Unaffected.Guarded,
            "the intruder is placed well clear, and the case returns where the reading came back "
                + "covered anyway — a desk holding a window over the fixture has nothing clear to "
                + "carry, and the receipt would refuse rather than answer"),
        new("AdoptedTrayTests.A_menu_that_shuts_itself_is_still_standing_for_the_step_that_reads_it",
            Unaffected.Guarded,
            "WW461. The only call here that touches the flyout is the tidy in the finally, after "
                + "everything has been asserted — what the case turns on is the run's own verdict, "
                + "and it stands down before that on all three readings it does depend on: the "
                + "notification area being reachable, the icon being placed, and the engine's own "
                + "unchecked count where the desk would not put the menu up"),
        new("NotificationAreaTests.A_case_reads_the_menu_a_step_opened_and_the_case_hands_the_desk_back",
            Unaffected.Guarded,
            "WW343. The same guard as the case below and for the same reason: the foreground is read "
                + "to say the case did not leave the taskbar holding it, and the case returns before "
                + "that assertion where the taskbar was already holding it before the run began. "
                + "What the engine could not reach is excused a floor up — a case whose verdict "
                + "carries any hole stands down rather than asserting about a shell that would not "
                + "put the menu up"),
        new("NotificationAreaTests.A_menu_that_never_came_leaves_the_taskbar_as_it_found_it",
            Unaffected.Guarded,
            "WW330. The foreground is read to say the act did not leave the taskbar holding it, and "
                + "the case returns before that assertion where the taskbar was already holding it "
                + "when the act began — a shell that had the desk anyway is not something this act "
                + "took, and asserting on it would report the desk as the defect. Its other two "
                + "facts are excused rather than reasoned about, which is the half a reader checks "
                + "first"),

        // --- WW462, and none of these is new: the sweep that keys this list walked one file, so the
        // synthesised acts and the menu walk were in no catalogue and no case was ever asked about
        // them. Each was reasoned about once here, at the moment the question first arrived.

        // The verb refuses at the door, and the refusal is the assertion.
        new("NudgeTests.A_range_with_no_room_at_all_is_refused_rather_than_reported_as_unmoved",
            Unaffected.Refused,
            "a range that accepts three values and holds three is one no nudge could prove anything "
                + "about, so the verb throws before a key is anywhere near the desk — and the "
                + "sentence it throws with is the whole of what this case reads"),
        new("PickVerbTests.A_value_the_picker_does_not_hold_is_refused_by_the_verb_too",
            Unaffected.Refused,
            "the picker is asked for a value it does not have and the verb refuses at the door, with "
                + "the count in the sentence. Nothing is sent, so there is no desk in it"),
        new("TraversalTests.Nudging_something_with_no_range_is_refused", Unaffected.Refused,
            "a text box offers no range pattern, so the act is refused for the pattern rather than "
                + "attempted and missed — which is the claim, and it is made before any key exists"),
        new("ShutPickerTests.A_position_the_picker_does_not_have_is_refused_with_how_many_it_holds",
            Unaffected.Refused,
            "position five of a five-value picker, refused with how many it holds — the arithmetic is "
                + "done on what the control reports and no route to the desk is opened"),
        new("SelectingTests.Selecting_something_that_is_not_there_is_refused_before_any_of_this",
            Unaffected.Refused,
            "an item no picker holds, refused before the pattern route is tried and long before the "
                + "pointer is allowed to be considered — which is what 'before any of this' names"),
        new("SynthesisedActTests.An_act_whose_locator_matched_nothing_reports_it_rather_than_throwing",
            Unaffected.Refused,
            "the locator matched nothing, so the act is in no window a key could be sent to and says "
                + "so through `Needed`. WW321: it reports rather than throwing, and the report is the "
                + "assertion — a desk that was fine would answer the same"),
        new("SynthesisedActTests.The_trace_line_for_an_act_against_nothing_says_so_instead_of_throwing",
            Unaffected.Refused,
            "the same act against nothing, read as a trace step: unchecked, resolving nothing, and "
                + "naming what it did not find. No desk decides any of those three"),
        new("MenuTests.An_element_in_no_window_is_a_hole_rather_than_a_menu_that_refused",
            Unaffected.Refused,
            "an element in no window is one no menu key could be aimed at, and the absence says that "
                + "rather than naming the foreground. What this asserts is which absence came back, "
                + "which is the same on any desk"),

        // The route under test asks the control, and nothing is read off the desk.
        new("SelectingTests.The_pattern_route_confirms_and_no_pointer_is_reached_for",
            Unaffected.NoDesk,
            "the claim is that the pattern route confirmed and the pointer was never reached for — "
                + "asserted on the route and on `PointerTried`, both of which are false of a case "
                + "that touched the desk at all"),
        new("SelectingTests.A_confirmation_that_cannot_pass_is_a_red_and_never_a_quiet_green",
            Unaffected.NoDesk,
            "the pointer is refused by the caller, so the only route open is the pattern's and the "
                + "sentence it comes back with says the pointer was not allowed. A desk this run "
                + "never had would change nothing about that"),
        new("SelectingTests.The_second_condition_is_what_says_the_application_agreed_and_not_only_the_control",
            Unaffected.NoDesk,
            "both halves are the pattern's: the first refuses the pointer outright, and the second "
                + "lands because the condition it was given is now true — which the pattern route "
                + "confirms without a pointer being reached for"),
        new("SelectingTests.Nothing_here_reports_a_landing_it_did_not_confirm", Unaffected.NoDesk,
            "both arms assert that nothing landed and that the route was not the pattern's, which is "
                + "a claim about what the verb refuses to say rather than about what a desk allowed"),
        new("ShutPickerTests.A_position_is_reached_without_the_case_naming_what_is_there",
            Unaffected.NoDesk,
            "the route is asserted to be the pattern's and the picker is asserted to have stayed "
                + "collapsed, which is the whole point: a position reached without opening anything "
                + "is a position reached without a desk"),
        new("MenuTests.An_ordinary_window_is_not_a_menu_the_desk_is_raising", Unaffected.NoDesk,
            "WW457's control. The reading stops at the control type before it asks about a thread at "
                + "all, so a window that is not a menu answers zero whoever holds the desk — and zero "
                + "is what this asserts"),

        // The assertion holds for whatever came back.
        new("SynthesisedActTests.A_synthesised_act_always_carries_what_it_needed_whichever_way_it_went",
            Unaffected.Shape,
            "what it asserts is the agreement between two fields — `Needed.Satisfied` and `Attempted` "
                + "— and the trace verdict that follows from them. Both arms are written out, and a "
                + "desk that refused is the arm this case exists to keep honest"),
        new("SelectingTests.The_pointer_is_reached_for_only_after_the_confirmation_did_not_pass",
            Unaffected.Shape,
            "the claim is the order: the pointer was tried, and nothing landed. Whatever the click "
                + "then met — a shut picker, or a desktop held elsewhere — the case asserts a route "
                + "of neither and a sentence, which is what the comment beside it already says"),
        new("AdoptedTrayTests.A_menu_held_open_for_the_harness_takes_no_key_and_the_verb_ran_all_the_same",
            Unaffected.Shape,
            "the highlight is asserted to be nothing, and a focus this run does not have answers "
                + "nothing too — so the desk cannot make this claim false. The act's own attempt is "
                + "the half that could, and the case reads the foreground and stands down first"),

        // The case takes the desk away itself.
        new("SelectingTests.A_click_with_the_desktop_elsewhere_says_so_rather_than_claiming_a_landing",
            Unaffected.Provoked,
            "a decoy takes the desktop and the case asserts that no landing is claimed — the refused "
                + "desk is the condition under test, and excusing it would excuse the subject"),
        new("RefusedForegroundTests.A_nudge_that_could_not_be_sent_is_a_hole", Unaffected.Provoked,
            "the same decoy, and the claim is the verdict's shape: unchecked, naming the foreground "
                + "as what was missing. A desk that had been granted would fail this case"),
        new("TraversalTests.A_nudge_with_the_desktop_elsewhere_sends_nothing", Unaffected.Provoked,
            "the decoy again, read off the act rather than off a verdict: nothing sent, nothing "
                + "moved, and a sentence that says which"),

        // The second fact is the same act's, asked a line later.
        new("MenuTests.A_walk_that_finds_nothing_leaves_the_menu_open_rather_than_resetting_it",
            Unaffected.TheSameReading,
            "the walk is excused through `BusyDesk` on the line above, and what the highlight is read "
                + "for is that the menu survived a miss — a walk that never ran returns before it"),
        new("MenuTests.Walking_past_a_destructive_entry_highlights_it_and_nothing_more",
            Unaffected.TheSameReading,
            "the same shape, and the highlight read at the end is the claim that highlighting is not "
                + "invoking: the run is still here and Quit is still only highlighted. The walk that "
                + "put it there is excused above"),
        new("CaptureRouteTests.A_real_menu_on_a_real_window_routes_to_the_copy_that_can_reach_it",
            Unaffected.TheSameReading,
            "the highlight is read inside the sentence this case excuses WITH — a shell that put no "
                + "menu window up is named as the absence, with what was highlighted in it. The "
                + "reading is part of the excuse rather than something asserted past one"),
    ]);

    /// <summary>Every case in this suite that makes one of those calls, whether or not it excuses.</summary>
    internal static IReadOnlyList<string> Asking() => scanned.Value.Select(one => one.Case).ToList();

    /// <summary>
    /// The ones asking about a desk fact nothing in them excused, which are what this pairs. WW197:
    /// a case that excused one fact and asked about a second is here for the second.
    /// </summary>
    internal static IReadOnlyList<string> Bare() =>
        scanned.Value.Where(one => one.Missing.Count > 0).Select(one => one.Case).ToList();

    /// <summary>Which facts a case left unexcused, which is what a red here has to say.</summary>
    internal static IReadOnlyList<string> Unexcused(string named) => scanned.Value
        .Where(one => string.Equals(one.Case, named, StringComparison.Ordinal))
        .SelectMany(one => one.Missing)
        .ToList();

    /// <summary>The reading a person gets: the counts first, then a line each.</summary>
    internal static IReadOnlyList<string> Render()
    {
        var asking = scanned.Value.Count;
        var bare = scanned.Value.Count(one => one.Missing.Count > 0);
        return new ReadOnlyCollection<string>(
        [
            $"{asking} case(s) ask a desk-dependent reading: {asking - bare} excuse the desk, "
                + $"{bare} are paired with why they need none",
            .. Known.Select(one => $"  {one}"),
        ]);
    }

    private sealed record Found(string Case, IReadOnlyList<string> Missing, bool Asks);

    private static readonly Lazy<IReadOnlyList<Found>> scanned = new(Scan);

    /// <summary>
    /// The cases, read out of the sources. A case is where the runner's unit is: a class that asks
    /// through a field initialiser has excused nothing for the case that then asserts on it.
    /// </summary>
    private static IReadOnlyList<Found> Scan() => Checkout
        .SourcesIn(Checkout.Suite, except: $"{nameof(DeskAsks)}.cs")
        .SelectMany(InFile)
        .OrderBy(one => one.Case, StringComparer.Ordinal)
        .ToList();

    private static IEnumerable<Found> InFile(string file)
    {
        var found = new List<Found>();
        var owner = "";
        var caseNext = false;
        var open = "";
        var body = new List<string>();

        foreach (var line in File.ReadLines(file))
        {
            if (Checkout.Owner(line) is { } named)
                owner = named;

            if (line.Contains("[Fact]", StringComparison.Ordinal)
                || line.Contains("[Theory]", StringComparison.Ordinal))
            {
                caseNext = true;
            }
            else if (Checkout.Member(line) is { } member)
            {
                Close();
                open = caseNext ? member : "";
                caseNext = false;
            }
            else if (open.Length > 0)
            {
                body.Add(Checkout.Code(line));
            }
        }

        Close();
        return found;

        void Close()
        {
            if (open.Length > 0)
            {
                var asked = Facts(body);
                if (asked.Count > 0)
                    found.Add(new Found($"{owner}.{open}", [.. asked.Except(Excused(body), StringComparer.Ordinal)], true));
            }

            open = "";
            body = [];
        }
    }

    /// <summary>Every desk fact the lines of a case ask about for a verdict.</summary>
    private static IReadOnlyList<string> Facts(IEnumerable<string> body) => body
        .SelectMany(AskedOf)
        .Distinct(StringComparer.Ordinal)
        .ToList();

    /// <summary>The facts one line asks about, which is none for most lines.</summary>
    private static IEnumerable<string> FactsOf(string code) => Calls
        .Where(one => code.Contains(one.Call, StringComparison.Ordinal))
        .Select(one => one.Fact);

    /// <summary>
    /// The facts a line asks <em>for a verdict</em>, which is what this rule is about.
    /// <para>
    /// WW197. A call whose answer is thrown away asks nothing: <c>NotificationArea.CloseOverflow();</c>
    /// on its own line is a case tidying up after itself, and a case cannot report the desk as a
    /// defect through a value it never looked at. The criterion says <em>asks a desk-dependent
    /// reading for a verdict</em>, and a discarded reading is the case where that is plainly false.
    /// </para>
    /// <para>
    /// A statement and not a mention: the line has to <em>start</em> with the call, so anything that
    /// assigns it, asserts on it or reads it inside a condition still asks.
    /// </para>
    /// </summary>
    private static IEnumerable<string> AskedOf(string code)
    {
        var trimmed = code.TrimStart();
        return Calls
            .Where(one => code.Contains(one.Call, StringComparison.Ordinal))
            .Where(one => !trimmed.StartsWith(one.Call, StringComparison.Ordinal))
            .Select(one => one.Fact);
    }

    /// <summary>
    /// Every desk fact a case actually excused, which is the whole of WW197.
    /// <para>
    /// WW190 counted a case as excused where its body mentioned <c>BusyDesk</c> anywhere, and that is
    /// the wrong unit by exactly the margin WW190 was about: a case that excuses its fixture and then
    /// asserts a shell fact was credited for both. Measured — that is what
    /// <c>TrayPlacementTests.The_fixture_leaves_the_overflow_the_way_it_found_it</c> does, and it
    /// went red on a desk whose flyout somebody had left standing.
    /// </para>
    /// <para>
    /// An excuse covers the fact of the reading it names. Where the reading is on the line — as in
    /// <c>BusyDesk.Excused(NotificationArea.Reachable())</c> — that is read directly; where the line
    /// names a variable, the assignment it came from is looked up in the same case, which is the
    /// shape nearly every excuse here is written in. An excuse that names neither covers nothing,
    /// and <c>BusyDesk.Built</c> around a fixture is exactly that: it excuses the fixture and says
    /// nothing about a shell the case reads afterwards.
    /// </para>
    /// </summary>
    private static IReadOnlyList<string> Excused(IReadOnlyList<string> body)
    {
        var covered = new List<string>();
        foreach (var line in Excuses(body))
        {
            covered.AddRange(FactsOf(line));
            foreach (var named in Reading(line))
                covered.AddRange(body.Where(one => Assigns(one, named)).SelectMany(FactsOf));
        }

        return covered.Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Each excuse as one expression, however many lines it was written across.
    /// <para>
    /// WW197's own check found this. An excuse whose reading did not fit on the line — the shape
    /// <c>if (BusyDesk.Excused(</c> then the reading underneath — was read as an excuse naming
    /// nothing, so the case looked as though it had excused a fact it plainly had.
    /// </para>
    /// </summary>
    private static IEnumerable<string> Excuses(IReadOnlyList<string> body)
    {
        for (var at = 0; at < body.Count; at++)
        {
            if (!body[at].Contains("BusyDesk.", StringComparison.Ordinal))
                continue;

            var joined = body[at];
            var depth = Depth(joined);
            for (var next = at + 1; next < body.Count && depth > 0; next++)
            {
                joined = $"{joined} {body[next].Trim()}";
                depth += Depth(body[next]);
            }

            yield return joined;
        }
    }

    /// <summary>How far one line opens or closes the brackets around it.</summary>
    private static int Depth(string code) => code.Count(one => one == '(') - code.Count(one => one == ')');

    /// <summary>
    /// The variables an excuse line reads a verdict off — <c>opened</c> in
    /// <c>BusyDesk.Excused(opened.AsAssertion(...))</c>. Named by what follows them rather than by a
    /// list of variable names, because the reading is what makes one of these a reading.
    /// </summary>
    private static IEnumerable<string> Reading(string line)
    {
        // WW462 added the fourth, and it is WW197's finding one shape over: an act carries what it
        // needed in `Needed`, so `BusyDesk.Excused(typed.Needed!)` is how every synthesised act in
        // this suite is excused — and this could not see it. The identifier before `)` is `Needed`
        // where the null-forgiving `!` does not stop the walk first, and either way what came back
        // was never the variable the assignment is under.
        //
        // It cost nothing until the sweep that keys this list crossed files: the acts were in no
        // catalogue, so no case was asked about them, and twenty-two cases that plainly excuse the
        // desk were about to be written down as cases that do not.
        foreach (var answer in (string[])["AsAssertion", "AsPrecondition", "AsFinding", "Needed"])
        {
            if (Before(line, $".{answer}") is { } named)
                yield return named;
        }

        // And the shorter spelling, where a case already holds the verdict: BusyDesk.Excused(result).
        // Found by WW197's own check, which reported a case that excuses on the line above the
        // assertion it excuses — the reading was there and this could not see it.
        if (Before(line, ")", after: "BusyDesk.Excused(") is { } held)
            yield return held;
    }

    /// <summary>The identifier immediately before a marker, where one is there.</summary>
    private static string? Before(string line, string marker, string? after = null)
    {
        var from = 0;
        if (after is not null)
        {
            var opens = line.IndexOf(after, StringComparison.Ordinal);
            if (opens < 0)
                return null;

            from = opens + after.Length;
        }

        var at = line.IndexOf(marker, from, StringComparison.Ordinal);
        if (at <= 0)
            return null;

        var began = at;
        while (began > 0 && (char.IsLetterOrDigit(line[began - 1]) || line[began - 1] == '_'))
            began--;

        return began < at && began >= from ? line[began..at] : null;
    }

    /// <summary>Whether a line is where that variable got its value.</summary>
    private static bool Assigns(string line, string named)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith($"var {named} =", StringComparison.Ordinal)
            || trimmed.StartsWith($"{named} =", StringComparison.Ordinal);
    }

    /// <summary>The name a declaration introduces, where the line is one.</summary>

    /// <summary>
    /// The member a line declares, at the one indentation a case sits at. Anything deeper is a
    /// local function or a lambda and belongs to the case above it.
    /// </summary>

}

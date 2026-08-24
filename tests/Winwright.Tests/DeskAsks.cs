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
/// case that makes one of the calls below; a case that mentions <c>BusyDesk</c> has answered the
/// question and is not listed here; every other one is paired with why the desk cannot reach it.
/// Both directions, so a pairing for a case that has since been fixed is red as well.
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

        // --- the readings that are about the desk itself ------------------------------------------
        new("Foreground.Now(", Winwright.Windowing.Foreground.PreconditionName,
            "who holds the keyboard right now, which on a locked or blank session is nobody"),
        new("Focus.In(", Winwright.Acting.FocusReading.Named,
            "what holds the focus, which may be another application's or may be nothing at all"),
        new("Focus.Held(", Winwright.Acting.FocusReading.Named,
            "the same reading with the sentence dropped"),
        new("ForeignInput.Read(", Winwright.Windowing.ForeignInput.PreconditionName,
            "whether somebody was using the machine, which is the machine's business"),
        new("Obstruction.Reading(", Winwright.Capturing.Obstruction.PreconditionName,
            "what is standing over a rectangle, which anything on the desk may be"),

        // --- the shell, which is another process and always there until it is not ------------------
        new("NotificationArea.Tray(", Winwright.Acting.TraySearch.PreconditionName,
            "the taskbar belongs to the shell, and a shell that is restarting has none"),
        new("NotificationArea.Showing(", Winwright.Acting.TraySearch.PreconditionName,
            "what is on the taskbar, which is whatever this desk happens to be running"),
        new("NotificationArea.Hidden(", Winwright.Acting.TraySearch.PreconditionName,
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
        new("PointerTests.A_click_count_of_nothing_is_refused",
            Unaffected.Refused,
            "zero clicks is refused as an argument, before the act has a foreground to want"),
        new("PointerTests.A_pointer_act_still_needs_the_element_to_be_there_and_on_screen",
            Unaffected.Refused,
            "the locator matches nothing, so the refusal is about the tree and the desk was never "
                + "asked for the desktop"),

        // --- nothing is read off the desk ----------------------------------------------------------
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
        new("RefusedForegroundTests.Typing_that_could_not_be_sent_is_a_hole_too",
            Unaffected.Provoked,
            "the decoy for the keyboard rather than the pointer, asserting the same hole"),

        // --- both arms are written out -------------------------------------------------------------
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
        new("NotificationAreaTests.A_shell_that_will_not_work_the_flyout_is_a_hole_naming_what_it_was",
            Unaffected.Shape,
            "the case exists for the refused arm: it asserts the pass where the flyout opened and "
                + "the hole where it did not, and excusing either would delete half of it"),
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
    ]);

    /// <summary>Every case in this suite that makes one of those calls, whether or not it excuses.</summary>
    internal static IReadOnlyList<string> Asking() => scanned.Value.Select(one => one.Case).ToList();

    /// <summary>The ones that ask and never mention <c>BusyDesk</c>, which are what this pairs.</summary>
    internal static IReadOnlyList<string> Bare() =>
        scanned.Value.Where(one => !one.Excuses).Select(one => one.Case).ToList();

    /// <summary>The reading a person gets: the counts first, then a line each.</summary>
    internal static IReadOnlyList<string> Render()
    {
        var asking = scanned.Value.Count;
        var bare = scanned.Value.Count(one => !one.Excuses);
        return new ReadOnlyCollection<string>(
        [
            $"{asking} case(s) ask a desk-dependent reading: {asking - bare} excuse the desk, "
                + $"{bare} are paired with why they need none",
            .. Known.Select(one => $"  {one}"),
        ]);
    }

    private sealed record Found(string Case, bool Excuses);

    private static readonly Lazy<IReadOnlyList<Found>> scanned = new(Scan);

    /// <summary>
    /// The cases, read out of the sources. A case is where the runner's unit is: a class that asks
    /// through a field initialiser has excused nothing for the case that then asserts on it.
    /// </summary>
    private static IReadOnlyList<Found> Scan() => Trees()
        .SelectMany(Sources)

        // This file names every call it looks for, so scanning itself would find them all.
        .Where(one => Path.GetFileName(one) != $"{nameof(DeskAsks)}.cs")
        .SelectMany(InFile)
        .OrderBy(one => one.Case, StringComparer.Ordinal)
        .ToList();

    private static IEnumerable<Found> InFile(string file)
    {
        var found = new List<Found>();
        var owner = "";
        var caseNext = false;
        var open = (Case: "", Asks: false, Excuses: false);

        foreach (var line in File.ReadLines(file))
        {
            if (Declares(line, "class ") is { } named)
                owner = named;

            if (line.Contains("[Fact]", StringComparison.Ordinal)
                || line.Contains("[Theory]", StringComparison.Ordinal))
            {
                caseNext = true;
            }
            else if (Member(line) is { } member)
            {
                Close();
                open.Case = caseNext ? member : "";
                caseNext = false;
            }
            else if (open.Case.Length > 0)
            {
                open.Asks |= Calls.Any(one => line.Contains(one.Call, StringComparison.Ordinal));
                open.Excuses |= line.Contains("BusyDesk.", StringComparison.Ordinal);
            }
        }

        Close();
        return found;

        void Close()
        {
            if (open.Case.Length > 0 && open.Asks)
                found.Add(new Found($"{owner}.{open.Case}", open.Excuses));

            open = ("", false, false);
        }
    }

    /// <summary>The name a declaration introduces, where the line is one.</summary>
    private static string? Declares(string line, string what)
    {
        // A declaration and never a mention: prose about a window class is not a type, and this
        // used to be the sort of thing that quietly renamed every case in a file.
        if (!line.StartsWith("public ", StringComparison.Ordinal)
            && !line.StartsWith("internal ", StringComparison.Ordinal))
            return null;

        var at = line.IndexOf(what, StringComparison.Ordinal);
        if (at < 0)
            return null;

        var rest = line[(at + what.Length)..].Trim();
        var end = rest.IndexOfAny([' ', ':', '(', '{', '<']);
        return end < 0 ? rest : rest[..end];
    }

    /// <summary>
    /// The member a line declares, at the one indentation a case sits at. Anything deeper is a
    /// local function or a lambda and belongs to the case above it.
    /// </summary>
    private static string? Member(string line)
    {
        if (!line.StartsWith("    public ", StringComparison.Ordinal)
            && !line.StartsWith("    private ", StringComparison.Ordinal)
            && !line.StartsWith("    internal ", StringComparison.Ordinal))
            return null;

        var bracket = line.IndexOf('(', StringComparison.Ordinal);
        if (bracket < 0)
            return null;

        var before = line[..bracket];
        var space = before.LastIndexOf(' ');
        return space < 0 ? null : before[(space + 1)..];
    }

    /// <summary>The sources under a tree, and never what a build left beside them.</summary>
    private static IEnumerable<string> Sources(string tree) =>
        Directory
            .EnumerateFiles(tree, "*.cs", SearchOption.AllDirectories)
            .Where(one => !one.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(one => !one.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    /// <summary>The suite, which is the only tree that holds cases.</summary>
    private static IReadOnlyList<string> Trees()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Winwright.slnx")))
            directory = directory.Parent;

        Assert.NotNull(directory);
        return [Path.Combine(directory.FullName, "tests")];
    }
}

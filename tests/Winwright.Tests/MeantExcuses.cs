using System.Collections.ObjectModel;

namespace Winwright.Tests;

/// <summary>
/// One case that is excused every run, and why that is the case's own doing.
/// </summary>
/// <param name="Case">The case, as <c>TypeTests.Method_name</c>.</param>
/// <param name="Because">Why it means to lose what it loses, in the words a reader weighs.</param>
internal sealed record MeantExcuse(string Case, string Because)
{
    public override string ToString() => $"{Case}: {Because}";
}

/// <summary>
/// WW248. The excuses this suite makes on purpose, written down so the ones it does not are red.
/// <para>
/// <see cref="DeskAsks" /> is the half that was already here: a case that asks a desk-dependent
/// reading and does <em>not</em> excuse says why it needs no excuse. This is its mirror, and the
/// defect it exists for is the one WW248 was filed against — <c>PumpedDialog</c> shows a window on
/// this thread, a window this process shows takes the foreground, so a launched fixture in the same
/// class is left without it and every synthesised act against that fixture is a hole. Correctly
/// reported, and for a reason nobody wrote down.
/// </para>
/// <para>
/// <b>Why a list rather than a rule.</b> Both obvious rules misfire, which is what the task's own
/// design records. At run time, <em>the holder is this process</em> marks
/// <c>RefusedForegroundTests</c>, which takes the desk deliberately. Over the sources, <em>a dialog
/// and a launch and a synthesised act</em> marks <c>NudgeTests</c>, whose act is against the dialog.
/// What separates a hole the suite built from one the machine handed it is intent, and intent is
/// not derivable — it is written down or it is not there.
/// </para>
/// <para>
/// <b>Why it is not simply "structural is red".</b> Measured: every excuse this suite makes recurs
/// in every run, because the cases that make them open a decoy or declare a budget on purpose. A
/// rule turning recurrence into a failure would turn all of them red and be turned off the same
/// afternoon. Recurrence is what makes the question worth asking; the answer is this list.
/// </para>
/// </summary>
internal static class MeantExcuses
{
    /// <summary>
    /// Every recurring excuse this suite accounts for. A case excused in every run and missing from
    /// here is red, and an entry naming a case that no longer recurs is red too — the second is what
    /// keeps this from becoming a list of reasons for things that stopped happening.
    /// </summary>
    public static IReadOnlyList<MeantExcuse> Known { get; } = new ReadOnlyCollection<MeantExcuse>(
    [
        new("TraversalTests.A_key_sent_nowhere_is_a_hole_and_the_focus_is_reported_unchanged",
            "the decoy is opened by this case, and a key that reached nothing is the reading it is "
                + "about — an excuse here would be an excuse for the subject"),
        new("RefusedForegroundTests.A_click_that_could_not_be_sent_is_a_hole_naming_the_desk",
            "same: the desk is taken away on purpose, and what is asserted is that the verdict "
                + "follows it rather than the act"),
        new("PointerTests.A_click_with_the_desktop_elsewhere_sends_nothing_and_names_the_intruder",
            "the intruder is this case's own, and naming it is the claim"),
        new("MenuTests.A_menu_key_sent_nowhere_is_a_hole_in_the_trace",
            "the decoy is this case's own, and the trace carrying the hole is what it reads"),
        new("KeyboardTests.Typing_with_the_desktop_elsewhere_sends_nothing_and_names_the_intruder",
            "the intruder is this case's own, and naming it is the claim"),

        new("SlowMachineTests.A_run_that_could_not_look_often_enough_measured_nothing_about_what_it_watched",
            "the budget is this suite's own declaration and the case exists for the arm where it is "
                + "not met, which on a guest under load is every run"),
        new("SlowMachineTests.A_wait_that_used_its_whole_budget_and_produced_nothing_is_excusable",
            "same: the deadline is declared here and the case is about the reading a run gets when "
                + "it runs out"),
        new("SlowMachineTests.A_run_that_arrived_after_the_window_closed_did_not_watch_the_fixture_lose_it",
            "same, and the one whose margin is thinnest: the fixture is asked to hold a window for "
                + "less time than a loaded guest takes to read it"),
    ]);

    /// <summary>Whether this case has said why it means the excuse it makes every run.</summary>
    /// <param name="named">The case, as the ledger spells it.</param>
    public static bool Accounted(string named) =>
        Known.Any(one => string.Equals(one.Case, named, StringComparison.Ordinal));

    /// <summary>The reading a person gets: the count, then a line each.</summary>
    public static IReadOnlyList<string> Render() => new ReadOnlyCollection<string>(
    [
        $"{Known.Count} recurring excuse(s) are accounted for",
        .. Known.Select(one => $"  {one}"),
    ]);
}

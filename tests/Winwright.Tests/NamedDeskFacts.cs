using System.Collections.ObjectModel;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// One case that provokes the desk and then names which desk reading it expects, with why that
/// reading is the arrangement's and not a guess about timing.
/// </summary>
/// <param name="Case">The case, as <c>TypeTests.Method_name</c>.</param>
/// <param name="Arranged">
/// What pins the fact: the sentence a reader weighs to see that the case arranges the reading it
/// names rather than asserting where the provocation happened to land.
/// </param>
internal sealed record NamedDeskFact(string Case, string Arranged)
{
    public override string ToString() => $"{Case}: {Arranged}";
}

/// <summary>
/// WW427. A case that takes the desk away and then names the condition it expects is asserting two
/// things, and only one of them is its claim.
/// <para>
/// WW401 built a run where a window takes the desk <em>during</em> a reading and asserted the hole
/// named the foreground. It went red on the guest naming the focus: the decoy had arrived a moment
/// earlier, before the act, so the act was what the desk refused. Both readings are the desk taking
/// the run's subject away, which is the whole claim — and which one the engine reaches is where in
/// the step the window landed, the one property that case cannot arrange. It now asks
/// <c>DeskFacts.Names</c> instead, and it cost a guest run to learn.
/// </para>
/// <para>
/// The others are not the same shape and that is the point of writing them down. A decoy opened
/// before the act and held for the whole case refuses the act at its door every time, so the fact is
/// the act's own precondition and naming it is a statement about the arrangement. A decoy timed to
/// arrive mid-reading is not, and the difference is invisible in either case's text.
/// </para>
/// <para>
/// So the rule holds rather than being remembered: a case that provokes the desk and names a
/// reading is red here until somebody says what pins it, and an entry for a case that has stopped
/// naming one is red too. What it cannot check is whether the sentence is true — that is the
/// judgement a reader makes, and the list is where they find it.
/// </para>
/// </summary>
internal static class NamedDeskFacts
{
    /// <summary>How a case takes the desk away from the window it is about.</summary>
    private static readonly string[] Provokes = ["Decoy(", "PumpedDialog.Open(", "Decoying("];

    /// <summary>How a case names one desk reading rather than asking whether it is the desk's.</summary>
    private static readonly string[] Names =
        ["PreconditionName", "FocusReading.Named", ".Foreground.Satisfied", ".Foreground.Absence"];

    /// <summary>Every case that does both, with what pins the reading it names.</summary>
    internal static IReadOnlyList<NamedDeskFact> Known { get; } = new ReadOnlyCollection<NamedDeskFact>(
    [
        new("RefusedForegroundTests.A_click_that_could_not_be_sent_is_a_hole_naming_the_desk",
            "the decoy is opened before the act and held for the whole case, so the click is refused "
                + "at its own door and the foreground is the precondition that door reads — the claim "
                + "is that nothing was sent and the verdict is a hole, and the name is the arrangement"),
        new("RefusedForegroundTests.Typing_that_could_not_be_sent_is_a_hole_too",
            "the same arrangement one verb along: the decoy stands before the type, so the hole is the "
                + "one the act's door produces rather than one a reading raced it to"),
        new("RefusedForegroundTests.A_navigation_that_could_not_be_sent_is_a_hole_and_stops_the_case",
            "the same decoy before a step carrying no expectation, which is what that case is about — "
                + "the step never reaches a reading, so nothing but the act's door can name the fact"),
        new("RefusedForegroundTests.A_traversal_that_could_not_be_sent_is_a_hole",
            "the same arrangement for the traversal key: the decoy stands before the act, so the fact "
                + "is the one its door reads"),
        new("RefusedForegroundTests.A_nudge_that_could_not_be_sent_is_a_hole",
            "the same again for the nudge, and the last of the verbs that class walks"),
        new("PickTests.The_pattern_route_needs_no_desktop_and_the_keyboard_one_says_when_it_had_none",
            "the decoy is taken twice on purpose — the pattern route brings the window forward again, "
                + "so the desk is taken away a second time before the keyboard route is asked — and "
                + "what is named is that route's own reading at its door, taken with the desk already "
                + "elsewhere"),
        new("DeskProbeTests.A_minimised_window_that_still_holds_the_desk_is_read_as_stale_end_to_end",
            "nothing takes the desk from the case: its own dialog is brought forward and put down, and "
                + "the foreground is named only for the one outcome the case cannot arrange — another "
                + "process claiming the desk the moment the dialog went down, which is what GitHub's "
                + "runner does with its terminal. WW449: that excuse is decided by whose window the "
                + "probe read, so the name is the desk's fact and never the loop's defect"),
        new("PreambleTests.A_window_that_came_forward_during_the_run_is_told_from_one_that_never_did",
            "WW471. Both ends are arranged and neither is timed. The decoy is brought forward and "
                + "proved to hold the desk before the preamble is opened, so the opening measurement "
                + "cannot be anything but absent; the subject's own `BringToFront` polls until it "
                + "holds, and only then does the run return and the closing reading get taken. What "
                + "is named is that opening measurement, which is what the arrangement pins"),
        new("PreambleTests.The_foreground_is_read_where_a_window_is_named",
            "nothing is provoked here: the dialog is the window the preamble is asked about, and the "
                + "reading named is the measurement the preamble takes for a named window rather than "
                + "a hole some act fell into"),
        new("NotificationAreaTests.A_window_taking_the_foreground_shuts_the_flyout_under_whoever_was_looking_in_it",
            "the reading it names is one the case composes for its own hole — WW411's arm for a flyout "
                + "that shut before anything provoked it — and never one it asserts the engine reached; "
                + "its claim is that the flyout goes when the foreground moves, and it reads who ended "
                + "up with the desk rather than assuming"),
        new("PointerTests.A_click_with_the_desktop_elsewhere_sends_nothing_and_names_the_intruder",
            "the decoy is held for the case and the reading named is the click's own Foreground, taken "
                + "at the door — the claim is that nothing landed and the box is untouched"),
        new("KeyboardTests.Typing_with_the_desktop_elsewhere_sends_nothing_and_names_the_intruder",
            "the same for the keyboard: the reading named is the act's own, and what the case claims "
                + "is that nothing was sent and the box still reads what it did"),
    ]);

    /// <summary>
    /// Every case in this suite that provokes the desk and names a reading, read out of the sources.
    /// </summary>
    internal static IReadOnlyList<string> Found()
    {
        var found = new List<string>();

        foreach (var file in Checkout.SourcesIn(Checkout.Suite, except: $"{nameof(NamedDeskFacts)}.cs"))
        {
            foreach (var member in Checkout.Members(file))
            {
                if (Provokes.Any(one => member.Body.Contains(one, StringComparison.Ordinal))
                    && Names.Any(one => member.Body.Contains(one, StringComparison.Ordinal)))
                {
                    found.Add(member.Named);
                }
            }
        }

        return new ReadOnlyCollection<string>(found);
    }
}

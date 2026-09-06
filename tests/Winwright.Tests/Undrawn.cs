using System.Collections.ObjectModel;

namespace Winwright.Tests;

/// <summary>
/// One shape that opens no window, what it does instead, and the case that drives it. WW409.
/// </summary>
/// <param name="Flag">The flag, without its dashes.</param>
/// <param name="Does">What a run asking for it produces, since it is not a window.</param>
/// <param name="Shown">The case that drives it, as <c>Class.Method</c>.</param>
internal sealed record Quiet(string Flag, string Does, string Shown);

/// <summary>
/// The shapes the run that drives every flag steps over, paired with what does drive them. WW409.
/// <para>
/// <c>Every_shape_that_draws_opens_a_window_somebody_can_look_at</c> reads the catalogue and skips
/// the rows marked <c>[draws nothing]</c>, which is right: a shape that opens no window cannot be
/// asserted to have opened one. What nothing said is what those shapes do instead. The drawing
/// half is covered by reading the catalogue, so a shape added tomorrow is driven; the other half
/// is skipped by reading the same catalogue, and nobody noticed that nobody else picked it up.
/// </para>
/// <para>
/// The evidence that this is a real gap and not a tidy one: the case that asserts those rows carry
/// the qualifier names six of them by hand, and there are eleven. Five shapes had drifted out of a
/// hand-written list inside the only case about them.
/// </para>
/// <para>
/// So this is the pairing, in the shape every other catalogue here takes: read against the built
/// fixture's own answer in both directions, and against the assembly for the case each row names.
/// A twelfth shape that draws nothing arrives as a red asking who drives it.
/// </para>
/// </summary>
internal static class Undrawn
{
    /// <summary>Every shape that draws nothing, and what proves it does what it says.</summary>
    internal static IReadOnlyList<Quiet> Known { get; } = new ReadOnlyCollection<Quiet>(
    [
        new("flags",
            "prints the catalogue and stops",
            "FixtureTests.The_fixture_prints_its_catalogue_without_having_to_be_misspelt_at"),
        new("cloak",
            "shows a window it has asked the compositor to stop drawing, which keeps every style bit saying it is visible",
            "FixtureTests.A_capture_of_a_window_the_application_cloaked_is_refused_rather_than_written"),
        new("render",
            "writes a picture of the fixed surface to the path it was given, and exits",
            "FixtureTests.The_render_is_drawn_on_the_background_the_application_declares"),
        new("resident",
            "runs and shows nothing, which is the ordinary state of a tray application",
            "FixtureTests.A_process_showing_nothing_must_never_trip_the_refusal"),
        new("shadowed",
            "owns a menu and the shell's shadow behind it, and no window at all",
            "FixtureTests.The_largest_window_a_frameless_menu_process_owns_is_the_menu_and_not_its_shadow"),
        new("profiles",
            "prints the profiles it has, one per line, and exits",
            "ReportedSetTests.The_expected_set_is_what_the_application_says_it_has"),
        new("profile",
            "prints the one profile it is using, and exits",
            "ReportedValueTests.The_value_is_what_the_application_says_it_is"),
        new("dies",
            "exits on startup, leaving nothing for a step to find",
            "ResidentFixtureTests.A_resident_launch_that_died_is_named_by_the_case_rather_than_by_its_locators"),
        new("sizeless",
            "renders a page that lays out to nothing, writes no file and exits 3",
            "ProvokedByFlagTests.A_page_whose_every_row_is_collapsed_lays_out_to_nothing_and_is_not_written"),
        new("blank",
            "renders a page of the right size that paints nothing, on no background",
            "ProvokedByFlagTests.A_page_that_paints_nothing_writes_a_picture_nothing_drew"),
        new("unbacked",
            "renders before the application exists, so nothing says what to draw the capture on",
            "ProvokedByFlagTests.A_render_with_no_application_above_it_has_nothing_saying_what_to_draw_it_on"),
    ]);

    /// <summary>
    /// The shapes the built fixture says draw nothing, which is the other side of this pairing.
    /// <para>
    /// Asked of the fixture rather than read off its source, for the reason <see cref="Surfaces" />
    /// gives: this suite references that project without its assembly, so the catalogue arrives the
    /// way an adopter meets it.
    /// </para>
    /// </summary>
    internal static IReadOnlySet<string> Declared() =>
        Surfaces.Declared().Keys.Where(one => !Surfaces.Drawing().Contains(one)).ToHashSet(StringComparer.Ordinal);
}

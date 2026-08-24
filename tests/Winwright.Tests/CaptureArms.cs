using System.Collections.ObjectModel;

using Winwright.Capturing;

namespace Winwright.Tests;

/// <summary>
/// One way a capture can be wrong, paired with what provokes it.
/// </summary>
/// <param name="Arm">Which way, as the engine names it.</param>
/// <param name="Flag">The fixture shape that provokes it, without its dashes. Empty where none does.</param>
/// <param name="Why">Why none does, where none does. Null where a flag is named.</param>
/// <param name="Because">What the arm is about, in the words the pairing is read in.</param>
/// <param name="Case">The case that drives it, as <c>TypeTests.Method_name</c>.</param>
internal sealed record CaptureArm(WrongCapture Arm, string Flag, Without? Why, string Because, string Case)
{
    /// <summary>Whether a shape of the proving ground provokes it.</summary>
    public bool ThroughTheFixture => Flag.Length > 0;

    public override string ToString() => ThroughTheFixture
        ? $"{Arm,-16} --{Flag}: {Because} [{Case}]"
        : $"{Arm,-16} (no flag): {Because} [{Case}]";
}

/// <summary>
/// WW188. <c>Provocation</c> pairs every refusal the framework names with a fixture flag or a
/// stated reason, and it reads exception <em>types</em> — which was right when a type was a refusal.
/// <para>
/// <c>WrongCaptureException</c> is five. The catalogue carried one entry naming one case, and its
/// shape cannot carry more: an entry holds a flag or a reason and never both, which WW40 already had
/// to write into its prose because there was nowhere else to put it. So Block K's first criterion —
/// every refusal has something that provokes it, checked both ways — was true of one arm in five,
/// while two of the others are provoked by real fixture shapes nothing read back.
/// </para>
/// <para>
/// Keyed on the arm the engine declares and never on the sentence thrown with it. A pairing that
/// matched a phrase would start matching a different arm the day somebody reworded a message, and
/// two of these open with the same six words before they say anything that tells them apart.
/// </para>
/// </summary>
internal static class CaptureArms
{
    internal static IReadOnlyList<CaptureArm> Known { get; } = new ReadOnlyCollection<CaptureArm>(
    [
        new(WrongCapture.AnotherProcess, "", Without.NoShape,
            "a receipt is composed over a window and a target a case hands it, and a case already "
                + "builds both — no second application has to exist for one to be of the wrong window",
            "CaptureReceiptTests.A_picture_of_somebody_elses_window_is_refused_and_names_both_processes"),
        new(WrongCapture.NothingDrawing, "cloak", null,
            "WW199: this said a cloaked window is a state the compositor puts a window into, and "
                + "half of that was wrong — DWMWA_CLOAK is what a suspended packaged application "
                + "sets on itself, which is why the reading has a ByTheApplication arm at all",
            "FixtureTests.A_capture_of_a_window_the_application_cloaked_is_refused_rather_than_written"),
        new(WrongCapture.RegionCovered, "intrude", null,
            "a topmost window over exactly the rectangle a run names, which is what WW103 built the "
                + "flag for and what the region check is driven against",
            "FixtureTests.A_capture_whose_region_was_stood_over_is_refused_and_names_the_intruder"),
        new(WrongCapture.DeskChanged, "", Without.NoShape,
            "the take itself opens the window that arrives, which is the defect performed rather "
                + "than a race waited for — two dialogs of this process take the same rectangle, so "
                + "the second stands exactly over the first and no fixture shape is needed",
            "ThroughoutTests.A_region_that_went_from_clear_to_covered_is_refused_as_the_desk_changing"),
        new(WrongCapture.GlassTransmits, "backdrop", null,
            "a window that asked the compositor for mica, acrylic or tabbed, so the refusal and the "
                + "pass beside it are both driven rather than reasoned about",
            "FixtureTests.A_capture_of_a_window_with_a_backdrop_is_refused_rather_than_warned_about"),
        new(WrongCapture.OneFlatColour, "", Without.NoShape,
            "a picture of one colour is a file, and no window has to exist for one — the session "
                + "this was measured on is a display rendering nothing, which no fixture can be",
            "FixtureTests.A_capture_that_is_one_flat_colour_is_refused_rather_than_reported_as_a_picture"),
    ]);

    /// <summary>Every arm the engine declares, read off the enum rather than off the list above.</summary>
    internal static IReadOnlyList<WrongCapture> Declared() =>
        Enum.GetValues<WrongCapture>().Where(one => one != WrongCapture.Unsaid).ToList();

    /// <summary>The reading a person gets: the counts first, then a line each.</summary>
    internal static IReadOnlyList<string> Render()
    {
        var flagged = Known.Count(one => one.ThroughTheFixture);
        return new ReadOnlyCollection<string>(
        [
            $"{Known.Count} arm(s) of one refusal: {flagged} provoked by a fixture shape, "
                + $"{Known.Count - flagged} by a case that builds the reading",
            .. Known.Select(one => $"  {one}"),
        ]);
    }
}

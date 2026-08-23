using System.Reflection;

using Pointer = Winwright.Acting.Pointer;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW133. WW114 asked for a desktop of the fixtures' own and the measurement killed it: a desktop
/// that is not the input desktop grants no foreground at all and refuses synthesised input
/// outright. What the complaint underneath was right about stands — whether these cases pass is
/// partly a question about what else is on the screen.
/// <para>
/// The answer is this block's own criterion: nothing about the desk is reported as a defect in the
/// code. Every input act already carried the refused foreground as a precondition and had no way
/// to become the third verdict, so a scenario asserting on one had to remember to look — and a
/// rule met by whoever remembers is met by nobody.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class RefusedForegroundTests : IDisposable
{
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint BsAutoCheckBox = 0x0003;
    private const uint TrackBarStyle = WsChild | WsVisible;

    private readonly PumpedDialog dialog = PumpedDialog.Open(
        "winwright statistics",
        new PumpedDialog.ChildWindow("Button", "Wrap lines", WsChild | WsVisible | BsAutoCheckBox, 20, 20, 160, 30),
        new PumpedDialog.ChildWindow("Edit", "alpha", WsChild | WsVisible, 20, 60, 200, 24),
        new PumpedDialog.ChildWindow("msctls_trackbar32", null, TrackBarStyle, 20, 100, 200, 30));

    private readonly List<PumpedDialog> decoys = [];

    public void Dispose()
    {
        foreach (var decoy in decoys)
            decoy.Dispose();

        dialog.Dispose();
    }

    /// <summary>Take the desk away, which is the condition every case here is about.</summary>
    private void Decoy()
    {
        var decoy = PumpedDialog.Open("winwright decoy");
        decoys.Add(decoy);

        // What these cases need is that the dialog under test does not hold the desk, and not that
        // the decoy does. Insisting on the second is asking Windows for a promise it does not make
        // - which is the very fragility this task is about, reproduced in the helper for it.
        Assert.NotEqual(ForegroundState.Ours, Foreground.Check(dialog.Frame).State);
    }

    private Subject On(string locator) =>
        new(dialog.Root, Locator.Parse(locator), deadlineMs: 2000, pollMs: 20);

    [Fact]
    public void A_click_that_could_not_be_sent_is_a_hole_naming_the_desk()
    {
        var checkbox = On("""CheckBox[name="Wrap lines"]""");
        Decoy();

        var result = Pointer.Click(checkbox, PointerReason.PointerIsTheAct).AsAssertion("the box is ticked");

        // Not a failure: nothing was sent, so nothing about the application was checked at all.
        Assert.Equal(AssertionOutcome.Unchecked, result.Outcome);
        Assert.Equal(Foreground.PreconditionName, result.Missing!.Name);

        // Whoever holds it, said. Not the decoy by name and not one wording: the desk may belong to
        // another window of this process, to whatever the person was reading, or to nothing at all,
        // and each of those is a different honest sentence about why nothing was sent.
        Assert.True(BusyDesk.Excused(result));
        Assert.DoesNotContain("winwright statistics", result.Missing.Absence);
    }

    [Fact]
    public void Typing_that_could_not_be_sent_is_a_hole_too()
    {
        var edit = On("Edit");
        Decoy();

        var result = Keyboard.Type(edit, "beta").AsAssertion("the box reads beta");

        Assert.Equal(AssertionOutcome.Unchecked, result.Outcome);
        Assert.Equal(Foreground.PreconditionName, result.Missing!.Name);
    }

    [Fact]
    public void A_traversal_that_could_not_be_sent_is_a_hole()
    {
        Decoy();

        var result = Traversal.Press(dialog.Root, TraversalKey.Tab).AsAssertion("tab moves the focus");

        Assert.Equal(AssertionOutcome.Unchecked, result.Outcome);
        Assert.Equal(Foreground.PreconditionName, result.Missing!.Name);
    }

    [Fact]
    public void A_nudge_that_could_not_be_sent_is_a_hole()
    {
        var slider = On("Slider");
        Decoy();

        var result = Traversal.Nudge(slider).AsAssertion("the slider moves");

        Assert.Equal(AssertionOutcome.Unchecked, result.Outcome);
        Assert.Equal(Foreground.PreconditionName, result.Missing!.Name);
    }

    [Fact]
    public void The_verdict_tracks_the_desk_rather_than_the_other_way_round()
    {
        // The other half, and the one that keeps this from being an excuse: with the desk in hand
        // the act is checked and answers like anything else. Stated as the invariant rather than by
        // insisting the window takes the foreground, because Windows does not promise that - which
        // is the whole reason this task exists, and a case demanding it here would be asking for
        // the same promise one floor down.
        var checkbox = On("""CheckBox[name="Wrap lines"]""");
        dialog.BringToFront();

        var ours = Foreground.Check(dialog.Frame).State == ForegroundState.Ours;
        var result = Pointer.Click(checkbox, PointerReason.PointerIsTheAct).AsAssertion("the box is ticked");

        Assert.Equal(!ours, result.DidNotRun);
        Assert.Equal(ours, result.Missing is null);
    }

    [Fact]
    public void A_hole_carries_the_precondition_rather_than_a_sentence_about_it()
    {
        // What makes it a hole rather than a nicer failure: the run's summary resolves it against
        // the conditions that were measured, so an excused check is one somebody can count.
        var checkbox = On("""CheckBox[name="Wrap lines"]""");
        Decoy();

        var result = Pointer.Click(checkbox, PointerReason.PointerIsTheAct).AsAssertion("the box is ticked");

        Assert.NotNull(result.Missing);
        Assert.False(result.Missing.Satisfied);
        Assert.Contains("the foreground belongs to", result.Missing.Name);
    }

    [Fact]
    public void Every_input_result_can_become_a_verdict_rather_than_only_some()
    {
        // The gap this closes was that four of these carried the precondition and none of them
        // could answer with it, so which acts a scenario could excuse depended on which type it
        // happened to hold. Reflection rather than a list, so a fifth result cannot be forgotten.
        var results = typeof(Pointer).Assembly
            .GetExportedTypes()
            .Where(one => one.Namespace == "Winwright.Acting")
            .Where(one => one.GetProperty("Foreground")?.PropertyType == typeof(Precondition))
            .ToList();

        Assert.NotEmpty(results);
        Assert.All(
            results,
            one => Assert.NotNull(one.GetMethod("AsAssertion", BindingFlags.Public | BindingFlags.Instance)));
    }
}

using System.Runtime.InteropServices;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW28. A claim about one switch is void when the walk made several, because each intermediate
/// stop is a switch of its own and the line observed belongs to some other value.
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class PickTests : IDisposable
{
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint WsTabStop = 0x00010000;
    private const uint CbsDropDownList = 0x0003;
    private const uint CbAddString = 0x0143;

    private static readonly string[] Values = ["Alpha", "Bravo", "Charlie", "Delta", "Echo"];

    private readonly PumpedDialog dialog;
    private readonly List<PumpedDialog> decoys = [];
    private Restorable asFound = null!;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SendMessageW(nint window, uint message, nint wParam, string lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint FindWindowExW(nint parent, nint after, string? className, string? title);

    public PickTests()
    {
        dialog = PumpedDialog.Open(
            "winwright statistics",
            new PumpedDialog.ChildWindow(
                "ComboBox", null, WsChild | WsVisible | WsTabStop | CbsDropDownList, 20, 20, 220, 240));

        var combo = FindWindowExW(dialog.Frame, 0, "ComboBox", null);
        Assert.NotEqual(0, combo);
        foreach (var value in Values)
            SendMessageW(combo, CbAddString, 0, value);

        asFound = Surface.AsFound(Combo);
    }

    public void Dispose()
    {
        // Hand the window back the way the class found it. A dropped-down combo holds the desk in
        // a way that outlives the window, and the next class to send a key finds the foreground
        // somewhere it cannot name — which is the failure this is the shipped answer to.
        asFound.PutBack();

        foreach (var decoy in decoys)
            decoy.Dispose();

        dialog.Dispose();
    }

    private void Decoy()
    {
        var decoy = PumpedDialog.Open("winwright decoy");
        decoys.Add(decoy);

        // WW133: what these cases need is that the dialog under test no longer holds the desk, and
        // not that the decoy took it. Windows makes the second promise only sometimes - once this
        // process has been refused the foreground it stops being granted - and insisting on it is
        // the misattribution this block's criterion forbids, one floor down in the fixture.
        Assert.NotEqual(ForegroundState.Ours, Foreground.Check(dialog.Frame).State);
    }

    private Subject Combo => Subject.Unguarded(dialog.Root, Locator.Parse("ComboBox"), deadlineMs: 2000, pollMs: 20);

    [Fact]
    public void The_pattern_route_is_one_switch_and_the_answer_says_so()
    {
        var picked = Pick.Value(Combo, "Charlie");

        Assert.True(picked.Landed);
        Assert.Equal(PickRoute.Pattern, picked.Route);
        Assert.Equal(1, picked.SelectionChanges);
        Assert.Equal(["Charlie"], picked.Passed);
    }

    [Fact]
    public void The_keyboard_route_anchors_at_the_nearer_end_going_down()
    {
        var picked = Pick.Value(Combo, "Bravo", byKeyboard: true);

        // WW172. The keyboard route needs the desk, and Pick already answers a hole when it did not
        // get it. Asserting Landed past that answer is the misattribution this project exists to
        // end, committed by its own suite: a guest run with the Start menu open turned 32 cases red
        // about the application, and this was one of them.
        if (BusyDesk.Excused(picked.AsAssertion("the nearer end anchors the walk down")))
            return;

        Assert.True(picked.Landed);
        Assert.Equal(PickRoute.Keyboard, picked.Route);
        Assert.Equal(["Alpha", "Bravo"], picked.Passed);
        Assert.Equal(2, picked.SelectionChanges);
    }

    [Fact]
    public void The_keyboard_route_anchors_at_the_nearer_end_going_up()
    {
        var picked = Pick.Value(Combo, "Delta", byKeyboard: true);

        if (BusyDesk.Excused(picked.AsAssertion("the nearer end anchors the walk up")))
            return;

        Assert.True(picked.Landed);
        Assert.Equal(["Echo", "Delta"], picked.Passed);
    }

    [Fact]
    public void Anchoring_at_the_nearer_end_is_what_bounds_the_count()
    {
        // Walking from one fixed end would cost four changes to reach Echo from Alpha. From the
        // nearer end it is one, and the observation about that one switch survives.
        var picked = Pick.Value(Combo, "Echo", byKeyboard: true);

        if (BusyDesk.Excused(picked.AsAssertion("the walk is bounded by the nearer end")))
            return;

        Assert.Equal(["Echo"], picked.Passed);
        Assert.Equal(1, picked.SelectionChanges);
    }

    [Fact]
    public void The_route_itself_is_part_of_the_answer()
    {
        var picked = Pick.Value(Combo, "Charlie", byKeyboard: true);

        if (BusyDesk.Excused(picked.AsAssertion("the route is part of the answer")))
            return;

        Assert.Contains("Alpha -> Bravo -> Charlie", picked.ToString());
        Assert.Contains("by the keyboard in 3 changes", picked.ToString());
    }

    [Fact]
    public void A_walk_of_more_than_one_change_is_carried_into_the_trace()
    {
        var walked = Pick.Value(Combo, "Charlie", byKeyboard: true);
        if (BusyDesk.Excused(walked.AsAssertion("the walk reaches the trace")))
            return;

        var step = walked.AsTraceStep();

        Assert.Equal(3, step.Polls);
        Assert.Contains("3 changes", step.Detail);

        var one = Pick.Value(Combo, "Delta").AsTraceStep();
        Assert.Equal(1, one.Polls);
        Assert.Null(one.Detail);
    }

    [Fact]
    public void A_value_the_picker_does_not_hold_is_refused_with_the_ones_it_does()
    {
        var refusal = Assert.Throws<NotActionableException>(() => Pick.Value(Combo, "Foxtrot"));

        Assert.Contains("holds no \"Foxtrot\"", refusal.Because);
        Assert.Contains("\"Alpha\", \"Bravo\", \"Charlie\", \"Delta\", \"Echo\"", refusal.Because);
    }

    [Fact]
    public void What_a_picker_holds_can_be_asked_for()
    {
        Assert.Equal(Values, Pick.Values(Combo));
    }

    [Fact]
    public void The_pattern_route_needs_no_desktop_and_the_keyboard_one_says_when_it_had_none()
    {
        Decoy();

        // The pattern asks the control, so it lands with the desktop somewhere else entirely.
        Assert.True(Pick.Value(Combo, "Delta").Landed);

        // And measured on the way past: selecting through the pattern brings the window forward
        // again, so the desktop has to be taken away a second time before the other route is asked.
        Decoy();

        var walked = Pick.Value(Combo, "Bravo", byKeyboard: true);
        Assert.False(walked.Foreground.Satisfied);
        Assert.Empty(walked.Passed);
        Assert.Contains("was not walked", walked.ToString());
    }
}

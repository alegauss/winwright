using Winwright.Acting;
using Winwright.Locating;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW27. Tab moving focus is a property of the window and nothing in a picture shows it.
/// <para>
/// The slider starts at its minimum, which is the measured detail this task is about seen from
/// the other end: a press downward there is a legitimate no-op, so the nudge has to choose the
/// direction that can move rather than the one a caller guessed.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class TraversalTests : IDisposable
{
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint WsTabStop = 0x00010000;

    private readonly PumpedDialog dialog = PumpedDialog.Open(
        "winwright statistics",
        new PumpedDialog.ChildWindow("Edit", "alpha", WsChild | WsVisible | WsTabStop, 20, 20, 200, 24),
        new PumpedDialog.ChildWindow("Edit", "bravo", WsChild | WsVisible | WsTabStop, 20, 60, 200, 24),
        new PumpedDialog.ChildWindow("msctls_trackbar32", null, WsChild | WsVisible | WsTabStop, 20, 110, 200, 32));

    private readonly List<PumpedDialog> decoys = [];

    public void Dispose()
    {
        foreach (var decoy in decoys)
            decoy.Dispose();

        dialog.Dispose();
    }

    /// <summary>Another pumped window, because only a thread that owns one gets the foreground.</summary>
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

    private Subject On(string locator) =>
        Subject.Unguarded(dialog.Root, Locator.Parse(locator), deadlineMs: 2000, pollMs: 20);

    private void Focus(string locator)
    {
        // The desktop first: these send keys, and another class's window may hold it by now.
        dialog.BringToFront();

        // Taking the focus is an act, so it goes through the door like every other one: there is
        // no way from out here to reach the element without having been judged fit to touch it.
        var admitted = Admitted.To(On(locator));
        admitted.Do(element => element.SetFocus());

        // WW143 converted this to a deadline on "the focus is where it was just put", and the
        // conversion was wrong. Measured rather than argued: with the condition, this class fails
        // A_focus_that_did_not_move three runs out of three, the Right press reading a focus with
        // no name at all; with the sleep it is ten of ten, twice over.
        //
        // So the 120 ms was never only waiting for the focus. UI Automation reports the element as
        // focused before the control has finished taking it, and what the rest of this class needs
        // is the state after that - which nothing out here can observe. A condition that is true
        // too early is worse than the sleep it replaced, because it reads as a wait that was
        // proved. It stays a sleep, with the measurement, until there is something to wait on.
        Thread.Sleep(120);
    }

    [Fact]
    public void Tab_moves_the_focus_and_the_answer_names_what_holds_it()
    {
        Focus("Edit[order=top]");

        var traversed = Traversal.Press(dialog.Root, TraversalKey.Tab);

        // WW133: a key that could not be sent is a hole about the desk and never a claim about
        // this window's tab order.
        if (BusyDesk.Excused(traversed.AsAssertion("tab moves the focus")))
            return;

        Assert.True(traversed.Moved);
        Assert.Equal("alpha", traversed.Before!.Name);
        Assert.Equal("bravo", traversed.After!.Name);
        Assert.Contains("moved the focus from Edit 'alpha' to Edit 'bravo'", traversed.ToString());
    }

    [Fact]
    public void Shift_tab_goes_back()
    {
        Focus("Edit[order=bottom]");

        var traversed = Traversal.Press(dialog.Root, TraversalKey.ShiftTab);
        if (!BusyDesk.Excused(traversed.AsAssertion("shift-tab goes back")))
            Assert.Equal("alpha", traversed.After!.Name);
    }

    [Fact]
    public void A_failure_says_where_the_focus_actually_went_and_not_only_that_it_did_not_move()
    {
        Focus("Edit[order=top]");

        // Tab twice: the second lands on the slider, so the answer names it rather than saying
        // that focus is "not on bravo", which is what a boolean would have said.
        Traversal.Press(dialog.Root, TraversalKey.Tab);
        var second = Traversal.Press(dialog.Root, TraversalKey.Tab);

        if (BusyDesk.Excused(second.AsAssertion("the second tab lands on the slider")))
            return;

        Assert.True(second.Moved);
        Assert.Equal("Slider", second.After!.ControlType);
    }

    [Fact]
    public void What_holds_the_focus_can_be_asked_without_pressing_anything()
    {
        Focus("Edit[order=bottom]");

        Assert.Equal("bravo", Traversal.WhoHasFocus()!.Name);
    }

    [Fact]
    public void A_key_sent_nowhere_is_a_hole_and_the_focus_is_reported_unchanged()
    {
        Focus("Edit[order=top]");
        Decoy();

        var traversed = Traversal.Press(dialog.Root, TraversalKey.Tab);

        Assert.False(traversed.Sent);
        Assert.False(traversed.Moved);

        // Whoever holds it, said. Not the decoy by name: the desk may belong to another window of
        // this process or to whatever was already up, and each is an honest answer.
        Assert.True(BusyDesk.Excused(traversed.AsAssertion("tab moves the focus")));
        Assert.Equal(Winwright.Tracing.StepVerdict.Unchecked, traversed.AsTraceStep().Verdict);
    }

    [Fact]
    public void A_focus_that_did_not_move_is_a_failed_step_and_names_what_still_holds_it()
    {
        // Right inside a text box moves the caret and never the focus, which is a key that did
        // land doing nothing to what this act observes — the case a boolean would report as a
        // window taking no keyboard input.
        Focus("Edit[order=top]");

        var traversed = Traversal.Press(dialog.Root, TraversalKey.Right, settleMs: 300, pollMs: 20);

        if (BusyDesk.Excused(traversed.AsAssertion("right leaves the focus where it was")))
            return;

        Assert.True(traversed.Sent, traversed.Foreground.Absence);
        Assert.Equal("alpha", traversed.After!.Name);
        Assert.False(traversed.Moved);
        Assert.Equal(Winwright.Tracing.StepVerdict.Failed, traversed.AsTraceStep().Verdict);
        Assert.Contains("left the focus on", traversed.ToString());
    }

    [Fact]
    public void A_nudge_at_the_bottom_of_the_range_presses_the_way_that_can_move()
    {
        var slider = On("Slider");
        Assert.Equal(0d, slider.ReadOnce().Values.Range);

        var nudged = Traversal.Nudge(slider);

        if (BusyDesk.Excused(nudged.AsAssertion("the slider moves up")))
            return;

        Assert.True(nudged.Moved);
        Assert.Equal(TraversalKey.Right, nudged.Pressed);
        Assert.False(nudged.ReversedBecauseItWasAtTheEnd);
        Assert.Equal(1d, nudged.After);
    }

    [Fact]
    public void A_nudge_at_the_top_of_the_range_reverses_and_says_so()
    {
        var slider = On("Slider");
        Act.SetRange(slider, 100);
        Assert.Equal(100d, slider.ReadOnce().Values.Range);

        var nudged = Traversal.Nudge(slider);

        if (BusyDesk.Excused(nudged.AsAssertion("the slider moves down from the top")))
            return;

        Assert.True(nudged.Moved);
        Assert.Equal(TraversalKey.Left, nudged.Pressed);
        Assert.True(nudged.ReversedBecauseItWasAtTheEnd);
        Assert.Equal(99d, nudged.After);
        Assert.Contains("because it was already at the end", nudged.ToString());
    }

    [Fact]
    public void Nudging_something_with_no_range_is_refused()
    {
        var refusal = Assert.Throws<NotActionableException>(() => Traversal.Nudge(On("Edit[order=top]")));

        Assert.Equal(Actionable.PatternMissing, refusal.Missing);
    }

    [Fact]
    public void A_nudge_with_the_desktop_elsewhere_sends_nothing()
    {
        var slider = On("Slider");
        Decoy();

        var nudged = Traversal.Nudge(slider);

        Assert.False(nudged.Sent);
        Assert.False(nudged.Moved);
        Assert.Contains("was not sent", nudged.ToString());
    }
}

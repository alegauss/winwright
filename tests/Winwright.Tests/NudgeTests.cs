using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Scenarios;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW226. <c>Traversal.Nudge</c> shipped in block D and nothing here drove it, so the branch that
/// decides which way to press — at the maximum a press upward is a legitimate no-op — had never run
/// against a real control. One slider would have made it reachable and left it untested; the pane
/// draws the three answers a key pressed at a range has, and each is checked here.
/// <para>
/// The direction is the interesting one. A verb that did not flip would read the starting value back
/// off a control that could not move and report that nothing happened, which is indistinguishable
/// from a control that is broken — and on the maximum it would have been the answer every time.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class NudgeTests : IDisposable
{
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint WsTabStop = 0x00010000;

    /// <summary>
    /// The window the driven case uses, and it is this thread's.
    /// <para>
    /// WW234. This class launched the fixture, and both of its driving cases excused themselves on
    /// every guest run — Windows refuses the foreground to a process that does not already own it, so
    /// a launched fixture cannot be sent a key whatever flag it carries. A trackbar takes the range
    /// 0..100 at position 0 with no message sent to it, which is an end: the direction has to be
    /// chosen, so one case here proves both that the act lands and that it reverses.
    /// </para>
    /// </summary>
    private readonly PumpedDialog dialog = PumpedDialog.Open(
        "winwright ranges",
        new PumpedDialog.ChildWindow("msctls_trackbar32", null, WsChild | WsVisible | WsTabStop, 20, 20, 200, 32));

    private readonly Settling settling = Attachable.Settling();
    private readonly AutomationElement fixtureRoot;

    public NudgeTests()
    {
        // The fixture as well, for the two claims a trackbar cannot make without a message sent to
        // it: a range with no room in either direction, and the pane drawing what it says it draws.
        // Neither needs the desktop — the refusal throws before anything is pressed.
        var launched = settling.Register.Launch(Fixture.Started("--ranges"));
        var drawn = Attempt.UntilTrue(() => TopLevelWindows.Largest(launched.Pid) is not null, 20000, 25);

        Assert.True(drawn.Happened, $"the fixture drew no window in {drawn.WaitedMs}ms");
        fixtureRoot = AutomationElement.FromHandle(TopLevelWindows.Largest(launched.Pid)!.Handle);
    }

    public void Dispose()
    {
        dialog.Dispose();
        settling.Dispose();
    }

    private Subject On(string named) =>
        Subject.Unguarded(fixtureRoot, Locator.Parse($"Slider#{named}"), 4000, pollMs: 25);

    private Subject Pumped() =>
        Subject.Unguarded(dialog.Root, Locator.Parse("Slider"), deadlineMs: 4000, pollMs: 25);

    [Fact]
    public void The_pane_draws_every_range_the_verb_has_an_answer_for()
    {
        // Named here rather than read off the pane: the fixture is referenced with no output
        // assembly on purpose, because an application under test is launched from its own build and
        // never from a half-copy beside the suite. So this is the drift check from the other side —
        // a slider renamed in the pane is a red here, on the run after it moves.
        foreach (var named in new[] { "roomEitherWay", "atTheMaximum", "noRoomAtAll" })
            Assert.True(On(named).Read().Found, $"the ranges pane draws no Slider#{named}");
    }

    [Fact]
    public void The_fixture_offers_the_flag_this_pane_is_behind()
    {
        // Asked of the fixture rather than assumed: it prints its own catalogue, so a flag that was
        // renamed says so here instead of on the run where every case above finds no window.
        Assert.Contains("--ranges", Fixture.Catalogue(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_range_sitting_at_an_end_is_pressed_the_way_that_can_move_it()
    {
        // The branch this verb exists to get right, against a window this thread owns. A trackbar
        // starts at its minimum, so the direction the verb prefers is the no-op: one that never
        // reversed would read the starting value back and report a control that does not respond.
        dialog.BringToFront();

        var nudged = Synthesised.Nudge(Pumped());

        if (BusyDesk.Excused(nudged.Needed!))
            return;

        Assert.True(nudged.Changed, nudged.ToString());
        Assert.Equal(Synthesised.ByKeyboard, nudged.Pattern);

        // Upward, because down from the minimum moves nothing. The number is the control's own and
        // is not named here: what is asserted is that it left the end it was sitting at.
        var after = nudged.After.Range!.Value;
        var before = nudged.Before.Range!.Value;
        Assert.True(after > before, $"{before} -> {after} is not a press away from the minimum");
    }

    [Fact]
    public void A_range_with_no_room_at_all_is_refused_rather_than_reported_as_unmoved()
    {
        // A control nothing could nudge is a scenario that proves nothing, so it is a refusal and
        // never a nudge that says the value stayed where it was.
        var refused = Assert.Throws<NotActionableException>(() => Synthesised.Nudge(On("noRoomAtAll")));

        Assert.Contains("accepts only 3", refused.Message, StringComparison.Ordinal);
        Assert.Contains("no nudge would prove anything", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_case_can_name_it_now_that_something_drives_it()
    {
        // The whole point of WW226 against WW225: the verb was left out of the vocabulary until a
        // control existed for it, and this is the line that says it is back in.
        Assert.Contains("nudge", ActVerb.All.Select(one => one.Name));
        Assert.True(ActVerb.Named("nudge").Synthesises);
        Assert.False(ActVerb.Named("nudge").Repeatable);
        Assert.Equal(Takes.Nothing, ActVerb.Named("nudge").Wants);
    }
}

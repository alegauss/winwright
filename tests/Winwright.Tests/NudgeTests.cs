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
    private readonly Settling settling = Attachable.Settling();
    private readonly AutomationElement root;

    public NudgeTests()
    {
        var launched = settling.Register.Launch(Fixture.Started("--ranges", "--show"));
        var drawn = Attempt.UntilTrue(() => TopLevelWindows.Largest(launched.Pid) is not null, 20000, 25);

        Assert.True(drawn.Happened, $"the fixture drew no window in {drawn.WaitedMs}ms");
        root = AutomationElement.FromHandle(TopLevelWindows.Largest(launched.Pid)!.Handle);
    }

    public void Dispose() => settling.Dispose();

    private Subject On(string named) =>
        Subject.Unguarded(root, Locator.Parse($"Slider#{named}"), 4000, pollMs: 25);

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
    public void A_range_with_room_either_way_moves_the_way_the_verb_prefers()
    {
        var nudged = Synthesised.Nudge(On("roomEitherWay"));

        if (BusyDesk.Excused(nudged.Needed!))
            return;

        Assert.True(nudged.Changed, nudged.ToString());
        Assert.Equal("synthesised keyboard", nudged.Pattern);

        // Up from the middle, because that is the direction it prefers when it has the room.
        Assert.Equal(6d, double.Parse(nudged.After.Range!.ToString()!, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void A_range_already_at_its_maximum_is_pressed_the_other_way_instead()
    {
        // The branch WW226 exists for. Without a control sitting at the end, a verb that never
        // flipped would have passed every run: the value would come back unchanged and read as a
        // control that does not respond.
        var nudged = Synthesised.Nudge(On("atTheMaximum"));

        if (BusyDesk.Excused(nudged.Needed!))
            return;

        Assert.True(nudged.Changed, nudged.ToString());
        Assert.Equal(9d, double.Parse(nudged.After.Range!.ToString()!, System.Globalization.CultureInfo.InvariantCulture));
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

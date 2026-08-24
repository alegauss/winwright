using Winwright.Acting;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW119. The fixture waited for <c>Shell_NotifyIconW</c> to return true and its summary claimed
/// it blocked until the shell had the icon. Those are different claims: a true return means the
/// shell accepted the message, and placing the icon and building the automation tree under it
/// happens afterwards on the shell's own schedule — so a test that looked immediately was racing
/// it. Measured across four consecutive full-suite runs on an untouched machine: two green, two
/// red with two failures each, every one of them here.
/// <para>
/// These are about the fixture rather than about the tray. What is proved is that when
/// <c>Add</c> returns, the thing every notification-area assertion depends on is already true.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class TrayPlacementTests
{
    [Fact]
    public void This_run_says_what_earlier_runs_left_in_the_notification_area()
    {
        // WW173, and the whole of it: the reading is taken whether or not anything is wrong, so a
        // shell holding icons from a run that died says so by name. What it replaces is four cases
        // failing at a five-second wait with nothing between them and a shell restart.
        //
        // Red rather than excused, and deliberately: a ghost is this suite's own leftover and not
        // something the desk did to it. WW126 made one harmless; leaving one is still leaving one,
        // and this block's whole theme is that a run leaves nothing behind.
        var census = TrayGhosts.Showing();

        // WW181: and a census that could not open the overflow is a hole, not a clean desk. This is
        // the one arm the first spelling got wrong, so it is the one asserted first.
        if (BusyDesk.Excused(census.AsAssertion("this suite left nothing in the notification area")))
            return;

        Assert.True(census.Clean, census.Sentence());
    }

    [Fact]
    public void When_the_fixture_returns_the_icon_is_already_findable()
    {
        using var icon = TrayIconFixture.Add("winwright placement");

        // No waiting here on purpose. Every case in the suite looks straight after Add, so what
        // has to hold is that looking straight after Add works — not that it works if you wait.
        var searched = NotificationArea.Find(icon.Tip);
        if (BusyDesk.Excused(searched.AsAssertion("the icon this run added can be found")))
            return;

        Assert.True(searched.Found, searched.Sentence());
    }

    [Fact]
    public void The_fixture_leaves_the_overflow_the_way_it_found_it()
    {
        // It opens the flyout to look, so it shuts it again: what this fixture promises is a
        // findable icon and never a flyout left standing for the next case to trip on. One of the
        // two flakes measured was a case that found one already open.
        using var icon = TrayIconFixture.Add("winwright placement");

        Assert.Null(NotificationArea.Overflow());
    }

    [Fact]
    public void Adding_one_and_finding_it_holds_every_time_rather_than_most_times()
    {
        // The flake was a race, so the shape of the proof is repetition: a single green here is
        // what the old fixture also produced about half the time.
        for (var round = 0; round < 5; round++)
        {
            using var icon = TrayIconFixture.Add($"winwright placement {round}");

            var searched = NotificationArea.Find(icon.Tip);
            if (BusyDesk.Excused(searched.AsAssertion("the icon this run added can be found")))
                return;

            Assert.True(searched.Found, searched.Sentence());
            NotificationArea.CloseOverflow();
        }
    }

    [Fact]
    public void Two_icons_from_the_same_run_are_each_placed_before_their_own_add_returns()
    {
        using var first = TrayIconFixture.Add("winwright placement one");
        using var second = TrayIconFixture.Add("winwright placement two");

        // Both tips carry the same process, so this also checks the marks tell them apart.
        Assert.NotEqual(first.Tip, second.Tip);
        var one = NotificationArea.Find(first.Tip);
        var other = NotificationArea.Find(second.Tip);

        if (BusyDesk.Excused(one.AsAssertion("the first icon can be found"))
            || BusyDesk.Excused(other.AsAssertion("the second icon can be found")))
        {
            return;
        }

        Assert.True(one.Found, one.Sentence());
        Assert.True(other.Found, other.Sentence());
        NotificationArea.CloseOverflow();
    }
}

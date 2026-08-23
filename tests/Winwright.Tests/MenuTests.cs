using Winwright.Acting;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW29. Down to the item, Right to expand, and never invoke — one entry launches a terminal and
/// another ends the run.
/// <para>
/// The fixture is a real Win32 menu bar with a real submenu, walked with real keys. The entries
/// are named after the two that made the rule: a Quit that would end the run, and a Terminal that
/// would launch one. Nothing in this suite ever presses either, because nothing here can.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class MenuTests : IDisposable
{
    private readonly PumpedDialog dialog = PumpedDialog.OpenWithMenu(
        "winwright statistics",
        new PumpedDialog.MenuEntry(
            "File",
            new PumpedDialog.MenuEntry("New"),
            new PumpedDialog.MenuEntry("Open"),
            new PumpedDialog.MenuEntry("Recent", new PumpedDialog.MenuEntry("one.txt"), new PumpedDialog.MenuEntry("two.txt")),
            new PumpedDialog.MenuEntry("Terminal"),
            new PumpedDialog.MenuEntry("Quit")),
        new PumpedDialog.MenuEntry("Help", new PumpedDialog.MenuEntry("About")));

    private readonly List<PumpedDialog> decoys = [];

    public void Dispose()
    {
        // Thoroughly, and this is not fussiness: an open menu holds its thread inside a modal
        // loop, so the WM_QUIT the dialog posts is never read and the window outlives the test.
        // The class that then asks whether another instance of this executable is showing a
        // window finds that one, and fails about something it has nothing to do with.
        for (var attempt = 0; attempt < 6 && Menu.Highlighted() is not null; attempt++)
            Menu.Dismiss(1);

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

    [Fact]
    public void The_menu_bar_is_entered_the_way_a_keyboard_user_enters_it()
    {
        var entered = Menu.Enter(dialog.Frame);

        // WW133: a menu is walked with keys, so a desk this run could not have is a hole about the
        // machine rather than a claim about this window's menu bar.
        if (BusyDesk.Excused(entered.AsAssertion("the menu bar is entered")))
            return;

        Assert.True(entered.Sent);
        Assert.Equal("File", entered.Highlighted);
    }

    [Fact]
    public void Walking_down_reaches_an_entry_and_reports_what_it_passed()
    {
        Menu.Enter(dialog.Frame);

        var walked = Menu.To(dialog.Frame, "Recent");

        if (BusyDesk.Excused(walked.AsAssertion("the walk reaches Recent")))
            return;

        Assert.True(walked.Reached);
        Assert.Equal(["File", "New", "Open", "Recent"], walked.Passed);
        Assert.Equal(4, walked.Hops);
    }

    [Fact]
    public void Right_expands_the_submenu_and_the_arrival_is_waited_for()
    {
        Menu.Enter(dialog.Frame);
        Menu.To(dialog.Frame, "Recent");

        var expanded = Menu.Expand(dialog.Frame);

        if (BusyDesk.Excused(expanded.AsAssertion("right expands the submenu")))
            return;

        Assert.True(expanded.Reached);
        Assert.Equal("one.txt", expanded.Highlighted);
    }

    [Fact]
    public void Expanding_something_with_no_submenu_says_which_entry_it_was()
    {
        Menu.Enter(dialog.Frame);
        Menu.To(dialog.Frame, "Open");

        var expanded = Menu.Expand(dialog.Frame, settleMs: 400, pollMs: 20);

        // Right on a top-level menu with no submenu moves to the next menu, which is a real
        // answer and not an error: what it says is where the highlight actually went.
        Assert.NotEqual("Open", expanded.Highlighted);
        Assert.NotNull(expanded.Highlighted);
    }

    [Fact]
    public void There_is_no_way_to_invoke_an_entry_from_this_surface()
    {
        var invoking = typeof(Menu).GetMethods()
            .Select(method => method.Name)
            .Where(name => name.Contains("Invoke", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Press", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Choose", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Activate", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Click", StringComparison.OrdinalIgnoreCase));

        Assert.Empty(invoking);
    }

    [Fact]
    public void Walking_past_a_destructive_entry_highlights_it_and_nothing_more()
    {
        Menu.Enter(dialog.Frame);

        var walked = Menu.To(dialog.Frame, "Quit");

        Assert.True(walked.Reached);
        Assert.Contains("Terminal", walked.Passed);
        Assert.Equal("Quit", walked.Highlighted);

        // The run is still here, which is the assertion: highlighting is not invoking.
        Assert.Equal("Quit", Menu.Highlighted());
    }

    [Fact]
    public void A_walk_that_finds_nothing_leaves_the_menu_open_rather_than_resetting_it()
    {
        Menu.Enter(dialog.Frame);

        var walked = Menu.To(dialog.Frame, "Nonexistent");

        Assert.False(walked.Reached);
        Assert.Contains("did not reach", walked.ToString());

        // Nothing was pressed to normalise, so the menu survived the miss. Left on a top-level
        // entry would have dismissed it, and then a retry walks a menu that is no longer there.
        Assert.NotNull(Menu.Highlighted());
        Assert.Contains(Menu.Highlighted(), walked.Passed);
    }

    [Fact]
    public void The_walk_is_bounded_by_the_menu_coming_round_rather_than_by_a_counter()
    {
        Menu.Enter(dialog.Frame);

        var walked = Menu.To(dialog.Frame, "Nonexistent");

        Assert.True(walked.Hops < Menu.MostEntries, $"the walk took {walked.Hops} hops");
        Assert.Equal(walked.Passed.Distinct().Count(), walked.Passed.Count);
    }

    [Fact]
    public void A_menu_key_sent_nowhere_is_a_hole_in_the_trace()
    {
        Decoy();

        var entered = Menu.Enter(dialog.Frame);

        Assert.False(entered.Sent);
        Assert.True(BusyDesk.Excused(entered.AsAssertion("the menu bar is entered")));
        Assert.Equal(Winwright.Tracing.StepVerdict.Unchecked, entered.AsTraceStep().Verdict);
    }

    [Fact]
    public void A_walk_that_reached_its_entry_is_a_clean_step()
    {
        Menu.Enter(dialog.Frame);

        var step = Menu.To(dialog.Frame, "Open").AsTraceStep();

        Assert.Equal(Winwright.Tracing.StepVerdict.Ok, step.Verdict);
        Assert.Equal("Open", step.ReadBack);
        Assert.Null(step.Detail);
    }
}

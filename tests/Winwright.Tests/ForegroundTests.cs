using System.Runtime.InteropServices;

using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW13. Windows refuses the foreground to a process that does not already own it, so a run
/// started from an editor drives somebody else's window. The four readings are settled here over
/// two sightings, because which window happens to be foreground on the machine running these
/// tests is exactly the thing that must not decide what they prove.
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ForegroundTests : IDisposable
{
    private const uint WsPopup = 0x80000000;

    private readonly List<nint> created = [];

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    public void Dispose()
    {
        foreach (var window in created)
            DestroyWindow(window);
    }

    /// <summary>
    /// Hidden on purpose. Creating a top-level window <em>with</em> WS_VISIBLE activates it, so
    /// there is no such thing as a visible window this process made and did not bring forward —
    /// which is what the first draft of the test below assumed, and Windows disagreed.
    /// </summary>
    private nint CreateHidden(string title)
    {
        var window = CreateWindowExW(0, "Static", title, WsPopup, 40, 40, 320, 200, 0, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
        return window;
    }

    private static readonly WindowOwner Editor = new(0x1111, 4242, "Code", "ROADMAP.md - winwright - Visual Studio Code");
    private static readonly WindowOwner UnderTest = new(0x2222, 777, "ClaudeTray", "Statistics");
    private static readonly WindowOwner ItsOwnDialog = new(0x3333, 777, "ClaudeTray", "Settings");

    [Fact]
    public void The_window_under_test_holding_it_is_the_only_reading_that_passes()
    {
        var foreground = Foreground.Between(UnderTest, UnderTest);

        Assert.Equal(ForegroundState.Ours, foreground.State);
        Assert.True(foreground.Ours);
        Assert.True(foreground.AsPrecondition().Satisfied);
    }

    [Fact]
    public void The_editor_holding_it_names_the_intruder()
    {
        var foreground = Foreground.Between(Editor, UnderTest);

        Assert.Equal(ForegroundState.Elsewhere, foreground.State);
        // WW245: both sides. Naming only the holder is what made a hole about the application under
        // test indistinguishable from a hole about a window nobody attached to.
        Assert.Equal(
            "the foreground belongs to Code (pid 4242) 'ROADMAP.md - winwright - Visual Studio Code', "
                + $"and the window under test is {UnderTest}",
            foreground.AsPrecondition().Absence);
    }

    [Fact]
    public void Another_window_of_the_same_process_is_its_own_reading()
    {
        var foreground = Foreground.Between(ItsOwnDialog, UnderTest);

        Assert.Equal(ForegroundState.SameProcess, foreground.State);
        Assert.Contains("another window of the same process owns it", foreground.AsPrecondition().Absence);
        Assert.Contains("'Settings'", foreground.AsPrecondition().Absence);
    }

    [Fact]
    public void A_desk_where_nothing_has_the_keyboard_says_so()
    {
        var foreground = Foreground.Between(WindowOwner.None, UnderTest);

        Assert.Equal(ForegroundState.Nobody, foreground.State);
        Assert.Equal(
            $"nothing owns the foreground, and the window under test is {UnderTest}",
            foreground.AsPrecondition().Absence);
    }

    [Fact]
    public void Losing_the_foreground_is_a_hole_and_never_a_failure()
    {
        var missing = Foreground.Between(Editor, UnderTest).AsPrecondition();
        var result = AssertionResult.Unchecked("Escape closes Settings", missing);

        Assert.Equal(AssertionOutcome.Unchecked, result.Outcome);
        Assert.Equal(RunOutcome.Degraded, RunVerdict.Over([result]).Outcome);
    }

    [Fact]
    public void There_is_no_way_to_wait_for_it_because_retrying_is_the_defect()
    {
        var waiting = typeof(Foreground).GetMethods()
            .Where(method => method.Name.Contains("Wait", StringComparison.OrdinalIgnoreCase)
                || method.Name.Contains("Retry", StringComparison.OrdinalIgnoreCase)
                || method.Name.Contains("Poll", StringComparison.OrdinalIgnoreCase));

        Assert.Empty(waiting);
    }

    [Fact]
    public void Whatever_holds_the_foreground_right_now_is_read_from_windows()
    {
        var holder = Foreground.Now();

        // WW190. A desk where nothing owns the foreground answers `Nobody`, and the reading is
        // right to: there is no holder to ask about itself. Excused rather than red, because a
        // locked or blank session is the machine's business and not this code's.
        if (BusyDesk.Excused(Foreground.Check(holder.Window).AsPrecondition()))
            return;

        // Nothing here asserts *which* window it is: that is the desk, not the code. What is
        // asserted is that asking the real foreground about itself is the reading that passes.
        Assert.Equal(ForegroundState.Ours, Foreground.Check(holder.Window).State);
    }

    [Fact]
    public void A_window_that_cannot_hold_the_foreground_reads_as_not_holding_it()
    {
        var mine = CreateHidden("winwright statistics");

        var foreground = Foreground.Check(mine);

        Assert.NotEqual(ForegroundState.Ours, foreground.State);
        Assert.False(foreground.AsPrecondition().Satisfied);
        Assert.Contains("winwright statistics", foreground.Sentence());
    }

    [Fact]
    public void The_sentence_says_who_had_it_either_way()
    {
        Assert.Equal(
            "the foreground belongs to the window under test, ClaudeTray (pid 777) 'Statistics'.",
            Foreground.Between(UnderTest, UnderTest).Sentence());

        Assert.Equal(
            "the foreground belongs to Code (pid 4242) 'ROADMAP.md - winwright - Visual Studio Code', "
            + "and the window under test is ClaudeTray (pid 777) 'Statistics'.",
            Foreground.Between(Editor, UnderTest).Sentence());
    }
}

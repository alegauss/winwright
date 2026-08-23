using System.Runtime.InteropServices;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW25. Some controls have no pattern at all, so reaching for the mouse is right — and doing it
/// silently is not, because the act then carries a precondition the file never mentions.
/// <para>
/// These synthesize real input, so they move the real pointer. The cursor is put back where it
/// was afterwards, which is the least a suite owes the desk it is running on.
/// </para>
/// <para>
/// The windows are pumped ones on their own threads. The first draft created them on the test
/// thread, which worked until the suite grew: once this process has been refused the foreground
/// once, Windows stops granting it, and a bare window created by a thread that owns nothing is
/// never activated. Only a thread holding the window it just made is given the desktop.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class PointerTests : IDisposable
{
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint BsAutoCheckBox = 0x0003;

    private readonly PumpedDialog dialog = PumpedDialog.Open(
        "winwright statistics",
        new PumpedDialog.ChildWindow("Button", "Wrap lines", WsChild | WsVisible | BsAutoCheckBox, 20, 20, 160, 30),
        new PumpedDialog.ChildWindow("Static", "a label", WsChild | WsVisible, 20, 80, 120, 20));

    private readonly List<PumpedDialog> decoys = [];
    private readonly Win32Cursor cursor = Win32Cursor.Where();

    public void Dispose()
    {
        foreach (var decoy in decoys)
            decoy.Dispose();

        dialog.Dispose();
        cursor.Restore();
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
        new(dialog.Root, Locator.Parse(locator), deadlineMs: 2000, pollMs: 20);

    [Fact]
    public void A_click_lands_where_the_element_is_when_the_window_owns_the_desktop()
    {
        var checkbox = On("""CheckBox[name="Wrap lines"]""");
        Assert.Equal("Off", checkbox.ReadOnce().Values.Toggle);

        var clicked = Pointer.Click(checkbox, PointerReason.PointerIsTheAct);

        // WW133: the desk is asked for rather than demanded. Where this run could not have it the
        // click is a hole about the machine, and the checkbox is untouched either way.
        if (BusyDesk.Excused(clicked.AsAssertion("the box is ticked")))
        {
            Assert.Equal("Off", checkbox.ReadOnce().Values.Toggle);
            return;
        }

        Assert.True(clicked.Landed);
        Assert.Equal(160, clicked.At.Width);
        Assert.True(
            Attempt.UntilTrue(() => checkbox.ReadOnce().Values.Toggle == "On", 2000, 20).Happened,
            "the click never reached the checkbox");
    }

    [Fact]
    public void A_click_with_the_desktop_elsewhere_sends_nothing_and_names_the_intruder()
    {
        var checkbox = On("""CheckBox[name="Wrap lines"]""");
        Decoy();

        var clicked = Pointer.Click(checkbox, PointerReason.PointerIsTheAct);

        Assert.False(clicked.Landed);
        Assert.False(clicked.Foreground.Satisfied);
        Assert.True(BusyDesk.Excused(clicked.AsAssertion("the box is ticked")));
        Assert.Equal("Off", checkbox.ReadOnce().Values.Toggle);
    }

    [Fact]
    public void Input_sent_nowhere_is_a_hole_in_the_trace_rather_than_a_step_that_ran()
    {
        var checkbox = On("""CheckBox[name="Wrap lines"]""");
        Decoy();

        var step = Pointer.Click(checkbox, PointerReason.PointerIsTheAct).AsTraceStep();

        Assert.Equal(Winwright.Tracing.StepVerdict.Unchecked, step.Verdict);
        Assert.False(string.IsNullOrWhiteSpace(step.Detail));
        Assert.Null(step.ReadBack);
    }

    [Fact]
    public void Nothing_in_this_project_falls_back_to_a_pointer_when_a_pattern_is_missing()
    {
        var label = On("""Text[name="a label"]""");

        // Invoke refuses rather than quietly clicking, which is the whole of this task.
        var refusal = Assert.Throws<NotActionableException>(() => Act.Invoke(label));
        Assert.Equal(Actionable.PatternMissing, refusal.Missing);

        // The pointer is reachable, but only by asking for it by name.
        var clicked = Pointer.Click(label, PointerReason.NoAutomationPeer);
        if (!BusyDesk.Excused(clicked.AsAssertion("the label takes a click")))
            Assert.True(clicked.Landed);
    }

    [Fact]
    public void A_pointer_act_still_needs_the_element_to_be_there_and_on_screen()
    {
        var refusal = Assert.Throws<NotActionableException>(
            () => Pointer.Click(On("""Button[name="Publish"]"""), PointerReason.PointerIsTheAct));

        Assert.Equal(Actionable.NotInTree, refusal.Missing);
    }

    [Fact]
    public void What_needs_a_real_desktop_is_countable_by_reading_the_declaration()
    {
        var declared = new List<PointerAct>
        {
            new("click", Locator.Parse("Custom#tray"), PointerReason.NotificationArea),
            new("right-click", Locator.Parse("Custom#tray"), PointerReason.NotificationArea, Button: MouseButton.Right),
            new("double-click", Locator.Parse("Pane#canvas"), PointerReason.CustomTemplate, Button: MouseButton.Left, Clicks: 2),
        };

        var said = Pointer.Summarise(declared);

        Assert.StartsWith("3 acts need a real desktop, for 2 reasons.", said);
        Assert.Contains("right-click Custom#tray (1 right click)", said);
        Assert.Contains("double-click Pane#canvas (2 left clicks)", said);
    }

    [Fact]
    public void A_scenario_with_no_pointer_act_says_so_rather_than_saying_nothing()
    {
        Assert.Equal("no act here needs a real desktop.", Pointer.Summarise([]));
    }

    [Fact]
    public void A_click_count_of_nothing_is_refused()
    {
        var checkbox = On("""CheckBox[name="Wrap lines"]""");

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Pointer.Run(new PointerAct("click", checkbox.Locator, PointerReason.PointerIsTheAct, Button: MouseButton.Left, Clicks: 0), checkbox));
    }

    [Fact]
    public void A_double_click_is_two_presses_and_says_so()
    {
        var act = new PointerAct("double-click", Locator.Parse("Pane#canvas"), PointerReason.CustomTemplate, Button: MouseButton.Left, Clicks: 2);

        Assert.Equal(2, act.Clicks);
        Assert.Contains("2 left clicks", act.ToString());
    }
}

/// <summary>Where the pointer was before a test moved it, and how it is put back.</summary>
internal sealed class Win32Cursor
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out CursorPoint point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int x, int y);

    [StructLayout(LayoutKind.Sequential)]
    private struct CursorPoint
    {
        public int X;
        public int Y;
    }

    private readonly CursorPoint was;
    private readonly bool known;

    private Win32Cursor(CursorPoint was, bool known)
    {
        this.was = was;
        this.known = known;
    }

    internal static Win32Cursor Where()
    {
        var known = GetCursorPos(out var point);
        return new Win32Cursor(point, known);
    }

    internal void Restore()
    {
        if (known)
            SetCursorPos(was.X, was.Y);
    }
}

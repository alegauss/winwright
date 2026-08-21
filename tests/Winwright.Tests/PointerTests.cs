using System.Runtime.InteropServices;
using System.Windows.Automation;

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
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class PointerTests : IDisposable
{
    private const uint WsPopup = 0x80000000;
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint BsAutoCheckBox = 0x0003;

    private readonly List<nint> created = [];
    private readonly Win32Cursor cursor = Win32Cursor.Where();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public nint Window;
        public uint Message;
        public nint WParam;
        public nint LParam;
        public uint Time;
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessageW(out Msg message, nint window, uint first, uint last, uint remove);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(ref Msg message);

    [DllImport("user32.dll")]
    private static extern nint DispatchMessageW(ref Msg message);

    /// <summary>
    /// Drain the queue, because synthesized input goes there and a window whose thread never
    /// pumps never sees it. A pattern act needs no pump — it reaches the window procedure
    /// directly — which is one more way the two kinds of act are not the same act.
    /// </summary>
    private static void Pump(int milliseconds = 400)
    {
        var until = System.Diagnostics.Stopwatch.StartNew();
        while (until.ElapsedMilliseconds < milliseconds)
        {
            while (PeekMessageW(out var message, 0, 0, 0, remove: 1))
            {
                TranslateMessage(ref message);
                DispatchMessageW(ref message);
            }

            Thread.Sleep(10);
        }
    }

    public void Dispose()
    {
        for (var index = created.Count - 1; index >= 0; index--)
            DestroyWindow(created[index]);

        cursor.Restore();
    }

    private nint Create(string className, string? title, uint style, int x, int y, int w, int h, nint parent = 0)
    {
        var window = CreateWindowExW(0, className, title, style, x, y, w, h, parent, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
        return window;
    }

    private nint Dialog()
    {
        var frame = Create("Static", "winwright statistics", WsPopup | WsVisible, 60, 60, 480, 320);
        Create("Button", "Wrap lines", WsChild | WsVisible | BsAutoCheckBox, 20, 20, 160, 30, frame);
        return frame;
    }

    private static Subject On(nint frame, string locator) =>
        new(AutomationElement.FromHandle(frame), Locator.Parse(locator), 2000, pollMs: 20);

    [Fact]
    public void A_click_lands_where_the_element_is_when_the_window_owns_the_desktop()
    {
        var frame = Dialog();
        var checkbox = On(frame, """CheckBox[name="Wrap lines"]""");

        // Creating a visible top-level window activates it, so the dialog owns the foreground.
        Assert.Equal(ForegroundState.Ours, Foreground.Check(frame).State);
        Assert.Equal("Off", checkbox.ReadOnce().Values.Toggle);

        var clicked = Pointer.Click(checkbox);
        Assert.True(clicked.Landed);
        Assert.Equal(160, clicked.At.Width);

        Pump();
        Assert.Equal("On", checkbox.ReadOnce().Values.Toggle);
    }

    [Fact]
    public void A_click_with_the_desktop_elsewhere_sends_nothing_and_names_the_intruder()
    {
        var frame = Dialog();
        var checkbox = On(frame, """CheckBox[name="Wrap lines"]""");
        Create("Static", "winwright decoy", WsPopup | WsVisible, 60, 60, 200, 120);

        var clicked = Pointer.Click(checkbox);

        Assert.False(clicked.Landed);
        Assert.False(clicked.Foreground.Satisfied);
        Assert.Contains("winwright decoy", clicked.Foreground.Absence);

        // And nothing was sent: the control is where it was, even after a pump.
        Pump(100);
        Assert.Equal("Off", checkbox.ReadOnce().Values.Toggle);
    }

    [Fact]
    public void Input_sent_nowhere_is_a_hole_in_the_trace_rather_than_a_step_that_ran()
    {
        var frame = Dialog();
        var checkbox = On(frame, """CheckBox[name="Wrap lines"]""");
        Create("Static", "winwright decoy", WsPopup | WsVisible, 60, 60, 200, 120);

        var step = Pointer.Click(checkbox).AsTraceStep();

        Assert.Equal(Winwright.Tracing.StepVerdict.Unchecked, step.Verdict);
        Assert.Contains("winwright decoy", step.Detail);
        Assert.Null(step.ReadBack);
    }

    [Fact]
    public void Nothing_in_this_project_falls_back_to_a_pointer_when_a_pattern_is_missing()
    {
        var frame = Dialog();
        Create("Static", "a label", WsChild | WsVisible, 20, 80, 120, 20, frame);
        var label = On(frame, """Text[name="a label"]""");

        // Invoke refuses rather than quietly clicking, which is the whole of this task.
        var refusal = Assert.Throws<NotActionableException>(() => Act.Invoke(label));
        Assert.Equal(Actionable.PatternMissing, refusal.Missing);

        // The pointer is reachable, but only by asking for it by name.
        Assert.True(Pointer.Click(label).Landed);
        Pump(100);
    }

    [Fact]
    public void A_pointer_act_still_needs_the_element_to_be_there_and_on_screen()
    {
        var refusal = Assert.Throws<NotActionableException>(
            () => Pointer.Click(On(Dialog(), """Button[name="Publish"]""")));

        Assert.Equal(Actionable.NotInTree, refusal.Missing);
    }

    [Fact]
    public void What_needs_a_real_desktop_is_countable_by_reading_the_declaration()
    {
        var declared = new List<PointerAct>
        {
            new("click", Locator.Parse("Custom#tray")),
            new("right-click", Locator.Parse("Custom#tray"), MouseButton.Right),
            new("double-click", Locator.Parse("Pane#canvas"), MouseButton.Left, 2),
        };

        var said = Pointer.Summarise(declared);

        Assert.StartsWith("3 acts need a real desktop:", said);
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
        var frame = Dialog();
        var checkbox = On(frame, """CheckBox[name="Wrap lines"]""");

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Pointer.Run(new PointerAct("click", checkbox.Locator, MouseButton.Left, 0), checkbox));
    }

    [Fact]
    public void A_double_click_is_two_presses_and_says_so()
    {
        var act = new PointerAct("double-click", Locator.Parse("Pane#canvas"), MouseButton.Left, 2);

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

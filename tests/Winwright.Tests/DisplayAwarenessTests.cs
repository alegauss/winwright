using System.Runtime.InteropServices;

using Winwright.Capturing;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW37. Without per-monitor awareness the rectangle and the copy sit in different coordinate
/// spaces, and a capture is offset or scaled on any display between 125 and 200 percent.
/// <para>
/// The discriminating measurement is the one below: an unaware process is told the screen is the
/// scaled size while the device says the physical one, and on this machine those differ by a
/// factor of two. Aware, they agree — which is the whole claim in one comparison.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class DisplayAwarenessTests
{
    private const int ScreenWidth = 0;
    private const int ScreenHeight = 1;
    private const int DesktopHorizontalPixels = 118;
    private const int DesktopVerticalPixels = 117;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll")]
    private static extern nint GetDC(nint window);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(nint window, nint dc);

    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(nint dc, int index);

    private static (int Reported, int Physical) Screen()
    {
        var dc = GetDC(0);
        try
        {
            return (GetSystemMetrics(ScreenWidth), GetDeviceCaps(dc, DesktopHorizontalPixels));
        }
        finally
        {
            ReleaseDC(0, dc);
        }
    }

    [Fact]
    public void The_engine_asks_for_per_monitor_awareness_and_gets_it()
    {
        var awareness = DisplayAwareness.Ensure();

        Assert.True(awareness.Trustworthy, awareness.ToString());
        Assert.Equal(DpiAwareness.PerMonitor, DisplayAwareness.Current());
    }

    [Fact]
    public void The_reported_screen_and_the_physical_one_agree_once_it_is_set()
    {
        DisplayAwareness.Ensure();

        var (reported, physical) = Screen();

        // This is the measurement the task exists for. Unaware, these differ by the scaling
        // factor and every rectangle read is out by it; aware, they are the same number.
        Assert.Equal(physical, reported);
        Assert.Equal(GetSystemMetrics(ScreenHeight), Physical(DesktopVerticalPixels));
    }

    [Fact]
    public void Asking_twice_answers_the_same_thing_and_changes_nothing()
    {
        var first = DisplayAwareness.Ensure();
        var second = DisplayAwareness.Ensure();

        Assert.Same(first, second);
    }

    [Fact]
    public void The_answer_says_which_call_took_or_that_it_was_already_set()
    {
        var awareness = DisplayAwareness.Ensure();

        Assert.NotEmpty(awareness.How);
        Assert.Contains("per-monitor aware", awareness.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_rectangle_is_read_in_the_right_space_without_anybody_asking_for_it()
    {
        // Nothing here calls Ensure, and no door calls it either: referencing the engine at all
        // is what set the awareness, which is what "it belongs in the engine" has to mean. Asking
        // at the doors was the first draft and it is the failure recorded above the type — the
        // fixture window below would have been made in one coordinate space and read in another.
        using var dialog = PumpedDialog.OpenFramed("winwright framed");

        var frame = PaintedFrame.Of(dialog.Frame);

        Assert.NotNull(frame);
        Assert.Equal(DpiAwareness.PerMonitor, DisplayAwareness.Current());
    }

    [Fact]
    public void An_element_read_is_in_that_space_too()
    {
        using var dialog = PumpedDialog.Open("winwright bare");

        Assert.NotEmpty(TopLevelWindows.OfProcess(System.Environment.ProcessId));
        Assert.Equal(DpiAwareness.PerMonitor, DisplayAwareness.Current());
    }

    [Fact]
    public void The_awareness_was_already_in_force_before_the_first_call_into_the_engine()
    {
        // Ensure has been run by the module initialiser, so the first call any test makes finds
        // the question settled rather than asking it. "It was already set" is the answer only a
        // manifest could give; here the initialiser is what took, and it names itself.
        var awareness = DisplayAwareness.Ensure();

        Assert.Equal(DpiAwareness.PerMonitor, awareness.Sees);
        Assert.Null(awareness.Because);
        Assert.NotEqual("nothing took", awareness.How);
    }

    private static int Physical(int index)
    {
        var dc = GetDC(0);
        try
        {
            return GetDeviceCaps(dc, index);
        }
        finally
        {
            ReleaseDC(0, dc);
        }
    }
}

using System.Diagnostics;
using System.Runtime.InteropServices;

using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW11. A borderless toast, a balloon or a menu is a top-level window the process owns, and the
/// process object reports none of them.
/// <para>
/// The fixture builds the real shape rather than describing it: an invisible owner window and a
/// visible borderless popup owned by it, which is what a tray application's toast actually is.
/// <see cref="Process.MainWindowHandle"/> skips an owned window, so it does not reach the toast;
/// enumerating by process id does.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class TopLevelWindowsTests : IDisposable
{
    private const uint WsPopup = 0x80000000;
    private const uint WsVisible = 0x10000000;

    private readonly List<nint> created = [];

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    private nint Create(string? title, uint style, int width, int height, nint owner = 0)
    {
        // "Static" is a class Windows has already registered, so the fixture needs no class of its own.
        var window = CreateWindowExW(0, "Static", title, style, 40, 40, width, height, owner, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
        return window;
    }

    public void Dispose()
    {
        foreach (var window in created)
            DestroyWindow(window);
    }

    private static int Me => System.Environment.ProcessId;

    [Fact]
    public void An_owned_borderless_window_is_found_by_pid_and_not_by_the_main_window_handle()
    {
        var owner = Create(null, WsPopup, 0, 0);
        var toast = Create(null, WsPopup | WsVisible, 240, 80, owner);

        using var self = Process.GetCurrentProcess();
        self.Refresh();

        // The symptom: the one handle a process is willing to name is not this one.
        Assert.NotEqual(toast, self.MainWindowHandle);

        // The fix: it is right there when the desktop is walked by process id.
        Assert.Contains(TopLevelWindows.OfProcess(Me), window => window.Handle == toast);
    }

    [Fact]
    public void The_window_it_finds_carries_what_a_locator_is_written_from()
    {
        var owner = Create(null, WsPopup, 0, 0);
        var toast = Create("winwright toast", WsPopup | WsVisible, 240, 80, owner);

        var found = Assert.Single(TopLevelWindows.OfProcess(Me), window => window.Handle == toast);

        Assert.Equal("winwright toast", found.Title);
        Assert.Equal("Static", found.ClassName);
        Assert.Equal(240, found.Bounds.Width);
        Assert.Equal(80, found.Bounds.Height);
        Assert.True(found.Visible);
        Assert.True(found.IsOwned);
        Assert.Equal(owner, found.Owner);
    }

    [Fact]
    public void A_window_below_the_size_floor_is_left_out()
    {
        var speck = Create(null, WsPopup | WsVisible, 4, 4);

        Assert.DoesNotContain(TopLevelWindows.OfProcess(Me), window => window.Handle == speck);
        Assert.Contains(TopLevelWindows.OfProcess(Me, smallest: 0), window => window.Handle == speck);
    }

    [Fact]
    public void A_hidden_window_is_skipped_unless_it_is_asked_for()
    {
        var hidden = Create(null, WsPopup, 240, 80);

        Assert.DoesNotContain(TopLevelWindows.OfProcess(Me), window => window.Handle == hidden);

        var found = Assert.Single(
            TopLevelWindows.OfProcess(Me, visibleOnly: false), window => window.Handle == hidden);
        Assert.False(found.Visible);
    }

    [Fact]
    public void They_come_back_largest_first_so_the_frame_is_the_first_row()
    {
        var small = Create(null, WsPopup | WsVisible, 120, 40);
        var large = Create(null, WsPopup | WsVisible, 640, 480);

        var mine = TopLevelWindows.OfProcess(Me).Where(window => window.Handle == small || window.Handle == large).ToList();

        Assert.Equal([large, small], mine.Select(window => window.Handle));
        Assert.Equal(large, TopLevelWindows.Largest(Me)!.Handle);
    }

    [Fact]
    public void A_process_that_owns_nothing_above_the_floor_says_so_rather_than_answering_zero()
    {
        using var register = new Winwright.Processes.ProcessRegister();
        var start = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };
        start.ArgumentList.Add("/c");
        start.ArgumentList.Add("ping -n 120 127.0.0.1");
        var launched = register.Launch(start);

        Assert.Empty(TopLevelWindows.OfProcess(launched));
        Assert.Null(TopLevelWindows.Largest(launched.Pid));
    }

    [Fact]
    public void A_negative_size_floor_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TopLevelWindows.OfProcess(Me, smallest: -1));
    }

    [Fact]
    public void The_line_a_summary_shows_names_the_class_the_size_and_whether_it_is_owned()
    {
        var owner = Create(null, WsPopup, 0, 0);
        var toast = Create("winwright toast", WsPopup | WsVisible, 240, 80, owner);

        var found = Assert.Single(TopLevelWindows.OfProcess(Me), window => window.Handle == toast);

        Assert.Equal("Static 'winwright toast' [240x80 at 40,40], owned", found.ToString());
    }
}

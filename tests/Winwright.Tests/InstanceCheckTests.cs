using System.Diagnostics;
using System.Runtime.InteropServices;

using Winwright.Processes;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW12. The failure this refusal exists for returned a picture of another instance's Settings
/// window when Statistics had been asked for, printed the size it captured, and exited zero.
/// <para>
/// This process stands in for the other instance: it is a real process running a real executable,
/// and windows can be created in it, which no launched system binary allows.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class InstanceCheckTests : IDisposable
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

    private void Show(int width, int height)
    {
        var window = CreateWindowExW(0, "Static", "winwright settings", WsPopup | WsVisible, OffScreen.Left, OffScreen.Top, width, height, 0, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
    }

    public void Dispose()
    {
        foreach (var window in created)
            DestroyWindow(window);
    }

    private static string Me => System.Environment.ProcessPath!;

    private static ProcessStartInfo Windowless(string executable)
    {
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };
        start.ArgumentList.Add("/c");
        start.ArgumentList.Add("ping -n 120 127.0.0.1");
        return start;
    }

    [Fact]
    public void Another_instance_showing_a_window_stops_the_run()
    {
        Show(420, 300);

        var check = InstanceCheck.Of(Me);

        Assert.True(check.Refuses);
        var refusal = Assert.Throws<AnotherInstanceException>(check.RequireSole);
        Assert.Contains($"pid {System.Environment.ProcessId}", refusal.Message);
        Assert.True(refusal.Message.Contains("winwright settings"), refusal.Message);
    }

    [Fact]
    public void The_refusal_names_the_override_that_gets_past_it()
    {
        Show(420, 300);

        var refusal = Assert.Throws<AnotherInstanceException>(InstanceCheck.Of(Me).RequireSole);

        Assert.Contains($"pass {InstanceCheck.OverrideName} to drive it anyway", refusal.Message);
    }

    [Fact]
    public void The_override_gets_past_it_and_is_named_in_the_output()
    {
        Show(420, 300);

        var check = InstanceCheck.Of(Me, allowOthers: true);

        Assert.False(check.Refuses);
        check.RequireSole();
        Assert.Contains($"allowed by {InstanceCheck.OverrideName}", check.Sentence());
    }

    [Fact]
    public void The_instance_this_run_owns_is_never_another_one()
    {
        Show(420, 300);

        var check = InstanceCheck.Of(Me, ours: [System.Environment.ProcessId]);

        Assert.False(check.Refuses);
        Assert.Empty(check.Others);
        Assert.Equal("nothing else is running this application.", check.Sentence());
    }

    [Fact]
    public void A_resident_instance_showing_nothing_is_the_ordinary_case_and_never_stops_a_run()
    {
        var root = Directory.CreateTempSubdirectory("winwright-instance-").FullName;
        try
        {
            var app = Path.Combine(root, "tray.exe");
            File.Copy(Path.Combine(System.Environment.SystemDirectory, "cmd.exe"), app);

            using var register = new ProcessRegister();
            register.Launch(Windowless(app));
            register.Launch(Windowless(app));

            var check = InstanceCheck.Of(app);

            Assert.Equal(2, check.Others.Count);
            Assert.Equal(2, check.Resident.Count);
            Assert.Empty(check.Windowed);
            Assert.False(check.Refuses);
            check.RequireSole();
            Assert.Contains("resident and showing no window", check.Sentence());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void A_window_below_the_size_floor_does_not_count_as_showing_something()
    {
        Show(4, 4);

        Assert.False(InstanceCheck.Of(Me).Refuses);
    }

    [Fact]
    public void A_different_executable_of_the_same_name_is_not_this_application()
    {
        var root = Directory.CreateTempSubdirectory("winwright-instance-").FullName;
        try
        {
            var here = Path.Combine(root, "here", "tray.exe");
            var there = Path.Combine(root, "there", "tray.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(here)!);
            Directory.CreateDirectory(Path.GetDirectoryName(there)!);
            File.Copy(Path.Combine(System.Environment.SystemDirectory, "cmd.exe"), here);
            File.Copy(here, there);

            using var register = new ProcessRegister();
            register.Launch(Windowless(there));

            Assert.Empty(InstanceCheck.Of(here).Others);
            Assert.Single(InstanceCheck.Of(there).Others);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Nothing_running_at_all_says_so_rather_than_saying_nothing()
    {
        var root = Directory.CreateTempSubdirectory("winwright-instance-").FullName;
        try
        {
            var app = Path.Combine(root, "absent.exe");
            File.Copy(Path.Combine(System.Environment.SystemDirectory, "cmd.exe"), app);

            var check = InstanceCheck.Of(app);

            Assert.Empty(check.Others);
            Assert.Equal("nothing else is running this application.", check.Sentence());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

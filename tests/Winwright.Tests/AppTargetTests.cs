using System.Diagnostics;
using System.Runtime.InteropServices;

using Winwright.Processes;
using Winwright.Scenarios;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW14. Attaching is convenient on a developer machine, where a single-instance mutex makes a
/// second launch exit silently. It is also a different claim: what gets checked is whatever
/// binary is up, and what this run never passed it cannot report.
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class AppTargetTests : IDisposable
{
    private const uint WsPopup = 0x80000000;

    private readonly List<nint> created = [];
    private readonly string root = Directory.CreateTempSubdirectory("winwright-attach-").FullName;

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

        Directory.Delete(root, recursive: true);
    }

    private ProcessStartInfo Windowless()
    {
        var app = Path.Combine(root, "tray.exe");
        if (!File.Exists(app))
            File.Copy(Path.Combine(System.Environment.SystemDirectory, "cmd.exe"), app);

        var start = new ProcessStartInfo(app)
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
    public void A_launch_knows_what_it_passed()
    {
        using var register = new ProcessRegister();
        var target = AppTarget.FromLaunch(register.Launch(Windowless()), "--profile", "beta");

        Assert.True(target.WasLaunched);
        Assert.True(target.LaunchArguments.Satisfied);
        Assert.Equal(["--profile", "beta"], Assert.IsType<LaunchedTarget>(target).Arguments);
        Assert.Contains("with --profile beta.", target.Sentence());
    }

    [Fact]
    public void An_attach_says_which_binary_it_reached()
    {
        using var register = new ProcessRegister();
        var launched = Attachable.Launch(register, Windowless());

        var target = AppTarget.AttachTo(launched.Pid);

        Assert.False(target.WasLaunched);
        Assert.Equal(launched.Pid, target.Pid);
        Assert.Equal(Path.Combine(root, "tray.exe"), target.Binary.Path);
        Assert.Contains($"attached to pid {launched.Pid}, running", target.Sentence());
    }

    [Fact]
    public void An_attached_target_has_no_arguments_to_read_at_all()
    {
        using var register = new ProcessRegister();
        var target = AppTarget.AttachTo(Attachable.Launch(register, Windowless()).Pid);

        Assert.IsType<AttachedTarget>(target);
        Assert.DoesNotContain(typeof(AttachedTarget).GetProperties(), property => property.Name == "Arguments");
    }

    [Fact]
    public void An_assertion_that_needed_a_launch_argument_is_a_hole_and_not_a_comparison()
    {
        using var register = new ProcessRegister();
        var target = AppTarget.AttachTo(Attachable.Launch(register, Windowless()).Pid);

        var declaration = AssertionDeclaration.Of(
            "the beta profile is selected", "the profile menu", AppTarget.LaunchArgumentsPreconditionName);
        var result = declaration.Unchecked(target.LaunchArguments);

        Assert.Equal(AssertionOutcome.Unchecked, result.Outcome);
        Assert.Equal(RunOutcome.Degraded, RunVerdict.Over([result]).Outcome);
        Assert.Contains("attached to pid", result.Detail);
    }

    [Fact]
    public void Attaching_by_window_reaches_the_process_that_owns_it()
    {
        var window = CreateWindowExW(0, "Static", "winwright statistics", WsPopup, OffScreen.Left, OffScreen.Top, 320, 200, 0, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);

        var target = AppTarget.AttachToWindow(window);

        Assert.Equal(System.Environment.ProcessId, target.Pid);
        Assert.Equal(window, Assert.IsType<AttachedTarget>(target).Window);
        Assert.Contains($"attached to window 0x{window:X}", target.Sentence());
    }

    [Fact]
    public void A_pid_nothing_is_running_as_is_refused_rather_than_run_against_something_else()
    {
        var refusal = Assert.Throws<AttachFailedException>(() => AppTarget.AttachTo(0x7FFFFFFF));

        Assert.Contains("no process is running as pid", refusal.Because);
    }

    [Fact]
    public void A_window_handle_of_zero_addresses_nothing()
    {
        Assert.Throws<AttachFailedException>(() => AppTarget.AttachToWindow(0));
    }

    [Fact]
    public void Nothing_decides_between_launching_and_attaching_for_the_caller()
    {
        var deciding = typeof(AppTarget).GetMethods()
            .Select(method => method.Name)
            .Where(name => name.Contains("OrAttach", StringComparison.OrdinalIgnoreCase)
                || name.Contains("OrLaunch", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Ensure", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Existing", StringComparison.OrdinalIgnoreCase));

        Assert.Empty(deciding);
    }

    [Fact]
    public void A_launch_that_passed_nothing_says_so_rather_than_printing_an_empty_tail()
    {
        using var register = new ProcessRegister();

        Assert.Contains("with no arguments.", AppTarget.FromLaunch(register.Launch(Windowless())).Sentence());
    }
}

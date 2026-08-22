using System.Diagnostics;

using Winwright.Locating;
using Winwright.Processes;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW89. Every loop in this framework was developed against somebody's shipping product, which
/// means a real account, a real transcript directory, a real controller and a machine somebody set
/// up by hand.
/// <para>
/// These tests drive the fixture as a real application: launched through the register, found by
/// enumeration, read through UI Automation. Nothing is stubbed, because a fixture proved against a
/// stub proves the stub.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class FixtureTests : IDisposable
{
    private readonly ProcessRegister register = new();

    public void Dispose() => register.Dispose();

    /// <summary>
    /// The fixture's executable, in its own output directory. Not copied beside the suite: the
    /// copy brought the apphost without its assembly, and every launch died at CLR startup with a
    /// number that says nothing about which file was missing.
    /// </summary>
    private static string Executable()
    {
        var here = new DirectoryInfo(AppContext.BaseDirectory);
        var framework = here.Name;
        var configuration = here.Parent!.Name;

        var repository = here;
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "Winwright.slnx")))
            repository = repository.Parent;

        Assert.NotNull(repository);
        var path = Path.Combine(
            repository.FullName, "src", "Winwright.Fixture", "bin", configuration, framework, "Winwright.Fixture.exe");

        Assert.True(File.Exists(path), $"the fixture was not built: {path}");
        return path;
    }

    /// <summary>Launch it and wait for the window it draws, which is the only signal worth waiting on.</summary>
    private TopLevelWindow Launched()
    {
        var launched = Attachable.Launch(register, new ProcessStartInfo(Executable()));

        for (var attempt = 0; attempt < 200; attempt++)
        {
            var window = TopLevelWindows.Largest(launched.Pid);
            if (window is not null && window.Title.Length > 0)
                return window;

            Thread.Sleep(25);
        }

        Assert.Fail($"the fixture never drew a window (pid {launched.Pid})");
        return null!;
    }

    [Fact]
    public void The_fixture_launches_and_draws_a_window_of_its_own()
    {
        var window = Launched();

        Assert.Equal("winwright fixture", window.Title);
        Assert.True(window.OnScreen, window.ToString());
        Assert.False(window.IsOwned, "the main window is owned by something, which a frame is not");
    }

    [Fact]
    public void It_needs_nothing_from_the_machine_it_runs_on()
    {
        // Launched with no arguments, no working directory of its own, and an environment carrying
        // nothing this repository set. A fixture needing any of those is one more thing to set up
        // by hand, which is the cost it exists to remove.
        var start = new ProcessStartInfo(Executable()) { WorkingDirectory = Path.GetTempPath() };
        start.Environment.Remove("WINWRIGHT_SURFACES");
        start.Environment.Remove("WINWRIGHT_GEOMETRY");

        var launched = Attachable.Launch(register, start);

        TopLevelWindow? window = null;
        for (var attempt = 0; attempt < 200 && window is null; attempt++)
        {
            window = TopLevelWindows.Largest(launched.Pid);
            if (window is null)
                Thread.Sleep(25);
        }

        Assert.NotNull(window);
    }

    [Fact]
    public void Every_control_on_it_carries_a_name_a_locator_can_be_written_against()
    {
        var tree = Inspect.Window(Launched().Handle, depth: 12);

        Assert.NotNull(tree);
        var ids = tree.Walk().Select(one => one.Facts.AutomationId).Where(one => one.Length > 0).ToList();

        // A surface addressed only by its position is one every layout change breaks.
        Assert.Contains("heading", ids);
        Assert.Contains("subheading", ids);
        Assert.Contains("panes", ids);
        Assert.Contains("save", ids);
        Assert.Contains("close", ids);

        // Not 'page': a layout panel has no automation peer, so a Grid is in no control view
        // however it is named. And not 'profile': a tab's content is realised when the tab is
        // selected, so the Config pane is in no tree until something picks it. Both are facts
        // about Windows that this fixture exists to make reproducible, not naming failures.
        Assert.DoesNotContain("page", ids);
        Assert.DoesNotContain("profile", ids);
    }

    [Fact]
    public void Every_line_of_the_fixture_s_own_tree_is_a_locator_the_grammar_accepts()
    {
        var tree = Inspect.Window(Launched().Handle, depth: 12)!;

        // The fixture's own controls, not the window chrome around them. The chrome's system menu
        // carries an automation id with a space in it - 'Item 1' - which inspect renders into a
        // step the grammar refuses. That is a defect in the rendering rather than in the fixture,
        // and it is filed under its own id rather than worked around here.
        var mine = tree.Children
            .Where(one => one.Facts.AutomationId != "TitleBar")
            .SelectMany(one => one.Walk())
            .ToList();

        Assert.NotEmpty(mine);
        foreach (var element in mine)
        {
            var step = element.Facts.AsLocatorStep().ToString();
            Assert.True(
                Locator.TryParse(step, out _, out var because),
                $"the fixture rendered '{step}', which the grammar refuses: {because}");
        }
    }

    [Fact]
    public void The_three_panes_it_shows_are_the_three_it_always_shows()
    {
        var tree = Inspect.Window(Launched().Handle, depth: 12)!;

        var headers = tree.Walk()
            .Where(one => one.Facts.ControlType == "TabItem")
            .Select(one => one.Facts.Name)
            .ToList();

        // Fixed by construction: two runs on two desks show the same three, which is what makes a
        // derived expectation and a byte-identical render mean anything at all.
        Assert.Equal(["Report", "Status", "Config"], headers);
    }

    [Fact]
    public void Nothing_it_draws_is_read_off_the_machine()
    {
        var tree = Inspect.Window(Launched().Handle, depth: 12)!;
        var texts = tree.Walk().Select(one => one.Facts.Name).Where(one => one.Length > 0).ToList();

        // The three things a fixture accidentally leaks: who is running it, where, and when.
        Assert.DoesNotContain(texts, one => one.Contains(Environment.MachineName, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(texts, one => one.Contains(Environment.UserName, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(texts, one => one.Contains(DateTime.Now.Year.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public void Stopping_it_leaves_no_process_running()
    {
        var window = Launched();
        Win32Pid(window, out var pid);

        register.StopAll();

        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (!Alive(pid))
                return;

            Thread.Sleep(20);
        }

        Assert.Fail($"pid {pid} was still running after the register stopped everything");
    }

    private static void Win32Pid(TopLevelWindow window, out int pid) => pid = window.Pid;

    private static bool Alive(int pid)
    {
        try
        {
            using var running = Process.GetProcessById(pid);
            return !running.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

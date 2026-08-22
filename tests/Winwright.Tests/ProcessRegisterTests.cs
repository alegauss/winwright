using System.Diagnostics;

using Winwright.Processes;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW8. Two trays a failing case had started were still alive afterwards, the next build died on
/// a file lock naming their process ids, and the command after that ran the previous executable.
/// These drive real processes, because a register that is only unit-tested proves nothing about
/// what is running on the desk afterwards.
/// </summary>
[Collection(WindowFixture.Serial)]
public class ProcessRegisterTests
{
    /// <summary>Something that stays alive on its own and needs no console or window.</summary>
    private static ProcessStartInfo LongRunning()
    {
        var start = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };
        start.ArgumentList.Add("/c");
        start.ArgumentList.Add("ping -n 120 127.0.0.1");
        return start;
    }

    private static ProcessStartInfo Brief()
    {
        var start = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };
        start.ArgumentList.Add("/c");
        start.ArgumentList.Add("exit 0");
        return start;
    }

    private static bool StillRunning(int pid)
    {
        try
        {
            using var found = Process.GetProcessById(pid);
            found.Refresh();
            return !found.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    [Fact]
    public void A_case_that_returns_early_still_has_its_process_stopped()
    {
        int pid;

        // The case: it launches, and then returns down a path that stops nothing.
        using (var register = new ProcessRegister())
        {
            pid = register.Launch(LongRunning()).Pid;
            Assert.True(StillRunning(pid));
        }

        Assert.False(StillRunning(pid));
    }

    [Fact]
    public void What_outlived_the_case_is_named_rather_than_cleaned_up_in_silence()
    {
        using var register = new ProcessRegister();
        var launched = register.Launch(LongRunning());

        var survivors = register.StopAll();

        var survivor = Assert.Single(survivors);
        Assert.Equal(launched.Pid, survivor.Pid);
        Assert.Equal("cmd.exe", survivor.Executable);
        Assert.Equal(SurvivorFate.Stopped, survivor.Fate);
        Assert.Contains($"pid {launched.Pid} cmd.exe - outlived its case and was stopped", survivor.ToString());
    }

    [Fact]
    public void A_process_that_went_on_its_own_is_not_a_survivor()
    {
        using var register = new ProcessRegister();
        var launched = register.Launch(Brief());
        Assert.True(launched.WaitForExit(10000));

        Assert.Empty(register.StopAll());
    }

    [Fact]
    public void The_register_is_total_because_every_launch_goes_through_it()
    {
        using var register = new ProcessRegister();
        register.Launch(LongRunning());
        register.Launch(LongRunning());

        Assert.Equal(2, register.Launched.Count);
        Assert.Equal(2, register.StopAll().Count);
    }

    [Fact]
    public void A_launched_process_cannot_be_constructed_any_other_way()
    {
        var constructors = typeof(LaunchedProcess).GetConstructors();

        Assert.Empty(constructors);
    }

    [Fact]
    public void Stopping_twice_answers_what_the_first_stop_found()
    {
        using var register = new ProcessRegister();
        register.Launch(LongRunning());

        var first = register.StopAll();

        Assert.Same(first, register.StopAll());
        Assert.Same(first, register.Survivors);
    }

    [Fact]
    public void Launching_after_the_run_ended_is_refused_rather_than_left_unregistered()
    {
        var register = new ProcessRegister();
        register.StopAll();

        Assert.Throws<ObjectDisposedException>(() => register.Launch(LongRunning()));
    }

    [Fact]
    public void Nothing_left_behind_still_says_so()
    {
        using var register = new ProcessRegister();

        Assert.Empty(register.StopAll());
        Assert.Equal("no process outlived the run that started it.", ProcessSummary.Sentence(register.Survivors));
        Assert.Empty(ProcessSummary.Detail(register.Survivors));
    }

    [Fact]
    public void The_summary_counts_what_stopped_apart_from_what_would_not()
    {
        var survivors = new[]
        {
            new Survivor(1234, "ClaudeTray.exe", SurvivorFate.Stopped),
            new Survivor(1235, "ClaudeTray.exe", SurvivorFate.WouldNotStop),
        };

        Assert.Equal("2 outlived the run: 1 stopped, 1 would not stop.", ProcessSummary.Sentence(survivors));
        Assert.Equal(
            ["  survived   pid 1234 ClaudeTray.exe - outlived its case and was stopped",
             "  survived   pid 1235 ClaudeTray.exe - outlived its case and would not stop"],
            ProcessSummary.Detail(survivors));
    }

    [Fact]
    public void A_process_given_no_time_to_stop_is_refused_at_construction()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProcessRegister(0));
    }

    [Fact]
    public void The_stop_timeout_comes_from_the_project_declaration()
    {
        var root = Directory.CreateTempSubdirectory("winwright-stop-").FullName;
        try
        {
            File.WriteAllText(
                Path.Combine(root, Winwright.Projects.ProjectDeclaration.FileName),
                """{ "timeouts": { "stop": 900 } }""");

            var declaration = Winwright.Projects.ProjectDeclaration.Find(root);
            Assert.Equal(900, declaration.Timeouts.For("stop"));

            using var register = ProcessRegister.For(declaration);
            var pid = register.Launch(LongRunning()).Pid;
            register.StopAll();

            Assert.False(StillRunning(pid));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

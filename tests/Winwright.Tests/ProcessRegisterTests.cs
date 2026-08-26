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
    public void A_case_that_gives_its_process_back_gives_it_back_before_the_run_ends()
    {
        // WW215. The claim is about when, not whether: StopAll already stopped everything, and a
        // suite of nine held nine windows until the last case was done.
        using var register = new ProcessRegister();
        var first = register.Launch(LongRunning());
        var second = register.Launch(LongRunning());

        Assert.Null(register.Stop(first));

        Assert.False(StillRunning(first.Pid));
        Assert.True(StillRunning(second.Pid), "the other case's process was stopped along with it");
        Assert.Equal(2, register.Launched.Count);
    }

    [Fact]
    public void A_process_given_back_did_not_outlive_its_case_and_is_not_reported_as_having()
    {
        // The whole vocabulary of Survivor is about outliving a case. Nine cases each cleanly
        // ending their own process must not read as nine leftovers, which is what a stop that
        // recorded itself would say.
        using var register = new ProcessRegister();
        var launched = register.Launch(LongRunning());

        register.Stop(launched);

        Assert.Empty(register.StopAll());
        Assert.Equal("no process outlived the run that started it.", ProcessSummary.Sentence(register.Survivors));
        Assert.True(register.AsFinding().Holds);
    }

    [Fact]
    public void Stopping_one_twice_is_the_second_call_finding_it_already_gone()
    {
        using var register = new ProcessRegister();
        var launched = register.Launch(LongRunning());

        Assert.Null(register.Stop(launched));
        Assert.Null(register.Stop(launched));
    }

    [Fact]
    public void A_process_this_register_never_launched_is_refused_rather_than_stopped()
    {
        // Otherwise the one list there is stops being the one list there is.
        using var mine = new ProcessRegister();
        using var theirs = new ProcessRegister();
        var elsewhere = theirs.Launch(LongRunning());

        var refused = Assert.Throws<ArgumentException>(() => mine.Stop(elsewhere));

        Assert.Contains($"pid {elsewhere.Pid} was not launched by this register", refused.Message);
        Assert.True(StillRunning(elsewhere.Pid), "refusing to stop it stopped it");
    }

    [Fact]
    public void Giving_a_process_back_after_the_roll_is_taken_is_refused()
    {
        // The same rule Launch keeps: after the roll, the reading is what it is, and a register that
        // let one more process through would answer a question it had already answered.
        var register = new ProcessRegister();
        var launched = register.Launch(LongRunning());
        register.StopAll();

        Assert.Throws<ObjectDisposedException>(() => register.Stop(launched));
        register.Dispose();
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

    [Fact]
    public void A_register_nobody_stopped_has_taken_no_roll_rather_than_found_nothing()
    {
        // WW152. Survivors answers an empty list twice over, and the two are a reading not taken
        // and a reading that came back clean. A caller that could not tell them apart would print
        // "nothing outlived the run" about a run whose processes are all still going — which is
        // this project's founding defect with a different subject.
        using var register = new ProcessRegister();
        var pid = register.Launch(LongRunning()).Pid;

        Assert.False(register.Stopped);
        Assert.Empty(register.Survivors);

        var before = register.AsFinding();

        Assert.False(before.Was);
        Assert.Null(before.Holds);
        Assert.Contains("never asked to stop", before.Sentence, StringComparison.Ordinal);
        Assert.StartsWith("  not read ", before.ToString(), StringComparison.Ordinal);

        // And the process really is still going, which is what makes the empty list a lie a caller
        // could have told.
        Assert.True(StillRunning(pid), "the long-running process was not running, so this proves nothing");
    }

    [Fact]
    public void A_run_that_had_to_stop_something_says_so_where_the_rest_of_the_run_is_read()
    {
        // The case the criterion was written for: a process still alive at the end is the
        // difference between a scenario that tidied up and one that left the machine in a state
        // the next run inherits, and both used to print the same nothing.
        using var register = new ProcessRegister();
        register.Launch(LongRunning());
        register.StopAll();

        var finding = register.AsFinding();

        Assert.True(finding.Was);
        Assert.False(finding.Holds);
        Assert.Contains("outlived the run", finding.Sentence, StringComparison.Ordinal);
        Assert.StartsWith("  differs ", finding.ToString(), StringComparison.Ordinal);
        Assert.Equal(ProcessSummary.Sentence(register.Survivors), finding.Sentence);
    }

    [Fact]
    public void A_run_that_left_nothing_behind_says_that_rather_than_saying_nothing()
    {
        using var register = new ProcessRegister();
        register.Launch(Brief()).WaitForExit(10_000);
        register.StopAll();

        var finding = register.AsFinding();

        Assert.True(finding.Holds);
        Assert.Empty(register.Survivors);
        Assert.Equal("no process outlived the run that started it.", finding.Sentence);
    }
}

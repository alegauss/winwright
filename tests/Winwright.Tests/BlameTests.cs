using Winwright.Blaming;

using Xunit;

using Thread = Winwright.Blaming.Thread;

namespace Winwright.Tests;

/// <summary>
/// WW406. What a hang dump says, read out of the frames rather than out of a dump — which is the
/// half that can be checked at all. A stack this reader has never met can be written down here and
/// the sentence it produces asserted; a dump cannot, because the run that produced one is a run
/// nobody can arrange.
/// <para>
/// The stacks here are real. The first is the one the third of these deaths actually left behind,
/// copied out of `testhost-hangdump-20260906b.dmp`, and it is the case this whole task exists for.
/// </para>
/// </summary>
public sealed class BlameTests
{
    /// <summary>
    /// The thread that was running a case when the host stopped, as the dump held it. Innermost
    /// first, trimmed of the sixty frames of test-platform machinery underneath.
    /// </summary>
    private static Thread TheDeath => new(
        11708,
        20,
        [
            "[InlinedCallFrame] (.GetWindowTextW)",
            "[InlinedCallFrame] (.GetWindowTextW)",
            "Winwright.Windowing.Win32.TextOf()",
            "Winwright.Windowing.TopLevelWindows+<>c__DisplayClass5_0.<OfProcess>b__0()",
            "[InlinedCallFrame] (.EnumWindows)",
            "[InlinedCallFrame] (.EnumWindows)",
            "Winwright.Windowing.TopLevelWindows.OfProcess()",
            "Winwright.Scenarios.CaseRun.Captured()",
            "Winwright.Scenarios.CaseRun.Performing()",
            "Winwright.Scenarios.CaseRun.Perform()",
            "Winwright.Scenarios.CaseRun.Stepping()",
            "Winwright.Scenarios.CaseRun.Of()",
            "Winwright.Scenarios.Suite.Run()",
            "Winwright.Tests.SuiteRunTests.A_project_that_declares_captures_runs_the_step_rather_than_refusing_it()",
            "System.Reflection.RuntimeMethodInfo.Invoke(System.Object, System.Reflection.BindingFlags)",
        ]);

    /// <summary>The test host's own main thread, which waits on the run and always has.</summary>
    private static Thread TheHost => new(
        12980,
        2,
        [
            "System.Threading.Monitor.Wait(System.Object, Int32)",
            "System.Threading.ManualResetEventSlim.Wait(Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task.Wait()",
            "Microsoft.VisualStudio.TestPlatform.TestHost.Program.Run(System.String[])",
        ]);

    [Fact]
    public void The_thread_that_was_in_our_own_code_is_named_first_with_what_it_was_waiting_on()
    {
        var said = Waiting.Said([TheHost, TheDeath]);

        // The whole answer in one line: which thread, which of our methods, under which case, and
        // the native call it never came back from.
        Assert.Contains("thread 11708", said[0], StringComparison.Ordinal);
        Assert.Contains("Winwright.Windowing.Win32.TextOf", said[0], StringComparison.Ordinal);
        Assert.Contains(
            "SuiteRunTests.A_project_that_declares_captures_runs_the_step_rather_than_refusing_it",
            said[0],
            StringComparison.Ordinal);

        Assert.Contains("GetWindowTextW", said[0], StringComparison.Ordinal);
    }

    [Fact]
    public void The_case_is_named_and_never_the_deepest_frame_that_happens_to_be_ours()
    {
        // Both are ours and they answer different questions: the innermost is the line to open, and
        // the outermost is what to re-run. A reading that named `Suite.Run` as the case would send
        // the reader to the engine's own entry point, which is in every one of these stacks.
        var blamed = Waiting.Blamed([TheHost, TheDeath]);

        Assert.NotNull(blamed);
        Assert.Equal(11708u, blamed.Os);
    }

    [Fact]
    public void A_native_call_is_named_by_its_entry_point_and_not_by_the_marker_frame()
    {
        // The runtime leaves `[InlinedCallFrame]` behind for a P/Invoke and puts the entry point in
        // brackets after it. The marker on its own says nothing at all, and it is the frame a naive
        // reading would print — which is how a hang inside GetWindowTextW reads as "InlinedCallFrame".
        Assert.Equal("GetWindowTextW", Waiting.Native("[InlinedCallFrame] (.GetWindowTextW)"));
        Assert.Equal(
            "System.Threading.WaitHandle.<WaitOneCore>g____PInvoke|0_0",
            Waiting.Native("[InlinedCallFrame] (System.Threading.WaitHandle.<WaitOneCore>g____PInvoke|0_0)"));

        Assert.Null(Waiting.Native("[InlinedCallFrame]"));
        Assert.Null(Waiting.Native("Winwright.Windowing.Win32.TextOf()"));
    }

    [Fact]
    public void Every_other_thread_that_waits_is_listed_so_two_waiting_on_each_other_is_visible()
    {
        var said = Waiting.Said([TheHost, TheDeath]);

        Assert.Equal(2, said.Count);
        Assert.Contains("thread 12980 waits in", said[1], StringComparison.Ordinal);

        // The one named at the top is not listed again underneath: a reader counting the waits
        // would otherwise count the one they were just told about twice.
        Assert.DoesNotContain("thread 11708", said[1], StringComparison.Ordinal);
    }

    [Fact]
    public void A_dump_with_no_frame_of_ours_says_so_rather_than_saying_nothing()
    {
        // A hang inside the test platform is a finding, and it is not the finding somebody reading
        // an empty answer would assume. Silence here reads as "the tool found nothing wrong".
        var said = Waiting.Said([TheHost]);

        Assert.Contains("no thread", said[0], StringComparison.Ordinal);
        Assert.Contains("test platform", said[0], StringComparison.Ordinal);
    }

    [Fact]
    public void A_dump_holding_no_threads_is_reported_as_not_being_a_dump_of_a_running_process()
    {
        var only = Assert.Single(Waiting.Said([]));

        Assert.Contains("no threads", only, StringComparison.Ordinal);
    }

    [Fact]
    public void The_runner_reads_the_dump_it_keeps_rather_than_only_printing_where_it_put_it()
    {
        // The pair, in one case, because the failure is always the pair: a runner that fetches a
        // dump and never reads it is the state WW406 was filed against with one more file in it,
        // and a reader nothing calls is a tool that answers a question nobody asks it.
        var runner = File.ReadAllText(Checkout.At("tools", "run-tests-vm.ps1"));

        Assert.Contains("hangdump.dmp", runner, StringComparison.Ordinal);
        Assert.Contains("Show-Blame -Dump", runner, StringComparison.Ordinal);

        // And what it reaches for is in this checkout. A path in a script is checked by nothing on
        // the runs that matter — this arm fires only where a test host has died, which is three
        // times in about fifteen guest runs.
        Assert.True(
            File.Exists(Checkout.At("tools", "Winwright.Blame", "Winwright.Blame.csproj")),
            "the runner names a reader this checkout does not build");
    }

    [Fact]
    public void A_thread_that_is_running_rather_than_waiting_is_said_to_be_running()
    {
        // The reading has to be able to say a thread was not waiting, because a spin is the other
        // way a run goes away and "waiting in" over a spin would be a sentence that reads true.
        var spinning = new Thread(
            99,
            7,
            ["Winwright.Locating.Admitted.Matching()", "Winwright.Tests.SpinTests.A_case()"]);

        var said = Waiting.Said([spinning]);

        Assert.Contains("was not waiting", said[0], StringComparison.Ordinal);
    }
}

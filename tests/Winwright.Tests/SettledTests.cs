using System.Diagnostics;

using Winwright.Processes;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW205. Stopped and gone told apart, on the register rather than in this suite. Two tasks needed
/// the difference — WW126 for the desktop and WW201 for a file lock — and both answered it here,
/// where an adopter cannot reach it.
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class SettledTests
{
    private static ProcessStartInfo LongRunning()
    {
        var start = new ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add("/c");
        start.ArgumentList.Add("ping -n 120 127.0.0.1");
        return start;
    }

    [Fact]
    public void A_register_that_started_something_says_it_has_left_the_machine()
    {
        using var register = new ProcessRegister();
        var launched = Attachable.Launch(register, LongRunning());

        var settled = Settled.Of(register, Waits.Declared.For("gone"));

        Assert.True(settled.Gone, settled.Sentence());
        Assert.Empty(settled.Lingering);
        Assert.Single(settled.Stopped, one => one.Pid == launched.Pid);
        Assert.True(settled.AsFinding().Holds);
        Assert.Contains("had left the machine after", settled.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_register_that_started_nothing_says_that_rather_than_nothing()
    {
        using var register = new ProcessRegister();

        var settled = Settled.Of(register, Waits.Declared.For("gone"));

        Assert.True(settled.Gone);
        Assert.Empty(settled.Stopped);
        Assert.Contains("nothing this run started was still running", settled.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_deadline_of_nothing_is_a_reading_that_was_never_taken()
    {
        // The third state, and the reason this is a reading rather than a bool. A caller that asked
        // with no deadline gets told nothing was waited for — never that everything had gone, which
        // is what a bool would have had to say.
        using var register = new ProcessRegister();

        var settled = Settled.Of(register, deadlineMs: 0);

        Assert.False(settled.Was);
        Assert.False(settled.Gone);
        Assert.Null(settled.AsFinding().Holds);
        Assert.Contains("nothing was waited for", settled.Sentence(), StringComparison.Ordinal);
        Assert.Equal(AssertionOutcome.Unchecked, settled.AsAssertion("everything has gone").Outcome);
    }

    [Fact]
    public void A_pid_the_machine_will_not_open_is_gone_and_never_still_running()
    {
        // The mistake an adopter writing this themselves would make, asserted so the engine's
        // version cannot drift into it: GetProcessById throws for a pid that has left, and reading
        // that throw as anything but "gone" reports a departed process as one still running.
        using var register = new ProcessRegister();
        var launched = Attachable.Launch(register, LongRunning());

        // Stopped and settled once, so the pid is genuinely out of the machine.
        Assert.True(Settled.Of(register, Waits.Declared.For("gone")).Gone);

        // Asked again about the same pid: the process object no longer opens, and the answer is
        // still that it has gone rather than a throw reaching the caller.
        var again = Settled.Of(register, Waits.Declared.For("gone"));

        Assert.True(again.Gone, again.Sentence());
        Assert.DoesNotContain($"pid {launched.Pid}", again.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void The_suite_no_longer_carries_its_own_copy_of_the_wait()
    {
        // The deletion is the proof, which is Block J's criterion pointed at this repository. What
        // is left in Attachable is the deadline this suite declares and the assertion; the walk and
        // the reading of a pid are the engine's.
        var attachable = File.ReadLines(Path.Combine(Checkout.Suite, "Winwright.Tests", "Attachable.cs"))
            .Select(Checkout.Code)
            .ToList();

        Assert.DoesNotContain(attachable, one => one.Contains("GetProcessById", StringComparison.Ordinal)
            && one.Contains("running", StringComparison.Ordinal));
        Assert.Contains(attachable, one => one.Contains("Settled.Of(", StringComparison.Ordinal));
    }
}

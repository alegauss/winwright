using Winwright.Locating;
using Winwright.Projects;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW211. The gate that separates a machine which was not given time from a fixture that wrote
/// nothing — see <see cref="SlowMachine" /> for why the two must not share an excuse.
/// </summary>
public sealed class SlowMachineTests
{
    private static Waited RanOut(string named) =>
        Attempt.UntilTrue(() => false, Waits.Declared.For(named), Waits.Declared.For("poll"));

    [Fact]
    public void A_wait_that_used_its_whole_budget_and_produced_nothing_is_excusable()
    {
        // The one this task is about, and the negative control the roadmap named: against a budget
        // this suite declares, a condition that never holds runs out, and running out is a fact
        // about how long this suite waited.
        var waited = RanOut("desktop");

        SlowMachine.Excusing("desktop", waited, absent: true);

        Assert.False(waited.Happened);
        Assert.True(waited.WaitedMs >= Waits.Declared.For("desktop"));
    }

    [Fact]
    public void A_wait_that_happened_has_nothing_to_excuse()
    {
        var waited = Attempt.UntilTrue(() => true, Waits.Declared.For("desktop"), Waits.Declared.For("poll"));

        var refused = Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => SlowMachine.Excusing("desktop", waited, absent: true));

        Assert.Contains("there is nothing to excuse", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_deadline_this_suite_does_not_declare_cannot_be_run_out_of()
    {
        // A name invented at the call site would be a budget nobody can find, and this would then
        // be excusing itself against a number it made up.
        var refused = Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => SlowMachine.Excusing("never-declared", RanOut("desktop"), absent: true));

        Assert.Contains("is not a deadline this suite declares", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_wait_that_gave_up_early_did_not_run_out_of_time()
    {
        // Built by hand because nothing here gives up early on purpose. What it stands for is a
        // wait that answered on its own terms rather than against the clock — and that failure is
        // about whatever answered, never about the machine.
        var early = new Waited(false, WaitedMs: 12, Polls: 3);

        var refused = Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => SlowMachine.Excusing("desktop", early, absent: true));

        Assert.Contains("did not run out of time", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Something_partly_there_is_never_the_machines_to_excuse()
    {
        // The half that keeps WW164. A dump that exists and reads as nothing is the fixture's
        // doing; excusing it would withdraw the very check WW164 added, on the run where it fires.
        var refused = Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => SlowMachine.Excusing("desktop", RanOut("desktop"), absent: false));

        Assert.Contains("partly there", refused.Message, StringComparison.Ordinal);
        Assert.Contains("rather than about how long this suite waited", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_sentence_says_it_is_unchecked_and_makes_no_claim_about_the_fixture()
    {
        var said = SlowMachine.Sentence("desktop", "pid 4 writing what it drew", RanOut("desktop"));

        Assert.StartsWith("unchecked: ", said, StringComparison.Ordinal);
        Assert.Contains("pid 4 writing what it drew", said, StringComparison.Ordinal);
        Assert.Contains($"{Waits.Declared.For("desktop")}ms this suite declares", said, StringComparison.Ordinal);
        Assert.Contains("was not given time", said, StringComparison.Ordinal);
    }

    [Fact]
    public void This_gate_is_not_the_desks_and_says_so_by_taking_a_name_the_desk_would_refuse()
    {
        // The two excuses are deliberately not interchangeable. BusyDesk demands a condition the
        // engine calls the desk's; a deadline this suite declares is not one of those and never
        // will be, which is why borrowing that gate was refused rather than widened.
        Assert.False(Winwright.Verdicts.DeskFacts.Names("wrote"));
        Assert.False(Winwright.Verdicts.DeskFacts.Names("draw"));

        // And every deadline this suite declares is reachable by name, so the gate above can
        // actually be closed rather than only claimed.
        Assert.All(
            new[] { "wrote", "draw", "readable", "gone", "loaded", "desktop" },
            one => Assert.True(Waits.Declared.For(one) > 0, $"'{one}' is not declared"));
    }
}

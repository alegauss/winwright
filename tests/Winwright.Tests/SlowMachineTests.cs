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

    /// <summary>
    /// WW280. A watch that kept pace, one that could not, and one that never measured a cadence —
    /// the same three states the wait gate above is held to, on the other number this suite chooses.
    /// </summary>
    /// <param name="taken">How many looks the run got.</param>
    /// <param name="apartMs">How far apart to space them.</param>
    private static Looks Watching(int taken, double apartMs) =>
        Looks.Over(
            Enumerable.Range(0, taken).Select(one => one * apartMs).ToList(),
            wanted: 3,
            lastingMs: 600);

    [Fact]
    public void A_run_that_could_not_look_often_enough_measured_nothing_about_what_it_watched()
    {
        // The guest run this task was filed from: 241ms a look at a 600ms state, which is fewer
        // than three looks each and so cannot have seen the sequence it was about to judge.
        var looks = Watching(taken: 20, apartMs: 241);

        Assert.False(looks.Enough);
        SlowMachine.Excusing(looks);

        var said = SlowMachine.Sentence("a cycle of 600ms a state", looks);

        Assert.StartsWith("unchecked: ", said, StringComparison.Ordinal);
        Assert.Contains("a cycle of 600ms a state", said, StringComparison.Ordinal);
        Assert.Contains("was not given time", said, StringComparison.Ordinal);
        Assert.Contains("nothing here is a claim about the fixture", said, StringComparison.Ordinal);

        // The cadence it actually met, so a reader can tell how far short it fell rather than only
        // that it did.
        Assert.Contains("241ms", said, StringComparison.Ordinal);
    }

    [Fact]
    public void A_run_that_kept_pace_has_nothing_to_excuse()
    {
        // Three looks a state exactly is not enough — the guard is strict, because a sampler at the
        // edge loses a member to any jitter and a count that lost members is a confident number
        // about the application.
        var looks = Watching(taken: 60, apartMs: 100);

        Assert.True(looks.Enough);

        var refused = Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => SlowMachine.Excusing(looks));

        Assert.Contains("there is nothing to excuse", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_run_that_got_one_look_has_not_shown_the_machine_was_slow()
    {
        // The half that keeps the check. One look is a gap between nothing: it is as likely to be a
        // window that answered once as a desk that could not keep up, and excusing it would
        // withdraw whichever of the two it actually was.
        var refused = Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => SlowMachine.Excusing(Watching(taken: 1, apartMs: 241)));

        Assert.Contains("no cadence at all", refused.Message, StringComparison.Ordinal);
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

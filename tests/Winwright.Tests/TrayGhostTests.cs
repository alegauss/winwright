using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW173. What a run leaves in the notification area, read back rather than remembered. The
/// classification is driven with names and a liveness answer this case supplies, because provoking
/// a real ghost means killing a run mid-flight — and a rule that can only be checked that way is a
/// rule nothing checks.
/// </summary>
public sealed class TrayGhostTests
{
    private static bool Nothing(int pid) => false;

    private static bool Everything(int pid) => true;

    [Fact]
    public void An_icon_whose_run_has_ended_is_named()
    {
        var ghosts = TrayGhosts.Among(["winwright under test #4321"], Nothing);

        Assert.Equal(["winwright under test #4321"], ghosts);
    }

    [Fact]
    public void An_icon_whose_run_is_still_going_is_this_run_and_never_a_ghost()
    {
        Assert.Empty(TrayGhosts.Among(["winwright under test #4321"], Everything));
    }

    [Fact]
    public void Somebody_elses_icon_is_left_alone_however_dead_its_process()
    {
        // The reading is about what this suite left, and a leftover nobody here can act on is a
        // leftover nobody should be told about as though they could.
        Assert.Empty(TrayGhosts.Among(["Windows Security", "Volume #99999"], Nothing));
    }

    [Fact]
    public void A_winwright_icon_carrying_no_mark_is_not_judged_either_way()
    {
        // WW126 puts the pid on every tip this suite adds, so an unmarked one came from somewhere
        // else or from before that. Calling it a ghost would be a claim this reading cannot make.
        Assert.Empty(TrayGhosts.Among(["winwright something"], Nothing));
    }

    [Fact]
    public void The_tip_is_read_off_its_first_line_because_a_real_one_runs_to_several()
    {
        var ghosts = TrayGhosts.Among(["winwright under test #4321\nstatus: idle\nqueued: 2"], Nothing);

        Assert.Equal(["winwright under test #4321"], ghosts);
    }

    [Fact]
    public void A_desk_read_all_the_way_through_and_holding_nothing_says_so()
    {
        var census = new TrayCensus([], everywhere: true, "");

        Assert.True(census.Clean);
        Assert.Equal("nothing this suite added is still in the notification area.", census.Sentence());
        Assert.Equal(
            Winwright.Verdicts.AssertionOutcome.Passed,
            census.AsAssertion("nothing was left").Outcome);
    }

    [Fact]
    public void A_desk_holding_ghosts_names_them_and_says_nothing_here_can_take_them_away()
    {
        var census = new TrayCensus(
            ["winwright under test #4321", "winwright placement #4321"], everywhere: true, "");

        Assert.False(census.Clean);
        Assert.Contains("still holds 2 icon(s)", census.Sentence(), StringComparison.Ordinal);
        Assert.Contains("no process here can withdraw", census.Sentence(), StringComparison.Ordinal);
        Assert.Contains("winwright placement #4321", census.Sentence(), StringComparison.Ordinal);
        Assert.Equal(
            Winwright.Verdicts.AssertionOutcome.Failed,
            census.AsAssertion("nothing was left").Outcome);
    }

    [Fact]
    public void A_census_that_could_not_open_the_overflow_never_claims_a_clean_desk()
    {
        // WW181, and the whole of it. The first spelling of this reading answered the taskbar alone
        // and said "nothing this suite added is still in the notification area", which is a green
        // covering what never ran — shipped inside the reading meant to stop exactly that.
        var census = new TrayCensus([], everywhere: false, "the taskbar shows no chevron");

        Assert.False(census.Clean);
        Assert.DoesNotContain(
            "nothing this suite added is still in the notification area.",
            census.Sentence(),
            StringComparison.Ordinal);
        Assert.Contains("the overflow was not read", census.Sentence(), StringComparison.Ordinal);
        Assert.Contains("the taskbar shows no chevron", census.Sentence(), StringComparison.Ordinal);

        // A hole and not a failure: the shell never got asked, so nothing about this suite's
        // leftovers was observed either way.
        var verdict = census.AsAssertion("nothing was left");
        Assert.Equal(Winwright.Verdicts.AssertionOutcome.Unchecked, verdict.Outcome);
        Assert.Equal(Winwright.Acting.TraySearch.PreconditionName, verdict.Missing!.Name);
    }

    [Fact]
    public void What_the_taskbar_held_is_still_reported_where_the_overflow_could_not_be_read()
    {
        // Half an answer is worth having and is not worth rounding up. What it must not do is read
        // as the whole one, so the count comes with the sentence saying it is a floor.
        var census = new TrayCensus(["winwright under test #4321"], everywhere: false, "no chevron");

        Assert.False(census.Clean);
        Assert.Contains("still holds 1 icon(s)", census.Sentence(), StringComparison.Ordinal);
        Assert.Contains("the overflow was not read", census.Sentence(), StringComparison.Ordinal);
        Assert.Equal(
            Winwright.Verdicts.AssertionOutcome.Unchecked,
            census.AsAssertion("nothing was left").Outcome);
    }

    [Fact]
    public void This_runs_own_process_is_alive_so_its_own_icons_are_never_ghosts()
    {
        // The control. Running is a real reading and not a constant, so a case built on it would
        // prove nothing if it answered false for everything.
        Assert.True(TrayGhosts.Running(Environment.ProcessId));
        Assert.Empty(TrayGhosts.Among([$"winwright under test #{Environment.ProcessId}"], TrayGhosts.Running));
    }
}

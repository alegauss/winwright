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
    public void The_sentence_says_so_either_way_rather_than_saying_nothing()
    {
        Assert.Equal(
            "nothing this suite added is still in the notification area.",
            TrayGhosts.Sentence([]));

        var said = TrayGhosts.Sentence(["winwright under test #4321", "winwright placement #4321"]);

        Assert.Contains("still holds 2 icon(s)", said, StringComparison.Ordinal);
        Assert.Contains("no process here can withdraw", said, StringComparison.Ordinal);
        Assert.Contains("winwright placement #4321", said, StringComparison.Ordinal);
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

using Winwright.Processes;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW156. The reading is taken before anything blames the code, and the refusal it produces is one
/// statement about the run rather than one per case.
/// <para>
/// Two halves, and they are provable in different places. That the desk joins the one composition
/// point is asserted against the real machine, because the point of joining it there is that a
/// runner cannot forget it. That a blocked desk refuses the whole run is asserted through
/// <see cref="Desk.Blocked(Precondition)"/>, since none of the six conditions is forceable in
/// process and the path would otherwise be reachable only on a machine nobody has.
/// </para>
/// </summary>
public class DeskGateTests
{
    private static Precondition Locked() =>
        Precondition.Absent(Desk.InputDesktop, "the input desktop would not open, which is a locked workstation");

    [Fact]
    public void A_desk_that_can_observe_refuses_nothing()
    {
        var desk = Desk.Read();

        // Branching on the real machine rather than asserting one way: this suite runs on desks
        // that have everything and, since WW111, on ones that do not.
        if (desk.CanObserve)
            Assert.Null(desk.Refusal("the whole run"));
        else
            Assert.NotNull(desk.Refusal("the whole run"));
    }

    [Fact]
    public void The_seam_opens_only_towards_a_desk_that_cannot_observe()
    {
        // The whole licence for Blocked being public. A fabricated absence costs a run that would
        // have passed; a fabricated presence would cost every run its meaning, so it is refused.
        var met = Precondition.Met(Desk.InputDesktop);

        var refused = Assert.Throws<ArgumentException>(() => Desk.Blocked(met));
        Assert.Contains(Desk.InputDesktop, refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_blocked_desk_names_what_it_lacks_and_says_it_cannot_observe()
    {
        var desk = Desk.Blocked(Locked());

        Assert.False(desk.CanObserve);
        Assert.NotNull(desk.FirstAbsent);
        Assert.Equal(Desk.InputDesktop, desk.FirstAbsent.Name);
    }

    [Fact]
    public void The_refusal_is_the_run_and_not_the_cases()
    {
        var verdict = Desk.Blocked(Locked()).Refusal("the whole run");

        // The criterion this task was filed under, asserted as a count. A run that stopped by
        // excusing every assertion in turn would satisfy every other line here and fail this one.
        Assert.NotNull(verdict);
        Assert.Single(verdict.Results);
        Assert.Equal(0, verdict.Ran);
        Assert.Empty(verdict.Failures);
        Assert.Empty(verdict.Broke);
    }

    [Fact]
    public void The_refusal_is_degraded_rather_than_failed()
    {
        var verdict = Desk.Blocked(Locked()).Refusal("the whole run");

        // Exit 2 and never 1. Nothing here is a statement about the application, and a reader told
        // this run failed opens a repository that has done nothing wrong.
        Assert.NotNull(verdict);
        Assert.Equal(RunOutcome.Degraded, verdict.Outcome);
        Assert.Equal(2, verdict.ExitCode);
    }

    [Fact]
    public void The_one_result_carries_the_absence_that_decided_it()
    {
        var verdict = Desk.Blocked(Locked()).Refusal("the whole run");

        Assert.NotNull(verdict);
        var only = Assert.Single(verdict.Unchecked);
        Assert.Equal("the whole run", only.Name);
        Assert.NotNull(only.Missing);
        Assert.Equal(Desk.InputDesktop, only.Missing.Name);
        Assert.False(string.IsNullOrWhiteSpace(only.Missing.Absence));
    }

    [Fact]
    public void A_refusal_with_no_name_for_the_run_is_refused()
    {
        // A hole reports what did not run. An unnamed one reports that something did not, which is
        // the shape of degraded reading this project spent WW1 refusing.
        Assert.Throws<ArgumentException>(() => Desk.Blocked(Locked()).Refusal("  "));
    }

    [Fact]
    public void The_preamble_carries_every_condition_the_desk_read()
    {
        var preamble = Preamble.Of(AppTarget.AttachTo(Environment.ProcessId));

        // The half a runner cannot forget: the six are in the one list, not behind a call of their
        // own. Named rather than counted, so a condition quietly dropped is caught here.
        foreach (var expected in new[]
        {
            Desk.InteractiveSession,
            Desk.InputDesktop,
            Desk.RenderingDisplay,
            Desk.TrustworthyCoordinates,
            Desk.AutomationAssemblies,
            Desk.ForegroundToTake,
        })
        {
            Assert.NotNull(preamble.Find(expected));
        }
    }

    [Fact]
    public void A_desk_that_has_everything_still_says_what_it_read()
    {
        var preamble = Preamble.Of(AppTarget.AttachTo(Environment.ProcessId));

        // The task's second criterion. Which of the six is being suffered in silence has never
        // been measured, and a reading printed only when it fails can never answer that.
        var rendered = preamble.Render();
        foreach (var condition in preamble.Machine.Conditions)
            Assert.Contains(rendered, line => line.Contains(condition.Name, StringComparison.Ordinal));
    }

    [Fact]
    public void The_preamble_refuses_exactly_where_its_own_desk_does()
    {
        var preamble = Preamble.Of(AppTarget.AttachTo(Environment.ProcessId));

        // Delegation asserted rather than assumed: two copies of the rule is two things to keep in
        // step, and this is what would catch them parting.
        Assert.Equal(preamble.Machine.CanObserve, preamble.Refusal("the whole run") is null);
    }
}

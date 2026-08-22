using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW68. A machine that can observe nothing reports a build failure, because every one of the
/// reasons it cannot observe arrives dressed as a failing assertion about the code.
/// <para>
/// The reading is taken on whatever desk the suite is running on, so these tests assert the shape
/// of the answer and the rules it obeys rather than the answer itself: a run on a hosted runner
/// and a run at somebody's desk are both correct, and only one of them can say every condition is
/// met.
/// </para>
/// </summary>
public sealed class DeskTests
{
    [Fact]
    public void Every_condition_is_reported_met_ones_included()
    {
        var desk = Desk.Read();

        Assert.Equal(6, desk.Conditions.Count);
        Assert.Equal(
            [
                Desk.InteractiveSession,
                Desk.InputDesktop,
                Desk.RenderingDisplay,
                Desk.TrustworthyCoordinates,
                Desk.AutomationAssemblies,
                Desk.ForegroundToTake,
            ],
            desk.Conditions.Select(one => one.Name));
    }

    [Fact]
    public void Every_absent_condition_says_what_was_missing()
    {
        var desk = Desk.Read();

        // The rule Precondition already enforces, asserted here because a desk reading whose
        // absences say nothing is a report that names a problem and no way to act on it.
        Assert.All(desk.Absent, one => Assert.False(string.IsNullOrWhiteSpace(one.Absence)));
    }

    [Fact]
    public void What_the_desk_can_observe_is_the_absent_list_being_empty_and_nothing_else()
    {
        var desk = Desk.Read();

        Assert.Equal(desk.Absent.Count == 0, desk.CanObserve);
        Assert.Equal(desk.CanObserve, desk.FirstAbsent is null);
    }

    [Fact]
    public void The_first_absence_is_the_first_one_read_so_a_dead_desk_reports_one_cause()
    {
        var desk = Desk.Read();

        if (desk.FirstAbsent is null)
            return;

        // Read in the order they stop each other mattering: no session means no desktop, no
        // display and no foreground, and reporting four absences would name three consequences.
        Assert.Equal(desk.Absent[0].Name, desk.FirstAbsent.Name);
        Assert.Same(desk.Conditions.First(one => !one.Satisfied), desk.FirstAbsent);
    }

    [Fact]
    public void The_report_carries_one_line_per_condition()
    {
        var desk = Desk.Read();

        var rendered = desk.Render();

        Assert.Equal(desk.Conditions.Count, rendered.Count);
        Assert.All(rendered, line => Assert.StartsWith("  ", line));
        Assert.Contains(rendered, line => line.Contains(Desk.AutomationAssemblies, StringComparison.Ordinal));
    }

    [Fact]
    public void The_sentence_never_says_the_desk_is_fine_while_anything_is_absent()
    {
        var desk = Desk.Read();

        var said = desk.Sentence();

        if (desk.CanObserve)
        {
            Assert.Contains("this desk can be observed", said);
            Assert.DoesNotContain("cannot", said);
        }
        else
        {
            Assert.Contains("cannot observe", said);
            Assert.All(desk.Absent, one => Assert.Contains(one.Name, said, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void An_excuse_is_a_hole_naming_the_condition_and_never_a_failure()
    {
        var desk = Desk.Read();

        if (desk.FirstAbsent is null)
        {
            // Nothing about a working desk explains a check that did not run, and answering with
            // a hole anyway would let a run excuse an assertion it was perfectly able to make.
            var refused = Assert.Throws<InvalidOperationException>(() => desk.Excuse("the tab headers"));
            Assert.Contains("nothing about it explains", refused.Message);
            return;
        }

        var result = desk.Excuse("the tab headers");

        Assert.Equal(AssertionOutcome.Unchecked, result.Outcome);
        Assert.True(result.DidNotRun);
        Assert.Equal(desk.FirstAbsent.Name, result.Missing?.Name);
    }

    [Fact]
    public void The_reading_is_taken_now_rather_than_remembered_from_the_last_one()
    {
        // Two readings are two records: a desk cached at first use would answer for the machine as
        // it was when the process started, and a workstation locks partway through a run.
        Assert.NotSame(Desk.Read(), Desk.Read());
    }

    [Fact]
    public void A_suite_running_at_all_proves_the_two_conditions_it_could_not_run_without()
    {
        var desk = Desk.Read();

        // Not a tautology: this suite creates real windows and reads real trees, so a reading that
        // called either of these absent would be a reading that disagrees with the run around it.
        Assert.True(Named(desk, Desk.AutomationAssemblies).Satisfied, Named(desk, Desk.AutomationAssemblies).Absence);
        Assert.True(Named(desk, Desk.InteractiveSession).Satisfied, Named(desk, Desk.InteractiveSession).Absence);
    }

    [Fact]
    public void Coordinates_are_trustworthy_because_this_assembly_sets_them_when_it_loads()
    {
        var desk = Desk.Read();

        Assert.True(
            Named(desk, Desk.TrustworthyCoordinates).Satisfied,
            Named(desk, Desk.TrustworthyCoordinates).Absence);
    }

    private static Precondition Named(Desk desk, string name) =>
        desk.Conditions.Single(one => one.Name == name);
}

using Winwright.Scenarios;
using Winwright.Verdicts;

using Xunit;

using static Winwright.Tests.Fixtures;

namespace Winwright.Tests;

/// <summary>
/// WW2. Unchecked is what an absent precondition produces, and nothing else produces it: an
/// assertion that could never run anywhere is refused at load rather than counted as a hole.
/// </summary>
public class PreconditionTests
{
    [Fact]
    public void An_assertion_whose_preconditions_are_all_met_is_free_to_run()
    {
        var declaration = AssertionDeclaration.Of("the tray menu opens", "the notification area", "a free notification area");
        var machine = PreconditionSet.Of(Precondition.Met("a free notification area"));

        Assert.Null(machine.FirstAbsent(declaration));
    }

    [Fact]
    public void An_absent_precondition_is_the_hole_it_explains()
    {
        var declaration = AssertionDeclaration.Of("the tray menu opens", "the notification area", "a free notification area");
        var machine = PreconditionSet.Of(FreeNotificationArea);

        var missing = machine.FirstAbsent(declaration);

        Assert.NotNull(missing);
        Assert.Equal("a free notification area", missing.Name);
        Assert.Equal(AssertionOutcome.Unchecked, declaration.Unchecked(missing).Outcome);
    }

    [Fact]
    public void The_first_absent_precondition_is_the_one_reported()
    {
        var declaration = AssertionDeclaration.Of(
            "the report renders", "the report file", "a registered profile", "a free notification area");
        var machine = PreconditionSet.Of(RegisteredProfile, FreeNotificationArea);

        Assert.Equal("a registered profile", machine.FirstAbsent(declaration)!.Name);
    }

    [Fact]
    public void A_requirement_nothing_measures_is_refused_at_load()
    {
        var declaration = AssertionDeclaration.Of("the tray menu opens", "the notification area", "a second moon");
        var machine = PreconditionSet.Of(Precondition.Met("a free notification area"));

        var refusal = Assert.Throws<ScenarioRefusedException>(() => machine.FirstAbsent(declaration));

        Assert.Equal("the tray menu opens", refusal.Subject);
        Assert.Contains("could never run on any machine", refusal.Because);
    }

    [Fact]
    public void A_precondition_the_assertion_never_declared_does_not_explain_its_hole()
    {
        var declaration = AssertionDeclaration.Of("the tray menu opens", "the notification area", "a free notification area");

        Assert.Throws<ArgumentException>(() => declaration.Unchecked(RegisteredProfile));
    }

    [Fact]
    public void One_precondition_measured_twice_is_refused()
    {
        Assert.Throws<ArgumentException>(
            () => PreconditionSet.Of(FreeNotificationArea, Precondition.Met("a free notification area")));
    }

    [Fact]
    public void A_precondition_says_whether_this_machine_measured_it_at_all()
    {
        var machine = PreconditionSet.Of(FreeNotificationArea);

        Assert.True(machine.Measured("a free notification area"));
        Assert.False(machine.Measured("a registered profile"));
    }
}

using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW57. A case is a name and its steps, and nothing else — no loop, no deadline, no retry cap, no
/// verdict. The two refusals here are both about a case that cannot fail, which is this project's
/// unearned green arriving as a file rather than as a verdict.
/// </summary>
public class CaseDeclarationTests
{
    private static StepDeclaration Checking() =>
        StepDeclaration.Of("""Edit[name="Profile"]""", "set value", "beta", expected: "beta");

    private static StepDeclaration Acting() => StepDeclaration.Of("TreeItem", "expand");

    [Fact]
    public void A_case_is_its_name_and_its_steps_in_order()
    {
        var declared = CaseDeclaration.Of("the profile survives a rename", Acting(), Checking());

        Assert.Equal("the profile survives a rename", declared.Name);
        Assert.Equal(["expand TreeItem", """set value Edit[name="Profile"]"""], declared.Steps.Select(step => step.Name));
        Assert.Equal(1, declared.Checks);
    }

    [Fact]
    public void An_unnamed_case_is_refused()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => CaseDeclaration.Of(" ", Checking()));

        Assert.Equal("<unnamed case>", refusal.Subject);
        Assert.Contains("reported under a name", refusal.Because);
    }

    [Fact]
    public void A_case_with_no_steps_drives_nothing_and_is_refused()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => CaseDeclaration.Of("the window opens"));

        Assert.Contains("no steps", refusal.Because);
        Assert.Contains("read green forever", refusal.Because);
    }

    [Fact]
    public void A_case_whose_steps_all_expect_nothing_is_refused_before_it_can_read_green()
    {
        // This is the one that matters. Every step lands, nothing is ever looked at, and the run
        // passes on a build with the defect still in it.
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => CaseDeclaration.Of("the tree opens", Acting(), Acting()));

        Assert.Contains("none of its 2 steps expects anything", refusal.Because);
        Assert.Contains("can only ever read green", refusal.Because);
    }

    [Fact]
    public void One_checked_step_among_many_acts_is_enough()
    {
        var declared = CaseDeclaration.Of("the tree opens and the field takes a name", Acting(), Acting(), Checking());

        Assert.Equal(3, declared.Steps.Count);
        Assert.Equal(1, declared.Checks);
        Assert.Contains("3 steps, 1 checked", declared.ToString());
    }

    [Fact]
    public void A_step_that_is_nothing_at_all_is_refused_rather_than_skipped()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => CaseDeclaration.Of("the window opens", Checking(), null!));

        Assert.Contains("nothing at all", refusal.Because);
    }
}

using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW2's load-time half: a check that covers nothing cannot ship. What is refused here is refused
/// on every machine, which is exactly what tells it apart from a hole in this one.
/// </summary>
public class AssertionDeclarationTests
{
    [Fact]
    public void An_assertion_that_names_nothing_to_observe_is_refused_at_load()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => AssertionDeclaration.Of("Escape closes Settings", ""));

        Assert.Equal("Escape closes Settings", refusal.Subject);
        Assert.Contains("read green forever", refusal.Because);
    }

    [Fact]
    public void A_refusal_names_the_assertion_it_is_about()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => AssertionDeclaration.Of("Escape closes Settings", "   "));

        Assert.StartsWith("Escape closes Settings:", refusal.Message);
    }

    [Fact]
    public void An_unnamed_assertion_is_refused_at_load_too()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => AssertionDeclaration.Of(" ", "the Settings window"));

        Assert.Equal("<unnamed assertion>", refusal.Subject);
    }

    [Fact]
    public void A_declaration_needs_no_preconditions_at_all()
    {
        var declaration = AssertionDeclaration.Of("the window is titled Claude", "the main window");

        Assert.Empty(declaration.Requires);
        Assert.Equal("the main window", declaration.Subject);
    }

    [Fact]
    public void The_same_precondition_declared_twice_is_refused()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => AssertionDeclaration.Of(
            "the tray menu opens", "the notification area", "a free notification area", "a free notification area"));

        Assert.Contains("twice", refusal.Because);
    }

    [Fact]
    public void Preconditions_keep_the_order_they_were_declared_in()
    {
        var declaration = AssertionDeclaration.Of(
            "the report renders", "the report file", "a registered profile", "a free notification area");

        Assert.Equal(["a registered profile", "a free notification area"], declaration.Requires);
    }
}

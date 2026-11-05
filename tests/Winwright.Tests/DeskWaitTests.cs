using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW470. How long an act waits for the desk is a number somebody chose, and this is the type that
/// makes them choose it. No desk is touched here: the whole of what is asserted is that a wait
/// nobody asked for cannot be constructed, and that the single look is reached by its name.
/// </summary>
public sealed class DeskWaitTests
{
    [Fact]
    public void The_single_look_is_a_name_and_not_a_zero()
    {
        Assert.False(DeskWait.Once.Waits);
        Assert.Equal(0, DeskWait.Once.DeadlineMs);
    }

    [Fact]
    public void A_declared_budget_waits()
    {
        var wait = DeskWait.Of(5000, 25);

        Assert.True(wait.Waits);
        Assert.Equal(5000, wait.DeadlineMs);
        Assert.Equal(25, wait.PollMs);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(-1, 25)]
    [InlineData(5000, 0)]
    [InlineData(5000, -1)]
    public void A_wait_of_nothing_is_refused_rather_than_taken_as_the_single_look(int deadlineMs, int pollMs)
    {
        // The same rule `Attempt` states about its own deadline, and for the same reason: a caller
        // that meant one look says <c>Once</c>, and one that passed a zero by accident is told so
        // rather than quietly given the behaviour it was trying to avoid.
        Assert.Throws<ArgumentOutOfRangeException>(() => DeskWait.Of(deadlineMs, pollMs));
    }
}

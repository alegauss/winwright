using System.Diagnostics;

using Winwright.Locating;
using Winwright.Projects;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW17. A helper that quietly retries for two hundred milliseconds folds that sleep into every
/// miss, so a loop doing its own waiting measures its own helper. Asking whether something arrived
/// needs a deadline; asking whether it is gone needs a single look.
/// </summary>
public class AttemptTests
{
    private static Func<string?> Never() => () => null;

    private static Func<string?> After(int looks)
    {
        var taken = 0;
        return () => ++taken >= looks ? "the Save button" : null;
    }

    [Fact]
    public void A_single_look_that_finds_nothing_costs_no_wait_at_all()
    {
        var clock = Stopwatch.StartNew();
        var sighting = Attempt.Once(Never());
        clock.Stop();

        Assert.False(sighting.Found);
        Assert.Equal(1, sighting.Polls);
        Assert.True(clock.ElapsedMilliseconds < 50, $"a single look slept for {clock.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void A_single_look_takes_exactly_one_look()
    {
        var looks = 0;
        Attempt.Once<string>(() =>
        {
            looks++;
            return null;
        });

        Assert.Equal(1, looks);
    }

    [Fact]
    public void Something_already_there_costs_nothing_to_wait_for()
    {
        var sighting = Attempt.Until(() => "the Save button", 5000);

        Assert.True(sighting.Found);
        Assert.Equal(1, sighting.Polls);
        Assert.True(sighting.WaitedMs < 50, $"an element already there cost {sighting.WaitedMs} ms");
    }

    [Fact]
    public void Waiting_polls_until_it_arrives_and_says_how_many_looks_it_took()
    {
        var sighting = Attempt.Until(After(3), 5000, pollMs: 10);

        Assert.True(sighting.Found);
        Assert.Equal("the Save button", sighting.Value);
        Assert.Equal(3, sighting.Polls);
    }

    [Fact]
    public void Waiting_stops_at_the_deadline_rather_than_at_a_number_it_chose()
    {
        var clock = Stopwatch.StartNew();
        var sighting = Attempt.Until(Never(), 200, pollMs: 10);
        clock.Stop();

        Assert.False(sighting.Found);
        Assert.True(sighting.WaitedMs >= 200, $"it gave up after {sighting.WaitedMs} ms of a 200 ms deadline");
        Assert.True(clock.ElapsedMilliseconds < 1000, $"it overran the deadline by far: {clock.ElapsedMilliseconds} ms");
        Assert.True(sighting.Polls > 1);
    }

    [Fact]
    public void The_two_forms_are_reached_by_name_and_never_by_an_argument_left_off()
    {
        var refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Attempt.Until(Never(), 0));

        Assert.Contains("a deadline of nothing is a single look, which is Attempt.Once", refusal.Message);
        Assert.Throws<ArgumentOutOfRangeException>(() => Attempt.Until(Never(), -1));
    }

    [Fact]
    public void There_is_no_waiting_form_with_a_deadline_it_chose_for_you()
    {
        var waiting = typeof(Attempt).GetMethods()
            .Where(method => method.Name == "Until")
            .Select(method => method.GetParameters()[1]);

        Assert.All(waiting, parameter => Assert.False(parameter.IsOptional));
    }

    [Fact]
    public void The_deadline_and_the_poll_come_from_what_the_project_declared()
    {
        var timeouts = Timeouts.Declared(new Dictionary<string, int> { ["resolve"] = 150, ["poll"] = 10 }, "test");

        var sighting = Attempt.Until(Never(), timeouts);

        Assert.False(sighting.Found);
        Assert.True(sighting.WaitedMs >= 150);
    }

    [Fact]
    public void A_named_timeout_the_project_never_declared_is_refused_rather_than_invented()
    {
        var timeouts = Timeouts.Declared(null, "test");

        Assert.Throws<DeclarationMissingException>(() => Attempt.Until(Never(), timeouts, "menu"));
    }

    [Fact]
    public void The_engine_seeds_a_poll_interval_so_a_bare_declaration_still_waits()
    {
        Assert.Equal(25, Timeouts.Declared(null, "test").For("poll"));
    }

    [Fact]
    public void A_sighting_that_found_nothing_refuses_with_what_it_cost()
    {
        var sighting = Attempt.Once(Never());

        var refusal = Assert.Throws<InvalidOperationException>(() => sighting.Require("#save"));

        Assert.Contains("#save was not there after", refusal.Message);
        Assert.Contains("1 look(s)", refusal.Message);
    }

    [Fact]
    public void A_sighting_that_found_something_hands_it_over()
    {
        Assert.Equal("the Save button", Attempt.Once(() => "the Save button").Require("#save"));
    }
}

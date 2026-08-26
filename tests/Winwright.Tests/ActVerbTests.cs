using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW57's vocabulary half. A script picks a method, so the compiler enforces the arity and the
/// author's memory enforces the spelling. A case is a data file, so both have to be fields — and
/// the field nobody would think to write is whether the act survives being repeated.
/// </summary>
public class ActVerbTests
{
    [Fact]
    public void Every_act_the_engine_offers_is_nameable_and_lists_itself()
    {
        Assert.Equal(
            ["invoke", "toggle", "set value", "set range", "select", "expand", "collapse"],
            ActVerb.All.Select(verb => verb.Name));

        Assert.All(ActVerb.All, verb => Assert.Same(verb, ActVerb.Named(verb.Name)));
    }

    [Fact]
    public void A_verb_that_does_not_exist_is_refused_with_the_ones_that_do()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ActVerb.Named("click"));

        Assert.Equal("click", refusal.Subject);
        Assert.Contains("invoke", refusal.Because);
        Assert.Contains("set range", refusal.Because);
    }

    [Fact]
    public void A_step_naming_no_verb_at_all_is_refused_the_same_way()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ActVerb.Named("  "));

        Assert.Equal("<unnamed act>", refusal.Subject);
        Assert.Contains("names no verb", refusal.Because);
    }

    [Fact]
    public void An_argument_beside_a_verb_that_takes_none_is_a_field_the_verb_cannot_use()
    {
        Assert.Null(ActVerb.Named("invoke").Refuses(null));
        Assert.Contains("takes nothing", ActVerb.Named("invoke").Refuses("Beta"));
    }

    [Fact]
    public void A_verb_that_needs_something_said_says_so_when_nothing_is()
    {
        Assert.Contains("acts on text", ActVerb.Named("set value").Refuses(null));
        Assert.Contains("acts on a number", ActVerb.Named("set range").Refuses(" "));
    }

    [Fact]
    public void A_number_a_range_cannot_read_is_refused_before_the_control_is_asked()
    {
        Assert.Null(ActVerb.Named("set range").Refuses("42.5"));
        Assert.Contains("'wide' is not one", ActVerb.Named("set range").Refuses("wide"));
    }

    [Fact]
    public void A_range_is_read_the_same_way_on_every_desk()
    {
        // The comma decimal separator is a real one under pt-BR, and a case that means 42.5 has to
        // mean it on a machine that would otherwise read it as 425.
        Assert.Null(ActVerb.Named("set range").Refuses("42.5"));
        Assert.Contains("not one", ActVerb.Named("set range").Refuses("42,5"));
    }

    [Fact]
    public void The_acts_that_do_not_survive_being_repeated_say_so()
    {
        // Toggling twice arrives back where it started, and pressing twice presses twice. The
        // engine reads this rather than asking the author to remember it per case.
        Assert.False(ActVerb.Named("toggle").Repeatable);
        Assert.False(ActVerb.Named("invoke").Repeatable);

        Assert.True(ActVerb.Named("set value").Repeatable);
        Assert.True(ActVerb.Named("set range").Repeatable);
        Assert.True(ActVerb.Named("expand").Repeatable);
        Assert.True(ActVerb.Named("collapse").Repeatable);
        Assert.True(ActVerb.Named("select").Repeatable);
    }

    [Fact]
    public void A_verb_is_named_whatever_the_trace_will_call_it()
    {
        // The names here are the strings Act stamps onto its own results, so a trace read back
        // names the verb the case declared rather than a second spelling of it.
        Assert.Equal("set value", ActVerb.Named("SET VALUE").Name);
        Assert.Equal("expand", ActVerb.Named("Expand").Name);
    }
}

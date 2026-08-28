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
        // WW225 and WW226 added the four that synthesise input, at the end: the order is the order a
        // reader is shown them, and the pattern acts come first because a pattern act is the default.
        // WW254 added 'pick' after those, which is where it belongs on both counts — it can need the
        // desk, and it is the newest. WW259 added 'open submenu' at the end on the same two counts,
        // after 'pick at' rather than beside 'expand': the pair it belongs to is a pattern act and a
        // keyboard act, and this list is ordered by which door a verb takes, not by what it pairs with.
        Assert.Equal(
            [
                "read", "invoke", "toggle", "set value", "set range", "select", "expand", "collapse",
                "type", "click", "nudge", "press", "pick", "pick at", "open submenu",
            ],
            ActVerb.All.Select(verb => verb.Name));

        Assert.All(ActVerb.All, verb => Assert.Same(verb, ActVerb.Named(verb.Name)));
    }

    [Fact]
    public void Exactly_one_of_them_reads_and_never_acts()
    {
        // WW213. The vocabulary was seven acts, so a case checking a label had to name an act to
        // get there — and selecting a Text element to read it says the case moved something.
        Assert.True(ActVerb.Named("read").Reads);
        Assert.Equal(["read"], ActVerb.All.Where(verb => verb.Reads).Select(verb => verb.Name));
        Assert.Equal(Takes.Nothing, ActVerb.Named("read").Wants);
    }

    [Fact]
    public void A_verb_that_does_not_exist_is_refused_with_the_ones_that_do()
    {
        // 'smash' and not 'click': WW225 made click a verb, and this case is about a name outside the
        // vocabulary being refused with the vocabulary, not about which word is outside it today.
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ActVerb.Named("smash"));

        Assert.Equal("smash", refusal.Subject);
        Assert.Contains("invoke", refusal.Because);
        Assert.Contains("set range", refusal.Because);
        Assert.Contains("click", refusal.Because);
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
        Assert.True(ActVerb.Named("read").Repeatable);
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

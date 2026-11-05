using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW450. Whether this desk draws the shadow a menu asks for, and the hole it makes where it does
/// not.
/// <para>
/// Both answers are driven through the half that turns a reading into a condition, because the other
/// way to provoke "off" is to switch a system setting off in the middle of a suite — which is a run
/// changing the desk it exists to report on, and a setting a crashed case would leave behind. The
/// mechanism itself was measured on the guest before any of this was written: the fixture's shadowed
/// menu had a <c>SysShadow</c> behind it with the setting on, none with it off, and one with it on
/// again.
/// </para>
/// </summary>
public sealed class DropShadowsTests
{
    [Fact]
    public void A_desk_that_draws_shadows_meets_the_condition_and_one_that_does_not_names_the_setting()
    {
        var drawn = DropShadows.Of(drawn: true);
        var off = DropShadows.Of(drawn: false);

        Assert.True(drawn.Satisfied);
        Assert.Equal(DropShadows.PreconditionName, drawn.Name);

        Assert.False(off.Satisfied);
        Assert.Equal(DropShadows.PreconditionName, off.Name);

        // The sentence is the whole content of the hole, and what it has to say is whose it is: a desk
        // with shadows off read as an application that drew nothing is the one thing it is not.
        Assert.Contains("switched off", off.Absence, StringComparison.Ordinal);
        Assert.Contains("not a window the application failed to draw", off.Absence, StringComparison.Ordinal);
    }

    [Fact]
    public void Its_absence_is_a_fact_the_engine_calls_the_desks()
    {
        // The reason this is an engine reading at all. `BusyDesk` refuses a hole whose condition the
        // engine does not declare as the desk's — WW183 moved that list into the engine so this suite
        // could not grow one of its own — so the case waiting for a shadow could not say "shadows are
        // off" until the engine could.
        Assert.True(DeskFacts.Names(DropShadows.PreconditionName));
    }

    [Fact]
    public void The_reading_asks_the_machine_and_answers_under_its_own_name()
    {
        // Taken for real, and either answer is right: this asks Windows for a setting, which a desk may
        // have on or off. What cannot vary is that it is this reading answering, and that an absence it
        // gives says why.
        var reading = DropShadows.Reading();

        Assert.Equal(DropShadows.PreconditionName, reading.Name);
        Assert.True(reading.Satisfied || reading.Absence.Length > 0, "an absent reading said nothing about why");
    }
}

using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Scenarios;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW265. <c>Pick</c> shipped in block D and <c>PickTests</c> drove every route of it against a Win32
/// <c>ComboBox</c> built with <c>CB_ADDSTRING</c>, which holds its items in the tree whether or not it
/// is dropped down. A WPF <c>ComboBox</c> realises its items when its popup opens and has none before
/// that — so the walk read <em>it holds nothing</em> about a picker holding five, and refused.
/// <para>
/// Measured on claude-tray's profile picker, which is the one <c>WW254</c> shipped to unblock: nought
/// items shut, two once expanded, nought again once collapsed. The suite could not have found it,
/// because until <c>--pickers</c> no WPF picker existed anywhere in it — which is the half of this
/// task that is a fixture and not a fix.
/// </para>
/// <para>
/// The launched fixture and no dialog beside it, which is WW248's rule: an in-process window shown by
/// this thread takes the desk, so a class holding one cannot also drive a launched window.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ShutPickerTests : IDisposable
{
    /// <summary>What the pane holds, which is also what a refusal has to be able to list.</summary>
    private static readonly string[] Values = ["Alpha", "Bravo", "Charlie", "Delta", "Echo"];

    private readonly Settling settling = Attachable.Settling();
    private readonly AutomationElement fixtureRoot;

    public ShutPickerTests()
    {
        var launched = settling.Register.Launch(Fixture.Started("--pickers"));
        var drawn = Attempt.UntilTrue(() => TopLevelWindows.Largest(launched.Pid) is not null, 20000, 25);

        Assert.True(drawn.Happened, $"the fixture drew no window in {drawn.WaitedMs}ms");
        fixtureRoot = AutomationElement.FromHandle(TopLevelWindows.Largest(launched.Pid)!.Handle);
    }

    public void Dispose() => settling.Dispose();

    private Subject Picker(string named) => Subject.Unguarded(
        fixtureRoot, Locator.Parse($"ComboBox#{named}"), deadlineMs: 4000, pollMs: 25);

    [Fact]
    public void The_pane_draws_what_it_says_it_draws()
    {
        Assert.True(Picker("shutPicker").Read().Found, "the pickers pane draws no ComboBox#shutPicker");
        Assert.True(Picker("openPicker").Read().Found, "the pickers pane draws no ComboBox#openPicker");
        Assert.Contains("--pickers", Fixture.Catalogue(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_shut_picker_answers_what_it_holds()
    {
        // The whole finding in one line. Before WW265 this was an empty list, and every claim built
        // on it was a refusal naming a picker that had five values in it all along.
        Assert.Equal(Values, Pick.Values(Picker("shutPicker")));
    }

    [Fact]
    public void A_shut_picker_is_walked_and_left_shut()
    {
        var picked = Pick.Value(Picker("shutPicker"), "Charlie");

        Assert.True(picked.Landed, picked.ToString());
        Assert.Equal("Charlie", picked.Selected);

        // The route, asserted rather than assumed. The selection pattern needs no foreground, so a
        // walk that fell through to the keyboard here is a red naming the route — never a reading
        // this case would otherwise have blamed on the control.
        Assert.Equal(PickRoute.Pattern, picked.Route);

        // Put back, and this is the half a fix could easily skip. A picker left dropped down holds
        // the desk in a way that outlives the step, and the next one finds a foreground it cannot
        // name — the failure Surface.AsFound exists for one floor up.
        Assert.Equal("Collapsed", State("shutPicker"));
    }

    [Fact]
    public void A_picker_already_open_is_left_open()
    {
        // The other half of the same rule. Only a walk that opened a picker owes it a close, or a
        // case driving a window somebody deliberately left dropped down would find it shut
        // afterwards and be reading a state it never asked for.
        Assert.Equal("Expanded", State("openPicker"));

        var picked = Pick.Value(Picker("openPicker"), "Delta");

        Assert.True(picked.Landed, picked.ToString());
        Assert.Equal(PickRoute.Pattern, picked.Route);
        Assert.Equal("Expanded", State("openPicker"));
    }

    [Fact]
    public void A_value_a_shut_picker_does_not_hold_is_refused_with_the_ones_it_does()
    {
        // The refusal has to list five values and not say "nothing", which is the sentence that sent
        // a reader looking for a picker that was there the whole time.
        var refusal = Assert.Throws<NotActionableException>(() => Pick.Value(Picker("shutPicker"), "Foxtrot"));

        Assert.Contains("holds no \"Foxtrot\"", refusal.Because, StringComparison.Ordinal);
        Assert.Contains("\"Alpha\"", refusal.Because, StringComparison.Ordinal);
        Assert.Equal("Collapsed", State("shutPicker"));
    }

    [Fact]
    public void What_the_picker_has_chosen_is_a_reading_a_case_can_name()
    {
        // WW266. Measured on claude-tray's profile picker and true of this one: a ComboBox carrying
        // Selection, ExpandCollapse and ItemContainer and no ValuePattern at all. So 'value' answers
        // nothing, 'name' answers the picker's own label, and without 'picked' a round trip comparing
        // either would have held on every machine whatever the picker did.
        var reading = Picker("shutPicker").Read();

        Assert.True(reading.Found);
        Assert.Null(reading.Values.Value);
        Assert.Equal("Alpha", ReadBack.Named("picked").Of(reading));

        // And it is what 'anything' answers too, rather than the shape the picker happens to be in.
        Assert.Equal("Alpha", reading.Values.Reading());
    }

    [Fact]
    public void A_position_is_reached_without_the_case_naming_what_is_there()
    {
        // WW267. The whole point: nothing here writes a value, because on the picker this is for the
        // values are the machine's own data. What it reached is still reported by name, since the
        // engine can read what the case could not have written.
        var picked = Pick.At(Picker("shutPicker"), 3);

        Assert.True(picked.Landed, picked.ToString());
        Assert.Equal("Delta", picked.Selected);
        Assert.Equal(PickRoute.Pattern, picked.Route);
        Assert.Equal("Collapsed", State("shutPicker"));
    }

    [Fact]
    public void A_position_the_picker_does_not_have_is_refused_with_how_many_it_holds()
    {
        var refusal = Assert.Throws<NotActionableException>(() => Pick.At(Picker("shutPicker"), 5));

        Assert.Contains("has no position 5", refusal.Because, StringComparison.Ordinal);
        Assert.Contains("5 value(s)", refusal.Because, StringComparison.Ordinal);
    }

    /// <summary>
    /// Whether the picker is dropped down, read through the engine's own reading rather than through
    /// the walk — so a walk that reported one thing and left another is a red here.
    /// </summary>
    private string State(string named)
    {
        var reading = Picker(named).Read();
        Assert.True(reading.Found, $"ComboBox#{named} is not in the tree");

        return reading.Values.ExpandCollapse ?? "<none>";
    }
}

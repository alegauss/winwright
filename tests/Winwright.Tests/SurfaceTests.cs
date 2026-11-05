using System.Runtime.InteropServices;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Tracing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW33. A popup is a toggle and a tab is a position, and the next case sharing that window asked
/// for neither.
/// <para>
/// The failure this prevents is the one this suite kept producing while Block D was being written:
/// order-dependent, visible only when everything runs, and gone when the case is run alone. A
/// combo left open by one class made another class's keys land somewhere it could not name.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class SurfaceTests : IDisposable
{
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint WsTabStop = 0x00010000;
    private const uint BsAutoCheckBox = 0x0003;

    /// <summary>
    /// WW147. A control with a state the case never asked for: off, on, and then a third the
    /// press cycle passes through on its way back. It is what makes a restore take more than one
    /// press without anything being wrong, which is exactly the shape the bounded retry exists for.
    /// </summary>
    private const uint BsAuto3State = 0x0006;
    private const uint CbsDropDownList = 0x0003;
    private const uint CbAddString = 0x0143;

    private readonly PumpedDialog dialog;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SendMessageW(nint window, uint message, nint wParam, string lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint FindWindowExW(nint parent, nint after, string? className, string? title);

    public SurfaceTests()
    {
        dialog = PumpedDialog.Open(
            "winwright statistics",
            new PumpedDialog.ChildWindow("Button", "Wrap lines", WsChild | WsVisible | BsAutoCheckBox, 20, 20, 160, 30),
            new PumpedDialog.ChildWindow(
                "Button", "Word wrap", WsChild | WsVisible | BsAuto3State, 200, 20, 160, 30),
            new PumpedDialog.ChildWindow("Edit", "alpha", WsChild | WsVisible | WsTabStop, 20, 60, 220, 24),
            new PumpedDialog.ChildWindow(
                "ComboBox", null, WsChild | WsVisible | WsTabStop | CbsDropDownList, 20, 100, 220, 240),
            new PumpedDialog.ChildWindow(
                "msctls_trackbar32", null, WsChild | WsVisible | WsTabStop, 20, 200, 200, 32));

        var combo = FindWindowExW(dialog.Frame, 0, "ComboBox", null);
        Assert.NotEqual(0, combo);
        foreach (var value in new[] { "Overview", "Statistics" })
            SendMessageW(combo, CbAddString, 0, value);
    }

    public void Dispose() => dialog.Dispose();

    private Subject On(string locator) =>
        Subject.Unguarded(dialog.Root, Locator.Parse(locator), deadlineMs: 2000, pollMs: 20);

    private Subject Combo => On("ComboBox");

    private Subject Checkbox => On("""CheckBox[name="Wrap lines"]""");

    /// <summary>The one that does not come back on the first press. See <see cref="BsAuto3State" />.</summary>
    private Subject Tristate => On("""CheckBox[name="Word wrap"]""");

    private Subject Slider => On("Slider");

    private Subject Edit => On("Edit[order=top]");

    [Fact]
    public void A_toggled_surface_is_put_back_where_the_case_found_it()
    {
        Assert.Equal("Off", Checkbox.ReadOnce().Values.Toggle);

        using (var found = Surface.AsFound(Checkbox))
        {
            Act.Toggle(Checkbox);
            Assert.Equal("On", Checkbox.ReadOnce().Values.Toggle);

            found.PutBack();
            Assert.True(found.HandedBackClean);
        }

        Assert.Equal("Off", Checkbox.ReadOnce().Values.Toggle);
    }

    [Fact]
    public void A_popup_left_open_is_shut_again()
    {
        using var found = Surface.AsFound(Combo);
        Act.Expand(Combo);
        Assert.Equal("Expanded", Combo.ReadOnce().Values.ExpandCollapse);

        found.PutBack();

        Assert.Equal("Collapsed", Combo.ReadOnce().Values.ExpandCollapse);
        Assert.True(found.HandedBackClean);
    }

    [Fact]
    public void A_position_is_a_number_and_goes_back_to_it()
    {
        using var found = Surface.AsFound(Slider);
        Act.SetRange(Slider, 42);

        found.PutBack();

        Assert.Equal(0d, Slider.ReadOnce().Values.Range);
    }

    [Fact]
    public void What_a_control_says_goes_back_too()
    {
        using var found = Surface.AsFound(Edit);
        Act.SetValue(Edit, "beta");

        found.PutBack();

        Assert.Equal("alpha", Edit.ReadOnce().Values.Value);
    }

    [Fact]
    public void Nothing_that_did_not_move_is_touched()
    {
        using var found = Surface.AsFound(Checkbox, Combo, Slider);

        var restorations = found.PutBack();

        Assert.All(restorations, one => Assert.False(one.Moved));
        Assert.All(restorations, one => Assert.Contains("was left as it was found", one.ToString()));
        Assert.Equal("nothing on this window was moved.", found.Sentence());
    }

    [Fact]
    public void Several_surfaces_go_back_together_and_the_answer_names_them()
    {
        using var found = Surface.AsFound(Checkbox, Combo, Edit);
        Act.Toggle(Checkbox);
        Act.Expand(Combo);
        Act.SetValue(Edit, "beta");

        found.PutBack();

        Assert.True(found.HandedBackClean);
        Assert.Equal("Off", Checkbox.ReadOnce().Values.Toggle);
        Assert.Equal("Collapsed", Combo.ReadOnce().Values.ExpandCollapse);
        Assert.Equal("alpha", Edit.ReadOnce().Values.Value);
        // The count is not asserted, and that is a finding rather than a shrug: a combo shuts
        // itself when it loses the focus, so by the time the restore looked, one of the three had
        // already gone back on its own. Whether a surface moved is a fact to read at the moment of
        // handing back, never a tally kept from when it was disturbed.
        Assert.Contains("were put back", found.Sentence());
    }

    [Fact]
    public void Scoping_it_is_the_whole_of_the_discipline()
    {
        // Nothing in the body puts anything back; leaving the scope does.
        using (Surface.AsFound(Checkbox, Combo))
        {
            Act.Toggle(Checkbox);
            Act.Expand(Combo);
        }

        Assert.Equal("Off", Checkbox.ReadOnce().Values.Toggle);
        Assert.Equal("Collapsed", Combo.ReadOnce().Values.ExpandCollapse);
    }

    [Fact]
    public void Putting_back_twice_answers_what_the_first_one_did()
    {
        using var found = Surface.AsFound(Checkbox);
        Act.Toggle(Checkbox);

        var first = found.PutBack();

        Assert.Same(first, found.PutBack());
        Assert.Same(first, found.Restorations);
    }

    [Fact]
    public void A_surface_that_went_away_is_reported_rather_than_thrown()
    {
        var absent = On("""Edit[name="never here"]""");
        using var found = Surface.AsFound(absent);

        var one = Assert.Single(found.PutBack());

        Assert.False(one.PutBack);
        Assert.Equal("it is no longer in the tree", one.Because);
        Assert.False(found.HandedBackClean);
    }

    [Fact]
    public void A_restoration_that_did_not_take_is_said_and_never_assumed()
    {
        // Nothing here fakes a stuck control; what is asserted is that the answer's shape can
        // carry the failure at all, which is what stops a restore from being taken on trust.
        var stuck = new Restoration("ComboBox", "Collapsed", "Expanded", true, false, "it did not go back");

        Assert.Contains("is on \"Expanded\" and was found on \"Collapsed\"", stuck.ToString());
    }

    [Fact]
    public void A_toggle_that_comes_round_the_long_way_says_how_many_presses_it_took()
    {
        // WW147. Three states and a case that asked for two: putting it back means pressing past
        // the one nobody asked for. Nothing here is wrong, which is the point — the count is a
        // finding about the control, and it used to exist for the length of one expression.
        Assert.Equal("Off", Tristate.ReadOnce().Values.Toggle);

        using var found = Surface.AsFound(Tristate);
        Act.Toggle(Tristate);
        Assert.Equal("On", Tristate.ReadOnce().Values.Toggle);

        var one = Assert.Single(found.PutBack());

        Assert.True(one.PutBack, one.ToString());
        Assert.Equal("Off", Tristate.ReadOnce().Values.Toggle);

        // Read off the answer and not typed: what is asserted is that the count survived, and how
        // many presses this particular control needs is the control's business.
        Assert.True(one.TookMoreThanOnePress, one.ToString());
        Assert.Equal(Surface.MostToggles, one.Pressing!.Cap);
        Assert.Contains($"worked on attempt {one.Pressing.Attempts} of {Surface.MostToggles}", one.ToString());

        // And it reaches the sentence a report prints, which is the half a caller cannot forget.
        Assert.Contains("worked on attempt", found.Sentence());
    }

    [Fact]
    public void A_first_press_that_worked_says_nothing_about_the_count()
    {
        // The other half of the rule: every restore would carry a count of one, and a number on
        // every line is a number nobody reads on the one line that matters.
        using var found = Surface.AsFound(Checkbox);
        Act.Toggle(Checkbox);

        var one = Assert.Single(found.PutBack());

        Assert.False(one.TookMoreThanOnePress, one.ToString());
        Assert.DoesNotContain("attempt", one.ToString());
    }

    [Fact]
    public void The_press_count_reaches_a_step_a_trace_records()
    {
        // The criterion this task was read against: a retry is bounded and said out loud, and the
        // attempt count reaches the trace. It reaches it through the same stamp every other traced
        // act would use, so a restore that took three goes reads like anything else that did.
        using var found = Surface.AsFound(Tristate);
        Act.Toggle(Tristate);
        found.PutBack();

        var step = Assert.Single(found.Steps());

        Assert.Equal("restore", step.Verb);
        Assert.Equal(StepVerdict.Ok, step.Verdict);
        Assert.Equal("Off", step.ReadBack);
        Assert.True(step.Attempts > 1, step.Detail);
        Assert.Contains($"\"attempts\":{step.Attempts}", TraceFormat.Line(step));
    }

    [Fact]
    public void A_surface_nothing_touched_is_not_a_step_at_all()
    {
        // A step is a record of something attempted. Nothing was, so a line saying so would be a
        // record of an act that never happened — which is the shape of every green this project
        // exists to refuse.
        using var found = Surface.AsFound(Checkbox, Combo, Slider);

        found.PutBack();

        Assert.All(found.Restorations, one => Assert.False(one.Moved));
        Assert.Empty(found.Steps());
    }

    [Fact]
    public void A_restore_that_did_not_take_reaches_the_trace_as_a_red_with_its_reason()
    {
        var absent = On("""Edit[name="never here"]""");
        using var found = Surface.AsFound(absent);
        found.PutBack();

        var step = Assert.Single(found.Steps());

        Assert.Equal(StepVerdict.Failed, step.Verdict);
        Assert.Equal("it is no longer in the tree", step.Detail);

        // One, and never zero: the surface was reached for. A step that claims no attempt is the
        // reading reserved for something a precondition stopped before it began.
        Assert.Equal(1, step.Attempts);
    }
}

using System.Runtime.InteropServices;

using Winwright.Acting;
using Winwright.Locating;

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
        new(dialog.Root, Locator.Parse(locator), deadlineMs: 2000, pollMs: 20);

    private Subject Combo => On("ComboBox");

    private Subject Checkbox => On("""CheckBox[name="Wrap lines"]""");

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
}

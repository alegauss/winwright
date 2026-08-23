using System.Runtime.InteropServices;
using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW24. A synthesised mouse click lands on whatever is drawn at a point, so it needs the window
/// in the foreground — and Windows refuses the foreground to a process that does not already own
/// it. A pattern asks the control instead.
/// <para>
/// The claim these tests exist to prove is not that the acts work, it is that they work
/// <em>while the foreground belongs to another window</em>. So the fixture takes the foreground
/// away before every act, and each act is chosen for having a consequence the control reports.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ActTests : IDisposable
{
    private const uint WsPopup = 0x80000000;
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint BsAutoCheckBox = 0x0003;
    private const uint CbsDropDownList = 0x0003;
    private const uint CbAddString = 0x0143;

    private readonly List<nint> created = [];

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SendMessageW(nint window, uint message, nint wParam, string lParam);

    public void Dispose()
    {
        for (var index = created.Count - 1; index >= 0; index--)
            DestroyWindow(created[index]);
    }

    private nint Create(string className, string? title, uint style, int w, int h, nint parent = 0)
    {
        var window = CreateWindowExW(0, className, title, style, 20, 20, w, h, parent, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
        return window;
    }

    private nint Dialog()
    {
        var frame = Create("Static", "winwright statistics", WsPopup | WsVisible, 480, 320);
        Create("Button", "Wrap lines", WsChild | WsVisible | BsAutoCheckBox, 140, 24, frame);
        Create("Edit", "alpha", WsChild | WsVisible, 200, 24, frame);
        var combo = Create("ComboBox", null, WsChild | WsVisible | CbsDropDownList, 200, 200, frame);
        SendMessageW(combo, CbAddString, 0, "Alpha");
        SendMessageW(combo, CbAddString, 0, "Beta");
        return frame;
    }

    /// <summary>Take the foreground away from the dialog, which is the condition under test.</summary>
    private void GiveTheForegroundAway()
    {
        Create("Static", "winwright decoy", WsPopup | WsVisible, 200, 120);
    }

    private static Subject On(nint frame, string locator) =>
        Subject.Unguarded(AutomationElement.FromHandle(frame), Locator.Parse(locator), 2000, pollMs: 20);

    [Fact]
    public void A_toggle_lands_while_the_foreground_belongs_to_another_window()
    {
        var frame = Dialog();
        var checkbox = On(frame, """CheckBox[name="Wrap lines"]""");
        GiveTheForegroundAway();

        Assert.NotEqual(ForegroundState.Ours, Foreground.Check(frame).State);

        var acted = Act.Toggle(checkbox);

        Assert.True(acted.Changed);
        Assert.Equal("Off", acted.Before.Toggle);
        Assert.Equal("On", acted.After.Toggle);
    }

    [Fact]
    public void An_invoke_lands_while_the_foreground_belongs_to_another_window()
    {
        var frame = Dialog();
        var combo = On(frame, "ComboBox");
        var dropDown = On(frame, "ComboBox > Button#DropDown");
        GiveTheForegroundAway();

        Assert.Equal("Collapsed", combo.ReadOnce().Values.ExpandCollapse);

        Act.Invoke(dropDown);

        Assert.Equal("Expanded", combo.ReadOnce().Values.ExpandCollapse);
    }

    [Fact]
    public void A_value_is_set_through_the_control_and_read_back_from_it()
    {
        var frame = Dialog();
        var edit = On(frame, "Edit");
        GiveTheForegroundAway();

        var acted = Act.SetValue(edit, "beta");

        Assert.Equal("alpha", acted.Before.Value);
        Assert.Equal("beta", acted.After.Value);
        Assert.True(acted.Changed);
    }

    [Fact]
    public void Expanding_and_collapsing_move_the_state_the_control_reports()
    {
        var frame = Dialog();
        var combo = On(frame, "ComboBox");
        GiveTheForegroundAway();

        Assert.Equal("Expanded", Act.Expand(combo).After.ExpandCollapse);
        Assert.Equal("Collapsed", Act.Collapse(combo).After.ExpandCollapse);
    }

    [Fact]
    public void Selecting_an_item_lands_and_is_read_back_from_the_item()
    {
        var frame = Dialog();
        var combo = On(frame, "ComboBox");
        var beta = On(frame, """ListItem[name="Beta"]""");
        GiveTheForegroundAway();

        // The item of a shut combo is in the tree and offscreen, so actionability refuses it.
        // That is the check working: the route has to be walked, and walking it is two acts.
        Assert.Equal(
            Actionable.Offscreen,
            Assert.Throws<NotActionableException>(() => Act.Select(beta)).Missing);

        Act.Expand(combo);
        var acted = Act.Select(beta);

        Assert.False(acted.Before.IsSelected);
        Assert.True(acted.After.IsSelected);
    }

    [Fact]
    public void An_act_the_control_cannot_take_is_refused_before_anything_is_touched()
    {
        var frame = Dialog();
        Create("Static", "a label", WsChild | WsVisible, 120, 20, frame);

        var refusal = Assert.Throws<NotActionableException>(
            () => Act.Invoke(On(frame, """Text[name="a label"]""")));

        Assert.Equal(Actionable.PatternMissing, refusal.Missing);
        Assert.Contains("offers no Invoke pattern", refusal.Because);
    }

    [Fact]
    public void An_act_against_something_that_is_not_there_is_refused_as_not_in_the_tree()
    {
        var refusal = Assert.Throws<NotActionableException>(
            () => Act.Invoke(On(Dialog(), """Button[name="Publish"]""")));

        Assert.Equal(Actionable.NotInTree, refusal.Missing);
    }

    [Fact]
    public void An_act_that_moved_nothing_says_so_rather_than_claiming_it_landed()
    {
        var frame = Dialog();
        var edit = On(frame, "Edit");
        GiveTheForegroundAway();

        var acted = Act.SetValue(edit, "alpha");

        Assert.False(acted.Changed);
        Assert.Contains("nothing it reports moved", acted.ToString());
    }

    [Fact]
    public void The_line_a_report_shows_names_the_element_the_pattern_and_what_moved()
    {
        var frame = Dialog();
        GiveTheForegroundAway();

        var acted = Act.Toggle(On(frame, """CheckBox[name="Wrap lines"]"""));

        Assert.Equal("toggle CheckBox 'Wrap lines' via Toggle: Off -> On", acted.ToString());
    }

    [Fact]
    public void An_act_produces_the_trace_step_that_records_it()
    {
        var frame = Dialog();
        GiveTheForegroundAway();

        var step = Act.Toggle(On(frame, """CheckBox[name="Wrap lines"]""")).AsTraceStep();

        Assert.Equal("toggle", step.Verb);
        Assert.Equal("""CheckBox[name="Wrap lines"]""", step.Locator);
        Assert.Equal("Toggle", step.Pattern);
        Assert.Equal("On", step.ReadBack);
        Assert.Equal(Winwright.Tracing.StepVerdict.Ok, step.Verdict);
        Assert.Contains("\"readBack\":\"On\"", Winwright.Tracing.TraceFormat.Line(step));
    }

    [Fact]
    public void Every_act_names_the_pattern_it_goes_through()
    {
        var frame = Dialog();
        GiveTheForegroundAway();

        Assert.Equal("Toggle", Act.Toggle(On(frame, """CheckBox[name="Wrap lines"]""")).Pattern);
        Assert.Equal("Value", Act.SetValue(On(frame, "Edit"), "gamma").Pattern);
        Assert.Equal("ExpandCollapse", Act.Expand(On(frame, "ComboBox")).Pattern);
    }
}

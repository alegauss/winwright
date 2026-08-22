using System.Runtime.InteropServices;
using System.Windows.Automation;

using Winwright.Locating;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW20. A control on a page that is not showing cannot be found by any id, which reads exactly
/// like a control that was renamed or removed.
/// <para>
/// The fixture holds both real shapes, measured rather than assumed. A hidden container is
/// <em>absent</em> from the UI Automation tree along with everything in it — that is the page that
/// is not showing. A collapsed Win32 combo box is the other shape: its list items are in the tree
/// and every one of them is offscreen, which is a different task's answer and not a miss at all.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class LocatorMissTests : IDisposable
{
    private const uint WsPopup = 0x80000000;
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint CbsDropDownList = 0x0003;
    private const uint CbAddString = 0x0143;

    private readonly List<nint> created = [];

    /// <summary>
    /// Whether this case needs its window where a person could see it. Off by default: a suite
    /// that moves the foreground takes its own readings of the foreground on a desk it disturbed.
    /// On for the cases that read actionability, because a window outside every monitor is one UI
    /// Automation reports as offscreen - correctly, and that is what those cases would measure.
    /// </summary>
    private bool onScreen;

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

    private nint Create(string className, string? title, uint style, int width, int height, nint parent = 0)
    {
        var window = CreateWindowExW(0, className, title, style, parent == 0 && !onScreen ? OffScreen.Left : 20, parent == 0 && !onScreen ? OffScreen.Top : 20, width, height, parent, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
        return window;
    }

    /// <summary>A Save button, a collapsed combo box, and a hidden page holding a Publish button.</summary>
    private AutomationElement Dialog()
    {
        var frame = Create("Static", "winwright statistics", WsPopup | WsVisible, 420, 300);
        Create("Button", "Save", WsChild | WsVisible, 90, 28, frame);
        var combo = Create("ComboBox", null, WsChild | WsVisible | CbsDropDownList, 200, 200, frame);
        SendMessageW(combo, CbAddString, 0, "Alpha");
        SendMessageW(combo, CbAddString, 0, "Beta");

        var page = Create("Static", "page two", WsChild, 300, 100, frame);
        Create("Button", "Publish", WsChild | WsVisible, 90, 28, page);
        return AutomationElement.FromHandle(frame);
    }

    [Fact]
    public void Something_that_is_there_resolves()
    {
        var resolution = Resolve.Once(Dialog(), Locator.Parse("""Button[name="Save"]"""));

        Assert.True(resolution.Found);
        Assert.Equal("Save", resolution.Facts!.Name);
        Assert.Null(resolution.Miss);
    }

    [Fact]
    public void A_control_on_a_page_that_is_not_showing_is_absent_from_the_tree()
    {
        // Measured, not assumed: a hidden container takes its whole subtree out of the tree.
        var resolution = Resolve.Once(Dialog(), Locator.Parse("""Button[name="Publish"]"""));

        Assert.False(resolution.Found);
        Assert.Equal(MissKind.Absent, resolution.Miss!.Kind);
    }

    [Fact]
    public void An_absent_control_is_told_apart_from_a_renamed_one_by_what_is_shut()
    {
        var miss = Resolve.Once(Dialog(), Locator.Parse("""Button[name="Publish"]""")).Miss!;

        Assert.NotEmpty(miss.ClosedDoors);
        Assert.Contains(miss.ClosedDoors, door => door.How == "expanded");
        Assert.Contains("it is behind something that is not showing", miss.Sentence());
    }

    [Fact]
    public void A_window_with_nothing_shut_says_the_control_really_is_gone()
    {
        var bare = Create("Static", "winwright bare", WsPopup | WsVisible, 300, 200);
        Create("Button", "Save", WsChild | WsVisible, 90, 28, bare);

        var miss = Resolve.Once(AutomationElement.FromHandle(bare), Locator.Parse("""Button[name="Publish"]""")).Miss!;

        Assert.Empty(miss.ClosedDoors);
        Assert.Contains("nothing in it is shut", miss.Sentence());
        Assert.Contains("renamed, removed, or never there", miss.Sentence());
    }

    [Fact]
    public void A_control_under_the_wrong_step_says_the_chain_is_wrong_before_any_route()
    {
        // The combo box is collapsed, so a route could be offered here. It is not, and that is
        // the point: Save exists elsewhere, and no amount of expanding the combo will find it.
        var resolution = Resolve.Once(Dialog(), Locator.Parse("""ComboBox > Button[name="Save"]"""));

        var miss = resolution.Miss!;
        Assert.Equal(MissKind.ElsewhereInTheWindow, miss.Kind);
        Assert.Equal(1, miss.Elsewhere);
        Assert.Contains("the chain is wrong rather than the control missing", miss.Sentence());
    }

    [Fact]
    public void A_step_under_something_shut_that_is_nowhere_else_names_the_route()
    {
        var resolution = Resolve.Once(Dialog(), Locator.Parse("""ComboBox > Button[name="Publish"]"""));

        var miss = resolution.Miss!;
        Assert.Equal(MissKind.NavigationNeeded, miss.Kind);
        Assert.Contains("is expanded", miss.Route);
        Assert.Contains("it will not be until", miss.Sentence());
    }

    [Fact]
    public void The_miss_names_the_step_that_stopped_and_how_far_it_got()
    {
        var miss = Resolve.Once(Dialog(), Locator.Parse("""ComboBox > Button[name="Publish"]""")).Miss!;

        Assert.Equal("""Button[name="Publish"]""", miss.Stopped.ToString());
        Assert.Equal(1, miss.Reached);
        Assert.Equal("ComboBox", miss.Deepest!.ControlType);
    }

    [Fact]
    public void A_first_step_that_matches_nothing_stops_at_the_window()
    {
        var miss = Resolve.Once(Dialog(), Locator.Parse("""Slider[name="Volume"]""")).Miss!;

        Assert.Equal(0, miss.Reached);
        Assert.Null(miss.Deepest);
        Assert.Equal(MissKind.Absent, miss.Kind);
    }

    [Fact]
    public void A_collapsed_combo_keeps_its_items_in_the_tree_and_offscreen()
    {
        // The other real shape, and the reason this task and actionability do not overlap: these
        // resolve, and what is wrong with them is a question WW18 already answers.
        var resolution = Resolve.Once(Dialog(), Locator.Parse("""ListItem[name="Beta"]"""));

        Assert.True(resolution.Found);
        Assert.True(resolution.Facts!.IsOffscreen);
        Assert.Equal(Actionable.Offscreen, ActionabilityCheck.Of(resolution.Facts).State);
    }

    [Fact]
    public void An_index_past_the_end_is_a_miss()
    {
        Assert.False(Resolve.Once(Dialog(), Locator.Parse("Button[index=40]")).Found);
    }

    [Fact]
    public void Waiting_for_something_that_never_arrives_reports_what_it_spent()
    {
        var resolution = Resolve.Until(Dialog(), Locator.Parse("""Slider[name="Volume"]"""), 150, pollMs: 20);

        Assert.False(resolution.Found);
        Assert.True(resolution.WaitedMs >= 150);
        Assert.True(resolution.Polls > 1);
    }

    [Fact]
    public void What_resolves_is_what_actionability_then_judges()
    {
        onScreen = true;
        var resolution = Resolve.Once(Dialog(), Locator.Parse("""Button[name="Save"]"""));

        Assert.True(ActionabilityCheck.Of(resolution.Facts, "Invoke").CanAct);
    }

    [Fact]
    public void A_pattern_in_the_locator_narrows_what_matches()
    {
        var dialog = Dialog();

        Assert.True(Resolve.Once(dialog, Locator.Parse("""Button[name="Save"][pattern=Invoke]""")).Found);
        Assert.False(Resolve.Once(dialog, Locator.Parse("""Button[name="Save"][pattern=Scroll]""")).Found);
    }
}

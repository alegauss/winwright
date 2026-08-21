using System.Runtime.InteropServices;
using System.Windows.Automation;

using Winwright.Locating;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW21. The sidebar items in claude-tray are bare borders with no automation peer, so they are
/// matched by the text inside them — and the page title carries the same words. Sorting by the
/// rectangle picks the one on the left, and that choice belongs in the locator where the next
/// reader can see it was made.
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class MatchOrderTests : IDisposable
{
    private const uint WsPopup = 0x80000000;
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;

    private readonly List<nint> created = [];

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    public void Dispose()
    {
        for (var index = created.Count - 1; index >= 0; index--)
            DestroyWindow(created[index]);
    }

    private nint Create(string className, string? title, uint style, int x, int y, int w, int h, nint parent = 0)
    {
        var window = CreateWindowExW(0, className, title, style, x, y, w, h, parent, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
        return window;
    }

    /// <summary>
    /// The claude-tray shape: a sidebar entry and a page title carrying the same words, created in
    /// the order that makes the tree's answer the wrong one.
    /// </summary>
    private AutomationElement SidebarAndTitle()
    {
        var frame = Create("Static", "winwright statistics", WsPopup | WsVisible, 20, 20, 600, 400);
        Create("Button", "Statistics", WsChild | WsVisible, 300, 10, 200, 30, frame);   // the page title, on the right
        Create("Button", "Statistics", WsChild | WsVisible, 10, 60, 120, 30, frame);    // the sidebar item, on the left
        return AutomationElement.FromHandle(frame);
    }

    [Fact]
    public void Two_elements_with_the_same_name_are_refused_rather_than_guessed_between()
    {
        var refusal = Assert.Throws<AmbiguousLocatorException>(
            () => Resolve.Once(SidebarAndTitle(), Locator.Parse("""Button[name="Statistics"]""")));

        Assert.Equal(2, refusal.Candidates.Count);
        Assert.Contains("does not say which", refusal.Message);
        Assert.Contains("[order=left|right|top|bottom]", refusal.Message);
    }

    [Fact]
    public void The_refusal_names_what_matched_as_locators_with_their_rectangles()
    {
        var refusal = Assert.Throws<AmbiguousLocatorException>(
            () => Resolve.Once(SidebarAndTitle(), Locator.Parse("""Button[name="Statistics"]""")));

        Assert.All(refusal.Candidates, candidate => Assert.Contains("""Button[name="Statistics"]""", candidate));
        Assert.All(refusal.Candidates, candidate => Assert.Contains(" at ", candidate));
    }

    [Fact]
    public void Ordering_by_the_rectangle_picks_the_one_on_the_left()
    {
        var resolution = Resolve.Once(SidebarAndTitle(), Locator.Parse("""Button[name="Statistics"][order=left]"""));

        Assert.True(resolution.Found);
        Assert.Equal(120, resolution.Facts!.Bounds.Width);
    }

    [Fact]
    public void The_other_three_orders_pick_the_other_ends()
    {
        var frame = SidebarAndTitle();

        Assert.Equal(200, Resolve.Once(frame, Locator.Parse("""Button[name="Statistics"][order=right]""")).Facts!.Bounds.Width);
        Assert.Equal(200, Resolve.Once(frame, Locator.Parse("""Button[name="Statistics"][order=top]""")).Facts!.Bounds.Width);
        Assert.Equal(120, Resolve.Once(frame, Locator.Parse("""Button[name="Statistics"][order=bottom]""")).Facts!.Bounds.Width);
    }

    [Fact]
    public void An_index_counts_within_the_order_that_was_asked_for()
    {
        var frame = SidebarAndTitle();

        Assert.Equal(200, Resolve.Once(frame, Locator.Parse("""Button[name="Statistics"][order=left][index=2]""")).Facts!.Bounds.Width);
        Assert.Equal(120, Resolve.Once(frame, Locator.Parse("""Button[name="Statistics"][order=right][index=2]""")).Facts!.Bounds.Width);
    }

    [Fact]
    public void An_index_on_its_own_still_says_which_and_is_not_refused()
    {
        Assert.True(Resolve.Once(SidebarAndTitle(), Locator.Parse("""Button[name="Statistics"][index=1]""")).Found);
    }

    [Fact]
    public void One_match_needs_no_disambiguation()
    {
        var frame = Create("Static", "winwright bare", WsPopup | WsVisible, 20, 20, 300, 200);
        Create("Button", "Save", WsChild | WsVisible, 10, 10, 90, 28, frame);

        Assert.True(Resolve.Once(AutomationElement.FromHandle(frame), Locator.Parse("""Button[name="Save"]""")).Found);
    }

    [Fact]
    public void The_order_is_part_of_the_grammar_and_round_trips()
    {
        var locator = Locator.Parse("""Button[name="Statistics"][order=left][index=2]""");

        Assert.Equal(MatchOrder.Left, locator.Steps[0].Order);
        Assert.Equal("""Button[name="Statistics"][order=left][index=2]""", locator.ToString());
        Assert.Equal(locator.Steps, Locator.Parse(locator.ToString()).Steps);
    }

    [Fact]
    public void An_order_the_grammar_does_not_have_is_refused_with_the_ones_it_does()
    {
        var refusal = Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("Button[order=leftish]"));

        Assert.Contains("they are left, right, top, bottom", refusal.Because);
    }

    [Fact]
    public void The_tree_order_is_not_something_a_locator_can_ask_for_by_name()
    {
        // It is the default and the arbitrary one; naming it would read as a choice having been
        // made, which is the whole thing this task is about.
        Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("Button[order=tree]"));
    }

    [Fact]
    public void An_order_on_its_own_is_a_step_that_constrains_something()
    {
        Assert.Equal(MatchOrder.Left, Locator.Parse("[order=left]").Steps[0].Order);
    }
}

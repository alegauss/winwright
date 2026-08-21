using System.Runtime.InteropServices;

using Winwright.Locating;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW19. The control view as a verb, so a locator is written from the tree instead of from the
/// markup — the markup being the thing the check is about to assert on.
/// <para>
/// The fixture builds a real window with real Win32 children, because a tree walk proved against
/// a fake tree proves the fake.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class InspectTests : IDisposable
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

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnableWindow(nint window, [MarshalAs(UnmanagedType.Bool)] bool enable);

    public void Dispose()
    {
        for (var index = created.Count - 1; index >= 0; index--)
            DestroyWindow(created[index]);
    }

    private nint Create(string className, string? title, uint style, int width, int height, nint parent = 0)
    {
        var window = CreateWindowExW(0, className, title, style, 20, 20, width, height, parent, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
        return window;
    }

    /// <summary>A frame with a button and a text box in it, which is a control view with depth.</summary>
    private nint Dialog()
    {
        var frame = Create("Static", "winwright statistics", WsPopup | WsVisible, 420, 300);
        Create("Button", "Save", WsChild | WsVisible, 90, 28, frame);
        Create("Edit", "profile", WsChild | WsVisible, 200, 24, frame);
        return frame;
    }

    [Fact]
    public void The_control_view_of_a_live_window_carries_its_children()
    {
        var tree = Inspect.Window(Dialog());

        Assert.NotNull(tree);
        Assert.Equal("winwright statistics", tree.Facts.Name);
        Assert.Contains(tree.Walk(), element => element.Facts.ControlType == "Button");
        Assert.Contains(tree.Walk(), element => element.Facts.ControlType == "Edit");
    }

    [Fact]
    public void Every_line_begins_with_a_locator_that_addresses_what_it_describes()
    {
        var tree = Inspect.Window(Dialog())!;

        foreach (var element in tree.Walk())
        {
            var step = element.Facts.AsLocatorStep().ToString();
            Assert.True(
                Locator.TryParse(step, out _, out var because),
                $"inspect rendered '{step}', which the grammar refuses: {because}");
        }
    }

    [Fact]
    public void A_line_carries_the_id_type_name_class_and_rectangle()
    {
        var tree = Inspect.Window(Dialog())!;
        var button = Assert.Single(tree.Walk(), element => element.Facts.ControlType == "Button");

        var line = Inspect.Line(button);

        Assert.StartsWith("Button", line);
        Assert.Contains("""[name="Save"]""", line);
        Assert.Contains("[class=\"Button\"]", line);
        Assert.Contains("90x28 at ", line);
    }

    [Fact]
    public void The_patterns_it_offers_are_on_the_line_too()
    {
        var tree = Inspect.Window(Dialog())!;
        var button = Assert.Single(tree.Walk(), element => element.Facts.ControlType == "Button");

        Assert.Contains("Invoke", Inspect.Line(button));
    }

    [Fact]
    public void A_disabled_control_says_so_on_its_own_line()
    {
        var frame = Create("Static", "winwright statistics", WsPopup | WsVisible, 420, 300);
        var button = Create("Button", "Save", WsChild | WsVisible, 90, 28, frame);
        EnableWindow(button, false);

        var tree = Inspect.Window(frame)!;
        var found = Assert.Single(tree.Walk(), element => element.Facts.ControlType == "Button");

        Assert.False(found.Facts.IsEnabled);
        Assert.Contains(" disabled", Inspect.Line(found));
    }

    [Fact]
    public void The_render_is_indented_by_depth()
    {
        var lines = Inspect.Render(Inspect.Window(Dialog())!);

        Assert.False(lines[0].StartsWith(' '));
        Assert.Contains(lines, line => line.StartsWith("  Button", StringComparison.Ordinal));
    }

    [Fact]
    public void A_walk_that_stops_at_the_depth_says_how_many_it_did_not_walk()
    {
        var tree = Inspect.Window(Dialog(), depth: 0)!;

        Assert.Empty(tree.Children);
        Assert.Equal(2, tree.Elided);
        Assert.Contains(Inspect.Render(tree), line => line.Contains("... 2 more not walked", StringComparison.Ordinal));
    }

    [Fact]
    public void A_walk_that_stops_at_the_width_says_so_too()
    {
        var tree = Inspect.Window(Dialog(), width: 1)!;

        Assert.Single(tree.Children);
        Assert.Equal(1, tree.Elided);
    }

    [Fact]
    public void A_handle_that_names_no_element_answers_nothing_rather_than_throwing()
    {
        Assert.Null(Inspect.Window(0));
        Assert.Null(Inspect.Window(0x7FFFFFFF));
    }

    [Fact]
    public void A_negative_depth_or_a_width_of_nothing_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Inspect.Window(Dialog(), depth: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Inspect.Window(Dialog(), width: 0));
    }

    [Fact]
    public void What_inspect_prints_is_what_actionability_reads()
    {
        var tree = Inspect.Window(Dialog())!;
        var button = Assert.Single(tree.Walk(), element => element.Facts.ControlType == "Button");

        Assert.True(ActionabilityCheck.Of(button.Facts, "Invoke").CanAct);
    }
}

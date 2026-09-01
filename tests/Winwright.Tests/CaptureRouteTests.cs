using System.Diagnostics;

using Winwright.Acting;
using Winwright.Capturing;
using Winwright.Processes;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW34. A screen copy can photograph anything that happens to be in the rectangle — the window
/// that stole the foreground, the notification that arrived, the editor the run was started from.
/// A render of a visual tree cannot.
/// <para>
/// The last two tests open a real menu and route a capture of it, because the one case a render
/// cannot reach is the only reason the screen copy exists at all.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class CaptureRouteTests
{
    private static TopLevelWindow Window(
        nint handle, string className = "Window", nint owner = 0, string title = "winwright statistics") =>
        new(handle, 1234, title, className, new WindowBounds(0, 0, 600, 400), true, owner);

    [Fact]
    public void The_application_window_is_rendered_and_not_photographed()
    {
        var main = Window(0x1000);

        var route = CaptureRoute.For(main, main);

        Assert.True(route.Renders);
        Assert.Equal(Route.OffScreenRender, route.Taken);
        Assert.Equal(OutOfReach.Renderable, route.Reach);
        Assert.Contains("its visual tree is renderable", route.Sentence());
    }

    [Fact]
    public void A_second_window_of_the_application_is_rendered_too()
    {
        // Not a case the screen copy exists for: it has a visual tree of its own, so the render
        // reaches it by being pointed at that tree rather than at the first one.
        var main = Window(0x1000);
        var second = Window(0x2000, title: "Settings");

        var route = CaptureRoute.For(second, main);

        Assert.True(route.Renders);
        Assert.Contains("is a window of the application", route.Sentence());
    }

    /// <summary>
    /// WW320. An application showing only a menu has no main window to measure it against, and the
    /// two ways round it were both wrong: the menu as its own main answers Render, and
    /// <c>Forced</c> records Renderable — that a render would have worked and somebody chose not to.
    /// </summary>
    [Fact]
    public void A_menu_routes_with_no_main_window_to_compare_it_against()
    {
        var route = CaptureRoute.For(Window(0x3000, "#32768", owner: 0x1000, title: ""));

        Assert.False(route.Renders);
        Assert.Equal(OutOfReach.Menu, route.Reach);
        Assert.Contains("is a menu", route.Sentence());
    }

    /// <summary>
    /// WW320. And the answer the main window is actually for is the one it keeps: without it,
    /// nothing here can say a window is <em>the</em> window, so a renderable one routes to a render
    /// on its own tree rather than on the application's first.
    /// </summary>
    [Fact]
    public void A_renderable_window_with_no_main_beside_it_is_still_rendered()
    {
        var route = CaptureRoute.For(Window(0x1000));

        Assert.True(route.Renders);
        Assert.Contains("is a window of the application", route.Sentence());
    }

    /// <summary>
    /// WW320. The overload is what the two-argument one answers with once the main window has said
    /// no, so a class it routes cannot route differently depending on which was called.
    /// </summary>
    [Theory]
    [InlineData("#32768", 0x1000)]
    [InlineData("tooltips_class32", 0x1000)]
    [InlineData("Window", 0x1000)]
    [InlineData("Window", 0)]
    public void The_two_agree_about_every_window_that_is_not_the_main_one(string className, int owner)
    {
        var window = Window(0x3000, className, owner, title: "");

        var alone = CaptureRoute.For(window);
        var against = CaptureRoute.For(window, Window(0x1000));

        Assert.Equal(against.Taken, alone.Taken);
        Assert.Equal(against.Reach, alone.Reach);
        Assert.Equal(against.Because, alone.Because);
    }

    [Fact]
    public void A_menu_is_the_case_a_render_cannot_reach()
    {
        var route = CaptureRoute.For(Window(0x3000, "#32768", owner: 0x1000, title: ""), Window(0x1000));

        Assert.False(route.Renders);
        Assert.Equal(OutOfReach.Menu, route.Reach);
        Assert.Contains("is a menu", route.Sentence());
        Assert.Contains("in no tree the application can render", route.Sentence());
    }

    [Fact]
    public void A_balloon_is_another()
    {
        var route = CaptureRoute.For(Window(0x3000, "tooltips_class32", owner: 0x1000, title: ""), Window(0x1000));

        Assert.Equal(OutOfReach.Balloon, route.Reach);
        Assert.Contains("is a tooltip or balloon", route.Sentence());
    }

    [Fact]
    public void An_owned_popup_is_the_third()
    {
        var route = CaptureRoute.For(Window(0x3000, "HwndWrapper", owner: 0x1000, title: ""), Window(0x1000));

        Assert.Equal(OutOfReach.OwnedPopup, route.Reach);
        Assert.Contains("is a popup owned by another window", route.Sentence());
    }

    /// <summary>
    /// WW87. The fourth, and the one ownership could not answer. freewilly's menu verb shows a
    /// WinForms drop-down with no form behind it, so GW_OWNER answers zero — and the class name it
    /// carries has a per-thread number in it, so nothing can match on that either. Before this it
    /// routed to a render, and a render of a drop-down has no tree to draw.
    /// </summary>
    [Fact]
    public void A_drop_down_that_nothing_owns_is_a_popup_too()
    {
        var menu = Window(0x3000, "WindowsForms10.Window.20808.app.0.5c39d4_r3_ad1", owner: 0, title: "")
            with { Popup = true };

        var route = CaptureRoute.For(menu);

        Assert.False(route.Renders, route.Sentence());
        Assert.Equal(OutOfReach.OwnedPopup, route.Reach);

        // Not "owned by another window", which is the half of the arm this one is not: a reader sent
        // looking for the window that owns it would find nothing and conclude the reading was wrong.
        Assert.Contains("is a popup a framework drew", route.Sentence());
        Assert.DoesNotContain("owned by", route.Sentence());
    }

    /// <summary>WW87, and the same agreement the two overloads owe every other class.</summary>
    [Fact]
    public void The_two_agree_about_a_drop_down_as_well()
    {
        var menu = Window(0x3000, "WindowsForms10.Window.20808.app.0", owner: 0, title: "") with { Popup = true };

        var alone = CaptureRoute.For(menu);
        var against = CaptureRoute.For(menu, Window(0x1000));

        Assert.Equal(against.Taken, alone.Taken);
        Assert.Equal(against.Reach, alone.Reach);
        Assert.Equal(against.Because, alone.Because);
    }

    /// <summary>
    /// WW87. A window with a title bar is a window somebody sized and moved, whatever else its style
    /// bits say — so the caption is what keeps the popup rule off the application's own frames.
    /// </summary>
    [Fact]
    public void A_window_nobody_read_the_style_bits_of_is_not_a_popup()
    {
        var route = CaptureRoute.For(Window(0x2000, title: "Settings"));

        Assert.True(route.Renders, route.Sentence());
    }

    [Fact]
    public void A_menu_is_named_a_menu_and_never_a_popup_although_it_is_owned()
    {
        // Both tests would pass on ownership alone, and calling a menu a popup sends whoever reads
        // the receipt looking for a flyout somebody opened.
        var route = CaptureRoute.For(Window(0x3000, "#32768", owner: 0x1000, title: ""), Window(0x1000));

        Assert.Equal(OutOfReach.Menu, route.Reach);
        Assert.DoesNotContain("popup", route.Sentence());
    }

    [Fact]
    public void A_forced_screen_copy_has_to_say_why()
    {
        var refused = Assert.Throws<ArgumentException>(() => CaptureRoute.Forced("  "));

        Assert.Contains("because", refused.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_forced_copy_carries_the_reason_into_the_receipt()
    {
        var route = CaptureRoute.Forced("the surface is drawn by the shell and has no tree at all");

        Assert.False(route.Renders);
        Assert.Equal("screen copy", route.ToString());
        Assert.Contains("copied from the screen: the surface is drawn by the shell", route.Sentence());
    }

    [Fact]
    public void A_receipt_that_recorded_no_route_says_nothing_rather_than_claiming_the_default()
    {
        using var self = Process.GetCurrentProcess();
        var window = Window(0x1000) with { Pid = self.Id };
        var receipt = CaptureReceipt.Of("shot.png", window, AppTarget.AttachTo(self.Id));

        Assert.Null(receipt.Route);
        Assert.DoesNotContain("off-screen render", receipt.Sentence());
        Assert.Equal("capture", receipt.AsTraceStep().Verb);
    }

    [Fact]
    public void A_receipt_that_did_route_says_which_way_and_why()
    {
        using var self = Process.GetCurrentProcess();
        var main = Window(0x1000) with { Pid = self.Id };
        var receipt = CaptureReceipt.Of(
            "shot.png", main, AppTarget.AttachTo(self.Id), null, CaptureRoute.For(main, main));

        Assert.Contains("rendered off-screen", receipt.Sentence());
        Assert.Equal("capture (off-screen render)", receipt.AsTraceStep().Verb);
    }

    [Fact]
    public void A_real_menu_on_a_real_window_routes_to_the_copy_that_can_reach_it()
    {
        using var dialog = PumpedDialog.OpenWithMenu(
            "winwright statistics", new PumpedDialog.MenuEntry("File", new PumpedDialog.MenuEntry("New")));

        try
        {
            // Entering the bar is not enough: the popup window only exists once an entry is
            // expanded, and that popup is the thing no render can reach.
            Menu.Enter(dialog.Frame);
            Menu.To(dialog.Frame, "New");

            using var self = Process.GetCurrentProcess();
            var windows = TopLevelWindows.OfProcess(self.Id, smallest: 0);
            var menu = windows.FirstOrDefault(one => one.ClassName == "#32768");

            // WW179. A shell that put no menu on the screen is a desk that refused, not a harness
            // that broke — and this used to end the case as the second. Excused here, so a reader
            // is sent to their own desk rather than to this repository.
            if (menu is null)
            {
                var because = $"the shell put no menu window on this desk; "
                    + $"highlighted={Menu.Highlighted(dialog.Frame)}; "
                    + string.Join(" | ", windows.Select(one => one.ToString()));

                Assert.True(
                    BusyDesk.Excused(Winwright.Verdicts.Precondition.Absent(Foreground.PreconditionName, because)),
                    because);

                return;
            }

            var main = windows.Single(one => one.Handle == dialog.Frame);
            var route = CaptureRoute.For(menu!, main);

            Assert.False(route.Renders, route.Sentence());
            Assert.Equal(OutOfReach.Menu, route.Reach);

            // WW87, and measured here rather than composed: the style bits an enumeration reads are
            // what routes a drop-down that nothing owns, and the pair is what says the rule discriminates
            // — the menu on this desk is a popup and the frame under it is not.
            Assert.True(menu.Popup, menu.ToString());
            Assert.False(main.Popup, main.ToString());
        }
        finally
        {
            for (var attempt = 0; attempt < 6 && Menu.Highlighted(dialog.Frame) is not null; attempt++)
                Menu.Dismiss(1);
        }
    }
}

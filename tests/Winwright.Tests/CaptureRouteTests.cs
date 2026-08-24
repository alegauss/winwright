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
        }
        finally
        {
            for (var attempt = 0; attempt < 6 && Menu.Highlighted(dialog.Frame) is not null; attempt++)
                Menu.Dismiss(1);
        }
    }
}

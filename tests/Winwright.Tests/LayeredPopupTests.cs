using Winwright.Capturing;
using Winwright.Processes;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW347. A popup a framework drew is layered for the drop shadow it draws itself, so the two
/// readings that route a capture close every way in at once: the render cannot reach a surface in no
/// tree the application can hand over, and the copy that can reach it is refused for the soft edge
/// it would carry.
/// <para>
/// Both readings are right, which is why this is a narrowing rather than a bug in either. What was
/// wrong is that nothing in this suite ever photographed a WPF popup, so the arm never fired on a
/// real one and nothing went red — and the first adopter to try met a refusal with nothing beside
/// it. These cases put the surface on the desk, take the refusal it produces, and drive the way
/// through that refusal now names.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class LayeredPopupTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-flyout-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private static TopLevelWindow? Found(nint handle) =>
        TopLevelWindows.OfProcess(Environment.ProcessId, smallest: 0)
            .FirstOrDefault(one => one.Handle == handle);

    [Fact]
    public void A_wpf_popup_is_layered_per_pixel_for_the_shadow_it_draws_itself()
    {
        using var flyout = PumpedFlyout.Open("shadowedFlyout");

        // Asserted and never skipped past, which is this project's own rule: a green covering an
        // assertion that did not run is the failure every excuse in this suite exists to avoid. And
        // there is nothing here to excuse — a popup puts its window up on its own thread with
        // nothing pumping and no desk involved, so a zero is WPF having failed rather than the desk
        // being busy.
        Assert.NotEqual(0, flyout.Handle);

        var layers = SeeThrough.Of(flyout.Handle);

        // The measurement the design was filed on, taken again rather than quoted: layered, and
        // GetLayeredWindowAttributes refuses — which is what an alpha per pixel answers, and there
        // is no attribute that says how much of the window is the desktop.
        Assert.Equal(Layering.PerPixel, layers.Layers);
        Assert.True(layers.Transmits, layers.Sentence());

        // And the backdrop says nothing about it, which is why the layer is read at all. The popup
        // never asked the compositor for anything, and the reading answers that truthfully.
        Assert.False(Glass.Of(flyout.Handle).Transmits);
    }

    [Fact]
    public void The_route_sends_a_wpf_popup_to_the_copy_that_is_then_refused()
    {
        // The narrowing performed end to end, which is the case that had never existed. The route
        // is right, the refusal is right, and between them a real surface has no picture.
        using var flyout = PumpedFlyout.Open("routedFlyout");
        Assert.NotEqual(0, flyout.Handle);

        var window = Found(flyout.Handle);
        Assert.NotNull(window);

        // A popup by the style bits and owned by nothing, which is the arm WW87 added: the
        // ownership test alone would have called this a window of the application and routed it to
        // a render with no tree to draw.
        Assert.True(window.Popup, $"the flyout is not drawn as a popup: {window}");
        Assert.False(window.IsOwned, $"something owns the flyout: {window}");

        var route = CaptureRoute.For(window);
        Assert.False(route.Renders, route.Sentence());
        Assert.Equal(OutOfReach.OwnedPopup, route.Reach);

        // The reading, taken rather than guarded on: the case above asserts what a WPF popup's layer
        // is, so a run where it came back anything else has a red there and this one would only be
        // hiding the second half of the same news.
        var layers = SeeThrough.Of(flyout.Handle);
        Assert.True(layers.Transmits, layers.Sentence());

        var refused = Assert.Throws<WrongCaptureException>(
            () => CaptureReceipt.Of(
                Path.Combine(root, "flyout.png"),
                window,
                AppTarget.AttachTo(Environment.ProcessId),
                route: route,
                layers: layers));

        Assert.Equal(WrongCapture.LayerTransmits, refused.Arm);
        Assert.Contains("alpha per pixel", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_refusal_names_the_way_through_rather_than_leaving_a_popup_with_none()
    {
        // The half WW347 added, and the reason it is on the route rather than on the reading: the
        // layer is a fact about the window, and which half of the harness can still draw it is not.
        using var flyout = PumpedFlyout.Open("signpostedFlyout");
        Assert.NotEqual(0, flyout.Handle);

        var window = Found(flyout.Handle);
        Assert.NotNull(window);

        var said = CaptureRoute.StillReachable(window);
        Assert.Contains("Popups.Picture", said, StringComparison.Ordinal);

        var layers = SeeThrough.Of(flyout.Handle);
        Assert.True(layers.Transmits, layers.Sentence());

        var refused = Assert.Throws<WrongCaptureException>(
            () => CaptureReceipt.Of(
                Path.Combine(root, "signposted.png"),
                window,
                AppTarget.AttachTo(Environment.ProcessId),
                route: CaptureRoute.For(window),
                layers: layers));

        // Composed rather than spelled twice: a case matching a phrase is one that stops matching
        // the day somebody rewords the sentence, and this asserts the refusal carries the clause the
        // route composed.
        Assert.EndsWith(said, refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_window_that_is_not_a_popup_is_offered_nothing_because_the_render_already_reaches_it()
    {
        // The other half of the same rule. A layered window of the application is refused by the
        // same arm, and there is no narrowing to signpost: its own visual tree is renderable, so
        // the route the refusal would name is the one the caller passed over.
        var window = new TopLevelWindow(
            0x1234,
            Environment.ProcessId,
            "winwright frame",
            "HwndWrapper[Winwright.Tests;;]",
            new WindowBounds(0, 0, 600, 400),
            Visible: true,
            Owner: 0);

        Assert.Equal("", CaptureRoute.StillReachable(window));
        Assert.True(CaptureRoute.For(window, window).Renders);
    }

    [Fact]
    public void The_popups_own_tree_is_photographed_where_no_copy_of_the_screen_could_be()
    {
        // The way through, driven against the surface that needed it. Nothing composited: the child
        // is an element in a tree this process owns, so it draws with no compositor, no z order, no
        // shadow, and no edge that is whatever the popup is standing in front of.
        using var flyout = PumpedFlyout.Open("photographedFlyout", width: 160, height: 90);
        Assert.NotEqual(0, flyout.Handle);

        var path = Path.Combine(root, "through-the-application.png");
        var picture = flyout.Picture(path);

        Assert.True(File.Exists(path), picture.Sentence());
        Assert.Equal(160, picture.Width);
        Assert.Equal(90, picture.Height);

        // And it is a picture of the popup rather than of a rectangle: the colour count is the
        // reading that tells those apart, and it is taken on the file this wrote.
        var colours = Colours.In(path);
        Assert.False(colours.IsFlat, colours.Sentence());

        // The application it was only supposed to photograph is unchanged. The popup's own root had
        // already laid the child out, and the render asks for the size it settled on rather than
        // for as much room as it is given.
        var after = flyout.Laid();
        Assert.Equal(160, after.Width);
        Assert.Equal(90, after.Height);
    }
}

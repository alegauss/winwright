using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;

namespace Winwright.Fixture;

/// <summary>A panel that is in no accessibility tree, and neither is anything under it.</summary>
public sealed class PeerlessPanel : Canvas
{
    /// <summary>
    /// None. Returning null is the documented way to keep an element out of the tree, and it is
    /// what a custom-drawn surface does by accident when nobody wrote a peer for it.
    /// </summary>
    protected override AutomationPeer OnCreateAutomationPeer() => null!;
}

/// <summary>A box on that panel, invisible to UI Automation for the same reason.</summary>
public sealed class PeerlessBox : Border
{
    /// <inheritdoc cref="PeerlessPanel.OnCreateAutomationPeer" />
    protected override AutomationPeer OnCreateAutomationPeer() => null!;
}

/// <summary>
/// A surface with no accessibility tree at all.
/// <para>
/// The geometry dump exists because some surfaces have none: an installer page, a custom-drawn
/// control, an immediate-mode canvas. The only example available otherwise is a page in another
/// repository behind a compiler that has to be installed first.
/// </para>
/// <para>
/// Every element here is laid out, measured and drawn, and none of it is in the control view. A
/// locator resolves against nothing on this pane and the dump reports all of it, which is the
/// contrast the dump was built for and the one nothing else here can produce.
/// </para>
/// </summary>
public static class Peerless
{
    /// <summary>What the boxes on it are called, in the order they are laid out.</summary>
    public static IReadOnlyList<string> Boxes { get; } = ["drawnHeader", "drawnBody", "drawnFooter"];

    /// <summary>Build the pane and add it to the tab control, selected.</summary>
    /// <param name="panes">The tab control to add it to.</param>
    public static TabItem AddTo(TabControl panes)
    {
        ArgumentNullException.ThrowIfNull(panes);

        var surface = new PeerlessPanel { Name = "drawnSurface", Width = 420, Height = 220 };

        var top = 0d;
        foreach (var (name, height) in Boxes.Zip<string, double>([40, 100, 40]))
        {
            var box = new PeerlessBox
            {
                Name = name,
                Width = 400,
                Height = height,
                Background = new SolidColorBrush(Color.FromRgb(0x1f, 0x6f, 0xeb)),
            };

            Canvas.SetLeft(box, 10);
            Canvas.SetTop(box, top);
            surface.Children.Add(box);
            top += height + 10;
        }

        var pane = new TabItem { Name = "drawnPane", Header = "Drawn", Content = surface };
        panes.Items.Add(pane);

        // Selected, or the surface is realised into nothing and the pane proves only that a tab
        // nobody picked has no contents - which every pane in this fixture already shows.
        panes.SelectedItem = pane;
        return pane;
    }
}

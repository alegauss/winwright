using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Winwright.Fixture;

/// <summary>
/// A crop rectangle drawn on a surface, whose top-left handle is pixels and no element. WW572.
/// <para>
/// Found by PhotoBoost (PB67): its crop editor is drawn on a GPU surface, so the eight handles are
/// pixels, and resizing the crop is a press on a handle released somewhere else on the same surface.
/// <c>drag</c> only went from one element onto another, so the gesture had no case. The surface here
/// is the one element; a press within a few pixels of the rectangle's top-left corner takes the
/// handle, and a press anywhere else moves nothing, which is what the control case presses.
/// </para>
/// </summary>
public static class HandlesPane
{
    /// <summary>The surface's size, in device-independent pixels.</summary>
    private const double Wide = 300;

    private const double High = 150;

    /// <summary>How near the corner a press has to land to take the handle.</summary>
    private const double Reach = 12;

    /// <summary>Build the pane and add it to the tab control, selected.</summary>
    /// <param name="panes">The tab control to add it to.</param>
    public static TabItem AddTo(TabControl panes)
    {
        ArgumentNullException.ThrowIfNull(panes);

        var crop = new Rect(30, 15, 240, 120);
        var said = new TextBlock { Name = "cropSaid", Margin = new Thickness(0, 0, 0, 8) };
        var drawn = new Rectangle { Stroke = Brushes.DarkOrange, StrokeThickness = 2 };
        var surface = new Surface { Name = "cropSurface", Width = Wide, Height = High, Background = Brushes.LightSteelBlue };
        surface.Children.Add(drawn);

        void Show()
        {
            Canvas.SetLeft(drawn, crop.Left);
            Canvas.SetTop(drawn, crop.Top);
            drawn.Width = crop.Width;
            drawn.Height = crop.Height;

            var text = $"crop: {crop.Left:0},{crop.Top:0} {crop.Width:0}x{crop.Height:0}";
            said.Text = text;
            System.Windows.Automation.AutomationProperties.SetName(said, text);
        }

        var holding = false;
        surface.MouseLeftButtonDown += (_, pressed) =>
        {
            var at = pressed.GetPosition(surface);
            holding = Math.Abs(at.X - crop.Left) <= Reach && Math.Abs(at.Y - crop.Top) <= Reach;
            if (holding)
                surface.CaptureMouse();
        };

        surface.MouseMove += (_, moved) =>
        {
            if (holding)
                crop = Cornered(crop, moved.GetPosition(surface));
        };

        surface.MouseLeftButtonUp += (_, released) =>
        {
            if (!holding)
                return;

            holding = false;
            surface.ReleaseMouseCapture();

            // Snapped to five, so a pointer a pixel off on a scaled display reads the same.
            var at = released.GetPosition(surface);
            crop = Cornered(crop, new Point(Math.Round(at.X / 5) * 5, Math.Round(at.Y / 5) * 5));
            Show();
        };

        Show();

        var page = new StackPanel { Name = "handlesRows", Margin = new Thickness(12) };
        page.Children.Add(said);
        page.Children.Add(surface);

        var pane = new TabItem { Name = "handlesPane", Header = "Handles", Content = page };
        panes.Items.Add(pane);

        // Selected, because a pane nobody picked is in no tree for a case to reach.
        panes.SelectedItem = pane;
        return pane;
    }

    /// <summary>The crop with its top-left corner moved to a point, keeping its bottom-right where it was.</summary>
    private static Rect Cornered(Rect crop, Point to)
    {
        var left = Math.Clamp(to.X, 0, crop.Right - 10);
        var top = Math.Clamp(to.Y, 0, crop.Bottom - 10);
        return new Rect(left, top, crop.Right - left, crop.Bottom - top);
    }

    /// <summary>A canvas with an automation peer, so a locator can reach the one element the handles are drawn on.</summary>
    private sealed class Surface : Canvas
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new SurfacePeer(this);
    }

    private sealed class SurfacePeer(Surface owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

        protected override string GetClassNameCore() => "CropSurface";
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Winwright.Fixture;

/// <summary>
/// A strip of tabs that reorders only when one is dragged onto another.
/// <para>
/// WW520. Found adopting quickshell's tab strip (QS250): a WPF <c>TabControl</c> whose tabs move when
/// dragged across their neighbours. A case could click a tab and read whether it was selected, and
/// had no verb to press on one, move to another and let go, so the reorder was proven only by an
/// in-process test calling the method the drag calls.
/// </para>
/// <para>
/// Driven the way that strip is: the press, a move past
/// <see cref="SystemParameters.MinimumHorizontalDragDistance"/> with the button held, and the release
/// over another tab. No pattern and no key reorders it, so a case that passes here made the gesture.
/// A press and release on the same tab is a click and moves nothing, which is the control.
/// </para>
/// </summary>
public static class DragsPane
{
    /// <summary>The tabs, in the order the strip opens with.</summary>
    public static IReadOnlyList<string> Opening { get; } = ["First", "Second", "Third"];

    /// <summary>Build the pane and add it to the tab control, selected.</summary>
    /// <param name="panes">The tab control to add it to.</param>
    public static TabItem AddTo(TabControl panes)
    {
        ArgumentNullException.ThrowIfNull(panes);

        var rows = new StackPanel { Name = "dragRows", Margin = new Thickness(12) };
        rows.Children.Add(new TextBlock
        {
            Name = "dragsTitle",
            Text = "Drags",
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 8),
        });

        var strip = new TabControl { Name = "dragStrip", Height = 120, Width = 420, HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var name in Opening)
        {
            strip.Items.Add(new TabItem
            {
                Name = $"{name.ToLowerInvariant()}Tab",
                Header = name,
                Width = 120,
                Content = new TextBlock { Text = $"{name}'s page", Margin = new Thickness(8) },
            });
        }

        rows.Children.Add(strip);

        // The read-out a case reads the order from, in the name as well as the text for WW238's reason.
        var order = new TextBlock { Name = "dragOrder", Margin = new Thickness(0, 8, 0, 0) };
        Said(order, strip);
        rows.Children.Add(order);

        Reorderable(strip, order);

        var pane = new TabItem { Name = "dragsPane", Header = "Drags", Content = rows };
        panes.Items.Add(pane);

        // Selected, because a tab's content is realised when it is picked and a pane nobody picked
        // is in no tree for a case to reach.
        panes.SelectedItem = pane;
        return pane;
    }

    /// <summary>
    /// The handlers a strip like quickshell's carries: remember the press, start the drag once the
    /// pointer has gone past the threshold with the button down, and move the pressed tab to wherever
    /// the release lands.
    /// </summary>
    private static void Reorderable(TabControl strip, TextBlock order)
    {
        TabItem? pressed = null;
        var from = default(Point);
        var dragging = false;

        strip.PreviewMouseLeftButtonDown += (_, e) =>
        {
            pressed = Under(strip, e.GetPosition(strip));
            from = e.GetPosition(strip);
            dragging = false;
        };

        strip.PreviewMouseMove += (_, e) =>
        {
            if (pressed is null || e.LeftButton != MouseButtonState.Pressed || dragging)
                return;

            var moved = e.GetPosition(strip) - from;
            if (Math.Abs(moved.X) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(moved.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            // Captured, so the release arrives here wherever the pointer is when it happens.
            dragging = true;
            strip.CaptureMouse();
        };

        strip.PreviewMouseLeftButtonUp += (_, e) =>
        {
            var source = pressed;
            var wasDragging = dragging;
            pressed = null;
            dragging = false;
            if (strip.IsMouseCaptured)
                strip.ReleaseMouseCapture();

            if (!wasDragging || source is null)
                return;

            var target = Under(strip, e.GetPosition(strip));
            if (target is null || ReferenceEquals(target, source))
                return;

            var to = strip.Items.IndexOf(target);
            strip.Items.Remove(source);
            strip.Items.Insert(to, source);
            strip.SelectedItem = source;
            Said(order, strip);
            e.Handled = true;
        };
    }

    /// <summary>The tab under a point in the strip, or null where the point is over no tab.</summary>
    private static TabItem? Under(TabControl strip, Point at)
    {
        var hit = strip.InputHitTest(at) as DependencyObject;
        while (hit is not null and not TabItem)
            hit = hit is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(hit) : LogicalTreeHelper.GetParent(hit);

        return hit is TabItem tab && strip.Items.Contains(tab) ? tab : null;
    }

    /// <summary>Write the strip's order into the read-out, as its text and as its name.</summary>
    private static void Said(TextBlock order, TabControl strip)
    {
        var said = string.Join(", ", strip.Items.OfType<TabItem>().Select(tab => tab.Header));
        order.Text = said;
        System.Windows.Automation.AutomationProperties.SetName(order, said);
    }
}

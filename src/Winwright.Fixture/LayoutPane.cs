using System.Windows;
using System.Windows.Controls;

namespace Winwright.Fixture;

/// <summary>
/// Three buttons in three boxes: one with room, one spilling out of a box too narrow for it, and one
/// laid out to nothing. WW539.
/// <para>
/// Found by quickshell (QS312): a tab's close button was in the tree, 22 by 22, and painted nothing,
/// because the theme's padding left its glyph no room — and every case passed, since a case could
/// read that an element exists and what it is called and never where it is drawn. Each box is a
/// <see cref="GroupBox"/> because a bare border has no automation peer and no locator reaches it.
/// </para>
/// <para>
/// The spill is a <see cref="Canvas"/> placing its button past the box's right edge, because a canvas
/// does not clip: the button is drawn where the tree says, outside its container, which is the claim
/// that has to fail. The empty one is zero wide, which is the shape QS312 took.
/// </para>
/// </summary>
public static class LayoutPane
{
    /// <summary>Build the pane and add it to the tab control, selected.</summary>
    /// <param name="panes">The tab control to add it to.</param>
    public static TabItem AddTo(TabControl panes)
    {
        ArgumentNullException.ThrowIfNull(panes);

        var rows = new StackPanel { Name = "layoutRows", Margin = new Thickness(12) };
        rows.Children.Add(new TextBlock { Name = "layoutTitle", Text = "Layout", FontSize = 16, Margin = new Thickness(0, 0, 0, 8) });

        rows.Children.Add(Box("roomyBox", 200, Placed("fitsButton", "Fits", 60, left: 8)));
        rows.Children.Add(Box("narrowBox", 60, Placed("spillsButton", "Spills", 80, left: 30)));
        rows.Children.Add(Box("squeezedBox", 200, Placed("emptyButton", "Empty", 0, left: 8)));

        var pane = new TabItem { Name = "layoutPane", Header = "Layout", Content = rows };
        panes.Items.Add(pane);

        // Selected, because a tab's content is realised when it is picked and a pane nobody picked is
        // in no tree for a locator to reach.
        panes.SelectedItem = pane;
        return pane;
    }

    private static GroupBox Box(string named, double width, UIElement content) => new()
    {
        Name = named,
        Header = named,
        Width = width,
        Height = 70,
        HorizontalAlignment = HorizontalAlignment.Left,
        Margin = new Thickness(0, 0, 0, 8),
        Content = content,
    };

    private static Canvas Placed(string named, string caption, double width, double left)
    {
        var button = new Button { Name = named, Content = caption, Width = width, Height = 24 };
        Canvas.SetLeft(button, left);
        Canvas.SetTop(button, 4);

        var canvas = new Canvas();
        canvas.Children.Add(button);
        return canvas;
    }
}

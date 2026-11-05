using System.Windows;
using System.Windows.Controls;

namespace Winwright.Fixture;

/// <summary>
/// Two lists of the same five values: one taking several rows at once, and one taking one. WW540.
/// <para>
/// Found by quickshell (QS314), whose sidebar takes several rows — Ctrl+click adds a row, Shift+click
/// takes a range — while <c>select</c> replaces the selection, so a case could hold one row and never
/// two. The single list is the control: adding to it would replace what it held, and is refused.
/// </para>
/// <para>
/// A pane of its own rather than a row on the pickers pane, and measured: that pane opens one of its
/// pickers dropped down, and a window holding an open popup was never given the desk, so every click
/// a case made on these lists was excused. Nothing here opens anything.
/// </para>
/// </summary>
public static class SelectionsPane
{
    /// <summary>What both lists hold, in order. Nothing is selected when the pane opens.</summary>
    public static IReadOnlyList<string> Values { get; } = ["Alpha", "Bravo", "Charlie", "Delta", "Echo"];

    /// <summary>Build the pane and add it to the tab control, selected.</summary>
    /// <param name="panes">The tab control to add it to.</param>
    public static TabItem AddTo(TabControl panes)
    {
        ArgumentNullException.ThrowIfNull(panes);

        var rows = new StackPanel { Name = "selectionRows", Margin = new Thickness(12) };
        rows.Children.Add(new TextBlock { Name = "selectionsTitle", Text = "Selections", FontSize = 16, Margin = new Thickness(0, 0, 0, 8) });

        var lists = new StackPanel { Orientation = Orientation.Horizontal };
        lists.Children.Add(Listed("severalList", "Several", SelectionMode.Extended));
        lists.Children.Add(Listed("oneList", "One", SelectionMode.Single));
        rows.Children.Add(lists);

        var pane = new TabItem { Name = "selectionsPane", Header = "Selections", Content = rows };
        panes.Items.Add(pane);

        // Selected, because a tab's content is realised when it is picked and a pane nobody picked is
        // in no tree for a locator to reach.
        panes.SelectedItem = pane;
        return pane;
    }

    private static ListBox Listed(string named, string caption, SelectionMode mode)
    {
        var list = new ListBox { Name = named, Width = 160, Height = 120, SelectionMode = mode, Margin = new Thickness(0, 0, 16, 0) };
        System.Windows.Automation.AutomationProperties.SetName(list, caption);

        foreach (var value in Values)
            list.Items.Add(new ListBoxItem { Content = value });

        return list;
    }
}

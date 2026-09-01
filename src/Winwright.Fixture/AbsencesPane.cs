using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Winwright.Fixture;

/// <summary>
/// Three kinds of absence on one page.
/// <para>
/// A collapsed pane, a closed popup and an unopened submenu are all missing from the tree and all
/// mean different things, and each one cost a real defect somewhere to learn. Having the three
/// behind one flag is what lets the classification of a miss be developed and asserted rather than
/// reasoned about from memory.
/// </para>
/// <para>
/// Each holds a control named for what it is, so a locator can be written for something that is
/// genuinely not there and the answer can be read for <em>why</em>. Beside them sits a control
/// that is simply present, because a classification that has never seen a hit cannot be trusted
/// about a miss.
/// </para>
/// </summary>
public static class AbsencesPane
{
    /// <summary>Build the pane and add it to the tab control, selected.</summary>
    /// <param name="panes">The tab control to add it to.</param>
    public static TabItem AddTo(TabControl panes)
    {
        ArgumentNullException.ThrowIfNull(panes);

        var rows = new StackPanel { Name = "absenceRows", Margin = new Thickness(12) };
        rows.Children.Add(new TextBlock
        {
            Name = "absencesTitle",
            Text = "Absences",
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 8),
        });

        // Present, so a reading that never saw a hit is not what classifies the misses.
        rows.Children.Add(new Button
        {
            Name = "showing",
            Content = "Showing",
            Width = 160,
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Left,
        });

        // WW325. There, and refusing. It is the state this page's three absences are most likely to
        // be confused with and the one none of them is: a control that is in the tree, that a
        // locator matches, that a screen reader announces — and that will not take input. WW318 drew
        // the same line from the other side, and a case that could not tell "not there" from "there
        // and greyed" would report a form as broken for the state it is supposed to be in.
        rows.Children.Add(new Button
        {
            Name = "refusing",
            Content = "Update",
            IsEnabled = false,
            Width = 160,
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 4, 0, 0),
        });

        // One: a pane that is collapsed. It is shut, it says so through ExpandCollapse, and what
        // is under it is absent by design until somebody opens it.
        var expander = new Expander
        {
            Name = "collapsedPane",
            Header = "Advanced",
            IsExpanded = false,
            Margin = new Thickness(0, 12, 0, 0),
            Content = new Button { Name = "insideThePane", Content = "Inside the pane", Width = 160, Height = 30 },
        };
        rows.Children.Add(expander);

        // Two: a popup that is closed. Its child is in no tree at all, and nothing about the popup
        // itself says what it is holding — which is why a miss under one reads as simply gone.
        var flyout = new Popup
        {
            Name = "closedFlyout",
            StaysOpen = false,
            PlacementTarget = expander,
            Child = new Button { Name = "insideTheFlyout", Content = "Inside the flyout", Width = 160, Height = 30 },
        };
        rows.Children.Add(flyout);

        // Three: a submenu nobody opened. The bar is in the tree, its top entry is in the tree,
        // and what hangs under that entry is not there until the shell builds it on demand.
        var menu = new Menu { Name = "absenceMenu", Margin = new Thickness(0, 12, 0, 0) };
        var file = new MenuItem { Name = "fileMenu", Header = "File" };
        var recent = new MenuItem { Name = "recentMenu", Header = "Recent" };
        recent.Items.Add(new MenuItem { Name = "recentFile", Header = "one.txt" });
        file.Items.Add(recent);
        menu.Items.Add(file);
        rows.Children.Add(menu);

        var pane = new TabItem { Name = "absencesPane", Header = "Absences", Content = rows };
        panes.Items.Add(pane);

        // Selected, for the same reason the naming pane is: a tab nobody picked is in no tree, and
        // that would be a fourth kind of absence sitting on top of the three being measured.
        panes.SelectedItem = pane;
        return pane;
    }
}

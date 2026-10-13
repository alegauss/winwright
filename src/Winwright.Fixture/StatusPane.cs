using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace Winwright.Fixture;

/// <summary>
/// An element that publishes its state as UIA ItemStatus, and a button that changes it.
/// <para>
/// WW523. Found in quickshell (QS278): each tab says Connected, Reconnecting, Disconnected or Local
/// in <c>AutomationProperties.ItemStatus</c>, the words a screen reader announces for the coloured
/// dot, and no case could read them. The button is what proves a reading follows the live value
/// rather than one taken when the window opened, and the neighbour that publishes nothing is what
/// proves an unset status reads as empty rather than as nothing observed.
/// </para>
/// </summary>
public static class StatusPane
{
    /// <summary>What the session publishes before anything is pressed.</summary>
    public const string Before = "Disconnected";

    /// <summary>And after the button.</summary>
    public const string After = "Connected";

    /// <summary>Build the pane and add it to the tab control, selected.</summary>
    /// <param name="panes">The tab control to add it to.</param>
    public static TabItem AddTo(TabControl panes)
    {
        ArgumentNullException.ThrowIfNull(panes);

        var rows = new StackPanel { Name = "statusRows", Margin = new Thickness(12) };
        rows.Children.Add(new TextBlock
        {
            Name = "statusTitle",
            Text = "Status",
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 8),
        });

        var session = new TextBlock { Name = "sessionState", Text = "prod-db", Margin = new Thickness(0, 4, 0, 4) };
        AutomationProperties.SetItemStatus(session, Before);
        rows.Children.Add(session);

        rows.Children.Add(new TextBlock { Name = "plainState", Text = "a local shell", Margin = new Thickness(0, 4, 0, 4) });

        var connect = new Button
        {
            Name = "connectButton",
            Content = "Connect",
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 8, 0, 0),
        };
        connect.Click += (_, _) => AutomationProperties.SetItemStatus(session, After);
        rows.Children.Add(connect);

        var pane = new TabItem { Name = "statusPane", Header = "Status", Content = rows };
        panes.Items.Add(pane);

        // Selected, because a tab's content is realised when it is picked and a pane nobody picked
        // is in no tree for a case to reach.
        panes.SelectedItem = pane;
        return pane;
    }
}

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Winwright.Fixture;

/// <summary>
/// A pane that exits the application the way an update does: one button starts a new copy of the
/// same executable and exits, the other only exits. WW575.
/// <para>
/// Found by Modelado (MOD31): "Update and restart" downloads the newer installer, starts it and exits,
/// and the installer starts the application again. A case could neither say the exit was the point
/// nor follow the new process, so the project drove the installed window with hand-written UI
/// Automation. The copy started again carries <c>--relaunched</c>, and its read-out says so, which is
/// what a step after the relaunch reads to prove it is looking at the new window.
/// </para>
/// </summary>
public static class RelaunchPane
{
    /// <summary>Build the pane and add it to the tab control, selected.</summary>
    /// <param name="panes">The tab control to add it to.</param>
    /// <param name="relaunched">Whether this is the copy a restart started.</param>
    public static TabItem AddTo(TabControl panes, bool relaunched)
    {
        ArgumentNullException.ThrowIfNull(panes);

        var text = relaunched ? "started again" : "first start";
        var said = new TextBlock { Name = "relaunchSaid", Text = text, Margin = new Thickness(0, 8, 0, 0) };
        System.Windows.Automation.AutomationProperties.SetName(said, text);

        var restart = new Button { Name = "updateAndRestart", Content = "Update and restart", Width = 160, HorizontalAlignment = HorizontalAlignment.Left };
        var quit = new Button { Name = "exitOnly", Content = "Exit", Width = 160, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };

        restart.Click += (_, _) =>
        {
            // The same executable and the same arguments, with the mark that says which copy this is,
            // started before this one exits: an installer starts the new copy as the old one goes.
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
            foreach (var argument in Environment.GetCommandLineArgs().Skip(1).Where(one => one != "--relaunched"))
                start.ArgumentList.Add(argument);

            start.ArgumentList.Add("--relaunched");
            Process.Start(start)?.Dispose();
            Application.Current.Shutdown();
        };

        quit.Click += (_, _) => Application.Current.Shutdown();

        var rows = new StackPanel { Name = "relaunchRows", Margin = new Thickness(12) };
        rows.Children.Add(restart);
        rows.Children.Add(quit);
        rows.Children.Add(said);

        var pane = new TabItem { Name = "relaunchPane", Header = "Relaunch", Content = rows };
        panes.Items.Add(pane);

        // Selected, because a pane nobody picked is in no tree for a case to reach.
        panes.SelectedItem = pane;
        return pane;
    }
}

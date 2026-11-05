using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Winwright.Fixture;

/// <summary>
/// A box that answers one thing the moment it is written and another a declared moment later.
/// <para>
/// WW379. Every control this fixture drew answered the instant it was asked, so nothing here could
/// produce the state a real application spends most of its time in: an act lands, the control shows
/// what was put in it, and the application then has its own say. WW366 wanted that case and could
/// not write it — the trace field it added was pinned through a reading that differs by
/// <em>projection</em> instead, a checkbox read two ways, and the timing half was asserted by
/// nothing.
/// </para>
/// <para>
/// What it does is the ordinary shape rather than an invented one: the box normalises what it was
/// given. Type <c>beta</c> and it reads <c>beta</c>; a moment later the application has upper-cased
/// it and it reads <c>BETA</c>. A commit that echoes back differently is what a form does, and it
/// is exactly the pair of instants a harness has to tell apart — the reading the act took, and the
/// reading the verdict turned on.
/// </para>
/// <para>
/// Late by a declared number of milliseconds and never by a guess. <c>SlowMachineTests</c> already
/// excuses checks for a desk that was slow, which is slowness by accident; this is the same thing
/// asked for, which is what makes a case about it repeatable.
/// </para>
/// <para>
/// Behind its own flag rather than always on. A box that rewrites itself would be a box every
/// unrelated case reading this window has to wait for, and a fixture whose every shape is hard is a
/// fixture nothing simple can be proved against.
/// </para>
/// </summary>
public static class SettlesPane
{
    /// <summary>The box a case acts on, which is what a locator addresses it by.</summary>
    public const string BoxId = "settlesBox";

    /// <summary>What it holds before anything has been written to it.</summary>
    public const string Waiting = "waiting";

    /// <summary>Build the pane and add it to the tab control, selected.</summary>
    /// <param name="panes">The tab control to add it to.</param>
    /// <param name="milliseconds">How long the application takes to have its own say.</param>
    public static TabItem AddTo(TabControl panes, int milliseconds)
    {
        ArgumentNullException.ThrowIfNull(panes);

        var rows = new StackPanel { Name = "settlesRows", Margin = new Thickness(12) };
        rows.Children.Add(new TextBlock
        {
            Name = "settlesTitle",
            Text = "Settles",
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 8),
        });

        var box = new TextBox { Name = BoxId, Text = Waiting, Width = 200 };

        // Restarted rather than one timer per change: a caller that writes twice quickly means the
        // second write, and a timer left running from the first would rewrite what it settled on.
        var settling = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };

        settling.Tick += (_, _) =>
        {
            settling.Stop();
            box.Text = box.Text.ToUpperInvariant();
        };

        box.TextChanged += (_, _) =>
        {
            // Its own write is the one thing this must not answer, or the box rewrites itself
            // forever and the reading never settles at all.
            if (string.Equals(box.Text, box.Text.ToUpperInvariant(), StringComparison.Ordinal))
                return;

            settling.Stop();
            settling.Start();
        };

        rows.Children.Add(Labelled("Settles late", box));

        var pane = new TabItem { Name = "settlesPane", Header = "Settles", Content = rows };
        panes.Items.Add(pane);

        // Selected, for the reason every other pane here is: a tab's content is realised when it is
        // picked, and a pane nobody picked is in no tree for a locator to reach.
        panes.SelectedItem = pane;
        return pane;
    }

    /// <summary>One row: a caption and the control it is about, laid out the way the others are.</summary>
    /// <param name="says">What the caption reads.</param>
    /// <param name="control">The control beside it.</param>
    private static StackPanel Labelled(string says, UIElement control)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
        row.Children.Add(new TextBlock { Text = says, Width = 140, VerticalAlignment = VerticalAlignment.Center });
        row.Children.Add(control);
        return row;
    }
}

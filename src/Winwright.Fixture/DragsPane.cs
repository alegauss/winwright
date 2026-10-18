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
    /// <summary>The title a case finds the second window by, from the desktop. WW530.</summary>
    public const string DropWindowTitle = "winwright drop target";

    private static Window? dropWindow;

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
        ClosedByTheMiddleButton(strip, order);

        // WW530. A note dragged out of this window onto another one, and the read-out the drop is
        // reported in, here, where the case's next step can read it. Beside the title and above the
        // strip, because the pane's lower edge sits under the window's own button row.
        var note = new TextBlock
        {
            Name = "dragNote",
            Text = "A note",
            Padding = new Thickness(8),
            Margin = new Thickness(16, 0, 0, 0),
            Background = Brushes.LightYellow,
        };

        var dropped = new TextBlock
        {
            Name = "dropSaid",
            Text = "nothing dropped",
            Margin = new Thickness(16, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        System.Windows.Automation.AutomationProperties.SetName(dropped, dropped.Text);

        var header = (TextBlock)rows.Children[0];
        rows.Children.RemoveAt(0);
        var top = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        header.Margin = new Thickness(0);
        header.VerticalAlignment = VerticalAlignment.Center;
        top.Children.Add(header);
        top.Children.Add(note);
        top.Children.Add(dropped);
        rows.Children.Insert(0, top);

        Draggable(note);
        rows.Loaded += (_, _) => DropWindowBeside(Window.GetWindow(rows), dropped);

        // WW544. A menu bar hidden until Alt is tapped alone, the Windows way, as quickshell's is
        // (QS354). Above the strip, so showing it pushes nothing a drag case aims at out of place.
        var bar = new Menu { Name = "altMenu", Visibility = Visibility.Collapsed };
        bar.Items.Add(new MenuItem { Name = "altFile", Header = "File" });
        rows.Children.Insert(0, bar);
        rows.Loaded += (_, _) => ShownByAlt(Window.GetWindow(rows), bar);

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

    /// <summary>
    /// Show or hide <paramref name="bar"/> when Alt goes down and up with no other key between, which
    /// is what Windows calls tapping Alt. WW544. Alt released after another key is not a tap, so
    /// <c>Alt+Escape</c> leaves the bar as it was — the control a case needs to tell the gesture from
    /// any key that happens to involve Alt.
    /// </summary>
    private static void ShownByAlt(Window? window, Menu bar)
    {
        if (window is null)
            return;

        var tapping = false;
        window.PreviewKeyDown += (_, e) =>
        {
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            tapping = key is Key.LeftAlt or Key.RightAlt;
        };

        window.PreviewKeyUp += (_, e) =>
        {
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (!tapping || key is not (Key.LeftAlt or Key.RightAlt))
                return;

            tapping = false;
            bar.Visibility = bar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            e.Handled = true;
        };
    }

    /// <summary>
    /// A middle click on a tab closes it, as it does in every browser and terminal on Windows. WW538.
    /// <para>
    /// Found by quickshell's QS310: its tab strip closes a tab on the middle button, and a case could
    /// press only the left one. A left click selects and closes nothing, which is the control.
    /// </para>
    /// </summary>
    private static void ClosedByTheMiddleButton(TabControl strip, TextBlock order)
    {
        strip.PreviewMouseDown += (_, e) =>
        {
            if (e.ChangedButton != MouseButton.Middle || Under(strip, e.GetPosition(strip)) is not { } tab)
                return;

            strip.Items.Remove(tab);
            Said(order, strip);
            e.Handled = true;
        };
    }

    /// <summary>
    /// A note that starts a real drag-and-drop once the pointer has gone past the threshold with the
    /// button down, the way a file browser offers a file to Explorer. WW530.
    /// <para>
    /// Through <see cref="DragDrop.DoDragDrop"/>, so what carries the note is the system's own drag
    /// loop, and the release is seen by whichever window is under the pointer rather than by this one.
    /// </para>
    /// </summary>
    private static void Draggable(TextBlock note)
    {
        var from = default(Point?);

        // Captured on the press, as a file list does, so the move that crosses the threshold reaches
        // the note wherever the pointer is by then.
        note.PreviewMouseLeftButtonDown += (_, e) =>
        {
            from = e.GetPosition(note);
            note.CaptureMouse();
        };

        note.PreviewMouseLeftButtonUp += (_, _) =>
        {
            from = null;
            note.ReleaseMouseCapture();
        };

        note.PreviewMouseMove += (_, e) =>
        {
            if (from is not { } pressed || e.LeftButton != MouseButtonState.Pressed)
                return;

            var moved = e.GetPosition(note) - pressed;
            if (Math.Abs(moved.X) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(moved.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            // Handed back before the system's loop starts, which takes the pointer itself.
            from = null;
            note.ReleaseMouseCapture();
            DragDrop.DoDragDrop(note, Carried(note.Text), DragDropEffects.Copy);
        };
    }

    /// <summary>The variable naming a file the note also carries, the way a file browser offers one. WW536.</summary>
    public const string CarriesFile = "WINWRIGHT_DRAG_FILE";

    /// <summary>
    /// What the note's drag carries: its text, and the file <see cref="CarriesFile"/> names where it
    /// names one that is there. WW536.
    /// <para>
    /// The file is what an Explorer folder takes: a drop of a file list there is a copy into the
    /// folder, which is quickshell's QS188 gesture, and a case can then read the copy with a file
    /// step. The text stays beside it, so the drop window this pane raises still takes the same drag.
    /// </para>
    /// </summary>
    private static DataObject Carried(string text)
    {
        var data = new DataObject(DataFormats.UnicodeText, text);
        if (Environment.GetEnvironmentVariable(CarriesFile) is { Length: > 0 } path && System.IO.File.Exists(path))
            data.SetFileDropList([path]);

        return data;
    }

    /// <summary>
    /// A second top-level window that takes a drop, raised once to the right of the window under
    /// test. WW530.
    /// <para>
    /// Found by quickshell's QS188: a file dragged out of its server pane is dropped onto an Explorer
    /// folder, which is another process's window, and a drag whose destination could only be looked
    /// for in the window it started in had no case. Explorer is not something this fixture can count
    /// on, so this stands in for it: a separate top-level window, found from the desktop by its title.
    /// </para>
    /// <para>
    /// Smaller than the main window, because the launcher takes the largest window a process draws
    /// as the one under test. Not owned and never activated, so it neither joins the main window's
    /// tree nor takes the desk a case needs. What it took is written into the main window's read-out,
    /// where the step after the drag can read it.
    /// </para>
    /// </summary>
    /// <param name="beside">The window under test, or null where it is not known yet.</param>
    /// <param name="said">The read-out in the window under test that a drop is reported in.</param>
    private static void DropWindowBeside(Window? beside, TextBlock said)
    {
        if (dropWindow is not null || beside is null)
            return;

        var zone = new TextBlock
        {
            Name = "dropZone",
            Text = "Drop here",
            Padding = new Thickness(16),
            Background = Brushes.Lavender,
            AllowDrop = true,
        };

        zone.DragOver += (_, e) =>
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.UnicodeText) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        };

        zone.Drop += (_, e) =>
        {
            if (e.Data.GetData(DataFormats.UnicodeText) is not string text)
                return;

            said.Text = $"dropped: {text}";
            System.Windows.Automation.AutomationProperties.SetName(said, said.Text);
            e.Handled = true;
        };

        var shown = new Window
        {
            Title = DropWindowTitle,
            Width = 260,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = beside.Left + beside.ActualWidth + 20,
            Top = beside.Top,
            ShowInTaskbar = false,
            ShowActivated = false,
            Content = zone,
        };

        // Said as its automation name as well, and measured: left to itself the window announced its
        // content's text, 'Drop here', and the locator naming it by its title found nothing.
        System.Windows.Automation.AutomationProperties.SetName(shown, DropWindowTitle);

        // Unowned, so the application's default shutdown would wait for this one too: closed with the
        // window under test, or closing that window would leave a process behind it.
        beside.Closed += (_, _) => shown.Close();

        dropWindow = shown;
        shown.Show();
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

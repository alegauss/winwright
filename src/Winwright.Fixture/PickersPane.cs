using System.Windows;
using System.Windows.Controls;

namespace Winwright.Fixture;

/// <summary>
/// Two pickers on one page, because the question *what does this picker hold* has two answers on
/// Windows and this suite only ever asked one of them.
/// <para>
/// WW265. <c>Pick</c> shipped in block D and every route was driven against a Win32 <c>ComboBox</c>
/// built with <c>CB_ADDSTRING</c>, which holds its items in the tree whether or not it is dropped
/// down. A WPF <c>ComboBox</c> realises its items when its popup opens and has none before that —
/// so the walk read <em>it holds nothing</em> about a picker holding two, and refused the one case
/// it had been built to unblock. Measured on claude-tray's profile picker: nought shut, two open,
/// nought again once collapsed.
/// </para>
/// <para>
/// The Win32 one is not replaced by this and must not be: the two frameworks answer differently and
/// a fixture carrying only the second would close this hole by opening its mirror image. What was
/// missing is that no WPF picker existed anywhere in the suite, so the difference had nowhere to
/// show.
/// </para>
/// <para>
/// Five items and not two. The keyboard walk anchors at whichever end of the list is nearer, and a
/// picker of two makes both ends the same place — so a walk that normalised to the top would look
/// correct on it, which is the defect the anchoring exists to prevent being invisible.
/// </para>
/// </summary>
public static class PickersPane
{
    /// <summary>What the pickers hold, in the order they hold them.</summary>
    public static IReadOnlyList<string> Values { get; } = ["Alpha", "Bravo", "Charlie", "Delta", "Echo"];

    /// <summary>Build the pane and add it to the tab control, selected.</summary>
    /// <param name="panes">The tab control to add it to.</param>
    public static TabItem AddTo(TabControl panes)
    {
        ArgumentNullException.ThrowIfNull(panes);

        var rows = new StackPanel { Name = "pickerRows", Margin = new Thickness(12) };
        rows.Children.Add(new TextBlock
        {
            Name = "pickersTitle",
            Text = "Pickers",
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 8),
        });

        // The one this pane exists for: shut, so its items are in no tree until something opens it.
        rows.Children.Add(Labelled("Shut", Picker("shutPicker", 0)));

        // The same control already dropped down, so a walk that reads a picker holding its items is
        // still driven here. Without it, fixing the first would move the untested case rather than
        // remove it — and which of the two a run met would decide whether the open path ever ran.
        var open = Picker("openPicker", 2);
        open.Loaded += (_, _) => open.IsDropDownOpen = true;
        rows.Children.Add(Labelled("Open", open));

        var pane = new TabItem { Name = "pickersPane", Header = "Pickers", Content = rows };
        panes.Items.Add(pane);

        // Selected, because a tab's content is realised when it is picked and a pane nobody picked is
        // in no tree for a locator to reach.
        panes.SelectedItem = pane;
        return pane;
    }

    /// <summary>
    /// One picker, holding the same five values and starting on the one it is told to.
    /// <para>
    /// A different starting index per picker, so the anchoring is exercised in both directions
    /// without a case having to move one first: from the top, index 0 is one end and index 4 is one
    /// change from the other.
    /// </para>
    /// </summary>
    /// <param name="named">Its automation id.</param>
    /// <param name="selected">Which of <see cref="Values"/> it opens on.</param>
    private static ComboBox Picker(string named, int selected)
    {
        var picker = new ComboBox
        {
            Name = named,
            Width = 200,
            HorizontalAlignment = HorizontalAlignment.Left,
            SelectedIndex = selected,
        };

        foreach (var value in Values)
            picker.Items.Add(new ComboBoxItem { Content = value });

        return picker;
    }

    /// <summary>
    /// A caption beside the control, named so a locator can reach either.
    /// <para>
    /// The caption is its own TextBlock rather than the picker's content, which is the shape that
    /// leaves a WPF control with no name of its own — the same one <c>NamesPane</c> is about, met
    /// here because it is how a settings row is actually built.
    /// </para>
    /// </summary>
    /// <param name="caption">What the row says.</param>
    /// <param name="control">The picker it labels.</param>
    private static StackPanel Labelled(string caption, ComboBox control)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
        row.Children.Add(new TextBlock
        {
            Text = caption,
            Width = 140,
            VerticalAlignment = VerticalAlignment.Center,
        });

        System.Windows.Automation.AutomationProperties.SetName(control, caption);
        row.Children.Add(control);
        return row;
    }
}

using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace Winwright.Fixture;

/// <summary>
/// Four settings rows, three of them named correctly and one wearing the row next door's label.
/// <para>
/// WW264. A sweep proves a control announces something. It cannot prove the something is its own
/// row's header, because it has no idea which row the control is in — and the failure hiding there
/// is worse than the one it catches: a rule that pairs the wrong two things gives several controls
/// one name, and a screen reader then reads the same label over three different controls. Every
/// check that asks whether a name exists passes that.
/// </para>
/// <para>
/// The rows are <c>GroupBox</c>es and not the <c>StackPanel</c>s a real page usually nests, and that
/// is the point rather than a convenience: a WPF panel has no automation peer, so a row built from
/// one is in no tree and its header is readable by nothing. What makes the claim askable at all is a
/// row with a peer of its own — which is the shape the application under test had to grow before its
/// own harness could check this.
/// </para>
/// <para>
/// Both branches of the rule are here, because a pane carrying only the first would be a pane the
/// easy half of the check passes. The one that must fire: a control with no content of its own takes
/// its row's header. The one that must <em>not</em>: a button beside a field keeps its own text.
/// </para>
/// </summary>
public static class RowsPane
{
    /// <summary>The header of the row whose slider wears the wrong one, so a case can name it.</summary>
    public const string Borrowed = "Language";

    /// <summary>What a run may ask this pane to be.</summary>
    public static IReadOnlyList<string> Names { get; } = ["paired", "borrowed"];

    /// <summary>
    /// Build the pane and add it to the tab control, selected.
    /// <para>
    /// The defect is asked for rather than always drawn, because a pane that always carries it is a
    /// pane nothing can prove the correct answer against: a claim that fails on every row set is
    /// indistinguishable from one that fails on everything.
    /// </para>
    /// </summary>
    /// <param name="panes">The tab control to add it to.</param>
    /// <param name="how">`paired` for a page that gets it right, `borrowed` for the defect.</param>
    public static TabItem AddTo(TabControl panes, string how)
    {
        ArgumentNullException.ThrowIfNull(panes);
        ArgumentException.ThrowIfNullOrWhiteSpace(how);

        var borrowed = string.Equals(how, "borrowed", StringComparison.OrdinalIgnoreCase);

        var rows = new StackPanel { Name = "settingsRows", Margin = new Thickness(12) };
        rows.Children.Add(new TextBlock
        {
            Name = "rowsTitle",
            Text = "Rows",
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 8),
        });

        // The branch that must fire: a picker with no content of its own, announcing its row.
        rows.Children.Add(Row("languageRow", Borrowed, Named(new ComboBox { Name = "languagePicker", Width = 160 }, Borrowed)));

        // The other branch, and the only one that can produce the duplicate: the field takes the
        // header and the button beside it keeps its own text.
        var directory = new StackPanel { Orientation = Orientation.Horizontal };
        directory.Children.Add(Named(new TextBox { Name = "directoryBox", Width = 160 }, "Working directory"));
        directory.Children.Add(Named(new Button { Name = "browseButton", Content = "Browse…", Margin = new Thickness(8, 0, 0, 0) }, "Browse…"));
        rows.Children.Add(Row("directoryRow", "Working directory", directory));

        // A row whose control says nothing at all. Not this claim's business — that is `eachSpoken`'s
        // — and it is here so the two are not quietly the same check.
        rows.Children.Add(Row("silentRow", "Startup", new CheckBox { Name = "startupCheck" }));

        // The defect: a slider in the interval row announcing the language row's header. Every name
        // here is non-empty, every one is a real label somebody wrote, and one of them is on the
        // wrong control.
        rows.Children.Add(Row(
            "intervalRow",
            "Refresh interval",
            Named(new Slider { Name = "intervalSlider", Width = 160 }, borrowed ? Borrowed : "Refresh interval")));

        var pane = new TabItem { Name = "rowsPane", Header = "Rows", Content = rows };
        panes.Items.Add(pane);

        // Selected, because a tab's content is realised when it is picked and a pane nobody picked
        // is in no tree for the check to read.
        panes.SelectedItem = pane;
        return pane;
    }

    /// <summary>
    /// One row: a container with an automation peer, carrying its header as its own name.
    /// <para>
    /// The header is the group's name rather than a label beside it, because that is what makes the
    /// pairing readable — a check can ask the row what it is called and ask what is inside it, which
    /// is the whole of what this claim needs and exactly what a bare panel cannot answer.
    /// </para>
    /// </summary>
    /// <param name="named">Its automation id.</param>
    /// <param name="header">What the row is called.</param>
    /// <param name="content">What sits in it.</param>
    private static GroupBox Row(string named, string header, UIElement content)
    {
        var row = new GroupBox { Name = named, Header = header, Margin = new Thickness(0, 4, 0, 4), Content = content };
        AutomationProperties.SetName(row, header);
        return row;
    }

    /// <summary>A control announcing a name somebody chose, which is what the rule under test does.</summary>
    /// <param name="control">The control.</param>
    /// <param name="announcing">What it should say.</param>
    private static T Named<T>(T control, string announcing)
        where T : UIElement
    {
        AutomationProperties.SetName(control, announcing);
        return control;
    }
}

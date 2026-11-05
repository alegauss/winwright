using System.Windows;
using System.Windows.Controls;

namespace Winwright.Fixture;

/// <summary>
/// A toolbar as wide as the window, holding more buttons than a narrow window has room for. WW571.
/// <para>
/// Found by PhotoBoost (PB65): on the guest, whose window was narrower than the host's, the editor's
/// toolbar moved its last buttons into its overflow, and a step pressing one was refused as nothing
/// matched. A WPF toolbar keeps what overflowed in a closed popup, outside the tree until the overflow
/// button opens it, so the refusal read as a misspelled id. The toolbar here is docked across the top
/// of its pane, so a case decides by sizing the window whether its last button is on the bar or in
/// the overflow.
/// </para>
/// </summary>
public static class ToolbarPane
{
    /// <summary>The buttons, by name and caption, the last of them the one a narrow window loses first.</summary>
    private static readonly (string Name, string Caption)[] Buttons =
    [
        ("openImage", "Open image"),
        ("saveImage", "Save image"),
        ("cropImage", "Crop"),
        ("resizeImage", "Resize"),
        ("sharpenImage", "Sharpen"),
        ("rotateLeft", "Rotate left"),
        ("rotateRight", "Rotate right"),
    ];

    /// <summary>Build the pane and add it to the tab control, selected.</summary>
    /// <param name="panes">The tab control to add it to.</param>
    public static TabItem AddTo(TabControl panes)
    {
        ArgumentNullException.ThrowIfNull(panes);

        var said = new TextBlock { Name = "toolbarSaid", Text = "nothing pressed", Margin = new Thickness(12) };
        Said(said, said.Text);

        var bar = new ToolBar { Name = "editorToolbar" };
        foreach (var (name, caption) in Buttons)
        {
            var button = new Button { Name = name, Content = caption, Margin = new Thickness(4, 0, 4, 0) };
            button.Click += (_, _) => Said(said, $"pressed: {caption}");
            bar.Items.Add(button);
        }

        var page = new DockPanel { Name = "toolbarPage", LastChildFill = true };
        DockPanel.SetDock(bar, Dock.Top);
        page.Children.Add(bar);
        page.Children.Add(said);

        var pane = new TabItem { Name = "toolbarPane", Header = "Toolbar", Content = page };
        panes.Items.Add(pane);

        // Selected, because a pane nobody picked is in no tree for a case to reach.
        panes.SelectedItem = pane;
        return pane;
    }

    /// <summary>Write the read-out, as its text and as its name.</summary>
    private static void Said(TextBlock said, string text)
    {
        said.Text = text;
        System.Windows.Automation.AutomationProperties.SetName(said, text);
    }
}

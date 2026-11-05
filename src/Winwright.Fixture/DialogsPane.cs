using System.Windows;
using System.Windows.Controls;

namespace Winwright.Fixture;

/// <summary>
/// A pane whose two buttons open the system's common Open and Save dialogs, owned by this window.
/// <para>
/// WW551. Found by PhotoBoost (PB23 and PB25): opening an image and exporting it are the first and
/// last steps of a document application, both go through the common dialog, and a case could reach
/// neither — so the application grew <c>--open</c> and <c>--export</c> flags only its tests used, and
/// the dialog code was never run by a case.
/// </para>
/// <para>
/// The dialogs are the shell's own, through <see cref="Microsoft.Win32.OpenFileDialog"/> and
/// <see cref="Microsoft.Win32.SaveFileDialog"/>, so the tree a case walks is the one an adopter's
/// application shows. What was chosen is said in a read-out, and a save writes a file whose text names
/// the type chosen, so a case can check both the path and the filter.
/// </para>
/// </summary>
public static class DialogsPane
{
    /// <summary>The types the save dialog offers, in order; the file a save writes says which was chosen.</summary>
    public const string SaveTypes = "PNG image|*.png|JPEG image|*.jpg|WebP image|*.webp";

    /// <summary>The name the save dialog offers before a case gives one.</summary>
    public const string DefaultSaveName = "untitled.png";

    /// <summary>Build the pane and add it to the tab control, selected.</summary>
    /// <param name="panes">The tab control to add it to.</param>
    public static TabItem AddTo(TabControl panes)
    {
        ArgumentNullException.ThrowIfNull(panes);

        var said = new TextBlock { Name = "dialogSaid", Text = "nothing chosen", Margin = new Thickness(0, 8, 0, 0) };
        Said(said, said.Text);

        var open = new Button { Name = "openFile", Content = "Open…", Width = 120, HorizontalAlignment = HorizontalAlignment.Left };
        var save = new Button { Name = "saveFile", Content = "Save…", Width = 120, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };

        open.Click += (_, _) =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Title = "Open an image", Filter = "Images|*.png;*.jpg;*.webp|All files|*.*" };
            Said(said, dialog.ShowDialog(Window.GetWindow(open)) == true ? $"opened: {dialog.FileName}" : "open cancelled");
        };

        save.Click += (_, _) =>
        {
            // WW561. A default name, the way an application exporting a document offers one: the
            // dialog puts it in the name box itself as it starts, which is what overwrote a path a
            // case had already written there.
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export the image",
                Filter = SaveTypes,
                AddExtension = true,
                FileName = DefaultSaveName,
            };
            if (dialog.ShowDialog(Window.GetWindow(save)) != true)
            {
                Said(said, "save cancelled");
                return;
            }

            // The type as the filter names it, so a case reads back which one the dialog settled on
            // and not only the path it was typed.
            var type = SaveTypes.Split('|')[(dialog.FilterIndex - 1) * 2];
            System.IO.File.WriteAllText(dialog.FileName, type);
            Said(said, $"saved: {System.IO.Path.GetFileName(dialog.FileName)} as {type}");
        };

        var rows = new StackPanel { Name = "dialogRows", Margin = new Thickness(12) };
        rows.Children.Add(open);
        rows.Children.Add(save);
        rows.Children.Add(said);

        var pane = new TabItem { Name = "dialogsPane", Header = "Dialogs", Content = rows };
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

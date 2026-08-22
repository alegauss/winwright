using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Winwright.Fixture;

/// <summary>
/// One page holding the whole naming rule.
/// <para>
/// A control with no name, one announcing a glyph codepoint, one whose label is a neighbouring
/// element, one handing back its own automation id — and beside them a button that must keep its
/// own text. Both branches of the rule on one surface.
/// </para>
/// <para>
/// That is the case set the naming check needs, and assembling it out of a real product means
/// waiting for that product to happen to have all of them at once. Two of them were found in one
/// shipping window and the rest were reasoned about, which is the wrong way round.
/// </para>
/// </summary>
public static class NamesPane
{
    /// <summary>
    /// A private-use codepoint from the icon font. Every icon in Segoe MDL2 and Fluent lives from
    /// U+E000 upwards, so a button whose content is one announces exactly this and a screen reader
    /// says nothing at all — while every check for a non-empty name passes.
    /// </summary>
    public const string Glyph = "";

    /// <summary>Build the pane and add it to the tab control, selected.</summary>
    /// <param name="panes">The tab control to add it to.</param>
    public static TabItem AddTo(TabControl panes)
    {
        ArgumentNullException.ThrowIfNull(panes);

        var rows = new StackPanel { Name = "nameRows", Margin = new Thickness(12) };
        rows.Children.Add(new TextBlock { Name = "namesTitle", Text = "Names", FontSize = 16, Margin = new Thickness(0, 0, 0, 8) });

        // Nothing at all. A control takes its name from its own content, and this one has none.
        rows.Children.Add(Sized(new Button { Name = "unnamed" }));

        // A glyph. Non-empty, drawable, and silent to everything that is not looking at it.
        rows.Children.Add(Sized(new Button
        {
            Name = "glyphOnly",
            Content = Glyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
        }));

        // Its own automation id handed back, which is a developer's name and not a person's.
        rows.Children.Add(Sized(new Button { Name = "echoesTheId", Content = "echoesTheId" }));

        // A label that is a separate element beside the box, which is how one real window came to
        // ship two controls reading as unnamed while every neighbouring button read fine.
        var labelled = new StackPanel { Name = "labelledRow", Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
        labelled.Children.Add(new TextBlock { Name = "profileLabel", Text = "Profile", Margin = new Thickness(0, 4, 8, 0) });
        labelled.Children.Add(new TextBox { Name = "profileBox", Width = 160 });
        rows.Children.Add(labelled);

        // The other branch, and it must not fire: a button that says what it does.
        rows.Children.Add(Sized(new Button { Name = "spoken", Content = "Save changes" }));

        var pane = new TabItem { Name = "namesPane", Header = "Names", Content = rows };
        panes.Items.Add(pane);

        // Selected, because a tab's content is realised when it is picked and a pane nobody picked
        // is in no tree for the check to read.
        panes.SelectedItem = pane;
        return pane;
    }

    private static Button Sized(Button button)
    {
        button.Width = 160;
        button.Height = 30;
        button.HorizontalAlignment = HorizontalAlignment.Left;
        button.Margin = new Thickness(0, 4, 0, 0);
        return button;
    }
}

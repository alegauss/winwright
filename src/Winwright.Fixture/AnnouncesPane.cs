using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace Winwright.Fixture;

/// <summary>
/// One page of entries that carry their own state in their text and say it in a sentence beside
/// their name.
/// <para>
/// WW83. It is the shape claude-tray's Profile submenu has, drawn without an account. Each row is
/// named <c>&lt;label&gt; — &lt;reading&gt;</c> with a suffix that comes and goes, so nothing can
/// address one by its whole name; and each announces a help sentence whose first word is the state
/// it is in, because the framework's own accessible object had to be replaced to carry the sentence
/// at all and the replacement costs the toggle pattern.
/// </para>
/// <para>
/// Both halves of that are what the two new fields exist for, and both were reasoned about rather
/// than driven until this pane. The last row is why the state has to be matched as a prefix and
/// never as a containment: its sentence explains what turning it <em>off</em> would do, and it is
/// off.
/// </para>
/// </summary>
public static class AnnouncesPane
{
    /// <summary>What a row announces in front of its sentence while it carries the mark.</summary>
    public const string Checked = "Checked";

    /// <summary>And what one announces while it does not.</summary>
    public const string Unchecked = "Not checked";

    /// <summary>Build the pane and add it to the tab control, selected.</summary>
    /// <param name="panes">The tab control to add it to.</param>
    public static TabItem AddTo(TabControl panes)
    {
        ArgumentNullException.ThrowIfNull(panes);

        var rows = new StackPanel { Name = "announceRows", Margin = new Thickness(12) };
        rows.Children.Add(new TextBlock
        {
            Name = "announcesTitle",
            Text = "Announces",
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 8),
        });

        // The one carrying the mark. Its label is "Pessoal" and its name is not: the reading behind
        // the dash is what the account happens to have consumed, and the suffix is there while it is
        // the one being followed.
        rows.Children.Add(Row(
            "pessoalRow",
            "Pessoal — used 41%  · active now",
            Checked + " · C:\\Users\\someone\\.claude · the tray only"));

        // The other one. Same shape, no mark, and a label that begins with the first one's would be
        // a containment match on the row above — which is the reason the locator matches a prefix.
        rows.Children.Add(Row(
            "workRow",
            "Work — used 8%",
            Unchecked + " · C:\\Users\\someone\\.claude-work · the tray only"));

        // The trap. It is off, and its own explanation contains the word the checked state announces,
        // so anything matching the state inside the sentence rather than at the front reports it on.
        rows.Children.Add(Row(
            "followRow",
            "Follow the active profile",
            Unchecked + " · Checked, the icon moves on its own to whichever profile just had a turn"));

        // And one that announces nothing at all, so "this row says something beside its name" stays a
        // claim that can be false.
        rows.Children.Add(Row("silentRow", "Quit", said: null));

        var pane = new TabItem { Name = "announcesPane", Header = "Announces", Content = rows };
        panes.Items.Add(pane);

        // Selected, because a tab's content is realised when it is picked and a pane nobody picked
        // is in no tree for a case to read.
        panes.SelectedItem = pane;
        return pane;
    }

    private static Button Row(string id, string text, string? said)
    {
        var row = new Button
        {
            Name = id,
            Content = text,
            Width = 320,
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 4, 0, 0),
        };

        if (said is not null)
            AutomationProperties.SetHelpText(row, said);

        return row;
    }
}

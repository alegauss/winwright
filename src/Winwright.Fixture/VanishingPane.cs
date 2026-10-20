using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Winwright.Fixture;

/// <summary>
/// Two buttons a locator finds and cannot read. WW557.
/// <para>
/// Found shipping WW555. photo-boost's select right after expand was refused as "nothing matched":
/// a look matched a list item whose peer was gone before its facts were read, and the act gave up on
/// that look instead of polling on. The guest's own picker realises its items before the first look,
/// so the race never ran here and the fix could have been undone with every case still green.
/// </para>
/// <para>
/// So the race is built rather than waited for. Each button's peer answers the automation id a search
/// matches on and refuses its name as an element that is no longer available, which is exactly what a
/// replaced peer says to a client. One stops refusing a moment after it is first asked; the other
/// never does.
/// </para>
/// </summary>
public static class VanishingPane
{
    /// <summary>How long the first button refuses, from the first time anything asks its name.</summary>
    public static TimeSpan Settles { get; } = TimeSpan.FromMilliseconds(300);

    /// <summary>Build the pane and add it to the tab control, selected.</summary>
    /// <param name="panes">The tab control to add it to.</param>
    public static TabItem AddTo(TabControl panes)
    {
        ArgumentNullException.ThrowIfNull(panes);

        var rows = new StackPanel { Name = "vanishingRows", Margin = new Thickness(12) };
        rows.Children.Add(new TextBlock
        {
            Name = "vanishingTitle",
            Text = "Vanishing",
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 8),
        });

        var pressed = new TextBlock { Name = "vanishingPressed", Text = "not pressed" };

        // The one that settles: gone for a moment after it is first asked, the way an item is while a
        // popup realises it, and then an ordinary button that says it was pressed.
        var settles = Row("vanishingOnce", "Settles", Settles);
        settles.Click += (_, _) => pressed.Text = "pressed";
        rows.Children.Add(settles);

        // The one that never settles, so the refusal has to say a match was there and gone each time
        // rather than that nothing matched.
        rows.Children.Add(Row("vanishingAlways", "Never settles", Timeout.InfiniteTimeSpan));
        rows.Children.Add(pressed);

        var pane = new TabItem { Name = "vanishingPane", Header = "Vanishing", Content = rows };
        panes.Items.Add(pane);

        // Selected, because a tab's content is realised when it is picked and a pane nobody picked
        // is in no tree for a case to reach.
        panes.SelectedItem = pane;
        return pane;
    }

    private static Vanishing Row(string id, string text, TimeSpan gone) => new(gone)
    {
        Name = id,
        Content = text,
        Width = 200,
        Height = 30,
        HorizontalAlignment = HorizontalAlignment.Left,
        Margin = new Thickness(0, 4, 0, 0),
    };

    /// <summary>A button whose peer refuses its name for a while after it is first asked.</summary>
    private sealed class Vanishing(TimeSpan gone) : Button
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this, gone);

        private sealed class Peer(Button owner, TimeSpan gone) : ButtonAutomationPeer(owner)
        {
            private Stopwatch? asked;

            protected override string GetNameCore()
            {
                asked ??= Stopwatch.StartNew();
                if (gone == Timeout.InfiniteTimeSpan || asked.Elapsed < gone)
                    throw new ElementNotAvailableException("this element was replaced before it could be read");

                return base.GetNameCore();
            }
        }
    }
}

using System.Windows;
using System.Windows.Controls;

namespace Winwright.Fixture;

/// <summary>
/// Three range controls on one page, because a key pressed at one has three answers.
/// <para>
/// WW226. <c>Traversal.Nudge</c> shipped in block D and nothing here drove it: no pane drew a
/// Slider, a ScrollBar or anything else offering RangeValue. So the branch that decides which
/// direction to press — at the maximum a press upward is a legitimate no-op, and pressing it would
/// test the starting value rather than the control — had never run against a real control.
/// </para>
/// <para>
/// One control would have made that branch reachable and left it untested. The three are the three
/// answers: room either way, so a nudge moves it the way it asked; already at the maximum, so the
/// verb has to flip and a check that reads the value going <em>down</em> is the one proving it did;
/// and a range with no room at all, which is the refusal — a control nothing could nudge, and a
/// scenario about it proves nothing.
/// </para>
/// </summary>
public static class RangesPane
{
    /// <summary>What the sliders are called, in the order they are laid out.</summary>
    public static IReadOnlyList<string> Names { get; } = ["roomEitherWay", "atTheMaximum", "noRoomAtAll"];

    /// <summary>Build the pane and add it to the tab control, selected.</summary>
    /// <param name="panes">The tab control to add it to.</param>
    public static TabItem AddTo(TabControl panes)
    {
        ArgumentNullException.ThrowIfNull(panes);

        var rows = new StackPanel { Name = "rangeRows", Margin = new Thickness(12) };
        rows.Children.Add(new TextBlock
        {
            Name = "rangesTitle",
            Text = "Ranges",
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 8),
        });

        // Room in both directions, sitting in the middle. A nudge presses the way it prefers and the
        // value moves that way, which is the only one of the three where nothing had to be decided.
        rows.Children.Add(Labelled("Room either way", Ranged("roomEitherWay", 0, 10, 5)));

        // Already at the end. A press in the preferred direction does nothing at all here, so a verb
        // that did not flip would read the starting value back and call it a pass — which is the
        // unearned green this pane exists to make impossible.
        rows.Children.Add(Labelled("At the maximum", Ranged("atTheMaximum", 0, 10, 10)));

        // A range with one value in it. Nothing could move it in either direction, so the honest
        // answer is a refusal and not a nudge that reports nothing happened.
        rows.Children.Add(Labelled("No room at all", Ranged("noRoomAtAll", 3, 3, 3)));

        var pane = new TabItem { Name = "rangesPane", Header = "Ranges", Content = rows };
        panes.Items.Add(pane);

        // Selected, because a tab's content is realised when it is picked and a pane nobody picked is
        // in no tree for a locator to reach.
        panes.SelectedItem = pane;
        return pane;
    }

    /// <summary>
    /// One slider, declared by what it accepts rather than by how it looks.
    /// <para>
    /// <c>IsSnapToTickEnabled</c> with a tick of one, so a key press moves it by exactly one and the
    /// read-back is a whole number a case can write. Without it WPF's small change is a fraction and
    /// an expectation would be about how this control rounds.
    /// </para>
    /// </summary>
    private static Slider Ranged(string named, double minimum, double maximum, double value) => new()
    {
        Name = named,
        Minimum = minimum,
        Maximum = maximum,
        Value = value,
        Width = 160,
        TickFrequency = 1,
        IsSnapToTickEnabled = true,
    };

    /// <summary>
    /// A slider with its label beside it, which is how a real settings row is built — and the shape
    /// that taught this fixture a control takes its name from its own content and a slider has none.
    /// </summary>
    private static StackPanel Labelled(string label, Slider slider)
    {
        var row = new StackPanel
        {
            Name = $"{slider.Name}Row",
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 8, 0, 0),
        };

        row.Children.Add(new TextBlock
        {
            Name = $"{slider.Name}Label",
            Text = label,
            Width = 120,
            Margin = new Thickness(0, 4, 8, 0),
        });

        row.Children.Add(slider);
        return row;
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Winwright.Fixture;

/// <summary>
/// A surface fixed by construction, so a byte-identical render has something to be identical to.
/// <para>
/// No clock, no machine name, no real data, and — the one that is easy to miss — no themed control
/// at all. A button, a tab header or a text box draws its chrome from the desktop's theme and
/// accent colour, so a pane holding one is a pane that renders differently on two desks while
/// every value on it is fixed. Everything here is a border, a rectangle or a text block with its
/// brush and its font named outright.
/// </para>
/// <para>
/// Producing one is a design constraint rather than an accident, and it doubles as the reference
/// for what an adopting project has to do before its own surfaces can be compared at all.
/// </para>
/// </summary>
public static class FixedPane
{
    /// <summary>The size it is always drawn at, so two renders cannot differ by their layout.</summary>
    public static Size Size { get; } = new(360, 200);

    /// <summary>Build it. The same call twice gives two trees that draw the same pixels.</summary>
    public static FrameworkElement Build()
    {
        var rows = new StackPanel { Name = "fixedRows" };
        rows.Children.Add(Text("fixedTitle", "winwright fixture", 20));
        rows.Children.Add(Text("fixedNote", "Fixed by construction: no clock, no machine, no theme.", 13));
        rows.Children.Add(new Border
        {
            Name = "fixedSwatch",
            Height = 60,
            Margin = new Thickness(0, 12, 0, 0),
            Background = new SolidColorBrush(Color.FromRgb(0x1f, 0x6f, 0xeb)),
        });

        return new Border
        {
            Name = "fixedPane",
            Width = Size.Width,
            Height = Size.Height,
            Padding = new Thickness(20),
            Background = new SolidColorBrush(Color.FromRgb(0xf6, 0xf8, 0xfa)),
            Child = rows,
        };
    }

    /// <summary>
    /// A text block with every drawing decision named. The font is one Windows ships in every
    /// edition: a family the machine does not have falls back to whatever it does have, which is
    /// the same variance by a longer road.
    /// </summary>
    private static TextBlock Text(string name, string said, double size)
    {
        var block = new TextBlock
        {
            Name = name,
            Text = said,
            FontSize = size,
            FontFamily = new FontFamily("Segoe UI"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x1f, 0x23, 0x28)),
            Margin = new Thickness(0, 0, 0, 4),
        };

        // Display and not Ideal: ideal formatting positions glyphs in fractional pixels, which is
        // right for a person reading and wrong for two renders that must be byte-identical.
        TextOptions.SetTextFormattingMode(block, TextFormattingMode.Display);
        return block;
    }
}

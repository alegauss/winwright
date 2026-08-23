using System.Windows;
using System.Windows.Controls;

namespace Winwright.Fixture;

/// <summary>
/// A page that lays out at exactly the right size and paints none of it.
/// <para>
/// WW146. The blank check scans a picture's alpha channel for any pixel anything drew, and the
/// fixture could not produce a picture with none: every surface here paints a background outright,
/// because two runs on two desks have to draw the same pixels. So the one refusal that answers the
/// question a file on disk cannot — did anything draw — had nothing to be provoked by.
/// </para>
/// <para>
/// The shape is the defect the render's own arrange step exists over: a tree that was measured and
/// never arranged draws a fully transparent picture of exactly the right size, which looks like a
/// drawing bug and is a calling bug. Reproduced here by hiding rather than by skipping the arrange,
/// and that is deliberate — the render arranges for its caller now, so the only honest way to get
/// the picture that defect produced is a page that really does paint nothing.
/// </para>
/// <para>
/// Rendered on no background, which is the other half of it: a blank composed onto a colour is
/// opaque everywhere whatever the tree did, and the check says so rather than calling it drawn.
/// </para>
/// </summary>
public static class BlankPane
{
    /// <summary>The size it is always drawn at, so the picture is the right size and empty.</summary>
    public static Size Size { get; } = new(360, 200);

    /// <summary>What the rows are called, in the order they are laid out.</summary>
    public static IReadOnlyList<string> Rows { get; } = ["blankTitle", "blankNote", "blankSwatch"];

    /// <summary>
    /// Build it. Hidden and not collapsed: a collapsed row asks for no space, and a page of those
    /// lays out to nothing, which is <see cref="SizelessPane" /> and a different refusal. The
    /// border carries no background of its own, so nothing in the tree paints a pixel.
    /// </summary>
    public static FrameworkElement Build()
    {
        var rows = new StackPanel { Name = "blankRows" };
        foreach (var name in Rows)
            rows.Children.Add(new TextBlock { Name = name, Text = name, Visibility = Visibility.Hidden });

        return new Border
        {
            Name = "blankPane",
            Width = Size.Width,
            Height = Size.Height,
            Child = rows,
        };
    }
}

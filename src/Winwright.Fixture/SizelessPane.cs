using System.Windows;
using System.Windows.Controls;

namespace Winwright.Fixture;

/// <summary>
/// A page whose every row is collapsed, so the whole of it lays out to nothing.
/// <para>
/// WW146. The render refuses a size of nothing, and until this shape existed nothing could make it
/// do so: every surface here is built to draw the same pixels twice, which is the opposite property.
/// A refusal nobody can provoke is one that will quietly stop working.
/// </para>
/// <para>
/// The shape is the point rather than the refusal. A page whose rows are all collapsed is what an
/// application shows when the thing it was going to display turned out not to be there — a report
/// with no rows, a panel whose every child is bound to an empty list — and it is a real page in a
/// real product rather than an element invented to be zero. What makes it worth refusing is the
/// file: an empty render is a successful one to everything that only checks a file exists.
/// </para>
/// </summary>
public static class SizelessPane
{
    /// <summary>What the rows are called, in the order they are laid out.</summary>
    public static IReadOnlyList<string> Rows { get; } = ["sizelessTitle", "sizelessNote", "sizelessSwatch"];

    /// <summary>
    /// Build it. Nothing here is given a size, and every row is collapsed rather than hidden: a
    /// hidden row still asks for its space, which would make this a page that renders blank instead
    /// of one that renders at no size at all — a different refusal, and one <see cref="BlankPane" />
    /// already covers.
    /// </summary>
    public static FrameworkElement Build()
    {
        var rows = new StackPanel { Name = "sizelessRows" };
        foreach (var name in Rows)
            rows.Children.Add(new TextBlock { Name = name, Text = name, Visibility = Visibility.Collapsed });

        return new Border { Name = "sizelessPane", Child = rows };
    }
}

using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Winwright.InApp;

/// <summary>One element the application drew, as the dump spells it.</summary>
/// <param name="Depth">How deep under the root it sits, so the tree survives a flat file.</param>
/// <param name="Kind">Its type, which is what a custom-drawn surface has instead of a control type.</param>
/// <param name="Name">Its name, empty where it has none.</param>
/// <param name="Left">Its left edge on screen, in physical pixels.</param>
/// <param name="Top">Its top edge.</param>
/// <param name="Right">Its right edge, exclusive.</param>
/// <param name="Bottom">Its bottom edge, exclusive.</param>
/// <param name="Visibility">
/// What the application set: Visible, Hidden or Collapsed. WW130 - a collapsed element lays out to
/// nothing correctly, deliberately, and on every page that hides anything, and without this field a
/// caption that wrapped at column zero and a note the page is deliberately not showing produce
/// exactly the same line.
/// </param>
/// <param name="Origin">
/// Who put it there: Application where the application's own markup or code did, Template where
/// expanding a control template did. WW131 - four of the fixture's forty-five elements are laid out
/// wrongly by every rule a geometry check has, and every one is a part of the default tab template
/// drawing a selected header over the edge on purpose. True statements, and not what anybody asked.
/// </param>
public sealed record DrawnElement(
    int Depth,
    string Kind,
    string Name,
    int Left,
    int Top,
    int Right,
    int Bottom,
    string Visibility = "Visible",
    string Origin = "Application")
{
    /// <summary>What the format calls an element the application is showing.</summary>
    public const string Visible = "Visible";

    /// <summary>What the format calls an element the application itself put there.</summary>
    public const string Declared = "Application";

    /// <summary>Whether the application is showing it at all.</summary>
    public bool Shown => string.Equals(Visibility, Visible, StringComparison.Ordinal);

    /// <summary>Whether the application itself put it there.</summary>
    public bool Own => string.Equals(Origin, Declared, StringComparison.Ordinal);

    /// <summary>How wide it is.</summary>
    public int Width => Right - Left;

    /// <summary>How tall it is.</summary>
    public int Height => Bottom - Top;

    /// <summary>Whether it occupies anything at all.</summary>
    public bool Drawn => Width > 0 && Height > 0;

    /// <summary>The line this occupies in the dump. Tabs, for the reason a name has spaces in it.</summary>
    public string Line() => string.Join(
        '\t',
        Depth.ToString(CultureInfo.InvariantCulture),
        Kind,
        Name,
        Left.ToString(CultureInfo.InvariantCulture),
        Top.ToString(CultureInfo.InvariantCulture),
        Right.ToString(CultureInfo.InvariantCulture),
        Bottom.ToString(CultureInfo.InvariantCulture),
        Visibility,
        Origin);

    /// <summary>The one phrase a person reads it by.</summary>
    public override string ToString() =>
        $"{Kind}{(Name.Length == 0 ? "" : $" '{Name}'")} {Width}x{Height} at {Left},{Top}"
        + (Shown ? "" : $" ({Visibility.ToLowerInvariant()})")
        + (Own ? "" : " (template)");
}

/// <summary>What one walk of the visual tree found.</summary>
/// <param name="Elements">Every element, the root first, in the order the walk reached them.</param>
/// <param name="Elided">How many were not walked because the depth or the width ran out.</param>
public sealed record GeometryDumped(IReadOnlyList<DrawnElement> Elements, int Elided)
{
    /// <summary>What was found, said with the part that was not.</summary>
    public string Sentence() => Elided == 0
        ? $"{Elements.Count} element(s) drawn."
        : $"{Elements.Count} element(s) drawn, and {Elided} not walked because the walk ran out.";
}

/// <summary>
/// The geometry of what was drawn, dumped for a harness that has no tree to read.
/// <para>
/// An installer page, a custom-drawn control or an immediate-mode surface has no accessibility
/// tree, and the only check available for one is reading its source — which misses the caption
/// that wrapped, the page that rendered above a screenful of blank space and the button nine
/// pixels out of place. A dump of what was actually laid out is what makes those surfaces
/// assertable at all.
/// </para>
/// <para>
/// Physical pixels, the same as a reported surface and for the same reason: it is the space a copy
/// works in, and a rectangle handed over in layout units is right at one hundred percent and wrong
/// at every scaling a developer runs. The depth is carried because a flat list loses the parent,
/// and half the checks worth making — nothing starts off the surface, nothing ends past it — are
/// about a child against the thing containing it.
/// </para>
/// <para>
/// A dump <em>replaces</em> the file rather than appending to it. It is a snapshot of one layout at
/// one moment, and two of them in one file is a tree that never existed.
/// </para>
/// </summary>
public static class Geometry
{
    /// <summary>The variable naming the file to dump into.</summary>
    public const string PathVariable = "WINWRIGHT_GEOMETRY";

    /// <summary>How deep a walk goes unless told otherwise.</summary>
    public const int DefaultDepth = 12;

    /// <summary>How many children of one element are walked unless told otherwise.</summary>
    public const int DefaultWidth = 500;

    /// <summary>The line that says what a walk did not reach. Never left out where anything was.</summary>
    public const string ElidedMarker = "#elided";

    /// <summary>Where dumps are being written, or null where nothing asked for any.</summary>
    public static string? Where()
    {
        var named = Environment.GetEnvironmentVariable(PathVariable);
        return string.IsNullOrWhiteSpace(named) ? null : Path.GetFullPath(named.Trim());
    }

    /// <summary>
    /// Walk what <paramref name="root"/> drew, without writing anything.
    /// </summary>
    /// <param name="root">The element to walk under. Its own rectangle is the first line.</param>
    /// <param name="depth">How deep to go.</param>
    /// <param name="width">How many children of one element to walk.</param>
    public static GeometryDumped Of(UIElement root, int depth = DefaultDepth, int width = DefaultWidth)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentOutOfRangeException.ThrowIfNegative(depth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        Freezables.Insist(root, "the element being dumped");

        var found = new List<DrawnElement>();
        var elided = 0;
        Walk(root, 0, depth, width, found, ref elided);
        return new GeometryDumped(new ReadOnlyCollection<DrawnElement>(found), elided);
    }

    /// <summary>Dump into the file the harness named, if it named one.</summary>
    /// <param name="root">The element to walk under.</param>
    /// <param name="depth">How deep to go.</param>
    /// <param name="width">How many children of one element to walk.</param>
    /// <returns>What was found, whether or not anything was listening.</returns>
    public static GeometryDumped Dump(UIElement root, int depth = DefaultDepth, int width = DefaultWidth)
    {
        var path = Where();
        return path is null ? Of(root, depth, width) : DumpTo(path, root, depth, width);
    }

    /// <summary>The same, into a named file, replacing whatever was there.</summary>
    /// <param name="path">The dump file.</param>
    /// <param name="root">The element to walk under.</param>
    /// <param name="depth">How deep to go.</param>
    /// <param name="width">How many children of one element to walk.</param>
    public static GeometryDumped DumpTo(
        string path, UIElement root, int depth = DefaultDepth, int width = DefaultWidth)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var dumped = Of(root, depth, width);
        var text = new StringBuilder();
        foreach (var element in dumped.Elements)
            text.Append(element.Line()).Append('\n');

        if (dumped.Elided > 0)
            text.Append(ElidedMarker).Append('\t').Append(dumped.Elided.ToString(CultureInfo.InvariantCulture)).Append('\n');

        // WW218. Filled beside the name the harness watches and moved over it, because writing to
        // that name truncates it first: a reader looking in the gap gets a dump that is there and
        // holds nothing, which is a fault reported against an application that was fine.
        Finished.Writing(
            Path.GetFullPath(path.Trim()),
            beside => File.WriteAllText(beside, text.ToString(), new UTF8Encoding(false)));

        return dumped;
    }

    private static void Walk(
        DependencyObject node, int level, int depth, int width, List<DrawnElement> found, ref int elided)
    {
        if (node is UIElement drawn && Measured(drawn, level) is DrawnElement line)
            found.Add(line);

        if (level >= depth)
        {
            elided += Children(node);
            return;
        }

        var children = Children(node);
        for (var at = 0; at < children; at++)
        {
            if (at >= width)
            {
                elided += children - width;
                return;
            }

            Walk(VisualTreeHelper.GetChild(node, at), level + 1, depth, width, found, ref elided);
        }
    }

    private static int Children(DependencyObject node)
    {
        try
        {
            return VisualTreeHelper.GetChildrenCount(node);
        }
        catch (InvalidOperationException)
        {
            // Some visuals refuse to be enumerated at all. Nothing under them is reachable, and
            // reporting that as zero children is the truth about what this walk can see.
            return 0;
        }
    }

    /// <summary>
    /// Who put this element in the tree.
    /// <para>
    /// WPF answers it exactly: an element the application's own markup or code created has no
    /// templated parent, and one produced by expanding a template does. The application declared
    /// the control; the template drew the part, whoever wrote that template - which is the
    /// distinction a reader wants, since a part drawn four pixels over the edge on purpose is not
    /// something an adopter can fix.
    /// </para>
    /// <para>
    /// Content a data template produced is the application's own markup and is counted as such: its
    /// templated parent is the content presenter that expanded it, which is the one case where a
    /// templated parent does not mean somebody else's chrome.
    /// </para>
    /// </summary>
    private static string Whose(UIElement element)
    {
        if (element is not FrameworkElement framework || framework.TemplatedParent is null)
            return DrawnElement.Declared;

        return framework.TemplatedParent is System.Windows.Controls.ContentPresenter
            ? DrawnElement.Declared
            : "Template";
    }

    private static DrawnElement? Measured(UIElement element, int level)
    {
        // PointToScreen is the whole conversion, the same one a reported surface uses. An element
        // on no presentation source has no screen rectangle at all, and it is left out rather than
        // given one at the origin — a dump full of rectangles at 0,0 is worse than a shorter one.
        if (PresentationSource.FromVisual(element) is null)
            return null;

        try
        {
            var size = element.RenderSize;
            var topLeft = element.PointToScreen(new Point(0, 0));
            var bottomRight = element.PointToScreen(new Point(size.Width, size.Height));

            return new DrawnElement(
                level,
                element.GetType().Name,
                element is FrameworkElement named ? named.Name ?? "" : "",
                (int)Math.Round(topLeft.X),
                (int)Math.Round(topLeft.Y),
                (int)Math.Round(bottomRight.X),
                (int)Math.Round(bottomRight.Y),
                element.Visibility.ToString(),
                Whose(element));
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}

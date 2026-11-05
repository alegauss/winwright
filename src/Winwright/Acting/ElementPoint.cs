using System.Globalization;

using Winwright.Windowing;

namespace Winwright.Acting;

/// <summary>
/// A point inside an element, as a case writes one: <c>x,y</c>, each a percentage of the element
/// (<c>12%</c>) or device-independent pixels from its top-left corner (<c>40</c>). WW572.
/// <para>
/// Found by PhotoBoost (PB67): the crop editor is drawn on a GPU surface, so its handles are pixels
/// and not elements, and resizing the crop is a press on a handle released somewhere else on the same
/// surface. A percentage means the same spot whatever size the surface was laid out at; pixels are
/// for a handle drawn a fixed distance in, and they are device-independent so a case means the same
/// spot on a display scaled to 150%.
/// </para>
/// </summary>
public sealed record ElementPoint
{
    private ElementPoint(string text, double x, bool xFraction, double y, bool yFraction)
    {
        Text = text;
        X = x;
        XIsPercent = xFraction;
        Y = y;
        YIsPercent = yFraction;
    }

    /// <summary>How the case wrote it.</summary>
    public string Text { get; }

    /// <summary>Across: a percentage of the width, or pixels from the left edge.</summary>
    public double X { get; }

    /// <summary>Whether <see cref="X"/> is a percentage.</summary>
    public bool XIsPercent { get; }

    /// <summary>Down: a percentage of the height, or pixels from the top edge.</summary>
    public double Y { get; }

    /// <summary>Whether <see cref="Y"/> is a percentage.</summary>
    public bool YIsPercent { get; }

    /// <summary>What a refusal says a point is.</summary>
    public const string Spelled =
        "'x,y', each a percentage of the element from 0% to 100% or whole device-independent pixels from "
        + "its top-left corner, such as '12%,8%' or '40,30'";

    /// <summary>The point a case wrote, or null where it spells none.</summary>
    /// <param name="written">What the case wrote.</param>
    public static ElementPoint? Parse(string? written)
    {
        if (string.IsNullOrWhiteSpace(written))
            return null;

        var parts = written.Split(',');
        if (parts.Length != 2 || Axis(parts[0]) is not { } x || Axis(parts[1]) is not { } y)
            return null;

        return new ElementPoint(written.Trim(), x.Value, x.Percent, y.Value, y.Percent);
    }

    /// <summary>
    /// Where this point is on the screen, in physical pixels, inside <paramref name="bounds"/> — or
    /// null where pixels written past the element's size put it outside.
    /// </summary>
    /// <param name="bounds">The element's rectangle, in physical pixels.</param>
    /// <param name="scale">Physical pixels per device-independent pixel on its display.</param>
    internal (int X, int Y)? On(WindowBounds bounds, double scale)
    {
        // A percentage of 100 is the far edge, which belongs to the next element over; the last pixel
        // inside is where it lands.
        var across = XIsPercent ? Math.Min(bounds.Width - 1, bounds.Width * X / 100) : X * scale;
        var down = YIsPercent ? Math.Min(bounds.Height - 1, bounds.Height * Y / 100) : Y * scale;
        if (across >= bounds.Width || down >= bounds.Height)
            return null;

        return (bounds.Left + (int)Math.Round(across), bounds.Top + (int)Math.Round(down));
    }

    /// <summary>The point as the case wrote it.</summary>
    public override string ToString() => Text;

    private static (double Value, bool Percent)? Axis(string written)
    {
        var text = written.Trim();
        if (text.EndsWith('%'))
        {
            return double.TryParse(text[..^1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var share)
                && share <= 100
                    ? (share, true)
                    : null;
        }

        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var pixels) ? (pixels, false) : null;
    }
}

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Winwright.InApp;

/// <summary>Raised where there is nothing to render, so no file is written instead of an empty one.</summary>
public sealed class UnrenderableException : InvalidOperationException
{
    /// <summary>Say what could not be rendered and why.</summary>
    public UnrenderableException(string message)
        : base(message)
    {
    }

    /// <summary>Unused. Present because an exception with no default shapes is awkward to catch.</summary>
    public UnrenderableException()
        : base("there was nothing to render")
    {
    }

    /// <summary>Unused. Present for the same reason.</summary>
    public UnrenderableException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>
/// What a render says about itself: the size it settled on, the resolution it drew at, and what
/// was behind it. Read back off the render rather than echoed from the request, so a picture
/// cannot describe a render other than the one that happened.
/// </summary>
/// <param name="Path">The file that was written, or empty where the render stayed in memory.</param>
/// <param name="Width">The width in device-independent units, as the arrange settled it.</param>
/// <param name="Height">The height, the same way.</param>
/// <param name="Pixels">The picture's own width in pixels, which is the width times the scale.</param>
/// <param name="Lines">Its height in pixels.</param>
/// <param name="Dpi">The resolution it was drawn at.</param>
/// <param name="Background">What was composed behind it, in words a report prints.</param>
public sealed record RenderedPicture(
    string Path, double Width, double Height, int Pixels, int Lines, double Dpi, string Background)
{
    /// <summary>The one line a run prints on a render that worked.</summary>
    public string Sentence() =>
        $"rendered {Width:0.##}x{Height:0.##} at {Dpi:0.##} dpi ({Pixels}x{Lines} pixels) on {Background}"
        + (Path.Length == 0 ? "." : $", written to {Path}.");
}

/// <summary>
/// The render, as one verb.
/// <para>
/// The measure, the arrange, the update, the render target, the composed background and the
/// encoder are the same six steps in every project that wants a picture of its own window, and
/// every project writes all six again. Here they are one call, and the ordering that matters is
/// inside it rather than expected of a caller.
/// </para>
/// <para>
/// The arrange is the step that carries the defect. A tree that was measured and never arranged
/// renders as nothing at all — a fully transparent picture of the right size, which looks like a
/// drawing bug and is a calling bug. Leaving it to the caller is what makes that mistake possible,
/// so it is not left to the caller.
/// </para>
/// <para>
/// A size of nothing is refused rather than written. An empty file is a successful render as far
/// as anything checking that a file exists can tell, and this is the one place that can still know
/// the difference.
/// </para>
/// </summary>
public static class Render
{
    /// <summary>The resolution WPF works in, and the one a render draws at unless told otherwise.</summary>
    public const double DefaultDpi = 96;

    /// <summary>Render an element to a PNG, measuring and arranging it first.</summary>
    /// <param name="element">The element. It does not have to be in a window, or in any tree.</param>
    /// <param name="path">Where to write the PNG.</param>
    /// <param name="size">The size to lay it out at, or null to take the size it asks for.</param>
    /// <param name="background">What to compose behind it, or null to leave it transparent.</param>
    /// <param name="dpi">The resolution to draw at.</param>
    /// <exception cref="UnrenderableException">Where the element lays out to nothing — see the remarks on the type.</exception>
    public static RenderedPicture ToFile(
        FrameworkElement element,
        string path,
        Size? size = null,
        Brush? background = null,
        double dpi = DefaultDpi)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var full = Path.GetFullPath(path.Trim());
        var picture = Rendered(element, size, background, dpi);

        var directory = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(picture.Image));

        // Written through a stream that is closed before the receipt is returned: a caller told a
        // file exists and then finding it half-written is the same lie as an empty one.
        using (var file = File.Create(full))
            encoder.Save(file);

        return picture.Facts with { Path = full };
    }

    /// <summary>
    /// The same, stopping at the bitmap. Frozen before it is handed back, so the picture can cross
    /// to the thread that wanted it — every render here happens on a thread that owns a message
    /// queue, and the caller asking for it usually does not.
    /// </summary>
    /// <param name="element">The element to render.</param>
    /// <param name="size">The size to lay it out at, or null to take the size it asks for.</param>
    /// <param name="background">What to compose behind it, or null to leave it transparent.</param>
    /// <param name="dpi">The resolution to draw at.</param>
    public static BitmapSource ToBitmap(
        FrameworkElement element, Size? size = null, Brush? background = null, double dpi = DefaultDpi) =>
        Rendered(element, size, background, dpi).Image;

    private static (BitmapSource Image, RenderedPicture Facts) Rendered(
        FrameworkElement element, Size? size, Brush? background, double dpi)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (!double.IsFinite(dpi) || dpi <= 0)
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "a render draws at a resolution, and this is not one");

        // Before anything is drawn, because the failure otherwise is a COM error about a message
        // filter that says nothing about the thread this was called on.
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new UnrenderableException(
                "this render was called from a thread that is not STA, and the presentation stack has no "
                    + "meaning on one — run it on a thread that owns a message queue");
        }

        // Measure, arrange, update: all three, in this order, and none of them optional. The
        // arrange is why this verb exists — see the note on the type.
        var wanted = size ?? new Size(double.PositiveInfinity, double.PositiveInfinity);
        element.Measure(wanted);
        var settled = size ?? element.DesiredSize;
        element.Arrange(new Rect(new Point(0, 0), settled));
        element.UpdateLayout();

        Refuse(element, settled);

        var scale = dpi / DefaultDpi;
        var pixels = (int)Math.Ceiling(settled.Width * scale);
        var lines = (int)Math.Ceiling(settled.Height * scale);

        var drawn = new RenderTargetBitmap(pixels, lines, dpi, dpi, PixelFormats.Pbgra32);
        drawn.Render(element);

        var composed = Composed(drawn, settled, background, dpi, pixels, lines);
        composed.Freeze();

        return (
            composed,
            new RenderedPicture("", settled.Width, settled.Height, pixels, lines, dpi, Describe(background)));
    }

    private static BitmapSource Composed(
        BitmapSource drawn, Size settled, Brush? background, double dpi, int pixels, int lines)
    {
        if (background is null)
            return drawn;

        // A second pass rather than a background drawn under the element in one: an element that
        // paints its own background would sit over this either way, and doing it after means what
        // is composed is the picture that was actually produced and not the tree that produced it.
        var canvas = new DrawingVisual();
        using (var context = canvas.RenderOpen())
        {
            context.DrawRectangle(background, null, new Rect(new Point(0, 0), settled));
            context.DrawImage(drawn, new Rect(new Point(0, 0), settled));
        }

        var target = new RenderTargetBitmap(pixels, lines, dpi, dpi, PixelFormats.Pbgra32);
        target.Render(canvas);
        return target;
    }

    private static void Refuse(FrameworkElement element, Size settled)
    {
        var named = string.IsNullOrEmpty(element.Name) ? element.GetType().Name : $"{element.GetType().Name} '{element.Name}'";

        if (!double.IsFinite(settled.Width) || !double.IsFinite(settled.Height))
        {
            throw new UnrenderableException(
                $"{named} laid out to {Measured(settled)}, which is not a size a picture can have: it wants as much "
                    + "room as it is given, so name the size to render it at");
        }

        if (settled.Width <= 0 || settled.Height <= 0)
        {
            throw new UnrenderableException(
                $"{named} laid out to {Measured(settled)}, so there is nothing to render — an empty file is a "
                    + "successful render to everything that only checks a file exists");
        }
    }

    private static string Measured(Size size) => $"{size.Width:0.##}x{size.Height:0.##}";

    private static string Describe(Brush? background) => background switch
    {
        null => "nothing, so it is transparent where the tree drew nothing",
        SolidColorBrush solid => solid.Color.ToString(System.Globalization.CultureInfo.InvariantCulture),
        _ => background.GetType().Name,
    };
}

using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Winwright.InApp;

/// <summary>
/// One surface the application drew, as it reports it: a name and four numbers, in physical
/// pixels.
/// </summary>
/// <param name="Name">What the application calls it, which is what a case asks for.</param>
/// <param name="Left">Its left edge on screen, in physical pixels.</param>
/// <param name="Top">Its top edge.</param>
/// <param name="Right">Its right edge, exclusive.</param>
/// <param name="Bottom">Its bottom edge, exclusive.</param>
public sealed record ReportedSurface(string Name, int Left, int Top, int Right, int Bottom)
{
    /// <summary>How wide it is.</summary>
    public int Width => Right - Left;

    /// <summary>How tall it is.</summary>
    public int Height => Bottom - Top;

    /// <summary>
    /// The line this occupies in the report. Tab-separated because a surface name has spaces in it
    /// and a reader splitting on those would find five fields where a name had two words.
    /// </summary>
    public string Line() => string.Join(
        '\t',
        Name,
        Left.ToString(CultureInfo.InvariantCulture),
        Top.ToString(CultureInfo.InvariantCulture),
        Right.ToString(CultureInfo.InvariantCulture),
        Bottom.ToString(CultureInfo.InvariantCulture));

    /// <summary>The one phrase a person reads it by.</summary>
    public override string ToString() => $"{Name} {Width}x{Height} at {Left},{Top}";
}

/// <summary>
/// What the application drew, said out loud so a capture can be asserted against it.
/// <para>
/// The application knows which rectangle it just painted and nothing has ever asked it. A harness
/// in another process can only guess — and a guess about a popup, a flyout or a page that scrolled
/// is a capture asserted against a rectangle nobody drew.
/// </para>
/// <para>
/// The rectangle is reported in <em>physical pixels</em>, because layout happens in
/// device-independent units and a copy works in pixels: a rectangle handed over in the wrong one
/// is right at one hundred percent and wrong at every scaling a developer actually runs. The
/// format is deliberately dull and machine-first — a name, then four numbers — because it is the
/// contract between two packages that cannot reference each other, and a format with opinions is
/// a format the two halves can disagree about.
/// </para>
/// <para>
/// An element that is on no screen reports nothing and raises nothing. It is caught by the
/// never-reported arm on the reading side, which is an answer a run can act on, rather than by an
/// exception thrown inside an application nobody is watching.
/// </para>
/// </summary>
public static class Surfaces
{
    /// <summary>
    /// The variable naming the file to report into. Unset means report nowhere: an application
    /// shipped to its users is not under test, and one writing files because it once was is worse
    /// than one that never reported at all.
    /// </summary>
    public const string PathVariable = "WINWRIGHT_SURFACES";

    private static readonly Lock Gate = new();

    /// <summary>Where reports are being written, or null where nothing asked for any.</summary>
    public static string? Where()
    {
        var named = Environment.GetEnvironmentVariable(PathVariable);
        return string.IsNullOrWhiteSpace(named) ? null : Path.GetFullPath(named.Trim());
    }

    /// <summary>
    /// Report a surface into the file the harness named, if it named one.
    /// </summary>
    /// <param name="name">What to call it, as a case will ask for it.</param>
    /// <param name="element">The element that was drawn.</param>
    /// <returns>What was reported, or null where the element is on no screen or nothing is listening.</returns>
    public static ReportedSurface? Report(string name, FrameworkElement element)
    {
        var path = Where();
        return path is null ? null : ReportTo(path, name, element);
    }

    /// <summary>
    /// The same, into a named file. This is the door a case uses when it would rather say where
    /// than set a variable for a whole process.
    /// </summary>
    /// <param name="path">The report file. Appended to, and created with its directory.</param>
    /// <param name="name">What to call the surface.</param>
    /// <param name="element">The element that was drawn.</param>
    /// <returns>What was reported, or null where the element is on no screen.</returns>
    /// <exception cref="ArgumentException">Where the name carries a tab or a newline, which would forge a line.</exception>
    public static ReportedSurface? ReportTo(string path, string name, FrameworkElement element)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(element);

        var named = name.Trim();
        if (named.AsSpan().IndexOfAny('\t', '\r', '\n') >= 0)
        {
            throw new ArgumentException(
                $"'{named}' carries a tab or a newline, and a name that can forge a line can report a rectangle "
                    + "nothing drew",
                nameof(name));
        }

        var measured = Measure(named, element);
        if (measured is null)
            return null;

        Append(Path.GetFullPath(path.Trim()), measured.Line());
        return measured;
    }

    /// <summary>
    /// The rectangle without writing anything, for a caller that has its own channel. Null where
    /// the element is on no screen, which is the one case there is no rectangle to name.
    /// </summary>
    /// <param name="name">What to call it.</param>
    /// <param name="element">The element that was drawn.</param>
    public static ReportedSurface? Measure(string name, FrameworkElement element)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(element);
        Freezables.Insist(element, "the element being reported");

        // PointToScreen is the whole conversion: it answers in physical pixels, which is the space
        // the copy already works in. Without a presentation source there is no screen to be on,
        // and inventing one from the layout would report a rectangle at the top-left of nothing.
        if (PresentationSource.FromVisual(element) is null)
            return null;

        try
        {
            var topLeft = element.PointToScreen(new Point(0, 0));
            var bottomRight = element.PointToScreen(new Point(element.ActualWidth, element.ActualHeight));

            return new ReportedSurface(
                name.Trim(),
                (int)Math.Round(topLeft.X),
                (int)Math.Round(topLeft.Y),
                (int)Math.Round(bottomRight.X),
                (int)Math.Round(bottomRight.Y));
        }
        catch (InvalidOperationException)
        {
            // The source went between the check and the read, which is a window closing under a
            // report. Nothing to say about it, and the never-reported arm says that much.
            return null;
        }
    }

    private static void Append(string path, string line)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        // Locked and appended a line at a time: an application reporting from two threads at once
        // is ordinary, and a half-written line is a rectangle a reader would parse as real.
        lock (Gate)
            File.AppendAllText(path, line + "\n", new UTF8Encoding(false));
    }
}

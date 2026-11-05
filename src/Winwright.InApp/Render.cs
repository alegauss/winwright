using System.Runtime.InteropServices;
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
/// <param name="Sees">
/// What this process can see of the display, read at the moment of the render. On the receipt
/// because a picture drawn by a system-aware process on a scaled display has a size that does not
/// mean what it says, and nothing else about the file would ever say so.
/// </param>
/// <param name="Composed">
/// WW552. What in the picture did not come from the visual tree, in words a report prints: a region
/// drawn by a window of its own and copied from that window, or a <c>D3DImage</c> and how it reached
/// the render. Empty where the tree drew all of it.
/// </param>
public sealed record RenderedPicture(
    string Path,
    double Width,
    double Height,
    int Pixels,
    int Lines,
    double Dpi,
    string Background,
    string Sees = "",
    string Composed = "")
{
    /// <summary>Whether the size on this receipt is in the space the window lives in.</summary>
    public bool Trustworthy => Sees == "per-monitor aware";

    /// <summary>The one line a run prints on a render that worked.</summary>
    public string Sentence() =>
        $"rendered {Width:0.##}x{Height:0.##} at {Dpi:0.##} dpi ({Pixels}x{Lines} pixels) on {Background}"
        + (Composed.Length == 0 ? "" : $", with {Composed}")
        + (Path.Length == 0 ? "" : $", written to {Path}")
        + (Sees.Length == 0 ? "." : $"; this process is {Sees}.");
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
        var picture = Rendered(element, size, background, dpi, null);
        Write(full, picture.Image);
        return picture.Facts with { Path = full };
    }

    /// <summary>
    /// The same, on the background the application itself chose. The receipt then names which
    /// source answered rather than only the colour, which is the difference between a run that can
    /// be diagnosed and a hex value nobody can trace back to a theme.
    /// </summary>
    /// <param name="element">The element to render.</param>
    /// <param name="path">Where to write the PNG.</param>
    /// <param name="background">What the application said to draw it on.</param>
    /// <param name="size">The size to lay it out at, or null to take the size it asks for.</param>
    /// <param name="dpi">The resolution to draw at.</param>
    public static RenderedPicture ToFile(
        FrameworkElement element,
        string path,
        ChosenBackground background,
        Size? size = null,
        double dpi = DefaultDpi)
    {
        ArgumentNullException.ThrowIfNull(background);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var full = Path.GetFullPath(path.Trim());
        var picture = Rendered(element, size, background.Brush, dpi, background.ToString());
        Write(full, picture.Image);
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
        Rendered(element, size, background, dpi, null).Image;

    private static void Write(string full, BitmapSource image)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));

        // Written through a stream that is closed before the receipt is returned: a caller told a
        // file exists and then finding it half-written is the same lie as an empty one.
        //
        // WW218. And filled beside the name rather than into it, which is the other half of the
        // same sentence for a reader in another process: File.Create truncates first, so a harness
        // polling in the gap sees a picture that is there and is not a picture yet.
        Finished.Writing(full, beside =>
        {
            using var file = File.Create(beside);
            encoder.Save(file);
        });
    }

    private static (BitmapSource Image, RenderedPicture Facts) Rendered(
        FrameworkElement element, Size? size, Brush? background, double dpi, string? describedAs)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (!double.IsFinite(dpi) || dpi <= 0)
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "a render draws at a resolution, and this is not one");

        // Before anything is drawn, because the failure otherwise is a COM error about a message
        // filter that says nothing about the thread this was called on.
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new ThreadBoundException(
                "this render was called from a thread that is not STA, and the presentation stack has no "
                    + "meaning on one — run it on a thread that owns a message queue");
        }

        // Both before the layout, and both refused for a reason about threading rather than about
        // the picture: what arrives otherwise is raised from inside the drawing and names neither
        // the object nor either thread.
        Freezables.Insist(element, "the element being rendered");
        Freezables.Insist(background, "the background brush");

        // Measure, arrange, update: all three, in this order, and none of them optional. The
        // arrange is why this verb exists — see the note on the type.
        var wanted = size ?? new Size(double.PositiveInfinity, double.PositiveInfinity);
        element.Measure(wanted);
        var settled = size ?? element.DesiredSize;
        element.Arrange(new Rect(new Point(0, 0), settled));
        element.UpdateLayout();

        Refuse(element, settled);
        var hosted = Hosted(element).ToList();
        RefuseUnshown(element, hosted);

        var scale = dpi / DefaultDpi;
        var pixels = (int)Math.Ceiling(settled.Width * scale);
        var lines = (int)Math.Ceiling(settled.Height * scale);

        var drawn = new RenderTargetBitmap(pixels, lines, dpi, dpi, PixelFormats.Pbgra32);
        drawn.Render(element);

        // WW552. Read after the tree is drawn and before anything is composed over it: a D3DImage is
        // in the tree, so what it put in the picture is in `drawn` or nowhere.
        var said = Surfaced(element, drawn, scale).ToList();

        // Copied after the render, so the windows are where the arrange just put them.
        var copied = hosted.Select(host => (Region: Bounds(host, element), Picture: Copied(host, element))).ToList();
        said.AddRange(hosted.Select(host => $"{Where(host, element)} copied from its own window"));

        var composed = Composed(drawn, settled, background, copied, dpi, pixels, lines);
        composed.Freeze();

        return (
            composed,
            new RenderedPicture(
                "",
                settled.Width,
                settled.Height,
                pixels,
                lines,
                dpi,
                describedAs ?? Describe(background),
                Coordinates.Sentence(),
                string.Join("; ", said)));
    }

    private static BitmapSource Composed(
        BitmapSource drawn,
        Size settled,
        Brush? background,
        IReadOnlyList<(Rect Region, BitmapSource Picture)> copied,
        double dpi,
        int pixels,
        int lines)
    {
        if (background is null && copied.Count == 0)
            return drawn;

        // A second pass rather than a background drawn under the element in one: an element that
        // paints its own background would sit over this either way, and doing it after means what
        // is composed is the picture that was actually produced and not the tree that produced it.
        //
        // WW552. The hosted windows go last and over everything: on the screen their pixels sit on
        // top of whatever the tree reserved for them, and the picture says the same.
        var canvas = new DrawingVisual();
        using (var context = canvas.RenderOpen())
        {
            if (background is not null)
                context.DrawRectangle(background, null, new Rect(new Point(0, 0), settled));

            context.DrawImage(drawn, new Rect(new Point(0, 0), settled));

            foreach (var (region, picture) in copied)
                context.DrawImage(picture, region);
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

    /// <summary>
    /// WW552. A region whose pixels belong to a window of its own: a child HWND carrying a DXGI
    /// swapchain, a WinForms control, a browser. A render draws the visual tree, so each would come
    /// out as the hole it is in the tree, the right size with nothing saying a part of it is missing.
    /// Found by PhotoBoost (PB27, PB40), whose image area is Direct3D in an HwndHost on a Mica window —
    /// the screen copy is refused for the glass, rightly, and this route answered with a picture of
    /// the comparator with no comparison in it.
    /// <para>
    /// Such a region is copied from its window and composed over the render. That needs the window
    /// to exist and be shown, so a host that has none — a page built and never put in a window — is
    /// refused here, by name, before anything is drawn.
    /// </para>
    /// </summary>
    private static void RefuseUnshown(FrameworkElement element, IReadOnlyList<System.Windows.Interop.HwndHost> hosted)
    {
        var unshown = hosted.Where(host => host.Handle == 0 || PresentationSource.FromVisual(host) is null || !IsWindowVisible(host.Handle)).ToList();
        if (unshown.Count == 0)
            return;

        var named = string.IsNullOrEmpty(element.Name) ? element.GetType().Name : $"{element.GetType().Name} '{element.Name}'";
        throw new UnrenderableException(
            $"{named} holds {unshown.Count} region{(unshown.Count == 1 ? "" : "s")} drawn by a window of "
                + $"{(unshown.Count == 1 ? "its" : "their")} own, which a render of the visual tree leaves empty: "
                + string.Join("; ", unshown.Select(one => Where(one, element)))
                + ". A render copies such a region from its window, and this one is in no window that is "
                + "shown; a picture with that hole in it would read as one of the whole window");
    }

    /// <summary>Where a hosted region sits in the render, in device-independent units.</summary>
    private static Rect Bounds(FrameworkElement host, FrameworkElement root) =>
        host.TransformToAncestor(root).TransformBounds(new Rect(host.RenderSize));

    /// <summary>
    /// The pixels a hosted window shows, copied out of its top-level window. WW552.
    /// <para>
    /// The top-level window and not the child, printed with <c>PW_RENDERFULLCONTENT</c>: that asks
    /// the compositor for what it composed, and a DXGI swapchain is composed there and nowhere a GDI
    /// copy of the child can reach. The child's own rectangle is then cut out of it, so the glass the
    /// rest of the window may carry is never in what is kept.
    /// </para>
    /// <para>
    /// A copy that came back black throughout is refused. That is what a surface the print could not
    /// reach looks like, and composing it would put a black rectangle where the image was and call
    /// the picture whole.
    /// </para>
    /// </summary>
    private static BitmapSource Copied(System.Windows.Interop.HwndHost host, FrameworkElement root)
    {
        var child = host.Handle;
        var top = GetAncestor(child, RootAncestor);
        if (!GetWindowRect(top, out var outer) || !GetWindowRect(child, out var inner))
            throw new UnrenderableException($"{Where(host, root)} has a window whose rectangle could not be read, so there is nothing to copy it from");

        var width = outer.Right - outer.Left;
        var height = outer.Bottom - outer.Top;
        var left = Math.Max(inner.Left - outer.Left, 0);
        var up = Math.Max(inner.Top - outer.Top, 0);
        var across = Math.Min(inner.Right - outer.Left, width) - left;
        var down = Math.Min(inner.Bottom - outer.Top, height) - up;
        if (width <= 0 || height <= 0 || across <= 0 || down <= 0)
            throw new UnrenderableException($"{Where(host, root)} has a window with no area inside its top-level window, so there is nothing to copy");

        var screen = GetDC(0);
        var memory = CreateCompatibleDC(screen);
        var header = new BitmapInfoHeader
        {
            Size = Marshal.SizeOf<BitmapInfoHeader>(),
            Width = width,
            Height = -height,
            Planes = 1,
            BitCount = 32,
        };

        var section = CreateDIBSection(memory, ref header, 0, out var bits, 0, 0);
        var before = SelectObject(memory, section);
        try
        {
            if (section == 0 || !PrintWindow(top, memory, RenderFullContent))
                throw new UnrenderableException($"{Where(host, root)} could not be copied from its window: the compositor printed nothing for it");

            var stride = across * 4;
            var kept = new byte[stride * down];
            for (var row = 0; row < down; row++)
                Marshal.Copy(bits + (((up + row) * width) + left) * 4, kept, row * stride, stride);

            // Opaque, because what a print leaves in the alpha byte is not a statement about the
            // window: a hosted window covers its rectangle completely, which is why it is a hole.
            var lit = false;
            for (var at = 0; at < kept.Length; at += 4)
            {
                kept[at + 3] = 255;
                lit |= kept[at] != 0 || kept[at + 1] != 0 || kept[at + 2] != 0;
            }

            if (!lit)
            {
                throw new UnrenderableException(
                    $"{Where(host, root)} came back black throughout from its window, which is what a surface "
                        + "the compositor's print could not reach looks like; composing it would put a black "
                        + "rectangle where the image was and call the picture whole");
            }

            var picture = BitmapSource.Create(across, down, DefaultDpi, DefaultDpi, PixelFormats.Bgra32, null, kept, stride);
            picture.Freeze();
            return picture;
        }
        finally
        {
            SelectObject(memory, before);
            if (section != 0)
                DeleteObject(section);

            DeleteDC(memory);
            ReleaseDC(0, screen);
        }
    }

    /// <summary>
    /// What each <c>D3DImage</c> in the tree put in the render. WW552.
    /// <para>
    /// A D3DImage is in the visual tree, so it is drawn — but a render is a software pass, and its
    /// surface reaches one only where the application set its back buffer with the software fallback
    /// on. Without it the region renders as nothing at all, the picture is the right size, and the
    /// receipt could not tell. So the region is read back off the render: one that drew something
    /// is named on the receipt, and one that drew nothing is refused.
    /// </para>
    /// </summary>
    private static IEnumerable<string> Surfaced(FrameworkElement root, BitmapSource drawn, double scale)
    {
        var said = new List<string>();
        // Shown by the element and every ancestor rather than by IsVisible, which is false for any
        // tree in no window — the ordinary case for a render. A size of nothing is not a reason to
        // skip one: an image whose surface was never set lays out to nothing and draws nothing,
        // which is the region this exists to name.
        foreach (var image in Elements<System.Windows.Controls.Image>(root)
            .Where(one => one.Source is System.Windows.Interop.D3DImage && Shown(one, root)))
        {
            var region = Bounds(image, root);
            var x = Math.Clamp((int)Math.Floor(region.X * scale), 0, drawn.PixelWidth);
            var y = Math.Clamp((int)Math.Floor(region.Y * scale), 0, drawn.PixelHeight);
            var across = Math.Clamp((int)Math.Ceiling(region.Right * scale), 0, drawn.PixelWidth) - x;
            var down = Math.Clamp((int)Math.Ceiling(region.Bottom * scale), 0, drawn.PixelHeight) - y;

            var read = new byte[Math.Max(across, 0) * Math.Max(down, 0) * 4];
            if (read.Length > 0)
                drawn.CopyPixels(new Int32Rect(x, y, across, down), read, across * 4, 0);

            var named = string.IsNullOrEmpty(image.Name) ? "D3DImage" : $"D3DImage in '{image.Name}'";
            var at = $"{region.Width:0.##}x{region.Height:0.##} at {region.X:0.##},{region.Y:0.##}";
            var drew = false;
            for (var alpha = 3; alpha < read.Length && !drew; alpha += 4)
                drew = read[alpha] != 0;

            if (!drew)
            {
                throw new UnrenderableException(
                    $"the {named} {at} drew nothing into the render: a render is a software pass, and a D3DImage "
                        + "reaches one only through its software fallback, which SetBackBuffer turns on with "
                        + "enableSoftwareFallback: true. A picture with that region empty would read as one of the whole window");
            }

            said.Add($"the {named} {at} drawn through its software fallback");
        }

        return said;
    }

    /// <summary>Whether the element and everything between it and the root is visible.</summary>
    private static bool Shown(UIElement element, DependencyObject root)
    {
        for (DependencyObject? at = element; at is not null; at = VisualTreeHelper.GetParent(at))
        {
            if (at is UIElement one && one.Visibility != Visibility.Visible)
                return false;

            if (ReferenceEquals(at, root))
                break;
        }

        return true;
    }

    /// <summary>Every element of that type under <paramref name="root"/>, not walking into hosted windows.</summary>
    private static IEnumerable<T> Elements<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T found)
                yield return found;

            foreach (var under in Elements<T>(child))
                yield return under;
        }
    }

    /// <summary>Every element under <paramref name="root"/> that hosts a window of its own. WW552.</summary>
    private static IEnumerable<System.Windows.Interop.HwndHost> Hosted(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is System.Windows.Interop.HwndHost host)
            {
                // Not walked into: what it hosts is not in this tree, which is the whole point.
                if (host.Visibility == Visibility.Visible)
                    yield return host;

                continue;
            }

            foreach (var under in Hosted(child))
                yield return under;
        }
    }

    /// <summary>A hosted region as a refusal names it: its type, its name, and where it sits in the render.</summary>
    private static string Where(FrameworkElement host, FrameworkElement root)
    {
        var named = string.IsNullOrEmpty(host.Name) ? host.GetType().Name : $"{host.GetType().Name} '{host.Name}'";
        var at = Bounds(host, root);
        return $"{named} {at.Width:0.##}x{at.Height:0.##} at {at.X:0.##},{at.Y:0.##}";
    }

    private static string Measured(Size size) => $"{size.Width:0.##}x{size.Height:0.##}";

    private static string Describe(Brush? background) => background switch
    {
        null => "nothing, so it is transparent where the tree drew nothing",
        SolidColorBrush solid => solid.Color.ToString(System.Globalization.CultureInfo.InvariantCulture),
        _ => background.GetType().Name,
    };

    /// <summary>GA_ROOT: the top-level window a child belongs to.</summary>
    private const uint RootAncestor = 2;

    /// <summary>PW_RENDERFULLCONTENT: print what the compositor composed, swapchains included.</summary>
    private const uint RenderFullContent = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct Bounding
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public int Size;
        public int Width;
        public int Height;
        public short Planes;
        public short BitCount;
        public int Compression;
        public int SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public int ClrUsed;
        public int ClrImportant;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint window);

    [DllImport("user32.dll")]
    private static extern nint GetAncestor(nint window, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint window, out Bounding rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PrintWindow(nint window, nint context, uint flags);

    [DllImport("user32.dll")]
    private static extern nint GetDC(nint window);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(nint window, nint context);

    [DllImport("gdi32.dll")]
    private static extern nint CreateCompatibleDC(nint context);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteDC(nint context);

    [DllImport("gdi32.dll")]
    private static extern nint CreateDIBSection(nint context, ref BitmapInfoHeader header, uint usage, out nint bits, nint section, uint offset);

    [DllImport("gdi32.dll")]
    private static extern nint SelectObject(nint context, nint chosen);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(nint chosen);
}

using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

using Winwright.Windowing;

namespace Winwright.Capturing;

/// <summary>
/// A rectangle of the screen, written to a file. WW336.
/// <para>
/// This engine has never performed a capture. Every door it has takes what to write as a delegate —
/// <see cref="CaptureReceipt.Taking" /> runs one between its readings, <see cref="FrameRun.At" />
/// calls one per frame — and the caller supplied it. That was right while every caller was C#: the
/// readings are the hard part and the writing is six lines somebody already has.
/// </para>
/// <para>
/// A case is a data file and has no caller. So the one act a scenario could not name was the one
/// with nobody to hand it a delegate, and three adopting projects each wrote the same test in C#
/// beside their cases rather than as one of them. The engine performs the copy it has always been
/// able to judge.
/// </para>
/// <para>
/// The copy and never the render. A render draws an application's own visual tree, which is the
/// safer picture and the one this engine cannot take from outside the process — that is the in-app
/// half's, and a capture step against a renderable window says so rather than reaching for the
/// screen. What is here is the route that exists for a surface no tree holds: a menu, a balloon, a
/// popup a framework drew.
/// </para>
/// <para>
/// No package. GDI copies the pixels and the presentation stack's own encoder writes them, both
/// in-box for a windows target, which is what keeps the engine's one non-goal about dependencies
/// intact.
/// </para>
/// </summary>
public static class ScreenCopy
{
    /// <summary>SRCCOPY: the raster op that copies rather than combining.</summary>
    private const uint SourceCopy = 0x00CC0020;

    /// <summary>
    /// CAPTUREBLT. Included, and the reason is the surface this exists for: without it a layered
    /// window in the rectangle is copied as the desktop behind it rather than as itself, so a menu
    /// with a shadow comes back with a hole where the shadow is.
    /// </summary>
    private const uint CaptureLayered = 0x40000000;

    /// <summary>
    /// Copy one rectangle of the screen into a PNG.
    /// </summary>
    /// <param name="region">What to copy, in the physical pixels the desktop is addressed in.</param>
    /// <param name="into">The file to write. Its directory is created where it is missing.</param>
    /// <exception cref="ArgumentException">Where no path was given.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Where the region has no area to copy.</exception>
    /// <exception cref="InvalidOperationException">Where the desktop would not answer for it.</exception>
    public static void Into(WindowBounds region, string into)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(into);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(region.Width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(region.Height);

        var full = System.IO.Path.GetFullPath(into.Trim());
        var directory = System.IO.Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var screen = GetDC(0);
        if (screen == 0)
            throw new InvalidOperationException("the desktop would not give up a device context to copy from");

        nint memory = 0;
        nint bitmap = 0;
        try
        {
            memory = CreateCompatibleDC(screen);
            bitmap = CreateCompatibleBitmap(screen, region.Width, region.Height);
            if (memory == 0 || bitmap == 0)
                throw new InvalidOperationException("a bitmap the size of the region could not be made");

            var previous = SelectObject(memory, bitmap);
            try
            {
                if (!BitBlt(memory, 0, 0, region.Width, region.Height, screen, region.Left, region.Top,
                        SourceCopy | CaptureLayered))
                {
                    throw new InvalidOperationException(
                        $"the copy of {region} was refused (0x{Marshal.GetLastWin32Error():X8})");
                }
            }
            finally
            {
                SelectObject(memory, previous);
            }

            Write(bitmap, full);
        }
        finally
        {
            if (bitmap != 0)
                DeleteObject(bitmap);

            if (memory != 0)
                DeleteDC(memory);

            ReleaseDC(0, screen);
        }
    }

    /// <summary>
    /// The pixels as a PNG. The presentation stack's own encoder, which is already referenced here
    /// because <see cref="Colours"/> and <see cref="Pictures"/> read files back through its decoder
    /// — one stack for both directions, so what is written is what those two can read.
    /// </summary>
    /// <param name="bitmap">The GDI bitmap holding the copy.</param>
    /// <param name="into">The file to write.</param>
    private static void Write(nint bitmap, string into)
    {
        var source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
            bitmap,
            0,
            System.Windows.Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));

        using var file = File.Create(into);
        encoder.Save(file);
    }

    [DllImport("user32.dll")]
    private static extern nint GetDC(nint window);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(nint window, nint context);

    [DllImport("gdi32.dll")]
    private static extern nint CreateCompatibleDC(nint context);

    [DllImport("gdi32.dll")]
    private static extern nint CreateCompatibleBitmap(nint context, int width, int height);

    [DllImport("gdi32.dll")]
    private static extern nint SelectObject(nint context, nint held);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(nint held);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteDC(nint context);

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BitBlt(
        nint into, int x, int y, int width, int height, nint from, int fromX, int fromY, uint how);
}

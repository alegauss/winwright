using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Winwright.Fixture;

/// <summary>
/// A topmost window over a rectangle the caller names.
/// <para>
/// The region check is the most intricate piece of the capture stack, and it is otherwise
/// exercised by moving a window by hand and hoping. Here the intersection, the naming of the
/// intruder and the raise-then-refuse loop are all driven — including the case that must pass, an
/// intruder that overlaps nothing.
/// </para>
/// <para>
/// The rectangle is named in <em>physical pixels</em> and placed with a call that takes them. A
/// window positioned through the layout's own units would land somewhere else on every scaled
/// display, which is the same mistake a surface reported in the wrong space makes and the one this
/// whole protocol is careful about.
/// </para>
/// </summary>
public static class Intruder
{
    private const int Topmost = -1;
    private const uint NoActivate = 0x0010;
    private const uint ShowWindow = 0x0040;

    /// <summary>Read a rectangle as a run spells it: four whole numbers, comma-separated.</summary>
    /// <param name="said">The flag's value.</param>
    /// <returns>Left, top, width and height in physical pixels.</returns>
    /// <exception cref="UnknownFlagException">Where it is not four numbers, or the size is nothing.</exception>
    public static (int Left, int Top, int Width, int Height) Read(string said)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(said);

        var fields = said.Split(',');
        var numbers = new int[4];
        if (fields.Length != 4)
            throw new UnknownFlagException(UnknownFlag.MalformedRectangle, $"--intrude takes left,top,width,height and was given '{said}'.");

        for (var at = 0; at < 4; at++)
        {
            if (!int.TryParse(fields[at].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out numbers[at]))
                throw new UnknownFlagException(UnknownFlag.NotAWholeNumber, $"--intrude takes whole numbers and '{fields[at].Trim()}' is not one.");
        }

        // A rectangle of no area covers nothing, so an intruder placed at one would be a shape that
        // provokes the refusal it exists for exactly never.
        if (numbers[2] <= 0 || numbers[3] <= 0)
            throw new UnknownFlagException(UnknownFlag.CoversNothing, $"--intrude was given a size of {numbers[2]}x{numbers[3]}, which covers nothing.");

        return (numbers[0], numbers[1], numbers[2], numbers[3]);
    }

    /// <summary>Raise one over that rectangle.</summary>
    /// <param name="over">Where it goes, in physical pixels.</param>
    public static Window Raise((int Left, int Top, int Width, int Height) over)
    {
        var window = new Window
        {
            Title = "",
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ResizeMode = ResizeMode.NoResize,
            Topmost = true,
            ShowActivated = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Background = new SolidColorBrush(Color.FromRgb(0xb3, 0x26, 0x1e)),
            Content = new TextBlock
            {
                Name = "intruderText",
                Text = "in the way",
                Margin = new Thickness(8),
                Foreground = new SolidColorBrush(Colors.White),
            },
        };

        window.SourceInitialized += (_, _) =>
        {
            // Placed in the space the caller named it in. Setting Left and Top instead would put it
            // at half the asked position on a display at two hundred percent, and the check reading
            // the result would be right about a window nobody meant to put there.
            var handle = new WindowInteropHelper(window).Handle;
            SetWindowPos(handle, Topmost, over.Left, over.Top, over.Width, over.Height, NoActivate | ShowWindow);
        };

        window.Show();
        return window;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
}

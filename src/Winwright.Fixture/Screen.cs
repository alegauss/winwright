using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace Winwright.Fixture;

/// <summary>
/// A window in full screen: no frame, nothing but a terminal, and the primary display either covered
/// or covered all but a border. WW548.
/// <para>
/// Found by quickshell (QS257): F11 took the frame, the strip and the bars away, and a case could read
/// only that they had gone, because no claim compared an element with its monitor. A full screen
/// leaving a border uncovered passed. Both shapes are here because the pass is as much of the claim
/// as the red: <c>fill</c> covers the monitor to the pixel and <c>short</c> stops eight pixels short
/// of its right and bottom edges, which is the border a size taken from the work area would leave.
/// </para>
/// <para>
/// The terminal is a <see cref="TextBox"/> because a bare panel has no automation peer and no locator
/// reaches it, and the window cannot be resized because a resizable chromeless window keeps a sizing
/// border inside its rectangle, which the terminal would then stop short of on a full screen too.
/// </para>
/// </summary>
public static class Screen
{
    /// <summary>How many pixels <c>short</c> leaves uncovered at the right and at the bottom.</summary>
    public const int ShortBy = 8;

    /// <summary>SWP_NOZORDER | SWP_NOACTIVATE: move and size, and take nothing else.</summary>
    private const uint MoveOnly = 0x0004 | 0x0010;

    /// <summary>SM_CXSCREEN and SM_CYSCREEN: the primary display's size, in pixels.</summary>
    private const int Wide = 0;
    private const int High = 1;

    /// <summary>The names the flag takes, in the order the catalogue prints them.</summary>
    public static IReadOnlyList<string> Names { get; } = ["fill", "short"];

    /// <summary>
    /// Take the frame away and leave the terminal alone in the window. Before the source exists,
    /// because the style a window has no frame by is the one it is created with.
    /// </summary>
    /// <param name="window">The window, not yet shown.</param>
    public static void Strip(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        window.WindowStyle = WindowStyle.None;
        window.ResizeMode = ResizeMode.NoResize;
        window.Content = new TextBox
        {
            Name = "screenTerminal",
            Text = "full screen",
            AcceptsReturn = true,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0),
        };
    }

    /// <summary>
    /// Size the window to the primary display, or to all of it but a border, the way the flag names.
    /// The primary one and never the display the window is on, because asking which display that is
    /// is the multi-display question Block K's criterion says the fixture never puts to a machine.
    /// </summary>
    /// <param name="window">The window handle, which exists only once the source is initialised.</param>
    /// <param name="named">One of <see cref="Names"/>.</param>
    public static void Size(nint window, string named)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(named);

        var by = named.Trim() switch
        {
            "fill" => 0,
            "short" => ShortBy,
            _ => throw new ArgumentException($"'{named}' is not a screen this fixture knows", nameof(named)),
        };

        // The primary display's origin is the desk's, always.
        SetWindowPos(window, 0, 0, 0, GetSystemMetrics(Wide) - by, GetSystemMetrics(High) - by, MoveOnly);
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
}

using System.Runtime.InteropServices;

namespace Winwright.Fixture;

/// <summary>
/// A window made see-through by its layer rather than by its backdrop. WW334.
/// <para>
/// The backdrop shape beside this one covers the way the compositor is asked, and it is the way a
/// Fluent window does it. Layering is the other way, it is older, and nothing about it answers the
/// backdrop question: a layered window reports the auto backdrop, truthfully, while being as much a
/// window on to the desktop as any acrylic one.
/// </para>
/// <para>
/// Measured beside freewilly's menu, which is where the task came from. The <c>SysShadow</c> window
/// Windows draws behind a drop-shadowed popup is layered per pixel, and a copy of its rectangle is
/// a copy of whatever the menu is standing in front of — while every route in the engine calls it a
/// popup, which is exactly what the menu beside it is.
/// </para>
/// <para>
/// Both readable kinds, because they are refused for different reasons and one of them is a trap: a
/// window layered at full alpha with no colour key is composited and is opaque anyway, so a check
/// that refused every layered window would refuse a window that hides what is behind it perfectly.
/// The pass is as much of the shape as the refusal.
/// </para>
/// </summary>
public static class Layered
{
    /// <summary>GWL_EXSTYLE, which is where WS_EX_LAYERED is.</summary>
    private const int ExtendedStyle = -20;

    /// <summary>WS_EX_LAYERED.</summary>
    private const long LayeredStyle = 0x0008_0000L;

    /// <summary>LWA_COLORKEY: the colour the window draws where the desktop should show.</summary>
    private const uint ByColourKey = 0x0000_0001;

    /// <summary>LWA_ALPHA: one alpha for the whole window.</summary>
    private const uint ByAlpha = 0x0000_0002;

    /// <summary>The names the flag takes, in the order the catalogue prints them.</summary>
    public static IReadOnlyList<string> Names { get; } = ["none", "half", "opaque", "keyed"];

    /// <summary>
    /// Layer a window the way the flag names, and read back what it is set to.
    /// </summary>
    /// <param name="window">The window handle, which exists only once the source is initialised.</param>
    /// <param name="named">One of <see cref="Names"/>.</param>
    /// <returns>
    /// Whether the window carries the layered style afterwards. Read back rather than assumed, for
    /// the reason <see cref="Backdrop.Set" /> gives about its own: asking and having are different
    /// claims, and a fixture that said otherwise would be lying to the check it exists to feed.
    /// </returns>
    public static bool Set(nint window, string named)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(named);

        var wanted = named.Trim();
        if (wanted == "none")
            return Carries(window);

        var style = (long)GetWindowLongPtrW(window, ExtendedStyle);
        SetWindowLongPtrW(window, ExtendedStyle, (nint)(style | LayeredStyle));

        // Half is a fixed number rather than a swept one: what is under test is that an alpha below
        // full is refused, and one below full is one below full. A run that wanted to know where the
        // threshold is would be asking about the check rather than about the window.
        switch (wanted)
        {
            case "half":
                SetLayeredWindowAttributes(window, 0, 128, ByAlpha);
                break;

            case "opaque":
                SetLayeredWindowAttributes(window, 0, 255, ByAlpha);
                break;

            case "keyed":
                // Magenta, because a colour key has to be a colour the window does not otherwise
                // draw — every pixel of it becomes the desktop, and a key on a colour the content
                // uses would take the content with it.
                SetLayeredWindowAttributes(window, 0x00FF00FF, 255, ByColourKey);
                break;

            default:
                throw new ArgumentException($"'{named}' is not a layer this fixture knows", nameof(named));
        }

        return Carries(window);
    }

    /// <summary>Whether the window carries the layered style at all.</summary>
    /// <param name="window">The window handle.</param>
    private static bool Carries(nint window) =>
        ((long)GetWindowLongPtrW(window, ExtendedStyle) & LayeredStyle) != 0;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint GetWindowLongPtrW(nint window, int index);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowLongPtrW(nint window, int index, nint value);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetLayeredWindowAttributes(nint window, uint key, byte alpha, uint flags);
}

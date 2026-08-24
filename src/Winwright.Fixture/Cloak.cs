using System.Runtime.InteropServices;

namespace Winwright.Fixture;

/// <summary>
/// A window the application has asked the compositor to stop drawing, on demand.
/// <para>
/// WW199. A capture of a cloaked window is a blank file, and the receipt refuses one — but nothing
/// provoked that refusal. The pairing said a cloaked window "is a state the compositor puts a window
/// into", and half of that is wrong: <c>DWMWA_CLOAK</c> is what a suspended packaged application
/// sets on itself, which is why the reading has a <c>ByTheApplication</c> arm at all. So the fixture
/// can be one, and an argument becomes a shape.
/// </para>
/// <para>
/// It matters because the window keeps every style bit saying it is visible. Measured on a stock
/// Windows 11 desktop: 27 windows report themselves visible and 12 are cloaked, 7 at their own
/// application's request. Nearly half a listing is windows nobody can see, and a capture of any of
/// them is a file that looks like a capture.
/// </para>
/// </summary>
public static class Cloak
{
    /// <summary>DWMWA_CLOAK. The attribute an application sets to take itself off the screen.</summary>
    private const int CloakAttribute = 13;

    /// <summary>DWMWA_CLOAKED. What the compositor answers about who took it off.</summary>
    private const int CloakedAttribute = 14;

    /// <summary>The application asked for it, which is the arm this shape exists to produce.</summary>
    public const int ByTheApplication = 0x1;

    /// <summary>
    /// Ask the compositor to stop drawing a window, and read back who it says cloaked it.
    /// </summary>
    /// <param name="window">The window handle, which exists only once the source is initialised.</param>
    /// <returns>
    /// What the compositor reports afterwards. Read back rather than assumed, for the reason
    /// <see cref="Backdrop.Set" /> gives about its own: asking and having are different claims, and
    /// a fixture that said otherwise would be lying to the check it exists to feed.
    /// </returns>
    public static int Set(nint window)
    {
        var wanted = 1;
        DwmSetWindowAttribute(window, CloakAttribute, ref wanted, sizeof(int));

        return DwmGetWindowAttribute(window, CloakedAttribute, out var by, sizeof(int)) == 0 ? by : -1;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);
}

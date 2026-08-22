using System.Runtime.InteropServices;

namespace Winwright.Fixture;

/// <summary>
/// The system backdrop a window opted into, on demand.
/// <para>
/// A refusal with only one arm tested is half a check: it can be right about the window it refuses
/// and wrong about everything it lets through. This gives the fixture both — a window that asked
/// the compositor for a backdrop and one that never did — so the refusal and the pass beside it
/// are driven rather than reasoned about.
/// </para>
/// <para>
/// It matters because z-order reasoning cannot answer for a backdrop. A window with one transmits
/// what is behind it through the glass, and every check that decides what a copy contains by
/// walking the windows above it is simply wrong about that window — while the picture looks
/// entirely ordinary.
/// </para>
/// </summary>
public static class Backdrop
{
    /// <summary>DWMWA_SYSTEMBACKDROP_TYPE. The one attribute that says what the glass is doing.</summary>
    private const int SystemBackdropType = 38;

    /// <summary>What a window that never asked reports: the compositor decides, and it decides none.</summary>
    public const int Auto = 0;

    /// <summary>Asked for, and asked for nothing — which is not the same as never having asked.</summary>
    public const int None = 1;

    /// <summary>Read what a window is set to. Negative where the compositor would not say.</summary>
    /// <param name="window">The window handle.</param>
    public static int Of(nint window)
    {
        var read = 0;
        return DwmGetWindowAttribute(window, SystemBackdropType, out read, sizeof(int)) == 0 ? read : -1;
    }

    /// <summary>
    /// Opt a window into a backdrop by name.
    /// </summary>
    /// <param name="window">The window handle, which exists only once the source is initialised.</param>
    /// <param name="named">One of the names the flag accepts.</param>
    /// <returns>What the compositor reports afterwards, read back rather than assumed.</returns>
    public static int Set(nint window, string named)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(named);

        var wanted = Value(named);
        DwmSetWindowAttribute(window, SystemBackdropType, ref wanted, sizeof(int));

        // Read back, because asking for a backdrop and having one are different claims: an older
        // build accepts the call and reports auto, and a fixture that said otherwise would be
        // lying to the check it exists to feed.
        return Of(window);
    }

    /// <summary>The names the flag takes, in the order the catalogue prints them.</summary>
    public static IReadOnlyList<string> Names { get; } = ["none", "mica", "acrylic", "tabbed"];

    private static int Value(string named) => named.Trim() switch
    {
        "none" => None,
        "mica" => 2,
        "acrylic" => 3,
        "tabbed" => 4,
        _ => throw new ArgumentException($"'{named}' is not a backdrop this fixture knows", nameof(named)),
    };

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);
}

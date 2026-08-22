using System.Runtime.InteropServices;

namespace Winwright.Tests;

/// <summary>
/// Where this suite puts a window nobody is meant to look at.
/// <para>
/// Creating a top-level window with WS_VISIBLE activates it, so every fixture here that needs a
/// visible window takes the foreground for as long as it lives. On a developer machine that is a
/// flash over whatever was being typed into, several times a run.
/// </para>
/// <para>
/// It is not test hygiene. This block's theme is leaving nothing behind, and a foreground handed
/// to a window that has since been destroyed is something left behind — and the tool <em>measures
/// the foreground</em>, so a suite that moves it is one whose own readings of it are taken on a
/// desk the suite disturbed. That is a test agreeing with itself.
/// </para>
/// <para>
/// A window placed past the right edge of every monitor is still visible to the enumeration under
/// test and invisible to the person at the keyboard. A test that genuinely needs one on screen
/// says so and places it deliberately, which is the difference between a decision and forty by
/// forty being the first pair of numbers somebody typed.
/// </para>
/// </summary>
public static class OffScreen
{
    private const int VirtualScreenX = 76;
    private const int VirtualScreenWidth = 78;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    /// <summary>
    /// The left edge to create at: past the right of every monitor, with a margin so a window as
    /// wide as any fixture makes still has none of itself on any of them.
    /// </summary>
    public static int Left { get; } = GetSystemMetrics(VirtualScreenX) + GetSystemMetrics(VirtualScreenWidth) + 200;

    /// <summary>The top edge to create at. Vertically ordinary, because the horizontal move is enough.</summary>
    public static int Top => 100;
}

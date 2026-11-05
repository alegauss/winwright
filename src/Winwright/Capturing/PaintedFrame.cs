using System.Runtime.InteropServices;

using Winwright.Windowing;

namespace Winwright.Capturing;

/// <summary>
/// The rectangle a window owns against the one it paints.
/// <para>
/// The window rectangle spans a window's invisible resize border and its drop-shadow margin, so
/// every copy of it carries a strip of whatever is behind the window down its edges — measured in
/// claude-tray as slivers of the editor along the left, the right and the bottom of every capture.
/// </para>
/// <para>
/// Measured here on an ordinary overlapped window: 600 by 400 owned against 578 by 389 painted,
/// trimming eleven columns from each side and eleven rows from the bottom and <em>none</em> from
/// the top — the top has no invisible border to spare. A popup with no sizing border trims nothing
/// at all, which is why the number is reported rather than assumed to be there.
/// </para>
/// </summary>
public sealed record PaintedFrame
{
    private const uint ExtendedFrameBounds = 9;

    private PaintedFrame(WindowBounds owned, WindowBounds painted, string? because)
    {
        Owned = owned;
        Painted = painted;
        Because = because;
    }

    /// <summary>What the window rectangle says it occupies, border and shadow included.</summary>
    public WindowBounds Owned { get; }

    /// <summary>What it actually paints, which is what a capture should copy.</summary>
    public WindowBounds Painted { get; }

    /// <summary>Why the painted frame could not be read, where it could not.</summary>
    public string? Because { get; }

    /// <summary>Columns of desktop down the left edge.</summary>
    public int TrimmedLeft => Painted.Left - Owned.Left;

    /// <summary>Rows of desktop along the top. Usually none: there is no invisible border there.</summary>
    public int TrimmedTop => Painted.Top - Owned.Top;

    /// <summary>Columns of desktop down the right edge.</summary>
    public int TrimmedRight => Owned.Right - Painted.Right;

    /// <summary>Rows of desktop along the bottom.</summary>
    public int TrimmedBottom => Owned.Bottom - Painted.Bottom;

    /// <summary>Whether anything was trimmed at all.</summary>
    public bool Trimmed => TrimmedLeft != 0 || TrimmedTop != 0 || TrimmedRight != 0 || TrimmedBottom != 0;

    /// <summary>
    /// Read both rectangles for a window. Null where the handle names no window; where the
    /// compositor will not answer, the painted frame is the owned one and the reason says so —
    /// a capture of the whole rectangle being better than no capture, provided nobody is misled.
    /// </summary>
    public static PaintedFrame? Of(nint window)
    {
        if (window == 0 || !Win32.GetWindowRect(window, out var owned))
            return null;

        var ownedBounds = new WindowBounds(owned.Left, owned.Top, owned.Right, owned.Bottom);
        var result = DwmGetWindowAttribute(window, ExtendedFrameBounds, out var painted, Marshal.SizeOf<Win32.Rect>());
        if (result != 0)
            return new PaintedFrame(ownedBounds, ownedBounds, $"the compositor answered 0x{result:X}");

        return new PaintedFrame(
            ownedBounds, new WindowBounds(painted.Left, painted.Top, painted.Right, painted.Bottom), null);
    }

    /// <summary>
    /// What was copied and what was left out. Printed whatever the answer is, because nobody can
    /// see the difference by looking at one file — a sliver of somebody else's editor down the
    /// left edge reads as part of the application until two captures are put side by side.
    /// </summary>
    public string Sentence()
    {
        if (Because is not null)
            return $"the painted frame could not be read ({Because}), so the whole window rectangle "
                + $"{Owned} was copied.";

        return Trimmed
            ? $"the painted frame is {Painted}, trimmed from {Owned} by "
                + $"left {TrimmedLeft}, top {TrimmedTop}, right {TrimmedRight}, bottom {TrimmedBottom}."
            : $"the painted frame is {Painted}, which is the whole window rectangle: nothing to trim.";
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(nint window, uint attribute, out Win32.Rect value, int size);
}

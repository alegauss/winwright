namespace Winwright.Windowing;

/// <summary>A window's rectangle in screen coordinates, as Windows reports it.</summary>
/// <param name="Left">Left edge.</param>
/// <param name="Top">Top edge.</param>
/// <param name="Right">Right edge, exclusive.</param>
/// <param name="Bottom">Bottom edge, exclusive.</param>
public readonly record struct WindowBounds(int Left, int Top, int Right, int Bottom)
{
    /// <summary>How wide it is.</summary>
    public int Width => Right - Left;

    /// <summary>How tall it is.</summary>
    public int Height => Bottom - Top;

    /// <summary>How much of the screen it covers, which is how the main window is picked out.</summary>
    public long Area => (long)Math.Max(0, Width) * Math.Max(0, Height);

    /// <summary>The rectangle as a summary prints it.</summary>
    public override string ToString() => $"{Width}x{Height} at {Left},{Top}";
}

/// <summary>
/// A top-level window a process owns. A borderless toast, a balloon or a menu is one of these,
/// and the process object reports none of them — which is why they are enumerated by process id
/// rather than asked for by the one handle a process is willing to name.
/// </summary>
/// <param name="Handle">The window handle, which every verb addresses it by.</param>
/// <param name="Pid">The process that owns it.</param>
/// <param name="Title">Its caption, empty where it has none — a toast usually has none.</param>
/// <param name="ClassName">Its window class, which is what tells a menu from a frame.</param>
/// <param name="Bounds">Where it is, in screen coordinates.</param>
/// <param name="Visible">Whether Windows considers it visible, which is a reading of style bits.</param>
/// <param name="Owner">The window that owns it, or zero. A toast has one; a main frame does not.</param>
/// <param name="Cloak">Who took it off the screen, if anybody. Carried separately from
/// <paramref name="Visible"/> because the two disagree: a cloaked window keeps every style bit
/// that says it is visible.</param>
/// <param name="Popup">
/// Whether a framework drew it as a popup — WS_POPUP with no caption. WW87: <paramref name="Owner"/>
/// answers a toast and a menu and does not answer a drop-down that nothing owns, which is what a
/// context menu shown with no window behind it is. False where nobody read the style bits, which is
/// every window composed by hand in a test.
/// </param>
public sealed record TopLevelWindow(
    nint Handle,
    int Pid,
    string Title,
    string ClassName,
    WindowBounds Bounds,
    bool Visible,
    nint Owner,
    Cloak Cloak = Cloak.NotCloaked,
    bool Popup = false)
{
    /// <summary>Whether something else owns it, which is exactly what the launcher's handle skips.</summary>
    public bool IsOwned => Owner != 0;

    /// <summary>
    /// Whether a person could see it: visible by its style bits <em>and</em> painted by the
    /// compositor. This is what "visible" means everywhere outside this record.
    /// </summary>
    public bool OnScreen => Visible && Cloak == Cloak.NotCloaked;

    /// <summary>The one line a trace or a summary shows.</summary>
    public override string ToString()
    {
        var named = string.IsNullOrEmpty(Title) ? "(untitled)" : $"'{Title}'";
        var owned = IsOwned ? ", owned" : "";

        // Said out loud whenever it is true, because the rectangle beside it looks perfectly
        // ordinary and nothing else in the line would tell a reader the window is not there.
        var hidden = Cloak == Cloak.NotCloaked ? "" : $", cloaked ({Cloaking.Because(Cloak)})";
        return $"{ClassName} {named} [{Bounds}]{owned}{hidden}";
    }
}

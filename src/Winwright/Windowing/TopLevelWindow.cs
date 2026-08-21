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
/// <param name="Visible">Whether Windows considers it visible.</param>
/// <param name="Owner">The window that owns it, or zero. A toast has one; a main frame does not.</param>
public sealed record TopLevelWindow(
    nint Handle,
    int Pid,
    string Title,
    string ClassName,
    WindowBounds Bounds,
    bool Visible,
    nint Owner)
{
    /// <summary>Whether something else owns it, which is exactly what the launcher's handle skips.</summary>
    public bool IsOwned => Owner != 0;

    /// <summary>The one line a trace or a summary shows.</summary>
    public override string ToString()
    {
        var named = string.IsNullOrEmpty(Title) ? "(untitled)" : $"'{Title}'";
        var owned = IsOwned ? ", owned" : "";
        return $"{ClassName} {named} [{Bounds}]{owned}";
    }
}

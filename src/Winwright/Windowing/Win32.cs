using System.Runtime.InteropServices;
using System.Text;

namespace Winwright.Windowing;

/// <summary>
/// The user32 entry points this project reaches for. They are in-box, which is why the engine
/// needs no package to see a window the process object does not report.
/// </summary>
internal static class Win32
{
    internal delegate bool EnumWindowsProc(nint window, nint parameter);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumWindows(EnumWindowsProc callback, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint GetWindowThreadProcessId(nint window, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindowVisible(nint window);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetWindowRect(nint window, out Rect rectangle);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int GetWindowTextW(nint window, StringBuilder text, int capacity);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int GetClassNameW(nint window, StringBuilder name, int capacity);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern nint GetWindow(nint window, uint command);

    [DllImport("user32.dll")]
    internal static extern nint GetForegroundWindow();

    /// <summary>GW_OWNER: the window that owns this one, which is what a toast or a menu has.</summary>
    internal const uint GwOwner = 4;

    internal static string TextOf(nint window)
    {
        var text = new StringBuilder(512);
        var read = GetWindowTextW(window, text, text.Capacity);
        return read > 0 ? text.ToString() : "";
    }

    internal static string ClassOf(nint window)
    {
        var name = new StringBuilder(256);
        var read = GetClassNameW(window, name, name.Capacity);
        return read > 0 ? name.ToString() : "";
    }
}

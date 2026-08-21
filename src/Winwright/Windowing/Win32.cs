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

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern nint GetAncestor(nint window, uint what);

    /// <summary>GA_ROOT: the top-level window a control belongs to, which is what owns a foreground.</summary>
    internal const uint GaRoot = 2;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MouseInput
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public nint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Input
    {
        public uint Type;
        public MouseInput Mouse;
    }

    internal const uint InputMouse = 0;
    internal const uint MouseMove = 0x0001;
    internal const uint MouseAbsolute = 0x8000;
    internal const uint MouseVirtualDesk = 0x4000;
    internal const uint MouseLeftDown = 0x0002;
    internal const uint MouseLeftUp = 0x0004;
    internal const uint MouseRightDown = 0x0008;
    internal const uint MouseRightUp = 0x0010;
    internal const uint MouseMiddleDown = 0x0020;
    internal const uint MouseMiddleUp = 0x0040;

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint SendInput(uint count, Input[] inputs, int size);

    [DllImport("user32.dll")]
    internal static extern int GetSystemMetrics(int index);

    internal const int VirtualScreenX = 76;
    internal const int VirtualScreenY = 77;
    internal const int VirtualScreenWidth = 78;
    internal const int VirtualScreenHeight = 79;

    [StructLayout(LayoutKind.Sequential)]
    internal struct Point
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetCursorPos(int x, int y);

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

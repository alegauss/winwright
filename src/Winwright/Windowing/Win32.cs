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

    /// <summary>
    /// WW330. Best effort and known to be: Windows refuses this to a process that does not already
    /// own the foreground, so what it is for is putting back a desk this run itself took — where the
    /// refusal is unlikely and the alternative is leaving the taskbar selected for the next run.
    /// </summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(nint window);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern nint FindWindowW(string? className, string? title);

    /// <summary>
    /// What <c>WM_COPYDATA</c> carries: a tag, a length and a buffer. WW349.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct CopyData
    {
        public nint Data;
        public int Size;
        public nint Buffer;
    }

    /// <summary>WM_COPYDATA, which is how a string crosses to another process with Windows marshalling it.</summary>
    internal const uint WmCopyData = 0x004A;

    /// <summary>SMTO_ABORTIFHUNG, so a wedged application is a reading rather than a run that stopped.</summary>
    internal const uint AbortIfHung = 0x0002;

    /// <summary>
    /// Sent rather than posted, and with a budget. WW349: the answer is the whole point of the ask,
    /// so the call waits for the other window's thread to produce one.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern nint SendMessageTimeoutW(
        nint window, uint message, nint wParam, ref CopyData lParam, uint flags, uint timeoutMs, out nint answer);

    /// <summary>
    /// The same number in every process for the same string, and one nobody else can collide with —
    /// which is the promise a magic constant cannot make. WW349.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint RegisterWindowMessageW([MarshalAs(UnmanagedType.LPWStr)] string name);

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
    internal struct KeyInput
    {
        public ushort VirtualKey;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public nint ExtraInfo;
    }

    /// <summary>INPUT is a union, and writing it as one is what keeps its size right on x64.</summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct InputPayload
    {
        [FieldOffset(0)]
        public MouseInput Mouse;

        [FieldOffset(0)]
        public KeyInput Key;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Input
    {
        public uint Type;
        public InputPayload Payload;
    }

    internal const uint InputMouse = 0;
    internal const uint InputKeyboard = 1;

    internal const uint KeyUp = 0x0002;
    internal const uint KeyUnicode = 0x0004;

    internal const ushort VkBack = 0x08;
    internal const ushort VkEnd = 0x23;
    internal const uint MouseMove = 0x0001;
    internal const uint MouseAbsolute = 0x8000;
    internal const uint MouseVirtualDesk = 0x4000;
    internal const uint MouseLeftDown = 0x0002;
    internal const uint MouseLeftUp = 0x0004;
    internal const uint MouseRightDown = 0x0008;
    internal const uint MouseRightUp = 0x0010;
    internal const uint MouseMiddleDown = 0x0020;
    internal const uint MouseMiddleUp = 0x0040;

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "SendInput")]
    private static extern uint SendInputRaw(uint count, Input[] inputs, int size);

    /// <summary>
    /// Synthesise input, and record that this run was the one that did it.
    /// <para>
    /// WW157. The stamp is here rather than at the three call sites because here is the only place
    /// input leaves this process: a fourth caller added later gets the stamp by construction, and
    /// one that had to remember would eventually be the one that forgot — at which point the
    /// reading stops noticing the operator and nobody finds out.
    /// </para>
    /// <para>
    /// Stamped before the call and not after. SendInput returns once the events are queued, so a
    /// long batch would leave a window in which the operating system has already recorded the
    /// input and this run has not yet claimed it, which reads exactly like a person.
    /// </para>
    /// </summary>
    internal static uint SendInput(uint count, Input[] inputs, int size)
    {
        ForeignInput.Sent();
        return SendInputRaw(count, inputs, size);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LastInput
    {
        internal uint Size;
        internal uint Ticks;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetLastInputInfo(ref LastInput info);

    [DllImport("user32.dll")]
    internal static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll")]
    internal static extern uint MapVirtualKeyW(uint code, uint mapping);

    /// <summary>MAPVK_VK_TO_VSC: the scan code a virtual key has on the layout in force.</summary>
    internal const uint VirtualKeyToScan = 0;

    internal const int VirtualScreenX = 76;
    internal const int VirtualScreenY = 77;
    internal const int VirtualScreenWidth = 78;
    internal const int VirtualScreenHeight = 79;

    /// <summary>SM_CMONITORS: how many displays are attached. Zero is a desk that draws nothing.</summary>
    internal const int MonitorCount = 80;

    /// <summary>SM_REMOTESESSION: whether this session is being served over a remote connection.</summary>
    internal const int RemoteSession = 0x1000;

    /// <summary>
    /// WW158. Whether the desktop is being composed at all. A desk that is not composing draws
    /// nothing, whatever the monitor count says about displays being attached.
    /// </summary>
    [DllImport("dwmapi.dll")]
    internal static extern int DwmIsCompositionEnabled([MarshalAs(UnmanagedType.Bool)] out bool enabled);

    /// <summary>WTS_CURRENT_SERVER_HANDLE: this machine, which is the only one this ever asks about.</summary>
    internal static readonly nint CurrentServer = 0;

    /// <summary>WTS_CURRENT_SESSION: the session this process is in.</summary>
    internal const uint CurrentSession = unchecked((uint)-1);

    /// <summary>WTSConnectState: what the session is doing, which is the fact a monitor count proxies for.</summary>
    internal const int SessionConnectState = 8;

    /// <summary>
    /// WTS_CONNECTSTATE_CLASS, in the order the header declares. Only two of them mean a desk that
    /// somebody could be looking at.
    /// </summary>
    internal enum ConnectState
    {
        Active = 0,
        Connected = 1,
        ConnectQuery = 2,
        Shadow = 3,
        Disconnected = 4,
        Idle = 5,
        Listen = 6,
        Reset = 7,
        Down = 8,
        Init = 9,
    }

    [DllImport("wtsapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool WTSQuerySessionInformationW(
        nint server, uint session, int what, out nint buffer, out uint bytes);

    [DllImport("wtsapi32.dll")]
    internal static extern void WTSFreeMemory(nint memory);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern nint GetProcessWindowStation();

    /// <summary>UOI_FLAGS: the flags of a window station or desktop, which is where WSF_VISIBLE lives.</summary>
    internal const int UserObjectFlags = 1;

    /// <summary>WSF_VISIBLE: the station has a visible desktop. A service's station does not.</summary>
    internal const int StationVisible = 0x0001;

    [StructLayout(LayoutKind.Sequential)]
    internal struct UserObjectFlagsInfo
    {
        public int Inherit;
        public int Reserved;
        public int Flags;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetUserObjectInformationW(
        nint handle, int index, out UserObjectFlagsInfo info, int length, out int needed);

    /// <summary>DESKTOP_READOBJECTS: the least this run can ask for and still have opened the desktop.</summary>
    internal const uint DesktopReadObjects = 0x0001;

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern nint OpenInputDesktop(uint flags, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint access);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseDesktop(nint desktop);

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

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLongPtrW")]
    internal static extern nint GetWindowLongPtrW(nint window, int index);

    /// <summary>GWL_STYLE: the style bits, which is where WS_POPUP and WS_CAPTION are.</summary>
    internal const int WindowStyle = -16;

    /// <summary>WS_POPUP: the window is a popup rather than something with a frame of its own.</summary>
    internal const long StylePopup = 0x8000_0000L;

    /// <summary>
    /// WS_CAPTION: it has a title bar, which is the thing a popup does not have. Two bits and not
    /// one — WS_BORDER and WS_DLGFRAME together — which is why the test below is against the whole
    /// mask rather than against any bit of it.
    /// </summary>
    internal const long StyleCaption = 0x00C0_0000L;

    /// <summary>
    /// Whether a window is drawn as a popup: WS_POPUP, and not a title bar.
    /// <para>
    /// WW87. The ownership question answers a menu and a toast and does not answer a drop-down that
    /// nothing owns. Measured in freewilly, whose menu verb shows a context menu with no window
    /// behind it: the menu came back <c>WindowsForms10.Window.20808.app.0.5c39d4_r3_ad1</c>, owner 0,
    /// style 0x96000000 — WS_POPUP and no WS_CAPTION. The class name carries a per-thread number in
    /// it, so it is not something a rule can match on; the style bits are what the framework actually
    /// set.
    /// </para>
    /// <para>
    /// WS_EX_TOOLWINDOW would have been the tighter rule and is not one: measured on the same menu it
    /// is clear, and set on the SysShadow window drawn behind it. So the caption is the discriminator
    /// — a window with a title bar is a window somebody sized and moved, and a window without one
    /// that also declares itself a popup is a surface a framework put up.
    /// </para>
    /// <para>
    /// The test is <c>!= StyleCaption</c> and not <c>== 0</c>, which is a measurement rather than a
    /// nicety: the first draft asked whether any caption bit was set, and a real <c>#32768</c> menu
    /// on the guest carries WS_BORDER without WS_DLGFRAME. So the one window the whole route exists
    /// for read as a window with a title bar, and the suite said so.
    /// </para>
    /// </summary>
    /// <param name="window">The window to ask about.</param>
    internal static bool IsPopup(nint window)
    {
        var style = (long)GetWindowLongPtrW(window, WindowStyle);
        return (style & StylePopup) != 0 && (style & StyleCaption) != StyleCaption;
    }

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

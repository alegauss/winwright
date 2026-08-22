using System.Runtime.InteropServices;

namespace Winwright.Tests;

/// <summary>
/// A real notification-area icon, owned by this run and taken away again.
/// <para>
/// Measured: an icon added now goes into the overflow rather than onto the taskbar, which is
/// exactly the case the task is about — it is not in the tree at all until the flyout is opened.
/// </para>
/// </summary>
internal sealed class TrayIconFixture : IDisposable
{
    private const uint WsPopup = 0x80000000;
    private const uint WmQuit = 0x0012;
    private const uint NimAdd = 0;
    private const uint NimDelete = 2;
    private const uint NifMessage = 0x01;
    private const uint NifIcon = 0x02;
    private const uint NifTip = 0x04;
    private const nint IdiApplication = 32512;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    [DllImport("user32.dll")]
    private static extern nint LoadIconW(nint instance, nint name);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public int Size;
        public nint Window;
        public uint Id;
        public uint Flags;
        public uint CallbackMessage;
        public nint Icon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Tip;

        public uint State;
        public uint StateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Info;

        public uint Version;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string InfoTitle;

        public uint InfoFlags;
        public Guid Item;
        public nint BalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIconW(uint message, ref NotifyIconData data);

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public nint Window;
        public uint Message;
        public nint WParam;
        public nint LParam;
        public uint Time;
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern int GetMessageW(out Msg message, nint window, uint first, uint last);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(ref Msg message);

    [DllImport("user32.dll")]
    private static extern nint DispatchMessageW(ref Msg message);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostThreadMessageW(uint thread, uint message, nint wParam, nint lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    private readonly Thread thread;
    private uint threadId;
    private nint owner;

    private TrayIconFixture(string tip)
    {
        // WW126: the tip carries this process, because a run that was killed leaves its icon
        // registered with the shell and nothing in this process can delete somebody else's. A
        // ghost from a previous run is then found by tip and read as this run's own, which is why
        // the suite got flakier the more times it was run rather than staying equally flaky.
        Tip = $"{tip} #{Environment.ProcessId}";
        using var ready = new ManualResetEventSlim();
        var added = false;

        thread = new Thread(() =>
        {
            threadId = GetCurrentThreadId();
            owner = CreateWindowExW(0, "Static", "winwright tray owner", WsPopup, 0, 0, 10, 10, 0, 0, 0, 0);
            var data = Describe();
            added = Shell_NotifyIconW(NimAdd, ref data);
            ready.Set();

            while (GetMessageW(out var message, 0, 0, 0) > 0)
            {
                TranslateMessage(ref message);
                DispatchMessageW(ref message);
            }

            var going = Describe();
            Shell_NotifyIconW(NimDelete, ref going);
            DestroyWindow(owner);
        })
        {
            IsBackground = true,
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!ready.Wait(TimeSpan.FromSeconds(10)) || !added)
            throw new InvalidOperationException("the tray icon was never added");
    }

    /// <summary>What the shell will call it.</summary>
    internal string Tip { get; }

    /// <summary>
    /// Add one, blocking until the shell has it. The tip it ends up with carries this process, so
    /// ask this object rather than passing the same string to a reading.
    /// </summary>
    internal static TrayIconFixture Add(string tip) => new(tip);

    /// <summary>Take it away, and the window that owned it.</summary>
    public void Dispose()
    {
        if (threadId != 0)
            PostThreadMessageW(threadId, WmQuit, 0, 0);

        thread.Join(TimeSpan.FromSeconds(5));
    }

    private NotifyIconData Describe() => new()
    {
        Size = Marshal.SizeOf<NotifyIconData>(),
        Window = owner,
        Id = 1,
        Flags = NifMessage | NifIcon | NifTip,
        CallbackMessage = 0x0400 + 1,
        Icon = LoadIconW(0, IdiApplication),
        Tip = Tip,
        Info = "",
        InfoTitle = "",
    };
}

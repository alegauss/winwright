using System.Runtime.InteropServices;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Projects;

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

    /// <summary>
    /// How long the shell gets to place an icon it has already accepted. The engine's own resolve
    /// deadline, because that is what this is: waiting for something to turn up in a tree.
    /// </summary>
    private static readonly int PlacedMs = Timeouts.Defaults["resolve"];

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

        Placed();
    }

    /// <summary>
    /// Wait until the shell has put the icon somewhere a reading can find it.
    /// <para>
    /// WW119. <c>Shell_NotifyIconW</c> returning true means the shell accepted the message, and
    /// this fixture used to return on that while claiming it blocked until the shell had it. Those
    /// are different claims: placing the icon and building the automation tree under it happens
    /// afterwards, on the shell's own schedule, so a test that looked immediately was racing it.
    /// Measured across four consecutive full-suite runs on an untouched machine — two green, two
    /// red with two failures each, every one of them in the notification-area cases.
    /// </para>
    /// <para>
    /// The wait is a deadline on the condition itself and not a sleep, because a sleep long enough
    /// for a busy machine is one every other run pays for and still loses on the machine after
    /// that.
    /// </para>
    /// </summary>
    private void Placed()
    {
        // WW168: the search answers a reading now, and a reading is never null — so the icon itself
        // is what this waits on. Handing Attempt.Until the search would have made every deadline
        // here one look long, since the first look already answers something.
        var last = default(TraySearch);
        var found = Attempt.Until(
            () =>
            {
                last = NotificationArea.Find(Tip, openingTheOverflow: true, settleMs: 1000, pollMs: 25);
                return last.Icon;
            },
            PlacedMs,
            pollMs: 50);

        // Looking may have opened the overflow, so it is shut again. What this fixture promises is
        // an icon that can be found, never a flyout left standing for the next case to trip on —
        // and one of the two flakes measured was exactly a case that found one already open.
        NotificationArea.CloseOverflow();

        if (!found.Found)
        {
            // The last search's own reason, which is the half this used to drop. A shell that would
            // not open the flyout and a shell that took the icon and placed it nowhere both ended
            // this sentence the same way, and the difference is the one a reader needs.
            throw new InvalidOperationException(
                $"the shell took '{Tip}' and never put it anywhere a reading could find it within "
                + $"{PlacedMs} ms, so nothing after this would be about the icon — {last?.Because}");
        }
    }

    /// <summary>What the shell will call it.</summary>
    internal string Tip { get; }

    /// <summary>
    /// Add one, blocking until a reading can find it — not until the shell took the message, which
    /// is the earlier and weaker thing this used to wait for. The tip it ends up with carries this
    /// process, so ask this object rather than passing the same string to a reading.
    /// </summary>
    /// <param name="tip">What the shell should call it, before this run's own mark is added.</param>
    /// <exception cref="InvalidOperationException">
    /// Where the shell accepted the icon and never placed it, which is a fixture that failed
    /// rather than a case that did.
    /// </exception>
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

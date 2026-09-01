using System.Runtime.InteropServices;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Projects;
using Winwright.Verdicts;

namespace Winwright.Tests;

/// <summary>
/// Which kind of menu a tray icon puts up. WW322, and the two arms are the whole of that task.
/// <para>
/// A real application's tray menu is one of these two, and until now this fixture could only be the
/// first. WW332 proved the verb against a <c>TrackPopupMenu</c>, which the desk reports as holding
/// the focus — so the reading the verb takes answered, the case passed, and three adopted cases went
/// on failing against the other kind.
/// </para>
/// </summary>
internal enum TrayMenuKind
{
    /// <summary>
    /// None. The icon is asked and shows nothing, which is what proves the verb reports the truth
    /// rather than claiming a menu it never saw.
    /// </summary>
    None,

    /// <summary>A Win32 popup through <c>TrackPopupMenu</c>, which the desk reports as focused.</summary>
    Win32,

    /// <summary>
    /// A WinForms <c>ToolStripDropDown</c>, which is a top-level menu window the desk does not
    /// report as focused. This is the kind freewilly and claude-tray both put up.
    /// </summary>
    DropDown,
}

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
    private const uint NimModify = 1;
    private const uint NimDelete = 2;
    private const uint NifMessage = 0x01;
    private const uint NifIcon = 0x02;
    private const uint NifTip = 0x04;
    private const nint IdiApplication = 32512;

    /// <summary>
    /// The callback the shell sends this icon's messages as, and the ones that mean "show your
    /// menu". WW332.
    /// <para>
    /// Four and not one, deliberately. This icon registers no version, so the shell speaks the
    /// legacy protocol and puts a mouse message in the lParam — but the keyboard route is
    /// <c>NIN_KEYSELECT</c>, and a shell that has decided this icon is version 4 sends
    /// <c>WM_CONTEXTMENU</c> instead. A fixture that answered only one of them would fail to show a
    /// menu for a reason that has nothing to do with the verb under test, and the case would read
    /// that as the verb being broken.
    /// </para>
    /// </summary>
    private const uint TrayCallback = 0x0400 + 1;

    private const uint WmContextMenu = 0x007B;
    private const uint WmRButtonUp = 0x0205;
    private const uint NinSelect = 0x0400;
    private const uint NinKeySelect = 0x0403;
    private const uint WmCancelMode = 0x001F;
    private const int GwlpWndProc = -4;

    /// <summary>Track the popup and hand back what was picked, rather than posting a command.</summary>
    private const uint TpmReturnCmd = 0x0100;
    private const uint TpmNonNotify = 0x0080;

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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowLongPtrW(nint window, int index, Subclassed replacement);

    [DllImport("user32.dll")]
    private static extern nint CallWindowProcW(nint previous, nint window, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenuW(nint menu, uint flags, nuint item, string text);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(nint menu);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int TrackPopupMenu(nint menu, uint flags, int x, int y, int reserved, nint window, nint rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint window);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint PostMessageW(nint window, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point where);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    private delegate nint Subclassed(nint window, uint message, nint wParam, nint lParam);

    /// <summary>
    /// How long the shell gets to place an icon it has already accepted. The engine's own resolve
    /// deadline, because that is what this is: waiting for something to turn up in a tree.
    /// </summary>
    private static readonly int PlacedMs = Timeouts.Defaults["resolve"];

    private readonly Thread thread;
    private uint threadId;
    private nint owner;

    /// <summary>Held for the life of the window it was given to. WW332.</summary>
    private Subclassed? answering;

    /// <summary>What this icon shows when the shell asks it for a menu. WW332, and WW322's arms.</summary>
    private readonly TrayMenuKind answers;

    /// <summary>
    /// The drop-down, once one has been put up. WW322.
    /// <para>
    /// Kept rather than built per request, because it is a window with a lifetime: a
    /// <c>ToolStripDropDown</c> shown and left standing is what a tray application's menu is, and a
    /// second one built on the next request would leave the first on the desk with nothing holding it.
    /// </para>
    /// </summary>
    private System.Windows.Forms.ToolStripDropDown? standing;

    private nint wasAnswering;

    /// <summary>
    /// How many times this icon has been asked for its menu and shown one. WW332.
    /// <para>
    /// Counted rather than flagged, because the claim a case makes is that the verb asked once and
    /// got one menu. A flag would read the same whether the shell delivered the request once or
    /// four times, and this icon answers four different messages on purpose.
    /// </para>
    /// </summary>
    private int shown;

    /// <summary>Whether this icon has shown its menu, and how often. WW332.</summary>
    public int MenusShown => Volatile.Read(ref shown);

    private TrayIconFixture(string tip, TrayMenuKind withMenu)
    {
        answers = withMenu;

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

            // WW332. Subclassed rather than given a class of its own, which is the smaller change to
            // a fixture that already works: the window exists to own an icon, and what it now also
            // does is answer the one message an icon's owner has to answer to have a menu at all.
            // Held in a field for the life of the thread — a delegate handed to the window and then
            // collected is a callback into freed memory.
            answering = Answer;
            wasAnswering = SetWindowLongPtrW(owner, GwlpWndProc, answering);

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
        //
        // WW200 found the reading being thrown away. A shell that would not shut the flyout left it
        // standing and said so to nobody, and the case asserting this fixture leaves the overflow as
        // it found it went red about the fixture. Answered as what it is: a desk that will not work
        // its own flyout is excusable, exactly as one that would not open it already is.
        var shut = NotificationArea.CloseOverflow();
        if (!shut.Held)
        {
            throw new DeskRefusedException(
                Precondition.Absent(OverflowState.PreconditionName, shut.Because ?? shut.ToString()),
                $"the overflow was opened looking for '{Tip}' and would not shut again: {shut}");
        }

        // WW179. The search already says which of the two it was, and this used to throw either
        // way — so a shell covering the taskbar ended the case as a broken harness and sent a
        // reader to this repository. A desk that would not let the search look is excusable; a
        // shell that looked everywhere and placed nothing is the fixture genuinely failing.
        if (!found.Found && last is { Everywhere: false })
        {
            throw new DeskRefusedException(
                Precondition.Absent(TraySearch.PreconditionName, last.Because),
                $"'{Tip}' could not be looked for: {last.Because}");
        }

        if (!found.Found)
        {
            // WW217. Asked once, after the fact, and about the desk rather than about this icon: a
            // search that opened the flyout and read it did look everywhere, so the old verdict here
            // was a red — and on a guest under a full suite what it was really reporting was a shell
            // that had not placed anybody's icon yet. A bar or a flyout with icons in it is a shell
            // that places them, and ours being absent from that is a finding.
            if (NotificationArea.Placing() is { Satisfied: false } desk)
            {
                throw new DeskRefusedException(
                    desk,
                    $"'{Tip}' could not be looked for on a desk that is placing no icons: {desk.Absence}");
            }

            // The last search's own reason, which is the half this used to drop. A shell that would
            // not open the flyout and a shell that took the icon and placed it nowhere both ended
            // this sentence the same way, and the difference is the one a reader needs.
            //
            // WW173: and what an earlier run left, because that is the answer measured behind four
            // of these. A shell holding icons from a run that died is a shell that will do this
            // again, and a reader told only about their own icon restarts nothing.
            throw new InvalidOperationException(
                $"the shell took '{Tip}' and never put it anywhere a reading could find it within "
                + $"{PlacedMs} ms, so nothing after this would be about the icon — {last?.Because} "
                + TrayGhosts.Showing().Sentence());
        }
    }

    /// <summary>What the shell calls it now, which <see cref="Rename"/> moves. WW82.</summary>
    internal string Tip { get; private set; }

    /// <summary>
    /// Add one, blocking until a reading can find it — not until the shell took the message, which
    /// is the earlier and weaker thing this used to wait for. The tip it ends up with carries this
    /// process, so ask this object rather than passing the same string to a reading.
    /// </summary>
    /// <param name="tip">What the shell should call it, before this run's own mark is added.</param>
    /// <param name="withMenu">
    /// What it answers the shell's request for a context menu with. WW332, and a parameter rather
    /// than a default because all three shapes assert something. An icon with no menu is what proves
    /// the verb reports the truth instead of claiming a menu it never saw — the false green this
    /// project is against. A Win32 popup is what proves the route works at all. And a drop-down is
    /// WW322: the kind a real tray puts up, which the desk does not report as focused.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Where the shell accepted the icon and never placed it, which is a fixture that failed
    /// rather than a case that did.
    /// </exception>
    internal static TrayIconFixture Add(string tip, TrayMenuKind withMenu = TrayMenuKind.None) =>
        new(tip, withMenu);

    /// <summary>
    /// Rename the icon in place, the way an application with a live tooltip does.
    /// <para>
    /// WW82. The icon is not replaced and not re-added: this is <c>NIM_MODIFY</c>, the same call a
    /// tray makes when its own state changes, so the element in the tree is the one that was there a
    /// moment ago wearing a different name. That is the condition claude-tray produced on every run
    /// and that no case here could reach — its tooltip says <c>connecting…</c> until data arrives.
    /// </para>
    /// <para>
    /// Waits for the shell to publish it, for the reason <see cref="Placed"/> gives about the add:
    /// the call returning means the shell took the message, not that the tree has it yet.
    /// </para>
    /// </summary>
    /// <param name="tip">The new tooltip, which this run's process id is appended to as before.</param>
    /// <exception cref="InvalidOperationException">Where the shell refused it, or never published it.</exception>
    internal void Rename(string tip)
    {
        Tip = $"{tip} #{Environment.ProcessId}";

        var data = Describe();
        if (!Shell_NotifyIconW(NimModify, ref data))
            throw new InvalidOperationException("the shell refused to rename the tray icon");

        Placed();
    }

    /// <summary>
    /// One message to the icon's owner, answered where it is the shell asking for a menu. WW332.
    /// <para>
    /// <c>SetForegroundWindow</c> before tracking and a cancel posted after, which is the sequence
    /// every tray application has to use and is not decoration here: a popup tracked from a window
    /// that does not hold the foreground is dismissed the moment the mouse moves, and one tracked
    /// without the cancel outlives the click that opened it.
    /// </para>
    /// </summary>
    /// <param name="window">The icon's owner.</param>
    /// <param name="message">What arrived.</param>
    /// <param name="wParam">The icon's id, where this is the tray callback.</param>
    /// <param name="lParam">Which request it is, where this is the tray callback.</param>
    private nint Answer(nint window, uint message, nint wParam, nint lParam)
    {
        if (message != TrayCallback)
            return CallWindowProcW(wasAnswering, window, message, wParam, lParam);

        var asked = (uint)(lParam & 0xFFFF);
        if (asked is WmContextMenu or WmRButtonUp or NinKeySelect or NinSelect)
        {
            if (answers == TrayMenuKind.Win32)
                Show();
            else if (answers == TrayMenuKind.DropDown)
                Drop();
        }

        return 0;
    }

    /// <summary>
    /// The other kind: a WinForms drop-down, shown and left standing. WW322.
    /// <para>
    /// Modeless, which is the difference that matters as much as the framework. <see cref="Show" />
    /// blocks inside <c>TrackPopupMenu</c> until the menu goes; this returns with the menu up, which
    /// is what a real tray does — the drop-down is a window of its own and the application's pump
    /// carries on. <c>AutoClose</c> is off for the same reason a capture of one needs it off: a
    /// drop-down dismisses itself the moment anything else takes the focus, and the verb under test
    /// is about a menu that stands while a reading is taken.
    /// </para>
    /// </summary>
    private void Drop()
    {
        _ = GetCursorPos(out var where);

        standing ??= Built();
        Volatile.Write(ref shown, Volatile.Read(ref shown) + 1);
        standing.Show(new System.Drawing.Point(where.X, where.Y));
    }

    /// <summary>Two real entries, the same two the Win32 menu has. WW322.</summary>
    private static System.Windows.Forms.ToolStripDropDown Built()
    {
        var strip = new System.Windows.Forms.ToolStripDropDown { AutoClose = false };
        strip.Items.Add("winwright open");
        strip.Items.Add("winwright quit");
        return strip;
    }

    /// <summary>The menu itself: two real entries, tracked the way the shell expects. WW332.</summary>
    private void Show()
    {
        var menu = CreatePopupMenu();
        if (menu == 0)
            return;

        try
        {
            AppendMenuW(menu, 0, 1, "winwright open");
            AppendMenuW(menu, 0, 2, "winwright quit");

            // Where the cursor is, which is where a tray menu goes. The keyboard route puts no
            // position in the message, so there is nothing better to use and nothing that needs to
            // be: what a case reads is that a menu exists, not where it was drawn.
            _ = GetCursorPos(out var where);

            SetForegroundWindow(owner);
            Volatile.Write(ref shown, Volatile.Read(ref shown) + 1);

            // TPM_RETURNCMD, so this blocks here until the menu is dismissed and nothing is posted
            // back to a window that would have to answer it. The case dismisses it.
            _ = TrackPopupMenu(menu, TpmReturnCmd | TpmNonNotify, where.X, where.Y, 0, owner, 0);
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    /// <summary>
    /// Shut a menu this icon is showing, so a case that opened one does not leave it up. WW332,
    /// and it is WW330's rule applied where the leak would be this suite's own.
    /// </summary>
    public void DismissMenu()
    {
        if (owner != 0)
            PostMessageW(owner, WmCancelMode, 0, 0);

        // WW322. A drop-down is a control and not a tracked popup, so WM_CANCELMODE at the owner
        // does not reach it: it has to be closed on the thread that made it, and with AutoClose put
        // back or the close is a request the control declines.
        Close(standing);
    }

    /// <summary>Shut a drop-down from the thread that owns it, where there is one. WW322.</summary>
    /// <param name="drop">The drop-down, or null where none was ever put up.</param>
    private static void Close(System.Windows.Forms.ToolStripDropDown? drop)
    {
        if (drop is null || drop.IsDisposed || !drop.IsHandleCreated)
            return;

        try
        {
            drop.Invoke(() =>
            {
                drop.AutoClose = true;
                drop.Close();
            });
        }
        catch (Exception gone) when (gone is InvalidOperationException or ObjectDisposedException)
        {
            // The thread that owns it has already gone, which is Dispose racing itself and is the
            // one case where a menu left standing takes its whole desktop with it anyway.
        }
    }

    /// <summary>Take it away, and the window that owned it.</summary>
    public void Dispose()
    {
        DismissMenu();

        if (threadId != 0)
            PostThreadMessageW(threadId, WmQuit, 0, 0);

        thread.Join(TimeSpan.FromSeconds(5));
        standing?.Dispose();
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

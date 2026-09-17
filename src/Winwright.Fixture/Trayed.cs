using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Winwright.Fixture;

/// <summary>
/// A process whose only surface is a notification-area icon, and the menu it puts up when the shell
/// asks. WW451.
/// <para>
/// This is what every adopter of this engine is. claude-tray and freewilly are tray applications: the
/// icon is theirs, the menu is theirs, and the harness is somewhere else entirely. Every case proving
/// <c>open tray menu</c> here adds its icon through <c>TrayIconFixture</c>, from inside the test host
/// — so the owner has always been the process driving it, and an adopter's never is.
/// </para>
/// <para>
/// What that cost is written in the verb's own source. WW322 repaired this engine for a WinForms
/// drop-down that answers no focus reading, and the way it was found was by reading claude-tray's
/// log: its tray opened the menu 22 milliseconds after the key and held it for the whole six seconds
/// the verb waits, while the engine reported that nothing had been highlighted. A defect here,
/// diagnosed out of somebody else's log, because nothing here could make the shape.
/// </para>
/// <para>
/// And the second reason, which is what filed the task. A run that looked like that defect was an
/// adopter restoring a package cut four hours before WW322 shipped — a suite that cannot make this
/// shape cannot tell a stale package from a broken route.
/// </para>
/// <para>
/// Ported from <see href="../../tests/Winwright.Tests/TrayIconFixture.cs">TrayIconFixture</see>
/// rather than invented, because every trap in it was paid for once already: the four messages a
/// shell may send, the foreground a tracked popup needs, and the container that answers
/// <c>MenuItem</c> rather than <c>Button</c>.
/// </para>
/// </summary>
internal sealed class Trayed : IDisposable
{
    /// <summary>
    /// The menus a real tray puts up, as this flag spells them.
    /// <para>
    /// WW455 added the third, and it is the first one here that behaves like a shipped application's.
    /// The other two hold themselves open — <c>AutoClose</c> off, or a <c>TrackPopupMenu</c> tracked
    /// on a thread that is blocked inside it — which every menu in this tree has done since the first
    /// one, each for the same good reason and with the same cost: the engine has never taken a reading
    /// against a menu that could go while it was looking, and that is the only kind an application has.
    /// </para>
    /// </summary>
    public static IReadOnlyList<string> Kinds { get; } = ["dropdown", "shuts", "win32"];

    /// <summary>The kind whose drop-down is left to close itself, as an application's does. WW455.</summary>
    private const string Shuts = "shuts";

    private const uint WsPopup = 0x80000000;
    private const uint NimAdd = 0;
    private const uint NimDelete = 2;
    private const uint NifMessage = 0x01;
    private const uint NifIcon = 0x02;
    private const uint NifTip = 0x04;
    private const nint IdiApplication = 32512;

    /// <summary>
    /// The callback the shell sends this icon's messages as, and the four that mean "show your menu".
    /// <para>
    /// Four and not one, which is WW332's measurement rather than caution. This icon registers no
    /// version, so the shell speaks the legacy protocol and puts a mouse message in the lParam — but
    /// the keyboard route is <c>NIN_KEYSELECT</c>, and a shell that has decided the icon is version 4
    /// sends <c>WM_CONTEXTMENU</c> instead. A fixture answering one of them would show no menu for a
    /// reason that has nothing to do with the verb under test.
    /// </para>
    /// </summary>
    private const uint TrayCallback = 0x0400 + 1;

    private const uint WmContextMenu = 0x007B;
    private const uint WmRButtonUp = 0x0205;
    private const uint NinSelect = 0x0400;
    private const uint NinKeySelect = 0x0403;
    private const int GwlpWndProc = -4;

    /// <summary>Track the popup and hand back what was picked, rather than posting a command.</summary>
    private const uint TpmReturnCmd = 0x0100;
    private const uint TpmNonNotify = 0x0080;

    /// <summary>An entry that opens a submenu rather than sending a command. WW453.</summary>
    private const uint MfPopup = 0x0010;

    private delegate nint Subclassed(nint window, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    [DllImport("user32.dll")]
    private static extern nint LoadIconW(nint instance, nint name);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowLongPtrW(nint window, int index, Subclassed replacement);

    [DllImport("user32.dll")]
    private static extern nint CallWindowProcW(nint previous, nint window, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint window);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint CreatePopupMenu();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(nint menu);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenuW(nint menu, uint flags, nuint item, string text);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int TrackPopupMenu(nint menu, uint flags, int x, int y, int reserved, nint window, nint rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point where);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIconW(uint message, ref NotifyIconData data);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    /// <summary>
    /// The whole of <c>NOTIFYICONDATAW</c>, and whole on purpose: the shell reads <c>cbSize</c> to
    /// decide which version of the structure it was handed, and a short one is a message it declines.
    /// </summary>
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

    /// <summary>
    /// What the shell calls this run's icon.
    /// <para>
    /// WW126's rule, and it matters more here than it did there: a run that was killed leaves its icon
    /// registered with the shell, and nothing in the next process can delete somebody else's. A ghost
    /// from a previous launch would otherwise be found by tip and driven as this launch's own.
    /// </para>
    /// <para>
    /// A harness addresses it by building the same string from the process it started, which is a
    /// spelling written down twice and the only shape available — the suite references this project
    /// without its assembly, because an application under test is launched from its own output.
    /// </para>
    /// </summary>
    public static string TipFor(int process) => $"winwright tray #{process}";

    /// <summary>The two entries, the same two every other menu in this tree has.</summary>
    private const string Open = "winwright open";
    private const string Quit = "winwright quit";

    /// <summary>
    /// The entry that opens something, and what is under it. WW453.
    /// <para>
    /// Every menu in this tree was flat until now — two commands and nothing under either — and
    /// `open submenu` is the third step of claude-tray's own case and the second of freewilly's. So
    /// the verb has only ever been driven against a menu with no submenu in it.
    /// </para>
    /// <para>
    /// Filled rather than left empty, which is WW259's measurement and not a preference: a WinForms
    /// submenu that is empty when the menu opens exposes no ExpandCollapse at all and draws no arrow,
    /// and the shell then handles Right as <em>activate a plain command</em> — which dismisses the
    /// whole menu. An empty one is a different defect and would need a flag of its own to be honest
    /// about which is being reproduced.
    /// </para>
    /// </summary>
    private const string More = "winwright profiles";
    private const string First = "winwright one";
    private const string Second = "winwright two";

    private readonly bool dropDown;

    /// <summary>
    /// Whether this run's drop-down is left to close itself. WW455.
    /// <para>
    /// Carried rather than folded into <see cref="dropDown" />, because the two say different things
    /// and one of them is what an adopter has: which framework draws the menu, and whether the menu
    /// survives losing the desk. A Win32 popup answers the second by being tracked on a blocked
    /// thread, so it is a drop-down question and not a menu one.
    /// </para>
    /// </summary>
    private readonly bool holdsOpen;

    private readonly Subclassed answering;
    private readonly nint owner;
    private readonly nint wasAnswering;
    private ToolStripDropDown? standing;

    private Trayed(string kind)
    {
        dropDown = !string.Equals(kind, "win32", StringComparison.Ordinal);
        holdsOpen = !string.Equals(kind, Shuts, StringComparison.Ordinal);

        owner = CreateWindowExW(0, "Static", "winwright tray owner", WsPopup, 0, 0, 10, 10, 0, 0, 0, 0);
        if (owner == 0)
            throw new InvalidOperationException("the tray owner window could not be created");

        // Subclassed rather than given a class of its own, which is WW332's shape: the window exists
        // to own an icon, and what it also does is answer the one message an icon's owner must answer
        // to have a menu at all. Held in a field for the life of the process — a delegate handed to a
        // window and then collected is a callback into freed memory.
        answering = Answer;
        wasAnswering = SetWindowLongPtrW(owner, GwlpWndProc, answering);

        var data = Describe();
        if (!Shell_NotifyIconW(NimAdd, ref data))
        {
            DestroyWindow(owner);
            throw new InvalidOperationException("the shell refused this run's tray icon");
        }
    }

    /// <summary>
    /// Put the icon up and hand back the thing that owns it, so the caller can run a loop.
    /// <para>
    /// The loop is the caller's for the reason <see cref="Shadowed" /> gives: a shape that started one
    /// of its own would be a second pump in a process whose whole point is that it has one, and the
    /// drop-down's messages go to the loop that owns it.
    /// </para>
    /// <para>
    /// Nothing is shown. The owner is a popup nobody makes visible, created only because
    /// <c>Shell_NotifyIcon</c> needs a window to deliver the icon's messages to — which is what a tray
    /// application is, and what <c>--resident</c> already says about a process that draws nothing.
    /// </para>
    /// </summary>
    /// <param name="kind">Which menu it answers with: <c>dropdown</c> or <c>win32</c>.</param>
    public static Trayed Raise(string kind) => new(kind);

    /// <summary>One message to the icon's owner, answered where it is the shell asking for a menu.</summary>
    private nint Answer(nint window, uint message, nint wParam, nint lParam)
    {
        if (message != TrayCallback)
            return CallWindowProcW(wasAnswering, window, message, wParam, lParam);

        var asked = (uint)(lParam & 0xFFFF);
        if (asked is WmContextMenu or WmRButtonUp or NinKeySelect or NinSelect)
        {
            if (dropDown)
                Drop();
            else
                Show();
        }

        return 0;
    }

    /// <summary>
    /// The kind both adopters put up: a WinForms drop-down.
    /// <para>
    /// WW356's two halves, kept. A <c>ToolStripDropDownMenu</c> holding <c>ToolStripMenuItem</c>s —
    /// <c>Items.Add(string)</c> asks the container what a default item is and <c>ToolStripDropDown</c>
    /// answers with a <c>ToolStripButton</c>, so an automation client reads <c>Button</c> under a menu
    /// with no name and a locator proven here would find nothing in an adopter's tree.
    /// </para>
    /// <para>
    /// WW455 gave it the other lifetime. <c>AutoClose</c> off is what every menu in this tree has
    /// had, and the argument for it is real: the harness is another process, so the menu has to
    /// stand while a tree is walked by something that has just taken the desk to press a key. What
    /// that argument does not establish is that a real menu does stand, and a real one does not —
    /// a <c>ContextMenuStrip</c> on a <c>NotifyIcon</c> closes when it loses activation. So the flag
    /// picks, and the case that needs the harder one can ask for it.
    /// </para>
    /// <para>
    /// WW457 measured what the standing arm costs, and it is more than a lifetime: a menu held open
    /// takes no keyboard at all. WinForms routes an arrow key to a drop-down through the modal filter
    /// it installs for a modal one, and <c>AutoClose</c> off is what makes this one not modal — so on
    /// the guest, with the desk admitted and the key delivered, it reads as highlighting nothing and
    /// nothing moves. A case that walks a menu of this fixture asks for the arm that shuts itself.
    /// </para>
    /// <para>
    /// Rebuilt rather than kept where it shuts, which is not tidiness. A drop-down that closed itself
    /// is a control whose handle the framework may already have let go, and reusing it is how a
    /// second ask silently shows nothing.
    /// </para>
    /// </summary>
    private void Drop()
    {
        _ = GetCursorPos(out var where);

        if (holdsOpen)
            standing ??= Built(autoClose: false);
        else
            standing = Built(autoClose: true);

        // WW457. The foreground first, exactly as the Win32 arm does — and it is what a real tray
        // application has to do before showing a menu, not a nicety. A `ContextMenuStrip` raised for
        // a `NotifyIcon` without it never sees the click that should dismiss it, which is the oldest
        // documented workaround in the notification area.
        //
        // Skipping it is the third time this fixture has modelled an adopter's process and not its
        // behaviour. It left the desk on whatever had it — the shell's overflow flyout, on a guest —
        // where a real tray leaves it on a window of its own, one thread away from the menu. So the
        // reading that admits that arrangement had no case here that could reach it, and
        // claude-tray's own run was the only place it showed.
        SetForegroundWindow(owner);

        standing.Show(new System.Drawing.Point(where.X, where.Y));
    }

    /// <param name="autoClose">Whether the menu shuts itself when it loses the desk, as a real one does.</param>
    private static ToolStripDropDown Built(bool autoClose)
    {
        var strip = new ToolStripDropDownMenu { AutoClose = autoClose };
        strip.Items.Add(new ToolStripMenuItem(Open));

        // WW453. Between the two commands rather than at the end, which is the position that matters:
        // a menu opens highlighting its first entry, so an entry that is not the first is the only
        // one that asks whether a step naming it walked there before pressing Right. claude-tray's
        // profile entry is the fourth of its menu, and WW83 is where that was measured.
        var more = new ToolStripMenuItem(More);
        more.DropDownItems.Add(new ToolStripMenuItem(First));
        more.DropDownItems.Add(new ToolStripMenuItem(Second));
        strip.Items.Add(more);

        strip.Items.Add(new ToolStripMenuItem(Quit));
        return strip;
    }

    /// <summary>The other kind: a Win32 popup, tracked the way the shell expects.</summary>
    private void Show()
    {
        var menu = CreatePopupMenu();
        if (menu == 0)
            return;

        // WW453. The submenu, built first because the entry that opens it names its handle. Destroyed
        // with its parent rather than here: `DestroyMenu` takes a popup's submenus with it, and
        // destroying one twice is how a tidy-up becomes the defect.
        var under = CreatePopupMenu();

        try
        {
            AppendMenuW(menu, 0, 1, Open);

            if (under != 0)
            {
                AppendMenuW(under, 0, 3, First);
                AppendMenuW(under, 0, 4, Second);

                // MF_POPUP, which is what makes the entry a door rather than a command. The item id
                // slot carries the submenu's handle in this arm, which is the one place in this file
                // where a Win32 flag changes what the next field means.
                AppendMenuW(menu, MfPopup, (nuint)under, More);
            }

            AppendMenuW(menu, 0, 2, Quit);

            // Where the cursor is, which is where a tray menu goes. The keyboard route puts no
            // position in the message, so there is nothing better to use and nothing that needs to
            // be: what a case reads is that a menu exists, not where it was drawn.
            _ = GetCursorPos(out var where);

            SetForegroundWindow(owner);

            // TPM_RETURNCMD, so this blocks here until the menu is dismissed and nothing is posted
            // back to a window that would have to answer it. Whoever opened it dismisses it.
            _ = TrackPopupMenu(menu, TpmReturnCmd | TpmNonNotify, where.X, where.Y, 0, owner, 0);
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    /// <summary>Take the icon away, and the window that owned it.</summary>
    public void Dispose()
    {
        var going = Describe();
        Shell_NotifyIconW(NimDelete, ref going);

        standing?.Dispose();
        DestroyWindow(owner);
    }

    private NotifyIconData Describe() => new()
    {
        Size = Marshal.SizeOf<NotifyIconData>(),
        Window = owner,
        Id = 1,
        Flags = NifMessage | NifIcon | NifTip,
        CallbackMessage = TrayCallback,
        Icon = LoadIconW(0, IdiApplication),
        Tip = TipFor(Environment.ProcessId),
        Info = "",
        InfoTitle = "",
    };
}

using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace Winwright.Tests;

/// <summary>
/// A window on a thread that pumps its own messages, which is what a real application is.
/// <para>
/// The earlier fixtures create windows on the test thread, which never drains the queue — so
/// synthesized input arrives nowhere until the test stops to pump, and an act that waits for its
/// own read-back would wait forever. Keyboard input is the case where that stops being a detail:
/// keys travel the queue by definition, so proving anything about them needs a window that
/// behaves like one somebody is using.
/// </para>
/// <para>
/// It also takes the foreground from its own thread. A decoy that has to hold the desktop is
/// another one of these rather than a bare window created by the test, for the same reason.
/// </para>
/// </summary>
internal sealed class PumpedDialog : IDisposable
{
    private const uint WsPopup = 0x80000000;
    private const uint WsVisible = 0x10000000;
    private const uint WsOverlappedWindow = 0x00CF0000;
    private const uint WmQuit = 0x0012;
    private const uint WmClose = 0x0010;
    private const uint WmCancelMode = 0x001F;
    private const uint MfString = 0x0000;
    private const uint MfPopup = 0x0010;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

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

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessageW(nint window, uint message, nint wParam, nint lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsDialogMessageW(nint dialog, ref Msg message);

    [DllImport("comctl32.dll")]
    private static extern void InitCommonControls();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint window);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern nint CreateMenu();

    [DllImport("user32.dll")]
    private static extern nint CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenuW(nint menu, uint flags, nint id, string? item);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetMenu(nint window, nint menu);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DrawMenuBar(nint window);

    private readonly Thread thread;
    private readonly List<nint> created = [];
    private uint threadId;

    private PumpedDialog(
        string title, IReadOnlyList<ChildWindow> children, IReadOnlyList<MenuEntry> menu, bool overlapped = false)
    {
        using var ready = new ManualResetEventSlim();
        thread = new Thread(() =>
        {
            threadId = GetCurrentThreadId();

            // The common controls have to be loaded before a trackbar can be created at all.
            InitCommonControls();

            // A menu bar needs a window with a caption, so a dialog carrying one is overlapped
            // rather than a bare popup.
            var style = menu.Count == 0 && !overlapped ? WsPopup | WsVisible : WsOverlappedWindow | WsVisible;
            Frame = Make("Static", title, style, 60, 60, 520, 360, 0);
            foreach (var child in children)
                Make(child.ClassName, child.Title, child.Style, child.X, child.Y, child.Width, child.Height, Frame);

            if (menu.Count > 0)
            {
                var bar = CreateMenu();
                foreach (var entry in menu)
                    Append(bar, entry);

                SetMenu(Frame, bar);
                DrawMenuBar(Frame);
            }

            // From this thread, because it owns the window it just created — which is the one
            // case Windows grants the foreground to. Asked from the test thread it is refused,
            // and then whichever window the desk already had keeps it. That refusal is WW13's
            // subject, and a fixture that needs the desktop has to be the process holding it.
            SetForegroundWindow(Frame);

            ready.Set();

            while (GetMessageW(out var message, 0, 0, 0) > 0)
            {
                // Through the dialog manager, because that is what makes Tab traverse. Measured:
                // without it the key reaches the focused control and focus never moves, which
                // reads as the window taking no keyboard input when the window is fine.
                if (IsDialogMessageW(Frame, ref message))
                    continue;

                TranslateMessage(ref message);
                DispatchMessageW(ref message);
            }

            for (var index = created.Count - 1; index >= 0; index--)
                DestroyWindow(created[index]);
        })
        {
            IsBackground = true,
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!ready.Wait(TimeSpan.FromSeconds(10)))
            throw new TimeoutException("the pumped dialog never opened");

        TakeTheDesktop();
    }

    /// <summary>
    /// Ask for the foreground for a moment, and carry on either way.
    /// <para>
    /// Windows hands it over asynchronously, so asking once and reading immediately is a race. It
    /// does not refuse to continue when the desk stays elsewhere, and that is deliberate: the
    /// tests that need it assert it themselves and the ones that need it absent rely on this not
    /// insisting. Making it insist turned one busy desktop into forty-seven failures at once.
    /// </para>
    /// </summary>
    private void TakeTheDesktop()
    {
        var until = System.Diagnostics.Stopwatch.StartNew();
        while (until.ElapsedMilliseconds < 500 && GetForegroundWindow() != Frame)
        {
            SetForegroundWindow(Frame);
            Thread.Sleep(20);
        }
    }

    /// <summary>One child control to build inside the frame.</summary>
    internal sealed record ChildWindow(
        string ClassName, string? Title, uint Style, int X, int Y, int Width, int Height);

    /// <summary>One menu entry, which is a submenu where it has children of its own.</summary>
    internal sealed record MenuEntry(string Name, params MenuEntry[] Children);

    /// <summary>The frame's window handle.</summary>
    internal nint Frame { get; private set; }

    /// <summary>The frame as UI Automation sees it.</summary>
    internal AutomationElement Root => AutomationElement.FromHandle(Frame);

    /// <summary>
    /// Ask for the desktop again. A test that is about to send input calls this first: another
    /// class's window may have taken it since this one opened, and a key sent to somebody else is
    /// a different act entirely.
    /// </summary>
    internal void BringToFront() => TakeTheDesktop();

    /// <summary>Open one, blocking until its thread has built it and is pumping.</summary>
    internal static PumpedDialog Open(string title, params ChildWindow[] children) => new(title, children, []);

    /// <summary>Open one carrying a real menu bar, which is what a keyboard user walks.</summary>
    internal static PumpedDialog OpenWithMenu(string title, params MenuEntry[] bar) => new(title, [], bar);

    /// <summary>
    /// Open an overlapped one — a caption, a sizing border and the drop shadow that comes with
    /// them. A bare popup has none of those, which is exactly the difference a capture cares about.
    /// </summary>
    internal static PumpedDialog OpenFramed(string title, params ChildWindow[] children) =>
        new(title, children, [], overlapped: true);

    /// <summary>
    /// Ask the thread to stop, and wait for it to take its windows with it.
    /// <para>
    /// The cancel first, and this is the whole repair: an open menu holds the thread inside a
    /// modal loop where the quit it is being sent is never read, so the window outlives the test
    /// and the next class to ask what this process is showing finds it. Measured, after a class
    /// that walks menus started failing a class that counts windows.
    /// </para>
    /// </summary>
    public void Dispose()
    {
        if (Frame != 0)
            PostMessageW(Frame, WmCancelMode, 0, 0);

        if (threadId != 0)
            PostThreadMessageW(threadId, WmQuit, 0, 0);

        if (thread.Join(TimeSpan.FromSeconds(3)))
            return;

        if (Frame != 0)
            PostMessageW(Frame, WmClose, 0, 0);

        if (!thread.Join(TimeSpan.FromSeconds(3)))
            throw new InvalidOperationException($"a pumped dialog would not close: 0x{Frame:X} is still up");
    }

    private static void Append(nint into, MenuEntry entry)
    {
        if (entry.Children.Length == 0)
        {
            AppendMenuW(into, MfString, 0, entry.Name);
            return;
        }

        var popup = CreatePopupMenu();
        foreach (var child in entry.Children)
            Append(popup, child);

        AppendMenuW(into, MfPopup, popup, entry.Name);
    }

    private nint Make(string className, string? title, uint style, int x, int y, int w, int h, nint parent)
    {
        var window = CreateWindowExW(0, className, title, style, x, y, w, h, parent, 0, 0, 0);
        if (window == 0)
            throw new InvalidOperationException($"could not create a {className}");

        created.Add(window);
        return window;
    }
}

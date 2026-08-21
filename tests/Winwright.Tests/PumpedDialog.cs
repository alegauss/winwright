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
/// </summary>
internal sealed class PumpedDialog : IDisposable
{
    private const uint WsPopup = 0x80000000;
    private const uint WsVisible = 0x10000000;
    private const uint WmQuit = 0x0012;

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

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    private readonly Thread thread;
    private readonly List<nint> created = [];
    private uint threadId;

    private PumpedDialog(string title, IReadOnlyList<ChildWindow> children)
    {
        using var ready = new ManualResetEventSlim();
        thread = new Thread(() =>
        {
            threadId = GetCurrentThreadId();
            Frame = Make("Static", title, WsPopup | WsVisible, 60, 60, 520, 360, 0);
            foreach (var child in children)
                Make(child.ClassName, child.Title, child.Style, child.X, child.Y, child.Width, child.Height, Frame);

            ready.Set();

            while (GetMessageW(out var message, 0, 0, 0) > 0)
            {
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
    }

    /// <summary>One child control to build inside the frame.</summary>
    internal sealed record ChildWindow(
        string ClassName, string? Title, uint Style, int X, int Y, int Width, int Height);

    /// <summary>The frame's window handle.</summary>
    internal nint Frame { get; private set; }

    /// <summary>The frame as UI Automation sees it.</summary>
    internal AutomationElement Root => AutomationElement.FromHandle(Frame);

    /// <summary>Open one, blocking until its thread has built it and is pumping.</summary>
    internal static PumpedDialog Open(string title, params ChildWindow[] children) => new(title, children);

    /// <summary>Ask the thread to stop, and wait for it to take its windows with it.</summary>
    public void Dispose()
    {
        if (threadId != 0)
            PostThreadMessageW(threadId, WmQuit, 0, 0);

        thread.Join(TimeSpan.FromSeconds(5));
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

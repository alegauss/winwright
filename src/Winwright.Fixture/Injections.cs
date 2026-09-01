using System.Runtime.InteropServices;
using System.Text;

namespace Winwright.Fixture;

/// <summary>
/// What <c>SendInput</c> handed the system, read before the window's queue ever sees it. WW312.
/// <para>
/// This exists because the pairing the fault needs was declared impossible one message too late. The
/// decisive question is whether the substitution is already in what was injected or is made of a
/// correct injection by the translation, and answering it needs the code unit of each injected key.
/// <see cref="Arrivals" /> looked for it in the <c>WM_KEYDOWN</c> and found it in neither word: the
/// lParam gives the scan code eight bits and the wParam's high word is zero. A UTF-16 code unit does
/// not fit in eight bits, so the message a thread dequeues cannot carry it, and no reading taken
/// from the queue ever will.
/// </para>
/// <para>
/// <c>KBDLLHOOKSTRUCT.scanCode</c> is a full <c>DWORD</c> and is where the code unit is, which makes
/// a low-level hook the one observation point between <c>SendInput</c> and the queue. That is the
/// whole of what this is for: the units in the order they were injected, against the characters
/// <see cref="Arrivals" /> records arriving, on the same round.
/// </para>
/// <para>
/// On its own thread with its own pump, which is WW316 and not tidiness. A low-level hook runs on
/// the thread that installed it; installed on the UI thread it would put the instrument inside the
/// queue it is measuring, and every keystroke on the desk would then wait behind whatever WPF was
/// doing — with Windows quietly dropping the hook past <c>LowLevelHooksTimeout</c>. The callback
/// here reads three integers and appends one character.
/// </para>
/// </summary>
internal sealed class Injections
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;

    /// <summary>VK_PACKET, which is what a <c>KEYEVENTF_UNICODE</c> injection arrives as.</summary>
    private const int VkPacket = 0xE7;

    /// <summary>
    /// LLKHF_INJECTED. Checked rather than assumed: this hook is global, so a person typing at the
    /// guest while a run is going would otherwise be recorded as part of the send.
    /// </summary>
    private const int Injected = 0x00000010;

    /// <summary>
    /// How many code units of the record stay on it, matching <see cref="Arrivals" />'s own bound for
    /// the same reason — every claim made against this record is about the end of it.
    /// </summary>
    private const int Kept = 400;

    /// <summary>
    /// Held between the hook's thread and the UI thread's read. Both critical sections are an append
    /// and a copy, and the hook's has to stay that short: it is on the path of every keystroke the
    /// desk delivers, this run's and anybody else's.
    /// </summary>
    private readonly Lock gate = new();

    private readonly StringBuilder units = new();

    /// <summary>
    /// Held for the life of the hook. A delegate handed to <c>SetWindowsHookEx</c> and then collected
    /// is a callback into freed memory, which the process learns about by dying.
    /// </summary>
    private Hooked? callback;

    private nint hook;

    /// <summary>Why the hook is not running, or empty where it is. WW316: an instrument that is not reading says so.</summary>
    private string absent = "";

    /// <summary>How many injected packets the hook saw, kept where trimming cannot reach it.</summary>
    private long seen;

    private Injections()
    {
    }

    /// <summary>
    /// Install the hook and start pumping for it, or come back saying why it could not be installed.
    /// </summary>
    /// <returns>The record, held by the caller so its callback outlives this call.</returns>
    public static Injections Start()
    {
        var record = new Injections();
        using var installed = new ManualResetEventSlim();

        var thread = new Thread(() => record.Pump(installed))
        {
            IsBackground = true,
            Name = "winwright-injections",
        };

        thread.Start();

        // Waited for, so a window that is already being typed at cannot be recorded by a hook that is
        // still being installed. Bounded, because a wait with no end would hang the fixture's startup
        // on a failure whose whole point is to be reported.
        if (!installed.Wait(5000))
            record.absent = "the hook thread did not start";

        return record;
    }

    /// <summary>The record as a caption carries it: both counts, then the units.</summary>
    /// <param name="dequeued">How many packet keydowns the window's own thread has pulled off its queue.</param>
    public string Counted(long dequeued)
    {
        // Both counts and not one. They are taken at opposite ends of the same path, so a difference
        // between them is the hook missing injections — starved past the timeout, or installed late —
        // and a reading whose instrument stopped reading halfway is worth nothing unless it says so.
        lock (gate)
        {
            return absent.Length > 0
                ? $"{dequeued} dequeued, no injections read: {absent}"
                : $"{dequeued} dequeued, {seen} injected: {units}";
        }
    }

    /// <summary>The hook's own thread: install it, then pump, because a low-level hook is delivered to a message loop.</summary>
    /// <param name="installed">Set once the outcome is known, either way.</param>
    private void Pump(ManualResetEventSlim installed)
    {
        callback = Heard;
        hook = SetWindowsHookExW(WhKeyboardLl, callback, GetModuleHandleW(null), 0);

        if (hook == 0)
            absent = $"SetWindowsHookEx refused with {Marshal.GetLastWin32Error()}";

        installed.Set();
        if (hook == 0)
            return;

        // No TranslateMessage and no DispatchMessage: nothing here has a window, and this loop exists
        // only because a low-level hook is called on a thread that is pumping. It ends with the
        // process, which is how the fixture ends.
        while (GetMessageW(out _, 0, 0, 0) > 0)
        {
        }
    }

    /// <summary>
    /// One key on its way into somebody's queue, looked at and passed on unchanged.
    /// </summary>
    /// <param name="code">Negative means this hook may not inspect the event.</param>
    /// <param name="what">Which message the key will become.</param>
    /// <param name="key">The <c>KBDLLHOOKSTRUCT</c>.</param>
    private nint Heard(int code, nint what, nint key)
    {
        // Read field by field rather than marshalled into a struct: this runs on the path of every
        // keystroke the desk delivers, and three reads of an integer allocate nothing.
        if (code >= 0 && (what == WmKeyDown || what == WmSysKeyDown) && key != 0)
        {
            var virtualKey = Marshal.ReadInt32(key, 0);
            var scan = Marshal.ReadInt32(key, 4);
            var flags = Marshal.ReadInt32(key, 8);

            if (virtualKey == VkPacket && (flags & Injected) != 0)
                Took((char)scan);
        }

        return CallNextHookEx(0, code, what, key);
    }

    /// <summary>One injected code unit, written down.</summary>
    /// <param name="unit">The code unit the injection carried.</param>
    private void Took(char unit)
    {
        lock (gate)
        {
            seen++;

            // Escaped where a console cannot draw it, which is the rule the character record already
            // applies — a record the two sides are compared in has to spell both sides the same way.
            if (char.IsControl(unit) || unit >= 0xE000)
                units.Append("\\u").Append(((int)unit).ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
            else
                units.Append(unit);

            if (units.Length > Kept)
                units.Remove(0, units.Length - Kept);
        }
    }

    private delegate nint Hooked(int code, nint what, nint key);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookExW(int hook, Hooked callback, nint module, uint thread);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hook, int code, nint what, nint key);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetMessageW(out Message message, nint window, uint first, uint last);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint GetModuleHandleW(string? name);

    [StructLayout(LayoutKind.Sequential)]
    private struct Message
    {
        public nint Window;
        public uint What;
        public nint WParam;
        public nint LParam;
        public uint Time;
        public int X;
        public int Y;
    }
}

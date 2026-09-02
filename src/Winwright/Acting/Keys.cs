using Winwright.Windowing;

namespace Winwright.Acting;

/// <summary>
/// Sending one named key, with the scan code the layout in force gives it.
/// <para>
/// The scan code is not optional. Measured: a virtual key sent with a scan code of zero does
/// nothing at all — End does not move a caret, Backspace erases nothing, and a held modifier is
/// not held. The Unicode path a character goes through is the one that needs no scan code, and it
/// is why the two are separate here.
/// </para>
/// </summary>
internal static class Keys
{
    /// <summary>
    /// How long a synthesised send is left alone before anything reads what it did. WW329 measured
    /// it, WW353 moved it here, and WW355 left the click as its only caller.
    /// <para>
    /// <c>SendInput</c> returns once the events are queued rather than processed, so a read taken the
    /// instant it returns puts a cross-process look into the target's thread while its packets are
    /// still being translated. Measured on the guest at 1200 rounds each of three pauses, in typing's
    /// own act shape: <b>31 substitutions with no pause (2.58%), none at 50ms, none at 150ms.</b>
    /// Fifty and not the safer hundred and fifty on what the same run priced — 7ms a round against
    /// 89ms at 150.
    /// </para>
    /// <para>
    /// WW355 took typing off it. What the pause was waiting out is the provider being asked a great
    /// deal, not being asked: every cheaper reader measured clean over two runs of 400 rounds each,
    /// so <c>Keyboard</c> resolves once and asks one pattern and needs no interval at all.
    /// </para>
    /// <para>
    /// The click still takes it, and for a different reason, which is why it is a constant rather
    /// than a line in the one verb left. WW353's pause is not about provoking anything — a click has
    /// no reading of its own to disturb — it is about not reporting the value from before the act on
    /// the one synthesised verb with no poll to fall back on. A reader wanting the measurement behind
    /// the number finds it here; a reader wanting why the click takes it finds that at the call.
    /// </para>
    /// </summary>
    internal const int FirstLookMs = 50;

    private const ushort VkTab = 0x09;
    private const ushort VkShift = 0x10;
    private const ushort VkEnd = 0x23;
    private const ushort VkHome = 0x24;
    private const ushort VkEscape = 0x1B;
    private const ushort VkF10 = 0x79;
    private const ushort VkApps = 0x5D;
    private const ushort VkLeft = 0x25;
    private const ushort VkUp = 0x26;
    private const ushort VkRight = 0x27;
    private const ushort VkDown = 0x28;

    /// <summary>
    /// F10, which is how a keyboard user enters a menu bar. It is used rather than Alt because it
    /// needs no modifier to be held, and a modifier is a thing another process has to agree about.
    /// </summary>
    internal static void SendMenuBar() => Press(Tap(VkF10));

    /// <summary>
    /// The application key — the one between the right Alt and Ctrl on a full keyboard. It is the
    /// route a keyboard user already has to a context menu, and on this shell it is the only one
    /// that reaches a notification-area icon at all.
    /// </summary>
    internal static void SendApplicationKey() => Press(Tap(VkApps));

    /// <summary>Escape, which backs out of a menu. Backing out is not the same as invoking.</summary>
    internal static void SendEscape() => Press(Tap(VkEscape));

    /// <summary>Anchor at one end of a list, which is a selection change like any other.</summary>
    internal static void SendHomeOrEnd(bool home) => Press(Tap(home ? VkHome : VkEnd));

    internal static void Send(TraversalKey key) => Press(key switch
    {
        TraversalKey.Tab => Tap(VkTab),
        TraversalKey.ShiftTab => WithShift(VkTab),
        TraversalKey.Right => Tap(VkRight),
        TraversalKey.Left => Tap(VkLeft),
        TraversalKey.Up => Tap(VkUp),
        _ => Tap(VkDown),
    });

    /// <summary>
    /// A key with modifiers held, sent as one batch.
    /// <para>
    /// WW317. One <c>SendInput</c> and not several, which is the reason this is here rather than
    /// spelled by the caller: modifiers held across separate calls are a window of time in which
    /// another process's key can arrive between them, and a chord half-delivered presses something
    /// nobody asked for. The mechanism is <see cref="WithShift"/>'s, widened — that has held a
    /// modifier for Shift+Tab since block D.
    /// </para>
    /// <para>
    /// Released in reverse, which is what a keyboard does and what applications watching for a
    /// modifier release expect: holding Ctrl then Shift and releasing Ctrl first leaves Shift held
    /// over a keyboard state nobody was in.
    /// </para>
    /// </summary>
    /// <param name="chord">The chord, already parsed.</param>
    internal static void Send(Chord chord)
    {
        ArgumentNullException.ThrowIfNull(chord);

        var holding = chord.Holding();
        var inputs = new List<Win32.Input>(holding.Count * 2 + 2);

        inputs.AddRange(holding.Select(Down));
        inputs.Add(Down(chord.Pressing()));
        inputs.Add(Up(chord.Pressing()));
        inputs.AddRange(holding.Reverse().Select(Up));

        Press([.. inputs]);
    }

    private static void Press(Win32.Input[] inputs) =>
        Win32.SendInput((uint)inputs.Length, inputs, System.Runtime.InteropServices.Marshal.SizeOf<Win32.Input>());

    private static Win32.Input[] Tap(ushort virtualKey) => [Down(virtualKey), Up(virtualKey)];

    private static Win32.Input[] WithShift(ushort virtualKey) =>
        [Down(VkShift), Down(virtualKey), Up(virtualKey), Up(VkShift)];

    private static Win32.Input Down(ushort virtualKey) => Key(virtualKey, 0);

    private static Win32.Input Up(ushort virtualKey) => Key(virtualKey, Win32.KeyUp);

    private static Win32.Input Key(ushort virtualKey, uint flags) => new()
    {
        Type = Win32.InputKeyboard,
        Payload = new Win32.InputPayload
        {
            Key = new Win32.KeyInput
            {
                VirtualKey = virtualKey,
                Scan = (ushort)Win32.MapVirtualKeyW(virtualKey, Win32.VirtualKeyToScan),
                Flags = flags,
            },
        },
    };
}

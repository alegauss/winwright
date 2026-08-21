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
    private const ushort VkTab = 0x09;
    private const ushort VkShift = 0x10;
    private const ushort VkLeft = 0x25;
    private const ushort VkUp = 0x26;
    private const ushort VkRight = 0x27;
    private const ushort VkDown = 0x28;

    internal static void Send(TraversalKey key)
    {
        var inputs = key switch
        {
            TraversalKey.Tab => Tap(VkTab),
            TraversalKey.ShiftTab => WithShift(VkTab),
            TraversalKey.Right => Tap(VkRight),
            TraversalKey.Left => Tap(VkLeft),
            TraversalKey.Up => Tap(VkUp),
            _ => Tap(VkDown),
        };

        Win32.SendInput((uint)inputs.Length, inputs, System.Runtime.InteropServices.Marshal.SizeOf<Win32.Input>());
    }

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

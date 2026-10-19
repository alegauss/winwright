namespace Winwright.Windowing;

/// <summary>
/// What a launch does so the window it starts may come forward on a desk nobody has touched. WW549.
/// <para>
/// Found by quickshell (QS368), soaking one typing case alone on the guest: Degraded three runs of
/// three, the foreground held by Program Manager for the whole wait, while the same case passed in
/// the full run after earlier cases had sent input. Windows lets a process take the foreground only
/// where the process that started it could: the foreground's own, or the one that received the last
/// input. A test host that has sent nothing is neither, so a launch first in its run, or alone in it,
/// starts an application whose activation the system refuses.
/// </para>
/// <para>
/// So a launch sends one mouse move of no distance first — which makes this process the one that
/// received the last input, as the adopter's own workaround did — and then hands the started process
/// the right by its pid. The move goes through <see cref="Win32.SendInput"/>, which stamps it as this
/// run's own, so <see cref="ForeignInput"/> never reads it as a person. It moves nothing on the screen.
/// Changing the machine's foreground-lock timeout would also work and is not done, because a run
/// leaves the machine as it found it.
/// </para>
/// </summary>
internal static class ForegroundLock
{
    /// <summary>Send the move of no distance. True where the input was queued.</summary>
    internal static bool Lift()
    {
        Win32.Input[] still =
        [
            new() { Type = Win32.InputMouse, Payload = new() { Mouse = new() { Flags = Win32.MouseMove } } },
        ];

        return Win32.SendInput(1, still, System.Runtime.InteropServices.Marshal.SizeOf<Win32.Input>()) == 1;
    }

    /// <summary>
    /// Hand the started process the right to take the foreground. True where the system agreed, which
    /// it does only where this process held that right itself, so false says the lift did not land.
    /// </summary>
    /// <param name="pid">The process just started.</param>
    internal static bool Hand(int pid) => Win32.AllowSetForegroundWindow((uint)pid);
}

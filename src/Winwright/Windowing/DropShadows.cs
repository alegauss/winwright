using Winwright.Verdicts;

namespace Winwright.Windowing;

/// <summary>
/// Whether this desk draws the shadow a menu asks for.
/// <para>
/// WW450. A menu's drop shadow is a window of its own — <c>SysShadow</c>, drawn by the shell a few
/// pixels larger than the menu — and whether it exists is a setting of the machine rather than
/// anything the application did. WW346 skips it in <see cref="TopLevelWindows" />, and WW358's case
/// provokes that skip by waiting for one to appear. On GitHub's hosted runner none ever did: the case
/// waited out its ten seconds and went red on every run from 2026-09-02, while the guest, which has
/// shadows on, never failed.
/// </para>
/// <para>
/// Measured before it was written, on the guest desk: the fixture's shadowed menu with the setting on
/// had a <c>SysShadow</c> behind it, with it off had none, and with it on again had one — and WinForms
/// says so in its own class name, <c>Window.20808</c> against <c>Window.808</c>, the missing bit being
/// <c>CS_DROPSHADOW</c>. So the setting is the whole of it, and it is the desk's.
/// </para>
/// <para>
/// Two halves, for the reason WW345 gave the desk probe: the reading of the machine, and the answer it
/// turns into. The second is what a case can drive without switching a system setting off in the
/// middle of a suite — which would be a run changing the desk it is supposed to be reporting on.
/// </para>
/// </summary>
public static class DropShadows
{
    /// <summary>What this reading is called wherever it is reported.</summary>
    public const string PreconditionName = "this desk draws the shadow behind a menu";

    /// <summary>What this desk answers now.</summary>
    public static Precondition Reading()
    {
        var drawn = false;
        return Win32.SystemParametersInfoBool(Win32.SpiGetDropShadow, 0, ref drawn, 0)
            ? Of(drawn)
            : Precondition.Absent(
                PreconditionName,
                "the desk would not say whether it draws drop shadows, so nothing waiting for one can tell "
                    + "a shadow that never came from a setting that forbids it");
    }

    /// <summary>
    /// The answer a reading already taken turns into.
    /// <para>
    /// The absence names the setting and says whose it is, because the case that consults this is one
    /// waiting for a window: without the sentence, a desk with shadows off reads as an application
    /// that failed to draw something, which is the one thing it is not.
    /// </para>
    /// </summary>
    /// <param name="drawn">Whether the desk draws drop shadows.</param>
    public static Precondition Of(bool drawn) =>
        drawn
            ? Precondition.Met(PreconditionName)
            : Precondition.Absent(
                PreconditionName,
                "drop shadows are switched off on this desk, so no menu here has a shadow window behind "
                    + "it — a setting of the machine, and not a window the application failed to draw");
}

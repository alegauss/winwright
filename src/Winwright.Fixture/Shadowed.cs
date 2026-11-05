using System.Windows.Forms;

namespace Winwright.Fixture;

/// <summary>
/// A process whose only windows are a menu and the shadow behind it. WW358.
/// <para>
/// WW346 found that <c>TopLevelWindows.Largest</c> answered a tray application's shadow rather than
/// its menu: the shell draws a <c>SysShadow</c> two pixels larger on every side, the listing is
/// sorted by area, and the shadow wins. Measured in freewilly, whose menu is 188x108 with a 190x111
/// shadow behind it — and WW334 refuses a capture of that one, so a caller asking for the largest
/// window got a refusal it did not expect about a window it did not know it had asked for.
/// </para>
/// <para>
/// Nothing here could provoke it. Every case runs inside the suite's own process, which owns a decoy
/// and a statistics window and whatever else the run left standing, so the sort has real windows to
/// put in front of the shadow and the arm passes with the skip deleted. What the fault needs is a
/// process with no frame at all, which is what a tray application is and what this shape is.
/// </para>
/// <para>
/// WinForms and not WPF, measured rather than chosen. A WPF popup draws its own shadow into its own
/// layer — that is WW347, and it produces no separate window — so it cannot reproduce this surface.
/// A <c>ToolStripDropDown</c> with no main window behind it does: measured at 96x46 with a
/// <c>SysShadow</c> of 101x51 over it, which is the same shape freewilly gave at a different size.
/// </para>
/// <para>
/// It stands rather than closing, which is what <c>AutoClose</c> off is for. A menu that shut the
/// moment anything else took the desk would be a fixture that answers nothing by the time a harness
/// in another process has enumerated its windows.
/// </para>
/// </summary>
internal static class Shadowed
{
    /// <summary>
    /// Where the menu goes. Fixed rather than at the cursor: a harness reads this process's windows
    /// and never the screen, so the position is only ever a thing that has to be on a desk.
    /// </summary>
    private const int Corner = 400;

    /// <summary>
    /// Put the menu up and hand back the form that owns nothing, so the caller can run a loop on it.
    /// <para>
    /// The two entries are the two every other menu in this tree has, which is the point of a
    /// fixture: what differs from the suite's own tray menu is the process around it and nothing
    /// else about the menu itself.
    /// </para>
    /// </summary>
    internal static ToolStripDropDown Raise()
    {
        var strip = new ToolStripDropDown { AutoClose = false };
        strip.Items.Add(new ToolStripMenuItem("winwright open"));
        strip.Items.Add(new ToolStripMenuItem("winwright quit"));
        strip.Show(new System.Drawing.Point(Corner, Corner));
        return strip;
    }
}

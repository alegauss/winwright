using Winwright.Locating;
using Winwright.Windowing;

namespace Winwright.Acting;

/// <summary>
/// The drag, which is the one pointer act made of more than one send. WW520.
/// <para>
/// Beside <see cref="Pointer"/> rather than inside it because its pauses are a different kind from
/// the click's. The click pauses once after sending, so the read that follows does not arrive while
/// the input is still draining. A drag pauses between its parts, because the application has to see
/// each part as its own message.
/// </para>
/// </summary>
public static class Drag
{
    /// <summary>How many moves a drag takes between the threshold and the destination.</summary>
    internal const int DragSteps = 8;

    /// <summary>The pause after each part of a drag, so each arrives as its own message.</summary>
    private const int DragPauseMs = 15;

    /// <summary>
    /// Press on one element, move onto another and release there.
    /// <para>
    /// Found adopting quickshell's tab strip (QS250): a tab reorders when it is dragged across its
    /// neighbours, and no pattern moves an item along a list, so the gesture a user makes had no
    /// case. The reason is always <see cref="PointerReason.PointerIsTheAct"/>, because the
    /// movement is the act.
    /// </para>
    /// <para>
    /// The press, the moves and the release go out as separate sends with a pause between them.
    /// One <c>SendInput</c> carrying all of them reaches the application as a jump: a framework
    /// starts a drag only after it has seen the pointer move past
    /// <c>SM_CXDRAG</c>/<c>SM_CYDRAG</c> with the button down, and it reads that from mouse-move
    /// messages it has to receive one at a time.
    /// </para>
    /// <para>
    /// Both elements are admitted before anything is pressed. A drag that pressed the source and then
    /// found no destination would leave the button down over somebody's control.
    /// </para>
    /// </summary>
    /// <param name="from">What to press.</param>
    /// <param name="to">
    /// What to release over, in the same window or, built by <see cref="Subject.OnDesktop"/>, in
    /// another one (WW530). Only the release lands there: the foreground asked for is still the
    /// window the press is in.
    /// </param>
    /// <exception cref="NotActionableException">Where either element cannot take the pointer.</exception>
    public static PointerResult Onto(Subject from, Subject to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        var act = new PointerAct("drag", from.Locator, PointerReason.PointerIsTheAct, $"onto {to.Locator}");
        var admitted = Admitted.To(from);
        var facts = admitted.Facts;
        var onto = Admitted.To(to).Facts;

        // WW470's wait, as the click takes it: a window still coming forward is not a desk somebody
        // else holds.
        var foreground = Foreground
            .Waited(admitted.Window, DeskWait.Of(from.DeadlineMs, from.PollMs))
            .AsPrecondition();

        if (!foreground.Satisfied)
            return new PointerResult(act, facts, foreground, default, PatternValues.None);

        var (x, y) = Centre(facts.Bounds);
        var (toX, toY) = Centre(onto.Bounds);

        // Past the threshold before the long move, sideways always and up or down where the
        // destination is, so a drag between two elements that share a row still crosses it.
        var nudge = Math.Max(
            Win32.GetSystemMetrics(Win32.DragWidth), Win32.GetSystemMetrics(Win32.DragHeight)) + 2;
        var sideways = toX >= x ? 1 : -1;

        try
        {
            Paced(Absolute(x, y), Pointer.Mouse(Win32.MouseLeftDown, 0, 0));
            Paced(Absolute(x + (sideways * nudge), y + (Math.Sign(toY - y) * nudge)));
            for (var step = 1; step <= DragSteps; step++)
                Paced(Absolute(x + ((toX - x) * step / DragSteps), y + ((toY - y) * step / DragSteps)));
        }
        finally
        {
            // In a finally, so a throw between the press and here never leaves the button held down
            // over a window the next case drives.
            Paced(Absolute(toX, toY), Pointer.Mouse(Win32.MouseLeftUp, 0, 0));
        }

        // WW353's pause, as the click takes it before its read, so the release is not read before
        // the application has handled it.
        Thread.Sleep(Keys.FirstLookMs);

        return new PointerResult(act, facts, foreground, onto.Bounds, from.Read().Values);
    }

    internal static (int X, int Y) Centre(WindowBounds at) => (at.Left + (at.Width / 2), at.Top + (at.Height / 2));

    /// <summary>
    /// One part of a drag, sent on its own and given time to arrive as its own message. Internal for
    /// <see cref="FileDrop"/>, whose drag is made of the same parts. WW550.
    /// </summary>
    internal static void Paced(params Win32.Input[] inputs)
    {
        Win32.SendInput((uint)inputs.Length, inputs, System.Runtime.InteropServices.Marshal.SizeOf<Win32.Input>());
        Thread.Sleep(DragPauseMs);
    }

    /// <summary>A move to a point on the virtual screen, normalised the way <see cref="Pointer.Send"/> does it.</summary>
    internal static Win32.Input Absolute(int x, int y)
    {
        var left = Win32.GetSystemMetrics(Win32.VirtualScreenX);
        var top = Win32.GetSystemMetrics(Win32.VirtualScreenY);
        var width = Math.Max(1, Win32.GetSystemMetrics(Win32.VirtualScreenWidth));
        var height = Math.Max(1, Win32.GetSystemMetrics(Win32.VirtualScreenHeight));

        return Pointer.Mouse(Win32.MouseMove | Win32.MouseAbsolute | Win32.MouseVirtualDesk,
            (x - left) * 65535 / width, (y - top) * 65535 / height);
    }
}

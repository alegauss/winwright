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

        Moved(Centre(facts.Bounds), Centre(onto.Bounds));
        return new PointerResult(act, facts, foreground, onto.Bounds, from.Read().Values);
    }

    /// <summary>
    /// Press at one point of an element and release at another point of the same one. WW572.
    /// <para>
    /// Found by PhotoBoost (PB67): a crop editor drawn on a GPU surface has its handles as pixels, so
    /// there is no second element to release over, and the gesture had no case. The same press, moves
    /// and release <see cref="Onto"/> sends, at two points the case wrote inside the subject. Both
    /// points are placed before anything is pressed, so a point written past the element's size is
    /// refused with the button never down.
    /// </para>
    /// </summary>
    /// <param name="subject">The element both points are in.</param>
    /// <param name="from">Where to press.</param>
    /// <param name="to">Where to release.</param>
    /// <exception cref="NotActionableException">Where the element cannot take the pointer, or a point is outside it.</exception>
    public static PointerResult Within(Subject subject, ElementPoint from, ElementPoint to)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        var act = new PointerAct("drag", subject.Locator, PointerReason.PointerIsTheAct, $"from {from} to {to}");
        var admitted = Admitted.To(subject);
        var facts = admitted.Facts;

        var scale = Win32.ScaleOf(admitted.Window);
        var pressed = Placed(subject, facts, from, scale);
        var released = Placed(subject, facts, to, scale);

        var foreground = Foreground
            .Waited(admitted.Window, DeskWait.Of(subject.DeadlineMs, subject.PollMs))
            .AsPrecondition();

        if (!foreground.Satisfied)
            return new PointerResult(act, facts, foreground, default, PatternValues.None);

        Moved(pressed, released);
        return new PointerResult(act, facts, foreground, facts.Bounds, subject.Read().Values);
    }

    /// <summary>A point of the subject on the screen, or the refusal naming the element's size. WW572.</summary>
    private static (int X, int Y) Placed(Subject subject, ElementFacts facts, ElementPoint point, double scale) =>
        point.On(facts.Bounds, scale) ?? throw new NotActionableException(
            subject.Locator.Text,
            Actionable.Offscreen,
            $"'{point}' is outside it: it is {Math.Round(facts.Bounds.Width / scale)}x"
                + $"{Math.Round(facts.Bounds.Height / scale)} device-independent pixels, and nothing was pressed");

    /// <summary>
    /// Press at one point, move past the drag threshold and on to the other, and release there,
    /// each part its own message.
    /// </summary>
    private static void Moved((int X, int Y) from, (int X, int Y) to)
    {
        var (x, y) = from;
        var (toX, toY) = to;

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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

using Winwright.Locating;
using Winwright.Verdicts;
using Winwright.Windowing;

namespace Winwright.Acting;

/// <summary>What a file drop did: the pointer half, and whether the target took what was offered. WW550.</summary>
/// <param name="Pointer">The press, the moves and the release, or the precondition that stopped them.</param>
/// <param name="Took">
/// Whether the target answered the drop with an effect. Absent where it answered none, where no drag
/// began, or where the pointer half never ran.
/// </param>
public sealed record DropResult(PointerResult Pointer, Precondition Took);

/// <summary>
/// A file dropped onto an element from outside the application, the way one dragged out of Explorer
/// arrives. WW550.
/// <para>
/// Found by PhotoBoost (PB23): an open-image screen whose drop zone takes image files from Explorer.
/// WW520 drags one element onto another in the same window and WW530 drops out of the application
/// onto another one, and nothing started a drag outside it — so the app grew an <c>--open</c> flag
/// only its tests used, and the drop handler itself was never run by a case.
/// </para>
/// <para>
/// The drag is a real one. A small window this process opens is the source: the press lands on it,
/// it starts the system's drag loop with the file list Explorer offers (<c>CF_HDROP</c>), and the
/// pointer is carried to the target with the button down and released there. So the application sees
/// what it sees from Explorer — a drag entering, moving and dropping, through its own drop target —
/// and the effect the target answers is read back as the act's own evidence.
/// </para>
/// <para>
/// Not Explorer itself, on purpose. A folder window placed beside the application would be a second
/// application to wait for and to find a row in, and its rows move with the view's sort and grouping;
/// what the target receives is the data object, and this offers the format every drop target reads.
/// </para>
/// </summary>
public static class FileDrop
{
    /// <summary>What a case names the condition by, and what a hole says was missing. Not the desk's.</summary>
    public const string TargetTakesIt = "the target takes the drop";

    /// <summary>The source window's side, in device-independent pixels.</summary>
    private const double SourceSide = 56;

    /// <summary>How far in from the target window's corner the source window sits, in pixels.</summary>
    private const int Inset = 8;

    /// <summary>
    /// Drag <paramref name="path"/> onto <paramref name="subject"/> and release it there.
    /// </summary>
    /// <param name="subject">What to drop onto.</param>
    /// <param name="path">The file to drop, as a full path.</param>
    /// <exception cref="FileNotFoundException">Where there is no file at <paramref name="path"/>.</exception>
    /// <exception cref="NotActionableException">Where the target cannot take the pointer.</exception>
    public static DropResult Onto(Subject subject, string path)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Before anything is shown or pressed: a file that is not there is the case being wrong on
        // every machine, and it stops the case rather than reading as a target that refused.
        if (!File.Exists(path))
            throw new FileNotFoundException($"the file to drop, {path}, is not there", path);

        var act = new PointerAct("drop file", subject.Locator, PointerReason.PointerIsTheAct, $"dropping {Path.GetFileName(path)}");
        var admitted = Admitted.To(subject);
        var facts = admitted.Facts;

        // WW470's wait, as the click and the drag take it: the release has to land on this window
        // with nothing of anybody else's over it.
        var foreground = Foreground
            .Waited(admitted.Window, DeskWait.Of(subject.DeadlineMs, subject.PollMs))
            .AsPrecondition();

        if (!foreground.Satisfied)
            return new DropResult(new PointerResult(act, facts, foreground, default, PatternValues.None), foreground);

        var (toX, toY) = Drag.Centre(facts.Bounds);
        Win32.GetWindowRect(admitted.Window, out var frame);

        using var source = new Source(path, frame, toX, toY);
        if (!source.Shown(subject.ActMs))
        {
            return new DropResult(
                new PointerResult(act, facts, foreground, default, PatternValues.None),
                Precondition.Absent(TargetTakesIt, $"the window a drop of {path} starts from was not up within {subject.ActMs}ms, so nothing was pressed"));
        }

        var (x, y) = source.Centre;
        var nudge = Math.Max(
            Win32.GetSystemMetrics(Win32.DragWidth), Win32.GetSystemMetrics(Win32.DragHeight)) + 2;

        try
        {
            Drag.Paced(Drag.Absolute(x, y), Pointer.Mouse(Win32.MouseLeftDown, 0, 0));
            Drag.Paced(Drag.Absolute(x + (Math.Sign(toX - x) * nudge), y + (Math.Sign(toY - y) * nudge)));
            for (var step = 1; step <= Drag.DragSteps; step++)
                Drag.Paced(Drag.Absolute(x + ((toX - x) * step / Drag.DragSteps), y + ((toY - y) * step / Drag.DragSteps)));
        }
        finally
        {
            // In a finally for the drag's reason: a throw between the press and here must never
            // leave the button held down over a window the next case drives.
            Drag.Paced(Drag.Absolute(toX, toY), Pointer.Mouse(Win32.MouseLeftUp, 0, 0));
        }

        var answered = source.Answered(subject.ActMs, toX, toY);
        var took = (answered, source.Failure) switch
        {
            (_, { } threw) => Precondition.Absent(
                TargetTakesIt, $"the drag of {path} could not be run: {threw}"),
            (null, _) => Precondition.Absent(
                TargetTakesIt,
                $"the press on the window a drop of {path} starts from never arrived, so no drag began"),
            (DragDropEffects.None, _) => Precondition.Absent(
                TargetTakesIt,
                $"{subject.Locator} answered the drop of {Path.GetFileName(path)} with no effect: a target "
                    + "that refuses a file and one this drop could not reach read the same from here"),
            ({ } effect, _) => Precondition.Met(TargetTakesIt, $"{subject.Locator} answered {effect}"),
        };

        // The read after the drop rather than after a pause: the drag loop returns once the target's
        // own drop handler has, so what the target did with the file is already done.
        return new DropResult(new PointerResult(act, facts, foreground, facts.Bounds, subject.Read().Values), took);
    }

    /// <summary>
    /// The window a drop starts from: shown topmost in the corner of the target's window farthest from
    /// the target, never activated, and closed again when the drag is over.
    /// </summary>
    private sealed class Source : IDisposable
    {
        // Never disposed: the drag thread may still set one after the act has given up waiting, and a
        // set on a disposed event would throw on a thread nothing catches.
        private readonly ManualResetEventSlim up = new();
        private readonly ManualResetEventSlim over = new();
        private volatile Window? window;
        private Win32.Rect at;
        private volatile bool pressed;
        private volatile bool abandon;
        private volatile string? failure;
        private DragDropEffects effect = DragDropEffects.None;

        internal Source(string path, Win32.Rect frame, int toX, int toY)
        {
            // Its own apartment and its own dispatcher: the drag loop is modal on the thread that
            // starts it, and this thread is the one sending the pointer that has to end it.
            var thread = new Thread(() => Run(path, frame, toX, toY)) { IsBackground = true, Name = "winwright file drop" };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        /// <summary>The middle of the source window, in screen pixels.</summary>
        internal (int X, int Y) Centre => ((at.Left + at.Right) / 2, (at.Top + at.Bottom) / 2);

        /// <summary>What the drag thread threw, or null where it threw nothing.</summary>
        internal string? Failure => failure;

        /// <summary>Whether the window came up within <paramref name="withinMs"/>.</summary>
        internal bool Shown(int withinMs) => up.Wait(withinMs);

        /// <summary>
        /// The effect the target answered, or null where no drag began. Where the loop is still running
        /// after <paramref name="withinMs"/>, it is told to cancel and the pointer is moved once so it
        /// hears that, and the drop then reads as one the target did not take.
        /// </summary>
        internal DragDropEffects? Answered(int withinMs, int x, int y)
        {
            if (!over.Wait(withinMs) && pressed)
            {
                abandon = true;
                Drag.Paced(Drag.Absolute(x + 1, y));
                over.Wait(withinMs);
            }

            return pressed ? effect : null;
        }

        public void Dispose()
        {
            if (window is { } shown)
                shown.Dispatcher.BeginInvoke(() => shown.Close());
        }

        /// <summary>
        /// The drag thread's whole life. Everything it throws is kept rather than let out, because an
        /// exception leaving a thread nobody joined ends the process — and the process is the harness.
        /// </summary>
        private void Run(string path, Win32.Rect frame, int toX, int toY)
        {
            try
            {
                Raise(path, frame, toX, toY);
            }
            catch (Exception thrown) when (thrown is not OutOfMemoryException)
            {
                failure = thrown.Message;
                over.Set();
            }
        }

        private void Raise(string path, Win32.Rect frame, int toX, int toY)
        {
            var face = new Border
            {
                Background = Brushes.LightSteelBlue,
                BorderBrush = Brushes.SteelBlue,
                BorderThickness = new Thickness(1),
                Child = new TextBlock { Text = Path.GetFileName(path), FontSize = 9, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(2) },
            };

            var shown = new Window
            {
                Title = "winwright file drop",
                Width = SourceSide,
                Height = SourceSide,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                ShowActivated = false,
                Topmost = true,
                Content = face,
            };

            shown.SourceInitialized += (_, _) =>
            {
                var handle = new WindowInteropHelper(shown).Handle;
                var style = (long)Win32.GetWindowLongPtrW(handle, Win32.ExtendedStyle);
                Win32.SetWindowLongPtrW(handle, Win32.ExtendedStyle, (nint)(style | Win32.StyleNeverActive));
            };

            shown.ContentRendered += (_, _) =>
            {
                var handle = new WindowInteropHelper(shown).Handle;
                Win32.GetWindowRect(handle, out var size);
                var (left, top) = Corner(frame, size.Right - size.Left, size.Bottom - size.Top, toX, toY);
                Win32.SetWindowPos(handle, Win32.Topmost, left, top, 0, 0, Win32.MoveOnTop);
                Win32.GetWindowRect(handle, out at);
                up.Set();
            };

            shown.PreviewMouseLeftButtonDown += (_, e) =>
            {
                if (pressed)
                    return;

                pressed = true;
                e.Handled = true;
                try
                {
                    var data = new DataObject(DataFormats.FileDrop, new[] { path });
                    effect = DragDrop.DoDragDrop(face, data, DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);
                }
                catch (Exception thrown) when (thrown is not OutOfMemoryException)
                {
                    failure = thrown.Message;
                }
                finally
                {
                    over.Set();
                    shown.Close();
                }
            };

            // Only the cancel is this handler's. The button coming up is what ends the drag, as it
            // does from Explorer, and the system's own reading of it is left to say so.
            DragDrop.AddQueryContinueDragHandler(face, (_, e) =>
            {
                if (!abandon)
                    return;

                e.Action = DragAction.Cancel;
                e.Handled = true;
            });

            shown.Closed += (_, _) => shown.Dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
            window = shown;
            shown.Show();
            Dispatcher.Run();
        }

        /// <summary>
        /// Where the source window goes: the corner of <paramref name="frame"/>, inset, whose middle is
        /// farthest from the target, so it never covers the point the release lands on.
        /// </summary>
        private static (int Left, int Top) Corner(Win32.Rect frame, int width, int height, int toX, int toY)
        {
            (int Left, int Top)[] corners =
            [
                (frame.Left + Inset, frame.Top + Inset),
                (frame.Right - Inset - width, frame.Top + Inset),
                (frame.Left + Inset, frame.Bottom - Inset - height),
                (frame.Right - Inset - width, frame.Bottom - Inset - height),
            ];

            return corners.MaxBy(corner =>
            {
                var dx = (long)(corner.Left + (width / 2) - toX);
                var dy = (long)(corner.Top + (height / 2) - toY);
                return (dx * dx) + (dy * dy);
            });
        }
    }
}

using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Tracing;
using Winwright.Verdicts;
using Winwright.Windowing;

namespace Winwright.Acting;

/// <summary>Which button a synthesized click presses.</summary>
public enum MouseButton
{
    /// <summary>The primary button.</summary>
    Left,

    /// <summary>The secondary one, which is what opens a context menu.</summary>
    Right,

    /// <summary>The wheel, pressed.</summary>
    Middle,
}

/// <summary>
/// One pointer act as a scenario declares it. It is a separate kind from the pattern acts on
/// purpose: what needs a real desktop is then countable by reading the file, rather than
/// discovered on the run where the desktop was busy.
/// </summary>
/// <param name="Verb">What the act is, as the scenario names it.</param>
/// <param name="Locator">What it addresses.</param>
/// <param name="Button">Which button it presses.</param>
/// <param name="Clicks">How many times, which is how a double click is said.</param>
public sealed record PointerAct(string Verb, Locator Locator, MouseButton Button = MouseButton.Left, int Clicks = 1)
{
    /// <summary>The one line a report names it by.</summary>
    public override string ToString() =>
        $"{Verb} {Locator} ({Clicks} {Button.ToString().ToLowerInvariant()} click{(Clicks == 1 ? "" : "s")})";
}

/// <summary>What a pointer act did, or the precondition that stopped it before it did anything.</summary>
public sealed record PointerResult
{
    internal PointerResult(
        PointerAct act, ElementFacts? element, Precondition foreground, WindowBounds at, PatternValues after)
    {
        Act = act;
        Element = element;
        Foreground = foreground;
        At = at;
        After = after;
    }

    /// <summary>The act as it was declared.</summary>
    public PointerAct Act { get; }

    /// <summary>What it addressed, or null where nothing resolved.</summary>
    public ElementFacts? Element { get; }

    /// <summary>
    /// Whether the window under test owned the keyboard and the pointer. Absent means nothing was
    /// synthesized: input sent to somebody else's window is not a weaker version of this act, it
    /// is a different act against a window nobody asked about.
    /// </summary>
    public Precondition Foreground { get; }

    /// <summary>Where the click landed, in screen coordinates. Empty where nothing was sent.</summary>
    public WindowBounds At { get; }

    /// <summary>What the element read afterwards, where there was one.</summary>
    public PatternValues After { get; }

    /// <summary>Whether input was actually synthesized.</summary>
    public bool Landed => Foreground.Satisfied && Element is not null;

    /// <summary>The step a trace records, unchecked where the desktop was not ours.</summary>
    public TraceStep AsTraceStep() => new()
    {
        Verb = Act.Verb,
        Locator = Act.Locator.Text,
        Resolved = Element?.ToString(),
        Pattern = "synthesized input",
        ReadBack = Landed ? After.Reading() : null,
        Verdict = Landed ? StepVerdict.Ok : StepVerdict.Unchecked,
        Detail = Landed ? null : Foreground.Absence,
    };
}

/// <summary>
/// Synthesized pointer input, for the controls that have no pattern at all: a bare border with no
/// automation peer, a notification-area icon, a segment of a custom template.
/// <para>
/// Reaching for the mouse there is right. Doing it silently is not, because the act then carries a
/// precondition the file never mentions — so nothing in this project falls back to a pointer, and
/// a scenario that needs one says so. <see cref="Summarise"/> is what makes the set countable.
/// </para>
/// </summary>
public static class Pointer
{
    /// <summary>Press once with the primary button.</summary>
    public static PointerResult Click(Subject subject, MouseButton button = MouseButton.Left, int clicks = 1)
    {
        ArgumentNullException.ThrowIfNull(subject);
        return Run(new PointerAct("click", subject.Locator, button, clicks), subject);
    }

    /// <summary>Press twice, which some templates need and no pattern expresses.</summary>
    public static PointerResult DoubleClick(Subject subject, MouseButton button = MouseButton.Left) =>
        Click(subject, button, clicks: 2);

    /// <summary>
    /// Run a declared act against its subject. The element must be actionable and the window must
    /// own the foreground; without the second, nothing is sent and the result says why.
    /// </summary>
    /// <exception cref="NotActionableException">Where the element cannot take a click at all.</exception>
    public static PointerResult Run(PointerAct act, Subject subject)
    {
        ArgumentNullException.ThrowIfNull(act);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(act.Clicks);

        // A pointer act needs no pattern — that is the whole reason it exists — but it does need
        // the element to be there, on screen and enabled, which is the rest of actionability. The
        // admission is how it is reached at all, so the check cannot be the line somebody drops.
        var admitted = Admitted.To(subject);
        var facts = admitted.Facts;

        var foreground = Foreground.Check(admitted.Window).AsPrecondition();
        if (!foreground.Satisfied)
            return new PointerResult(act, facts, foreground, default, PatternValues.None);

        var at = facts.Bounds;
        Send(at.Left + (at.Width / 2), at.Top + (at.Height / 2), act.Button, act.Clicks);

        return new PointerResult(act, facts, foreground, at, subject.Read().Values);
    }

    /// <summary>
    /// What in a scenario needs a real desktop, said out loud. This is the point of declaring
    /// them: the cost lands where the reader is instead of on the run where it was discovered.
    /// </summary>
    public static string Summarise(IReadOnlyList<PointerAct> acts)
    {
        ArgumentNullException.ThrowIfNull(acts);
        return acts.Count == 0
            ? "no act here needs a real desktop."
            : $"{acts.Count} act{(acts.Count == 1 ? "" : "s")} need a real desktop: "
                + string.Join(", ", acts.Select(one => one.ToString())) + ".";
    }

    private static void Send(int x, int y, MouseButton button, int clicks)
    {
        var (down, up) = button switch
        {
            MouseButton.Right => (Win32.MouseRightDown, Win32.MouseRightUp),
            MouseButton.Middle => (Win32.MouseMiddleDown, Win32.MouseMiddleUp),
            _ => (Win32.MouseLeftDown, Win32.MouseLeftUp),
        };

        var left = Win32.GetSystemMetrics(Win32.VirtualScreenX);
        var top = Win32.GetSystemMetrics(Win32.VirtualScreenY);
        var width = Math.Max(1, Win32.GetSystemMetrics(Win32.VirtualScreenWidth));
        var height = Math.Max(1, Win32.GetSystemMetrics(Win32.VirtualScreenHeight));

        var inputs = new List<Win32.Input>
        {
            Mouse(Win32.MouseMove | Win32.MouseAbsolute | Win32.MouseVirtualDesk,
                (x - left) * 65535 / width, (y - top) * 65535 / height),
        };

        for (var press = 0; press < clicks; press++)
        {
            inputs.Add(Mouse(down, 0, 0));
            inputs.Add(Mouse(up, 0, 0));
        }

        Win32.SendInput((uint)inputs.Count, [.. inputs], System.Runtime.InteropServices.Marshal.SizeOf<Win32.Input>());
    }

    private static Win32.Input Mouse(uint flags, int dx, int dy) => new()
    {
        Type = Win32.InputMouse,
        Payload = new Win32.InputPayload
        {
            Mouse = new Win32.MouseInput { Dx = dx, Dy = dy, Flags = flags },
        },
    };
}

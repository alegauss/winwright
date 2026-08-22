using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Tracing;
using Winwright.Verdicts;
using Winwright.Windowing;

namespace Winwright.Acting;

/// <summary>The keys a scenario traverses with. Only the ones this project has proven land.</summary>
public enum TraversalKey
{
    /// <summary>Forward through the tab order.</summary>
    Tab,

    /// <summary>Backward through it.</summary>
    ShiftTab,

    /// <summary>Right, which increases a horizontal range.</summary>
    Right,

    /// <summary>Left, which decreases one.</summary>
    Left,

    /// <summary>Up, which increases a vertical range.</summary>
    Up,

    /// <summary>Down, which decreases one.</summary>
    Down,
}

/// <summary>Where the focus went after a traversal key, and where it was.</summary>
public sealed record TraversalResult
{
    internal TraversalResult(
        TraversalKey key, ElementFacts? before, ElementFacts? after, bool moved, Precondition foreground)
    {
        Key = key;
        Before = before;
        After = after;
        Moved = moved;
        Foreground = foreground;
    }

    /// <summary>Which key was pressed.</summary>
    public TraversalKey Key { get; }

    /// <summary>What held the focus before.</summary>
    public ElementFacts? Before { get; }

    /// <summary>What holds it now — which is the answer, and not merely whether it changed.</summary>
    public ElementFacts? After { get; }

    /// <summary>Whether it moved at all.</summary>
    public bool Moved { get; }

    /// <summary>Whether the window owned the desktop. Absent means no key was sent.</summary>
    public Precondition Foreground { get; }

    /// <summary>Whether a key was sent at all.</summary>
    public bool Sent => Foreground.Satisfied;

    /// <summary>Where the focus went, said either way.</summary>
    public override string ToString()
    {
        if (!Sent)
            return $"{Key} was not sent: {Foreground.Absence}.";

        return Moved
            ? $"{Key} moved the focus from {Named(Before)} to {Named(After)}."
            : $"{Key} left the focus on {Named(After)}.";
    }

    /// <summary>The step a trace records.</summary>
    public TraceStep AsTraceStep() => new()
    {
        Verb = Key.ToString().ToLowerInvariant(),
        Locator = Named(Before),
        Resolved = Named(After),
        Pattern = "synthesized keyboard",
        ReadBack = Named(After),
        Verdict = !Sent ? StepVerdict.Unchecked : Moved ? StepVerdict.Ok : StepVerdict.Failed,
        Detail = Sent && Moved ? null : ToString(),
    };

    private static string Named(ElementFacts? facts) => facts?.ToString() ?? "nothing";
}

/// <summary>What nudging a range did, and which way it had to go.</summary>
public sealed record NudgeResult
{
    internal NudgeResult(
        ElementFacts element, TraversalKey pressed, double before, double after, bool reversed, Precondition foreground)
    {
        Element = element;
        Pressed = pressed;
        Before = before;
        After = after;
        ReversedBecauseItWasAtTheEnd = reversed;
        Foreground = foreground;
    }

    /// <summary>The control that was nudged.</summary>
    public ElementFacts Element { get; }

    /// <summary>The key actually used, which is not always the one a caller would have guessed.</summary>
    public TraversalKey Pressed { get; }

    /// <summary>What it read before.</summary>
    public double Before { get; }

    /// <summary>What it reads now.</summary>
    public double After { get; }

    /// <summary>
    /// Whether the direction was flipped because the control already sat at that end. At the
    /// maximum a press upward is a legitimate no-op, so pressing it would test the starting value
    /// rather than the control.
    /// </summary>
    public bool ReversedBecauseItWasAtTheEnd { get; }

    /// <summary>Whether the window owned the desktop.</summary>
    public Precondition Foreground { get; }

    /// <summary>Whether a key was sent at all.</summary>
    public bool Sent => Foreground.Satisfied;

    /// <summary>Whether the control moved.</summary>
    public bool Moved => Sent && Before != After;

    /// <summary>What happened, with the reason for the direction where it was not the obvious one.</summary>
    public override string ToString()
    {
        if (!Sent)
            return $"{Pressed} was not sent: {Foreground.Absence}.";

        var why = ReversedBecauseItWasAtTheEnd ? $" ({Pressed} because it was already at the end)" : "";
        return Moved
            ? $"{Element} moved from {Before} to {After}{why}."
            : $"{Element} stayed at {Before}{why}.";
    }
}

/// <summary>
/// Keyboard traversal, which has no observable in a picture at all.
/// <para>
/// Tab moving focus is a property of the window and nothing a screenshot shows. What holds the
/// focus after the key is read and named, so a failure says where the focus actually went rather
/// than only that it did not move.
/// </para>
/// </summary>
public static class Traversal
{
    /// <summary>What holds the keyboard focus right now, anywhere on the desktop.</summary>
    public static ElementFacts? WhoHasFocus()
    {
        try
        {
            return ElementFacts.Of(AutomationElement.FocusedElement);
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
    }

    /// <summary>
    /// Press a traversal key at <paramref name="window"/> and read where the focus went. The wait
    /// is a deadline on the focus changing; a key that legitimately moves nothing costs all of it
    /// and then says what still holds the focus, which is the useful half of that answer.
    /// </summary>
    public static TraversalResult Press(
        AutomationElement window, TraversalKey key, int settleMs = 2000, int pollMs = 25)
    {
        ArgumentNullException.ThrowIfNull(window);

        var handle = (nint)window.Current.NativeWindowHandle;
        var foreground = Foreground.Check(handle == 0 ? 0 : Win32.GetAncestor(handle, Win32.GaRoot)).AsPrecondition();
        var before = FocusedElement();
        if (!foreground.Satisfied)
            return new TraversalResult(key, ElementFacts.Of(before), ElementFacts.Of(before), false, foreground);

        Keys.Send(key);

        var moved = Attempt.Until(
            () =>
            {
                var now = FocusedElement();
                return now is not null && !Same(now, before) ? now : null;
            },
            settleMs,
            pollMs);

        var after = moved.Value ?? FocusedElement();
        return new TraversalResult(key, ElementFacts.Of(before), ElementFacts.Of(after), moved.Found, foreground);
    }

    /// <summary>
    /// Nudge a range control with an arrow key, choosing the direction that can actually move it.
    /// <para>
    /// At the maximum a press in that direction is a legitimate no-op, so the other one is used —
    /// which keeps the assertion about whether the control responds rather than about where it
    /// happened to start.
    /// </para>
    /// </summary>
    /// <exception cref="NotActionableException">
    /// Where the control offers no range, or where its range has no room to move in either
    /// direction, which is a control nothing could nudge and a scenario that proves nothing.
    /// </exception>
    public static NudgeResult Nudge(Subject slider, bool vertical = false)
    {
        ArgumentNullException.ThrowIfNull(slider);

        var admitted = Admitted.To(slider, "RangeValue");
        var facts = admitted.Facts;
        var value = admitted.Values.Range!.Value;
        var minimum = admitted.Values.RangeMinimum ?? double.MinValue;
        var maximum = admitted.Values.RangeMaximum ?? double.MaxValue;
        if (minimum >= maximum)
        {
            throw new NotActionableException(
                slider.Locator.Text,
                Actionable.PatternMissing,
                $"{facts} accepts only {minimum}, so no key could move it and no nudge would prove anything");
        }

        var atTheTop = value >= maximum;
        var pressed = (vertical, atTheTop) switch
        {
            (true, true) => TraversalKey.Down,
            (true, false) => TraversalKey.Up,
            (false, true) => TraversalKey.Left,
            _ => TraversalKey.Right,
        };

        var foreground = Foreground.Check(admitted.Window).AsPrecondition();
        if (!foreground.Satisfied)
            return new NudgeResult(facts, pressed, value, value, atTheTop, foreground);

        admitted.Do(element => element.SetFocus());
        Keys.Send(pressed);

        Attempt.UntilTrue(() => slider.ReadOnce().Values.Range != value, slider.ActMs, slider.PollMs);

        return new NudgeResult(
            facts, pressed, value, slider.ReadOnce().Values.Range ?? value, atTheTop, foreground);
    }

    private static AutomationElement? FocusedElement()
    {
        try
        {
            return AutomationElement.FocusedElement;
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
    }

    private static bool Same(AutomationElement left, AutomationElement? right)
    {
        try
        {
            return right is not null && Automation.Compare(left, right);
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
    }
}

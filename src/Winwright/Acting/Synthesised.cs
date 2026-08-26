using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Verdicts;

namespace Winwright.Acting;

/// <summary>
/// The synthesised acts, answered in the shape a step of a case is answered in.
/// <para>
/// WW225. A case could name eight acts, all of them through a control's own pattern, and that is
/// exactly the set that fits <see cref="ActResult"/>. The families that put input on the desk —
/// <see cref="Keyboard"/>, <see cref="Pointer"/>, <see cref="Traversal"/> — each answer with their
/// own record carrying their own preconditions, and none of them was reachable from a data file. So
/// WW78's keyboard case, whose whole point is that a WPF window under a WinForms pump took no
/// keyboard input at all, would have migrated into <c>set value</c> and <c>set range</c>: the two
/// pattern routes that passed on the day of the bug.
/// </para>
/// <para>
/// Deliberately not on <see cref="Act"/>, and this is the interesting part. Block D's first criterion
/// is that the default act needs no foreground, and <c>SynthesisingTests</c> asserts that <c>Act</c>
/// reaches no send at any depth across any file. An adapter living there would dissolve the
/// distinction that criterion exists to keep — and the check said so the moment it was tried, which
/// is the difference between a rule and a note.
/// </para>
/// <para>
/// Each of these has a pattern act beside it that reads almost the same and proves something else.
/// That is not duplication: <c>set value</c> writes through ValuePattern and <c>type</c> presses
/// keys, and which of the two a case names is the whole of what an interaction loop is for.
/// </para>
/// </summary>
public static class Synthesised
{
    /// <summary>
    /// The route the keyboard acts went through, in the field <see cref="ActResult.Pattern"/> carries.
    /// Named rather than repeated, and a route and not a pattern: <c>Pick</c> has written
    /// <c>"synthesized keyboard"</c> there since block D, so the field already says which door an act
    /// took rather than which pattern it asked.
    /// </summary>
    public const string ByKeyboard = "synthesised keyboard";

    /// <summary>
    /// Type into a control with real keys, and read back what it says afterwards.
    /// </summary>
    /// <param name="subject">What to type into.</param>
    /// <param name="text">What to type. What was there is erased first.</param>
    /// <exception cref="NotActionableException">Where the element cannot take the act.</exception>
    public static ActResult Type(Subject subject, string text)
    {
        ArgumentNullException.ThrowIfNull(subject);

        var before = subject.Read();
        var typed = Keyboard.Type(subject, text);

        // The foreground first, then the focus: read in the order they stop each other mattering, so
        // a run that never owned the desktop is reported as that rather than as a control that would
        // not take the focus underneath it.
        return Landed(
            subject,
            "type",
            ByKeyboard,
            typed.Element,
            before,
            typed.Foreground.Satisfied ? typed.Focus : typed.Foreground);
    }

    /// <summary>
    /// Press a control with the pointer, because no pattern would express it and this says which.
    /// <para>
    /// The reason is a field and never inferred, for the reason <see cref="Pointer"/> made it one: a
    /// click whose justification defaults is a click nobody had to justify, and then every act
    /// quietly escalates and the suite is driving the desktop instead of asking controls.
    /// </para>
    /// </summary>
    /// <param name="subject">What to press.</param>
    /// <param name="because">Why the pattern route is unavailable, named rather than guessed.</param>
    public static ActResult Click(Subject subject, PointerReason because)
    {
        ArgumentNullException.ThrowIfNull(subject);

        var before = subject.Read();
        var clicked = Pointer.Click(subject, because);

        return Landed(
            subject, "click", $"synthesised pointer ({because})", clicked.Element, before, clicked.Foreground);
    }

    /// <summary>
    /// Move a range control one step with an arrow key, in whichever direction can actually move it.
    /// <para>
    /// At the end of its range the other direction is used, so the assertion stays about whether the
    /// control responds rather than about where it happened to start. WW226 is what made that branch
    /// reachable: it drew a control already sitting at its maximum, which is the only thing that
    /// provokes the flip.
    /// </para>
    /// </summary>
    /// <param name="subject">The range control.</param>
    /// <param name="vertical">That it is driven by Up and Down rather than by Left and Right.</param>
    /// <exception cref="NotActionableException">Where it offers no range, or none with room to move.</exception>
    public static ActResult Nudge(Subject subject, bool vertical = false)
    {
        ArgumentNullException.ThrowIfNull(subject);

        var before = subject.Read();
        var nudged = Traversal.Nudge(subject, vertical);

        return Landed(subject, "nudge", ByKeyboard, nudged.Element, before, nudged.Foreground);
    }

    /// <summary>
    /// Send a traversal key at the window this element is in, and read the element again after.
    /// <para>
    /// WW225. The act the keyboard case's second assertion needs, and the one that could not be
    /// written at all: <em>Tab moves the focus off this box</em>. The locator is the element the step
    /// is about rather than the window the key goes to, because that is what the expectation is
    /// then about — pair it with the <c>focused</c> reading and the claim is exactly the one
    /// claude-tray's harness made in eight lines of focus bookkeeping.
    /// </para>
    /// </summary>
    /// <param name="subject">The element the step is about. The key is sent at its window.</param>
    /// <param name="key">Which traversal key.</param>
    public static ActResult Press(Subject subject, TraversalKey key)
    {
        ArgumentNullException.ThrowIfNull(subject);

        var before = subject.Read();

        // The window rather than the element: a traversal key is a keystroke at whatever holds the
        // focus, and sending it at an element would say this act is about that element's own input.
        var window = subject.Window == 0 ? null : AutomationElement.FromHandle(subject.Window);
        if (window is null)
        {
            return Landed(
                subject,
                $"press {key}",
                ByKeyboard,
                before.Facts,
                before,
                Precondition.Absent(Windowing.Desk.ForegroundToTake, "this element is in no window a key could be sent to"));
        }

        var pressed = Traversal.Press(window, key, subject.ActMs, subject.PollMs);
        return Landed(subject, $"press {key}", ByKeyboard, before.Facts, before, pressed.Foreground);
    }

    /// <summary>
    /// What a synthesised act answers: the readings either side, and what it needed of the machine —
    /// which is the field the pattern acts leave null and none of these can.
    /// </summary>
    private static ActResult Landed(
        Subject subject,
        string verb,
        string route,
        ElementFacts? element,
        Reading before,
        Precondition needed)
    {
        // Read again through the subject rather than through whatever the verb held: an act that was
        // declined still has to answer with a reading taken now, or "nothing moved" is a claim about
        // an instant before the act was even refused.
        var after = subject.Read();
        return new ActResult(
            verb,
            subject.Locator,
            route,
            element ?? before.Facts ?? after.Facts!,
            before.Values,
            after.Values,
            before.Resolution.WaitedMs,
            before.Resolution.Polls,
            needed);
    }
}

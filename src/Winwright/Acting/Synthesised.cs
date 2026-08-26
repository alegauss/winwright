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
            "synthesised keyboard",
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

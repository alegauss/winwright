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
    /// What <see cref="ExpandMenu" /> reports itself as, which is the same word a case writes.
    /// Named once so the verb and the act cannot spell it two ways — the drift
    /// <see cref="OverflowState" />'s own condition name was fixed for.
    /// </summary>
    public const string ExpandsMenu = "open submenu";

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
    /// Press a key with modifiers held, at whatever holds the focus in this element's window.
    /// <para>
    /// WW317. The traversal half of <c>press</c> claims the focus moved, because that is what Tab
    /// and the arrows are for. A chord is the other kind of keystroke: it invokes a command, and a
    /// command that opened a dialog or wrote a file has moved no focus this act can read. So this
    /// makes no claim of its own — it is a navigation, and the step after it is the check, which is
    /// the same shape <c>click</c> has and for the same reason.
    /// </para>
    /// <para>
    /// The foreground is read before the keys are sent and reported either way, so a chord that went
    /// nowhere is a hole naming the desk rather than a command that did not run.
    /// </para>
    /// </summary>
    /// <param name="subject">Any element of the window the keys are sent to.</param>
    /// <param name="chord">The chord, parsed when the case was declared.</param>
    public static ActResult Press(Subject subject, Chord chord)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(chord);

        var before = subject.Read();
        var named = $"press {chord}";

        // The window and not the element, exactly as the traversal half: a chord is a keystroke at
        // whatever holds the focus, and sending it at an element would say this act is about that
        // element's own input.
        if (subject.Window == 0)
        {
            return Landed(
                subject,
                named,
                ByKeyboard,
                before.Facts,
                before,
                Precondition.Absent(
                    Windowing.Desk.ForegroundToTake, "this element is in no window a key could be sent to"));
        }

        var top = Windowing.Win32.GetAncestor(subject.Window, Windowing.Win32.GaRoot);
        var foreground = Windowing.Foreground.Check(top).AsPrecondition();
        if (!foreground.Satisfied)
            return Landed(subject, named, ByKeyboard, before.Facts, before, foreground);

        Keys.Send(chord);

        // Read afterwards rather than not at all, so the trace carries what the window looked like
        // when the command had been sent — the step that checks the consequence is the next one, and
        // a reader of its red wants to know this one landed.
        return Landed(subject, named, ByKeyboard, subject.Read().Facts ?? before.Facts, before, foreground);
    }

    /// <summary>
    /// Open the submenu of whatever is highlighted in this window's menu, by pressing Right — the
    /// keyboard half of the pair <c>expand</c> is the pattern half of.
    /// <para>
    /// WW259. <c>expand</c> asks ExpandCollapsePattern, and the menu this exists for exposes none: a
    /// WinForms submenu that is empty when the menu opens offers no ExpandCollapse at all, draws no
    /// arrow, and the shell handles Right as <em>activate a plain command</em>, which dismisses the
    /// whole menu. A mouse hover always worked, which is why it went unnoticed until something drove
    /// it from the keyboard — and a case naming <c>expand</c> against that menu would ask the pattern,
    /// find nothing to ask, and report a control rather than the gesture.
    /// </para>
    /// <para>
    /// The element handed back is what the menu landed on rather than what the locator matched, and
    /// that is the whole of how a case states the claim: paired with the <c>name</c> reading, an
    /// <c>expect</c> names the submenu entry and <c>moves</c> says the highlight went somewhere
    /// without naming where. No new reading is needed, because the highlight already is the focus.
    /// </para>
    /// <para>
    /// Not repeatable, for the reason <see cref="Menu" /> gives about its own retries: Right again
    /// walks deeper rather than arriving again, so a second attempt is a different gesture.
    /// </para>
    /// <para>
    /// WW83. The locator names <em>which</em> entry, and the walk goes there before Right is pressed.
    /// It did not, and the sentence here said so: the walk was window-scoped, on the ground that a
    /// menu popup's entries are not reliably addressable. They are — a popup is a top-level Menu
    /// window of its own — and what the old shape actually meant was that a step naming the fourth
    /// entry expanded the first, because that is the one a menu opens highlighting.
    /// </para>
    /// </summary>
    /// <param name="subject">Any element of the window whose menu is open.</param>
    public static ActResult ExpandMenu(Subject subject)
    {
        ArgumentNullException.ThrowIfNull(subject);

        var before = subject.Read();

        // The same hole `Press` answers with, and for the same reason: an element in no window is an
        // element no key could be sent at, which is a fact about the tree and not a menu that refused.
        if (subject.Window == 0)
        {
            return Landed(
                subject,
                ExpandsMenu,
                ByKeyboard,
                before.Facts,
                before,
                Precondition.Absent(
                    Windowing.Desk.ForegroundToTake, "this element is in no window a menu key could be sent to"));
        }

        // WW83. Walked to first, where the locator named an entry that is not the one the menu opened
        // on. Right expands whatever is highlighted, and a menu opens highlighting its first entry —
        // so a step naming the fourth entry used to expand the first, and the submenu that arrived
        // was somebody else's. The harness this replaces walked with Down for exactly this reason.
        //
        // A menu entry and nothing else, which keeps the sentence above true rather than replacing
        // it: the old contract took any element of the window the menu belongs to, and a walk looking
        // for a name no entry has would press Down at every entry there is and then expand whichever
        // one it stopped on. Skipped too where the locator named what is already highlighted.
        if (before.Facts is { ControlType: "MenuItem", Says: { } wanted }
            && Menu.Highlighted(subject.Window) != wanted)
        {
            Menu.To(subject.Window, wanted, subject.ActMs, subject.PollMs);
        }

        var walk = Menu.Expand(subject.Window, subject.ActMs, subject.PollMs);

        // What the menu landed on where it landed anywhere, and what the locator matched otherwise.
        // A walk the desk refused read nothing, so its own reading is not one to hand back.
        return Landed(subject, ExpandsMenu, ByKeyboard, walk.Focus.Held ?? before.Facts, before, walk.Foreground);
    }

    /// <summary>
    /// Reach a value in a picker, by the selection pattern where that works and by the keyboard where
    /// it does not.
    /// <para>
    /// WW254. <see cref="Acting.Pick"/> has walked pickers since block D and no case could name it, so
    /// the profiles case — the only thing in claude-tray that drives one — had no first step to write.
    /// The value the case asks for is the argument; the route and the number of switches it took come
    /// back in the field a pattern act puts its pattern in, because a claim about one switch is void
    /// when the walk made several and the reader of that claim is reading this line.
    /// </para>
    /// <para>
    /// Here rather than on <see cref="Act"/> for the same reason as the four above it: the fallback
    /// sends keys, and block D's criterion is that the default act reaches no send at any depth. It is
    /// the only one of the five that tries not to — the pattern route is attempted first and needs
    /// nothing of the desk, which is why <see cref="ActResult.Needed"/> and not the verb is what says
    /// whether this particular run was one a busy desk could have taken away.
    /// </para>
    /// </summary>
    /// <param name="subject">The picker.</param>
    /// <param name="wanted">The value to reach, by name.</param>
    /// <exception cref="NotActionableException">
    /// Where the picker cannot take the act, or holds no value by that name — and then the refusal
    /// lists what it does hold.
    /// </exception>
    public static ActResult Pick(Subject subject, string wanted)
    {
        ArgumentNullException.ThrowIfNull(subject);

        var before = subject.Read();
        var picked = Acting.Pick.Value(subject, wanted);

        return Landed(subject, "pick", picked.Door, picked.Container, before, picked.Foreground);
    }

    /// <summary>
    /// Reach whatever sits at a position in a picker, the same two routes as <see cref="Pick"/>.
    /// <para>
    /// WW267. The verb for a picker whose values are the machine's data rather than the application's
    /// vocabulary — a profile list, an account, a device — where naming one is the hardcoded
    /// expectation with the worst possible scope. What it reached is still reported by name, because
    /// the engine can read what the case could not have written.
    /// </para>
    /// </summary>
    /// <param name="subject">The picker.</param>
    /// <param name="index">Which position, counted from the top and from zero.</param>
    /// <exception cref="NotActionableException">
    /// Where the picker cannot take the act, or holds no such position.
    /// </exception>
    public static ActResult PickAt(Subject subject, int index)
    {
        ArgumentNullException.ThrowIfNull(subject);

        var before = subject.Read();
        var picked = Acting.Pick.At(subject, index);

        return Landed(subject, "pick at", picked.Door, picked.Container, before, picked.Foreground);
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

using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Tracing;
using Winwright.Verdicts;
using Winwright.Windowing;

namespace Winwright.Acting;

/// <summary>One typing act as a scenario declares it.</summary>
/// <param name="Verb">What the act is, as the scenario names it.</param>
/// <param name="Locator">What it types into.</param>
/// <param name="Text">What it types.</param>
/// <param name="ReplacingWhatIsThere">Whether what is already there is erased first.</param>
public sealed record TypedAct(string Verb, Locator Locator, string Text, bool ReplacingWhatIsThere = true)
{
    /// <summary>The one line a report names it by.</summary>
    public override string ToString() => $"{Verb} \"{Text}\" into {Locator}";
}

/// <summary>What typing did, and what the control says it now holds.</summary>
public sealed record TypedResult
{
    internal TypedResult(
        TypedAct act,
        ElementFacts? element,
        Precondition foreground,
        Precondition focus,
        string? before,
        string? readBack,
        bool readOnly)
    {
        Act = act;
        Element = element;
        Foreground = foreground;
        Focus = focus;
        Before = before;
        ReadBack = readBack;
        ReadOnly = readOnly;
    }

    /// <summary>The act as it was declared.</summary>
    public TypedAct Act { get; }

    /// <summary>What it typed into, or null where nothing resolved.</summary>
    public ElementFacts? Element { get; }

    /// <summary>Whether the window owned the desktop. Absent means no key was sent.</summary>
    public Precondition Foreground { get; }

    /// <summary>Whether the control actually held the keyboard focus. Absent means no key was sent.</summary>
    public Precondition Focus { get; }

    /// <summary>What the control said before.</summary>
    public string? Before { get; }

    /// <summary>What the control says now — the only observable that separates the two input paths.</summary>
    public string? ReadBack { get; }

    /// <summary>Whether the control says it is read-only, which is one reason typing lands nowhere.</summary>
    public bool ReadOnly { get; }

    /// <summary>Whether keys were sent at all.</summary>
    public bool Sent => Foreground.Satisfied && Focus.Satisfied;

    /// <summary>
    /// Whether the text arrived. This is the whole task: a picture of the window cannot tell a
    /// live input path from one that swallowed every key, and the value the control reports can.
    /// </summary>
    public bool Arrived => Sent && Expected() == ReadBack;

    /// <summary>What the control should say if the text arrived.</summary>
    public string Expected() => Act.ReplacingWhatIsThere ? Act.Text : (Before ?? "") + Act.Text;

    /// <summary>The reading in one sentence, whichever way it went.</summary>
    public override string ToString()
    {
        if (!Foreground.Satisfied)
            return $"{Act}: nothing was sent, {Foreground.Absence}.";
        if (!Focus.Satisfied)
            return $"{Act}: nothing was sent, {Focus.Absence}.";
        if (Arrived)
            return $"{Act}: the control reads \"{ReadBack}\".";

        var because = ReadOnly ? ", and the control says it is read-only" : "";
        return $"{Act}: the control reads \"{ReadBack}\" and not \"{Expected()}\"{because}.";
    }

    /// <summary>The step a trace records, unchecked where no key was sent.</summary>
    public TraceStep AsTraceStep() => new()
    {
        Verb = Act.Verb,
        Locator = Act.Locator.Text,
        Resolved = Element?.ToString(),
        Pattern = "synthesized keyboard",
        ReadBack = ReadBack,
        Verdict = !Sent ? StepVerdict.Unchecked : Arrived ? StepVerdict.Ok : StepVerdict.Failed,
        Detail = Sent ? Arrived ? null : ToString() : Foreground.Satisfied ? Focus.Absence : Foreground.Absence,
    };
}

/// <summary>
/// Typing, through the keyboard and read back through the control.
/// <para>
/// The windows in claude-tray accepted no keyboard input at all from the day the first one
/// shipped, while every screenshot ever taken of them looked perfect — because mouse input travels
/// the window procedure and keyboard input travels the component dispatcher, and those are
/// different input environments. Setting a value through the pattern would pass on such a window.
/// Sending real keys and reading back what the control reports is the only observable that
/// separates the two, and it is why an interaction loop exists beside the picture loop at all.
/// </para>
/// </summary>
public static class Keyboard
{
    /// <summary>The name every scenario refers to the focus condition by.</summary>
    public const string FocusPreconditionName = "the control under test holds the keyboard focus";

    /// <summary>Type into a control and read back what it says afterwards.</summary>
    /// <exception cref="NotActionableException">Where the element cannot take the act.</exception>
    public static TypedResult Type(Subject subject, string text, bool replacingWhatIsThere = true)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(text);
        return Run(new TypedAct("type", subject.Locator, text, replacingWhatIsThere), subject);
    }

    /// <summary>Run a declared typing act against its subject.</summary>
    /// <exception cref="NotActionableException">
    /// Where the element is missing, offscreen or disabled — or where it reports no value at all,
    /// because typing that cannot be read back is the screenshot this task exists to replace.
    /// </exception>
    public static TypedResult Run(TypedAct act, Subject subject)
    {
        ArgumentNullException.ThrowIfNull(act);
        ArgumentNullException.ThrowIfNull(subject);

        // Typing needs no one pattern, so the admission asks for none — and then this verb's own
        // rule runs on top of the four, which is where a verb's extra judgement belongs.
        var admitted = Admitted.To(subject);
        var reading = admitted.Reading;

        var facts = admitted.Facts;
        if (!facts.Supports("Value") && !facts.Supports("Text"))
        {
            throw new NotActionableException(
                subject.Locator.Text,
                Actionable.PatternMissing,
                $"{facts} reports no value, so what was typed could not be read back — "
                + "and typing nobody can read back is the screenshot this act exists to replace.");
        }

        var before = reading.Values.Value ?? reading.Values.Text;
        var readOnly = reading.Values.IsReadOnly ?? false;

        var foreground = Foreground.Check(admitted.Window).AsPrecondition();
        if (!foreground.Satisfied)
            return new TypedResult(act, facts, foreground, Precondition.Met(FocusPreconditionName), before, before, readOnly);

        var focus = admitted.Do(element => TakeFocus(element, facts));
        if (!focus.Satisfied)
            return new TypedResult(act, facts, foreground, focus, before, before, readOnly);

        MoveToTheEnd();
        if (act.ReplacingWhatIsThere)
            Erase((before ?? "").Length);

        Send(act.Text);

        // Wait for it to show up rather than reading the instant after sending: keys go into a
        // queue another thread drains, so an immediate read is a race whichever way it lands.
        // A deadline on a condition, and the failing case costs the whole of it and says what it
        // found — which is the honest price of not reporting a value from before the act.
        var expected = act.ReplacingWhatIsThere ? act.Text : (before ?? "") + act.Text;
        var settled = Attempt.Until(
            () => Reading(subject) == expected ? expected : null, subject.ActMs, subject.PollMs);

        return new TypedResult(
            act, facts, foreground, focus, before, settled.Found ? expected : Reading(subject), readOnly);
    }

    private static string? Reading(Subject subject)
    {
        var values = subject.ReadOnce().Values;
        return values.Value ?? values.Text;
    }

    private static Precondition TakeFocus(AutomationElement element, ElementFacts facts)
    {
        try
        {
            element.SetFocus();
        }
        catch (Exception refused)
            when (refused is InvalidOperationException or ElementNotAvailableException)
        {
            return Precondition.Absent(FocusPreconditionName, $"{facts} would not take the focus: {refused.Message}");
        }

        try
        {
            var focused = AutomationElement.FocusedElement;
            if (focused is not null && Automation.Compare(focused, element))
                return Precondition.Met(FocusPreconditionName);

            var holder = ElementFacts.Of(focused);
            return Precondition.Absent(
                FocusPreconditionName,
                $"the focus is on {(holder is null ? "nothing" : holder.ToString())} and not on {facts}");
        }
        catch (ElementNotAvailableException gone)
        {
            return Precondition.Absent(FocusPreconditionName, $"the focused element went away: {gone.Message}");
        }
    }

    /// <summary>
    /// Put the caret after what is there. A fresh control has it at the start, so without this a
    /// scenario that appends inserts at the front instead — which is a different sentence.
    /// </summary>
    private static void MoveToTheEnd() => Press([Key(Win32.VkEnd, 0), Key(Win32.VkEnd, Win32.KeyUp)]);

    /// <summary>
    /// Clear by pressing Backspace once per character, and not by selecting first.
    /// <para>
    /// Measured: Ctrl+A and Ctrl+Shift+End sent through SendInput both left the text untouched on
    /// a plain edit control, so what they had actually done was move the caret and select nothing
    /// — and typing then appended where it was supposed to replace. A modifier is a thing another
    /// process has to agree is held; a Backspace is not. This route uses no modifier at all.
    /// </para>
    /// </summary>
    private static void Erase(int characters)
    {
        if (characters <= 0)
            return;

        var inputs = new List<Win32.Input>(characters * 2);
        for (var each = 0; each < characters; each++)
        {
            inputs.Add(Key(Win32.VkBack, 0));
            inputs.Add(Key(Win32.VkBack, Win32.KeyUp));
        }

        Press([.. inputs]);
    }

    private static void Press(Win32.Input[] inputs) =>
        Win32.SendInput((uint)inputs.Length, inputs, System.Runtime.InteropServices.Marshal.SizeOf<Win32.Input>());

    /// <summary>
    /// One virtual key, carrying the scan code the layout in force gives it.
    /// <para>
    /// Measured, and the reason the first draft of this typed into the wrong end of the field:
    /// a virtual key sent with a scan code of zero did nothing at all — End did not move the
    /// caret and Backspace erased nothing, so text meant to replace was inserted in front of what
    /// was there. Windows wants the pair, and the Unicode path above is the one that does not.
    /// </para>
    /// </summary>
    private static Win32.Input Key(ushort virtualKey, uint flags) => new()
    {
        Type = Win32.InputKeyboard,
        Payload = new Win32.InputPayload
        {
            Key = new Win32.KeyInput
            {
                VirtualKey = virtualKey,
                Scan = (ushort)Win32.MapVirtualKeyW(virtualKey, Win32.VirtualKeyToScan),
                Flags = flags,
            },
        },
    };

    private static void Send(string text)
    {
        // One input pair per UTF-16 code unit, as Unicode rather than as a virtual key: a scan
        // code would go through the keyboard layout, and then what arrives depends on which one
        // the desk happens to have loaded.
        var inputs = new List<Win32.Input>(text.Length * 2);
        foreach (var character in text)
        {
            inputs.Add(Typed(character, 0));
            inputs.Add(Typed(character, Win32.KeyUp));
        }

        if (inputs.Count == 0)
            return;

        Press([.. inputs]);
    }

    private static Win32.Input Typed(char character, uint flags) => new()
    {
        Type = Win32.InputKeyboard,
        Payload = new Win32.InputPayload
        {
            Key = new Win32.KeyInput { Scan = character, Flags = Win32.KeyUnicode | flags },
        },
    };
}

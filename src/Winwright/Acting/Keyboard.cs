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

    /// <summary>
    /// How many times the send had to be repeated because WW249's substitution landed on it.
    /// <para>
    /// Reported and never swallowed, which is the whole condition on the repair below existing. A
    /// resend that nobody could see would make this engine say a control took input cleanly when it
    /// did not, and the one thing <c>Type</c> is for is being the observable that separates those.
    /// Non-zero here is the engine's own fault rate, measurable by anyone running a suite.
    /// </para>
    /// <para>
    /// Set beside the constructor rather than through it: the readings above are what an act is and
    /// arrive together, and this is a count of what it took to get them.
    /// </para>
    /// </summary>
    public int Resends { get; internal init; }

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
        if (Arrived && Resends == 0)
            return $"{Act}: the control reads \"{ReadBack}\".";

        if (Arrived)
        {
            var again = Resends == 1 ? "resend" : "resends";
            return $"{Act}: the control reads \"{ReadBack}\", after {Resends} {again} — the send"
                + " substituted a code unit and it was sent again, WW249.";
        }

        var because = ReadOnly ? ", and the control says it is read-only" : "";
        return $"{Act}: the control reads \"{ReadBack}\" and not \"{Expected()}\"{because}.";
    }

    /// <summary>
    /// The result a verdict counts. A desk that refused the foreground is a <em>hole</em> and never
    /// a failure.
    /// <para>
    /// WW133. Input synthesised into somebody else's window is not a weaker version of this act, it
    /// is a different act against a window nobody asked about — so nothing is sent, and what a case
    /// then reports has to be that the check did not run rather than that the application is wrong.
    /// This block's criterion says it outright: nothing about the desk is reported as a defect in
    /// the code.
    /// </para>
    /// </summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult AsAssertion(string named)
    {
        // The focus is the second condition and the same kind of fact: a control that would not
        // take the caret is a desk this run could not arrange, not an application that is wrong.
        if (!Foreground.Satisfied)
            return AssertionResult.Unchecked(named, Foreground);

        if (!Focus.Satisfied)
            return AssertionResult.Unchecked(named, Focus);

        return Arrived
            ? AssertionResult.Pass(named, ToString())
            : AssertionResult.Fail(named, ToString());
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
        Detail = TraceDetail(),
    };

    /// <summary>
    /// The sentence a trace carries beside the verdict, or null where the verdict is the whole of it.
    /// <para>
    /// A repaired send says so even though it arrived, which is the difference between a rate that
    /// can be read off the runs and one that has to be measured on purpose. Green with a sentence is
    /// how a fault this engine compensates for stays countable.
    /// </para>
    /// </summary>
    private string? TraceDetail()
    {
        if (!Foreground.Satisfied)
            return Foreground.Absence;

        if (!Focus.Satisfied)
            return Focus.Absence;

        return Arrived && Resends == 0 ? null : ToString();
    }
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

        var expected = act.ReplacingWhatIsThere ? act.Text : (before ?? "") + act.Text;
        var readBack = Settled(subject, admitted, expected, act.Text);

        // WW249. Send it again where the reading carries this engine's own substitution, and never
        // where it carries anything else. WW329 made this the backstop rather than the repair: the
        // pause before the first look takes the fault away, and a resend is what is left for a
        // machine where it does not.
        //
        // The fault is measured and narrow: the send puts the last code unit of the string where an
        // earlier one belongs, leaving the length intact. WW310 counted 130 of 130 failures with that
        // shape, unchanged across every spacing it swept. Everything the interaction loop exists to
        // catch reads differently — a control that takes no keyboard input at all reads back what was
        // there before, and one that drops keys reads short. Neither can reach the test below, so
        // neither is repaired into a pass.
        //
        // This is what the alternatives cost. A blanket retry would turn a window accepting one key
        // in four green, which is the exact defect this loop was built for and the reason a pattern
        // act is not enough. A spacing between code units was measured instead and refused on its
        // price: 128ms a code unit is paid by every keystroke this engine ever sends.
        //
        // WW310's band was the second reason and WW312 withdrew it. Swept again over three send
        // shapes and six spacings — 2700 rounds, the engine's own shape among them — the fault
        // appeared twice and both times at no spacing at all, where the band would have put about
        // 35 substitutions in the 48-to-64ms cells alone. What that leaves is a suppression with no
        // shape rather than one with a hole in it, which is WW337's question and not this one's.
        var resends = 0;
        while (resends < Resends && TookTheLastSent(readBack, expected, act.Text))
        {
            // Erased by what the control says it holds, not by what was expected: the two are the
            // same length while the rule holds, and this is the one place that must not assume it.
            MoveToTheEnd();
            Erase(readBack!.Length);

            // The whole expected string and not just the act's text, because the erase above took
            // what was already there with it — an append whose repair sent only its own half would
            // read back short and be reported as the failure it is.
            Send(expected);
            resends++;
            readBack = Settled(subject, admitted, expected, act.Text);
        }

        return new TypedResult(act, facts, foreground, focus, before, readBack, readOnly)
        {
            Resends = resends,
        };
    }

    /// <summary>
    /// How many times one act's send may be repeated before what it reads back is the answer.
    /// <para>
    /// The fault runs near 2% a send and reached 3.5% on the worst evening measured, so three
    /// repeats put a surviving substitution somewhere past one act in a million — below the rate of
    /// every other thing that makes a guest run lie. It is bounded rather than deadlined because a
    /// count is what a reader can price: nothing is paid on the 97% of sends that arrive, and a send
    /// that is going to fail costs at most three more of itself.
    /// </para>
    /// </summary>
    private const int Resends = 3;


    /// <summary>
    /// Wait for the control to reach a reading that will not change, and report it.
    /// <para>
    /// Waited on rather than read the instant after sending: keys go into a queue another thread
    /// drains, so an immediate read is a race whichever way it lands. A deadline on a condition, and
    /// anything that never settles costs the whole of it and says what it found — which is the
    /// honest price of not reporting a value from before the act.
    /// </para>
    /// <para>
    /// Two readings end the wait and not one. A substitution is not a reading on its way to being
    /// right: the text arrived and one code unit of it is wrong, and it will still be wrong at the
    /// deadline. Waiting it out bought nothing when the act was reported straight away, and would
    /// cost four deadlines on the acts <c>Run</c> now repeats. What is genuinely still arriving
    /// reads short, and a short reading answers neither test — so the early exit cannot take one.
    /// </para>
    /// </summary>
    /// <param name="subject">The control to read.</param>
    /// <param name="admitted">The admission taken before the send, whose element this reads.</param>
    /// <param name="expected">What it should say.</param>
    /// <param name="sent">The text the keyboard was given, which decides what a substitution is.</param>
    private static string? Settled(Subject subject, Admitted admitted, string expected, string sent)
    {
        // WW329 put a fifty-millisecond pause here and the fault went away. WW342 then found which
        // half of a read does it — 4800 dispatched messages provoked nothing and the automation read
        // provoked 8 of 400 — and WW355 asked the question that left: whether a cheaper read
        // provokes at all.
        //
        // It does not, and the margin is not close. Two runs of 400 rounds an arm on the guest, with
        // the engine's own whole pass beside them: the window's title through WM_GETTEXT 0 and 0, one
        // cached ask 0 and 0, one property of an already-resolved element 0 and 0, that element's
        // ValuePattern — which is this very read — 0 and 0, against the whole pass at 5 of 400 and 4
        // of 400. So the provider is not disturbed by being asked; it is disturbed by being asked a
        // great deal, and the pause was standing in for a read that never needed one.
        //
        // The walk is most of what was being asked, and it is the whole of what had to go. First
        // measured wrong here: this resolved once and then polled one pattern, which is the arm that
        // read zero — and the engine's own act read 10 of 400, the rate WW329 took away. The
        // difference is where the walk is. `provoke` resolves before all its rounds; this resolved
        // after the send, so one walk landed in the window's thread mid-drain and one was enough.
        //
        // So nothing is resolved here at all. The admission above already found the element, before
        // the keys went anywhere near the queue, and this asks that one for its value.
        //
        // The pause stays, and the measurement is why. Reading cheaply took the rate from 2.58% with
        // no pause to 1 of 1200 without one — thirty-one times better and not zero, where the pause
        // reads zero. So the arm's answer did not fully transfer: something the real act does and the
        // arm did not still reaches the queue, and a rate this project already drove to nothing is
        // not one to give back for 50ms a send. What the cheap read buys is that the pause now
        // guards a fault thirty times rarer, and that a round is very much shorter: 1200 rounds with
        // both read 0 faulted at 91-95ms a quarter, where WW329 priced the same act at 146ms with no
        // pause and 153ms with one. Most of what a settle cost was the walk it took every poll.
        //
        // WW368 found what did not transfer, and it is not on this line. A ladder walked the arm to
        // the act one difference at a time on the guest, 1200 rounds a rung: the arm's own shape 0,
        // plus a SetFocus every round 1, plus the engine's split send 0, plus the engine's own
        // stop-on-match read 1 — which is the act's rate, reproduced outside the engine for the
        // first time. The three rungs that take the focus read 2 of 3600 where the one that does not
        // read 0 of 1200, against WW355's 0 of 3200.
        //
        // So the provoking call is on the other side of the send. `TakeFocus` above is a provider
        // round-trip issued on the line before the keys go in, and this pause is spent after them —
        // it is guarding the read, and the read was acquitted twice. What that opens is a pause that
        // guards the focus instead, which would be a repair rather than a floor found by sweeping.
        Thread.Sleep(Keys.FirstLookMs);

        var settled = Attempt.Until(
            () =>
            {
                var reading = Cheaply(subject, admitted);
                return reading == expected || TookTheLastSent(reading, expected, sent) ? reading : null;
            },
            subject.ActMs,
            subject.PollMs);

        return settled.Found ? settled.Value : Cheaply(subject, admitted);
    }

    /// <summary>
    /// What the control says it holds, asked the way WW355 measured as harmless: one pattern on the
    /// element this act already resolved.
    /// <para>
    /// Falls back to the whole pass wherever the cheap read cannot answer — no element, no value
    /// pattern, or an element that went while the queue drained. That last is the one worth naming:
    /// re-resolving is exactly what this avoids, so it is done only where the alternative is not
    /// reading at all, and a control that closed under a settle is a fact the full pass reports
    /// properly.
    /// </para>
    /// </summary>
    /// <param name="subject">The control, for the fallback that re-resolves.</param>
    /// <param name="admitted">The admission taken before the send, which already holds the element.</param>
    private static string? Cheaply(Subject subject, Admitted admitted)
    {
        if (!admitted.Facts.Supports("Value"))
            return Reading(subject);

        try
        {
            return admitted.Do(element => element.GetCurrentPattern(ValuePattern.Pattern) is ValuePattern pattern
                ? pattern.Current.Value
                : Reading(subject));
        }
        catch (ElementNotAvailableException)
        {
            return Reading(subject);
        }
        catch (InvalidOperationException)
        {
            return Reading(subject);
        }
    }

    /// <summary>
    /// Whether a reading is WW249's substitution and not some other way of being wrong.
    /// <para>
    /// Length for length, with every character that differs being the last code unit sent. Not
    /// <em>exactly one</em> character, though that is how most of them arrive: §WW249 records
    /// <c>WW246-5</c> read back as <c>W5245-5</c>, which is two positions taking the same intruder,
    /// and a test written to the common case would have refused to repair the example the task is
    /// filed under.
    /// </para>
    /// <para>
    /// The last code unit sent is the last of <paramref name="sent"/> and not of
    /// <paramref name="expected"/> — the same character while an act replaces, and different from an
    /// append's point of view, where what was already there was never sent at all.
    /// </para>
    /// </summary>
    /// <param name="readBack">What the control says it holds.</param>
    /// <param name="expected">What it should say.</param>
    /// <param name="sent">The text the keyboard was given, whose last code unit is the intruder.</param>
    private static bool TookTheLastSent(string? readBack, string expected, string sent)
    {
        if (sent.Length == 0 || readBack is null || readBack.Length != expected.Length)
            return false;

        var last = sent[^1];
        var substituted = false;
        for (var at = 0; at < expected.Length; at++)
        {
            if (readBack[at] == expected[at])
                continue;

            if (readBack[at] != last)
                return false;

            substituted = true;
        }

        return substituted;
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

    /// <summary>
    /// Put the text into the queue, one call carrying every code unit.
    /// <para>
    /// This shape was measured against its alternatives and kept. WW302 read 14 substitutions in 400
    /// batched against 0 sent one <c>Type</c> at a time, which looked like the array — and the next
    /// measurement refused that reading: one <c>SendInput</c> per code unit left the rate where it
    /// was, 11 in 400. Same call count, same fault. What the quiet arm also did was a whole
    /// <c>Type</c> between characters, so what separated them was time and not batching.
    /// </para>
    /// <para>
    /// Time was then swept and refused on its price: it works at 128ms a code unit, on every
    /// keystroke this engine ever sends. WW310's band was the second reason and WW312 could not
    /// reproduce it — 2700 rounds over three send shapes and six spacings faulted twice, both at no
    /// spacing at all. So what refuses the delay today is the price alone, and WW337 is whether that
    /// price is really 128ms. Until it answers, the send stays as it was and <c>Run</c> above repairs
    /// the fault by its signature instead.
    /// </para>
    /// </summary>
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

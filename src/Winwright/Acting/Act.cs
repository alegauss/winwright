using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Verdicts;

namespace Winwright.Acting;

/// <summary>
/// The acts that ask a control directly.
/// <para>
/// pportal's harness states the rule and the reason in its own header: a synthesised mouse click
/// lands on whatever is drawn at a point, so it needs the window in the foreground, and Windows
/// refuses the foreground to a process that does not already own it — which means a run started
/// from an editor or an agent drives somebody else's window. A pattern asks the control: no
/// pointer, no foreground, nothing on top to be confused with. That is what lets these run
/// unattended, which is the whole point of them.
/// </para>
/// <para>
/// Every one of them re-resolves its subject, requires actionability before touching anything, and
/// reads the control's values either side of the act, so what a report says moved is two values
/// and not one live view asked twice.
/// </para>
/// </summary>
public static class Act
{
    /// <summary>Press it, the way its own accessibility peer would.</summary>
    public static ActResult Invoke(Subject subject) => Through(
        subject, "invoke", "Invoke",
        element => ((InvokePattern)element.GetCurrentPattern(InvokePattern.Pattern)).Invoke());

    /// <summary>Flip it — on to off, off to on, and whatever a third state does next.</summary>
    public static ActResult Toggle(Subject subject) => Through(
        subject, "toggle", "Toggle",
        element => ((TogglePattern)element.GetCurrentPattern(TogglePattern.Pattern)).Toggle());

    /// <summary>Put text into it through the control's own value, rather than through the keyboard.</summary>
    public static ActResult SetValue(Subject subject, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Through(
            subject, "set value", "Value",
            element => ((ValuePattern)element.GetCurrentPattern(ValuePattern.Pattern)).SetValue(value));
    }

    /// <summary>Move it to a number, refusing one the control says it does not accept.</summary>
    public static ActResult SetRange(Subject subject, double value) => Through(
        subject, "set range", "RangeValue",
        element => ((RangeValuePattern)element.GetCurrentPattern(RangeValuePattern.Pattern)).SetValue(value));

    /// <summary>Select it, replacing whatever the container had selected.</summary>
    public static ActResult Select(Subject subject) => Through(
        subject, "select", "SelectionItem",
        element => ((SelectionItemPattern)element.GetCurrentPattern(SelectionItemPattern.Pattern)).Select());

    /// <summary>Open it, which is what puts its contents into the tree at all.</summary>
    public static ActResult Expand(Subject subject) => Through(
        subject, "expand", "ExpandCollapse",
        element => ((ExpandCollapsePattern)element.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Expand());

    /// <summary>Shut it again.</summary>
    public static ActResult Collapse(Subject subject) => Through(
        subject, "collapse", "ExpandCollapse",
        element => ((ExpandCollapsePattern)element.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Collapse());

    /// <summary>
    /// The shape every act has: resolve, judge, act, read back. The judgement comes before the
    /// act and not after it, so nothing here ever touches a control that could not take it.
    /// </summary>
    /// <exception cref="NotActionableException">Where the element cannot take the act.</exception>
    private static ActResult Through(Subject subject, string verb, string pattern, Action<AutomationElement> doing)
    {
        ArgumentNullException.ThrowIfNull(subject);

        // The element is reached only through the admission, which judged it on the way out.
        var admitted = Admitted.To(subject, pattern);
        var before = admitted.Reading;

        admitted.Do(doing);

        // Read again through the subject rather than through the element just used: the act may
        // have replaced what it addressed, and a handle held across it is a handle to what was.
        var after = subject.Read();
        return new ActResult(
            verb,
            subject.Locator,
            pattern,
            admitted.Facts,
            before.Values,
            after.Values,
            before.Resolution.WaitedMs,
            before.Resolution.Polls);
    }
}


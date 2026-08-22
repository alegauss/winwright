using System.Windows.Automation;

using Winwright.Windowing;

namespace Winwright.Locating;

/// <summary>
/// The element an act is allowed to touch, together with the judgement that allowed it.
/// <para>
/// The actionability check landed and the chokepoint did not. A verb about to press something
/// could read the facts, judge them and require the answer — or it could resolve, take the element
/// and press it, and nothing in the types noticed. The block's criterion "an act never runs
/// against an element that cannot take it" was met by whoever remembered, which is the shape of
/// rule this project has closed twice already: a process cannot be launched outside the register
/// because the type a caller needs is not constructible from outside it, and an attached target
/// cannot be asked what arguments it was started with because the property is not on it. Neither
/// relies on anybody reading a note.
/// </para>
/// <para>
/// This is the same move. <see cref="Resolution.Element"/> is no longer public, so the only way to
/// reach the live element is <see cref="To(Subject, string?)"/>, which resolves, judges and refuses
/// before it hands anything over. A verb holding one of these knows the four properties held at
/// the moment it was made; a verb that wants to skip the check has nothing to call.
/// </para>
/// <para>
/// It does not stop a caller keeping hold of what it was handed inside <c>Do</c>, and it does not
/// need to: what it makes impossible is reaching an element to act on without having asked first.
/// Where the judgement then belongs — refusal, hole or failure — stays each verb's decision.
/// </para>
/// </summary>
public sealed class Admitted
{
    private readonly AutomationElement element;
    private nint window = -1;

    private Admitted(Subject subject, Reading reading, ActionabilityCheck judged, AutomationElement element)
    {
        Subject = subject;
        Reading = reading;
        Judged = judged;
        this.element = element;
    }

    /// <summary>What the act is about.</summary>
    public Subject Subject { get; }

    /// <summary>The look that admitted it, taken once, before the act.</summary>
    public Reading Reading { get; }

    /// <summary>The judgement that let it through, which said yes to all four.</summary>
    public ActionabilityCheck Judged { get; }

    /// <summary>What the element said about itself at the moment it was admitted.</summary>
    public ElementFacts Facts => Reading.Facts!;

    /// <summary>What its patterns read at that same moment.</summary>
    public PatternValues Values => Reading.Values;

    /// <summary>
    /// The top-level window the element belongs to, or 0 where it has none of its own. Read here
    /// rather than in each verb: four of them wanted it, and a door invented per verb is four
    /// doors that differ.
    /// </summary>
    public nint Window
    {
        get
        {
            if (window == -1)
            {
                var handle = (nint)element.Current.NativeWindowHandle;
                window = handle == 0 ? 0 : Win32.GetAncestor(handle, Win32.GaRoot);
            }

            return window;
        }
    }

    /// <summary>
    /// Resolve the subject, judge what was found, and admit it — or refuse.
    /// </summary>
    /// <param name="subject">What the act is about.</param>
    /// <param name="pattern">The pattern the act goes through, or null where it needs none.</param>
    /// <exception cref="NotActionableException">Where the element cannot take the act.</exception>
    public static Admitted To(Subject subject, string? pattern = null)
    {
        ArgumentNullException.ThrowIfNull(subject);
        return Of(subject, subject.Read(), pattern);
    }

    /// <summary>
    /// The same, against a look already taken. For a verb that needs the reading before it knows
    /// which pattern it wants — the judgement is still made here and still made first.
    /// </summary>
    /// <param name="subject">What the act is about.</param>
    /// <param name="reading">The look to judge, taken from that subject.</param>
    /// <param name="pattern">The pattern the act goes through, or null where it needs none.</param>
    /// <exception cref="NotActionableException">Where the element cannot take the act.</exception>
    public static Admitted Of(Subject subject, Reading reading, string? pattern = null)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(reading);

        var judged = ActionabilityCheck.Of(reading.Facts, pattern);
        judged.Require(subject.Locator.Text);

        return new Admitted(subject, reading, judged, reading.Resolution.Element!);
    }

    /// <summary>Do the thing the admission was for.</summary>
    /// <param name="doing">The act, against the element it was judged against.</param>
    public void Do(Action<AutomationElement> doing)
    {
        ArgumentNullException.ThrowIfNull(doing);
        doing(element);
    }

    /// <summary>The same, where the act answers something.</summary>
    /// <param name="doing">The act, against the element it was judged against.</param>
    /// <typeparam name="T">What it answers.</typeparam>
    public T Do<T>(Func<AutomationElement, T> doing)
    {
        ArgumentNullException.ThrowIfNull(doing);
        return doing(element);
    }

    /// <summary>What was admitted and what it may take.</summary>
    public override string ToString() => $"{Facts} was admitted: {Judged.Because}";
}

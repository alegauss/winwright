using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Verdicts;
using Winwright.Windowing;

namespace Winwright.Acting;

/// <summary>
/// What holds the keyboard focus, and whether it is anything this run is entitled to talk about.
/// </summary>
/// <remarks>
/// WW155. The reading underneath every menu walk and every traversal used to be the focused element
/// of the whole desktop, and nothing narrowed it to the application that was asked about. So what a
/// case asserted on was whatever held the desk, and the menu was only implied.
/// </remarks>
public sealed record FocusReading
{
    internal FocusReading(ElementFacts? element, bool inside, string because)
    {
        Element = element;
        Inside = inside;
        Because = because;
    }

    /// <summary>What this reading is called wherever it is reported.</summary>
    public const string Named = "the focus is inside the application under test";

    /// <summary>What holds the focus, wherever it is. Null where nothing does, or nothing readable.</summary>
    public ElementFacts? Element { get; }

    /// <summary>Whether it belongs to the application this run is driving.</summary>
    public bool Inside { get; }

    /// <summary>Where the focus is instead, or why it could not be read. Empty where it is inside.</summary>
    public string Because { get; }

    /// <summary>
    /// The answer, or nothing. Reading this rather than <see cref="Element"/> is what stops an
    /// element belonging to somebody else's window being compared against a wanted entry.
    /// </summary>
    public ElementFacts? Held => Inside ? Element : null;

    /// <summary>
    /// The condition an assertion is resolved against. A focus that went elsewhere is a
    /// <em>hole</em> and never a failure — the run stopped being able to observe the thing it was
    /// asked about, which is not the same as the application being wrong about it.
    /// </summary>
    /// <param name="named">What the condition is called, where a caller names it.</param>
    public Precondition AsPrecondition(string named = Named) =>
        Inside ? Precondition.Met(named) : Precondition.Absent(named, Because);

    /// <summary>What was read, said either way.</summary>
    public string Sentence() => Inside
        ? $"{Element} holds the focus, and it is this application's."
        : $"{Because}.";
}

/// <summary>
/// Reading the focus against the application it is supposed to be in.
/// <para>
/// Scoped by process and not by window handle, which is a measurement rather than a preference: a
/// menu popup and a combo drop-down are top-level windows of their own, so a reading that insisted
/// on one root handle would reject every legitimate menu entry this project exists to walk. What it
/// does reject is the case that was actually observed twice — an element belonging to another
/// application entirely, compared against a wanted entry and reported as a red about this one.
/// </para>
/// <para>
/// Measured rather than supposed. Shipping WW143, a submenu case went red with
/// <c>Expected: "one.txt"</c> against <c>Actual: "1 Yes"</c>; shipping WW145, a shift-tab case went
/// red with <c>Expected: "alpha"</c> against <c>Actual: "Mostrar Ícones Ocultos …"</c>, which is the
/// notification area's overflow button. Neither string exists anywhere in this repository, and both
/// classes were green on the next run — which is what a misattribution looks like from the inside:
/// a red about the application that nobody can reproduce.
/// </para>
/// </summary>
public static class Focus
{
    /// <summary>Read the focus against the application that owns <paramref name="window"/>.</summary>
    /// <param name="window">Any window of the application under test.</param>
    public static FocusReading In(nint window)
    {
        var driving = ProcessOf(window);
        if (driving == 0)
            return new FocusReading(null, false, "the window under test names no process to read the focus against");

        var focused = Focused();
        if (focused is null)
            return new FocusReading(null, false, "nothing on this desk holds the keyboard focus");

        var facts = ElementFacts.Of(focused);
        var holding = ProcessOfElement(focused);

        if (holding == driving)
            return new FocusReading(facts, true, "");

        // Named rather than merely refused: a run that says the focus left is a run somebody can
        // act on, and the thing that took it is the whole of what they need to know.
        return new FocusReading(
            facts,
            false,
            holding == 0
                ? $"{Say(facts)} holds the focus and names no process, so it is not this application's"
                : $"{Say(facts)} in pid {holding} holds the focus, and this run is driving pid {driving}");
    }

    /// <summary>What holds it in that application, or null where the focus is somewhere else.</summary>
    /// <param name="window">Any window of the application under test.</param>
    public static ElementFacts? Held(nint window) => In(window).Held;

    private static string Say(ElementFacts? facts) => facts?.ToString() ?? "something unreadable";

    private static uint ProcessOf(nint window)
    {
        if (window == 0)
            return 0;

        _ = Win32.GetWindowThreadProcessId(window, out var pid);
        return pid;
    }

    private static uint ProcessOfElement(AutomationElement element)
    {
        try
        {
            return (uint)element.Current.ProcessId;
        }
        catch (Exception gone) when (gone is ElementNotAvailableException or InvalidOperationException)
        {
            return 0;
        }
    }

    private static AutomationElement? Focused()
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
}

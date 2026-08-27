using System.Windows.Automation;

namespace Winwright.Locating;

/// <summary>Why a locator found nothing. Three answers, because they have three remedies.</summary>
public enum MissKind
{
    /// <summary>
    /// Something matching the step is in this window, just not under the step before it. The
    /// chain is wrong rather than the control missing, and no amount of navigating will help.
    /// </summary>
    ElsewhereInTheWindow,

    /// <summary>
    /// The step before it resolved and is closed — a collapsed combo box, an unselected page, a
    /// control scrolled out of view. What is under it is absent by design until that is opened.
    /// </summary>
    NavigationNeeded,

    /// <summary>
    /// Nothing in the window matches it. Whether that means renamed, removed, or sitting behind
    /// something that is not showing is settled by <see cref="LocatorMiss.ClosedDoors"/>: with
    /// nothing closed, it is genuinely not there.
    /// </summary>
    Absent,

    /// <summary>
    /// The whole chain matches now, and did not while it was looked for.
    /// <para>
    /// WW252. A diagnosis runs after a resolve gave up, so at first sight nothing can match all the
    /// way — what can is time. Measured against a pane whose numbers render from a transcript scan:
    /// the read timed out and the value was there a moment later, and naming the step that stopped
    /// the walk indexed one past the last one there was.
    /// </para>
    /// <para>
    /// It is a reading rather than an accident, and one no other sentence here makes: it is the
    /// signature of a wait that was too short, which is a fact about the case and not the window.
    /// </para>
    /// </summary>
    ArrivedLate,
}

/// <summary>One thing in the window that is shut, and how it would be opened.</summary>
/// <param name="What">The element, named the way a locator names one.</param>
/// <param name="How">What would open it — expanded, or selected.</param>
public sealed record ClosedDoor(string What, string How)
{
    /// <summary>The one phrase a sentence lists it by.</summary>
    public override string ToString() => $"{What} ({How})";
}

/// <summary>
/// A miss, diagnosed. A control on a page that is not showing cannot be found by any id, which
/// reads exactly like a control that was renamed or removed — so the miss says which of the two it
/// is. The answer is a route rather than a puzzle: either the thing that has to be opened first,
/// or the statement that nothing here is shut and the control really is gone.
/// </summary>
public sealed record LocatorMiss
{
    internal LocatorMiss(
        Locator locator,
        int reached,
        ElementFacts? deepest,
        MissKind kind,
        string? route,
        int elsewhere,
        IReadOnlyList<ClosedDoor> closedDoors)
    {
        Locator = locator;
        Reached = reached;
        Deepest = deepest;
        Kind = kind;
        Route = route;
        Elsewhere = elsewhere;
        ClosedDoors = closedDoors;
    }

    /// <summary>The locator that missed, whole.</summary>
    public Locator Locator { get; }

    /// <summary>How many of its steps resolved before the miss. Zero where the first one did not.</summary>
    public int Reached { get; }

    /// <summary>The step that did not resolve.</summary>
    /// <summary>
    /// The step the walk stopped at, or the last one where it stopped at none.
    /// <para>
    /// WW252. <see cref="MissKind.ArrivedLate"/> is the whole chain matching, so there is no step it
    /// stopped at and the index would be one past the end — the second place the same arithmetic was
    /// written, and the one that would have thrown again after the first was fixed.
    /// </para>
    /// </summary>
    public LocatorStep Stopped =>
        Locator.Steps[Math.Min(Reached, Locator.Steps.Count - 1)];

    /// <summary>What the search was under when it stopped, or null where that was the window itself.</summary>
    public ElementFacts? Deepest { get; }

    /// <summary>Which of the three this is.</summary>
    public MissKind Kind { get; }

    /// <summary>What would have to be navigated first, where that is the answer.</summary>
    public string? Route { get; }

    /// <summary>How many elements elsewhere in the window match the step that stopped.</summary>
    public int Elsewhere { get; }

    /// <summary>
    /// What is shut in this window right now. Offered as leads rather than as an answer: each is
    /// a true statement about the window, and none of them claims to hold what was looked for.
    /// </summary>
    public IReadOnlyList<ClosedDoor> ClosedDoors { get; }

    /// <summary>The whole reading, in the sentence a person acts on.</summary>
    public string Sentence()
    {
        var under = Deepest is null ? "the window" : Deepest.ToString();
        return Kind switch
        {
            MissKind.ElsewhereInTheWindow =>
                $"{Stopped} is not under {under}, but {Elsewhere} like it are elsewhere in the window: "
                + "the chain is wrong rather than the control missing.",
            MissKind.NavigationNeeded =>
                $"{Stopped} is not in the tree under {under}, and it will not be until {Route}.",
            MissKind.ArrivedLate =>
                $"{Stopped} matches now and did not while it was looked for: the window was still "
                + "drawing, so this is a wait that was too short rather than anything about the control.",
            _ when ClosedDoors.Count > 0 =>
                $"nothing in the window matches {Stopped}: it was renamed, removed, or it is behind "
                + $"something that is not showing — {string.Join(", ", ClosedDoors)} "
                + (ClosedDoors.Count == 1 ? "is shut." : "are shut."),
            _ => $"nothing in the window matches {Stopped}, and nothing in it is shut: "
                + "it was renamed, removed, or never there.",
        };
    }

    /// <summary>
    /// Read the route off a live element: the thing that has to happen before its contents can be
    /// in the tree at all. Null where nothing about it explains the miss.
    /// </summary>
    internal static string? RouteFrom(AutomationElement? element, ElementFacts? facts)
    {
        if (element is null || facts is null)
            return null;

        var how = Shut(element, facts);
        if (how is not null)
            return $"{facts} is {how}";

        return facts.IsOffscreen ? $"{facts} is scrolled into view" : null;
    }

    /// <summary>How this element is shut — expanded, or selected — or null where it is not shut.</summary>
    internal static string? Shut(AutomationElement element, ElementFacts facts)
    {
        try
        {
            if (facts.Supports("ExpandCollapse")
                && element.GetCurrentPattern(ExpandCollapsePattern.Pattern) is ExpandCollapsePattern expandable
                && expandable.Current.ExpandCollapseState == ExpandCollapseState.Collapsed)
            {
                return "expanded";
            }

            // Only a page header, and not every selectable thing: an unselected list item is not a
            // door, and listing every one of them is how a lead list stops being read.
            if (facts.ControlType == "TabItem"
                && element.GetCurrentPattern(SelectionItemPattern.Pattern) is SelectionItemPattern selectable
                && !selectable.Current.IsSelected)
            {
                return "selected";
            }
        }
        catch (Exception unreadable)
            when (unreadable is ElementNotAvailableException or InvalidOperationException)
        {
            return null;
        }

        return null;
    }
}

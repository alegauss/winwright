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

    /// <summary>
    /// What the walk stopped under was holding, named the way a locator names one — at most a
    /// handful, with <see cref="Holding" /> saying how many there were in all. WW456.
    /// <para>
    /// Every sentence below says what was <em>not</em> found and nothing said what was. That is
    /// tolerable against a window a reader can go and look at, and it is not tolerable against the
    /// desktop, which is the root a resident fixture's steps are given: a tray application draws no
    /// window, so its case resolves against everything on the desk at once.
    /// </para>
    /// <para>
    /// Measured by a session rather than argued. An adopter's step reported <c>nothing answered to
    /// it in 22 polls over 6177ms</c>, one line after the same run reported the menu it had read
    /// back, and three tasks each spent a guest run ruling out one thing that sentence could have
    /// said: whether a menu was standing at all, whether more than one was, whether it held entries,
    /// and what type those entries were. The children of the thing the walk stopped under answer all
    /// four, and are one cheap read on a path that has already given up.
    /// </para>
    /// <para>
    /// Children and not descendants, which is what keeps it cheap and what makes it readable: under
    /// the desktop they are the top-level windows, and under a menu they are its entries. A
    /// descendants walk of the desktop is the most expensive question this engine asks, and WW328
    /// measured it failing outright on a guest.
    /// </para>
    /// </summary>
    public IReadOnlyList<string> Held { get; init; } = [];

    /// <summary>
    /// How many it was holding in all, where <see cref="Held" /> lists fewer — and null where the
    /// walk was cut short, which is not the same as nothing. WW456.
    /// <para>
    /// Nullable rather than zero, for the reason this whole engine has a third verdict: a tree that
    /// went while it was being read and a parent that really is empty are different facts, and
    /// "holding nothing" is the more useful of the two to say out loud. A menu that opened empty is
    /// exactly the defect an adopter's case was written to catch.
    /// </para>
    /// </summary>
    public int? Holding { get; init; }

    /// <summary>The whole reading, in the sentence a person acts on.</summary>
    public string Sentence() => Diagnosis() + Holdings();

    /// <summary>
    /// What the walk stopped under held, as the clause that follows the diagnosis. WW456.
    /// <para>
    /// Appended rather than folded into each arm, because it is a different kind of statement: the
    /// arms say why the locator missed and this says what was there instead. Empty where nothing was
    /// read, so a reading that could not be taken adds no words rather than claiming an empty tree —
    /// which is the same distinction every verdict in this engine makes about not having looked.
    /// </para>
    /// </summary>
    private string Holdings()
    {
        // Nothing read at all, which is a reading that was not taken rather than a parent that was
        // empty. No words, because the alternative is this sentence claiming an empty tree.
        if (Holding is null && Held.Count == 0)
            return "";

        var under = Deepest is null ? "What it looked under" : $"{Deepest}";
        var named = Held.Count == 0 ? "" : $": {string.Join(", ", Held)}";

        // Cut short, so what is known is a floor. Said as one, because a count that is really a
        // minimum printed as a total is the kind of number somebody reasons from.
        if (Holding is null)
            return $" {under} was holding at least {Held.Count}{named} — the walk was cut short.";

        if (Holding == 0)
            return $" {under} was holding nothing.";

        var rest = Holding > Held.Count ? $", and {Holding - Held.Count} more" : "";
        return Held.Count == 0
            ? $" {under} was holding {Holding}, none of them readable."
            : $" {under} was holding {Holding}{named}{rest}.";
    }

    private string Diagnosis()
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

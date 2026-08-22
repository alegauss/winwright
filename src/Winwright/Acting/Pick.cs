using System.Collections.ObjectModel;
using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Tracing;
using Winwright.Verdicts;
using Winwright.Windowing;

namespace Winwright.Acting;

/// <summary>Which way a value was reached.</summary>
public enum PickRoute
{
    /// <summary>Through the selection pattern, which is one change and no keys.</summary>
    Pattern,

    /// <summary>Through the keyboard, anchored at an end and walked.</summary>
    Keyboard,
}

/// <summary>What picking a value did, and how many switches it took to get there.</summary>
public sealed record PickResult
{
    internal PickResult(
        string wanted,
        ElementFacts container,
        PickRoute route,
        IReadOnlyList<string> passed,
        string? selected,
        string? patternRefused,
        Precondition foreground)
    {
        Wanted = wanted;
        Container = container;
        Route = route;
        Passed = passed;
        Selected = selected;
        PatternRefused = patternRefused;
        Foreground = foreground;
    }

    /// <summary>The value that was asked for.</summary>
    public string Wanted { get; }

    /// <summary>The picker it was asked of.</summary>
    public ElementFacts Container { get; }

    /// <summary>Which route was taken.</summary>
    public PickRoute Route { get; }

    /// <summary>
    /// Every value the picker stopped on, in order. This is the route itself: each stop is a
    /// selection change of its own, and a line observed at one of them belongs to that value.
    /// </summary>
    public IReadOnlyList<string> Passed { get; }

    /// <summary>What is selected now.</summary>
    public string? Selected { get; }

    /// <summary>Why the pattern route was not used, where it was tried and refused.</summary>
    public string? PatternRefused { get; }

    /// <summary>Whether the window owned the desktop, on the route that needed it.</summary>
    public Precondition Foreground { get; }

    /// <summary>
    /// How many times the selection changed. An observation about one switch is void when this is
    /// more than one, which is the whole reason it is reported rather than assumed.
    /// </summary>
    public int SelectionChanges => Passed.Count;

    /// <summary>Whether the picker ended up on the value that was asked for.</summary>
    public bool Landed => string.Equals(Selected, Wanted, StringComparison.Ordinal);

    /// <summary>What happened, with the route and the count both in it.</summary>
    public override string ToString()
    {
        if (!Foreground.Satisfied)
            return $"{Container} was not walked: {Foreground.Absence}.";

        var route = Route == PickRoute.Pattern ? "the selection pattern" : "the keyboard";
        var refused = PatternRefused is null ? "" : $" (the pattern refused: {PatternRefused})";
        var by = SelectionChanges == 1 ? "1 change" : $"{SelectionChanges} changes";
        return $"{Container} reached \"{Selected}\" by {route} in {by}: "
            + $"{string.Join(" -> ", Passed)}{refused}.";
    }

    /// <summary>The step a trace records, carrying the hop count where it is more than one.</summary>
    public TraceStep AsTraceStep() => new()
    {
        Verb = "pick",
        Locator = Container.ToString(),
        Resolved = Selected,
        Pattern = Route == PickRoute.Pattern ? "SelectionItem" : "synthesized keyboard",
        ReadBack = Selected,
        Polls = SelectionChanges,
        Verdict = !Foreground.Satisfied ? StepVerdict.Unchecked : Landed ? StepVerdict.Ok : StepVerdict.Failed,
        Detail = Landed && SelectionChanges == 1 ? null : ToString(),
    };
}

/// <summary>
/// Reaching a value in a picker, and saying how many switches it took.
/// <para>
/// A claim about one switch is void when the walk made several, because each intermediate stop is
/// a switch of its own and the line observed belongs to some other value. claude-tray's picker
/// normalised to the top and walked down, which silently voided the timing assertion whenever the
/// pattern route threw — and that fallback exists precisely because the pattern route sometimes
/// does. Anchoring at whichever end is nearer costs at most one change for a two-item picker, so
/// the assertion holds on both routes, and the count is reported so a reader can tell which was
/// taken.
/// </para>
/// </summary>
public static class Pick
{
    /// <summary>
    /// Select <paramref name="wanted"/> in a picker, by pattern where that works and by keyboard
    /// where it does not.
    /// </summary>
    /// <param name="container">The picker.</param>
    /// <param name="wanted">The value to reach, by name.</param>
    /// <param name="byKeyboard">Take the keyboard route deliberately, which is its own claim.</param>
    /// <exception cref="NotActionableException">
    /// Where the picker cannot take the act, or where it holds no value by that name — and then
    /// the refusal lists what it does hold, which is what a reader needs next.
    /// </exception>
    public static PickResult Value(Subject container, string wanted, bool byKeyboard = false)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentException.ThrowIfNullOrWhiteSpace(wanted);

        // Picking is several acts against the one picker — read its items, press one, read what it
        // ended up on — so the admission is taken once and the picker is used for the length of it.
        var admitted = Admitted.To(container, "Selection");
        var facts = admitted.Facts;
        var element = admitted.Do(picker => picker);
        var items = Items(element);
        var index = items.FindIndex(item => string.Equals(item.Name, wanted, StringComparison.Ordinal));
        if (index < 0)
        {
            throw new NotActionableException(
                container.Locator.Text,
                Actionable.NotInTree,
                $"{facts} holds no \"{wanted}\"; it holds "
                + (items.Count == 0 ? "nothing" : string.Join(", ", items.Select(item => $"\"{item.Name}\""))));
        }

        var met = Precondition.Met(Windowing.Foreground.PreconditionName);
        string? refused = null;
        if (!byKeyboard && TryThePattern(items[index].Element, out refused))
        {
            return new PickResult(
                wanted, facts, PickRoute.Pattern, [wanted], Selected(element), null, met);
        }

        // The keyboard route needs the desktop; the pattern one never did.
        var foreground = Windowing.Foreground.Check(admitted.Window).AsPrecondition();
        if (!foreground.Satisfied)
            return new PickResult(wanted, facts, PickRoute.Keyboard, [], Selected(element), refused, foreground);

        return Walked(container, element, facts, items, index, wanted, refused, foreground);
    }

    /// <summary>Every value a picker holds, in the order it holds them.</summary>
    public static IReadOnlyList<string> Values(Subject container)
    {
        ArgumentNullException.ThrowIfNull(container);

        var element = container.ReadOnce().Resolution.Element;
        return element is null ? [] : Items(element).Select(item => item.Name).ToList();
    }

    private static PickResult Walked(
        Subject container,
        AutomationElement element,
        ElementFacts facts,
        List<(string Name, AutomationElement Element)> items,
        int index,
        string wanted,
        string? refused,
        Precondition foreground)
    {
        // Anchor at whichever end is nearer. Normalising to one end always is what voided the
        // observation on a long picker; from the nearer end a two-item picker costs one change.
        var fromTheTop = index <= (items.Count - 1) / 2;
        var hops = fromTheTop ? index : items.Count - 1 - index;
        var walk = fromTheTop ? TraversalKey.Down : TraversalKey.Up;

        element.SetFocus();
        Keys.SendHomeOrEnd(fromTheTop);

        var passed = new List<string>();
        Record(container, element, passed);

        for (var hop = 0; hop < hops; hop++)
        {
            Keys.Send(walk);
            Record(container, element, passed);
        }

        return new PickResult(
            wanted,
            facts,
            PickRoute.Keyboard,
            new ReadOnlyCollection<string>(passed),
            Selected(element),
            refused,
            foreground);
    }

    private static void Record(Subject container, AutomationElement element, List<string> passed)
    {
        Attempt.UntilTrue(
            () => Selected(element) is { } now && (passed.Count == 0 || now != passed[^1]),
            container.ActMs,
            container.PollMs);

        var settled = Selected(element);
        if (settled is not null && (passed.Count == 0 || settled != passed[^1]))
            passed.Add(settled);
    }

    private static bool TryThePattern(AutomationElement item, out string? refused)
    {
        try
        {
            ((SelectionItemPattern)item.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
            refused = null;
            return true;
        }
        catch (Exception thrown)
            when (thrown is InvalidOperationException or ElementNotAvailableException or ArgumentException)
        {
            refused = thrown.Message;
            return false;
        }
    }

    private static string? Selected(AutomationElement container)
    {
        try
        {
            if (container.GetCurrentPattern(SelectionPattern.Pattern) is not SelectionPattern selection)
                return null;

            var chosen = selection.Current.GetSelection();
            return chosen.Length == 0 ? null : chosen[0].Current.Name;
        }
        catch (Exception unreadable)
            when (unreadable is InvalidOperationException or ElementNotAvailableException)
        {
            return null;
        }
    }

    /// <summary>
    /// The values, read through the container rather than through each item's own actionability.
    /// An item of a shut picker is offscreen by design, and that is a property of the picker being
    /// shut rather than of the item being unreachable — the container is what has to be actionable.
    /// </summary>
    private static List<(string Name, AutomationElement Element)> Items(AutomationElement container)
    {
        var found = new List<(string, AutomationElement)>();
        try
        {
            foreach (AutomationElement item in container.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem)))
            {
                found.Add((item.Current.Name ?? "", item));
            }
        }
        catch (ElementNotAvailableException)
        {
            // The picker went while it was being read; what was found stands.
        }

        return found;
    }
}

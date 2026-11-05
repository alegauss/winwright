using System.Collections.ObjectModel;

namespace Winwright.Locating;

/// <summary>
/// One of the four things an act needs, or the answer that it has all of them. Each is its own
/// member because each has a different remedy, and a refusal saying only "not actionable" leaves
/// the reader to find out which.
/// </summary>
public enum Actionable
{
    /// <summary>All four hold, and the act may run.</summary>
    Yes,

    /// <summary>Nothing matched the locator, or what matched has since gone.</summary>
    NotInTree,

    /// <summary>UI Automation considers it out of view: scrolled away, collapsed, or minimised.</summary>
    Offscreen,

    /// <summary>It is there and it will not take input.</summary>
    Disabled,

    /// <summary>It offers no pattern for the act. The one no browser has to check.</summary>
    PatternMissing,
}

/// <summary>
/// Whether an element can take the act about to be run against it.
/// <para>
/// Playwright waits for visible, stable, enabled and able to receive events. The Windows
/// equivalent is present in the tree, not offscreen, enabled, and carrying the pattern the act
/// needs — and the fourth is the one no browser has, because a control offering no invoke pattern
/// cannot be pressed without going through the foreground, which is a different task's problem
/// and a worse answer.
/// </para>
/// </summary>
public sealed record ActionabilityCheck
{
    private ActionabilityCheck(
        ElementFacts? element, string? patternNeeded, IReadOnlyList<Actionable> missing)
    {
        Element = element;
        PatternNeeded = patternNeeded;
        Missing = missing;
        State = missing.Count == 0 ? Actionable.Yes : missing[0];
    }

    /// <summary>What was read, or null where nothing was there to read.</summary>
    public ElementFacts? Element { get; }

    /// <summary>The pattern the act needs, or null where the act needs none.</summary>
    public string? PatternNeeded { get; }

    /// <summary>
    /// Every one of the four that does not hold, in the order they are checked. Kept whole because
    /// an element that is both offscreen and disabled has two things wrong with it, and fixing the
    /// first would otherwise reveal the second one run later.
    /// </summary>
    public IReadOnlyList<Actionable> Missing { get; }

    /// <summary>The first thing missing, which is the one the sentence leads with.</summary>
    public Actionable State { get; }

    /// <summary>Whether the act may run.</summary>
    public bool CanAct => State == Actionable.Yes;

    /// <summary>
    /// Judge one element. The order is the order the four are stated in: present, on screen,
    /// enabled, carrying the pattern — structural first, so a refusal about a pattern is never
    /// read off an element that was not there.
    /// </summary>
    /// <param name="element">What was read, or null where the locator matched nothing.</param>
    /// <param name="patternNeeded">The pattern the act goes through, or null where it needs none.</param>
    public static ActionabilityCheck Of(ElementFacts? element, string? patternNeeded = null)
    {
        var missing = new List<Actionable>();

        if (element is null)
        {
            missing.Add(Actionable.NotInTree);
            return new ActionabilityCheck(null, patternNeeded, new ReadOnlyCollection<Actionable>(missing));
        }

        if (element.IsOffscreen)
            missing.Add(Actionable.Offscreen);
        if (!element.IsEnabled)
            missing.Add(Actionable.Disabled);
        if (!string.IsNullOrWhiteSpace(patternNeeded) && !element.Supports(patternNeeded))
            missing.Add(Actionable.PatternMissing);

        return new ActionabilityCheck(element, patternNeeded, new ReadOnlyCollection<Actionable>(missing));
    }

    /// <summary>What is wrong and what would fix it, or the sentence saying nothing is.</summary>
    public string Because => State switch
    {
        Actionable.Yes => "it is in the tree, on screen, enabled, and carries what the act needs",
        Actionable.NotInTree => "nothing matched, or what matched has gone since",
        Actionable.Offscreen => $"{Element} is offscreen: scroll it into view, or the window is minimised",
        Actionable.Disabled => $"{Element} is disabled: the application is not ready for this act yet",
        _ => $"{Element} offers no {PatternNeeded} pattern; it has {Offered()}",
    };

    /// <summary>The whole reading, with the rest of what is wrong where more than one thing is.</summary>
    public string Sentence()
    {
        if (CanAct)
            return $"{Element} can take the act: {Because}.";

        var rest = Missing.Count > 1
            ? $" (also {string.Join(", ", Missing.Skip(1).Select(Worded))})"
            : "";

        return $"{Because}{rest}.";
    }

    /// <summary>One of the four as a person says it, rather than as the enum spells it.</summary>
    public static string Worded(Actionable missing) => missing switch
    {
        Actionable.Yes => "actionable",
        Actionable.NotInTree => "not in the tree",
        Actionable.Offscreen => "offscreen",
        Actionable.Disabled => "disabled",
        _ => "missing the pattern",
    };

    /// <summary>Stop here unless all four hold.</summary>
    /// <exception cref="NotActionableException">Where one of them does not.</exception>
    public void Require(string locator)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locator);
        if (!CanAct)
            throw new NotActionableException(locator, State, Sentence());
    }

    private string Offered() => Element is null || Element.Patterns.Count == 0
        ? "none at all"
        : string.Join(", ", Element.Patterns.OrderBy(name => name, StringComparer.Ordinal));
}

/// <summary>
/// An act was about to run against an element that could not take it. It carries which of the four
/// was missing, because each one has a different remedy and the reader's next move depends on it.
/// </summary>
public sealed class NotActionableException : Exception
{
    /// <param name="locator">The locator the act was about, as it was written.</param>
    /// <param name="missing">Which of the four was missing.</param>
    /// <param name="because">The sentence naming it and what would fix it.</param>
    public NotActionableException(string locator, Actionable missing, string because)
        : base($"{locator} cannot take this act: {because}")
    {
        Locator = locator;
        Missing = missing;
        Because = because;
    }

    /// <summary>The locator the act was about.</summary>
    public string Locator { get; }

    /// <summary>Which of the four was missing.</summary>
    public Actionable Missing { get; }

    /// <summary>The sentence naming it.</summary>
    public string Because { get; }
}

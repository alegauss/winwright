using System.Windows.Automation;

namespace Winwright.Locating;

/// <summary>
/// What an element's patterns read at one instant, as values.
/// <para>
/// This exists because of one trap. UI Automation's <c>Current</c> accessors are live views: a
/// copy of one is not a copy of what it said, it is another way to ask the same question later.
/// Holding a pattern and comparing its value before an act against its value after compares the
/// reading with itself and can never fail — claude-tray's slider check carries a note about it and
/// casts the numbers out first. Every field here is a plain value read at construction, so the
/// trap is closed once rather than remembered by every author who writes an assertion about a
/// change.
/// </para>
/// </summary>
public sealed record PatternValues
{
    /// <summary>How much text is taken from a text pattern. A document is not a value to compare.</summary>
    public const int MostText = 4096;

    private PatternValues()
    {
    }

    /// <summary>What a value pattern read, or null where the element offers none.</summary>
    public string? Value { get; private init; }

    /// <summary>Whether that value pattern says it is read-only.</summary>
    public bool? IsReadOnly { get; private init; }

    /// <summary>What a range value pattern read.</summary>
    public double? Range { get; private init; }

    /// <summary>The smallest that range accepts.</summary>
    public double? RangeMinimum { get; private init; }

    /// <summary>The largest that range accepts.</summary>
    public double? RangeMaximum { get; private init; }

    /// <summary>On, Off or Indeterminate, where the element toggles.</summary>
    public string? Toggle { get; private init; }

    /// <summary>Whether a selection item reads as selected.</summary>
    public bool? IsSelected { get; private init; }

    /// <summary>Expanded, Collapsed, PartiallyExpanded or LeafNode, where the element expands.</summary>
    public string? ExpandCollapse { get; private init; }

    /// <summary>The first <see cref="MostText"/> characters a text pattern reads.</summary>
    public string? Text { get; private init; }

    /// <summary>Nothing was read, because the element offered nothing worth reading.</summary>
    public static PatternValues None { get; } = new();

    /// <summary>
    /// The one value worth showing in a line, or null where the element reports none. The order is
    /// what a reader looks at first: what it says, then where it sits, then what state it is in.
    /// </summary>
    public string? Reading() =>
        Value
        ?? Range?.ToString(System.Globalization.CultureInfo.InvariantCulture)
        ?? Toggle
        ?? (IsSelected is { } selected ? selected ? "selected" : "not selected" : null)
        ?? ExpandCollapse
        ?? Text;

    /// <summary>
    /// Read every pattern this element offers into values, now. A pattern that throws while being
    /// read leaves its own field null rather than losing the ones already read: the element is
    /// going away, and half a reading of what it was is better than none of it.
    /// </summary>
    public static PatternValues Of(AutomationElement? element, ElementFacts? facts)
    {
        if (element is null || facts is null)
            return None;

        var values = new PatternValues();
        if (facts.Supports("Value"))
            values = Read(values, () => element.GetCurrentPattern(ValuePattern.Pattern) is ValuePattern pattern
                ? values with { Value = pattern.Current.Value, IsReadOnly = pattern.Current.IsReadOnly }
                : values);

        if (facts.Supports("RangeValue"))
            values = Read(values, () => element.GetCurrentPattern(RangeValuePattern.Pattern) is RangeValuePattern pattern
                ? values with
                {
                    Range = pattern.Current.Value,
                    RangeMinimum = pattern.Current.Minimum,
                    RangeMaximum = pattern.Current.Maximum,
                }
                : values);

        if (facts.Supports("Toggle"))
            values = Read(values, () => element.GetCurrentPattern(TogglePattern.Pattern) is TogglePattern pattern
                ? values with { Toggle = pattern.Current.ToggleState.ToString() }
                : values);

        if (facts.Supports("SelectionItem"))
            values = Read(values, () =>
                element.GetCurrentPattern(SelectionItemPattern.Pattern) is SelectionItemPattern pattern
                    ? values with { IsSelected = pattern.Current.IsSelected }
                    : values);

        if (facts.Supports("ExpandCollapse"))
            values = Read(values, () =>
                element.GetCurrentPattern(ExpandCollapsePattern.Pattern) is ExpandCollapsePattern pattern
                    ? values with { ExpandCollapse = pattern.Current.ExpandCollapseState.ToString() }
                    : values);

        if (facts.Supports("Text"))
            values = Read(values, () => element.GetCurrentPattern(TextPattern.Pattern) is TextPattern pattern
                ? values with { Text = pattern.DocumentRange.GetText(MostText) }
                : values);

        return values;
    }

    private static PatternValues Read(PatternValues so_far, Func<PatternValues> reading)
    {
        try
        {
            return reading();
        }
        catch (Exception unreadable)
            when (unreadable is ElementNotAvailableException or InvalidOperationException or ArgumentException)
        {
            return so_far;
        }
    }
}

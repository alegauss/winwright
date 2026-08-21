using System.Text;

namespace Winwright.Locating;

/// <summary>
/// How matches are put in order before an index picks one. The tree's own order is whatever the
/// application happened to create things in; the other four are properties of the window a reader
/// can see, which is what makes the choice reviewable in the file.
/// </summary>
public enum MatchOrder
{
    /// <summary>The order UI Automation walks them in. The default, and the arbitrary one.</summary>
    Tree,

    /// <summary>Left to right, then top to bottom.</summary>
    Left,

    /// <summary>Right to left, then top to bottom.</summary>
    Right,

    /// <summary>Top to bottom, then left to right.</summary>
    Top,

    /// <summary>Bottom to top, then left to right.</summary>
    Bottom,
}

/// <summary>
/// One hop of a locator: what to match at this level of the tree. Every field is optional on its
/// own and a step with none of them is refused, because a step that constrains nothing addresses
/// everything.
/// </summary>
public sealed record LocatorStep
{
    internal LocatorStep(
        string? controlType,
        string? automationId,
        string? name,
        string? className,
        string? pattern,
        int? index,
        MatchOrder? order = null)
    {
        ControlType = controlType;
        AutomationId = automationId;
        Name = name;
        ClassName = className;
        Pattern = pattern;
        Index = index;
        Order = order;
    }

    /// <summary>The UI Automation control type, spelled as UI Automation spells it.</summary>
    public string? ControlType { get; }

    /// <summary>The automation id — the one field an application controls and a locator should prefer.</summary>
    public string? AutomationId { get; }

    /// <summary>The name, which is what a person sees and therefore what a language changes.</summary>
    public string? Name { get; }

    /// <summary>The window class, which is what tells one framework's chrome from another's.</summary>
    public string? ClassName { get; }

    /// <summary>The pattern the element must carry, which is what makes a step actionable.</summary>
    public string? Pattern { get; }

    /// <summary>Which match, counting from one. Null where the step must match exactly one.</summary>
    public int? Index { get; }

    /// <summary>
    /// The order matches are put in before <see cref="Index"/> picks one. Null where the step
    /// says nothing about it, which is the tree's own order.
    /// </summary>
    public MatchOrder? Order { get; }

    /// <summary>
    /// Whether this step says which one it means. A step that does not, and that matches more
    /// than one element, is refused rather than answered with whichever came first.
    /// </summary>
    public bool Disambiguated => Index is not null || Order is not null;

    /// <summary>The step as the grammar writes it, in a fixed order, so a parse round-trips.</summary>
    public override string ToString()
    {
        var text = new StringBuilder();
        if (ControlType is not null)
            text.Append(ControlType);
        if (AutomationId is not null)
            text.Append('#').Append(Quoted(AutomationId, bare: true));
        if (Name is not null)
            text.Append("[name=").Append(Quoted(Name, bare: false)).Append(']');
        if (ClassName is not null)
            text.Append("[class=").Append(Quoted(ClassName, bare: false)).Append(']');
        if (Pattern is not null)
            text.Append("[pattern=").Append(Pattern).Append(']');
        if (Order is not null)
            text.Append("[order=").Append(Order.Value.ToString().ToLowerInvariant()).Append(']');
        if (Index is not null)
            text.Append("[index=").Append(Index.Value).Append(']');

        return text.ToString();
    }

    private static string Quoted(string value, bool bare)
    {
        if (bare)
            return value;

        return $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }
}

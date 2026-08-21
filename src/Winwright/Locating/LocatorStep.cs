using System.Text;

namespace Winwright.Locating;

/// <summary>
/// One hop of a locator: what to match at this level of the tree. Every field is optional on its
/// own and a step with none of them is refused, because a step that constrains nothing addresses
/// everything.
/// </summary>
public sealed record LocatorStep
{
    internal LocatorStep(
        string? controlType, string? automationId, string? name, string? className, string? pattern, int? index)
    {
        ControlType = controlType;
        AutomationId = automationId;
        Name = name;
        ClassName = className;
        Pattern = pattern;
        Index = index;
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

using System.Windows.Automation;

namespace Winwright.Locating;

/// <summary>
/// What UI Automation says about one element, read once. It is a snapshot rather than a live
/// handle on purpose: every property on a live element is a cross-process call that can throw the
/// moment the element goes away, and a judgement made from four properties read at four different
/// instants is a judgement about no element that ever existed.
/// </summary>
/// <param name="Name">What a person sees, and therefore what a language changes.</param>
/// <param name="AutomationId">The one field an application controls.</param>
/// <param name="ControlType">The control type, spelled as UI Automation spells it.</param>
/// <param name="ClassName">The window class.</param>
/// <param name="IsOffscreen">Whether UI Automation considers it scrolled or collapsed out of view.</param>
/// <param name="IsEnabled">Whether it will take input at all.</param>
/// <param name="Patterns">The patterns it offers, by short name.</param>
public sealed record ElementFacts(
    string Name,
    string AutomationId,
    string ControlType,
    string ClassName,
    bool IsOffscreen,
    bool IsEnabled,
    IReadOnlySet<string> Patterns)
{
    /// <summary>Whether it offers that pattern, spelled as the locator grammar spells one.</summary>
    public bool Supports(string pattern) => Patterns.Contains(pattern);

    /// <summary>The one line a trace or a refusal names it by.</summary>
    public override string ToString()
    {
        var named = string.IsNullOrEmpty(AutomationId)
            ? string.IsNullOrEmpty(Name) ? "(unnamed)" : $"'{Name}'"
            : $"#{AutomationId}";

        return $"{ControlType} {named}";
    }

    /// <summary>
    /// Read all of it in one pass. Null where the element is gone — which is not an error here,
    /// it is the first of the four things actionability is about, and it has its own sentence.
    /// </summary>
    public static ElementFacts? Of(AutomationElement? element)
    {
        if (element is null)
            return null;

        try
        {
            var current = element.Current;
            return new ElementFacts(
                current.Name ?? "",
                current.AutomationId ?? "",
                Short(current.ControlType?.ProgrammaticName, "ControlType."),
                current.ClassName ?? "",
                current.IsOffscreen,
                current.IsEnabled,
                element.GetSupportedPatterns()
                    .Select(pattern => Short(pattern.ProgrammaticName, suffix: "PatternIdentifiers.Pattern"))
                    .ToHashSet(StringComparer.Ordinal));
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
    }

    private static string Short(string? programmaticName, string prefix = "", string suffix = "")
    {
        var name = programmaticName ?? "";
        if (prefix.Length > 0 && name.StartsWith(prefix, StringComparison.Ordinal))
            name = name[prefix.Length..];
        if (suffix.Length > 0 && name.EndsWith(suffix, StringComparison.Ordinal))
            name = name[..^suffix.Length];

        return name;
    }
}

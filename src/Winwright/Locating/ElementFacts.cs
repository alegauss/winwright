using System.Windows.Automation;

using Winwright.Windowing;

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
/// <param name="Bounds">Where it is on screen, which is what a capture and a click both need.</param>
/// <param name="Patterns">The patterns it offers, by short name.</param>
public sealed record ElementFacts(
    string Name,
    string AutomationId,
    string ControlType,
    string ClassName,
    bool IsOffscreen,
    bool IsEnabled,
    WindowBounds Bounds,
    IReadOnlySet<string> Patterns)
{
    /// <summary>
    /// Whether it held the keyboard focus at the instant the rest of this was read.
    /// <para>
    /// WW225. Read in the same pass as the other seven and for the reason this type exists at all: a
    /// case asserting that Tab moved the focus off a box is comparing where the focus was against
    /// where it is, and two answers taken at two instants would be a judgement about no moment that
    /// happened. An init property with a default rather than a ninth positional field, so the places
    /// that construct facts by hand for a refusal are not made to answer a question they have no
    /// element to ask.
    /// </para>
    /// </summary>
    public bool HasKeyboardFocus { get; init; }

    /// <summary>
    /// What this element says, which is its <see cref="Name"/> where it has one and nothing where it
    /// does not.
    /// <para>
    /// WW238. One definition of that, because there are two readers of it and they have to agree: the
    /// <c>name</c> reading a step names, and the sweep <c>covers</c> makes over every element a locator
    /// matched. A sweep that counted the unnamed as having read the empty string would hold against a
    /// pane of blank controls, so both want the name only where there is one — and deriving that twice
    /// is the second spelling that goes on saying the old thing after the first moves.
    /// </para>
    /// <para>
    /// Blank and not merely empty: UI Automation reports a name of spaces for some frameworks, and a
    /// label that shows a person nothing has said nothing.
    /// </para>
    /// </summary>
    public string? Says => string.IsNullOrWhiteSpace(Name) ? null : Name;

    /// <summary>
    /// The locator step that would address this element, spelled the way the grammar spells one.
    /// It is the whole point of inspecting: the line a reader copies is already a locator, so it
    /// is written from the tree rather than from the markup the check is about to assert on.
    /// <para>
    /// A control type UI Automation reports that the grammar does not accept is left out rather
    /// than written into a step that would not parse — a custom control can report anything.
    /// </para>
    /// </summary>
    public LocatorStep AsLocatorStep() => new(
        UiaVocabulary.IsControlType(ControlType) ? [ControlType] : [],
        string.IsNullOrEmpty(AutomationId) ? null : AutomationId,
        string.IsNullOrEmpty(Name) ? null : Name,
        string.IsNullOrEmpty(ClassName) ? null : ClassName,
        null,
        null);

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
                Rectangle(current.BoundingRectangle),
                element.GetSupportedPatterns()
                    .Select(pattern => Short(pattern.ProgrammaticName, suffix: "PatternIdentifiers.Pattern"))
                    .ToHashSet(StringComparer.Ordinal))
            {
                HasKeyboardFocus = current.HasKeyboardFocus,
            };
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
    }

    private static WindowBounds Rectangle(System.Windows.Rect rect) =>
        rect.IsEmpty || double.IsInfinity(rect.Width) || double.IsInfinity(rect.Height)
            ? default
            : new WindowBounds((int)rect.Left, (int)rect.Top, (int)rect.Right, (int)rect.Bottom);

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

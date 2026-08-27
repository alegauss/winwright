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
        IReadOnlyList<string> controlTypes,
        string? automationId,
        string? name,
        string? className,
        string? pattern,
        int? index,
        MatchOrder? order = null)
    {
        ControlTypes = controlTypes;
        AutomationId = automationId;
        Name = name;
        ClassName = className;
        Pattern = pattern;
        Index = index;
        Order = order;
    }

    /// <summary>
    /// The UI Automation control types this step matches, spelled as UI Automation spells them, and
    /// empty where the step says nothing about the type.
    /// <para>
    /// WW274. Several and not one, because a rule under test governs a family of controls as often as
    /// it governs one. claude-tray's row rule names every control with no content of its own to derive
    /// a name from — a ComboBox, a Slider, a TextBox, and a switch — and excludes the rest by <em>what
    /// they are</em> rather than by a list of ids. Written as one step per type, most of the steps
    /// match nothing on any given panel and the run is a page of holes.
    /// </para>
    /// </summary>
    public IReadOnlyList<string> ControlTypes { get; }

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

    /// <summary>
    /// Whether any of the strings this step matches on carries that text.
    /// <para>
    /// WW276. It is asked of the <em>last</em> step of a locator and of no other, because that is the
    /// one a sweep's matches are of. `Group[name="{}"]` finding nothing means the strings declare a
    /// row this window does not draw; `Group[name="{}"] &gt; ComboBox` finding nothing means the row
    /// is there and holds no picker, which is a different fact and a different verdict.
    /// </para>
    /// </summary>
    /// <param name="what">The text to look for, such as the member placeholder.</param>
    public bool Mentions(string what) =>
        (Name?.Contains(what, StringComparison.Ordinal) ?? false)
        || (AutomationId?.Contains(what, StringComparison.Ordinal) ?? false)
        || (ClassName?.Contains(what, StringComparison.Ordinal) ?? false);

    /// <summary>
    /// Two steps are equal where they constrain the same things, which the compiler's own answer
    /// stopped being the day <see cref="ControlTypes"/> became a list: a record compares a reference
    /// there, so two parses of one locator were unequal and the round-trip that proves the grammar
    /// writes what it reads went red on two identical lines.
    /// </summary>
    /// <param name="other">The step to compare with.</param>
    public bool Equals(LocatorStep? other) =>
        other is not null
        && ControlTypes.SequenceEqual(other.ControlTypes, StringComparer.Ordinal)
        && string.Equals(AutomationId, other.AutomationId, StringComparison.Ordinal)
        && string.Equals(Name, other.Name, StringComparison.Ordinal)
        && string.Equals(ClassName, other.ClassName, StringComparison.Ordinal)
        && string.Equals(Pattern, other.Pattern, StringComparison.Ordinal)
        && Index == other.Index
        && Order == other.Order;

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var one in ControlTypes)
            hash.Add(one, StringComparer.Ordinal);

        hash.Add(AutomationId, StringComparer.Ordinal);
        hash.Add(Name, StringComparer.Ordinal);
        hash.Add(ClassName, StringComparer.Ordinal);
        hash.Add(Pattern, StringComparer.Ordinal);
        hash.Add(Index);
        hash.Add(Order);
        return hash.ToHashCode();
    }

    /// <summary>The step as the grammar writes it, in a fixed order, so a parse round-trips.</summary>
    public override string ToString()
    {
        var text = new StringBuilder();
        if (ControlTypes.Count > 0)
            text.Append(string.Join("|", ControlTypes));
        if (AutomationId is not null)
            text.Append('#').Append(Quoted(AutomationId, bare: Bare(AutomationId)));
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

    /// <summary>
    /// Whether an id can be written without quotation marks — which is to say, whether the grammar
    /// would read the whole of it back.
    /// <para>
    /// WW124. The name field was quoted for exactly this reason and the id was not, so an id was
    /// assumed to be an identifier. Windows gives a window's own system menu the id <c>Item 1</c>,
    /// and a step rendered from it was refused at the space by the grammar that had just written
    /// it. The whole claim of inspecting is that a line it printed can be copied into a scenario,
    /// and a line that cannot is worse than no line: it is an answer that looks usable and fails
    /// at parse time, in a file somebody wrote from it.
    /// </para>
    /// </summary>
    private static bool Bare(string id) =>
        id.Length > 0 && id.All(one => char.IsLetterOrDigit(one) || one is '_' or '.' or '-');

    /// <summary>
    /// A value as the grammar writes it. The backslash first, or every escape written after it
    /// would be escaped again on the next pass.
    /// <para>
    /// WW124. The line break is escaped for the same reason the quotation mark is, and it was
    /// found the same way: a tray icon's name is a tooltip and a real one runs to several lines, so
    /// a step rendered from one ran to several lines — and a verb whose whole claim is one line per
    /// element was printing three, of which only the first could be copied anywhere.
    /// </para>
    /// </summary>
    private static string Quoted(string value, bool bare)
    {
        if (bare)
            return value;

        var escaped = value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);

        return $"\"{escaped}\"";
    }
}

using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Automation;

namespace Winwright.Locating;

/// <summary>One element of an inspected tree, with whatever of its children was walked.</summary>
/// <param name="Facts">What UI Automation says about it.</param>
/// <param name="Children">Its children in control-view order, as far as the walk went.</param>
/// <param name="Elided">
/// How many children were not walked here, because the depth or the width ran out. Never silent:
/// a listing that stops without saying so reads as a tree that ends there.
/// </param>
public sealed record InspectedElement(
    ElementFacts Facts, IReadOnlyList<InspectedElement> Children, int Elided)
{
    /// <summary>Every element in this subtree, this one first, in the order they were walked.</summary>
    public IEnumerable<InspectedElement> Walk()
    {
        yield return this;
        foreach (var child in Children)
            foreach (var deeper in child.Walk())
                yield return deeper;
    }
}

/// <summary>
/// The control view, as a verb.
/// <para>
/// The dump exists in claude-tray and prints only on a failure, after a throwaway script had
/// already been written to get the same output while diagnosing a missing template part. Making
/// it a verb is what lets a locator be written from the tree instead of from the markup — and the
/// markup is the thing being asserted, so reading the expected name out of it is how a check comes
/// to agree with the defect it was written to catch.
/// </para>
/// <para>
/// Every rendered line begins with a locator step that addresses the element it describes, so the
/// answer is copied into a scenario rather than transcribed into one.
/// </para>
/// </summary>
public static class Inspect
{
    /// <summary>How deep a walk goes unless told otherwise. A desktop tree has no natural end.</summary>
    public const int DefaultDepth = 5;

    /// <summary>How many children of one element are walked unless told otherwise.</summary>
    public const int DefaultWidth = 200;

    /// <summary>The control view under a window, by handle.</summary>
    /// <returns>The tree, or null where the handle names no element UI Automation can reach.</returns>
    public static InspectedElement? Window(nint window, int depth = DefaultDepth, int width = DefaultWidth)
    {
        // Before the try, and deliberately: ArgumentOutOfRangeException is an ArgumentException,
        // so a guard thrown inside would be swallowed by the catch meant for an unusable handle,
        // and a caller passing a negative depth would get null instead of being told.
        ArgumentOutOfRangeException.ThrowIfNegative(depth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);

        if (window == 0)
            return null;

        try
        {
            return Under(AutomationElement.FromHandle(window), depth, width);
        }
        catch (Exception unreachable)
            when (unreachable is ElementNotAvailableException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>The control view under an element already in hand.</summary>
    public static InspectedElement? Under(AutomationElement? root, int depth = DefaultDepth, int width = DefaultWidth)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(depth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);

        var facts = ElementFacts.Of(root);
        return facts is null ? null : Walked(root!, facts, depth, width);
    }

    /// <summary>
    /// The tree as a person reads it: one line per element, indented, each beginning with the
    /// locator step that addresses it and ending with its rectangle and the patterns it offers.
    /// </summary>
    public static IReadOnlyList<string> Render(InspectedElement tree)
    {
        ArgumentNullException.ThrowIfNull(tree);

        var lines = new List<string>();
        Render(tree, 0, lines);
        return lines;
    }

    /// <summary>One element as its own line, without its children.</summary>
    public static string Line(InspectedElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        var facts = element.Facts;
        var text = new StringBuilder(facts.AsLocatorStep().ToString());
        text.Append("  ").Append(facts.Bounds);
        if (facts.IsOffscreen)
            text.Append(" offscreen");
        if (!facts.IsEnabled)
            text.Append(" disabled");
        if (facts.Patterns.Count > 0)
            text.Append("  ").Append(string.Join(", ", facts.Patterns.OrderBy(name => name, StringComparer.Ordinal)));

        return text.ToString();
    }

    private static void Render(InspectedElement element, int level, List<string> lines)
    {
        var indent = new string(' ', level * 2);
        lines.Add(indent + Line(element));
        foreach (var child in element.Children)
            Render(child, level + 1, lines);

        if (element.Elided > 0)
            lines.Add($"{indent}  ... {element.Elided} more not walked");
    }

    private static InspectedElement Walked(AutomationElement element, ElementFacts facts, int depth, int width)
    {
        if (depth == 0)
            return new InspectedElement(facts, [], Count(element));

        var walker = TreeWalker.ControlViewWalker;
        var children = new List<InspectedElement>();
        var elided = 0;

        try
        {
            for (var child = walker.GetFirstChild(element); child is not null; child = walker.GetNextSibling(child))
            {
                if (children.Count >= width)
                {
                    elided++;
                    continue;
                }

                var childFacts = ElementFacts.Of(child);
                if (childFacts is not null)
                    children.Add(Walked(child, childFacts, depth - 1, width));
            }
        }
        catch (ElementNotAvailableException)
        {
            // The subtree went while it was being walked; what was seen stands.
        }

        return new InspectedElement(facts, new ReadOnlyCollection<InspectedElement>(children), elided);
    }

    private static int Count(AutomationElement element)
    {
        try
        {
            var walker = TreeWalker.ControlViewWalker;
            var seen = 0;
            for (var child = walker.GetFirstChild(element); child is not null; child = walker.GetNextSibling(child))
                seen++;

            return seen;
        }
        catch (ElementNotAvailableException)
        {
            return 0;
        }
    }
}

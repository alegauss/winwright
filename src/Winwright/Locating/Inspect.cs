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

    /// <summary>
    /// How many elements this walk did not reach, anywhere under here.
    /// <para>
    /// WW189. <see cref="Elided" /> is per element and <c>Inspect.Render</c> prints it, so a
    /// person reading the page is told. A caller <em>searching</em> the walk was not: it looked at
    /// the elements it got and said nothing about the ones it did not, so a control deeper than the
    /// walk went read as a control that is not there. Above zero, an absence is not a finding.
    /// </para>
    /// </summary>
    public int NotWalked => Walk().Sum(one => one.Elided);

    /// <summary>
    /// Whether the walk reached everything under here, so an absence in it is a real absence.
    /// </summary>
    public bool Whole => NotWalked == 0;
}

/// <summary>
/// One line of a rendered tree, with its parts kept rather than run together.
/// <para>
/// WW144. The line begins with the locator step and continues with the rectangle and the patterns,
/// separated by two spaces — so anything wanting the step back had to find where it ended, and two
/// files had grown their own parser for that. The second was rewritten mid-task when a name turned
/// out to contain a run of spaces: the separator is two spaces, and two spaces occur inside a name
/// somebody else wrote. The renderer had the step in its hand a moment earlier, so it hands it
/// over instead.
/// </para>
/// </summary>
/// <param name="Text">The whole line, indent and all, exactly as it prints.</param>
/// <param name="Step">
/// The locator step this line addresses its element by, or null where the line is not one to copy
/// — the root, which is marked rather than printed as a step, and the marker saying how many
/// children were not walked. That null is the filter downstream wants: a reader keeping the lines
/// with a step keeps exactly the lines that resolve.
/// </param>
/// <param name="Indent">The leading spaces, which are the only thing the depth shows on the page.</param>
/// <param name="Depth">How deep in the tree this is, the root being zero.</param>
/// <param name="Element">
/// What this line describes, or null on the marker line, which describes children that were not.
/// </param>
public sealed record RenderedLine(
    string Text, string? Step, string Indent, int Depth, InspectedElement? Element);

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
    public static IReadOnlyList<string> Render(InspectedElement tree) =>
        new ReadOnlyCollection<string>(Rendered(tree).Select(one => one.Text).ToList());

    /// <summary>
    /// The same tree, each line with its parts kept. This is the one renderer;
    /// <see cref="Render(InspectedElement)"/> is its text, so the step a reader is handed cannot
    /// drift from the step the line opens with.
    /// </summary>
    public static IReadOnlyList<RenderedLine> Rendered(InspectedElement tree)
    {
        ArgumentNullException.ThrowIfNull(tree);

        var lines = new List<RenderedLine>();
        Render(tree, 0, lines);
        return new ReadOnlyCollection<RenderedLine>(lines);
    }

    /// <summary>What a root's line opens with, so it is never mistaken for one to copy.</summary>
    public const string RootMark = "(root; locators below are relative to it)";

    /// <summary>
    /// The locator step this element's line opens with, or null where the line is not one to copy.
    /// <see cref="Line"/> opens with exactly this, which is what keeps the two the same answer.
    /// </summary>
    /// <param name="element">What is being rendered.</param>
    /// <param name="root">Whether this is the tree's own root — see <see cref="Line"/>.</param>
    public static string? CopyableStep(InspectedElement element, bool root = false)
    {
        ArgumentNullException.ThrowIfNull(element);
        return root ? null : element.Facts.AsLocatorStep().ToString();
    }

    /// <summary>One element as its own line, without its children.</summary>
    /// <param name="element">What to render.</param>
    /// <param name="root">
    /// Whether this is the tree's own root. A step searches the descendants of the root it is
    /// given and never the root itself, so a reader copying the first line got a miss diagnosed as
    /// absent — the least helpful answer this tool gives about the most obvious mistake. The root
    /// is therefore marked as the root rather than printed in the shape of something to copy, and
    /// what is left on the page is a set of lines every one of which resolves.
    /// </param>
    public static string Line(InspectedElement element, bool root = false)
    {
        ArgumentNullException.ThrowIfNull(element);

        var facts = element.Facts;
        var text = new StringBuilder(CopyableStep(element, root) ?? $"{RootMark} {facts}");
        text.Append("  ").Append(facts.Bounds);
        if (facts.IsOffscreen)
            text.Append(" offscreen");
        if (!facts.IsEnabled)
            text.Append(" disabled");
        if (facts.Patterns.Count > 0)
            text.Append("  ").Append(string.Join(", ", facts.Patterns.OrderBy(name => name, StringComparer.Ordinal)));

        return text.ToString();
    }

    private static void Render(InspectedElement element, int level, List<RenderedLine> lines)
    {
        var indent = new string(' ', level * 2);
        var root = level == 0;
        lines.Add(new RenderedLine(
            indent + Line(element, root), CopyableStep(element, root), indent, level, element));

        foreach (var child in element.Children)
            Render(child, level + 1, lines);

        // No step, and that is the field saying so rather than a reader matching on the words: what
        // this line describes is the children that were not walked, and none of them is addressable.
        if (element.Elided > 0)
        {
            var deeper = indent + "  ";
            lines.Add(new RenderedLine(
                $"{deeper}... {element.Elided} more not walked", null, deeper, level + 1, null));
        }
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

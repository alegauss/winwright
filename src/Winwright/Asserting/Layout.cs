using System.Collections.ObjectModel;
using System.Globalization;

using Winwright.Capturing;
using Winwright.Verdicts;
using Winwright.Windowing;

namespace Winwright.Asserting;

/// <summary>The four things a laid-out surface can be wrong about that no tree would report.</summary>
public enum Fault
{
    /// <summary>Two children of one parent cover the same pixels.</summary>
    Overlaps,

    /// <summary>A child begins left of or above the thing containing it.</summary>
    StartsOutside,

    /// <summary>A child ends right of or below it.</summary>
    EndsOutside,

    /// <summary>An element was laid out and occupies nothing.</summary>
    MeasuresNothing,
}

/// <summary>One thing wrong with a layout.</summary>
/// <param name="Kind">Which of the four.</param>
/// <param name="What">The element that is wrong.</param>
/// <param name="Against">What it is wrong against — its parent, or its sibling. Null where it stands alone.</param>
/// <param name="Detail">The sentence a red step carries, with the numbers in it.</param>
public sealed record LayoutFault(Fault Kind, DrawnElement What, DrawnElement? Against, string Detail)
{
    /// <summary>The one line a report shows.</summary>
    public override string ToString() => Detail;
}

/// <summary>What checking one geometry dump turned out to say.</summary>
public sealed record LayoutReading
{
    internal LayoutReading(
        int examined,
        IReadOnlyList<LayoutFault> faults,
        DrawnElement? root,
        WindowBounds covered,
        IReadOnlyList<DrawnElement>? concealed = null,
        IReadOnlyList<LayoutFault>? chrome = null)
    {
        Examined = examined;
        Faults = faults;
        Root = root;
        Covered = covered;
        Concealed = concealed ?? new ReadOnlyCollection<DrawnElement>([]);
        Chrome = chrome ?? new ReadOnlyCollection<LayoutFault>([]);
    }

    /// <summary>How many elements were examined. Zero is not a pass — see <see cref="AsAssertion"/>.</summary>
    public int Examined { get; }

    /// <summary>Everything wrong, in the order the walk found it.</summary>
    public IReadOnlyList<LayoutFault> Faults { get; }

    /// <summary>The surface everything was checked against.</summary>
    public DrawnElement? Root { get; }

    /// <summary>
    /// The smallest rectangle holding everything that drew. Against the root, this is what the
    /// page-above-a-screenful-of-blank-space failure looks like as a number.
    /// </summary>
    public WindowBounds Covered { get; }

    /// <summary>
    /// The elements the application was not showing, which this reading deliberately left alone.
    /// <para>
    /// WW130. A collapsed element lays out to nothing correctly, and on a real page the check fired
    /// on every hidden thing at once. They are recorded rather than dropped: a page hiding a note is
    /// not a page with a defect on it, and a count that is not silent is not a defect either.
    /// </para>
    /// </summary>
    public IReadOnlyList<DrawnElement> Concealed { get; }

    /// <summary>
    /// The faults a framework's own template is responsible for, kept apart from the rest.
    /// <para>
    /// WW131. Against a real themed window four of forty-five elements are laid out wrongly by every
    /// rule this check has, and every one is a part of the default tab template drawing a selected
    /// header over the edge on purpose. Those are true statements about what was drawn and they are
    /// not what anybody asked: no adopter can fix them, and every adopter would read past them.
    /// </para>
    /// <para>
    /// A fault lands here when either element it is about came out of a template. Both have to be
    /// the application's for it to be a finding about the application.
    /// </para>
    /// </summary>
    public IReadOnlyList<LayoutFault> Chrome { get; }

    /// <summary>
    /// The same reading with the framework's own faults counted among the rest, for a caller that
    /// really does mean to assert about somebody else's template.
    /// </summary>
    public LayoutReading WithChrome() => new(
        Examined,
        new ReadOnlyCollection<LayoutFault>([.. Faults, .. Chrome]),
        Root,
        Covered,
        Concealed);

    /// <summary>Whether nothing was found wrong. False on an empty dump, which found nothing at all.</summary>
    public bool Held => Examined > 0 && Faults.Count == 0;

    /// <summary>Rows of the surface below everything that drew on it.</summary>
    public int BlankBelow => Root is null ? 0 : Math.Max(0, Root.Bounds.Bottom - Covered.Bottom);

    /// <summary>Columns of it right of everything that drew.</summary>
    public int BlankRight => Root is null ? 0 : Math.Max(0, Root.Bounds.Right - Covered.Right);

    /// <summary>Narrow this to the faults a case actually means to insist on.</summary>
    /// <param name="kinds">The faults that matter here. Naming none is refused.</param>
    /// <remarks>
    /// Overlap is the one worth narrowing. Two children of one panel covering the same pixels is a
    /// defect on an installer page and ordinary on a window with an overlay in it, and this tool
    /// does not get to decide which a surface is.
    /// </remarks>
    public LayoutReading Only(params Fault[] kinds)
    {
        ArgumentNullException.ThrowIfNull(kinds);
        if (kinds.Length == 0)
            throw new ArgumentException("a reading of no faults holds against every layout there is", nameof(kinds));

        var wanted = new HashSet<Fault>(kinds);
        return new LayoutReading(
            Examined,
            new ReadOnlyCollection<LayoutFault>(Faults.Where(one => wanted.Contains(one.Kind)).ToList()),
            Root,
            Covered,
            Concealed,
            new ReadOnlyCollection<LayoutFault>(Chrome.Where(one => wanted.Contains(one.Kind)).ToList()));
    }

    /// <summary>What was checked and what was wrong, said either way.</summary>
    public string Sentence()
    {
        if (Examined == 0)
            return "there was no geometry to check.";

        var hidden = Concealed.Count == 0
            ? ""
            : $", and {Concealed.Count} the application is not showing left alone "
                + $"({string.Join(", ", Concealed.Select(one => one.ToString()))})";

        var theirs = Chrome.Count == 0
            ? ""
            : $", and {Chrome.Count} left to the framework's own template "
                + $"({string.Join("; ", Chrome.Select(one => one.Detail))})";

        hidden += theirs;

        if (Faults.Count == 0)
            return $"{Examined} element(s) laid out correctly under {Root}{hidden}.";

        return $"{Faults.Count} of {Examined} element(s) are laid out wrongly: "
            + string.Join("; ", Faults.Select(one => one.Detail)) + hidden + ".";
    }

    /// <summary>
    /// The result a verdict counts. An empty dump is <em>unchecked</em> and never a pass: nothing
    /// was examined, so a green would cover a check that did not run.
    /// </summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult AsAssertion(string named = "the page is laid out")
    {
        if (Examined == 0)
        {
            return AssertionResult.Unchecked(
                named,
                Precondition.Absent(
                    "a geometry dump to check",
                    "the application dumped nothing, so no layout was read at all"));
        }

        return Held ? AssertionResult.Pass(named, Sentence()) : AssertionResult.Fail(named, Sentence());
    }

    /// <summary>
    /// Whether the surface is filled to at least <paramref name="fraction"/> of its height. This is
    /// the reading the four faults cannot make: a page above a screenful of blank space is correct
    /// in every relation and wrong about the thing anybody looking would see first.
    /// </summary>
    /// <param name="fraction">How much of the height must be drawn on, from 0 to 1. The case names it, not this.</param>
    /// <param name="named">What the assertion claims.</param>
    public AssertionResult FillsAtLeast(double fraction, string named = "the page fills its surface")
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fraction);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fraction, 1);

        if (Root is null || Root.Bounds.Height <= 0)
        {
            return AssertionResult.Unchecked(
                named,
                Precondition.Absent("a surface with a height", "nothing was dumped, or the surface measured nothing"));
        }

        var drawn = (double)(Covered.Bottom - Root.Bounds.Top) / Root.Bounds.Height;
        var said = $"{(drawn * 100).ToString("0.#", CultureInfo.InvariantCulture)}% of {Root}'s height is drawn on, "
            + $"leaving {BlankBelow} row(s) blank below it";

        return drawn >= fraction
            ? AssertionResult.Pass(named, said + ".")
            : AssertionResult.Fail(
                named,
                said + $", and this case asks for {(fraction * 100).ToString("0.#", CultureInfo.InvariantCulture)}%.");
    }
}

/// <summary>
/// The layout, checked against itself.
/// <para>
/// One installer page was built four times and verified every time by reading the script, and the
/// failures that misses are the ones it had already produced: a caption assigned before its width
/// wrapped at column zero, a page that rendered correctly above a screenful of blank space, and a
/// button standing nine pixels below the box it belongs to — because an edit sizes itself to its
/// font and a button does not. Each was found by running an installer, which is the most expensive
/// place to find anything.
/// </para>
/// <para>
/// Nothing here is typed: every expectation is derived from the dump itself. A child is checked
/// against the parent the depth says it has, and a sibling against its siblings, so there is no
/// number in a scenario to go stale when the page is redesigned.
/// </para>
/// </summary>
public static class Layout
{
    /// <summary>Check a dump against itself.</summary>
    /// <param name="geometry">What the application dumped.</param>
    public static LayoutReading Of(ReadGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);

        var elements = geometry.Elements;
        if (elements.Count == 0)
            return new LayoutReading(0, new ReadOnlyCollection<LayoutFault>([]), null, default);

        var root = elements[0];
        var faults = new List<LayoutFault>();
        var concealed = new List<DrawnElement>();
        var covered = Nothing;

        for (var at = 0; at < elements.Count; at++)
        {
            var element = elements[at];

            // The root is left out of the covering box on purpose: it is the surface, not
            // something drawn on it, and including it makes every page read as entirely filled.
            if (element.Drawn && at > 0)
            {
                covered = Union(covered, element.Bounds);
            }
            else if (at > 0 && Concealing(elements, at) is not null)
            {
                // WW130: not laid out because the application is not showing it, or is not showing
                // something above it. That is the page working, not the fault this check is for.
                concealed.Add(element);
            }
            else if (at > 0)
            {
                faults.Add(new LayoutFault(
                    Fault.MeasuresNothing, element, null, $"{element} was laid out and occupies nothing"));
            }

            if (at == 0)
                continue;

            var parent = ParentOf(elements, at);
            if (parent is not null)
                Against(faults, element, parent);
        }

        // Siblings after parents, so a report reads containment first and then the two things that
        // are only wrong about each other. Both are found on the same walk.
        Overlapping(elements, faults);

        // WW131: sorted at the end rather than at each site, so the four ways a fault is found stay
        // one rule each and the question of whose element it is stays one rule too.
        return new LayoutReading(
            elements.Count,
            new ReadOnlyCollection<LayoutFault>(faults.Where(Ours).ToList()),
            root,
            covered.Equals(Nothing)
                ? new WindowBounds(root.Bounds.Left, root.Bounds.Top, root.Bounds.Left, root.Bounds.Top)
                : covered,
            new ReadOnlyCollection<DrawnElement>(concealed),
            new ReadOnlyCollection<LayoutFault>(faults.Where(one => !Ours(one)).ToList()));
    }

    /// <summary>The same, reading the dump off disk first.</summary>
    /// <param name="path">The dump file.</param>
    public static LayoutReading Of(string path) => Of(GeometryDump.Read(path));

    private static readonly WindowBounds Nothing = new(int.MaxValue, int.MaxValue, int.MinValue, int.MinValue);

    private static WindowBounds Union(WindowBounds left, WindowBounds right) => new(
        Math.Min(left.Left, right.Left),
        Math.Min(left.Top, right.Top),
        Math.Max(left.Right, right.Right),
        Math.Max(left.Bottom, right.Bottom));

    /// <summary>
    /// Whether a fault is one the application could do anything about. Both elements have to be
    /// its own: a tab item the application declared, drawn out of place by the panel its framework
    /// templated, is a fact about the framework however the application named the item.
    /// </summary>
    private static bool Ours(LayoutFault fault) => fault.What.IsOwn && (fault.Against?.IsOwn ?? true);

    /// <summary>
    /// The element that explains why this one was not laid out, which is itself or the nearest
    /// ancestor the application is not showing. Null where nothing explains it.
    /// <para>
    /// The ancestry is walked because a child of a collapsed panel is Visible in its own right and
    /// still measures nothing — the panel is why, and only the panel says so.
    /// </para>
    /// </summary>
    private static DrawnElement? Concealing(IReadOnlyList<DrawnElement> elements, int at)
    {
        if (!elements[at].IsShown)
            return elements[at];

        var depth = elements[at].Depth;
        for (var back = at - 1; back >= 0 && depth > 0; back--)
        {
            if (elements[back].Depth != depth - 1)
                continue;

            if (!elements[back].IsShown)
                return elements[back];

            depth--;
        }

        return null;
    }

    /// <summary>
    /// The parent, found by walking back to the nearest element one level shallower. That is what
    /// the depth is carried for: a flat file has no parent pointer and the order is the tree.
    /// </summary>
    private static DrawnElement? ParentOf(IReadOnlyList<DrawnElement> elements, int at)
    {
        for (var back = at - 1; back >= 0; back--)
        {
            if (elements[back].Depth == elements[at].Depth - 1)
                return elements[back];

            if (elements[back].Depth < elements[at].Depth - 1)
                return null;
        }

        return null;
    }

    private static void Against(List<LayoutFault> faults, DrawnElement child, DrawnElement parent)
    {
        if (!child.Drawn || !parent.Drawn)
            return;

        if (child.Bounds.Left < parent.Bounds.Left || child.Bounds.Top < parent.Bounds.Top)
        {
            faults.Add(new LayoutFault(
                Fault.StartsOutside,
                child,
                parent,
                $"{child} starts outside {parent} by left {Math.Max(0, parent.Bounds.Left - child.Bounds.Left)}, "
                    + $"top {Math.Max(0, parent.Bounds.Top - child.Bounds.Top)}"));
        }

        if (child.Bounds.Right > parent.Bounds.Right || child.Bounds.Bottom > parent.Bounds.Bottom)
        {
            faults.Add(new LayoutFault(
                Fault.EndsOutside,
                child,
                parent,
                $"{child} ends past {parent} by right {Math.Max(0, child.Bounds.Right - parent.Bounds.Right)}, "
                    + $"bottom {Math.Max(0, child.Bounds.Bottom - parent.Bounds.Bottom)}"));
        }
    }

    private static void Overlapping(IReadOnlyList<DrawnElement> elements, List<LayoutFault> faults)
    {
        for (var at = 1; at < elements.Count; at++)
        {
            var parent = ParentOf(elements, at);
            if (parent is null || !elements[at].Drawn)
                continue;

            for (var other = at + 1; other < elements.Count; other++)
            {
                if (elements[other].Depth < elements[at].Depth)
                    break;

                if (elements[other].Depth != elements[at].Depth || !elements[other].Drawn)
                    continue;

                if (!ReferenceEquals(ParentOf(elements, other), parent))
                    continue;

                if (Overlap(elements[at].Bounds, elements[other].Bounds) is WindowBounds shared)
                {
                    faults.Add(new LayoutFault(
                        Fault.Overlaps,
                        elements[at],
                        elements[other],
                        $"{elements[at]} overlaps {elements[other]} over {shared}"));
                }
            }
        }
    }

    private static WindowBounds? Overlap(WindowBounds left, WindowBounds right)
    {
        var shared = new WindowBounds(
            Math.Max(left.Left, right.Left),
            Math.Max(left.Top, right.Top),
            Math.Min(left.Right, right.Right),
            Math.Min(left.Bottom, right.Bottom));

        return shared.Width > 0 && shared.Height > 0 ? shared : null;
    }
}

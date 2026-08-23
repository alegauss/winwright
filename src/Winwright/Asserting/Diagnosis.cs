using System.Collections.ObjectModel;
using System.Text;

using Winwright.Locating;
using Winwright.Verdicts;

namespace Winwright.Asserting;

/// <summary>One element of the control view as a failure carries it: its line, and whether it is
/// the element the assertion was about.</summary>
/// <param name="Text">The line, indented under its parent the way the tree is shaped.</param>
/// <param name="IsSubject">Whether this is the element the check was reading when it went red.</param>
/// <param name="Step">
/// WW144: the locator step the line opens with, or null where the line is not one to copy — the
/// root and the marker saying how many children were not walked. Carried rather than recovered,
/// because a reader parsing it back out of <paramref name="Text"/> has to know where two spaces
/// separate a field and where two spaces are inside somebody's name.
/// </param>
public sealed record DiagnosedLine(string Text, bool IsSubject, string? Step = null)
{
    /// <summary>The line as a report prints it, the subject carrying the marker that finds it.</summary>
    public override string ToString() => (IsSubject ? "> " : "  ") + Text;
}

/// <summary>
/// A failure with the control view that explains it already attached.
/// <para>
/// Diagnosing a missing template part in claude-tray took a throwaway script that dumped the tree,
/// and the defect was obvious the moment somebody read the output. The check had the tree in hand
/// when it went red and printed a sentence about it instead, so the reading had to be done twice —
/// once by the harness, once by a person writing a script to ask the same question again.
/// </para>
/// <para>
/// Three things keep the attachment worth reading. It is on a red only, because a dump under every
/// passing assertion is a report nobody reaches the end of. It is bounded, and it says how much it
/// cut, since a listing that stops without saying so reads as a tree that ends there. And where
/// there is a subject the window is kept around <em>it</em> rather than around the root — a budget
/// that drops the one line the reader came for is worse than no dump at all.
/// </para>
/// </summary>
public sealed record Diagnosis
{
    /// <summary>How many elements are shown unless told otherwise.</summary>
    public const int DefaultBudget = 40;

    private Diagnosis(
        AssertionResult failure,
        IReadOnlyList<DiagnosedLine> view,
        int total,
        int above,
        int below,
        string absence)
    {
        Failure = failure;
        View = view;
        Total = total;
        DroppedAbove = above;
        DroppedBelow = below;
        Absence = absence;
    }

    /// <summary>The red this explains.</summary>
    public AssertionResult Failure { get; }

    /// <summary>The control view as it stood when the check went red, bounded by the budget.</summary>
    public IReadOnlyList<DiagnosedLine> View { get; }

    /// <summary>How many elements the tree held, shown and cut together.</summary>
    public int Total { get; }

    /// <summary>How many elements were cut from before the window.</summary>
    public int DroppedAbove { get; }

    /// <summary>How many were cut from after it.</summary>
    public int DroppedBelow { get; }

    /// <summary>How many were cut in all.</summary>
    public int Dropped => DroppedAbove + DroppedBelow;

    /// <summary>
    /// Why there is no control view here, where there is none — empty where there is one. A dump
    /// that is simply absent reads as a check that did not bother to look, which is the friction
    /// this file exists to remove rather than to reproduce in a quieter form.
    /// </summary>
    public string Absence { get; }

    /// <summary>Whether the tree was read at all.</summary>
    public bool WasRead => Absence.Length == 0;

    /// <summary>Whether the element the check was reading is in the view and carries the marker.</summary>
    public bool Marks => View.Any(line => line.IsSubject);

    /// <summary>
    /// Attach a tree already in hand to a failure.
    /// </summary>
    /// <param name="failure">The red. A pass or a hole is refused — see the remarks.</param>
    /// <param name="tree">The control view, or null where it could not be read.</param>
    /// <param name="subject">The element the check was reading, marked in the view where it is in it.</param>
    /// <param name="budget">How many elements to show. The window is kept around the subject.</param>
    /// <remarks>
    /// A pass is refused because a dump per passing assertion buries the one that matters, and an
    /// unchecked result is refused because the tree is not its explanation: an assertion that never
    /// ran is explained by the precondition it lacked, and a control view offered in its place
    /// invites a reader to hunt through a window for a defect that was never looked for.
    /// </remarks>
    public static Diagnosis Of(
        AssertionResult failure,
        InspectedElement? tree,
        ElementFacts? subject = null,
        int budget = DefaultBudget)
    {
        ArgumentNullException.ThrowIfNull(failure);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(budget);

        if (failure.Outcome != AssertionOutcome.Failed)
        {
            throw new ArgumentException(
                failure.Outcome == AssertionOutcome.Passed
                    ? $"'{failure.Name}' passed, and a control view under every green is a report nobody reads to the end"
                    : $"'{failure.Name}' never ran, and what explains a hole is the precondition it lacked, not the tree",
                nameof(failure));
        }

        if (tree is null)
        {
            return new Diagnosis(
                failure,
                [],
                0,
                0,
                0,
                "the control view could not be read: the window was gone, or never reachable, by the time this failed");
        }

        // WW144: rendered by the renderer rather than beside it. This walked the tree itself and
        // spelled the indent, the root mark and the elided marker a second time, so the view under
        // a red was one edit away from being a different page than the one `inspect` prints.
        var lines = Inspect.Rendered(tree)
            .Select(one => new DiagnosedLine(
                one.Text, one.Element is not null && IsSubject(one.Element.Facts, subject), one.Step))
            .ToList();

        return Bounded(failure, lines, budget);
    }

    /// <summary>The same, reading the window's control view now, at the instant it went red.</summary>
    /// <param name="failure">The red.</param>
    /// <param name="window">The window handle. Zero, or one UI Automation cannot reach, is an absence and not a throw.</param>
    /// <param name="subject">The element the check was reading.</param>
    /// <param name="budget">How many elements to show.</param>
    public static Diagnosis OfWindow(
        AssertionResult failure,
        nint window,
        ElementFacts? subject = null,
        int budget = DefaultBudget) =>
        Of(failure, Inspect.Window(window), subject, budget);

    /// <summary>
    /// The whole thing as a report prints it: the failure's own line, then the control view under
    /// it. This is what a red step carries, so the summary line and its diagnosis cannot drift.
    /// </summary>
    public IReadOnlyList<string> Render()
    {
        var lines = new List<string> { VerdictSummary.Line(Failure) };
        if (!WasRead)
        {
            lines.Add(Indent + Absence + ".");
            return new ReadOnlyCollection<string>(lines);
        }

        lines.Add(Indent + Heading());
        if (DroppedAbove > 0)
            lines.Add(Indent + $"  ... {Elements(DroppedAbove)} above");

        foreach (var line in View)
            lines.Add(Indent + line);

        if (DroppedBelow > 0)
            lines.Add(Indent + $"  ... {Elements(DroppedBelow)} below");

        return new ReadOnlyCollection<string>(lines);
    }

    /// <summary>The rendering as one block of text.</summary>
    public override string ToString() => string.Join('\n', Render());

    private const string Indent = "    ";

    private string Heading()
    {
        var shown = View.Count == Total
            ? $"{VerdictSummary.Plural(Total, "element")}"
            : $"{View.Count} of {VerdictSummary.Plural(Total, "element")}";

        var marked = Marks ? ", the one it read marked >" : "";
        return $"the control view when it failed ({shown}{marked}):";
    }

    private static string Elements(int count) => VerdictSummary.Plural(count, "element") + " not shown";

    /// <summary>
    /// Whether this is the element the check was reading. Compared on the step that addresses it
    /// rather than on the record, because the subject was read at one instant and the tree at
    /// another: bounds and enablement move between the two, and identity does not.
    /// </summary>
    private static bool IsSubject(ElementFacts facts, ElementFacts? subject) =>
        subject is not null
        && string.Equals(facts.AsLocatorStep().ToString(), subject.AsLocatorStep().ToString(), StringComparison.Ordinal);

    private static Diagnosis Bounded(AssertionResult failure, List<DiagnosedLine> lines, int budget)
    {
        if (lines.Count <= budget)
            return new Diagnosis(failure, new ReadOnlyCollection<DiagnosedLine>(lines), lines.Count, 0, 0, "");

        // Centred on the subject where there is one: a budget that drops the line the failure
        // pointed at leaves a reader with a tree and no reason to have been shown it.
        var marked = lines.FindIndex(line => line.IsSubject);
        var start = marked < 0 ? 0 : Math.Clamp(marked - (budget / 2), 0, lines.Count - budget);
        var kept = lines.GetRange(start, budget);

        return new Diagnosis(
            failure,
            new ReadOnlyCollection<DiagnosedLine>(kept),
            lines.Count,
            start,
            lines.Count - start - budget,
            "");
    }
}

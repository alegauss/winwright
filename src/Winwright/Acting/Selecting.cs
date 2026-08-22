using Winwright.Locating;
using Winwright.Tracing;
using Winwright.Verdicts;

namespace Winwright.Acting;

/// <summary>Which of the two routes made the selection stick, or that neither did.</summary>
public enum SelectRoute
{
    /// <summary>The selection pattern was enough, which is the ordinary case and needs no desktop.</summary>
    Pattern,

    /// <summary>The pattern did not confirm and a declared click did.</summary>
    Pointer,

    /// <summary>Neither did. This is a red, and it is said here rather than left to the next step.</summary>
    Neither,
}

/// <summary>What selecting something did, and whether anybody checked.</summary>
public sealed record Selected
{
    internal Selected(
        ElementFacts item, SelectRoute route, bool landed, bool pointerTried, string? because, Precondition foreground)
    {
        Item = item;
        Route = route;
        Landed = landed;
        PointerTried = pointerTried;
        Because = because;
        Foreground = foreground;
    }

    /// <summary>What was selected.</summary>
    public ElementFacts Item { get; }

    /// <summary>Which route took, or that neither did.</summary>
    public SelectRoute Route { get; }

    /// <summary>Whether the selection was confirmed to have landed.</summary>
    public bool Landed { get; }

    /// <summary>Whether the pointer was reached for at all.</summary>
    public bool PointerTried { get; }

    /// <summary>Why it did not land, where it did not.</summary>
    public string? Because { get; }

    /// <summary>Whether the window owned the desktop, on the route that needed it.</summary>
    public Precondition Foreground { get; }

    /// <summary>What happened, with the route in it.</summary>
    public override string ToString() => Route switch
    {
        SelectRoute.Pattern => $"{Item} was selected through the pattern and confirmed.",
        SelectRoute.Pointer => $"{Item} was selected by clicking it, the pattern having not confirmed.",
        _ => $"{Item} was not selected: {Because}.",
    };

    /// <summary>The step a trace records.</summary>
    public TraceStep AsTraceStep() => new()
    {
        Verb = "select",
        Locator = Item.ToString(),
        Resolved = Item.ToString(),
        Pattern = Route == SelectRoute.Pointer ? "synthesized input" : "SelectionItem",
        ReadBack = Landed ? "selected" : "not selected",
        Verdict = Landed ? StepVerdict.Ok : StepVerdict.Failed,
        Detail = Landed && Route == SelectRoute.Pattern ? null : ToString(),
    };
}

/// <summary>
/// Selecting something, and then checking that it took.
/// <para>
/// A tab control builds a tab's content on its first visit, so a selection that silently does not
/// land leaves the list inside it never realised — and the case then blames a forty-second scan
/// for a tab it never opened. It was seen alternating pass and degrade while that case was being
/// written, which is the shape that teaches a reader to re-run rather than to look.
/// </para>
/// <para>
/// Confirming that the selection took, and only then falling back to the pointer, is what makes
/// the next step's failure mean what it says. Nothing here reports a landing it did not confirm.
/// </para>
/// </summary>
public static class Selecting
{
    /// <summary>
    /// Select, confirm, and click only if the confirmation did not pass.
    /// </summary>
    /// <param name="item">What to select.</param>
    /// <param name="alsoUntil">
    /// A second condition that must also become true — the pane being realised, the list being in
    /// the tree. Selection alone says the control agreed; this says the application did.
    /// </param>
    /// <param name="mayUseThePointer">
    /// Whether the click is allowed at all. It is a declared act needing a real desktop, so a
    /// caller that must not need one says so and gets the honest red instead.
    /// </param>
    /// <param name="settleMs">How long each confirmation waits. The subject's own act time by default.</param>
    /// <exception cref="NotActionableException">Where the element cannot be selected at all.</exception>
    public static Selected Confirmed(
        Subject item, Func<bool>? alsoUntil = null, bool mayUseThePointer = true, int? settleMs = null)
    {
        ArgumentNullException.ThrowIfNull(item);

        var acted = Act.Select(item);
        var deadline = settleMs ?? item.ActMs;

        if (Took(item, alsoUntil, deadline, item.PollMs))
            return new Selected(acted.Element, SelectRoute.Pattern, true, false, null, Met());

        if (!mayUseThePointer)
        {
            return new Selected(
                acted.Element,
                SelectRoute.Neither,
                false,
                false,
                $"the pattern did not confirm within {deadline} ms, and the pointer was not allowed",
                Met());
        }

        PointerResult clicked;
        try
        {
            clicked = Pointer.Click(item);
        }
        catch (NotActionableException refused)
        {
            return new Selected(
                acted.Element, SelectRoute.Neither, false, true, $"the click was refused: {refused.Because}", Met());
        }

        if (!clicked.Landed)
        {
            return new Selected(
                acted.Element,
                SelectRoute.Neither,
                false,
                true,
                $"the pattern did not confirm and nothing was clicked: {clicked.Foreground.Absence}",
                clicked.Foreground);
        }

        return Took(item, alsoUntil, deadline, item.PollMs)
            ? new Selected(acted.Element, SelectRoute.Pointer, true, true, null, clicked.Foreground)
            : new Selected(
                acted.Element,
                SelectRoute.Neither,
                false,
                true,
                $"neither the pattern nor a click made it stick within {deadline} ms",
                clicked.Foreground);
    }

    private static bool Took(Subject item, Func<bool>? alsoUntil, int deadlineMs, int pollMs) =>
        Attempt.UntilTrue(
            () => (item.ReadOnce().Values.IsSelected ?? false) && (alsoUntil?.Invoke() ?? true),
            deadlineMs,
            pollMs).Happened;

    private static Precondition Met() => Precondition.Met(Windowing.Foreground.PreconditionName);
}

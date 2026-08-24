using System.Collections.ObjectModel;

using Winwright.Acting;
using Winwright.Capturing;
using Winwright.Windowing;

namespace Winwright.Verdicts;

/// <summary>One condition that is about the desk rather than about the code under test.</summary>
/// <param name="Named">The condition, spelled exactly as the reading that answers it spells it.</param>
/// <param name="Because">Why it is the desk's and not the application's.</param>
public sealed record DeskFact(string Named, string Because)
{
    /// <summary>The one line a listing shows.</summary>
    public override string ToString() => $"{Named}: {Because}";
}

/// <summary>
/// Which of this engine's conditions are facts about the desk, and why each one is.
/// <para>
/// WW183. The judgement was kept in an array of five names typed into the suite, and it had already
/// missed two: <see cref="ForeignInput.PreconditionName" />, which measured a person at the keyboard
/// and turned it into a red twice while WW172 was being measured, and
/// <see cref="Obstruction.PreconditionName" />, which WW38 added and nothing went back for.
/// </para>
/// <para>
/// It belongs here because this is where the readings are. A condition added beside its reading is
/// one somebody can classify while they still remember what it means; a condition classified in
/// another project is one classified by whoever next has a red they cannot explain.
/// </para>
/// <para>
/// The distinction is not mechanical and is not pretended to be. A desk fact is one the machine
/// could have arranged differently — who holds the foreground, what is on top of a rectangle,
/// whether somebody is typing. A stale binary, a page still computing and a window's own backdrop
/// are facts about the thing under test, and excusing an assertion on one of those would excuse it
/// on the defect it was looking for.
/// </para>
/// </summary>
public static class DeskFacts
{
    /// <summary>Every condition this engine calls the desk's, with the reason it is.</summary>
    public static IReadOnlyList<DeskFact> Known { get; } = new ReadOnlyCollection<DeskFact>(
    [
        new(Foreground.PreconditionName,
            "Windows grants the foreground to whoever it grants it to, and once this process has "
                + "been refused once it stops being granted"),
        new(Keyboard.FocusPreconditionName,
            "a key goes where the focus is, and the focus is the desk's to give"),
        new(FocusReading.Named,
            "an element belonging to another application is not an answer about this one"),
        new(TraySearch.PreconditionName,
            "the shell decides whether the overflow opens, and a taskbar something is covering has "
                + "no chevron to open it with"),
        new(OverflowState.PreconditionName,
            "the chevron belongs to the shell and so does the flyout it opens, and a taskbar "
                + "something is covering answers neither"),
        new(TrayMenu.PreconditionName,
            "the route to a tray menu is focus and then the application key, and a desk that gives "
                + "neither stops the act before it starts"),
        new(ForeignInput.PreconditionName,
            "somebody using the machine during a run is the machine's business, and a run cannot "
                + "tell its own synthesised input from a second person's"),
        new(Obstruction.PreconditionName,
            "a window somebody else left over the region is on the desk, and no capture of that "
                + "rectangle is a capture of what was underneath"),
    ]);

    /// <summary>The names alone, which is what a caller matching a condition against them wants.</summary>
    public static IReadOnlyList<string> Named { get; } =
        new ReadOnlyCollection<string>(Known.Select(one => one.Named).ToList());

    /// <summary>
    /// Whether a condition is one of them. Ordinal and exact: a condition matched loosely is a
    /// condition that will one day match a different one and excuse an assertion nobody meant to.
    /// </summary>
    /// <param name="condition">The condition's name, as the reading spells it.</param>
    public static bool Names(string condition) =>
        Named.Contains(condition?.Trim() ?? "", StringComparer.Ordinal);
}

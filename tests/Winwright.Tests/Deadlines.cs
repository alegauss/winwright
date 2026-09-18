using System.Collections.ObjectModel;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// One place this project waits for something to turn up, and what its look answers when nothing
/// has.
/// </summary>
/// <param name="File">The source file, by name.</param>
/// <param name="Waits">How many deadlines it opens.</param>
/// <param name="Nothing">What the look answers where the thing is not there yet.</param>
internal sealed record Deadline(string File, int Waits, string Nothing)
{
    public override string ToString() => $"{File,-22} {Waits} wait(s), nothing reads as {Nothing}";
}

/// <summary>
/// WW175. <c>Attempt.Until</c> polls until its look answers something other than null, so a look
/// that cannot answer null returns on the first poll and the deadline is gone. Nothing throws,
/// nothing warns, and the <c>Sighting</c> says it was found — because it was.
/// <para>
/// It nearly shipped. WW168 changed <c>NotificationArea.Find</c> from <c>TrayIcon?</c> to a reading,
/// and <c>TrayIconFixture.Placed</c> waits on it for five seconds to prove the shell placed the
/// icon. That wait became one look and the fixture would have gone on passing — the icon is usually
/// there by then — while quietly losing the only thing it exists for, which is failing when the
/// shell is slow. It was caught by reading the call site, and nothing else would have caught it.
/// </para>
/// <para>
/// C# cannot refuse the shape at the call: nullability is an annotation and not a type, so
/// <c>Func&lt;T?&gt;</c> and <c>Func&lt;T&gt;</c> are one runtime type and a look that never answers
/// nothing is a legal caller. <c>AttemptTests</c> has one on purpose, because a thing already there
/// costing no sleep is the documented behaviour. What separates that from the defect is intent, and
/// intent is not something a runtime check can read.
/// </para>
/// <para>
/// So what this does instead is make every deadline visible. A wait added later is red here until
/// somebody writes down what its look answers when the thing has not arrived — which is the one
/// question that would have caught the near-miss, asked at the moment the wait is written.
/// </para>
/// </summary>
internal static class Deadlines
{
    /// <summary>The call this is about, matched in the sources exactly as it is written.</summary>
    internal const string Opening = "Attempt.Until(";

    /// <summary>The generic spelling, which one caller needs because its lambda infers nothing.</summary>
    internal const string OpeningTyped = "Attempt.Until<";

    /// <summary>
    /// The other spelling, which takes a look answering a bool. WW446.
    /// <para>
    /// This catalogue matched one of the two for as long as it existed, and the one it matched is
    /// the rarer: eighteen deadlines were written down and there were sixty-odd. A reader was shown
    /// a list claiming to be every place this project waits, with most of them missing — which is
    /// worse than a shorter list, because nothing said it was short.
    /// </para>
    /// <para>
    /// The hazard is the same one read through a different type. <c>Until</c> collapses where the
    /// look cannot answer null; this collapses where it cannot answer false, and C# refuses neither
    /// at the call site. What the entries below say for one of these is therefore what makes the
    /// look false, which is the same question in the words this spelling has.
    /// </para>
    /// </summary>
    internal const string OpeningTrue = "Attempt.UntilTrue(";

    /// <summary>Every way a deadline is opened, matched in the sources exactly as written. WW446.</summary>
    internal static IReadOnlyList<string> Spellings { get; } = new ReadOnlyCollection<string>(
        [Opening, OpeningTyped, OpeningTrue]);

    internal static IReadOnlyList<Deadline> Known { get; } = new ReadOnlyCollection<Deadline>(
    [
        // --- the engine ---------------------------------------------------------------------------
        new("Keyboard.cs", 1, "null, where the control reads back as something other than what was typed"),
        new("Resolve.cs", 1, "null, from a walk that matched no element under the root"),
        new("Traversal.cs", 2, "null, where the focused element is absent or is the one it started on. "
            + "WW446's second is the nudge: false while the slider still reads the range it was on, "
            + "so a key that moved nothing waits out its budget rather than reporting a move"),
        new("Desk.cs", 1, "null, where the automation root cannot be touched or the reading threw"),
        new("Menu.cs", 3, "false while the highlight is where it was — the three are opening a menu, "
            + "walking it and opening a submenu, and each waits for the highlighted entry to become "
            + "something other than the one it started on, which is what says the key landed"),
        new("NotificationArea.cs", 8, "false while the shell's flyout is not readable, or while the "
            + "icon is not in it, or while the menu the icon put up has not arrived — and false the "
            + "other way round for the one that waits for the flyout to go, where it is the overflow "
            + "still being there. The eight are the whole of what this surface has to wait for, "
            + "because the shell draws all of it and answers for none of it"),
        new("Pick.cs", 3, "false while the picker holds no items, while it is still expanded, and "
            + "while the selection is the one the walk had already passed — the last is what stops a "
            + "walk reading the entry it came from as the entry it landed on"),
        new("Selecting.cs", 1, "false while the item does not read as selected, and where a caller "
            + "handed a second condition, while that is not true either"),
        new("Settled.cs", 1, "false while any process this run started is still alive, which is the "
            + "reading Block B's first criterion is about"),
        new("Suite.cs", 1, "false while the launched process owns no top-level window above the floor "
            + "— a launch returns before a window exists, and this is the gap WW374 is inside"),
        new("CaseRun.cs", 5, "false while the reading a step claims has not arrived: the matches "
            + "under a locator, the rows whose headers are wrong, the elements that announce nothing "
            + "and the window a step is about. Each look rebuilds its own list every poll, so what "
            + "makes it false is the tree as it is now rather than what an earlier look kept"),

        // --- the suite ----------------------------------------------------------------------------
        new("AttemptTests.cs", 7, "null on most, and deliberately never on one: a look that is always "
            + "answered is what proves a thing already there costs no sleep"),
        new("FixtureTests.cs", 1, "null, until the fixture has written the dump this is waiting on"),
        new("NotificationAreaTests.cs", 2, "null, while no Menu on the desktop holds this fixture's "
            + "entries — a menu the verb has reported open is a window the tree may not have caught "
            + "up with, and nothing found is what that looks like. WW446's second is false while the "
            + "shell's flyout is not readable, which is the same wait the engine takes and is taken "
            + "here because the case is about what the fixture did to the tray"),
        new("TrayIconFixture.cs", 1, "null, from the search's own Icon — which is the whole of WW175: "
            + "the search itself is never null and waiting on it would poll once"),
        new("Waits.cs", 2, "whatever the caller's look answers, since this only supplies the deadline. "
            + "Both spellings pass through here for the same reason, and neither is this file's to "
            + "explain: what it owns is the number, and what it is waiting for belongs to whoever asked"),
        new("DeadlineTests.cs", 1, "never nothing, on purpose: the one case here that drives the collapse "
            + "this whole catalogue exists because of, so the behaviour is stated and not discovered"),
        new("ThroughoutTests.cs", 1, "false while the window is still where its frame was read from. "
            + "WW465: the case moves it inside the take and then waits for the move to be observable, "
            + "so what the deadline buys is a compositor that has not caught up yet — and spending it "
            + "in full would mean the move did not happen, which fails the case on the line after"),
        new("SlowMachineTests.cs", 2, "the two ends, written as the constants they are: one look is "
            + "always false and one is always true, because what these cases drive is the machinery "
            + "itself — a deadline spent in full and a deadline answered on the first poll"),

        // --- the launch wait, which is one call now -----------------------------------------------
        // WW448. Twelve entries stood here, one per file, each the same look at the same process on
        // the same budget. They are `Fixture.Drew` now, and it waits through `Waits` on the `draw`
        // deadline this suite has declared since WW143 — so the wait is in this list once, under the
        // file that owns it, and the twelve numbers nobody could find are gone.

        // --- the rest of the suite, and the tools ----------------------------------------------------
        new("OwnRenderTests.cs", 1, "false while the application has not said it is answering renders, "
            + "which is the state WW374 is about: a window that is up and a half that is not hooked yet"),
        new("PointerTests.cs", 1, "false while the checkbox still reads Off, so a click that landed "
            + "nowhere spends the budget rather than reading as a click that worked"),
        new("TrayPlacementTests.cs", 2, "false while the shell's flyout is not readable, both times — "
            + "once to open it and once to say it was standing before the fixture added an icon, "
            + "which is how a flyout the fixture never opened is told from one it did"),
        new("Landing.cs", 1, "false while the control reads back what it read before the keys were "
            + "sent, which is what the typing rig is measuring the cost of"),
        new("Program.cs", 1, "false while the fixture's process owns no top-level window — the typing "
            + "rig's own launch, waiting for the window it is about to type into"),
    ]);

    /// <summary>
    /// Every file that opens a deadline, and how many it opens, read out of the sources rather than
    /// out of the list above. Both trees, because a wait in the suite decays exactly as quietly as
    /// one in the engine — and the one that nearly decayed was in the suite.
    /// </summary>
    internal static IReadOnlyList<Deadline> Found() => scanned.Value;

    /// <summary>
    /// Scanned once. Every case here asks the same question of the same tree, and reading two
    /// hundred source files four times inside a suite whose other cases are waiting on 5000ms
    /// deadlines is load this check has no reason to add.
    /// </summary>
    private static readonly Lazy<IReadOnlyList<Deadline>> scanned = new(Scan);

    private static IReadOnlyList<Deadline> Scan()
    {
        var found = new List<Deadline>();

        // WW193. The walk is Checkout's, exclusions included — this is the copy that shipped
        // recursing into bin and obj, and the guest went red twice on timing before anybody looked
        // at the enumeration. The question below is still this catalogue's own.
        foreach (var file in Checkout.Sources(Checkout.Everything, except: $"{nameof(Deadlines)}.cs"))
        {
            // WW202, and this is the one that mattered most: Sleeps was repaired for exactly this a
            // task earlier and its twin was left with it. Nothing was miscounted, which is the
            // point — the next entry explaining itself in prose is what would have broken a count.
            // WW446. Every spelling rather than the one this started with, which is WW198's repair
            // made to this catalogue's twin: a rule matching one way of writing the thing is answered
            // by somebody writing it the other way, and then nothing knows about it at all.
            var waits = File.ReadLines(file)
                .Select(Checkout.Code)
                .Sum(line => Spellings.Sum(one => Occurrences(line, one)));
            if (waits > 0)
                found.Add(new Deadline(Path.GetFileName(file), waits, ""));
        }

        return found.OrderBy(one => one.File, StringComparer.Ordinal).ToList();
    }

    private static int Occurrences(string text, string what)
    {
        var count = 0;
        var at = text.IndexOf(what, StringComparison.Ordinal);
        while (at >= 0)
        {
            count++;
            at = text.IndexOf(what, at + what.Length, StringComparison.Ordinal);
        }

        return count;
    }

}

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

    internal static IReadOnlyList<Deadline> Known { get; } = new ReadOnlyCollection<Deadline>(
    [
        // --- the engine ---------------------------------------------------------------------------
        new("Keyboard.cs", 1, "null, where the control reads back as something other than what was typed"),
        new("Resolve.cs", 1, "null, from a walk that matched no element under the root"),
        new("Traversal.cs", 1, "null, where the focused element is absent or is the one it started on"),
        new("Desk.cs", 1, "null, where the automation root cannot be touched or the reading threw"),

        // --- the suite ----------------------------------------------------------------------------
        new("AttemptTests.cs", 7, "null on most, and deliberately never on one: a look that is always "
            + "answered is what proves a thing already there costs no sleep"),
        new("FixtureTests.cs", 1, "null, until the fixture has written the dump this is waiting on"),
        new("TrayIconFixture.cs", 1, "null, from the search's own Icon — which is the whole of WW175: "
            + "the search itself is never null and waiting on it would poll once"),
        new("Waits.cs", 1, "whatever the caller's look answers, since this only supplies the deadline"),
        new("DeadlineTests.cs", 1, "never nothing, on purpose: the one case here that drives the collapse "
            + "this whole catalogue exists because of, so the behaviour is stated and not discovered"),
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
        foreach (var file in Trees().SelectMany(Sources))
        {
            // This file names the call it is looking for, so counting itself would count the naming.
            if (Path.GetFileName(file) == $"{nameof(Deadlines)}.cs")
                continue;

            var text = File.ReadAllText(file);
            var waits = Occurrences(text, Opening) + Occurrences(text, OpeningTyped);
            if (waits > 0)
                found.Add(new Deadline(Path.GetFileName(file), waits, ""));
        }

        return found.OrderBy(one => one.File, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// The sources under a tree, and never what a build left beside them. Two reasons, and the
    /// second is the one that matters: bin and obj hold thousands of files on a machine that has
    /// built, which is load a check has no business adding to a suite full of deadlines — and they
    /// hold copies, so a wait deleted from a source still standing in obj would be a phantom entry
    /// nobody could find.
    /// </summary>
    private static IEnumerable<string> Sources(string tree) =>
        Directory
            .EnumerateFiles(tree, "*.cs", SearchOption.AllDirectories)
            .Where(one => !one.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(one => !one.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

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

    /// <summary>The engine's sources and the suite's, found from the solution file.</summary>
    private static IReadOnlyList<string> Trees()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Winwright.slnx")))
            directory = directory.Parent;

        Assert.NotNull(directory);
        return
        [
            Path.Combine(directory.FullName, "src"),
            Path.Combine(directory.FullName, "tests"),
        ];
    }
}

using System.Collections.ObjectModel;
using System.Globalization;
using System.Xml.Linq;

namespace Winwright.RollCall;

/// <summary>One case a run wrote down, and whether writing it down meant running it.</summary>
/// <param name="Name">The case, as the results file spells it.</param>
/// <param name="Outcome">What the run called it — Passed, Failed, NotExecuted.</param>
/// <param name="Ran">Whether it executed at all.</param>
public sealed record Recorded(string Name, string Outcome, bool Ran)
{
    /// <summary>The one line a report names it by.</summary>
    public override string ToString() => Ran ? Name : $"{Name} ({Outcome})";
}

/// <summary>
/// Reading the two lists this check compares.
/// <para>
/// Both are read from what the runner already produces, on purpose: a roll call that needed the
/// suite to cooperate would be a roll call the suite could stop cooperating with by dying, which
/// is the case it exists for.
/// </para>
/// </summary>
public static class Readers
{
    /// <summary>
    /// The names in a <c>dotnet test --list-tests</c> listing.
    /// <para>
    /// Matched on shape rather than on the header, because the header is translated and a check
    /// that reads it works on one machine's locale and silently finds nothing on another's. A test
    /// is an indented line whose name, up to the arguments a theory carries, is a dotted path with
    /// no spaces in it; the banner naming the assembly is neither indented nor dotted that way.
    /// </para>
    /// </summary>
    /// <param name="listing">The runner's output, whole.</param>
    public static IReadOnlyList<string> Discovered(string listing)
    {
        ArgumentNullException.ThrowIfNull(listing);

        var names = listing
            .Split('\n')
            .Where(line => line.Length > 0 && char.IsWhiteSpace(line[0]))
            .Select(line => line.Trim())
            .Where(Named)
            .ToList();

        return new ReadOnlyCollection<string>(names);
    }

    /// <summary>
    /// Whether a line is a test rather than prose. What is inside the arguments is not looked at:
    /// a theory writes anything at all in there, spaces and quotation marks included.
    /// </summary>
    private static bool Named(string line)
    {
        var head = Roll.Method(line);
        return head.Length > 0
            && head.Contains('.', StringComparison.Ordinal)
            && !head.Any(char.IsWhiteSpace);
    }

    /// <summary>The same, read from a file the runner wrote.</summary>
    /// <param name="path">The listing file.</param>
    public static IReadOnlyList<string> DiscoveredIn(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Discovered(File.ReadAllText(path));
    }

    /// <summary>
    /// What the runs before this one excused, oldest first, empty where there was no earlier run.
    /// <para>
    /// WW289. Every run writes under its own directory beneath the results root, so the runs before
    /// this one are the sibling directories that carry a ledger. Not an average and not the best —
    /// the counts themselves, which is what "the usual number" means to somebody reading one run.
    /// </para>
    /// <para>
    /// WW298. Several and not one. A desk that is busy for two runs makes the second read as a steady
    /// state, so a single predecessor turns the anomaly into its own baseline exactly where it is
    /// worst. Four counts read as a series need no threshold and hide no repetition.
    /// </para>
    /// <para>
    /// By write time and never by name. A caller may name its own run — the VM runner does — so the
    /// names are not ordered, and sorting them would compare against whichever directory sorts last
    /// rather than whichever ran last.
    /// </para>
    /// <para>
    /// Empty where there is no earlier run at all, which a first run on a fresh checkout always is.
    /// That is a different fact from zero, and reporting it as zero would read a first run as an
    /// improvement on nothing.
    /// </para>
    /// </summary>
    /// <param name="root">The results root every run writes a directory under.</param>
    /// <param name="thisRun">This run's own directory, which is not its own predecessor.</param>
    /// <param name="most">How many of the most recent to read; the default is what the tool uses.</param>
    public static IReadOnlyList<int> ExcusedRecently(string root, string thisRun, int most = Recent) =>
        Series(root, thisRun, most, Excused, one => ExcusedIn(one)?.Count);

    /// <summary>
    /// What the runs before this one discovered, oldest first, empty where there was no earlier run.
    /// <para>
    /// WW299. The roll's own arithmetic is discovered against recorded, and both come from the same
    /// run — so a run where discovery itself fell short is whole by its own measure and says "all
    /// 1,204 discovered cases ran" in the words it uses for 1,807. A class made internal, a deleted
    /// `[Fact]`, a `#if` that stopped matching: each takes both numbers down together.
    /// </para>
    /// <para>
    /// Read off the listing this run's own roll was taken from, which is the same file and the same
    /// parser. A count kept in a format of its own would be a second thing to keep true.
    /// </para>
    /// </summary>
    /// <param name="root">The results root every run writes a directory under.</param>
    /// <param name="thisRun">This run's own directory, which is not its own predecessor.</param>
    /// <param name="most">How many of the most recent to read; the default is what the tool uses.</param>
    public static IReadOnlyList<int> DiscoveredRecently(string root, string thisRun, int most = Recent) =>
        Series(root, thisRun, most, Listing, one => DiscoveredIn(one).Count);

    /// <summary>
    /// One number per earlier run, oldest first, read off the file each run left under the root.
    /// <para>
    /// Shared by the two series so they cannot drift apart on the parts that are the same question:
    /// which directories are earlier runs, which of them left the file, and which four are most
    /// recent. What differs is the file's name and how a count is got out of it.
    /// </para>
    /// </summary>
    private static IReadOnlyList<int> Series(
        string root, string thisRun, int most, string named, Func<string, int?> count)
    {
        var earlier = Kept(root, thisRun, most, named)
            .Select(count)
            .Where(one => one is not null)
            .Select(one => one!.Value)
            .ToList();

        return new ReadOnlyCollection<int>(earlier);
    }

    /// <summary>
    /// The cases every one of the last few runs excused, which is what recurs rather than what
    /// happened once.
    /// <para>
    /// WW248. An excuse that arrives every time is structural — a dialog this process shows takes the
    /// foreground from a fixture launched beside it — and one that arrives once is a desk somebody
    /// else was using. One run cannot tell them apart, and the ledgers of four can.
    /// </para>
    /// <para>
    /// Empty where fewer than two earlier runs were found. One run agreeing with itself is not a
    /// pattern, and reporting it as one would call the first coincidence structural.
    /// </para>
    /// </summary>
    /// <param name="root">The results root every run writes a directory under.</param>
    /// <param name="thisRun">This run's own directory, which is not its own predecessor.</param>
    /// <param name="most">How many of the most recent to read; the default is what the tool uses.</param>
    public static IReadOnlyList<string> ExcusedEveryTime(string root, string thisRun, int most = Recent)
    {
        var ledgers = Kept(root, thisRun, most, Excused);
        if (ledgers.Count < 2)
            return [];

        var everywhere = ledgers
            .Select(one => ExcusedIn(one)?
                .Select(row => Excuse(row).Case)
                .Where(named => named is not null)
                .Select(named => named!)
                .ToHashSet(StringComparer.Ordinal))
            .Where(one => one is not null)
            .Select(one => one!)
            .ToList();

        if (everywhere.Count < 2)
            return [];

        // Intersected and not counted: "in all four" is the claim, so a run missing it ends the
        // matter. A threshold like "three of four" is a number somebody tunes the day it refuses.
        var always = everywhere.Aggregate(
            new HashSet<string>(everywhere[0], StringComparer.Ordinal),
            (standing, one) =>
            {
                standing.IntersectWith(one);
                return standing;
            });

        return new ReadOnlyCollection<string>(always.OrderBy(one => one, StringComparer.Ordinal).ToList());
    }

    /// <summary>
    /// The file each of the last few runs kept under the root, oldest last read first — the part both
    /// series and the recurrence reading ask identically, so they cannot drift apart on it.
    /// </summary>
    private static IReadOnlyList<string> Kept(string root, string thisRun, int most, string named)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(thisRun);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(most);

        if (!Directory.Exists(root))
            return [];

        var mine = Path.GetFullPath(thisRun);

        try
        {
            // Newest first to take from, then reversed: the caller reads them as time runs, and
            // taking the most recent is what the ordering is for. A file that is not there is a run
            // that kept none, and is dropped rather than read as zero.
            return Directory.GetDirectories(root)
                .Where(one => !string.Equals(Path.GetFullPath(one), mine, StringComparison.OrdinalIgnoreCase))
                .Select(one => Path.Combine(one, named))
                .Where(File.Exists)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .Take(most)
                .Reverse()
                .ToList();
        }
        catch (Exception unreadable) when (unreadable is IOException or UnauthorizedAccessException)
        {
            // Unreadable is unknown and never zero, which is the rule the ledger itself is under.
            return [];
        }
    }

    /// <summary>What a run's ledger is called where it is kept beside that run's own results.</summary>
    public const string Excused = "excused.txt";

    /// <summary>What discovery's own output is called where a run keeps it beside its results.</summary>
    public const string Listing = "discovered.txt";

    /// <summary>
    /// How many earlier runs are read to say what this one's count is worth.
    /// <para>
    /// WW298. Four, because the question a reader has is whether this run is the ordinary one, and
    /// four numbers answer it by being read rather than by being tested against a threshold. One is
    /// too few — a desk busy for two runs makes the second read as a steady state — and a long tail
    /// is a sentence nobody finishes.
    /// </para>
    /// </summary>
    public const int Recent = 4;

    /// <summary>
    /// The desk facts a run excused a check for, one per line, or null where nobody wrote them down.
    /// <para>
    /// WW231. Null and empty are different answers and this is the one place that matters: a suite
    /// that excused nothing wrote an empty file, and a suite whose ledger never appeared is a run
    /// whose excuses are unknown. Reporting the second as zero is the reading the whole roll call
    /// exists to refuse, one level up.
    /// </para>
    /// </summary>
    /// <param name="path">The ledger the suite wrote beside its own assembly.</param>
    public static IReadOnlyList<string>? ExcusedIn(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            return null;

        try
        {
            return File.ReadAllLines(path)
                .Select(one => one.Trim())
                .Where(one => one.Length > 0)
                .ToList();
        }
        catch (Exception unreadable) when (unreadable is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// One ledger line split into the desk fact and the case it excused.
    /// <para>
    /// WW233. The name is what makes eleven holes actionable rather than merely counted, and a line
    /// that carries none is read as unnamed rather than refused: a ledger written by an older build,
    /// or by a frame the stack could not answer for, still says how many.
    /// </para>
    /// </summary>
    /// <param name="line">A line as the ledger wrote it.</param>
    public static (string Fact, string? Case, string? Absence, string Kind) Excuse(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        // WW248: three fields, and the third optional for the same reason the second was — a ledger
        // written by an older build still says how many, and still says which case. What the third
        // adds is what the engine said was missing, which after WW245 names both sides of the
        // comparison. A reader of the roll can then tell a desk somebody else was using from this
        // suite's own window standing in front of the one under test, and that difference is what
        // decides whether an excuse is circumstance or structure.
        // WW281: four, and the fourth says which kind of thing was not met — a fact about the desk,
        // or a budget this suite chose. It is last because this reader was written to tolerate a row
        // an older build wrote, and every one of those is a desk row: a missing kind is not unknown
        // here, it is the answer.
        // WW248: five, and the fifth says whether the case has written down that it means the excuse
        // it makes. Last, like the two before it, and absent on every row an older build wrote —
        // where a missing kind was the answer, a missing account is genuinely unknown, so a run with
        // no fifth column anywhere refuses nothing rather than refusing everything.
        var apart = line.Split('\t', 5);
        return (
            apart[0].Trim(),
            apart.Length > 1 && apart[1].Trim().Length > 0 ? apart[1].Trim() : null,
            apart.Length > 2 && apart[2].Trim().Length > 0 ? apart[2].Trim() : null,
            apart.Length > 3 ? Kind(apart[3]) : Desk);
    }

    /// <summary>What the account column says for a case that has written down why it means it.</summary>
    public const string Meant = "Meant";

    /// <summary>
    /// Whether this row says the case means the excuse it made.
    /// <para>
    /// WW248. Read as its own question rather than folded into <see cref="Excuse" />, because the
    /// answer has three values and that tuple has room for two: accounted, not accounted, and a
    /// ledger that does not carry the column at all. The third is what every run before this change
    /// wrote, and reporting it as <em>not accounted</em> would make the first run after it refuse
    /// every excuse in the history it is comparing against.
    /// </para>
    /// </summary>
    /// <param name="line">One row of the ledger.</param>
    /// <returns>True or false where the row says, and null where it does not carry the column.</returns>
    public static bool? Accounted(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var apart = line.Split('\t', 5);
        return apart.Length > 4 ? string.Equals(apart[4].Trim(), Meant, StringComparison.Ordinal) : null;
    }

    /// <summary>What the kind column says for a check the desk excused, and for every row without one.</summary>
    public const string Desk = "Desk";

    /// <summary>What it says for a budget this suite chose and could not meet.</summary>
    public const string Budget = "Budget";

    /// <summary>
    /// Which kind a row says it is, out of the two there are.
    /// <para>
    /// A closed set and not the field as written, because this one is last: the split stops at four
    /// so the earlier fields cannot run into each other, which leaves anything after the fourth tab
    /// sitting inside the kind. Naming the two is also what stops a row from a newer build inventing
    /// a category this reader would then count under its own name.
    /// </para>
    /// <para>
    /// Unrecognised reads as the desk's, which is the same rule <see cref="NeverRan" /> follows one
    /// level down: the safe reading of a row this tool does not understand is the one that keeps it
    /// reporting rather than excusing.
    /// </para>
    /// </summary>
    /// <param name="said">The fourth field, as the ledger wrote it.</param>
    private static string Kind(string said) =>
        said.Trim().Equals(Budget, StringComparison.Ordinal) ? Budget : Desk;


    /// <summary>
    /// The outcomes that mean a case was written down and never executed.
    /// <para>
    /// WW137. A deliberate skip, or a case the runner listed and then abandoned. Both are recorded
    /// and neither ran, and counting them as answers is the founding defect one level in: a run
    /// where every name is present and twenty-two say NotExecuted reads as a pass for exactly the
    /// reason 352 of 374 did — a number that agrees, and nobody asking what it is a number of.
    /// </para>
    /// <para>
    /// Named rather than derived from a list of the ones that did run: an outcome this reader has
    /// never heard of counts as having run, which keeps it reporting rather than excusing.
    /// </para>
    /// </summary>
    private static readonly HashSet<string> NeverRan = new(StringComparer.OrdinalIgnoreCase)
    {
        "NotExecuted",
        "NotRunnable",
        "Pending",
        "Disconnected",
    };

    /// <summary>
    /// Every case a run recorded, in the order they finished, each saying whether it ran.
    /// <para>
    /// Ordered by when each ended rather than by where it sits in the file: what the roll wants
    /// from this list is which name answered last, and the document's order is the runner's
    /// business rather than the run's.
    /// </para>
    /// </summary>
    /// <param name="trx">The results file.</param>
    /// <exception cref="InvalidDataException">Where the file is not a results document.</exception>
    public static IReadOnlyList<Recorded> RecordedIn(string trx)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trx);

        XDocument document;
        try
        {
            document = XDocument.Load(Path.GetFullPath(trx.Trim()));
        }
        catch (System.Xml.XmlException unreadable)
        {
            // A results file truncated by the same crash this check is about is the ordinary case
            // here, not an exceptional one, so it is named rather than rethrown as a parser error.
            throw new InvalidDataException(
                $"{trx} is not readable as a results file, which is what a run killed while writing one leaves: "
                + unreadable.Message,
                unreadable);
        }

        var results = document
            .Descendants()
            .Where(node => node.Name.LocalName == "UnitTestResult")
            .Select(node => (
                Name: (string?)node.Attribute("testName"),
                Outcome: (string?)node.Attribute("outcome") ?? "",
                Ended: Moment((string?)node.Attribute("endTime"))))
            .Where(one => !string.IsNullOrWhiteSpace(one.Name))
            .OrderBy(one => one.Ended)
            .Select(one => new Recorded(one.Name!.Trim(), one.Outcome.Trim(), !NeverRan.Contains(one.Outcome.Trim())))
            .ToList();

        return new ReadOnlyCollection<Recorded>(results);
    }

    private static DateTimeOffset Moment(string? written) =>
        DateTimeOffset.TryParse(written, CultureInfo.InvariantCulture, DateTimeStyles.None, out var when)
            ? when
            : DateTimeOffset.MinValue;
}

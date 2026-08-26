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

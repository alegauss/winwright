using System.Collections.ObjectModel;
using System.Globalization;
using System.Xml.Linq;

namespace Winwright.RollCall;

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
    /// The names a run recorded a result for, in the order they finished.
    /// <para>
    /// Ordered by when each ended rather than by where it sits in the file: what the roll wants
    /// from this list is which name answered last, and the document's order is the runner's
    /// business rather than the run's.
    /// </para>
    /// </summary>
    /// <param name="trx">The results file.</param>
    /// <exception cref="InvalidDataException">Where the file is not a results document.</exception>
    public static IReadOnlyList<string> AnsweredIn(string trx)
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
                Ended: Moment((string?)node.Attribute("endTime"))))
            .Where(one => !string.IsNullOrWhiteSpace(one.Name))
            .OrderBy(one => one.Ended)
            .Select(one => one.Name!.Trim())
            .ToList();

        return new ReadOnlyCollection<string>(results);
    }

    private static DateTimeOffset Moment(string? written) =>
        DateTimeOffset.TryParse(written, CultureInfo.InvariantCulture, DateTimeStyles.None, out var when)
            ? when
            : DateTimeOffset.MinValue;
}

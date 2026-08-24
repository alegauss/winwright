using System.Collections.ObjectModel;
using System.Reflection;

namespace Winwright.Verdicts;

/// <summary>Whose a hole is — which is what decides what the reader does about it.</summary>
public enum Whose
{
    /// <summary>
    /// The desk's. The machine could have been arranged differently: who holds the foreground, what
    /// is standing over a rectangle, whether the shell answers. Nothing to fix in any repository.
    /// </summary>
    Desk,

    /// <summary>
    /// The thing under test's. A stale binary, a page still computing, an application in the wrong
    /// language. A repository to open, and re-running changes nothing.
    /// </summary>
    UnderTest,

    /// <summary>
    /// Neither, because nobody has said. The condition was composed at the throw site, or it is one
    /// this engine declares and <see cref="DeskFacts" /> has never classified — or the hole carries
    /// no condition at all, which is worse than a failure and is counted here rather than rounded
    /// into one of the other two.
    /// </summary>
    Unclassified,
}

/// <summary>
/// How a run's holes divided, with the third bucket kept.
/// </summary>
/// <param name="Desk">Holes about the machine.</param>
/// <param name="UnderTest">Holes about the thing being tested.</param>
/// <param name="Unclassified">Holes nobody has classified, counted rather than assigned.</param>
public sealed record HoleDivision(int Desk, int UnderTest, int Unclassified)
{
    /// <summary>How many holes this divided.</summary>
    public int Total => Desk + UnderTest + Unclassified;

    /// <summary>
    /// The clause a headline carries. One kind reads as <em>all</em>, because "3 the desk's" beside
    /// "3 unchecked" is the same number twice and a reader has to check they are the same number.
    /// </summary>
    public string Sentence()
    {
        if (Total == 0)
            return "none";

        var parts = new List<string>();
        if (Desk > 0)
            parts.Add($"{Desk} {Holes.Worded(Whose.Desk)}");
        if (UnderTest > 0)
            parts.Add($"{UnderTest} {Holes.Worded(Whose.UnderTest)}");
        if (Unclassified > 0)
            parts.Add($"{Unclassified} {Holes.Worded(Whose.Unclassified)}");

        if (parts.Count > 1)
            return string.Join(", ", parts);

        if (Desk > 0)
            return $"all {Holes.Worded(Whose.Desk)}";

        return UnderTest > 0
            ? $"all {Holes.Worded(Whose.UnderTest)}"
            : $"all {Holes.Worded(Whose.Unclassified)}";
    }
}

/// <summary>
/// Whose each hole in a run was.
/// <para>
/// WW192. WW183 wrote this engine's judgement down — which of its conditions are the desk's, and
/// why each one is — and only the suite ever read it. A summary said three assertions never ran and
/// named the absent condition beside each, which is everything except the thing that decides what
/// happens next. Three holes because a foreground was not granted and a window stood over a capture
/// is a machine to clear. Three holes because a binary was stale and a page was still computing is
/// a repository to open. The exit code is 2 either way.
/// </para>
/// <para>
/// The third bucket is not politeness. A condition composed at a throw site is in no catalogue and
/// answers to nothing — WW190 found one of those — and a hole carrying no condition at all is the
/// shape <c>BusyDesk</c> calls worse than a failure. Rounding either into "the desk's" would excuse
/// a defect, and rounding it into "this run's" would send a reader to the wrong repository. Counted,
/// and named in the line, so somebody can go and classify it.
/// </para>
/// </summary>
public static class Holes
{
    /// <summary>How each kind is spelled wherever it is printed.</summary>
    public static string Worded(Whose whose) => whose switch
    {
        Whose.Desk => "the desk's",
        Whose.UnderTest => "this run's",
        _ => "unclassified",
    };

    /// <summary>
    /// Every condition this engine declares, read off its own assembly rather than off a list.
    /// <para>
    /// Two spellings, because the engine has two: a constant named for the precondition it is, and
    /// <c>Named</c> where the reading it belongs to reads better that way. A name nothing here
    /// declares is one somebody composed at the call, and that is the finding rather than an
    /// oversight to be forgiven.
    /// </para>
    /// </summary>
    public static IReadOnlyList<string> Declared { get; } = new ReadOnlyCollection<string>(
        typeof(Precondition).Assembly
            .GetExportedTypes()
            .SelectMany(one => one.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(one => one.IsLiteral && !one.IsInitOnly && one.FieldType == typeof(string))
            .Where(one => one.Name.Contains("PreconditionName", StringComparison.Ordinal)
                || string.Equals(one.Name, "Named", StringComparison.Ordinal))
            .Select(one => one.GetRawConstantValue() as string ?? "")
            .Where(one => one.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(one => one, StringComparer.Ordinal)
            .ToList());

    /// <summary>
    /// Whose one absent condition is.
    /// <para>
    /// Null is answered rather than refused, and it is worth saying why it cannot arrive from a hole
    /// this engine built: <see cref="AssertionResult.Unchecked" /> refuses one. The parameter is
    /// nullable because <see cref="AssertionResult.Missing" /> is, and a caller holding a result it
    /// did not build gets an answer instead of a throw.
    /// </para>
    /// </summary>
    /// <param name="missing">The condition, or null where nothing named one.</param>
    public static Whose Of(Precondition? missing)
    {
        if (missing is null || string.IsNullOrWhiteSpace(missing.Name))
            return Whose.Unclassified;

        if (DeskFacts.Names(missing.Name))
            return Whose.Desk;

        // Declared and not the desk's is a fact about the thing under test — which is the judgement
        // DeskFacts already makes and states, one call away rather than repeated here.
        return Declared.Contains(missing.Name.Trim(), StringComparer.Ordinal)
            ? Whose.UnderTest
            : Whose.Unclassified;
    }

    /// <summary>Whose one hole is. A result that ran is not a hole and is refused.</summary>
    /// <param name="hole">The assertion that never ran.</param>
    /// <exception cref="ArgumentException">Where it did run, either way.</exception>
    public static Whose Of(AssertionResult hole)
    {
        ArgumentNullException.ThrowIfNull(hole);

        if (hole.Outcome != AssertionOutcome.Unchecked)
        {
            throw new ArgumentException(
                $"'{hole.Name}' {hole.Outcome.ToString().ToLowerInvariant()}, and an assertion that ran "
                    + "is not a hole anybody has to apportion",
                nameof(hole));
        }

        return Of(hole.Missing);
    }

    /// <summary>How a run's holes divide. Anything that ran is passed over rather than refused.</summary>
    /// <param name="results">Every result, or only the holes — either is answered the same way.</param>
    public static HoleDivision Divide(IEnumerable<AssertionResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var desk = 0;
        var underTest = 0;
        var unclassified = 0;

        foreach (var hole in results.Where(one => one.Outcome == AssertionOutcome.Unchecked))
        {
            switch (Of(hole.Missing))
            {
                case Whose.Desk:
                    desk++;
                    break;
                case Whose.UnderTest:
                    underTest++;
                    break;
                default:
                    unclassified++;
                    break;
            }
        }

        return new HoleDivision(desk, underTest, unclassified);
    }
}

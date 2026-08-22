using System.Collections.ObjectModel;

namespace Winwright.RollCall;

/// <summary>One test method, with how many of its cases were found and how many answered.</summary>
/// <param name="Method">The test, without its arguments.</param>
/// <param name="Discovered">How many cases discovery found.</param>
/// <param name="Answered">How many the run recorded a result for.</param>
public sealed record Attendance(string Method, int Discovered, int Answered)
{
    /// <summary>How many were found and never answered. Negative where more answered than were found.</summary>
    public int Absent => Discovered - Answered;

    /// <summary>The one line a report names it by.</summary>
    public override string ToString() => Answered == 0
        ? $"{Method} never ran ({Cases(Discovered)})"
        : Discovered == 0
            ? $"{Method} answered {Cases(Answered)} that discovery never found"
            : $"{Method} ran {Answered} of {Cases(Discovered)}";

    private static string Cases(int many) => $"{many} case{(many == 1 ? "" : "s")}";
}

/// <summary>
/// Who was discovered against who answered.
/// <para>
/// Measured while building WW39. A test declared a sixteen-byte RECT as an eight-byte long, so the
/// call corrupted the stack and the host died partway through an unrelated class. The runner
/// printed a pass with no failures and a total of 352 where the run before it had 374 — twenty-two
/// tests gone, and the only sign was a number nobody had a reason to read.
/// </para>
/// <para>
/// That is the defect this project was started over, in the suite that is supposed to prove the
/// project does not have it. A green covering tests that never ran is worth nothing, and a green
/// covering tests that never ran <em>in the harness proving greens do not do that</em> is worth
/// less than nothing.
/// </para>
/// <para>
/// Counted per method rather than matched name for name, and that is a measurement rather than a
/// preference: a theory's arguments are rendered by two different tools here, and the results file
/// writes an emoji as an escape where the listing writes the character. Comparing those texts is
/// comparing two spellings of the same test. Comparing how many cases each method was found with
/// against how many answered is exact, and loses nothing — a run that drops one row of a theory is
/// a method that answered three of four.
/// </para>
/// <para>
/// Nothing here diagnoses the crash. A fatal error has no managed stack to read, and the last name
/// that answered is the whole of what can honestly be said about where it stopped. What this
/// refuses to do is call the result a pass.
/// </para>
/// </summary>
public sealed record Roll
{
    private Roll(
        IReadOnlyList<string> discovered,
        IReadOnlyList<string> answered,
        IReadOnlyList<Attendance> missing,
        IReadOnlyList<Attendance> unexpected,
        string? lastAnswered)
    {
        Discovered = discovered;
        Answered = answered;
        Missing = missing;
        Unexpected = unexpected;
        LastAnswered = lastAnswered;
    }

    /// <summary>Every case discovery found, in the order it found them.</summary>
    public IReadOnlyList<string> Discovered { get; }

    /// <summary>Every case the run recorded a result for, in the order they finished.</summary>
    public IReadOnlyList<string> Answered { get; }

    /// <summary>The methods that answered with fewer cases than were found, in discovery order.</summary>
    public IReadOnlyList<Attendance> Missing { get; }

    /// <summary>The methods that answered with more cases than were found, or with none found at all.</summary>
    public IReadOnlyList<Attendance> Unexpected { get; }

    /// <summary>
    /// The last case the run recorded, which is as near as anything gets to where it stopped.
    /// Null where nothing answered at all.
    /// </summary>
    public string? LastAnswered { get; }

    /// <summary>How many cases were found and never answered.</summary>
    public int Absent => Missing.Sum(one => one.Absent);

    /// <summary>Whether everybody who was discovered answered.</summary>
    public bool Complete => Missing.Count == 0;

    /// <summary>Whether this is a run that may be called a pass at all.</summary>
    public bool Whole => Complete && Unexpected.Count == 0 && Discovered.Count > 0;

    /// <summary>
    /// Take the roll.
    /// <para>
    /// A method the run recorded and discovery never found is neither counted as missing nor
    /// ignored: it is a disagreement about what the suite contains, and it goes in the sentence,
    /// because a reconciliation that only looks one way is half a check.
    /// </para>
    /// </summary>
    /// <param name="discovered">The cases discovery reported.</param>
    /// <param name="answered">The cases the run recorded a result for.</param>
    public static Roll Of(IEnumerable<string> discovered, IEnumerable<string> answered)
    {
        ArgumentNullException.ThrowIfNull(discovered);
        ArgumentNullException.ThrowIfNull(answered);

        var found = Named(discovered);
        var ran = Named(answered);

        var foundBy = Counted(found);
        var ranBy = Counted(ran);

        var missing = new List<Attendance>();
        foreach (var method in found.Select(Method).Distinct(StringComparer.Ordinal))
        {
            var was = foundBy[method];
            var came = ranBy.GetValueOrDefault(method);
            if (came < was)
                missing.Add(new Attendance(method, was, came));
        }

        var unexpected = ranBy
            .Where(one => one.Value > foundBy.GetValueOrDefault(one.Key))
            .Select(one => new Attendance(one.Key, foundBy.GetValueOrDefault(one.Key), one.Value))
            .OrderBy(one => one.Method, StringComparer.Ordinal)
            .ToList();

        return new Roll(
            found,
            ran,
            new ReadOnlyCollection<Attendance>(missing),
            new ReadOnlyCollection<Attendance>(unexpected),
            ran.Count == 0 ? null : ran[^1]);
    }

    /// <summary>What the roll found, in the one sentence a reader skims.</summary>
    public string Sentence()
    {
        if (Discovered.Count == 0)
            return "discovery found no test at all, which is not a suite that passed.";

        if (Whole)
            return $"all {Discovered.Count} discovered cases answered.";

        var parts = new List<string>();
        if (Missing.Count > 0)
        {
            parts.Add(
                $"{Absent} of {Discovered.Count} never ran"
                + (LastAnswered is null ? ", and nothing ran at all" : $", the last to answer being {LastAnswered}"));
        }

        if (Unexpected.Count > 0)
            parts.Add($"{Unexpected.Count} method(s) answered with cases discovery never found");

        return string.Join("; ", parts) + ".";
    }

    /// <summary>The whole reading: the sentence, then the methods, bounded so a wipeout is readable.</summary>
    /// <param name="most">How many methods to list before saying how many were cut.</param>
    public IReadOnlyList<string> Render(int most = 25)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(most);

        var lines = new List<string> { Sentence() };
        lines.AddRange(Listed(Missing, most));
        lines.AddRange(Listed(Unexpected, most));
        return new ReadOnlyCollection<string>(lines);
    }

    /// <summary>The reading as a block of text.</summary>
    public override string ToString() => string.Join('\n', Render());

    /// <summary>One case's method, which is its name without the arguments a theory carries.</summary>
    /// <param name="name">The case, as either tool spells it.</param>
    public static string Method(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var arguments = name.IndexOf('(', StringComparison.Ordinal);
        return (arguments < 0 ? name : name[..arguments]).Trim();
    }

    private static IEnumerable<string> Listed(IReadOnlyList<Attendance> methods, int most)
    {
        if (methods.Count == 0)
            yield break;

        foreach (var method in methods.Take(most))
            yield return $"  {method}";

        if (methods.Count > most)
            yield return $"  ... and {methods.Count - most} more";
    }

    private static Dictionary<string, int> Counted(IEnumerable<string> names)
    {
        var by = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var name in names)
            by[Method(name)] = by.GetValueOrDefault(Method(name)) + 1;

        return by;
    }

    private static IReadOnlyList<string> Named(IEnumerable<string> names) =>
        new ReadOnlyCollection<string>(names
            .Where(one => !string.IsNullOrWhiteSpace(one))
            .Select(one => one.Trim())
            .ToList());
}

using System.Collections.ObjectModel;
using System.Globalization;

namespace Winwright.RollCall;

/// <summary>One test method, with how many of its cases were found, ran, and were only written down.</summary>
/// <param name="Method">The test, without its arguments.</param>
/// <param name="Discovered">How many cases discovery found.</param>
/// <param name="Ran">How many actually executed.</param>
/// <param name="Skipped">
/// How many the run recorded and never executed. WW137 - a recorded skip and an executed pass are
/// different facts, and a check that adds them is the check being replaced.
/// </param>
public sealed record Attendance(string Method, int Discovered, int Ran, int Skipped = 0)
{
    /// <summary>How many were found and never written down at all, which is a run that lost them.</summary>
    public int Absent => Math.Max(0, Discovered - Ran - Skipped);

    /// <summary>How many answers this method owes, whichever way it failed to give them.</summary>
    public int Short => Absent + Skipped;

    /// <summary>The one line a report names it by.</summary>
    public override string ToString()
    {
        if (Discovered == 0)
            return $"{Method} answered {Cases(Ran + Skipped)} that discovery never found";

        var kept = Skipped == 0 ? "" : $", {Skipped} recorded without running";
        return Ran == 0 && Skipped == 0
            ? $"{Method} never ran ({Cases(Discovered)})"
            : $"{Method} ran {Ran} of {Cases(Discovered)}{kept}";
    }

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
/// WW137: and it counts answers rather than names. A results file records an outcome for each case
/// and NotExecuted is among them — a deliberate skip, or one the runner listed and abandoned. Both
/// are recorded and neither ran, so counting them as answers would let a run where every name is
/// present and twenty-two say NotExecuted read as whole, for exactly the reason 352 of 374 did.
/// They are kept on their own line: a recorded skip and an executed pass are different facts.
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
        IReadOnlyList<Recorded> recorded,
        IReadOnlyList<Attendance> missing,
        IReadOnlyList<Attendance> skipping,
        IReadOnlyList<Attendance> unexpected,
        string? lastAnswered,
        IReadOnlyList<string>? excused,
        bool asked,
        IReadOnlyList<int> recent,
        IReadOnlyList<int> discovering,
        bool comparing)
    {
        Discovered = discovered;
        Recorded = recorded;
        Missing = missing;
        Skipping = skipping;
        Unexpected = unexpected;
        LastAnswered = lastAnswered;
        Excused = excused;
        Asked = asked;
        Recent = recent;
        Found = discovering;
        Comparing = comparing;
    }

    /// <summary>
    /// Whether anybody asked how this run's excuses compare with the run before it.
    /// <para>
    /// WW289. The same three states <see cref="Asked"/> keeps, and for the same reason: a caller that
    /// never asked is silent, one that asked on a machine with no earlier run is told there is nothing
    /// to compare with, and one that found a run gets the number. Collapsing the middle into zero
    /// would report a first run as an improvement on nothing.
    /// </para>
    /// </summary>
    public bool Comparing { get; }

    /// <summary>
    /// What the runs before this one excused, oldest first, empty where there was no earlier run.
    /// <para>
    /// WW289. Measured: a guest run passed having excused 49 checks where every run before it excused
    /// 8, because a notification toast held the foreground. Every one of the 49 was printed and the
    /// run still read exactly like one that checked them all — the count was honest and nothing
    /// compared it with anything.
    /// </para>
    /// <para>
    /// WW298. Several and not one, because one predecessor is a difference and not a baseline. A
    /// notification toast is not a thing that appears for a single run and leaves: where the desk
    /// stays busy for two, the second reads 43 against 43 and says nothing changed, so the anomaly
    /// becomes its own baseline exactly when it is worst. Several counts said as they are need no
    /// threshold to tune and promote no run to normal by repeating.
    /// </para>
    /// </summary>
    public IReadOnlyList<int> Recent { get; }

    /// <summary>
    /// What the runs before this one discovered, oldest first, empty where there was no earlier run.
    /// <para>
    /// WW299. Discovery is the one number the roll never checked against anything but itself: it is
    /// weighed against what the run recorded, and both fall together where a class stops loading or
    /// a `[Fact]` goes with the method it annotated. A run that found 1,204 of 1,807 then says all
    /// 1,204 discovered cases ran, in the words a whole run uses.
    /// </para>
    /// </summary>
    public IReadOnlyList<int> Found { get; }

    /// <summary>
    /// Whether anybody asked what the run excused. Three states and not two: a caller that never
    /// asked is silent, a caller that asked and found no ledger is told it is unknown, and a caller
    /// that read one gets the count. Collapsing the first two would put "not read" on the end of
    /// every sentence in this tool's own tests, which is how a clause stops being read.
    /// </summary>
    public bool Asked { get; }

    /// <summary>
    /// Every desk fact a check was excused for, or null where nobody wrote them down.
    /// <para>
    /// WW231. Null is not zero. A suite that excused nothing wrote an empty ledger; a run whose
    /// ledger never appeared has excuses nobody counted, and calling that zero is the same reading
    /// this whole tool exists to withdraw.
    /// </para>
    /// </summary>
    public IReadOnlyList<string>? Excused { get; }

    /// <summary>How many checks the desk was excused for, or null where that was not read.</summary>
    public int? Holes => Excused?.Count;

    /// <summary>Every case discovery found, in the order it found them.</summary>
    public IReadOnlyList<string> Discovered { get; }

    /// <summary>Every case the run wrote down, in the order they finished, ran or not.</summary>
    public IReadOnlyList<Recorded> Recorded { get; }

    /// <summary>Every case that actually executed.</summary>
    public IReadOnlyList<Recorded> Answered => new ReadOnlyCollection<Recorded>(
        Recorded.Where(one => one.Ran).ToList());

    /// <summary>The methods with cases discovery found and the run never wrote down.</summary>
    public IReadOnlyList<Attendance> Missing { get; }

    /// <summary>
    /// The methods with cases the run wrote down and never executed. Their own list, because a
    /// skip and a lost host are different things and a reader's next move differs for each.
    /// </summary>
    public IReadOnlyList<Attendance> Skipping { get; }

    /// <summary>The methods that answered with more cases than were found, or with none found at all.</summary>
    public IReadOnlyList<Attendance> Unexpected { get; }

    /// <summary>
    /// The last case the run recorded, which is as near as anything gets to where it stopped.
    /// Null where nothing answered at all.
    /// </summary>
    public string? LastAnswered { get; }

    /// <summary>How many cases were found and never written down.</summary>
    public int Absent => Missing.Sum(one => one.Absent);

    /// <summary>How many were written down and never executed.</summary>
    public int Skipped => Skipping.Sum(one => one.Skipped);

    /// <summary>Whether everybody who was discovered was written down.</summary>
    public bool Complete => Missing.Count == 0;

    /// <summary>Whether this is a run that may be called a pass at all.</summary>
    public bool Whole =>
        Complete && Skipping.Count == 0 && Unexpected.Count == 0 && Discovered.Count > 0;

    /// <summary>
    /// Take the roll.
    /// <para>
    /// A method the run recorded and discovery never found is neither counted as missing nor
    /// ignored: it is a disagreement about what the suite contains, and it goes in the sentence,
    /// because a reconciliation that only looks one way is half a check.
    /// </para>
    /// </summary>
    /// <param name="discovered">The cases discovery reported.</param>
    /// <param name="recorded">The cases the run wrote down, each saying whether it ran.</param>
    /// <remarks>
    /// This overload asks nothing about what the run excused, and says nothing about it. WW231: the
    /// excuses are the third number in this arithmetic and the only one nobody was told, and the
    /// overload below is how a caller asks. Neither ever makes a run red — a hole is not a failure —
    /// but a green covering 1,551 of 1,563 checks has to say so.
    /// </remarks>
    public static Roll Of(IEnumerable<string> discovered, IEnumerable<Recorded> recorded) =>
        Taken(discovered, recorded, null, asked: false, recent: [], discovering: [], comparing: false);

    /// <inheritdoc cref="Of(IEnumerable{string}, IEnumerable{Recorded})" />
    /// <param name="discovered">The cases discovery reported.</param>
    /// <param name="recorded">The cases the run wrote down.</param>
    /// <param name="excused">
    /// What the run excused, or null where the ledger was not there. Calling this overload is the
    /// asking, so null here is <em>unknown</em> and never <em>none</em>.
    /// </param>
    public static Roll Of(
        IEnumerable<string> discovered, IEnumerable<Recorded> recorded, IEnumerable<string>? excused) =>
        Taken(discovered, recorded, excused, asked: true, recent: [], discovering: [], comparing: false);

    /// <inheritdoc cref="Of(IEnumerable{string}, IEnumerable{Recorded})" />
    /// <param name="discovered">The cases discovery reported.</param>
    /// <param name="recorded">The cases the run wrote down.</param>
    /// <param name="excused">What the run excused, or null where the ledger was not there.</param>
    /// <param name="recent">
    /// What the runs before this one excused, oldest first, empty where there was no earlier run.
    /// WW289: calling this overload is the asking, so empty here is <em>there was none</em> and never
    /// zero. WW298: several rather than one, so a desk that stays busy cannot become its own baseline.
    /// </param>
    /// <param name="discovering">
    /// What the runs before this one discovered, oldest first. WW299: said only where this run found
    /// a different number, so a suite that quietly stopped loading a class cannot read as whole.
    /// </param>
    public static Roll Of(
        IEnumerable<string> discovered,
        IEnumerable<Recorded> recorded,
        IEnumerable<string>? excused,
        IEnumerable<int> recent,
        IEnumerable<int>? discovering = null) =>
        Taken(discovered, recorded, excused, asked: true, recent, discovering ?? [], comparing: true);

    private static Roll Taken(
        IEnumerable<string> discovered,
        IEnumerable<Recorded> recorded,
        IEnumerable<string>? excused,
        bool asked,
        IEnumerable<int> recent,
        IEnumerable<int> discovering,
        bool comparing)
    {
        ArgumentNullException.ThrowIfNull(discovered);
        ArgumentNullException.ThrowIfNull(recorded);
        ArgumentNullException.ThrowIfNull(recent);
        ArgumentNullException.ThrowIfNull(discovering);

        var found = Named(discovered);
        var written = recorded
            .Where(one => one is not null && !string.IsNullOrWhiteSpace(one.Name))
            .Select(one => one with { Name = one.Name.Trim() })
            .ToList();

        var foundBy = Counted(found);
        var ranBy = Counted(written.Where(one => one.Ran).Select(one => one.Name));
        var skippedBy = Counted(written.Where(one => !one.Ran).Select(one => one.Name));

        var missing = new List<Attendance>();
        var skipping = new List<Attendance>();
        foreach (var method in found.Select(Method).Distinct(StringComparer.Ordinal))
        {
            var was = foundBy[method];
            var came = ranBy.GetValueOrDefault(method);
            var kept = skippedBy.GetValueOrDefault(method);
            var attendance = new Attendance(method, was, came, kept);

            if (attendance.Absent > 0)
                missing.Add(attendance);

            if (kept > 0)
                skipping.Add(attendance);
        }

        var everyMethod = ranBy.Keys.Concat(skippedBy.Keys).Distinct(StringComparer.Ordinal);
        var unexpected = everyMethod
            .Select(one => new Attendance(
                one, foundBy.GetValueOrDefault(one), ranBy.GetValueOrDefault(one), skippedBy.GetValueOrDefault(one)))
            .Where(one => one.Ran + one.Skipped > one.Discovered)
            .OrderBy(one => one.Method, StringComparer.Ordinal)
            .ToList();

        var answered = written.Where(one => one.Ran).ToList();

        return new Roll(
            found,
            new ReadOnlyCollection<Recorded>(written),
            new ReadOnlyCollection<Attendance>(missing),
            new ReadOnlyCollection<Attendance>(skipping),
            new ReadOnlyCollection<Attendance>(unexpected),
            answered.Count == 0 ? null : answered[^1].Name,
            excused is null ? null : new ReadOnlyCollection<string>(excused.Where(one => !string.IsNullOrWhiteSpace(one)).ToList()),
            asked,
            new ReadOnlyCollection<int>(recent.ToList()),
            new ReadOnlyCollection<int>(discovering.ToList()),
            comparing);
    }

    /// <summary>What the roll found, in the one sentence a reader skims.</summary>
    public string Sentence()
    {
        if (Discovered.Count == 0)
            return "discovery found no test at all, which is not a suite that passed.";

        // WW231. The excuses ride on the sentence and never on the verdict: a hole is not a failure,
        // and a roll that went red over one would have every desk-dependent case turned off inside a
        // week. What it changes is the claim — "all 1,563 ran" is false of a run where twelve of them
        // looked at the desk and left, and that sentence was the only thing anybody read.
        if (Whole)
            return $"all {Discovered.Count} discovered cases ran{Finding()}{Excusing()}.";

        var parts = new List<string>();
        if (Missing.Count > 0)
        {
            parts.Add(
                $"{Absent} of {Discovered.Count} were never recorded at all"
                + (LastAnswered is null ? ", and nothing ran at all" : $", the last to answer being {LastAnswered}"));
        }

        // Its own clause and never added to the one above: a recorded skip and a lost host are
        // different facts, and the reader's next move differs for each.
        if (Skipping.Count > 0)
            parts.Add($"{Skipped} of {Discovered.Count} were recorded and never ran");

        if (Unexpected.Count > 0)
            parts.Add($"{Unexpected.Count} method(s) answered with cases discovery never found");

        return string.Join("; ", parts) + Excusing() + ".";
    }

    /// <summary>
    /// What the runs before this one discovered, said only where this run found a different number.
    /// <para>
    /// WW299. Unlike the excuses, which move with the desk and need a baseline to be read at all,
    /// discovery is meant to hold still — so the informative event is the change, and a series
    /// printed beside every run would be a clause nobody finishes reading. Said on a rise as well as
    /// a fall: cases landing is how a reader sees the suite grow, and a rule that only ever reports
    /// bad news is one that gets read as noise.
    /// </para>
    /// <para>
    /// Silent where nothing was asked or no earlier run was found, and never "unchanged": the run
    /// before is the only claim this makes, and it is made by naming the numbers.
    /// </para>
    /// </summary>
    private string Finding()
    {
        if (Found.Count == 0 || Found[^1] == Discovered.Count)
            return "";

        var all = Found.Count == 1
            ? Found[0].ToString(CultureInfo.InvariantCulture)
            : string.Join(", ", Found.Take(Found.Count - 1)) + " and " + Found[^1];

        return Found.Count == 1
            ? $", where the run before discovered {all}"
            : $", where the {Found.Count} runs before it discovered {all}";
    }

    /// <summary>
    /// What this run's count is worth beside the one before it, or nothing where nobody asked.
    /// <para>
    /// WW289. The number that matters is not 49 but 49-against-8: a reader who is told only the first
    /// has no way to know whether this run is the ordinary one. Said next to the count rather than
    /// after the conditions, because it changes how much the whole sentence is worth.
    /// </para>
    /// </summary>
    private string Against()
    {
        if (!Comparing)
            return "";

        if (Recent.Count == 0)
            return " and no earlier run was there to compare with";

        // One reads as a comparison and several read as a series, and the two want different words:
        // "against 8 the run before" says what changed, where "8, 43, 8 and 8" says what is usual.
        if (Recent.Count == 1)
            return $" against {Recent[0]} the run before";

        var all = string.Join(", ", Recent.Take(Recent.Count - 1)) + " and " + Recent[^1];

        // Oldest first, so the numbers read left to right as time does and the last one named is the
        // run immediately before this one. Said in the clause rather than explained by a parenthesis
        // about ordering, which is a thing a reader has to hold rather than read.
        return $" where the {Recent.Count} runs before it excused {all}";
    }

    /// <summary>
    /// What the excuses add to either sentence, or nothing at all where none were made.
    /// <para>
    /// Silent on a run that excused nothing, because a clause saying "and none were excused" on every
    /// green is a clause nobody reads by the third run — and then the one that says twelve reads the
    /// same. Never silent about not knowing: a ledger nobody wrote is the reading this tool is for.
    /// </para>
    /// </summary>
    private string Excusing()
    {
        if (!Asked)
            return "";

        if (Excused is null)
            return ", and how many checks the desk excused was not read";

        if (Excused.Count == 0)
            return "";

        // The conditions and not only the count. Twelve excuses for one absent foreground is a desk
        // somebody was using; twelve for six different facts is a machine that cannot observe at all,
        // and the reader's next move differs for each.
        var facts = Excused
            .Select(one => Readers.Excuse(one).Fact)
            .GroupBy(one => one, StringComparer.Ordinal)
            .OrderByDescending(one => one.Count())
            .Select(one => $"{one.Count()} for {one.Key}");

        // WW281. One count and then the split, because the two questions arrive in that order: how
        // much of this green is real, and then whose doing the rest was. A desk excuse says come back
        // when the machine is quiet; a budget this suite chose and missed says the number is wrong,
        // and a reader who cannot tell them apart cannot act on either.
        return $", and {Excused.Count} check(s) were excused{Against()}{Kinds(Excused)} — {string.Join(", ", facts)}";
    }

    /// <summary>
    /// How the excuses divide between the desk and this suite's own budgets, said only where both
    /// are present: a run with one kind has already named it in the facts that follow.
    /// </summary>
    /// <param name="excused">The rows, which the caller has already found to be there and non-empty.</param>
    private static string Kinds(IReadOnlyList<string> excused)
    {
        var budgets = excused.Count(one => Readers.Excuse(one).Kind == Readers.Budget);
        return budgets == 0 || budgets == excused.Count
            ? ""
            : $" ({excused.Count - budgets} by the desk, {budgets} by a budget this suite chose)";
    }

    /// <summary>The whole reading: the sentence, then the methods, bounded so a wipeout is readable.</summary>
    /// <param name="most">How many methods to list before saying how many were cut.</param>
    public IReadOnlyList<string> Render(int most = 25)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(most);

        var lines = new List<string> { Sentence() };
        lines.AddRange(Listed(Missing, most));
        lines.AddRange(Listed(Skipping, most));
        lines.AddRange(Listed(Unexpected, most));
        lines.AddRange(Excusing(most));
        return new ReadOnlyCollection<string>(lines);
    }

    /// <summary>
    /// A line per case the desk excused, which is what WW233 is for: the sentence says eleven and one
    /// condition, and this says which eleven. Bounded like every other list here, because a machine
    /// that can observe nothing excuses all of them and a wipeout has to stay readable.
    /// </summary>
    private IReadOnlyList<string> Excusing(int most)
    {
        if (Excused is null || Excused.Count == 0)
            return [];

        var read = Excused.Select(Readers.Excuse).ToList();
        var lines = read
            .Take(most)
            .Select(one => $"  excused   {one.Case ?? "<unnamed>"}: {one.Fact}"
                + (one.Absence is null ? "" : $" — {one.Absence}"))
            .ToList();

        if (read.Count > most)
            lines.Add($"  excused   and {read.Count - most} more");

        return lines;
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

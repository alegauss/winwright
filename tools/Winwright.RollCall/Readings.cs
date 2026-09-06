using System.Collections.ObjectModel;

namespace Winwright.RollCall;

/// <summary>
/// One reading a run can make of its own excuses. WW389.
/// </summary>
/// <param name="Named">What it is called, in the words a reader would use.</param>
/// <param name="Task">The task that added it, so the argument for it is findable.</param>
/// <param name="Marker">
/// Words its clause always carries. It is what makes this a list with teeth rather than a comment:
/// a case builds a run each reading should speak on and looks for exactly this.
/// </param>
/// <param name="Where">Whether it lands in the sentence or on one excused line.</param>
/// <param name="Over">The window it speaks over, which is the thing two readings most often differ in.</param>
/// <param name="Louder">
/// The reading that silences this one where both are true, or empty where none does. Precedence and
/// not preference: two clauses saying the same thing in one breath is the failure every one of these
/// rules was written against.
/// </param>
/// <param name="Quiet">
/// What else keeps it quiet, which is its own sufficiency rather than anybody else's precedence.
/// </param>
public sealed record Reading(
    string Named,
    string Task,
    string Marker,
    string Where,
    string Over,
    string Louder,
    string Quiet)
{
    /// <summary>The line a listing shows.</summary>
    public override string ToString() =>
        $"{Named,-34} {Task}  {Where}, over {Over}"
            + (Louder.Length == 0 ? "" : $"; silent where {Louder} spoke")
            + (Quiet.Length == 0 ? "" : $"; {Quiet}");
}

/// <summary>
/// Every reading this tool makes of a run's excuses, and how they compose. WW389.
/// <para>
/// Five of them come off one set of ledgers and each knew when to be quiet only because whoever
/// added it had read the ones already there. WW376's rule cites WW363's, which cites WW248's, and
/// the way to find them was to know they existed — so a sixth reading's author has to reconstruct
/// the composition before deciding anything about their own.
/// </para>
/// <para>
/// What that produces is a fragile sentence rather than a wrong one. Two clauses that both fire say
/// the same thing twice, which is the failure WW363 wrote its own rule against, and the reader who
/// stops finishing the sentence is the one the report is for. It is also how a reading goes quiet
/// for good: WW363's rate appeared on no run of a whole session that <em>none of them is new</em>
/// had not already covered.
/// </para>
/// <para>
/// So this is the composition, in the shape this project uses wherever two things must agree: a
/// list, read back by a case that builds a run for each row and one for each precedence. What the
/// list cannot do is decide — <see cref="Roll" /> still holds the code, and the case is what stops
/// the two drifting.
/// </para>
/// </summary>
public static class Readings
{
    /// <summary>A reading that lands in the run's one sentence.</summary>
    public const string InTheSentence = "the sentence";

    /// <summary>One that lands on the line naming a single excused case.</summary>
    public const string OnAnExcusedLine = "an excused line";

    /// <summary>Every reading, in the order a report says them.</summary>
    public static IReadOnlyList<Reading> All { get; } = new ReadOnlyCollection<Reading>(
    [
        new(
            "the series",
            "WW298",
            "where the",
            InTheSentence,
            "the few runs before this one",
            "",
            "silent on a first run, which has nothing to be a series against"),
        new(
            "none of them is new",
            "WW248",
            "none of them is new",
            InTheSentence,
            "every run on disk, all of which must have excused it",
            "",
            "silent unless every excuse this run made recurs in every one of them"),
        new(
            "how many the ledgers have seen",
            "WW376",
            "inside the last",
            InTheSentence,
            "the rate window, which is deeper than the recurrence one",
            "none of them is new",
            "silent too where any row names no case, because a row it cannot place makes the count a guess"),
        new(
            "the desk against a budget",
            "WW281",
            "by a budget this suite chose",
            InTheSentence,
            "this run alone",
            "",
            "silent where one kind is the whole set, which the facts that follow have already named"),
        new(
            "in all N runs before it",
            "WW248",
            "in all",
            OnAnExcusedLine,
            "every run on disk",
            "none of them is new",
            "silent where this case did not recur; a mark on every line marks nothing"),
        new(
            "excused in N of the last M",
            "WW363",
            "excused in",
            OnAnExcusedLine,
            "the rate window",
            "in all N runs before it",
            "silent below two, because one run is where every excuse starts"),
    ]);

    /// <summary>
    /// The reading that name belongs to, or null where none does. It is what makes
    /// <see cref="Reading.Louder" /> a pointer rather than a word: a precedence naming nothing is
    /// caught by the case that follows it, not by a reader noticing.
    /// </summary>
    /// <param name="named">What it is called.</param>
    public static Reading? Named(string? named) =>
        named is null ? null : All.FirstOrDefault(one => string.Equals(one.Named, named, StringComparison.Ordinal));
}

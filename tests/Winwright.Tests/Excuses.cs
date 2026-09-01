using Xunit;

namespace Winwright.Tests;

/// <summary>What kind of thing a check was excused against.</summary>
/// <remarks>
/// WW281. The two are not interchangeable and the ledger has to keep them apart, which is the same
/// argument WW211 made about the gates: a desk fact is somebody else owning something the engine
/// declares, and a budget is a number this suite chose. A reader who cannot tell them apart cannot
/// act on either — one says come back when the machine is quiet, the other says the number is wrong.
/// </remarks>
internal enum ExcusedBy
{
    /// <summary>
    /// A fact about the desk, named by the engine and gated by <see cref="BusyDesk" />. The default,
    /// because it is what every row written before this column existed was.
    /// </summary>
    Desk,

    /// <summary>
    /// A budget this suite chose — a deadline that ran out, or a cadence it could not keep — gated
    /// by <see cref="SlowMachine" />.
    /// </summary>
    Budget,
}

/// <summary>
/// Where a run writes down the checks it did not make.
/// <para>
/// WW231 built this inside <see cref="BusyDesk" />, when the desk was the only thing that excused
/// anything. WW211 then added a second excuse with its own gate and no ledger at all, so a run that
/// lost a wait to a busy guest reported a clean sweep with the same arithmetic as one where every
/// check ran — WW231's founding defect, in the excuse added after it.
/// </para>
/// <para>
/// So the file has one owner and both gates write through it. One arithmetic, because what a reader
/// needs first is how much of the green is real; one column saying which kind, because what they
/// need next is whose fault it was.
/// </para>
/// </summary>
internal static class Excuses
{
    /// <summary>
    /// The ledger, beside the assembly that wrote it.
    /// <para>
    /// A file rather than a counter, because the process that excuses is not the process that
    /// reports: the roll call runs after the test host, including after one that died, which is the
    /// whole reason it is its own program. A counter in memory would go with the host it was
    /// counting.
    /// </para>
    /// </summary>
    internal static string Ledger { get; } = Path.Combine(AppContext.BaseDirectory, "excused.txt");

    private static readonly object Writing = new();

    /// <summary>
    /// Truncated once, which is once per run: this type is touched by every gate, and a file left
    /// from an earlier run would be read as this one's excuses. Absent afterwards means nothing was
    /// excused; absent because nobody wrote it is what the roll call reports as unknown.
    /// </summary>
    static Excuses()
    {
        try
        {
            File.WriteAllText(Ledger, "");
        }
        catch (Exception unwritable) when (unwritable is IOException or UnauthorizedAccessException)
        {
            // Deliberately swallowed. A suite that cannot write its own ledger still has a suite to
            // run, and the roll call reports the absence rather than this failing every case.
        }
    }

    /// <summary>
    /// Write one excuse down.
    /// <para>
    /// The kind goes last on purpose. The roll call reads this file positionally and was written to
    /// tolerate a row from an older build, so a column appended is a column a reader without it
    /// still parses — and every row that predates this one is a desk row, which is what a missing
    /// kind now means.
    /// </para>
    /// </summary>
    /// <param name="kind">Which kind of thing was not met.</param>
    /// <param name="named">What was not met, as the reading that answers it spells it.</param>
    /// <param name="gate">The type that validated the excuse, so the walk can step over it.</param>
    /// <param name="absence">What the reading said, which is how one excuse is told from another.</param>
    internal static void Written(ExcusedBy kind, string named, Type gate, string absence)
    {
        lock (Writing)
        {
            try
            {
                // WW248. A fifth column, appended for the reason the third and fourth were: this
                // file is read positionally by a reader written to tolerate a row an older build
                // wrote, so a column added is one a reader without it still parses. What it says is
                // whether the case has written down that it means the excuse — and a recurring
                // excuse with nothing here is what the roll refuses, because a hole this suite built
                // for itself is a check it has permanently switched off.
                File.AppendAllText(Ledger, Row(kind, named, Whose(gate), absence) + Environment.NewLine);
            }
            catch (Exception unwritable) when (unwritable is IOException or UnauthorizedAccessException)
            {
                // As above: the case it excused is still excused, and a ledger that could not be
                // written is a number the roll call says it does not have.
            }
        }
    }

    /// <summary>
    /// One row of the ledger, as text.
    /// <para>
    /// WW248. Its own method so the join can be checked without writing to the ledger. The first
    /// version of that check appended a real row, which put a fabricated hole into this run's own
    /// arithmetic — <c>1 for a fact nobody measured</c> — and into the count every later run compares
    /// itself against. A check that has to dirty the reading it is about is checking the wrong thing.
    /// </para>
    /// </summary>
    /// <param name="kind">Which kind of thing was not met.</param>
    /// <param name="named">What was not met.</param>
    /// <param name="whose">The case being excused, as the ledger spells it.</param>
    /// <param name="absence">What the reading said.</param>
    internal static string Row(ExcusedBy kind, string named, string whose, string absence) =>
        $"{named}\t{whose}\t{OneLine(absence)}\t{kind}"
            + $"\t{(whose.Length > 0 && MeantExcuses.Accounted(whose) ? "Meant" : "")}";

    /// <summary>
    /// One line of it, because the ledger is tab-separated and a newline in a field is a second row
    /// that parses as a fact nobody measured.
    /// </summary>
    private static string OneLine(string absence) =>
        absence.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Trim();

    /// <summary>
    /// Which case is being excused, read off the stack.
    /// <para>
    /// WW233. The count landed without the names, and the trade was written up as eighty-one call
    /// sites each taking an argument — which is a price nobody should pay and was the wrong list of
    /// options. The stack already knows: the first frame outside this type and its gate is the case,
    /// or the initialiser that built its fixture, and neither xunit nor any call site has to be asked.
    /// </para>
    /// <para>
    /// It catches nothing, and that is a correction rather than an omission. The first version wrapped
    /// the walk in a catch answering <c>&lt;unnamed&gt;</c> — a defensive catch with no failure mode
    /// anybody could name, which is exactly what <see cref="Swallowing"/> exists to refuse: a value
    /// answered out of a catch block is "I could not tell" spelled like an answer. A frame whose
    /// method is unreadable is skipped, and a walk that finds nothing outside them says so.
    /// </para>
    /// </summary>
    /// <param name="gate">The type that validated the excuse, which is not the case either.</param>
    private static string Whose(Type gate)
    {
        foreach (var frame in new System.Diagnostics.StackTrace(fNeedFileInfo: false).GetFrames())
        {
            if (frame.GetMethod() is { DeclaringType: { } owner } method
                && owner != typeof(Excuses)
                && owner != gate)
            {
                return $"{owner.Name}.{method.Name}";
            }
        }

        return "<unnamed>";
    }

    /// <summary>
    /// That a name is one somebody can act on. Both gates check what they are entitled to check
    /// before they call this; what is common to them is that an excuse with no name at all is worse
    /// than a failure, because it withdraws a check without saying what withdrew it.
    /// </summary>
    /// <param name="named">What was not met.</param>
    internal static void Named(string named) =>
        Assert.False(string.IsNullOrWhiteSpace(named), "an excuse with no name withdraws a check and says nothing");
}

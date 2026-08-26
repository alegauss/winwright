using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// What a case does when the desk it needed was not available.
/// <para>
/// WW133. Windows grants the foreground to a thread holding a window it has just created — usually.
/// Once this process has been refused once it stops being granted, so whether a case that
/// synthesises input can run at all is partly a question about what else is on the screen. Going
/// red then is the misattribution this block's criterion forbids: nothing about the desk is
/// reported as a defect in the code.
/// </para>
/// <para>
/// The framework's answer is the third verdict, and this is that answer inside a harness that has
/// no third verdict. Where the desk refused the act, the case asserts that the result says so
/// honestly and stops; where it did not, the case asserts everything it always did. Both branches
/// assert something real, which is the difference between this and a case that quietly passes.
/// </para>
/// </summary>
/// <summary>
/// A fixture that could not be built because the desk refused, carrying the reading that says so.
/// <para>
/// WW179. A throw is not a hole. <c>RunVerdict</c> ranks a broken harness above a failure precisely
/// because nothing past the throw was observed and the reader is being sent to this repository
/// rather than to the one under test — which is the wrong repository when the shell was merely
/// covering the taskbar.
/// </para>
/// </summary>
internal sealed class DeskRefusedException : Exception
{
    internal DeskRefusedException(Precondition missing, string because)
        : base(because)
    {
        Missing = missing;
    }

    /// <summary>The desk fact that stopped it, which is what makes this excusable rather than red.</summary>
    internal Precondition Missing { get; }
}

internal static class BusyDesk
{
    /// <summary>
    /// Where an excuse is written down, beside the assembly that made it.
    /// <para>
    /// WW231. This was called at eighty-one sites and counted nowhere. An excused case returns and
    /// xunit records a pass, so a run on a locked desk reported every case passing and the roll call's
    /// arithmetic — discovery against results — balanced perfectly. That is the founding defect
    /// wearing the other hat: the suite that reported 352 where the run before had 374 at least moved
    /// a number, and this moved none.
    /// </para>
    /// <para>
    /// A file rather than a counter, because the process that excuses is not the process that reports:
    /// the roll call runs after the test host, including after one that died, which is the whole
    /// reason it is its own program. A counter in memory would go with the host it was counting.
    /// </para>
    /// </summary>
    internal static string Ledger { get; } = Path.Combine(AppContext.BaseDirectory, "excused.txt");

    private static readonly object Writing = new();

    /// <summary>
    /// Truncated once, which is once per run: this type is touched by every desk-dependent class, and
    /// a file left from an earlier run would be read as this one's excuses. Absent afterwards means
    /// nothing was excused; absent because nobody wrote it is what the roll call reports as unknown.
    /// </summary>
    static BusyDesk()
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
    /// Build a fixture, answering null where the desk refused rather than letting the throw stand.
    /// <para>
    /// WW179. Setup meets the same desk facts an act does, and there is no act there to answer them
    /// — so a shell that would not place a tray icon or would not put a menu on the screen ended a
    /// case as a broken harness. The reading is the same one an act carries; what was missing is a
    /// door for it to come through before the act exists.
    /// </para>
    /// </summary>
    /// <typeparam name="T">What is being built.</typeparam>
    /// <param name="build">How to build it.</param>
    /// <returns>The fixture, or null where the desk refused — which the caller returns on.</returns>
    internal static T? Built<T>(Func<T> build)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(build);

        try
        {
            return build();
        }
        catch (DeskRefusedException refused)
        {
            // Checked exactly as an act's hole is checked, so a fixture cannot excuse itself with a
            // precondition that is really about the code under test.
            Excusing(refused.Missing);
            return null;
        }
    }

    /// <summary>
    /// Whether this verdict is a hole about the desk — and, where it is, that it is an honest one.
    /// </summary>
    /// <param name="verdict">What the act answered.</param>
    /// <returns>True where the act never ran, so the caller has nothing further to check.</returns>
    internal static bool Excused(AssertionResult verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);

        if (!verdict.DidNotRun)
            return false;

        // A hole with no precondition is worse than a failure: it excuses a check without saying
        // what excused it, which is the shape of green this whole project exists to withdraw.
        Assert.NotNull(verdict.Missing);
        Excusing(verdict.Missing);

        return true;
    }

    /// <summary>
    /// The same for a reading that answers a precondition and no verdict — the focus reading, which
    /// a case holds directly rather than through an act.
    /// </summary>
    /// <param name="condition">What the reading turned out to be.</param>
    /// <returns>True where the desk refused, so the caller has nothing further to check.</returns>
    internal static bool Excused(Precondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        if (condition.Satisfied)
            return false;

        Excusing(condition);
        return true;
    }

    /// <summary>
    /// Which case is being excused, read off the stack.
    /// <para>
    /// WW233. The count landed without the names, and the trade was written up as eighty-one call
    /// sites each taking an argument — which is a price nobody should pay and was the wrong list of
    /// options. The stack already knows: the first frame outside this type is the case, or the
    /// initialiser that built its fixture, and neither xunit nor any call site has to be asked.
    /// </para>
    /// <para>
    /// It catches nothing, and that is a correction rather than an omission. The first version wrapped
    /// the walk in a catch answering <c>&lt;unnamed&gt;</c> — a defensive catch with no failure mode
    /// anybody could name, which is exactly what <see cref="Swallowing"/> exists to refuse: a value
    /// answered out of a catch block is "I could not tell" spelled like an answer. A frame whose
    /// method is unreadable is skipped, and a walk that finds nothing outside this type says so.
    /// </para>
    /// </summary>
    private static string Whose()
    {
        foreach (var frame in new System.Diagnostics.StackTrace(fNeedFileInfo: false).GetFrames())
        {
            if (frame.GetMethod() is { DeclaringType: { } owner } method && owner != typeof(BusyDesk))
                return $"{owner.Name}.{method.Name}";
        }

        return "<unnamed>";
    }

    /// <summary>
    /// That the hole is an honest one, and about the desk rather than about anything the machine
    /// could have arranged.
    /// </summary>
    /// <summary>
    /// One line of it, because the ledger is tab-separated and a newline in a field is a second row
    /// that parses as a fact nobody measured.
    /// </summary>
    private static string OneLine(string absence) =>
        absence.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Trim();

    private static void Excusing(Precondition missing)
    {
        Assert.False(missing.Satisfied);
        Assert.False(string.IsNullOrWhiteSpace(missing.Absence));

        // WW183. The engine says which conditions are the desk's, so this no longer keeps a list.
        // The one it kept had missed two — the reading that measures a person at the keyboard, and
        // the one WW38 added for a window standing over a capture — and a hole about something the
        // machine could not have arranged is a hole nobody should accept.
        Assert.True(
            DeskFacts.Names(missing.Name),
            $"'{missing.Name}' is not a fact this engine calls the desk's: "
                + string.Join("; ", DeskFacts.Named));

        // WW231. Written down after the validation and never before it: an excuse this method is
        // about to refuse is not an excuse, and recording it would put a hole in the count that the
        // run does not have.
        lock (Writing)
        {
            try
            {
                // WW248: the absence as well, because the name alone cannot say whether the desk
                // belonged to a person or to this suite's own dialog — and an excuse that arrives on
                // every run because of the second is a check nobody is running.
                File.AppendAllText(
                    Ledger,
                    $"{missing.Name}\t{Whose()}\t{OneLine(missing.Absence)}{Environment.NewLine}");
            }
            catch (Exception unwritable) when (unwritable is IOException or UnauthorizedAccessException)
            {
                // As above: the case it excused is still excused, and a ledger that could not be
                // written is a number the roll call says it does not have.
            }
        }
    }
}

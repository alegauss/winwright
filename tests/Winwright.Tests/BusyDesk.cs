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
    /// Where an excuse is written down.
    /// <para>
    /// WW231. This was called at eighty-one sites and counted nowhere. An excused case returns and
    /// xunit records a pass, so a run on a locked desk reported every case passing and the roll call's
    /// arithmetic — discovery against results — balanced perfectly. That is the founding defect
    /// wearing the other hat: the suite that reported 352 where the run before had 374 at least moved
    /// a number, and this moved none.
    /// </para>
    /// <para>
    /// WW281 moved the file itself to <see cref="Excuses" />, because the desk stopped being the only
    /// thing that excuses anything the day <see cref="SlowMachine" /> was written.
    /// </para>
    /// </summary>
    internal static string Ledger => Excuses.Ledger;

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
    /// That the hole is an honest one, and about the desk rather than about anything the machine
    /// could have arranged.
    /// </summary>
    private static void Excusing(Precondition missing)
    {
        Assert.False(missing.Satisfied);
        Excuses.Named(missing.Name);
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
        //
        // WW248: the absence as well, because the name alone cannot say whether the desk belonged to
        // a person or to this suite's own dialog — and an excuse that arrives on every run because of
        // the second is a check nobody is running.
        Excuses.Written(ExcusedBy.Desk, missing.Name, typeof(BusyDesk), missing.Absence);
    }
}

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
internal static class BusyDesk
{
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
        Assert.False(string.IsNullOrWhiteSpace(missing.Absence));

        // The five conditions a desk-dependent act needs and never anything else. The last two are
        // WW172's: a focus that left the application under test, and a notification area that could
        // not be searched because the shell was covering it, are the same class of fact as a
        // foreground Windows would not grant — and a case holding one has as little to say.
        Assert.Contains(
            missing.Name,
            new[]
            {
                Foreground.PreconditionName,
                Winwright.Acting.Keyboard.FocusPreconditionName,
                Winwright.Acting.FocusReading.Named,
                Winwright.Acting.TraySearch.PreconditionName,
                Winwright.Acting.TrayMenu.PreconditionName,
            });
    }
}

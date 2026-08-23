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
        Assert.False(verdict.Missing.Satisfied);
        Assert.False(string.IsNullOrWhiteSpace(verdict.Missing.Absence));

        // One of the two conditions an input act needs from the desk, and never anything else: a
        // hole about something the machine could have arranged is a hole nobody should accept.
        Assert.Contains(
            verdict.Missing.Name,
            new[] { Foreground.PreconditionName, Winwright.Acting.Keyboard.FocusPreconditionName });

        return true;
    }
}

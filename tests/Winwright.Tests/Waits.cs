using Winwright.Locating;
using Winwright.Projects;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// Every wait this suite takes, as a deadline on a condition through the engine's own attempt.
/// <para>
/// WW143. Block C's criterion says no scenario carries a sleep and every wait is a deadline on a
/// condition with the time it took recorded. The engine kept it; the suite did not — eighteen
/// hand-rolled loops across five files, each a for loop over a fixed count with a
/// <c>Thread.Sleep</c> in it, and one bare sleep of 120 ms waiting for a focus change with no
/// condition at all. They were not wrong so much as unowned: none reported how long it took and
/// none failed saying what it waited for, so one slightly too short on a busy machine produced a
/// red about the application, which is the misattribution WW119 measured.
/// </para>
/// <para>
/// The deadlines are named here and nowhere else. A number typed into the case that needs it is one
/// nobody can tune without reading every case, which is the same argument
/// <see cref="Timeouts"/> makes to a scenario — so the suite makes it to itself, with its own
/// declaration rather than by borrowing the seeded names: a fixture waiting a launch timeout to
/// notice a file would be the wrong number arrived at tidily.
/// </para>
/// </summary>
internal static class Waits
{
    /// <summary>What this suite is willing to wait for, by name.</summary>
    internal static Timeouts Declared { get; } = Timeouts.Declared(
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            // A launched process saying what it is running, which is Windows and not the fixture.
            ["readable"] = 5000,

            // A window drawn and enumerable. Longest of these: it covers a cold start of the
            // fixture on a runner that has never run it, which is what CI always is.
            ["draw"] = 10000,

            // A process actually exited, which WW129 measured as well after its last window went.
            ["gone"] = 8000,

            // A file the application under test writes, once its window is up.
            //
            // WW203. "Once its window is up" is the whole of the repair. Both callers waited on this
            // from a standing start, so one 5000ms budget covered a cold start, a layout and the
            // write — while `draw` above is given 10000ms for the cold start alone. Two guest runs
            // came in at 5006ms and 5009ms and said the fixture never wrote what it drew, which is a
            // claim about the application arriving through a number this suite chose. Each caller
            // now waits for the window on `draw` first, so this budget is about the write.
            ["wrote"] = 5000,

            // A page that finishes loading, against the fixture's own declared --loading duration.
            ["loaded"] = 5000,

            // A focus that moved. Never asserted — see Trying.
            ["focus"] = 1000,

            // A menu letting go of the desk after Escape.
            ["dismiss"] = 1500,

            // WW159. An animation showing every state it declares. Long, and deliberately: the
            // check stops the moment it has seen them all, so this is only ever paid by a machine
            // slow enough to need it — and the alternative was a fixed window that a loaded desk
            // lost, which reds about an animation that was cycling exactly as asked.
            ["cycle"] = 8000,

            // The desktop changing hands, which Windows does asynchronously and not always.
            ["desktop"] = 500,
        },
        "tests/Winwright.Tests/Waits.cs");

    /// <summary>
    /// Wait for a condition, and fail naming what was waited for, for how long, and over how many
    /// looks. The failure is the point: a loop that times out silently and lets the next assertion
    /// go red reports the wrong thing about the application.
    /// </summary>
    /// <param name="named">Which declared deadline this wait is on.</param>
    /// <param name="what">What is being waited for, as the failure should name it.</param>
    /// <param name="condition">Whether it has happened yet.</param>
    internal static Waited Until(string named, string what, Func<bool> condition)
    {
        var waited = Trying(named, condition);
        Assert.True(waited.Happened, Missed(named, what, waited));
        return waited;
    }

    /// <summary>The same, for a wait whose answer is a thing rather than a state.</summary>
    /// <typeparam name="T">What is being looked for.</typeparam>
    internal static T Until<T>(string named, string what, Func<T?> look)
        where T : class
    {
        var seen = Attempt.Until(look, Declared, named);
        Assert.True(seen.Found, $"{what} — nothing after {seen.WaitedMs}ms and {seen.Polls} look(s), "
            + $"against the {Declared.For(named)}ms this suite declares for '{named}'");
        return seen.Value!;
    }

    /// <summary>
    /// Wait, and carry on either way. For the two waits whose failure is a fact about the desk
    /// rather than about the application: a desktop that stays elsewhere and a focus that never
    /// arrived are holes the cases already excuse, and asserting them here would turn one busy
    /// machine into a wall of reds about tab order. What is gained over a fixed sleep is still the
    /// whole of it — it returns the moment the condition holds, and it says what it was waiting for.
    /// </summary>
    internal static Waited Trying(string named, Func<bool> condition) =>
        Attempt.UntilTrue(condition, Declared.For(named), Declared.For("poll"));

    /// <summary>How a missed deadline reads, which is the half the hand-rolled loops never had.</summary>
    internal static string Missed(string named, string what, Waited waited) =>
        $"{what} — not after {waited.WaitedMs}ms over {waited.Polls} look(s), "
            + $"against the {Declared.For(named)}ms this suite declares for '{named}'";
}

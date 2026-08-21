using System.Diagnostics;

using Winwright.Projects;

namespace Winwright.Locating;

/// <summary>
/// The two ways this project looks for something, and the reason there are two.
/// <para>
/// A helper that quietly retries for two hundred milliseconds folds that sleep into every miss, so
/// a loop doing its own waiting ends up measuring its own helper rather than the application.
/// Asking whether something <em>arrived</em> needs a deadline; asking whether it is <em>gone</em>
/// needs a single look, and using the first for the second is how a timing observation loses its
/// resolution — which is also how a scenario ends up carrying a sleep to get it back.
/// </para>
/// <para>
/// So the two are separate methods with separate names, neither reachable by leaving an argument
/// off the other: <see cref="Once{T}"/> sleeps never, and <see cref="Until{T}(Func{T}, int, int)"/>
/// takes the deadline as a required argument so nobody inherits one they did not choose.
/// </para>
/// </summary>
public static class Attempt
{
    /// <summary>
    /// Look exactly once. No poll, no retry and no sleep — which is what asking whether something
    /// has gone actually needs, and what makes the answer's timing the application's and not this
    /// method's.
    /// </summary>
    public static Sighting<T> Once<T>(Func<T?> look)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(look);

        var clock = Stopwatch.StartNew();
        var seen = look();
        clock.Stop();
        return new Sighting<T>(seen, (int)clock.ElapsedMilliseconds, 1);
    }

    /// <summary>
    /// Look until <paramref name="deadlineMs"/> has passed, polling every
    /// <paramref name="pollMs"/>. The first look is taken before any waiting, so something already
    /// there costs nothing.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Where the deadline is not positive. A deadline of nothing is a single look, and a single
    /// look is <see cref="Once{T}"/> — reached by name rather than by an argument left at zero.
    /// </exception>
    public static Sighting<T> Until<T>(Func<T?> look, int deadlineMs, int pollMs = 25)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(look);
        if (deadlineMs <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(deadlineMs), deadlineMs, "a deadline of nothing is a single look, which is Attempt.Once");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pollMs);

        var clock = Stopwatch.StartNew();
        var polls = 0;
        while (true)
        {
            polls++;
            var seen = look();
            if (seen is not null)
                return new Sighting<T>(seen, (int)clock.ElapsedMilliseconds, polls);

            var left = deadlineMs - (int)clock.ElapsedMilliseconds;
            if (left <= 0)
                return new Sighting<T>(null, (int)clock.ElapsedMilliseconds, polls);

            Thread.Sleep(Math.Min(pollMs, left));
        }
    }

    /// <summary>
    /// The same, with the deadline and the poll interval read from what the project declared —
    /// which is where a number belongs, so a scenario never carries one.
    /// </summary>
    public static Sighting<T> Until<T>(Func<T?> look, Timeouts timeouts, string named = "resolve")
        where T : class
    {
        ArgumentNullException.ThrowIfNull(timeouts);
        return Until(look, timeouts.For(named), timeouts.For("poll"));
    }
}

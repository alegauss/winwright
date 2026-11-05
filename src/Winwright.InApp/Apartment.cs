using System.Runtime.ExceptionServices;

namespace Winwright.InApp;

/// <summary>Raised where work on a single-threaded apartment did not finish inside its bound.</summary>
public sealed class ApartmentTimeoutException : TimeoutException
{
    /// <summary>Say what did not finish, and in how long.</summary>
    public ApartmentTimeoutException(string message)
        : base(message)
    {
    }

    /// <summary>Unused. Present because an exception with no default shapes is awkward to catch.</summary>
    public ApartmentTimeoutException()
        : base("work on a single-threaded apartment did not finish")
    {
    }

    /// <summary>Unused. Present for the same reason.</summary>
    public ApartmentTimeoutException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>
/// Running work on a single-threaded apartment, once, for everybody.
/// <para>
/// One project carries the same eight-line runner in twenty-seven test files, each with its own
/// timeout and its own message for a thread that does not finish. It is not boilerplate that
/// happens to repeat: controls cannot be constructed off that apartment at all, and a suite that
/// hangs on a UI primitive reports nothing whatever — no pass, no failure, no name. The runner is
/// load-bearing, which is exactly why it should exist once, bounded, and surfacing what the thread
/// threw rather than a wrapper around it.
/// </para>
/// <para>
/// Two promises and one refusal. It is <em>bounded</em>: a thread that does not finish becomes a
/// named timeout rather than a suite that stops. It <em>surfaces</em>: what the work threw is
/// rethrown as itself, stack intact, so a refusal a caller wrote can still be asserted on by type.
/// And it does <em>not pump</em>: work that defers to the dispatcher will not run here, and a
/// runner that quietly pumped would make a check pass in a way the application never would.
/// </para>
/// </summary>
public static class Apartment
{
    /// <summary>How long work is given unless told otherwise.</summary>
    public static TimeSpan DefaultLimit { get; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Run <paramref name="work"/> on a fresh single-threaded apartment and hand back its answer.
    /// </summary>
    /// <param name="work">The work. Every presentation object it touches must be made inside it.</param>
    /// <param name="within">How long to wait. Defaults to <see cref="DefaultLimit"/>.</param>
    /// <param name="named">What to call the work in a timeout, so the message names something.</param>
    /// <exception cref="ApartmentTimeoutException">Where it did not finish in time — see the remarks.</exception>
    /// <remarks>
    /// The thread is left running on a timeout rather than aborted: there is no safe way to stop a
    /// thread holding presentation state, and one killed mid-layout takes the process with it. It
    /// is a background thread, so it holds nothing open — the answer is simply never coming.
    /// </remarks>
    public static T Run<T>(Func<T> work, TimeSpan? within = null, string? named = null)
    {
        ArgumentNullException.ThrowIfNull(work);

        var limit = within ?? DefaultLimit;
        if (limit <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(within), limit, "work is given a length of time, and this is not one");

        var what = string.IsNullOrWhiteSpace(named) ? "the work" : named.Trim();

        T? answer = default;
        ExceptionDispatchInfo? threw = null;

        var thread = new Thread(() =>
        {
            try
            {
                answer = work();
            }
            catch (Exception broke)
            {
                // Captured rather than caught-and-rethrown here: capturing keeps the stack the
                // work built, and a caller asserting on a refusal wants the refusal's own type.
                threw = ExceptionDispatchInfo.Capture(broke);
            }
        })
        {
            IsBackground = true,
            Name = $"winwright: {what}",
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (!thread.Join(limit))
        {
            // Invariant, and measured rather than assumed: the first draft formatted in the
            // machine's own culture and read "within 0,2s" here, so the same timeout was a
            // different sentence on every desk and on the runner.
            var seconds = limit.TotalSeconds.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            throw new ApartmentTimeoutException(
                $"{what} did not finish within {seconds}s on its single-threaded apartment. It is still running "
                    + "and cannot safely be stopped, so nothing further will come back from it.");
        }

        threw?.Throw();
        return answer!;
    }

    /// <summary>The same, for work with no answer.</summary>
    /// <param name="work">The work.</param>
    /// <param name="within">How long to wait.</param>
    /// <param name="named">What to call it in a timeout.</param>
    public static void Run(Action work, TimeSpan? within = null, string? named = null)
    {
        ArgumentNullException.ThrowIfNull(work);
        Run<object?>(
            () =>
            {
                work();
                return null;
            },
            within,
            named);
    }
}

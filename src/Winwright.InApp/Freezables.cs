using System.Windows.Media;
using System.Windows.Threading;

namespace Winwright.InApp;

/// <summary>
/// Raised where something a capture was handed belongs to another thread. Its own type, because
/// the whole point is that the reason is about threading rather than about the picture.
/// </summary>
public sealed class ThreadBoundException : InvalidOperationException
{
    /// <summary>Say what belongs to which thread, and what to do instead.</summary>
    public ThreadBoundException(string message)
        : base(message)
    {
    }

    /// <summary>Unused. Present because an exception with no default shapes is awkward to catch.</summary>
    public ThreadBoundException()
        : base("this capture was handed something that belongs to another thread")
    {
    }

    /// <summary>Unused. Present for the same reason.</summary>
    public ThreadBoundException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>
/// Whether a brush, or anything else with thread affinity, may cross to a capture thread.
/// <para>
/// A brush is a freezable, and an unfrozen one belongs to the thread that made it. A static brush
/// therefore belongs to whichever thread reached the class first, and every capture thread after
/// that is refused — and captures run on their own single-threaded apartment by nature, so the
/// second one always throws. Found by the first run of a capture suite rather than by reading, and
/// reproduced twice in this repository's own tests before this was written.
/// </para>
/// <para>
/// The failure it produces without this is <em>"cannot use a DependencyObject that belongs to a
/// different thread than its parent Freezable"</em>, raised from inside the drawing. That sentence
/// names neither the brush nor either thread, which is why the refusal here is worth more than the
/// exception it replaces.
/// </para>
/// </summary>
public static class Freezables
{
    /// <summary>
    /// A copy of <paramref name="brush"/> that may be shared with any capture thread. Copied
    /// before freezing, always: freezing the one the caller handed over would make that caller's
    /// own next change to it throw, from inside a capture nobody would think to blame.
    /// </summary>
    /// <param name="brush">The brush.</param>
    /// <exception cref="ThreadBoundException">
    /// Where it cannot be frozen at all — a brush painting a live visual tree is one of those, and
    /// it stays bound to the thread that made it however it is passed around.
    /// </exception>
    public static Brush Shared(Brush brush)
    {
        ArgumentNullException.ThrowIfNull(brush);
        Insist(brush, "this brush");

        if (brush.IsFrozen)
            return brush;

        var copy = brush.Clone();
        if (!copy.CanFreeze)
        {
            throw new ThreadBoundException(
                $"a {brush.GetType().Name} cannot be frozen, so it belongs to the thread that made it however it is "
                    + "passed around — make one per capture thread rather than sharing this");
        }

        copy.Freeze();
        return copy;
    }

    /// <summary>
    /// The same, answering rather than refusing. Null where it cannot be shared, so a caller with
    /// a fallback can take it without catching anything.
    /// </summary>
    /// <param name="brush">The brush, or null.</param>
    public static Brush? Shareable(Brush? brush)
    {
        if (brush is null || !Reaches(brush))
            return null;

        return brush.IsFrozen ? brush : Frozen(brush);
    }

    /// <summary>
    /// Whether this thread may touch <paramref name="what"/> at all. Frozen things belong to
    /// nobody and are reachable from everywhere; an unfrozen one is reachable only from the thread
    /// that made it. Asked without touching it, because asking the wrong way is the same throw.
    /// </summary>
    /// <param name="what">Anything with thread affinity.</param>
    public static bool Reaches(DispatcherObject what)
    {
        ArgumentNullException.ThrowIfNull(what);

        // Dispatcher is null on a frozen freezable and safe to read from any thread, which is what
        // makes this askable at all: IsFrozen is not — reading it from the wrong thread throws the
        // very exception this exists to replace.
        return what.Dispatcher is null || what.CheckAccess();
    }

    /// <summary>
    /// Refuse where this thread may not touch it, naming both threads. Does nothing for null, so
    /// an optional brush stays optional.
    /// </summary>
    /// <param name="what">Anything with thread affinity, or null.</param>
    /// <param name="named">What to call it in the refusal.</param>
    /// <exception cref="ThreadBoundException">Where it belongs to another thread and is not frozen.</exception>
    public static void Insist(DispatcherObject? what, string named)
    {
        if (what is null || Reaches(what))
            return;

        throw new ThreadBoundException(
            $"{named} is a {what.GetType().Name} that is not frozen, so it belongs to thread "
                + $"{Named(what.Dispatcher)} and this capture is on {Named(Dispatcher.CurrentDispatcher)} — "
                + "freeze it, or make one per capture thread");
    }

    private static Brush? Frozen(Brush brush)
    {
        var copy = brush.Clone();
        if (!copy.CanFreeze)
            return null;

        copy.Freeze();
        return copy;
    }

    private static string Named(Dispatcher? dispatcher) =>
        dispatcher is null ? "(none)" : $"{dispatcher.Thread.ManagedThreadId}";
}

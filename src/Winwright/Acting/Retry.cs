using Winwright.Tracing;

namespace Winwright.Acting;

/// <summary>What a bounded set of attempts produced, and how many it took.</summary>
/// <typeparam name="T">What the act answers with.</typeparam>
public sealed record Attempted<T>
{
    internal Attempted(T last, int attempts, int cap, bool succeeded)
    {
        Last = last;
        Attempts = attempts;
        Cap = cap;
        Succeeded = succeeded;
    }

    /// <summary>What the last attempt answered, whether it succeeded or not.</summary>
    public T Last { get; }

    /// <summary>How many attempts were made.</summary>
    public int Attempts { get; }

    /// <summary>How many were allowed.</summary>
    public int Cap { get; }

    /// <summary>Whether one of them worked.</summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Whether it worked, but not the first time. This is a finding in its own right: an act that
    /// only ever works on the third attempt is telling you something, and a green that hid the
    /// count would have thrown it away.
    /// </summary>
    public bool NeededMoreThanOne => Succeeded && Attempts > 1;

    /// <summary>What happened, with the count in it whichever way it went.</summary>
    public override string ToString() => Succeeded
        ? NeededMoreThanOne
            ? $"worked on attempt {Attempts} of {Cap}."
            : $"worked first time."
        : $"did not work in {Cap} attempt{(Cap == 1 ? "" : "s")}.";
}

/// <summary>
/// Attempts, capped and counted.
/// <para>
/// One walk and one read is a coin toss against a shell that drops synthesised input: three runs
/// in ten reported a submenu that did not expand, against a build with nothing wrong with it,
/// wearing the wording of a real defect.
/// </para>
/// <para>
/// What is deliberately not here is retrying until it passes. The attempts are capped, so an act
/// that genuinely stopped working still goes red and merely stops doing so at random — and the
/// count reaches the output, because an act that only ever works on the third attempt is itself a
/// finding rather than a green.
/// </para>
/// </summary>
public static class Retry
{
    /// <summary>How many attempts a project gets without declaring anything.</summary>
    public const int DefaultCap = 3;

    /// <summary>
    /// The most a cap may be. Past a handful it stops being a cap and becomes the loop this whole
    /// type exists to refuse — an act allowed ten goes is one nobody will ever see fail.
    /// </summary>
    public const int MostAttempts = 5;

    /// <summary>
    /// Run <paramref name="act"/> until <paramref name="succeeded"/> says so or the cap is spent.
    /// There is no overload without a cap, and that is the point of the type.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Where the cap is not between one and <see cref="MostAttempts"/>.
    /// </exception>
    public static Attempted<T> Bounded<T>(Func<T> act, Func<T, bool> succeeded, int cap = DefaultCap)
    {
        ArgumentNullException.ThrowIfNull(act);
        ArgumentNullException.ThrowIfNull(succeeded);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cap);
        if (cap > MostAttempts)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cap),
                cap,
                $"a cap of {cap} is not a cap; past {MostAttempts} an act is one nobody will ever see fail");
        }

        var answer = act();
        var attempts = 1;
        while (!succeeded(answer) && attempts < cap)
        {
            answer = act();
            attempts++;
        }

        return new Attempted<T>(answer, attempts, cap, succeeded(answer));
    }

    /// <summary>
    /// Stamp the count onto the step a trace records. A step that took three goes is a different
    /// step from one that took one, even when both are green, so the record says which it was.
    /// </summary>
    public static TraceStep Recorded<T>(TraceStep step, Attempted<T> attempted)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(attempted);

        return step with
        {
            Attempts = attempted.Attempts,
            Detail = attempted.NeededMoreThanOne
                ? string.IsNullOrEmpty(step.Detail)
                    ? attempted.ToString()
                    : $"{step.Detail} ({attempted})"
                : step.Detail,
        };
    }
}

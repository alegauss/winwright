namespace Winwright.Locating;

/// <summary>
/// What one attempt to find something saw, and what it cost to see it. The two numbers are the
/// ones a trace step records, because a run that says only "found" cannot be read afterwards for
/// how close to its deadline it came.
/// </summary>
/// <typeparam name="T">What was being looked for.</typeparam>
public sealed record Sighting<T>
    where T : class
{
    internal Sighting(T? value, int waitedMs, int polls)
    {
        Value = value;
        WaitedMs = waitedMs;
        Polls = polls;
    }

    /// <summary>What was found, or null where nothing was.</summary>
    public T? Value { get; }

    /// <summary>Whether anything was found at all.</summary>
    public bool Found => Value is not null;

    /// <summary>
    /// How long this took, in milliseconds. On a single look that is what the look itself cost and
    /// never a sleep; on a waiting one it is how much of the deadline was spent.
    /// </summary>
    public int WaitedMs { get; }

    /// <summary>How many looks were taken. Exactly one where nothing waited.</summary>
    public int Polls { get; }

    /// <summary>What was found, or a refusal naming what was being looked for.</summary>
    /// <exception cref="InvalidOperationException">Where nothing was found.</exception>
    public T Require(string what) => Value
        ?? throw new InvalidOperationException($"{what} was not there after {WaitedMs} ms and {Polls} look(s)");
}

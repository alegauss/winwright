namespace Winwright.Processes;

/// <summary>
/// Attaching did not reach anything. It refuses rather than reporting a hole, because attaching
/// names a specific process: a caller who passed a pid or a window handle has already decided
/// which instance this run is about, and quietly running against another one is the defect.
/// </summary>
public sealed class AttachFailedException : Exception
{
    /// <param name="target">The pid or window that was named, as it was named.</param>
    /// <param name="because">Why nothing was reached.</param>
    public AttachFailedException(string target, string because)
        : base($"cannot attach to {target}: {because}")
    {
        Target = target;
        Because = because;
    }

    /// <summary>What was named.</summary>
    public string Target { get; }

    /// <summary>Why nothing was reached.</summary>
    public string Because { get; }
}

namespace Winwright.Tracing;

/// <summary>
/// What became of one step. The same three readings the run itself has, because a step that could
/// not be attempted is the thing a hole in the verdict is made of, and a trace that had only two
/// words for it would lose the distinction one line before the summary needs it.
/// </summary>
public enum StepVerdict
{
    /// <summary>It was attempted and it did what it said.</summary>
    Ok,

    /// <summary>It was attempted and it did not.</summary>
    Failed,

    /// <summary>It was never attempted, because a precondition for it was absent.</summary>
    Unchecked,

    /// <summary>
    /// The harness threw here. Kept apart from <see cref="Failed"/> in the trace as well as in
    /// the verdict, because the reader who greps for what broke is deciding which repository to
    /// open, and a step that threw is the one line that answers that.
    /// </summary>
    Threw,
}

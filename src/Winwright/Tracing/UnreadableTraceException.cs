namespace Winwright.Tracing;

/// <summary>
/// A trace line that is not a step, named where it is.
/// <para>
/// A trace is read after a run that already went wrong, and often after one that was truncated, so
/// this is the second bad moment in a row for whoever is reading it. What the parser's own
/// exception offers them is a complaint about an invalid start of a value and a byte offset into a
/// string that is no longer on screen: no path, no line number, and nothing saying the file was a
/// trace at all.
/// </para>
/// <para>
/// A blank line is not this. A trace ended by a crash finishes on one, and skipping it is the
/// reader working rather than failing. This is the line that has content and is not a step.
/// </para>
/// </summary>
public sealed class UnreadableTraceException : InvalidOperationException
{
    /// <summary>How much of the offending line a refusal shows before cutting it.</summary>
    public const int Shown = 120;

    /// <summary>Say which file, which line, and what was on it.</summary>
    /// <param name="file">The trace, or a phrase naming the reader where there is no file.</param>
    /// <param name="line">The line's ordinal, counted from 1 as an editor counts them.</param>
    /// <param name="text">The line itself.</param>
    /// <param name="because">What the parser objected to.</param>
    public UnreadableTraceException(string file, int line, string text, string because)
        : base($"{file}:{line} is not a trace step: {because}\n  {Cut(text)}")
    {
        File = file;
        Line = line;
        Text = text ?? "";
        Because = because;
    }

    /// <summary>Unused. Present because an exception with no default shapes is awkward to catch.</summary>
    public UnreadableTraceException()
        : base("a line in the trace is not a step")
    {
        File = "";
        Text = "";
        Because = "";
    }

    /// <summary>Unused. Present for the same reason.</summary>
    public UnreadableTraceException(string message)
        : base(message)
    {
        File = "";
        Text = "";
        Because = "";
    }

    /// <summary>Unused. Present for the same reason.</summary>
    public UnreadableTraceException(string message, Exception inner)
        : base(message, inner)
    {
        File = "";
        Text = "";
        Because = "";
    }

    /// <summary>The trace this was read from, as the reader was given it.</summary>
    public string File { get; }

    /// <summary>Which line it was, counted the way an editor counts.</summary>
    public int Line { get; }

    /// <summary>The line itself, whole — the message cuts it and this does not.</summary>
    public string Text { get; }

    /// <summary>What the parser objected to.</summary>
    public string Because { get; }

    /// <summary>
    /// The line as a terminal can show it. Cut rather than wrapped, because a trace line holding a
    /// whole element tree would bury the file and the number that came before it.
    /// </summary>
    private static string Cut(string? text)
    {
        var line = (text ?? "").Trim();
        return line.Length <= Shown ? line : line[..Shown] + "…";
    }
}

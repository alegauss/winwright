using System.Text.Json;

namespace Winwright.Tracing;

/// <summary>
/// Reads a trace back. It exists because a record nothing can read is a record nobody checks —
/// the round trip through here is what keeps the written form honest, and it is how a report is
/// built from what the run saw rather than from what the run remembered.
/// </summary>
public static class TraceLog
{
    /// <summary>What a refusal calls a reader that came from no file.</summary>
    private const string NoFile = "(a trace with no file)";

    /// <summary>Every step in the file, in the order the run wrote them.</summary>
    /// <param name="path">The trace file.</param>
    /// <exception cref="UnreadableTraceException">Where a line has content and is not a step.</exception>
    public static IReadOnlyList<TraceStep> ReadFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var full = Path.GetFullPath(path.Trim());
        using var reader = new StreamReader(full);
        return Read(reader, full);
    }

    /// <summary>
    /// Every step the reader holds. A blank line is skipped, because a trace truncated by a crash
    /// ends in one; a line that is not a step is not, because that is the file saying it is not a
    /// trace and quietly dropping it would let a report be built out of half a record.
    /// </summary>
    /// <param name="reader">The trace.</param>
    /// <param name="named">
    /// What to call it in a refusal. A path where there is one: a reader is being read after a run
    /// already went wrong, and a complaint naming no file is the second unhelpful thing in a row.
    /// </param>
    /// <exception cref="UnreadableTraceException">Where a line has content and is not a step.</exception>
    public static IReadOnlyList<TraceStep> Read(TextReader reader, string named = NoFile)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var steps = new List<TraceStep>();
        var at = 0;

        while (reader.ReadLine() is { } line)
        {
            at++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            steps.Add(Step(line, named, at));
        }

        return steps;
    }

    private static TraceStep Step(string line, string named, int at)
    {
        try
        {
            return TraceFormat.Parse(line);
        }
        catch (Exception unreadable) when (unreadable is JsonException or FormatException or ArgumentException or NotSupportedException)
        {
            // Wrapped and never rethrown as itself: what the parser says is a byte offset into a
            // string nobody is looking at, and the three facts worth having - which file, which
            // line, what was on it - are known here and nowhere else.
            throw new UnreadableTraceException(named, at, line, Because(unreadable));
        }
    }

    /// <summary>
    /// What the parser objected to, in one line. A JSON error carries its own line and position
    /// within the fragment it was given, and both are noise here: the fragment is one line, and
    /// this refusal already says which.
    /// </summary>
    private static string Because(Exception unreadable)
    {
        var said = unreadable.Message;
        var noise = said.IndexOf(" LineNumber:", StringComparison.Ordinal);
        return noise < 0 ? said : said[..noise].TrimEnd();
    }
}

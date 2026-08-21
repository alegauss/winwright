namespace Winwright.Tracing;

/// <summary>
/// Reads a trace back. It exists because a record nothing can read is a record nobody checks —
/// the round trip through here is what keeps the written form honest, and it is how a report is
/// built from what the run saw rather than from what the run remembered.
/// </summary>
public static class TraceLog
{
    /// <summary>Every step in the file, in the order the run wrote them.</summary>
    public static IReadOnlyList<TraceStep> ReadFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var reader = new StreamReader(path);
        return Read(reader);
    }

    /// <summary>
    /// Every step the reader holds. A blank line is skipped, because a trace truncated by a crash
    /// ends in one; a line that is not a step is not, because that is the file saying it is not a
    /// trace and quietly dropping it would let a report be built out of half a record.
    /// </summary>
    public static IReadOnlyList<TraceStep> Read(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var steps = new List<TraceStep>();
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            steps.Add(TraceFormat.Parse(line));
        }

        return steps;
    }
}

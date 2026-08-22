using Winwright.Verdicts;

namespace Winwright.Tracing;

/// <summary>
/// Writes the trace as it happens, one line per step, flushed on every one. The flush is the
/// point: the run this record exists for is the run that broke, and a buffer holding the last
/// four steps when the harness died is a record missing exactly the steps worth reading.
/// </summary>
public sealed class TraceWriter : IDisposable
{
    private readonly TextWriter writer;
    private readonly bool ownsWriter;
    private int written;

    private TraceWriter(TextWriter writer, bool ownsWriter)
    {
        this.writer = writer;
        this.ownsWriter = ownsWriter;
    }

    /// <summary>How many steps have been written so far, which is the ordinal the next one takes.</summary>
    public int Count => written;

    /// <summary>Write to somewhere already open. Disposing this does not close it.</summary>
    public static TraceWriter To(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        return new TraceWriter(writer, ownsWriter: false);
    }

    /// <summary>
    /// Write to a file beside the run, creating the directory it sits in. Truncates: a trace is
    /// one run's record, and appending would silently join two runs into one story.
    /// </summary>
    public static TraceWriter ToFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        return new TraceWriter(new StreamWriter(path, append: false) { AutoFlush = true }, ownsWriter: true);
    }

    /// <summary>
    /// Record one step and return it as it was written — with the ordinal this writer assigned,
    /// so a caller that wants the number reads it back rather than keeping a count of its own.
    /// </summary>
    public TraceStep Write(TraceStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        var numbered = step with { Step = ++written };
        writer.Write(TraceFormat.Line(numbered));
        writer.Write('\n');
        writer.Flush();
        return numbered;
    }

    /// <summary>
    /// Record a step that settled an assertion, and hand back the result joined to it.
    /// <para>
    /// One call and not two, deliberately. The join is a property of what a verdict is, and a
    /// runner left to make it itself would make it one way; the next runner another. Here the step
    /// carries the assertion's name and the result carries the step's ordinal, and neither half
    /// can be written without the other.
    /// </para>
    /// </summary>
    /// <param name="step">The step, without its ordinal — this writer assigns that.</param>
    /// <param name="result">What the step settled.</param>
    /// <returns>The result, carrying the ordinal of the step just written.</returns>
    public AssertionResult Settled(TraceStep step, AssertionResult result)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(result);

        var written = Write(step with { Asserted = result.Name });
        return result.At(written.Step);
    }

    /// <summary>Closes the file where this writer opened one, and nothing where it did not.</summary>
    public void Dispose()
    {
        if (ownsWriter)
            writer.Dispose();
    }
}

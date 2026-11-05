using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Winwright.Tracing;

/// <summary>
/// How a step is spelled on disk. One JSON object per line, camel-cased, nulls left out and no
/// indentation — the reader is usually an agent, and a viewer nobody can grep is a viewer nobody
/// opens. System.Text.Json is in-box, so the trace costs an adopting project no package.
/// </summary>
public static class TraceFormat
{
    /// <summary>The one options instance the writer and the reader share, so they cannot disagree.</summary>
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        WriteIndented = false,

        // The default encoder escapes an apostrophe to ', and window titles and control names
        // are full of them: `Save as...`, `Claude's window`. A trace whose text does not survive a
        // grep for what the scenario wrote is the viewer nobody opens. This goes to a file beside
        // the run and is never interpolated into markup, which is what makes relaxing it safe.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>One step as the single line it occupies. Never contains a newline.</summary>
    public static string Line(TraceStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        return JsonSerializer.Serialize(step, Options);
    }

    /// <summary>Read one line back. The inverse of <see cref="Line"/>, and tested as one.</summary>
    public static TraceStep Parse(string line)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(line);
        return JsonSerializer.Deserialize<TraceStep>(line, Options)
            ?? throw new FormatException($"not a trace step: {line}");
    }
}

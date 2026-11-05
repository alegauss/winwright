using System.Text.Json;
using System.Text.Json.Serialization;

namespace Winwright.Verdicts;

/// <summary>
/// Where an answer came from, in a form a reader can check without opening anything.
/// <para>
/// An answer an agent cannot audit gets audited by reading the file — which is the cost the verb
/// existed to remove. Reading a backlog end to end to find one ready task cost about five thousand
/// tokens in the repository this rule came from, and the same arithmetic holds here: the
/// alternative to a verb that says <c>strings.en.json:12</c> is a reader opening the strings file
/// to find out whether the expectation was derived from anything at all.
/// </para>
/// <para>
/// Two shapes, because a reading comes from one of two places. A value read out of a file has a
/// path, a line and the key it sat under. A value read off the window has the locator step that
/// addresses the element and the pattern it was read through. Neither is prose: both are fields,
/// so the trace carries them and a report renders them rather than describing them.
/// </para>
/// </summary>
public sealed record Provenance
{
    /// <summary>Construct one from its fields. Public so the trace reader can rebuild what was written.</summary>
    [JsonConstructor]
    public Provenance(string? file, int line, string? key, string? element, string? pattern)
    {
        File = Trimmed(file);
        Line = Math.Max(0, line);
        Key = Trimmed(key);
        Element = Trimmed(element);
        Pattern = Trimmed(pattern);
    }

    /// <summary>The file the value was read out of, absolute. Null where the source was the window.</summary>
    public string? File { get; }

    /// <summary>The line it is declared on, counted from 1. Zero where the file is known and the line is not.</summary>
    public int Line { get; }

    /// <summary>The key inside the file, dotted for a nested one.</summary>
    public string? Key { get; }

    /// <summary>The locator step that addresses the element it was read off.</summary>
    public string? Element { get; }

    /// <summary>The UI Automation pattern it was read through, where one was used.</summary>
    public string? Pattern { get; }

    /// <summary>Nothing is known about where this came from. The one honest answer when there is none.</summary>
    public static Provenance Unknown { get; } = new(null, 0, null, null, null);

    /// <summary>Whether this says anything at all.</summary>
    public bool Known => File is not null || Element is not null;

    /// <summary>A value read out of a file, at a line, under a key.</summary>
    /// <param name="file">The file. Recorded as a full path, so two runs on two machines name the same thing.</param>
    /// <param name="line">The line, counted from 1. Zero says the file is known and the line is not.</param>
    /// <param name="key">The key it sat under, where the file has keys.</param>
    public static Provenance InFile(string file, int line = 0, string? key = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);
        return new Provenance(Path.GetFullPath(file.Trim()), line, key, null, null);
    }

    /// <summary>A value read off an element, through a pattern where one was used.</summary>
    /// <param name="element">The locator step that addresses it, so the reader can go and look.</param>
    /// <param name="pattern">The UI Automation pattern it was read through.</param>
    public static Provenance OnElement(string element, string? pattern = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(element);
        return new Provenance(null, 0, null, element, pattern);
    }

    /// <summary>
    /// The one phrase a sentence names it by: <c>strings.en.json:12 'tabs.logs'</c>, or the locator
    /// step and the pattern behind it. Short on purpose — a report carries one of these per value.
    /// </summary>
    public override string ToString()
    {
        if (File is not null)
        {
            var at = Line > 0 ? $"{Path.GetFileName(File)}:{Line}" : Path.GetFileName(File);
            return Key is null ? at : $"{at} '{Key}'";
        }

        if (Element is null)
            return "(source unrecorded)";

        return Pattern is null ? Element : $"{Element} via {Pattern}";
    }

    /// <summary>
    /// The machine-readable form: one JSON object, camel-cased, absent fields left out — the same
    /// spelling the trace uses, because two spellings of one fact are two facts that can disagree.
    /// </summary>
    public string Json() => JsonSerializer.Serialize(this, Tracing.TraceFormat.Options);

    private static string? Trimmed(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

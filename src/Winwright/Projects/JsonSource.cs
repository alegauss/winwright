using System.Text.Json;

using Winwright.Verdicts;

namespace Winwright.Projects;

/// <summary>
/// Where a key sits in a JSON file, by line.
/// <para>
/// A path alone is not provenance a reader can act on: a strings file with four hundred keys in it
/// names the file for every one of them, which is a reader opening it and searching anyway. The
/// line is what turns "derived from the project's strings" into a claim that can be checked in one
/// jump.
/// </para>
/// <para>
/// Read with <see cref="Utf8JsonReader"/> rather than with a document, because a document throws
/// the positions away as it builds the tree — and a line counted from a byte offset is the only
/// number that stays true through a file that mixes tabs, spaces and both line endings.
/// </para>
/// </summary>
public static class JsonSource
{
    /// <summary>
    /// The string under <paramref name="key"/>, or null where the file declares none.
    /// <para>
    /// Nested first and then flat, because a strings file comes in both shapes and a project using
    /// one should not have to say which. Lifted here from the label reader when the destructive
    /// guard needed the same answer: two copies of how this project reads a strings file would be
    /// two readers that drift, and the second would drift silently.
    /// </para>
    /// </summary>
    /// <param name="file">The JSON file.</param>
    /// <param name="key">The key, dotted for a nested one.</param>
    /// <exception cref="IOException">Where the file cannot be read.</exception>
    /// <exception cref="JsonException">Where it is not JSON.</exception>
    public static string? Value(string file, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        using var document = JsonDocument.Parse(
            File.ReadAllText(file),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

        var root = document.RootElement;
        var walked = root;
        foreach (var step in key.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (walked.ValueKind != JsonValueKind.Object || !walked.TryGetProperty(step, out walked))
                return Flat(root, key);
        }

        return walked.ValueKind == JsonValueKind.String ? walked.GetString() : Flat(root, key);
    }

    private static string? Flat(JsonElement root, string key) =>
        root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty(key.Trim(), out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

    /// <summary>
    /// The line <paramref name="key"/> is declared on, counted from 1. Zero where the file is not
    /// there, does not parse, or declares no such key — an absence rather than a throw, since a
    /// provenance nobody could read is still an answer about where a value came from.
    /// </summary>
    /// <param name="file">The JSON file.</param>
    /// <param name="key">The key, dotted for a nested one. Matched nested first, then flat.</param>
    public static int LineOf(string file, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return LinesOf(file, [key.Trim()]).GetValueOrDefault(key.Trim());
    }

    /// <summary>
    /// The lines every one of <paramref name="keys"/> is declared on, in one pass over the file. A
    /// key the file does not declare is left out rather than recorded as zero, so a caller can tell
    /// "not in this file" from "in it, at a line nothing could number".
    /// </summary>
    /// <param name="file">The JSON file.</param>
    /// <param name="keys">The keys, dotted for nested ones.</param>
    public static IReadOnlyDictionary<string, int> LinesOf(string file, IReadOnlyCollection<string> keys)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);
        ArgumentNullException.ThrowIfNull(keys);

        var found = new Dictionary<string, int>(StringComparer.Ordinal);
        if (keys.Count == 0)
            return found;

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(Path.GetFullPath(file.Trim()));
        }
        catch (Exception unreadable) when (unreadable is IOException or UnauthorizedAccessException)
        {
            return found;
        }

        var wanted = new HashSet<string>(keys, StringComparer.Ordinal);
        var path = new List<string>();
        var reader = new Utf8JsonReader(
            bytes,
            new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

        try
        {
            while (reader.Read())
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                var name = reader.GetString() ?? "";
                Descend(path, name, reader.CurrentDepth);

                // The dotted walk and the flat name both, because a strings file may spell a key
                // either way and DerivedSet already reads whichever one the file used.
                var dotted = string.Join('.', path);
                foreach (var key in Matching(wanted, dotted, name))
                    found.TryAdd(key, LineAt(bytes, reader.TokenStartIndex));
            }
        }
        catch (JsonException)
        {
            // A file that does not parse has no line to offer, and saying so is the caller's job:
            // DerivedSet refuses on the same file with the parser's own message, which is better
            // than a line number invented from a half-read token stream.
            return found;
        }

        return found;
    }

    /// <summary>The provenance of a key in a file, line included where the file yields one.</summary>
    public static Provenance Of(string file, string key) => Provenance.InFile(file, LineOf(file, key), key);

    private static IEnumerable<string> Matching(HashSet<string> wanted, string dotted, string name)
    {
        if (wanted.Contains(dotted))
            yield return dotted;

        if (!string.Equals(dotted, name, StringComparison.Ordinal) && wanted.Contains(name))
            yield return name;
    }

    private static void Descend(List<string> path, string name, int depth)
    {
        var at = Math.Max(0, depth - 1);
        if (path.Count > at)
            path.RemoveRange(at, path.Count - at);

        while (path.Count < at)
            path.Add("");

        path.Add(name);
    }

    private static int LineAt(ReadOnlySpan<byte> bytes, long offset)
    {
        var upTo = (int)Math.Min(offset, bytes.Length);
        var line = 1;
        for (var index = 0; index < upTo; index++)
        {
            if (bytes[index] == (byte)'\n')
                line++;
        }

        return line;
    }
}

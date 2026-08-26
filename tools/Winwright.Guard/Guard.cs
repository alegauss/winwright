using System.Collections.ObjectModel;
using System.Text.Json.Nodes;

using Winwright.Scenarios;

namespace Winwright.Guarding;

/// <summary>What the guard decided about one write.</summary>
/// <param name="Denied">Whether the write is refused.</param>
/// <param name="Because">The refusal, naming the verb that replaces the script. Empty where allowed.</param>
public sealed record Verdict(bool Denied, string Because = "")
{
    /// <summary>The answer to everything this guard has no opinion about, which is nearly everything.</summary>
    public static Verdict Allowed { get; } = new(false);
}

/// <summary>
/// What the guard needs to know about a path it was handed: the project that owns it, or null.
/// <para>
/// Injected rather than looked up, so the suite decides on paths that do not exist. A guard whose
/// judgements can only be exercised by building a tree on disk is a guard whose judgements are
/// checked as often as somebody builds one.
/// </para>
/// </summary>
/// <param name="path">The file being written.</param>
/// <returns>The text of the nearest project file above it, or null where there is none.</returns>
public delegate string? Owning(string path);

/// <summary>
/// The guard that makes the case file the easy path.
/// <para>
/// WW67. A hand-written harness script is always available and always faster in the moment, and that
/// is exactly how 2,732 lines happen. Nothing about the tool stops it: the engine is a library, and
/// a script that drives a window through <see cref="Winwright.Acting"/> compiles and runs and is
/// wrong for every reason this project exists — the vocabulary is whatever the author remembered, the
/// retries are whatever they wrote, and a check that could not run is a green.
/// </para>
/// <para>
/// So the refusal arrives before the work rather than after it, which is the same shape roadkeep puts
/// in front of a governed file. A linter reporting afterwards asks the author to delete what they
/// just wrote; a deny at the point of insertion asks them to write the other thing first.
/// </para>
/// <para>
/// Two rules keep it from making anything worse. It never denies a write to a case file — that is the
/// verb, and a guard standing in front of its own replacement is a guard nobody keeps. And it never
/// denies inside the engine's own tree: the suite here drives windows on purpose, and a guard that
/// cannot tell the tool from a use of it would have to be turned off to work on the tool.
/// </para>
/// </summary>
public static class Guard
{
    /// <summary>
    /// The namespaces a window cannot be driven without. Content naming one of these is a script
    /// driving an application, whatever the file is called — which is the point, because a name is
    /// the one thing an author can change to get past a guard without changing what they wrote.
    /// </summary>
    public static IReadOnlyList<string> Driving { get; } = new ReadOnlyCollection<string>(
    [
        "Winwright.Acting",
        "Winwright.Locating",
        "Winwright.Asserting",
    ]);

    /// <summary>The tools a write arrives through.</summary>
    public static IReadOnlyList<string> Writing { get; } = new ReadOnlyCollection<string>(
    [
        "Write",
        "Edit",
        "MultiEdit",
    ]);

    /// <summary>What the engine's own projects are recognised by, which is a reference to its source.</summary>
    public const string Engine = "Winwright.csproj";

    /// <summary>
    /// Judge one hook call.
    /// </summary>
    /// <param name="call">What the harness sent, already parsed.</param>
    /// <param name="owning">How to find the project above a path.</param>
    /// <returns>
    /// The verdict. Anything this cannot read is allowed: a hook that denies what it did not
    /// understand is a hook whose first false deny gets it removed, and then nothing is guarded.
    /// </returns>
    public static Verdict On(JsonObject call, Owning owning)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(owning);

        if (call["tool_input"] is not JsonObject input)
            return Verdict.Allowed;

        var tool = call["tool_name"]?.GetValue<string>() ?? "";
        if (!Writing.Contains(tool, StringComparer.Ordinal))
            return Verdict.Allowed;

        var path = input["file_path"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(path))
            return Verdict.Allowed;

        // The verb this exists to make the easy path. Never in its own way.
        if (path.EndsWith(ScenarioFile.Extension, StringComparison.OrdinalIgnoreCase))
            return Verdict.Allowed;

        var written = Written(input);
        var driving = Driving.Where(one => written.Contains(one, StringComparison.Ordinal)).ToList();
        if (driving.Count == 0)
            return Verdict.Allowed;

        // The engine's own tree drives windows on purpose. Recognised by a reference to the engine's
        // source and not by a path, because a path is this repository's shape and the guard has to
        // read the same in a clone that vendored it somewhere else.
        var project = owning(path);
        if (project is not null && project.Contains(Engine, StringComparison.Ordinal))
            return Verdict.Allowed;

        return new Verdict(true, Because(path, driving));
    }

    /// <summary>The project above <paramref name="path"/> on this disk, or null where there is none.</summary>
    public static string? Nearest(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var walking = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(path.Trim())) ?? ".");
        while (walking is not null)
        {
            var found = walking.Exists ? walking.GetFiles("*.csproj") : [];
            if (found.Length > 0)
            {
                try
                {
                    return string.Join('\n', found.Select(one => File.ReadAllText(one.FullName)));
                }
                catch (Exception unreadable) when (unreadable is IOException or UnauthorizedAccessException)
                {
                    return null;
                }
            }

            walking = walking.Parent;
        }

        return null;
    }

    /// <summary>The refusal, which has to name the verb or it is only an obstacle.</summary>
    private static string Because(string path, IReadOnlyList<string> driving)
    {
        var named = Path.GetFileName(path);
        return $"'{named}' drives a window from a script: it names {string.Join(" and ", driving)}. "
            + $"A case is a data file the engine runs, not a script an author repeats — declare the "
            + $"steps in a '{ScenarioFile.Extension}' file instead, where each one is a locator, an "
            + $"act and what the reading should be, and the engine owns the waits, the retries and "
            + $"the third verdict for a check that could not run. Ask 'winwright_format' what a case "
            + $"may say and 'winwright_check' whether the one you wrote loads, before the file "
            + $"exists. Writing the script anyway means writing the vocabulary, the retries and the "
            + $"reporting again, and a check that could not run reads as a pass.";
    }

    /// <summary>What this call would put in the file — the whole of it, or whatever an edit adds.</summary>
    private static string Written(JsonObject input)
    {
        var written = new List<string>();
        if (input["content"]?.GetValue<string>() is { } content)
            written.Add(content);

        if (input["new_string"]?.GetValue<string>() is { } replacement)
            written.Add(replacement);

        // MultiEdit carries its replacements one level down, and reading only the top level is how a
        // guard passes the write it was registered for.
        if (input["edits"] is JsonArray edits)
        {
            written.AddRange(edits
                .OfType<JsonObject>()
                .Select(one => one["new_string"]?.GetValue<string>())
                .OfType<string>());
        }

        return string.Join('\n', written);
    }
}

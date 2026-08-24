using System.Collections.ObjectModel;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// The checkout this suite is running out of: where it is, and what source files are in it.
/// <para>
/// WW193. Every case that reads a file walks up from <c>AppContext.BaseDirectory</c> looking for the
/// solution file, and that loop was written out eighteen times across sixteen files — spelled with
/// three different variable names, which is how a reader misses that they are the same four lines.
/// Four of them go on to enumerate <c>*.cs</c> and skip <c>bin</c> and <c>obj</c>.
/// </para>
/// <para>
/// Extracted because of how the first one went. <c>Deadlines</c> shipped recursing into <c>bin</c>
/// and <c>obj</c> — thousands of files instead of two hundred, inside a suite whose other cases are
/// waiting on five-second deadlines — and the guest went red twice with two different timing
/// failures before the cause was found. The run went 2m50s to 3m53s and back. That was one copy
/// getting one exclusion wrong, and it was also a correctness fault: a stale copy under <c>bin</c>
/// is an entry in a catalogue for a file nobody has.
/// </para>
/// <para>
/// The walk and never the question. Each catalogue keeps its own rule about what it is looking for;
/// a shared answer would be the opposite of what this is for.
/// </para>
/// </summary>
internal static class Checkout
{
    /// <summary>What marks the root. The solution, because that is what a checkout has one of.</summary>
    internal const string Marker = "Winwright.slnx";

    /// <summary>Where the repository is, walked up from where the suite is running.</summary>
    internal static string Root => root.Value;

    /// <summary>A path inside it, joined the way the platform spells one.</summary>
    /// <param name="parts">The segments under the root, e.g. <c>docs</c> then <c>ROADMAP.md</c>.</param>
    internal static string At(params string[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        return Path.Combine([Root, .. parts]);
    }

    /// <summary>The engine's sources, which is one of the two trees anything here reads.</summary>
    internal static string Engine => At("src");

    /// <summary>This suite's own, which is the other.</summary>
    internal static string Suite => At("tests");

    /// <summary>
    /// Both, for a catalogue whose question is about the whole repository.
    /// <para>
    /// Computed rather than initialised, and that is not a preference. A static field initialiser
    /// here runs before the one holding the walk, which is declared lower down — so this answered a
    /// null reference in every case that reads a file, and the guest said so.
    /// </para>
    /// </summary>
    internal static IReadOnlyList<string> Everything =>
        new ReadOnlyCollection<string>([Engine, Suite]);

    /// <summary>
    /// Every C# source under those trees, and never what a build left beside them.
    /// </summary>
    /// <param name="trees">Which trees to walk.</param>
    /// <param name="except">
    /// A file name to leave out — a catalogue that spells the thing it searches for would otherwise
    /// find it in itself, and report the naming as a use.
    /// </param>
    internal static IEnumerable<string> Sources(IEnumerable<string> trees, string? except = null)
    {
        ArgumentNullException.ThrowIfNull(trees);

        return trees
            .SelectMany(one => Directory.EnumerateFiles(one, "*.cs", SearchOption.AllDirectories))
            .Where(Written)
            .Where(one => except is null || !string.Equals(Path.GetFileName(one), except, StringComparison.Ordinal));
    }

    /// <summary>The same over one tree, which is what most of them want.</summary>
    /// <param name="tree">The tree to walk.</param>
    /// <param name="except">A file name to leave out.</param>
    internal static IEnumerable<string> SourcesIn(string tree, string? except = null) =>
        Sources([tree], except);

    /// <summary>
    /// One line of source with its quoted text taken out, so a call named as data is not read as a
    /// call made.
    /// <para>
    /// WW191 found this in one scanner: a case asserting that <c>NotificationArea.OpenOverflow(</c>
    /// is among the calls a catalogue sweeps for was reported as a case that opens the overflow. The
    /// fragment was in a string, which is the one place in a source file where a call is a subject
    /// rather than an act.
    /// </para>
    /// <para>
    /// WW197 found it again in a second scanner, on the same file, and that is why it is here rather
    /// than in either. A catalogue of calls holds every call it knows about as text, so any sweep
    /// reading it raw finds all of them at once.
    /// </para>
    /// <para>
    /// A line whose quotes do not pair is left whole. A raw literal opens on one line and shuts on
    /// another, and stripping from an unmatched quote to the end of the line would delete real code
    /// — which turns a call that was made into one that appears not to be, and a sweep goes quiet
    /// about it. Reading too much is a red somebody answers; reading too little is not.
    /// </para>
    /// </summary>
    /// <param name="line">The line as the file spells it.</param>
    internal static string Code(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        return Uncommented(Unquoted(line));
    }

    /// <summary>
    /// The line with what a person wrote about it taken off.
    /// <para>
    /// Found by WW197 on the doc comment of this very method, which names a call in a <c>see</c> tag
    /// to explain what it does — and a sweep reading comments reported three helpers that touch
    /// nothing as reaching for it. Prose about a call is the other place a call is a subject rather
    /// than an act, and every catalogue here explains itself in prose.
    /// </para>
    /// <para>
    /// After the quotes and never before, so a <c>//</c> inside a string has already gone and cannot
    /// take the rest of a real line with it.
    /// </para>
    /// </summary>
    private static string Uncommented(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("//", StringComparison.Ordinal) || trimmed.StartsWith('*'))
            return "";

        var at = line.IndexOf("//", StringComparison.Ordinal);
        return at < 0 ? line : line[..at];
    }

    /// <summary>The line with its quoted text taken off.</summary>
    private static string Unquoted(string line)
    {
        var quotes = line.Count(one => one == '"');
        if (quotes == 0 || quotes % 2 != 0)
            return line;

        var kept = new System.Text.StringBuilder(line.Length);
        var inside = false;
        foreach (var letter in line)
        {
            if (letter == '"')
            {
                inside = !inside;
                continue;
            }

            if (!inside)
                kept.Append(letter);
        }

        return kept.ToString();
    }

    /// <summary>
    /// Whether a path is a source somebody wrote rather than a copy a build made. Matched on the
    /// separators either side, so a directory called <c>binding</c> is not mistaken for output.
    /// </summary>
    /// <param name="path">The file.</param>
    internal static bool Written(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var separator = Path.DirectorySeparatorChar;
        return !path.Contains($"{separator}bin{separator}", StringComparison.Ordinal)
            && !path.Contains($"{separator}obj{separator}", StringComparison.Ordinal);
    }

    private static readonly Lazy<string> root = new(Walk);

    private static string Walk()
    {
        var walking = new DirectoryInfo(AppContext.BaseDirectory);
        while (walking is not null && !File.Exists(Path.Combine(walking.FullName, Marker)))
            walking = walking.Parent;

        Assert.NotNull(walking);
        return walking.FullName;
    }
}

using Winwright.Projects;

namespace Winwright.Concordance;

/// <summary>
/// One copy of the version that is a file in this tree, and can therefore be raised as well as read.
/// <para>
/// WW239. The release workflow kept its own list of where the version lives — five paths in YAML —
/// and the check kept another, four flags on a command line. Neither owned the enumeration, and the
/// first of them was wrong on its first run, which is how the fifth copy was found. A sixth added
/// tomorrow would reach neither.
/// </para>
/// <para>
/// So a copy that came from a file carries the file. The same invocation that names the copies to
/// compare is the one that raises them, and a copy nobody added to it is a copy neither half knows
/// about — which is one list rather than two that agree by hand.
/// </para>
/// </summary>
/// <param name="Where">What this copy is, as a report names it.</param>
/// <param name="Path">The file it was read out of.</param>
public sealed record WritableCopy(string Where, string Path)
{
    /// <summary>
    /// Rewrite this file's version to <paramref name="version"/>, or say why nothing was written.
    /// <para>
    /// The string replaced is the one this copy was <em>read</em> as, rather than one the caller
    /// passed in. That is the whole difference from the workflow step this replaces: it was told the
    /// old version and threw where a file did not mention it, so a file holding a different version
    /// from the rest was a file it refused to touch rather than one it corrected. Reading each copy
    /// for itself means a tree that had already drifted is raised into agreement rather than stuck.
    /// </para>
    /// <para>
    /// Every occurrence, because a file may show the reference more than once — the README shows it
    /// twice, once per package — and rewriting the first would leave the file disagreeing with
    /// itself, which <see cref="Engine.Documented"/> then reports as unpinnable.
    /// </para>
    /// </summary>
    /// <param name="was">The version this copy currently reads as.</param>
    /// <param name="version">What it should read as.</param>
    /// <returns>What happened, as the one line a report shows.</returns>
    public string Raise(string was, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(was);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        if (string.Equals(was, version, StringComparison.Ordinal))
            return $"{Where} already reads {version}";

        var text = File.ReadAllText(Path);
        var raised = text.Replace(was, version, StringComparison.Ordinal);
        if (string.Equals(text, raised, StringComparison.Ordinal))
        {
            // It read as `was` and does not contain it, which means the reader and the file disagree
            // about what this copy says. Refused rather than reported as done: a raise that wrote
            // nothing and said it did is the half-done sequence this whole task is about.
            return $"{Where} reads {was} and the file does not contain it, so nothing was written";
        }

        File.WriteAllText(Path, raised);
        return $"{Where}: {was} -> {version}";
    }

    /// <summary>Whether a line names something that was actually written.</summary>
    /// <param name="said">A line <see cref="Raise"/> returned.</param>
    public static bool Wrote(string said) => said?.Contains(" -> ", StringComparison.Ordinal) ?? false;

    /// <summary>Whether a line names a refusal rather than a raise or a no-op.</summary>
    /// <param name="said">A line <see cref="Raise"/> returned.</param>
    public static bool Refused(string said) =>
        said?.Contains("nothing was written", StringComparison.Ordinal) ?? false;
}

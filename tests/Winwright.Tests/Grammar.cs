using System.Collections.ObjectModel;

using Xunit;

namespace Winwright.Tests;

/// <summary>One row of a grammar block: a locator, and what it addresses.</summary>
/// <param name="Written">The locator as the block spells it.</param>
/// <param name="Addresses">What the row says it addresses.</param>
internal sealed record GrammarForm(string Written, string Addresses)
{
    public override string ToString() => $"{Written,-38} {Addresses}";
}

/// <summary>
/// The blocks that state the locator grammar, wherever they are written. WW499.
/// <para>
/// There are two: the <c>code</c> block in <see cref="Winwright.Locating.Locator" />'s own remarks,
/// which the documentation area publishes and which <c>LocatorTests</c> parses row by row, and the
/// fenced one in the README, which is what an adopter reads on nuget.org before cloning anything.
/// They had already drifted — fourteen rows against thirteen — and nothing said so.
/// </para>
/// <para>
/// The walk is shared and the question never is, which is WW193's rule one file over. This reads a
/// block; which block, and what has to be true of it, belongs to whoever asks. Two readers rather
/// than one because the blocks are written differently — a doc comment carries <c>///</c> and
/// escapes its angle brackets, and a fence carries neither — and the difference is the file's,
/// not the grammar's.
/// </para>
/// </summary>
internal static class Grammar
{
    /// <summary>The forms the parser's own summary states, which is the block the site publishes.</summary>
    internal static IReadOnlyList<GrammarForm> Stated() =>
        Rows(File.ReadAllText(Checkout.At("src", "Winwright", "Locating", "Locator.cs")), "<code>", "</code>");

    /// <summary>
    /// The forms the README shows, which is the block an adopter reads before they have a
    /// checkout. Found under the heading rather than by position: the file is a thousand lines and
    /// a fence counted from the top is one a new section moves.
    /// </summary>
    internal static IReadOnlyList<GrammarForm> Published()
    {
        var readme = File.ReadAllText(Checkout.At("README.md"));
        var section = readme.IndexOf("## Addressing an element", StringComparison.Ordinal);
        Assert.True(section >= 0, "the README no longer has an 'Addressing an element' section to read the grammar out of");

        return Rows(readme[section..], "```", "```");
    }

    /// <summary>
    /// The rows of one block, as two columns split on the alignment between them.
    /// </summary>
    /// <param name="text">The file, or enough of it to hold the block.</param>
    /// <param name="opens">What opens the block.</param>
    /// <param name="shuts">What closes it.</param>
    private static IReadOnlyList<GrammarForm> Rows(string text, string opens, string shuts)
    {
        var from = text.IndexOf(opens, StringComparison.Ordinal);
        Assert.True(from >= 0, $"no {opens} opens a grammar block here");

        var to = text.IndexOf(shuts, from + opens.Length, StringComparison.Ordinal);
        Assert.True(to > from, $"the grammar block opened by {opens} is never closed");

        var found = new List<GrammarForm>();
        foreach (var raw in text[(from + opens.Length)..to].Split('\n'))
        {
            // A doc comment carries `///` and a fence does not, so the trim is unconditional: a
            // locator never opens with a slash, and one that did would show up as a missing row
            // rather than as a wrong page.
            var line = raw.Trim().TrimStart('/').Trim();
            if (line.Length == 0)
                continue;

            var columns = line.Split("  ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Assert.Equal(2, columns.Length);

            // The one entity a doc comment needs, since the descendant operator inside XML is
            // escaped. A fence has none and this leaves it alone.
            found.Add(new GrammarForm(columns[0].Replace("&gt;", ">", StringComparison.Ordinal), columns[1]));
        }

        Assert.NotEmpty(found);
        return new ReadOnlyCollection<GrammarForm>(found);
    }
}

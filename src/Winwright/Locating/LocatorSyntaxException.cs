namespace Winwright.Locating;

/// <summary>
/// A locator that does not parse. It carries the position as well as the reason, because a
/// refusal that says only "bad locator" sends the reader back to count characters — and this text
/// came out of a scenario file, where the next thing they will do is find the column.
/// </summary>
public sealed class LocatorSyntaxException : Exception
{
    /// <param name="locator">The text as it was written.</param>
    /// <param name="position">The zero-based offset the refusal is about.</param>
    /// <param name="because">What is wrong, in the sentence the author has to act on.</param>
    public LocatorSyntaxException(string locator, int position, string because)
        : base($"{locator}\n{new string(' ', Math.Max(0, position))}^ {because}")
    {
        Locator = locator;
        Position = position;
        Because = because;
    }

    /// <summary>The text as it was written.</summary>
    public string Locator { get; }

    /// <summary>The zero-based offset the refusal is about.</summary>
    public int Position { get; }

    /// <summary>What is wrong.</summary>
    public string Because { get; }
}

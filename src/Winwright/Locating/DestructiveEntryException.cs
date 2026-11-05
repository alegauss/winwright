namespace Winwright.Locating;

/// <summary>
/// An act was about to reach an entry this project declared destructive, and the scenario never
/// said it meant that one.
/// <para>
/// The refusal names the entry rather than the rule, because a reader who hits this is either
/// about to write <c>MeaningIt()</c> deliberately or has just discovered that the control their
/// locator matched is the one that ends the run. Both need to know which entry it was.
/// </para>
/// </summary>
public sealed class DestructiveEntryException : Exception
{
    /// <param name="locator">The locator the act was about, as it was written.</param>
    /// <param name="entry">The declared entry it matched.</param>
    /// <param name="element">What was actually there, as a report names it.</param>
    public DestructiveEntryException(string locator, string entry, string element)
        : base($"{locator} is \"{entry}\", which this project declares destructive: "
            + $"{element} will not be acted on unless the scenario says MeaningIt()")
    {
        Locator = locator;
        Entry = entry;
        Element = element;
    }

    /// <summary>The locator the act was about.</summary>
    public string Locator { get; }

    /// <summary>The declared entry it matched, spelled as the project declared it.</summary>
    public string Entry { get; }

    /// <summary>What was there, as a report names it.</summary>
    public string Element { get; }
}

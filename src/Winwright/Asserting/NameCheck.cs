using System.Globalization;
using System.Text;

using Winwright.Locating;
using Winwright.Verdicts;

namespace Winwright.Asserting;

/// <summary>What a control's name turned out to be, once "non-empty" stops being the question.</summary>
public enum Named
{
    /// <summary>A label. The only one of these a screen reader can read out.</summary>
    Spoken,

    /// <summary>Nothing at all, which is what a control with no content of its own reports.</summary>
    Missing,

    /// <summary>A font glyph: drawn by the icon font and silent to everything else.</summary>
    Glyph,

    /// <summary>Its own automation id handed back, which is a developer's name and not a person's.</summary>
    EchoesTheId,

    /// <summary>A template nobody filled in, which is a defect in the window rather than a label.</summary>
    Templated,
}

/// <summary>
/// Whether a control's name is its label.
/// <para>
/// A placeholder, an automation id echoed back or a glyph codepoint all satisfy non-empty and none
/// of them is what a screen reader should say. claude-tray found two controls carrying empty names
/// while every neighbouring button read fine, because a control derives its name from its own
/// content and both of those had none — one's label was a separate text block, the other's content
/// was a font glyph.
/// </para>
/// <para>
/// The printing matters as much as the verdict. A name the console cannot draw is printed as
/// escapes, or the worst case in the whole check — a control announcing <c>U+E711</c> and nothing
/// else — arrives in a report looking exactly like the empty case it is not.
/// </para>
/// </summary>
public sealed record NameCheck
{
    internal NameCheck(string? name, string? automationId, Named Verdict)
    {
        Name = name;
        AutomationId = automationId;
        this.Verdict = Verdict;
    }

    /// <summary>The name as the tree reports it, which may be unprintable or absent.</summary>
    public string? Name { get; }

    /// <summary>The automation id it was compared against, where there was one.</summary>
    public string? AutomationId { get; }

    /// <summary>What the name turned out to be.</summary>
    public Named Verdict { get; }

    /// <summary>Whether a screen reader would read this control's own name and say something useful.</summary>
    public bool IsALabel => Verdict == Named.Spoken;

    /// <summary>
    /// The name with everything a console cannot draw written out as an escape. This is what a
    /// report prints, always — a glyph name shown raw is indistinguishable from no name at all,
    /// and that confusion is half the defect.
    /// </summary>
    public string Printable => Names.Printable(Name);

    /// <summary>Why this name is or is not a label, in the words a red step carries.</summary>
    public string Sentence(string named) => Verdict switch
    {
        Named.Spoken => $"{named} is named \"{Printable}\".",
        Named.Missing => $"{named} has no name at all: a control takes its name from its own content, and this "
            + "one has none — its label is likely a separate text block beside it.",
        Named.Glyph => $"{named} is named \"{Printable}\", which is a font glyph and not a label: it satisfies "
            + "every check for a non-empty name and a screen reader reads nothing.",
        Named.EchoesTheId => $"{named} is named \"{Printable}\", which is its own automation id handed back "
            + "rather than a label anybody wrote.",
        _ => $"{named} is named \"{Printable}\", which is a template nobody filled in.",
    };

    /// <summary>The result a verdict counts, carrying that sentence as its detail.</summary>
    public AssertionResult AsAssertion(string named) =>
        IsALabel ? AssertionResult.Pass(named, Sentence(named)) : AssertionResult.Fail(named, Sentence(named));
}

/// <summary>Reading a control's name for what it is rather than for whether it is there.</summary>
public static class Names
{
    /// <summary>
    /// Classify a name. A null or blank one is <see cref="Named.Missing"/> rather than a refusal:
    /// it is a fact about the window, and the whole point is to report it as one.
    /// </summary>
    /// <param name="name">The name the tree reports.</param>
    /// <param name="automationId">Its automation id, where it has one, to catch the echo.</param>
    public static NameCheck Of(string? name, string? automationId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new NameCheck(name, automationId, Named.Missing);

        // Before the echo, because a control whose id is a glyph is still a glyph — and after
        // the blank, because nothing else has anything to say about a name that is not there.
        if (IsAllGlyph(name))
            return new NameCheck(name, automationId, Named.Glyph);

        if (!string.IsNullOrWhiteSpace(automationId)
            && string.Equals(name.Trim(), automationId.Trim(), StringComparison.Ordinal))
        {
            return new NameCheck(name, automationId, Named.EchoesTheId);
        }

        return Labels.CarriesAPlaceholder(name)
            ? new NameCheck(name, automationId, Named.Templated)
            : new NameCheck(name, automationId, Named.Spoken);
    }

    /// <summary>The same, for an element this run already read.</summary>
    public static NameCheck Of(ElementFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return Of(facts.Name, facts.AutomationId);
    }

    /// <summary>
    /// Write a name out so a console can print it: everything it cannot draw becomes an escape,
    /// and nothing else changes. Null and empty are told apart, because a report that prints both
    /// as nothing is a report where the two claude-tray controls looked identical.
    /// </summary>
    public static string Printable(string? text)
    {
        if (text is null)
            return "(no name)";

        if (text.Length == 0)
            return "";

        var built = new StringBuilder(text.Length);
        foreach (var rune in text.EnumerateRunes())
        {
            if (Drawable(rune))
            {
                built.Append(rune);
                continue;
            }

            built.Append(rune.Value <= 0xFFFF
                ? $"\\u{rune.Value:X4}"
                : $"\\U{rune.Value:X8}");
        }

        return built.ToString();
    }

    private static bool IsAllGlyph(string name)
    {
        var glyphs = 0;
        foreach (var rune in name.EnumerateRunes())
        {
            if (Rune.IsWhiteSpace(rune))
                continue;

            // A name is a glyph only where every non-space rune in it is one. "Save " is a
            // label with an icon after it, and refusing that would be refusing a real name.
            if (!Undrawable(rune))
                return false;

            glyphs++;
        }

        return glyphs > 0;
    }

    private static bool Drawable(Rune rune) => !Undrawable(rune) || Rune.IsWhiteSpace(rune);

    private static bool Undrawable(Rune rune) => Rune.GetUnicodeCategory(rune) switch
    {
        // Private use is the one that matters: Segoe MDL2 and Fluent put every icon in U+E000
        // upwards, so a button whose content is an icon announces a codepoint from that block.
        UnicodeCategory.PrivateUse => true,
        UnicodeCategory.Control => true,
        UnicodeCategory.Format => true,
        UnicodeCategory.Surrogate => true,
        UnicodeCategory.OtherNotAssigned => true,
        _ => false,
    };
}

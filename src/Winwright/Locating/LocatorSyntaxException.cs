namespace Winwright.Locating;

/// <summary>
/// The ways a locator can fail to parse, each of which a reader meets as its own refusal.
/// <para>
/// WW196. WW188 gave the capture refusal an arm it declares, because a reader meets an arm rather
/// than a type and a catalogue keyed on the type counted six refusals as one. This type is thrown
/// from thirteen places and was one entry naming one case — so twelve of these could stop working
/// and the pairing would go on saying the type is covered.
/// </para>
/// <para>
/// Thirteen and not fewer, and the grouping is the judgement rather than a step before it. An index
/// that is not a number and an index below one are two arms: both are about the same predicate, and
/// what the author does about each is different. A control type and a pattern this vocabulary does
/// not have are two arms for the same reason — the nearest words offered come from different lists.
/// A predicate with no <c>=</c> and one with no <c>]</c> are two, because one is a malformed
/// predicate and the other is an unfinished one.
/// </para>
/// </summary>
public enum LocatorFault
{
    /// <summary>Thrown without saying which. Pairs with nothing, and the suite refuses it.</summary>
    Unsaid,

    /// <summary>Two steps with something between them that is not the descendant operator.</summary>
    StepNotSeparated,

    /// <summary>A word in the control-type position that UI Automation has no such type for.</summary>
    UnknownControlType,

    /// <summary>A <c>#</c> introducing an automation id, with no id after it.</summary>
    EmptyAutomationId,

    /// <summary>A predicate that does not read <c>[key=value]</c>.</summary>
    PredicateMalformed,

    /// <summary>A predicate whose closing bracket never arrives.</summary>
    PredicateNotClosed,

    /// <summary>A quoted value whose closing quote never arrives.</summary>
    QuoteNotClosed,

    /// <summary>A key the grammar does not have.</summary>
    UnknownKey,

    /// <summary>One key claimed twice in one step, which is two claims.</summary>
    KeyClaimedTwice,

    /// <summary>A pattern name UI Automation has no such pattern for.</summary>
    UnknownPattern,

    /// <summary>An order that is none of left, right, top or bottom.</summary>
    UnknownOrder,

    /// <summary>An index that is not a whole number.</summary>
    IndexNotANumber,

    /// <summary>An index below one, which addresses nothing.</summary>
    IndexBelowOne,

    /// <summary>A step that constrains nothing, and so addresses everything.</summary>
    StepConstrainsNothing,
}

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

    /// <summary>
    /// The same, saying which of the ways a locator can fail to parse this one is.
    /// </summary>
    /// <param name="arm">Which way.</param>
    /// <param name="locator">The text as it was written.</param>
    /// <param name="position">The zero-based offset the refusal is about.</param>
    /// <param name="because">What is wrong, in the sentence the author has to act on.</param>
    public LocatorSyntaxException(LocatorFault arm, string locator, int position, string because)
        : this(locator, position, because)
    {
        Arm = arm;
    }

    /// <summary>The text as it was written.</summary>
    public string Locator { get; }

    /// <summary>The zero-based offset the refusal is about.</summary>
    public int Position { get; }

    /// <summary>What is wrong.</summary>
    public string Because { get; }

    /// <summary>
    /// Which way this locator is wrong. <see cref="LocatorFault.Unsaid" /> where it was thrown
    /// without saying — a refusal nothing can pair, and the check says so.
    /// </summary>
    public LocatorFault Arm { get; } = LocatorFault.Unsaid;
}

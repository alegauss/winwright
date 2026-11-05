using System.Collections.ObjectModel;
using System.Reflection;

namespace Winwright.Tests;

/// <summary>
/// One arm of one refusal, paired with the case that provokes it.
/// </summary>
/// <param name="Refusal">The exception type, by name.</param>
/// <param name="Arm">The member of its arm enum, by name.</param>
/// <param name="Case">The case that drives it, as <c>TypeTests.Method_name</c>.</param>
/// <param name="Because">What the arm is about, in the words the pairing is read in.</param>
internal sealed record RefusalArm(string Refusal, string Arm, string Case, string Because)
{
    /// <summary>How the pairing addresses it, which is the pair and never either half.</summary>
    public string Named => $"{Refusal}.{Arm}";

    public override string ToString() => $"{Named,-46} {Because} [{Case}]";
}

/// <summary>
/// WW196. WW188 gave <c>WrongCaptureException</c> an arm it declares and paired the arms one by one,
/// and it fixed one type. Counted after it shipped: <c>LocatorSyntaxException</c> was thrown from
/// thirteen places, <c>UnusableLabelException</c> from six, <c>DeclarationMissingException</c> from
/// four, <c>NotActionableException</c> from four — and <c>Provocation</c> held one entry for each
/// naming one case. So a locator that will not parse for a reason nobody provokes is a refusal that
/// can quietly stop working while the catalogue says the type is covered.
/// <para>
/// The judgement is the task and not a step before it, and the arithmetic records it. Thirteen throw
/// sites of the locator grammar are thirteen arms, because each sends the author to a different
/// character with a different fix. Four throw sites of the declaration are three arms: a missing
/// <c>executable</c> and a missing <c>timeouts.settle</c> are one refusal carrying a different key,
/// and giving each its own arm would be counting values rather than refusals.
/// </para>
/// <para>
/// Read off the engine and never off this list. An arm is a member of an enum a refusal type
/// exposes, which is what makes an arm added tomorrow red here until somebody says what provokes it.
/// <c>Unsaid</c> is left out everywhere: it is what a throw that named no arm carries, and nothing
/// provokes a refusal nobody described.
/// </para>
/// <para>
/// <c>CaptureArms</c> is folded in rather than restated. It carries something this cannot — which
/// fixture flag provokes each capture arm — so it stays the source for those six and hands them
/// over here, and one arm is never written down in two places.
/// </para>
/// </summary>
internal static class RefusalArms
{
    /// <summary>What a throw that named no arm carries, in every one of these enums.</summary>
    internal const string Unsaid = "Unsaid";

    /// <summary>
    /// The members that are not refusals, so nothing provokes them.
    /// <para>
    /// Two words rather than one, and the second was found by this check. <c>Unsaid</c> is a throw
    /// that named no arm. <c>Actionable.Yes</c> is the opposite: all four properties hold and the
    /// act runs, so there is no refusal to provoke at all. A sweep that demanded a case for it would
    /// be asking somebody to provoke a success.
    /// </para>
    /// </summary>
    internal static IReadOnlyList<string> NotRefusals { get; } =
        new ReadOnlyCollection<string>([Unsaid, "Yes"]);

    /// <summary>
    /// Every arm this suite pairs, the capture ones included.
    /// <para>
    /// Held behind a <see cref="Lazy{T}" /> rather than initialised, because a static field
    /// initialiser runs in declaration order and the lists it joins are declared below it. WW193 hit
    /// exactly this and only the guest said so; here the compiler did.
    /// </para>
    /// </summary>
    internal static IReadOnlyList<RefusalArm> Known => known.Value;

    private static readonly Lazy<IReadOnlyList<RefusalArm>> known = new(() =>
        new ReadOnlyCollection<RefusalArm>(
        [
            .. CaptureArms.Known.Select(one => new RefusalArm(
                nameof(Winwright.Capturing.WrongCaptureException), one.Arm.ToString(), one.Case, one.Because)),
            .. Locators,
            .. Labels,
            .. Declarations,
            .. Actionabilities,
        ]));

    /// <summary>
    /// The grammar, which is the largest of them. Every one of these is a different character in a
    /// different place, which is why none of them is folded into another.
    /// </summary>
    private static IReadOnlyList<RefusalArm> Locators => new ReadOnlyCollection<RefusalArm>(
    [
        new(Locator, "StepNotSeparated", "LocatorTests.The_refusal_points_at_the_column_it_is_about",
            "two steps with something between them that is not the descendant operator"),
        new(Locator, "UnknownControlType", "LocatorTests.A_control_type_ui_automation_does_not_have_is_refused_with_the_nearest_words",
            "a word in the control-type position that UI Automation has no such type for, answered "
                + "with the nearest ones it does have"),
        new(Locator, "EmptyAutomationId", "LocatorTests.A_hash_with_no_id_after_it_is_refused",
            "a '#' introducing an automation id with nothing after it, which addresses every element "
                + "rather than one"),
        new(Locator, "PredicateMalformed", "LocatorTests.A_predicate_that_is_not_key_equals_value_is_refused",
            "a predicate that does not read [key=value], answered with the keys the grammar has"),
        new(Locator, "PredicateNotClosed", "LocatorTests.A_predicate_that_is_not_closed_is_refused",
            "a predicate whose closing bracket never arrives, which is a different mistake from one "
                + "spelled wrongly"),
        new(Locator, "QuoteNotClosed", "LocatorTests.A_quoted_value_that_is_never_closed_is_refused_as_the_quote",
            "a quoted value whose closing quote never arrives — refused as the quote and not as the "
                + "predicate, because the bracket inside it was never a bracket"),
        new(Locator, "UnknownKey", "LocatorTests.A_key_the_grammar_does_not_have_is_refused_with_the_ones_it_does",
            "a key the grammar does not have, answered with the ones it does"),
        new(Locator, "KeyClaimedTwice", "LocatorTests.The_same_key_twice_in_one_step_is_two_claims_and_is_refused",
            "one key claimed twice in a step, which is two claims and not a repetition"),
        new(Locator, "UnknownPattern", "LocatorTests.A_pattern_ui_automation_does_not_have_is_refused_with_the_nearest_words",
            "a pattern name UI Automation has no such pattern for, answered from a different "
                + "vocabulary than the control types"),
        new(Locator, "UnknownOrder", "MatchOrderTests.An_order_the_grammar_does_not_have_is_refused_with_the_ones_it_does",
            "an order that is none of left, right, top or bottom"),
        new(Locator, "IndexNotANumber", "LocatorTests.An_index_that_is_not_a_number_is_refused",
            "an index that is not a whole number, where the fix is to write one"),
        new(Locator, "IndexBelowOne", "LocatorTests.An_index_counts_from_one_and_says_so",
            "an index below one, where the fix is to count from one — a different errand from the "
                + "arm above, which is why they are two"),
        new(Locator, "StepConstrainsNothing", "LocatorTests.A_step_that_constrains_nothing_is_refused",
            "a step that constrains nothing and so addresses everything"),
    ]);

    /// <summary>The strings, where each arm is a different file to open.</summary>
    private static IReadOnlyList<RefusalArm> Labels => new ReadOnlyCollection<RefusalArm>(
    [
        new(Label, "NoLanguageFiles", "LabelTests.A_file_whose_name_carries_no_language_never_answers_and_the_refusal_says_so",
            "the project declares no languageFiles whose names carry a language tag"),
        new(Label, "KeyNotThere", "LabelTests.A_key_the_strings_do_not_carry_is_refused_and_names_the_file_it_looked_in",
            "the key is not in the strings this project ships for the chosen language"),
        new(Label, "CarriesAPlaceholder", "LabelTests.A_value_carrying_a_placeholder_is_refused_and_never_skipped",
            "the label carries a placeholder, so a tree holding it filled in could never match — "
                + "refused rather than skipped, which is the whole of WW121"),
        new(Label, "NoFallbackDeclared", "LabelTests.A_language_the_project_ships_nothing_for_is_refused_rather_than_answered_in_english",
            "the application is in a language this project does not ship and nothing says what to "
                + "fall back to"),
        new(Label, "FallbackNotShipped", "LabelTests.A_fallback_the_project_does_not_ship_is_refused_and_names_what_it_does",
            "a fallback is declared and the project ships no strings for it"),
        new(Label, "FileUnreadable", "LabelTests.A_strings_file_that_will_not_parse_is_refused_naming_it",
            "the strings file itself could not be read, which is neither a missing key nor a missing "
                + "language"),
    ]);

    /// <summary>The project file. Four throw sites, three arms, and the arithmetic is the finding.</summary>
    private static IReadOnlyList<RefusalArm> Declarations => new ReadOnlyCollection<RefusalArm>(
    [
        new(Declaration, "NotAtThePathNamed", "ProjectDeclarationTests.A_path_that_names_no_declaration_says_so",
            "a path was named and there is no declaration at it, where the fix is the path"),
        new(Declaration, "NotUpTheTree", "ProjectDeclarationTests.A_checkout_that_declares_nothing_refuses_and_says_where_it_looked",
            "nothing was named and no declaration is in this directory or any above it, where the "
                + "fix is to write one"),
        new(Declaration, "KeyNotDeclared", "ProjectDeclarationTests.What_the_project_never_declared_is_refused_by_name",
            "the declaration was read and does not carry the key — thrown from two places, for a "
                + "top-level key and for a timeout, which are one refusal carrying a different key"),
    ]);

    /// <summary>The four properties an act needs, which have carried their arm since WW17.</summary>
    private static IReadOnlyList<RefusalArm> Actionabilities => new ReadOnlyCollection<RefusalArm>(
    [
        new(Actionable, "NotInTree", "ActionabilityTests.Nothing_in_the_tree_is_the_first_of_the_four",
            "the locator matched nothing, or what matched has since gone, so there is no element"),
        new(Actionable, "Offscreen", "ActionabilityTests.Offscreen_names_the_remedy_that_is_its_own",
            "UI Automation considers it out of view — scrolled away, collapsed or minimised — and "
                + "the remedy is its own"),
        new(Actionable, "Disabled", "ActionabilityTests.Disabled_names_a_different_remedy",
            "the element is there and will not take input, which every verb refuses at the same door"),
        new(Actionable, "PatternMissing", "ActionabilityTests.The_missing_pattern_is_the_one_no_browser_has_to_check",
            "it offers no pattern for the act, which is the property no browser has to check"),
    ]);

    private const string Locator = nameof(Winwright.Locating.LocatorSyntaxException);
    private const string Label = nameof(Winwright.Asserting.UnusableLabelException);
    private const string Declaration = nameof(Winwright.Projects.DeclarationMissingException);
    private const string Actionable = nameof(Winwright.Locating.NotActionableException);

    /// <summary>
    /// Every arm the engine declares, read off the assemblies. A refusal is a type that carries a
    /// property whose type is an enum, which is the shape WW188 settled on and every armed refusal
    /// since has followed.
    /// </summary>
    internal static IReadOnlyList<string> Declared() => declared.Value;

    /// <summary>Every refusal type that carries an arm at all.</summary>
    internal static IReadOnlyList<string> Armed() => Declared()
        .Select(one => one[..one.IndexOf('.', StringComparison.Ordinal)])
        .Distinct(StringComparer.Ordinal)
        .OrderBy(one => one, StringComparer.Ordinal)
        .ToList();

    /// <summary>The reading a person gets: the counts first, then a line each.</summary>
    internal static IReadOnlyList<string> Render() => new ReadOnlyCollection<string>(
    [
        $"{Known.Count} arm(s) across {Armed().Count} refusal(s), each paired with a case that provokes it",
        .. Known.Select(one => $"  {one}"),
    ]);

    private static readonly Lazy<IReadOnlyList<string>> declared = new(Sweep);

    private static IReadOnlyList<string> Sweep() => typeof(Winwright.Locating.Subject).Assembly
        .GetExportedTypes()
        .Where(one => typeof(Exception).IsAssignableFrom(one))
        .SelectMany(one => one
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(arm => arm.PropertyType.IsEnum)
            .SelectMany(arm => Enum.GetNames(arm.PropertyType)
                .Where(member => !NotRefusals.Contains(member, StringComparer.Ordinal))
                .Select(member => $"{one.Name}.{member}")))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(one => one, StringComparer.Ordinal)
        .ToList();
}

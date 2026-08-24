using System.Collections.ObjectModel;

namespace Winwright.Tests;

/// <summary>One way the fixture refuses being driven, paired with the case that provokes it.</summary>
/// <param name="Arm">The arm, as the fixture prints it.</param>
/// <param name="Driven">What the fixture is given to provoke it, as the command line spells it.</param>
/// <param name="Case">The case that drives it, as <c>TypeTests.Method_name</c>.</param>
/// <param name="Because">What the arm is about, in the words the pairing is read in.</param>
internal sealed record FixtureArm(string Arm, string Driven, string Case, string Because)
{
    public override string ToString() => $"{Arm,-20} {Driven,-28} {Because} [{Case}]";
}

/// <summary>
/// WW200. WW196 armed four refusals of five. The fifth is the fixture's own, and it was left because
/// the suite references the fixture without its assembly on purpose — an application under test is
/// launched from its own output rather than read from beside the harness — so an enum swept by
/// reflection had nothing to reflect over.
/// <para>
/// The boundary is crossed the way WW146 crossed it, by running the article and reading what it
/// says. The fixture names the arm at the head of every refusal and prints the whole list under
/// <c>It refuses:</c> with its catalogue, so both directions are checked against what an adopter
/// would see. Nothing here loads a type it cannot load.
/// </para>
/// <para>
/// Every arm is driven by really running the fixture wrong, which is the only way available and also
/// the better one: the pairing asserts the exit code and the arm a person gets, not a call this
/// suite could make into a library.
/// </para>
/// </summary>
internal static class FixtureArms
{
    /// <summary>The line the fixture prints its arms under, which also carries the exit code.</summary>
    internal const string Heading = "It refuses (exit ";

    /// <summary>How the fixture spells an arm, which is what both ends of the pairing match on.</summary>
    internal const string Spelling = "refused ";

    /// <summary>Every arm, paired with the command line that provokes it.</summary>
    internal static IReadOnlyList<FixtureArm> Known { get; } = new ReadOnlyCollection<FixtureArm>(
    [
        new("NotAFlag", "show",
            "FixtureArmTests.Every_arm_is_provoked_by_running_the_fixture_wrong",
            "an argument that does not begin with two dashes, which names no flag at all"),
        new("NoSuchShape", "--nonesuch",
            "FixtureArmTests.Every_arm_is_provoked_by_running_the_fixture_wrong",
            "a flag spelled correctly and belonging to no shape this fixture has"),
        new("NeedsAValue", "--language",
            "FixtureArmTests.Every_arm_is_provoked_by_running_the_fixture_wrong",
            "a flag that takes a value, given none"),
        new("TakesNothing", "--show=please",
            "FixtureArmTests.Every_arm_is_provoked_by_running_the_fixture_wrong",
            "a flag that takes nothing, given something"),
        new("ValueNotAccepted", "--backdrop=marble",
            "FixtureArmTests.Every_arm_is_provoked_by_running_the_fixture_wrong",
            "a flag with a fixed set of values, given one outside it"),
        new("NotAWholeNumber", "--loading=twoseconds",
            "FixtureArmTests.Every_arm_is_provoked_by_running_the_fixture_wrong",
            "a value that has to be a whole number and is not one — thrown from two places, for a "
                + "flag and for a field of a rectangle, which are one refusal carrying a different "
                + "value because the reader writes a number either way"),
        new("NeedsACompanion", "--mutate",
            "FixtureArmTests.Every_arm_is_provoked_by_running_the_fixture_wrong",
            "a flag that means nothing without another — leaving a store changed needs a store to "
                + "change, and asking for it alone has nothing to do"),
        new("TwoRendersAtOnce", "--render=nowhere.png --sizeless --blank",
            "FixtureArmTests.Every_arm_is_provoked_by_running_the_fixture_wrong",
            "two shapes that each want to be the thing drawn, asked for together — and the render "
                + "they both need is asked for too, or the companion check answers first"),
        new("MalformedRectangle", "--intrude=200,200",
            "FixtureArmTests.Every_arm_is_provoked_by_running_the_fixture_wrong",
            "a rectangle that is not four comma-separated fields"),
        new("CoversNothing", "--intrude=200,200,0,150",
            "FixtureArmTests.Every_arm_is_provoked_by_running_the_fixture_wrong",
            "a rectangle of no area, which would be a shape that provokes nothing"),
    ]);

    /// <summary>Every arm the built fixture says it has, read off what it prints.</summary>
    internal static IReadOnlyList<string> Declared() => Declared(Fixture.Catalogue());

    /// <summary>The same, over a catalogue already in hand.</summary>
    /// <param name="catalogue">What the fixture printed.</param>
    internal static IReadOnlyList<string> Declared(string catalogue)
    {
        ArgumentNullException.ThrowIfNull(catalogue);

        var at = catalogue.IndexOf(Heading, StringComparison.Ordinal);
        if (at < 0)
            return [];

        return new ReadOnlyCollection<string>(catalogue[(at + Heading.Length)..]
            .Split('\n')
            .Select(one => one.Trim())
            .Where(one => one.StartsWith(Spelling, StringComparison.Ordinal))
            .Select(one => one[Spelling.Length..].Trim())
            .Where(one => one.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList());
    }

    /// <summary>
    /// What a run refused this way exits with, read off the heading rather than copied.
    /// <para>
    /// WW161's rule, which this heading exists to keep: the suite used to carry the number as a
    /// constant transcribed out of the fixture, and a second transcription of one fact is the thing
    /// the catalogue was built to stop.
    /// </para>
    /// </summary>
    /// <param name="catalogue">What the fixture printed.</param>
    internal static int Exits(string catalogue)
    {
        ArgumentNullException.ThrowIfNull(catalogue);

        var at = catalogue.IndexOf(Heading, StringComparison.Ordinal);
        if (at < 0)
            return -1;

        var from = at + Heading.Length;
        var shuts = catalogue.IndexOf(')', from);
        return shuts < 0
            ? -1
            : int.Parse(catalogue[from..shuts], System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>The reading a person gets: the count first, then a line each.</summary>
    internal static IReadOnlyList<string> Render() => new ReadOnlyCollection<string>(
    [
        $"{Known.Count} arm(s) of the fixture's own refusal, each driven by a command line",
        .. Known.Select(one => $"  {one}"),
    ]);
}

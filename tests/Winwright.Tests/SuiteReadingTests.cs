using System.Reflection;

using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW182. The engine's rules stop at its assembly boundary, and the suite is where this project's
/// own defects have been shipping.
/// <para>
/// <c>RecordedResultTests</c> reads the engine assembly and asserts that every result answering a
/// verdict answers the step behind it. Nothing read this one. So <c>TrayCensus</c> was written with
/// a sentence and a list, a list has no way to say "I did not look", and WW181 shipped a reading
/// that reported a clean desk it had never looked at — this project's founding non-goal, committed
/// inside the reading built to stop it.
/// </para>
/// <para>
/// The rule this leaves behind is not about care. A suite reading that can fail to observe answers
/// the engine's <see cref="Finding" />, whose <c>bool? Holds</c> makes the third state a property of
/// the type rather than something an author has to remember — and a reading added later with a
/// verdict and no <c>Finding</c> is red here until somebody has thought about what it says when it
/// could not look.
/// </para>
/// </summary>
public sealed class SuiteReadingTests
{
    /// <summary>Every type in this suite that answers a verdict a case counts.</summary>
    private static IReadOnlyList<Type> Answering() =>
        typeof(SuiteReadingTests).Assembly
            .GetTypes()
            .Where(one => one.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Any(method => method.Name == "AsAssertion" && method.ReturnType == typeof(AssertionResult)))
            .OrderBy(one => one.Name, StringComparer.Ordinal)
            .ToList();

    /// <summary>The three-state reading that type answers, where it answers one.</summary>
    private static MethodInfo? Reading(Type answering) =>
        answering.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(one => one.Name == "AsFinding" && one.ReturnType == typeof(Finding));

    [Fact]
    public void Every_suite_reading_that_answers_a_verdict_answers_a_finding_too()
    {
        var answering = Answering();

        var silent = answering.Where(one => Reading(one) is null).Select(one => one.Name).ToList();

        Assert.True(
            silent.Count == 0,
            $"{silent.Count} of {answering.Count} suite reading(s) answer a verdict and no Finding, so "
                + $"nothing says what they report when they could not look: {string.Join(", ", silent)}");
    }

    [Fact]
    public void The_reading_is_taken_off_the_assembly_and_not_off_a_list_kept_here()
    {
        // The control, and the same one RecordedResultTests keeps for its own sweep: a check that
        // found nothing to check is a green about an empty set, and this one is small enough that
        // an empty set is exactly what a broken predicate would produce.
        var answering = Answering();

        Assert.NotEmpty(answering);
        Assert.Contains(answering, one => one.Name == nameof(TrayCensus));

        // And the finder discriminates, which is the other half of not being vacuous: one that
        // answered something for every type would pass this rule for a suite that follows none of it.
        Assert.NotNull(Reading(typeof(TrayCensus)));
        Assert.Null(Reading(typeof(BusyDesk)));
        Assert.Null(Reading(typeof(string)));
        Assert.DoesNotContain(answering, one => one.Name == nameof(Provocation));
    }

    [Fact]
    public void The_third_state_is_a_property_of_the_type_and_never_of_the_author()
    {
        // Why a Finding rather than a convention. Holds is nullable, so a reading that answers one
        // cannot fail to have somewhere to put "I did not look" — which is the whole difference
        // between this rule and asking people to remember.
        var holds = typeof(Finding).GetProperty(nameof(Finding.Holds));

        Assert.NotNull(holds);
        Assert.Equal(typeof(bool?), holds.PropertyType);
    }

    [Fact]
    public void The_census_says_it_did_not_look_rather_than_that_it_found_nothing()
    {
        // WW181's defect, asserted through the shape that now prevents it. A census that could not
        // open the overflow answers a Finding with no verdict at all, and one that read everything
        // and found nothing answers a Finding that holds.
        var unread = new TrayCensus([], everywhere: false, "the taskbar shows no chevron").AsFinding();

        Assert.False(unread.Was);
        Assert.Null(unread.Holds);
        Assert.StartsWith("  not read ", unread.ToString(), StringComparison.Ordinal);

        var clean = new TrayCensus([], everywhere: true, "").AsFinding();

        Assert.True(clean.Was);
        Assert.True(clean.Holds);

        var holding = new TrayCensus(["winwright under test #4321"], everywhere: true, "").AsFinding();

        Assert.True(holding.Was);
        Assert.False(holding.Holds);
        Assert.Contains("winwright under test #4321", holding.Sentence, StringComparison.Ordinal);
    }

    [Fact]
    public void The_finding_is_named_the_same_wherever_it_is_reported()
    {
        // One spelling, for the reason ProcessSummary gives about its own: a reading named two ways
        // is two things a reader has to match up by hand.
        Assert.Equal(TrayGhosts.Named, new TrayCensus([], everywhere: true, "").AsFinding().Named);
    }
}

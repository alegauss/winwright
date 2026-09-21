using System.Reflection;

using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW183. Which conditions are the desk's was an array of five names typed into this suite, and it
/// had already missed two — the reading that measures a person at the keyboard, which turned into a
/// red twice while WW172 was being measured, and the one WW38 added for a window standing over a
/// capture, which nothing went back for.
/// <para>
/// The judgement now lives beside the readings it is about. What is checked here is that it stays a
/// judgement about conditions this engine really declares: a renamed constant is a name in this list
/// that matches nothing, which is a desk fact that has quietly stopped excusing anything.
/// </para>
/// </summary>
public sealed class DeskFactTests
{
    /// <summary>Every condition the engine declares, read off the assembly rather than off a list.</summary>
    private static IReadOnlyList<string> Declared() =>
        typeof(Precondition).Assembly
            .GetExportedTypes()
            .SelectMany(one => one.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(one => one.IsLiteral && !one.IsInitOnly && one.FieldType == typeof(string))
            .Select(one => one.GetRawConstantValue() as string ?? "")
            .Where(one => one.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    [Fact]
    public void Every_desk_fact_is_a_condition_this_engine_really_declares()
    {
        var declared = Declared();

        var orphaned = DeskFacts.Named.Where(one => !declared.Contains(one, StringComparer.Ordinal)).ToList();

        Assert.True(
            orphaned.Count == 0,
            $"{orphaned.Count} desk fact(s) name a condition nothing declares, so they excuse nothing: "
                + string.Join("; ", orphaned));
    }

    [Fact]
    public void The_two_the_hand_kept_list_missed_are_in_it()
    {
        // Named rather than counted, because these two are the whole measurement. One turned a
        // person at the keyboard into a red about this code; the other arrived with WW38 and was
        // never classified at all.
        Assert.Contains(Winwright.Windowing.ForeignInput.PreconditionName, DeskFacts.Named);
        Assert.Contains(Winwright.Capturing.Obstruction.PreconditionName, DeskFacts.Named);
    }

    [Fact]
    public void A_fact_about_the_thing_under_test_is_not_the_desks()
    {
        // The judgement, asserted rather than assumed. Excusing an assertion on a stale binary or a
        // page still computing would excuse it on the defect it was looking for — and a list that
        // grew until it held everything would be a suite that can never go red.
        Assert.DoesNotContain(Winwright.Projects.Staleness.PreconditionName, DeskFacts.Named);
        Assert.DoesNotContain(Winwright.Asserting.LoadingCheck.PreconditionName, DeskFacts.Named);
        Assert.DoesNotContain(Winwright.Capturing.Glass.PreconditionName, DeskFacts.Named);
        Assert.DoesNotContain(Winwright.Processes.RunningBinary.PreconditionName, DeskFacts.Named);

        // And the reading really did find those, so the four above are absences rather than typos.
        var declared = Declared();
        Assert.Contains(Winwright.Projects.Staleness.PreconditionName, declared);
        Assert.Contains(Winwright.Capturing.Glass.PreconditionName, declared);
    }

    [Fact]
    public void Every_desk_fact_says_why_it_is_one()
    {
        // The half a list of names cannot carry. Which conditions are the desk's is a judgement,
        // and a judgement nobody wrote down is one the next person makes again from scratch.
        Assert.All(
            DeskFacts.Known,
            one =>
            {
                Assert.False(string.IsNullOrWhiteSpace(one.Because), $"{one.Named} says nothing");
                Assert.True(one.Because.Length > 40, $"'{one.Because}' is too short to be a reason");
                Assert.DoesNotContain(one.Named, one.Because, StringComparison.Ordinal);
            });
    }

    [Fact]
    public void No_desk_fact_is_named_twice_and_the_match_is_exact()
    {
        Assert.Equal(DeskFacts.Named.Count, DeskFacts.Named.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(DeskFacts.Known.Count, DeskFacts.Named.Count);

        // Exact, because a condition matched loosely is one that will one day match a different one
        // and excuse an assertion nobody meant to.
        Assert.True(DeskFacts.Names(Winwright.Windowing.Foreground.PreconditionName));
        Assert.False(DeskFacts.Names(Winwright.Windowing.Foreground.PreconditionName + " or thereabouts"));
        Assert.False(DeskFacts.Names("the foreground"));
        Assert.False(DeskFacts.Names(""));
    }

    [Fact]
    public void The_sources_declare_exactly_the_conditions_the_assembly_carries()
    {
        // WW491. The documentation area publishes every condition that can earn a hole, and it is
        // built by Node with no assembly to reflect over — so its generator sweeps these sources
        // for the two spellings `Holes.Declared` selects on. That sweep is only as good as this
        // equality: a condition declared some third way would be missing from the page, and a
        // page that is silently short is the shape of answer this project refuses everywhere else.
        // Read with `Spoken` and not raw: the conditions are string literals, so the reading that
        // keeps strings and drops comments is the one this wants — and a doc comment quoting a
        // declaration would otherwise be swept as a second one.
        var swept = Checkout.SourcesIn(Checkout.At("src", "Winwright"))
            .SelectMany(file => File.ReadLines(file)
                .Select(Checkout.Spoken)
                .Select(line => System.Text.RegularExpressions.Regex.Match(
                    line,
                    """public const string (?:[A-Za-z]*PreconditionName|Named) = "([^"]+)";"""))
                .Where(match => match.Success)
                .Select(match => match.Groups[1].Value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(one => one, StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(swept);
        Assert.Equal(Holes.Declared.OrderBy(one => one, StringComparer.Ordinal), swept);
    }
}

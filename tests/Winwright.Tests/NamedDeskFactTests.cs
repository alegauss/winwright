using Xunit;

using Winwright.Verdicts;

namespace Winwright.Tests;

/// <summary>
/// WW427. The catalogue read back against the sources, both ways.
/// <para>
/// The distinction it holds is the one WW401 learned twice: a case that provokes the desk and then
/// names the reading it expects is asserting its arrangement as well as its claim, and only one of
/// those is about the engine. Nothing here decides whether a sentence is true — what it stops is a
/// case naming a fact with nobody having said what pins it.
/// </para>
/// </summary>
public sealed class NamedDeskFactTests
{
    [Fact]
    public void Every_case_that_provokes_the_desk_and_names_a_reading_says_what_pins_it()
    {
        var paired = NamedDeskFacts.Known.Select(one => one.Case).ToHashSet(StringComparer.Ordinal);

        var unsaid = NamedDeskFacts.Found().Where(one => !paired.Contains(one)).ToList();

        Assert.True(
            unsaid.Count == 0,
            $"{unsaid.Count} case(s) take the desk away and then name which reading they expect, and "
                + "nothing says whether the arrangement pins that reading or the provocation happened "
                + $"to land there: {string.Join(", ", unsaid)}");
    }

    [Fact]
    public void Nothing_is_paired_that_no_longer_names_a_reading()
    {
        // The other direction, and the one that rots quietly: an entry for a case that has since been
        // repaired reads as a reason somebody still needs, and the next reader weighs a sentence
        // about a case that does not do it any more.
        var found = NamedDeskFacts.Found().ToHashSet(StringComparer.Ordinal);

        var stale = NamedDeskFacts.Known.Where(one => !found.Contains(one.Case)).Select(one => one.Case).ToList();

        Assert.True(
            stale.Count == 0,
            $"{stale.Count} entry(s) name a case that no longer provokes the desk and names a reading: "
                + string.Join(", ", stale));
    }

    [Fact]
    public void The_sweep_finds_the_cases_it_is_about_and_not_the_one_that_was_repaired()
    {
        // The control. Both checks above pass against a sweep that found nothing at all, and the
        // catalogue would then be a list of reasons for nothing.
        var found = NamedDeskFacts.Found();

        Assert.NotEmpty(found);
        Assert.Contains(
            "RefusedForegroundTests.A_click_that_could_not_be_sent_is_a_hole_naming_the_desk",
            found,
            StringComparer.Ordinal);

        // And WW401's own case, which provokes the desk harder than any of them and names no
        // reading: it asks the engine whether the condition is the desk's. A sweep that caught it
        // would be one that cannot tell the repair from the thing repaired.
        Assert.DoesNotContain(
            found,
            one => one.StartsWith("CaseRunTests.A_window_taking_the_desk_mid", StringComparison.Ordinal));
    }

    [Fact]
    public void Every_reason_is_a_sentence_somebody_weighs()
    {
        Assert.All(
            NamedDeskFacts.Known,
            one =>
            {
                Assert.False(string.IsNullOrWhiteSpace(one.Arranged), $"{one.Case} says nothing");
                Assert.DoesNotContain(one.Case, one.Arranged, StringComparison.Ordinal);
            });
    }

    [Fact]
    public void The_engine_still_calls_the_foreground_the_desks()
    {
        // What every entry above leans on, asked of the engine rather than assumed: a reading these
        // cases name that the engine stopped calling the desk's would make each of them a red about
        // the application, which is the misattribution the whole catalogue is about.
        Assert.True(DeskFacts.Names(Winwright.Windowing.Foreground.PreconditionName));
    }
}

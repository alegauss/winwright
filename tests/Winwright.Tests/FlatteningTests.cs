using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW191. The boundary that reaches the defect. <c>Swallowing</c> draws one at a catch and WW181 had
/// none — the overflow refusing to open arrived as a bool on a reading that knew it had not looked,
/// and a helper dropped it on the way to a list. This is the rule about that: a helper that reads
/// something carrying the third state answers something that can carry it too.
/// </summary>
public sealed class FlatteningTests
{
    [Fact]
    public void Every_helper_that_narrows_a_three_state_reading_says_why_that_loses_nothing()
    {
        var paired = Flattening.Known.Select(one => one.Named).ToHashSet(StringComparer.Ordinal);

        var silent = Flattening.Found().Where(one => !paired.Contains(one)).ToList();

        Assert.True(
            silent.Count == 0,
            $"{silent.Count} helper(s) read a reading that can say it did not look and answer a type "
                + $"that cannot:{Environment.NewLine}  "
                + string.Join($"{Environment.NewLine}  ", silent));
    }

    [Fact]
    public void Nothing_is_paired_that_no_longer_narrows_anything()
    {
        var narrowing = Flattening.Found().ToHashSet(StringComparer.Ordinal);

        var stale = Flattening.Known.Where(one => !narrowing.Contains(one.Named)).ToList();

        Assert.True(
            stale.Count == 0,
            $"{stale.Count} pairing(s) name a helper that no longer narrows anything: "
                + string.Join(", ", stale.Select(one => one.Named)));
    }

    [Fact]
    public void The_rule_finds_the_defect_it_was_written_for()
    {
        // The whole of WW191, as a measurement rather than a claim. WW182's rule keys on answering a
        // verdict, and what WW181 shipped answered none — so that rule would have passed it. This one
        // finds it, and the shape it finds is the shipped signature rather than a description of it.
        Assert.Contains("TheShapeWW181Shipped.Showing", Flattening.Found(), StringComparer.Ordinal);

        // And WW182's rule really would not have: the type it lived on answers no verdict at all.
        Assert.Null(typeof(TheShapeWW181Shipped).GetMethod("AsAssertion"));
    }

    [Fact]
    public void The_repair_passes_the_rule_that_would_have_caught_the_defect()
    {
        // The other end of the same measurement. TrayGhosts.Showing does exactly what the control
        // does and answers TrayCensus, which carries all three — so it is absent here, and that
        // absence is the repair being right rather than the sweep being blind.
        Assert.DoesNotContain("TrayGhosts.Showing", Flattening.Found(), StringComparer.Ordinal);
        Assert.True(Flattening.Carries(typeof(TrayCensus)));
        Assert.False(Flattening.Carries(typeof(IReadOnlyList<string>)));
    }

    [Fact]
    public void What_carries_the_third_state_is_read_off_the_engine_and_not_named_here()
    {
        // The engine's own shapes, and the nullable that Finding's `bool? Holds` argues for.
        Assert.True(Flattening.Carries(typeof(Finding)));
        Assert.True(Flattening.Carries(typeof(AssertionResult)));
        Assert.True(Flattening.Carries(typeof(Precondition)));
        Assert.True(Flattening.Carries(typeof(bool?)));

        // And a verdict, which answers none of those and is certainly not two-state: Degraded is the
        // third state under another name, which is why the vocabulary is read rather than the type.
        Assert.True(Flattening.Carries(typeof(RunVerdict)));

        Assert.False(Flattening.Carries(typeof(bool)));
        Assert.False(Flattening.Carries(typeof(string)));
    }

    [Fact]
    public void Every_word_the_vocabulary_uses_is_one_this_engine_really_says()
    {
        // The one hand-written thing here, checked rather than believed. A word this engine has
        // stopped using is a word that quietly stops recognising the third state.
        var said = typeof(Precondition).Assembly
            .GetTypes()
            .Where(one => one.IsEnum)
            .SelectMany(Enum.GetNames)
            .ToHashSet(StringComparer.Ordinal);

        var unsaid = Flattening.Vocabulary.Where(one => !said.Contains(one)).ToList();

        Assert.True(
            unsaid.Count == 0,
            $"{unsaid.Count} word(s) for the third state name nothing this engine declares: "
                + string.Join(", ", unsaid));
    }

    [Fact]
    public void The_producers_are_swept_off_both_assemblies_and_discriminate()
    {
        var producers = Flattening.Producers();

        Assert.NotEmpty(producers);
        Assert.Contains("NotificationArea.OpenOverflow(", producers, StringComparer.Ordinal);
        Assert.Contains("Focus.In(", producers, StringComparer.Ordinal);
        Assert.DoesNotContain("NotificationArea.Showing(", producers, StringComparer.Ordinal);
    }

    [Fact]
    public void The_pairing_reads_as_a_count_and_then_a_line_each()
    {
        var rendered = Flattening.Render();

        Assert.Equal(Flattening.Known.Count + 1, rendered.Count);
        Assert.StartsWith(
            $"{Flattening.Found().Count} helper(s) read a three-state reading and answer something narrower, ",
            rendered[0],
            StringComparison.Ordinal);
        Assert.All(rendered.Skip(1), line => Assert.StartsWith("  ", line, StringComparison.Ordinal));
    }
}

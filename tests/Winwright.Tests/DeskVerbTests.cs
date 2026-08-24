using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW208. The other end of WW190's door. That task built the reading that finds a case asking the
/// desk for a verdict without excusing it, and keyed it on a list of calls typed by hand — so a
/// reading the list had never heard of was one no case was ever asked to excuse.
/// </summary>
public sealed class DeskVerbTests
{
    private static IReadOnlyList<string> Named() => DeskAsks.Calls
        .Select(one => one.Call.TrimEnd('('))
        .ToList();

    [Fact]
    public void Every_engine_verb_that_reaches_the_desk_is_a_call_a_case_has_to_excuse()
    {
        var named = Named().ToHashSet(StringComparer.Ordinal);
        var excused = DeskVerbs.Excused.Select(one => one.Named).ToHashSet(StringComparer.Ordinal);

        var missing = DeskVerbs.Reaching()
            .Where(one => !named.Contains(one) && !excused.Contains(one))
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"{missing.Count} engine verb(s) ask the desk what is on it and are in neither list, so "
                + $"a case calling one is asked to excuse nothing:{Environment.NewLine}  "
                + string.Join($"{Environment.NewLine}  ", missing));
    }

    [Fact]
    public void Nothing_is_excused_that_no_longer_reaches_the_desk()
    {
        var reaching = DeskVerbs.Reaching().ToHashSet(StringComparer.Ordinal);

        var stale = DeskVerbs.Excused.Where(one => !reaching.Contains(one.Named)).ToList();

        Assert.True(
            stale.Count == 0,
            $"{stale.Count} excuse(s) name a verb that no longer reaches the desk: "
                + string.Join(", ", stale.Select(one => one.Named)));
    }

    [Fact]
    public void The_reading_finds_the_two_calls_it_was_written_about()
    {
        // Both were absent from the list when this was written. One was found by a guest run going
        // red twice; the other by this reading, on the same day — which is the difference the task
        // is about, since only one of those two ways costs anybody a morning.
        var reaching = DeskVerbs.Reaching();

        Assert.Contains("Traversal.WhoHasFocus", reaching, StringComparer.Ordinal);
        Assert.Contains("Traversal.Press", reaching, StringComparer.Ordinal);
        Assert.Contains("Traversal.WhoHasFocus", Named(), StringComparer.Ordinal);
        Assert.Contains("Traversal.Press", Named(), StringComparer.Ordinal);
    }

    [Fact]
    public void The_reading_discriminates_rather_than_finding_the_whole_engine()
    {
        // A sweep that named everything would pass the rule above by making the list a formality.
        // Two ends: a verb that plainly asks the desk, and one that plainly reads what it was given.
        var reaching = DeskVerbs.Reaching();

        Assert.Contains("Foreground.Now", reaching, StringComparer.Ordinal);
        Assert.Contains("NotificationArea.Find", reaching, StringComparer.Ordinal);

        Assert.DoesNotContain("Locator.Parse", reaching, StringComparer.Ordinal);
        Assert.DoesNotContain("Inspect.Render", reaching, StringComparer.Ordinal);
        Assert.DoesNotContain("PaintedFrame.Of", reaching, StringComparer.Ordinal);

        Assert.True(reaching.Count < 30, $"{reaching.Count} verbs reach the desk, which is more than expected");
    }

    [Fact]
    public void Every_primitive_is_one_the_engine_really_calls()
    {
        // The one hand-written thing here, checked rather than believed. A primitive this engine has
        // stopped calling is a word that quietly stops finding anything, and the list would go on
        // looking complete.
        var sources = Checkout.SourcesIn(Checkout.Engine)
            .SelectMany(one => File.ReadLines(one).Select(Checkout.Code))
            .ToList();

        var unused = DeskVerbs.Primitives
            .Where(one => !sources.Any(line => line.Contains(one, StringComparison.Ordinal)))
            .ToList();

        Assert.True(
            unused.Count == 0,
            $"{unused.Count} primitive(s) name something this engine no longer calls: "
                + string.Join(", ", unused));
    }

    [Fact]
    public void Every_excuse_says_something_and_names_a_verb_once()
    {
        var named = DeskVerbs.Excused.Select(one => one.Named).ToList();

        Assert.Equal(named.Count, named.Distinct(StringComparer.Ordinal).Count());
        Assert.All(DeskVerbs.Excused, one => Assert.True(one.Because.Length > 60, one.Because));
    }

    [Fact]
    public void The_reading_reads_as_a_count_and_then_a_line_each()
    {
        var rendered = DeskVerbs.Render();

        Assert.Equal(DeskVerbs.Excused.Count + 1, rendered.Count);
        Assert.StartsWith($"{DeskVerbs.Reaching().Count} engine verb(s) reach the desk", rendered[0], StringComparison.Ordinal);
        Assert.All(rendered.Skip(1), line => Assert.StartsWith("  ", line, StringComparison.Ordinal));
    }
}

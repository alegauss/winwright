using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW204. The pairing in <see cref="DeskDiscards" /> is checked against the sources in both
/// directions. A desk reading thrown away is one nobody asked, and the cost of that lands on
/// whoever asserts afterwards rather than on the line that discarded it — so a discard added later
/// is red here until somebody says what downstream could be wronged by it.
/// </summary>
public sealed class DeskDiscardTests
{
    [Fact]
    public void Every_discarded_desk_reading_says_why_throwing_it_away_loses_nothing()
    {
        var paired = DeskDiscards.Known.Select(one => one.Named).ToHashSet(StringComparer.Ordinal);

        var silent = DeskDiscards.Sites().Where(one => !paired.Contains(one.Named)).ToList();

        Assert.True(
            silent.Count == 0,
            $"{silent.Count} desk reading(s) are thrown away and nothing says what could be wronged "
                + $"by it:{Environment.NewLine}  "
                + string.Join($"{Environment.NewLine}  ", silent.Select(one => one.Named)));
    }

    [Fact]
    public void Nothing_is_paired_that_is_no_longer_thrown_away()
    {
        var discarding = DeskDiscards.Sites().Select(one => one.Named).ToHashSet(StringComparer.Ordinal);

        // The half that decays, and the one this task exists because of: TrayIconFixture read its
        // close in the end, and an entry left standing here would go on excusing a discard that is
        // not there — which is the reading the next person takes for the state of the suite.
        var stale = DeskDiscards.Known.Where(one => !discarding.Contains(one.Named)).ToList();

        Assert.True(
            stale.Count == 0,
            $"{stale.Count} pairing(s) name a reading that is no longer discarded: "
                + string.Join(", ", stale.Select(one => one.Named)));
    }

    [Fact]
    public void The_reading_looks_at_helpers_and_not_only_at_cases()
    {
        // Where this differs from DeskAsks, and it is the whole reason it exists separately: the
        // discard that cost a red was in a fixture helper, and a sweep over cases alone would have
        // been looking away from it.
        var found = DeskDiscards.Found();

        Assert.Contains("TrayGhosts.Showing", found, StringComparer.Ordinal);
        Assert.Contains("NotificationAreaTests.Dispose", found, StringComparer.Ordinal);

        // And the one WW200 repaired is absent, which is what a repair looks like from here.
        Assert.DoesNotContain(found, one => one.StartsWith("TrayIconFixture.", StringComparison.Ordinal));
    }

    [Fact]
    public void A_reading_whose_answer_is_read_is_not_a_discard()
    {
        // The other end of the same rule. A line that assigns the reading, asserts on it or asks it
        // a question has asked the desk something, and DeskAsks is what governs those.
        var found = DeskDiscards.Found();

        Assert.DoesNotContain("NotificationAreaTests.The_overflow_opens_through_the_pattern_and_shuts_again", found, StringComparer.Ordinal);
        Assert.NotEmpty(found);
    }

    [Fact]
    public void No_site_is_paired_twice_and_every_reason_says_something()
    {
        var named = DeskDiscards.Known.Select(one => one.Named).ToList();

        Assert.Equal(named.Count, named.Distinct(StringComparer.Ordinal).Count());
        Assert.All(DeskDiscards.Known, one => Assert.False(string.IsNullOrWhiteSpace(one.Because)));

        // Long enough to be a reason rather than a shrug, which is the bar Sleeps sets for its own.
        Assert.All(DeskDiscards.Known, one => Assert.True(one.Because.Length > 60, one.Because));
    }

    [Fact]
    public void The_pairing_reads_as_a_count_and_then_a_line_each()
    {
        var rendered = DeskDiscards.Render();

        Assert.Equal(DeskDiscards.Known.Count + 1, rendered.Count);
        Assert.StartsWith(
            $"{DeskDiscards.Found().Count} desk reading(s) this suite throws away",
            rendered[0],
            StringComparison.Ordinal);
        Assert.All(rendered.Skip(1), line => Assert.StartsWith("  ", line, StringComparison.Ordinal));
    }
}

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW184. The catalogue in <see cref="Sleeps" /> is checked against the sources in both directions,
/// which is the whole of it: a sleep added later is red here until somebody says why it is not a
/// scenario waiting — which is the question Block C's second criterion asks and nothing else did.
/// </summary>
public sealed class SleepTests
{
    [Fact]
    public void Every_sleep_in_the_tree_is_in_the_catalogue()
    {
        var listed = Sleeps.Known.Select(one => one.File).ToHashSet(StringComparer.Ordinal);

        var missing = Sleeps.Found().Where(one => !listed.Contains(one.File)).ToList();

        Assert.True(
            missing.Count == 0,
            $"{missing.Count} file(s) sleep and nothing says why it is not a wait: "
                + string.Join(", ", missing.Select(one => one.File)));
    }

    [Fact]
    public void Nothing_is_catalogued_that_no_longer_sleeps()
    {
        var found = Sleeps.Found().Select(one => one.File).ToHashSet(StringComparer.Ordinal);

        var gone = Sleeps.Known.Where(one => !found.Contains(one.File)).ToList();

        Assert.True(
            gone.Count == 0,
            $"{gone.Count} file(s) are catalogued and sleep nowhere: "
                + string.Join(", ", gone.Select(one => one.File)));
    }

    [Fact]
    public void The_count_in_the_catalogue_is_the_count_in_the_file()
    {
        // Per file and not in total, for the reason Deadlines gives about its own: a sleep deleted
        // in one file and added in another is what a total calls unchanged, and the added one is
        // the one nobody has looked at.
        var found = Sleeps.Found().ToDictionary(one => one.File, one => one.Sleeps, StringComparer.Ordinal);

        Assert.All(
            Sleeps.Known,
            one => Assert.True(
                found.TryGetValue(one.File, out var calls) && calls == one.Sleeps,
                $"{one.File} is catalogued with {one.Sleeps} and the file has "
                    + $"{(found.TryGetValue(one.File, out var actual) ? actual : 0)}"));
    }

    [Fact]
    public void The_call_is_counted_and_never_the_word()
    {
        // Two files discuss sleeping in prose — Sleeps itself and Waits, whose whole comment is
        // about the eighteen hand-rolled loops it replaced. A scan matching the words would report
        // the criterion broken by a comment explaining why it is not.
        var found = Sleeps.Found().Select(one => one.File).ToList();

        Assert.DoesNotContain("Waits.cs", found);
        Assert.DoesNotContain("Criteria.cs", found);

        // And it really is reading something, so the two absences above are absences rather than a
        // scan that matched nothing at all.
        Assert.True(found.Count >= 5, $"only {found.Count} file(s) were found to sleep, which is unexpected");
        Assert.Contains("Attempt.cs", found);
    }

    [Fact]
    public void Every_sleep_says_why_it_is_not_a_wait()
    {
        Assert.All(
            Sleeps.Known,
            one =>
            {
                Assert.False(string.IsNullOrWhiteSpace(one.Because), $"{one.File} says nothing");
                Assert.True(one.Because.Length > 40, $"'{one.Because}' is too short to be a reason");
            });
    }

    [Fact]
    public void A_sleep_that_is_still_a_wait_is_counted_rather_than_argued_away()
    {
        // The entry that makes this a catalogue rather than a ban. One sleep here is a wait and
        // stays one, because the state a traversal settles into is the one after the change and a
        // condition true too early reads as a wait that was proved.
        //
        // A rule admitting no exceptions would be answered by somebody spelling the sleep
        // differently, and then nothing would know about it at all.
        var waiting = Sleeps.Waiting();

        Assert.NotEmpty(waiting);
        Assert.All(waiting, one => Assert.Contains("nothing", one.Because, StringComparison.OrdinalIgnoreCase));

        // Small, and worth watching. This is the number Block C's criterion is really about, and it
        // is the one that must not quietly grow.
        Assert.True(waiting.Count <= 2, $"{waiting.Count} sleeps are still waits, which is more than this block allows");
    }

    [Fact]
    public void The_deadline_machinery_is_where_the_sleeping_belongs()
    {
        // The positive half of the criterion. A deadline that did not sleep would be a spin, so the
        // engine's own waiting is where a sleep is not only allowed but required.
        var machinery = Sleeps.Known.Where(one => one.Kind == Sleeping.Machinery).ToList();

        Assert.Contains(machinery, one => one.File == "Attempt.cs");
        Assert.Contains(machinery, one => one.File == "Expectation.cs");
    }
}

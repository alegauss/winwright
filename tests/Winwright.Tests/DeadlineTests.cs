using Winwright.Locating;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW175. The catalogue in <see cref="Deadlines" /> is checked against the sources in both
/// directions, which is the whole of it: a wait added later is red here until somebody says what its
/// look answers when the thing has not arrived, and that is the question that would have caught the
/// near-miss at the moment it was written rather than three commits later.
/// </summary>
public sealed class DeadlineTests
{
    [Fact]
    public void Every_deadline_in_the_tree_is_in_the_catalogue()
    {
        var listed = Deadlines.Known.Select(one => one.File).ToHashSet(StringComparer.Ordinal);

        var missing = Deadlines.Found().Where(one => !listed.Contains(one.File)).ToList();

        Assert.True(
            missing.Count == 0,
            $"{missing.Count} file(s) open a deadline nothing says anything about: "
                + string.Join(", ", missing.Select(one => one.File)));
    }

    [Fact]
    public void Nothing_is_catalogued_that_no_longer_opens_one()
    {
        var found = Deadlines.Found().Select(one => one.File).ToHashSet(StringComparer.Ordinal);

        var gone = Deadlines.Known.Where(one => !found.Contains(one.File)).ToList();

        Assert.True(
            gone.Count == 0,
            $"{gone.Count} file(s) are catalogued and open none: " + string.Join(", ", gone.Select(one => one.File)));
    }

    [Fact]
    public void The_count_in_the_catalogue_is_the_count_in_the_file()
    {
        // Per file and not in total, because a wait deleted in one file and added in another is the
        // case a total would call unchanged — and the added one is the one nobody has looked at.
        var found = Deadlines.Found().ToDictionary(one => one.File, one => one.Waits, StringComparer.Ordinal);

        Assert.All(
            Deadlines.Known,
            one => Assert.True(
                found.TryGetValue(one.File, out var waits) && waits == one.Waits,
                $"{one.File} is catalogued with {one.Waits} wait(s) and the file has {(found.TryGetValue(one.File, out var actual) ? actual : 0)}"));
    }

    [Fact]
    public void No_file_is_catalogued_twice()
    {
        var listed = Deadlines.Known.Select(one => one.File).ToList();

        Assert.Equal(listed.Count, listed.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Every_deadline_says_what_its_look_answers_when_the_thing_is_not_there()
    {
        // The one question worth asking, and the only one that separates a wait from a wait that
        // has quietly stopped being one. A sentence that does not mention what nothing looks like
        // is an entry somebody filled in to make this go green.
        Assert.All(
            Deadlines.Known,
            one =>
            {
                Assert.False(string.IsNullOrWhiteSpace(one.Nothing), $"{one.File} says nothing about nothing");
                Assert.True(one.Nothing.Length > 30, $"{one.File} says '{one.Nothing}', which answers no question");
            });
    }

    [Fact]
    public void A_look_that_can_never_answer_nothing_polls_once_and_the_deadline_is_gone()
    {
        // Stated rather than discovered. This is legal, documented and occasionally wanted — a
        // thing already there costs no sleep — and it is also exactly what a wait looks like after
        // somebody changes the thing it waits on from nullable to a reading. The behaviour is not
        // the defect; nothing saying it out loud was.
        var polls = 0;

        var sighting = Attempt.Until(
            () =>
            {
                polls++;
                return "always answered";
            },
            deadlineMs: 5000,
            pollMs: 10);

        Assert.True(sighting.Found);
        Assert.Equal(1, polls);
        Assert.Equal(1, sighting.Polls);

        // And it says so: a wait of five seconds that took none is a wait that never waited, which
        // is readable off the sighting by anyone who thinks to look.
        Assert.True(sighting.WaitedMs < 1000, $"the deadline was five seconds and this took {sighting.WaitedMs}ms");
    }
}

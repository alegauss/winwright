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
        //
        // WW198: said by the reading rather than by the spelling. The call with its bracket was
        // chosen so prose would not match; the lines are now read as code, so a comment naming the
        // call exactly is not counted either.
        var found = Sleeps.Found().Select(one => one.File).ToList();

        Assert.DoesNotContain("Waits.cs", found);
        Assert.DoesNotContain("Criteria.cs", found);
        Assert.DoesNotContain("SleepTests.cs", found);

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
    public void More_than_one_spelling_of_parking_a_thread_is_read()
    {
        // WW198. The catalogue argued that a ban "would be answered by somebody spelling the sleep
        // differently, and then nothing would know about it at all" — and it matched one spelling,
        // so it was answered from inside. FrameRun sleeps for the bulk of an interval and spins for
        // the last sixteen milliseconds, and the count said one.
        Assert.Contains("Thread.Sleep(", Sleeps.Spellings, StringComparer.Ordinal);
        Assert.Contains("Thread.SpinWait(", Sleeps.Spellings, StringComparer.Ordinal);
        Assert.True(Sleeps.Spellings.Count > 1);

        // The measurement, and it is what makes the widening more than a longer list: this file is
        // two parkings and was catalogued as one.
        var frames = Assert.Single(Sleeps.Found(), one => one.File == "FrameRun.cs");
        Assert.Equal(2, frames.Sleeps);
    }

    [Fact]
    public void A_thread_parked_on_a_signal_is_seen_and_then_called_what_it_is()
    {
        // The other half of what widening found: three files park on an event, which is the opposite
        // of a sleep and was invisible to a reading that matched one call. Seen first, judged second
        // — an unseen parking cannot be called right.
        var signalled = Sleeps.Known.Where(one => one.Kind == Sleeping.OnASignal).ToList();

        Assert.NotEmpty(signalled);
        Assert.Contains(signalled, one => one.File == "PumpedDialog.cs");
        Assert.Contains(signalled, one => one.File == "TrayIconFixture.cs");

        // And none of them is counted among the ones that are still waits, which is the number this
        // block's criterion is really about and which widening the reading must not inflate.
        Assert.DoesNotContain(Sleeps.Waiting(), one => one.Kind == Sleeping.OnASignal);
    }

    [Fact]
    public void A_sleep_that_exists_because_looking_disturbs_it_is_called_that_and_not_a_wait()
    {
        // WW329. The arm this catalogue did not have, and the reason it needed one: the entry was a
        // sleep in the engine's own typing path, and every word already here would have been wrong
        // about it. It is not the interval between looks, it is not the resolution of a measurement,
        // it is not the thing under test — and calling it a wait that stays one would say there is
        // nothing to observe, when what is true is that observing is what breaks it.
        //
        // WW353 gave the click one too, and WW355 measured whether typing's could go: a cheaper read
        // took the provocation down thirty-one times and not to nothing, so both are here and both
        // carry what they were measured at.
        var undisturbed = Sleeps.Known.Where(one => one.Kind == Sleeping.Undisturbed).ToList();

        Assert.Contains(undisturbed, one => one.File == "Keyboard.cs");
        Assert.Contains(undisturbed, one => one.File == "Pointer.cs");

        // The measurement, in the entry. A pause with no number beside it is the guess this project
        // refuses everywhere else, and this one replaced a repair that had numbers of its own.
        Assert.All(undisturbed, one => Assert.Contains("rounds", one.Because, StringComparison.Ordinal));

        // And none of them counted among the ones that are still waits, which is the number Block C's
        // criterion is about: a new arm that inflated it would be the criterion loosened rather than
        // a distinction drawn.
        Assert.DoesNotContain(Sleeps.Waiting(), one => one.Kind == Sleeping.Undisturbed);
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

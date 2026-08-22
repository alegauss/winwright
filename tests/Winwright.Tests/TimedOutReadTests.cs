using Winwright.Asserting;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW54. No reading and a window that was talking are reported with the same sentence.
/// <para>
/// The first test is the pair the task exists for, side by side: two reads that both waited the
/// whole deadline and both came back without what they wanted, and which mean opposite things.
/// One is a slow machine. The other is a window nobody is reading — and telling a person to raise
/// the deadline there is how a defect hunt spends an afternoon on timing and ends somewhere else.
/// </para>
/// </summary>
public sealed class TimedOutReadTests
{
    private const string Computing = "Computing...";
    private const string Rendering = "Rendering...";

    private static Expectation Waited(Func<string?> read) => Expect.That("the status line", "Ready", read, 80, 10);

    [Fact]
    public void The_two_timeouts_that_read_alike_are_two_different_endings()
    {
        var busy = Reads.Diagnose(Waited(() => Computing), Computing);
        var silent = Reads.Diagnose(Waited(() => null), Computing);

        // Same deadline, same lack of an answer, opposite meanings.
        Assert.Equal(ReadEnded.Working, busy.Ended);
        Assert.Equal(ReadEnded.Blank, silent.Ended);
        Assert.NotEqual(busy.Sentence(), silent.Sentence());
    }

    [Fact]
    public void The_blank_one_is_pointed_at_the_locator_and_never_at_the_deadline()
    {
        var read = Reads.Diagnose(Waited(() => null), Computing);

        Assert.Contains("nothing was ever in the tree", read.Sentence());
        Assert.Contains("look at the locator", read.Remedy);

        // The sentence that cost the afternoon, and the one this ending may never carry.
        Assert.Contains("not at the deadline", read.Remedy);
    }

    [Fact]
    public void The_working_one_says_what_it_was_saying_and_how_long_it_said_it()
    {
        var read = Reads.Diagnose(Waited(() => Computing), Computing);

        var said = read.Sentence();

        Assert.Contains("was working for the whole wait", said);
        Assert.Contains($"last reading '{Computing}'", said);
        Assert.Contains($"{read.PollsThatSaw} of {read.Watched.Polls} polls saw it", said);
        Assert.Equal(read.Watched.Polls, read.PollsThatSaw);
    }

    [Fact]
    public void A_window_that_stopped_working_and_said_something_else_is_not_working()
    {
        var polls = 0;

        // 'Computing' then 'Could not load'. It did not work the whole time — it stopped and told
        // you something, and calling that Working would send the reader back to the deadline.
        var read = Reads.Diagnose(
            Waited(() => ++polls <= 2 ? Computing : "Could not load"), Computing, Rendering);

        Assert.Equal(ReadEnded.Wrong, read.Ended);
        Assert.Contains("which nothing declares as working", read.Sentence());
        Assert.Contains("this is about the value and not about the wait", read.Remedy);
    }

    [Fact]
    public void A_window_that_moved_between_two_working_states_was_still_working()
    {
        var polls = 0;

        var read = Reads.Diagnose(
            Waited(() => ++polls <= 3 ? Computing : Rendering), Computing, Rendering);

        Assert.Equal(ReadEnded.Working, read.Ended);
        Assert.Contains($"last reading '{Rendering}'", read.Sentence());
    }

    [Fact]
    public void A_project_that_declared_no_working_text_is_told_so_rather_than_guessed_at()
    {
        var read = Reads.Diagnose(Waited(() => Computing));

        // Without declared working text every busy window lands in Wrong. That is a real limit
        // and it is printed, because a reader not told about it reads a confident diagnosis.
        Assert.Equal(ReadEnded.Wrong, read.Ended);
        Assert.False(read.KnowsWhatWorkingLooksLike);
        Assert.Contains("No working text is declared for this project", read.Sentence());
        Assert.Contains("cannot be told from one that answered wrongly", read.Sentence());
    }

    [Fact]
    public void A_project_that_did_declare_it_carries_no_such_caveat()
    {
        var read = Reads.Diagnose(Waited(() => Computing), Computing);

        Assert.True(read.KnowsWhatWorkingLooksLike);
        Assert.DoesNotContain("No working text is declared", read.Sentence());
    }

    [Fact]
    public void A_read_that_arrived_says_so_and_asks_for_nothing()
    {
        var read = Reads.Diagnose(Expect.Now("the status line", "Ready", () => "Ready"), Computing);

        Assert.Equal(ReadEnded.Arrived, read.Ended);
        Assert.Contains("read 'Ready' after", read.Sentence());
        Assert.Equal("nothing: it arrived.", read.Remedy);
        Assert.DoesNotContain("What to do", read.Sentence());
    }

    [Fact]
    public void Blank_and_working_never_share_a_remedy()
    {
        var endings = new[] { ReadEnded.Blank, ReadEnded.Working, ReadEnded.Wrong }
            .Select(ending => ending switch
            {
                ReadEnded.Blank => Reads.Diagnose(Waited(() => null), Computing),
                ReadEnded.Working => Reads.Diagnose(Waited(() => Computing), Computing),
                _ => Reads.Diagnose(Waited(() => "Could not load"), Computing),
            })
            .Select(read => read.Remedy)
            .ToList();

        Assert.Equal(3, endings.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void The_working_readings_can_be_derived_rather_than_typed()
    {
        var folder = Directory.CreateTempSubdirectory("winwright-working-").FullName;
        try
        {
            var strings = Path.Combine(folder, "strings.en.json");
            File.WriteAllText(
                strings, """{ "busy": { "computing": "Computing...", "rendering": "Rendering..." } }""");

            var read = Reads.Diagnose(
                Waited(() => Rendering), DerivedSet.From("the working readings", strings, "busy"));

            // One less hardcoded list to fall behind the window it describes.
            Assert.Equal(ReadEnded.Working, read.Ended);
            Assert.Equal([Computing, Rendering], read.Working);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void The_result_a_verdict_counts_carries_the_same_sentence()
    {
        var blank = Reads.Diagnose(Waited(() => null), Computing).AsAssertion();
        var arrived = Reads.Diagnose(Expect.Now("the status line", "Ready", () => "Ready"), Computing).AsAssertion();

        Assert.Equal(AssertionOutcome.Failed, blank.Outcome);
        Assert.Equal("the status line", blank.Name);
        Assert.Contains("nothing was ever in the tree", blank.Detail);
        Assert.Equal(AssertionOutcome.Passed, arrived.Outcome);
    }

    [Fact]
    public void An_expectation_or_a_set_that_is_not_there_is_refused()
    {
        Assert.Throws<ArgumentNullException>(() => Reads.Diagnose(null!, Computing));
        Assert.Throws<ArgumentNullException>(() => Reads.Diagnose(Waited(() => null), (string[])null!));
        Assert.Throws<ArgumentNullException>(() => Reads.Diagnose(Waited(() => null), (DerivedSet)null!));
    }
}

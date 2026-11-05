using Winwright.Acting;
using Winwright.Asserting;
using Winwright.Projects;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW192. A hole says whose it was. WW183 wrote the judgement down and only the suite read it, so a
/// run that never checked three things named the absent condition beside each and left the reader to
/// work out from those names whether to clear a machine or open a repository.
/// </summary>
public sealed class HoleDivisionTests
{
    private static AssertionResult Hole(string named, string condition, string because) =>
        AssertionResult.Unchecked(named, Precondition.Absent(condition, because));

    [Fact]
    public void A_condition_this_engine_calls_the_desks_is_the_desks()
    {
        Assert.Equal(Whose.Desk, Holes.Of(Precondition.Absent(Foreground.PreconditionName, "somebody else has it")));
        Assert.Equal(Whose.Desk, Holes.Of(Precondition.Absent(TraySearch.PreconditionName, "no chevron")));
        Assert.Equal(Whose.Desk, Holes.Of(Precondition.Absent(OverflowState.PreconditionName, "it would not open")));
    }

    [Fact]
    public void A_condition_about_the_thing_under_test_is_this_runs()
    {
        // The four DeskFactTests names as absences: a stale binary, a page still computing, a
        // window's own glass, a running instance that is not the binary named. Each is a repository
        // to open, and excusing an assertion on one would excuse it on the defect it looked for.
        Assert.Equal(Whose.UnderTest, Holes.Of(Precondition.Absent(Staleness.PreconditionName, "no binary")));
        Assert.Equal(Whose.UnderTest, Holes.Of(Precondition.Absent(LoadingCheck.PreconditionName, "still computing")));
        Assert.Equal(Whose.UnderTest, Holes.Of(Precondition.Absent(Winwright.Capturing.Glass.PreconditionName, "mica")));
        Assert.Equal(
            Whose.UnderTest,
            Holes.Of(Precondition.Absent(Winwright.Processes.RunningBinary.PreconditionName, "a different build")));
    }

    [Fact]
    public void A_condition_nobody_declared_is_counted_and_never_assigned()
    {
        // WW190 found one of these in this engine: a name composed at the throw site, in no
        // catalogue, answering to nothing. Rounding it into the desk's would excuse a defect and
        // rounding it into this run's would send a reader to the wrong repository.
        Assert.Equal(Whose.Unclassified, Holes.Of(Precondition.Absent("an overflow this run can open", "no")));
        Assert.Equal(Whose.Unclassified, Holes.Of(Precondition.Absent("a free notification area", "no")));

        // And a hole carrying no condition at all — which this engine will not build, because
        // Unchecked refuses a null. Answered rather than thrown at, since Missing is nullable and a
        // caller holding a result somebody else composed gets a bucket instead of an exception.
        Assert.Equal(Whose.Unclassified, Holes.Of((Precondition?)null));
        Assert.Throws<ArgumentNullException>(() => AssertionResult.Unchecked("the menu opens", null!));
    }

    [Fact]
    public void Every_condition_this_engine_declares_is_one_of_the_first_two()
    {
        // The property that makes the third bucket meaningful: it holds what nobody declared, and
        // not what nobody got round to. A declared condition falling into it would mean the sweep
        // that reads them had stopped seeing one.
        Assert.All(
            Holes.Declared,
            one => Assert.NotEqual(Whose.Unclassified, Holes.Of(Precondition.Absent(one, "because"))));

        Assert.NotEmpty(Holes.Declared);
        Assert.Contains(Foreground.PreconditionName, Holes.Declared);
        Assert.Contains(Staleness.PreconditionName, Holes.Declared);
    }

    [Fact]
    public void An_assertion_that_ran_is_not_a_hole_anybody_apportions()
    {
        Assert.Throws<ArgumentException>(() => Holes.Of(AssertionResult.Pass("the window opens")));
        Assert.Throws<ArgumentException>(() => Holes.Of(AssertionResult.Fail("the window opens", "it did not")));
    }

    [Fact]
    public void A_run_divides_its_holes_and_passes_over_what_ran()
    {
        var divided = Holes.Divide([
            AssertionResult.Pass("the window opens"),
            AssertionResult.Fail("the title reads Claude", "it read Cloud"),
            Hole("the box is ticked", Foreground.PreconditionName, "somebody else has it"),
            Hole("the icon is there", TraySearch.PreconditionName, "no chevron"),
            Hole("the report renders", Staleness.PreconditionName, "there is no binary"),
            Hole("the menu opens", "a condition nobody declared", "who knows"),
        ]);

        Assert.Equal(2, divided.Desk);
        Assert.Equal(1, divided.UnderTest);
        Assert.Equal(1, divided.Unclassified);
        Assert.Equal(4, divided.Total);
        Assert.Equal("2 the desk's, 1 this run's, 1 unclassified", divided.Sentence());
    }

    [Fact]
    public void One_kind_reads_as_all_of_them_rather_than_as_the_same_number_twice()
    {
        // "3 unchecked (3 the desk's)" is one number printed twice and a reader has to check they
        // agree. "all the desk's" is the same fact and cannot be misread as a second count.
        var desk = Holes.Divide([
            Hole("one", Foreground.PreconditionName, "somebody else has it"),
            Hole("two", TrayMenu.PreconditionName, "no focus"),
        ]);

        Assert.Equal("all the desk's", desk.Sentence());
        Assert.Equal("all this run's", Holes.Divide([Hole("one", Staleness.PreconditionName, "no binary")]).Sentence());
        Assert.Equal("none", Holes.Divide([AssertionResult.Pass("the window opens")]).Sentence());
    }

    [Fact]
    public void The_headline_says_how_many_of_each_and_a_clean_run_says_nothing()
    {
        var degraded = RunVerdict.Over([
            AssertionResult.Pass("the window opens"),
            Hole("the box is ticked", Foreground.PreconditionName, "somebody else has it"),
            Hole("the report renders", Staleness.PreconditionName, "there is no binary"),
        ]);

        Assert.Contains("2 unchecked (1 the desk's, 1 this run's)", VerdictSummary.Headline(degraded), StringComparison.Ordinal);

        // And a run with nothing unchecked carries no clause at all: "(none)" beside "0 unchecked"
        // is a second way of saying the same nothing.
        var clean = RunVerdict.Over([AssertionResult.Pass("the window opens")]);

        Assert.Contains("0 unchecked", VerdictSummary.Headline(clean), StringComparison.Ordinal);
        Assert.DoesNotContain("(", VerdictSummary.Headline(clean)[10..], StringComparison.Ordinal);
    }

    [Fact]
    public void The_line_says_which_hole_was_which()
    {
        // The half a headline cannot carry. Two counts and three lines leaves a reader matching
        // condition names against a judgement that lives in another file.
        var summary = VerdictSummary.Render(RunVerdict.Over([
            Hole("the box is ticked", Foreground.PreconditionName, "somebody else has it"),
            Hole("the report renders", Staleness.PreconditionName, "there is no binary"),
            Hole("the menu opens", "a free notification area", "a tray is already resident"),
        ]));

        Assert.Contains($"'{Foreground.PreconditionName}' absent (the desk's)", summary, StringComparison.Ordinal);
        Assert.Contains($"'{Staleness.PreconditionName}' absent (this run's)", summary, StringComparison.Ordinal);

        // The one nobody declared says so on its own line, so the headline's count has somewhere for
        // a reader to go — which is the difference between a number and a thing to classify.
        Assert.Contains("'a free notification area' absent (unclassified)", summary, StringComparison.Ordinal);
        Assert.Contains("3 unchecked (1 the desk's, 1 this run's, 1 unclassified)", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void A_failure_says_nothing_about_whose_because_a_failure_is_the_codes()
    {
        var line = VerdictSummary.Line(AssertionResult.Fail("the title reads Claude", "it read Cloud"));

        Assert.Equal("  failed     the title reads Claude - it read Cloud", line);
        Assert.DoesNotContain("desk", line, StringComparison.Ordinal);
    }

    [Fact]
    public void The_sweep_divides_the_same_way_and_over_the_distinct_holes()
    {
        // WW185's lesson kept: the sweep gets it in the same commit. Divided over the distinct
        // assertions rather than the occurrences, which is the rule this headline already follows.
        var sweep = SweepVerdict.Over([
            new EnvironmentRun("light", RunVerdict.Over([
                Hole("the box is ticked", Foreground.PreconditionName, "somebody else has it"),
                Hole("the report renders", Staleness.PreconditionName, "there is no binary"),
            ])),
            new EnvironmentRun("dark", RunVerdict.Over([
                Hole("the box is ticked", Foreground.PreconditionName, "somebody else has it"),
            ])),
        ]);

        var headline = SweepSummary.Headline(sweep);

        Assert.Contains("2 unchecked (in 3 of them) (1 the desk's, 1 this run's)", headline, StringComparison.Ordinal);
    }
}

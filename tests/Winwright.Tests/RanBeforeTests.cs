using Winwright.Projects;
using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW532. A run that remembers which cases it ran, so the next one over the same selection names a
/// substitution a count cannot see.
/// <para>
/// Raised by a reader of the article about the run that went from 374 to 352, who reproduced it in
/// pytest: four tests passed, one became a skip and one was renamed, and the run still exited 0 with
/// the line people read unchanged. The control here is the run that changed nothing, which has to say
/// so rather than say nothing.
/// </para>
/// </summary>
public sealed class RanBeforeTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-ranbefore-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void A_case_swapped_for_another_at_the_same_total_is_named_both_ways()
    {
        var project = Declared(remembering: true);

        RanBefore.Of(Selection.All, ["opens", "saves", "renames", "quits"], project);
        var read = RanBefore.Of(Selection.All, ["opens", "saves", "renamed", "quits"], project);

        Assert.False(read.Same);
        Assert.Equal(4, read.Count);
        Assert.Equal(["renamed"], read.Appeared);
        Assert.Equal(["renames"], read.Vanished);
        Assert.False(read.AsFinding().Holds);
        Assert.Equal(
            "the cases that ran are not the ones the last run over everything ran: 1 appeared ('renamed'), "
                + "and 1 vanished ('renames'); 1 that vanished said nothing about what it catches ('renames'), "
                + "so whether a claim went with it cannot be told.",
            read.Sentence());
    }

    [Fact]
    public void A_rename_that_kept_its_claim_holds_though_the_names_changed()
    {
        // WW533. The noise a name diff makes: most renames change nothing that is checked.
        var project = Declared(remembering: true);

        RanBefore.Of(Selection.All, [("renames", "a rename that writes nothing"), ("opens", "a blank window")], project);
        var read = RanBefore.Of(
            Selection.All, [("renamed", "a rename that writes nothing"), ("opens", "a blank window")], project);

        Assert.False(read.Same);
        Assert.True(read.Kept);
        Assert.True(read.AsFinding().Holds);
        Assert.Empty(read.Lost);
        Assert.EndsWith("; every claim the last run made is still made.", read.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_claim_no_case_makes_any_more_is_named_as_lost()
    {
        // WW533. The substitution the commenter's pytest run hid behind an unchanged total.
        var project = Declared(remembering: true);

        RanBefore.Of(Selection.All, [("opens", "a blank window"), ("saves", "a save that writes nothing")], project);
        var read = RanBefore.Of(Selection.All, [("opens", "a blank window"), ("prints", "a print with no pages")], project);

        Assert.False(read.Kept);
        Assert.False(read.AsFinding().Holds);
        Assert.Equal(["a save that writes nothing"], read.Lost);
        Assert.Empty(read.Unclaimed);
        Assert.Contains("1 claim no case makes any more ('a save that writes nothing')", read.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_claim_one_case_dropped_while_another_still_makes_it_is_no_loss()
    {
        var project = Declared(remembering: true);

        RanBefore.Of(Selection.All, [("saves", "a save that writes nothing"), ("saves as", "a save that writes nothing")], project);
        var read = RanBefore.Of(Selection.All, [("saves", "a save that writes nothing")], project);

        Assert.Equal(["saves as"], read.Vanished);
        Assert.Empty(read.Lost);
        Assert.True(read.Kept);
    }

    [Fact]
    public void A_record_kept_as_names_alone_still_reads()
    {
        // The shape WW532 wrote: a list of names per selection, which reads as names that claimed nothing.
        var project = Declared(remembering: true);
        Directory.CreateDirectory(Path.GetDirectoryName(project.LastRun)!);
        File.WriteAllText(project.LastRun, """{ "everything": ["opens"] }""");

        var read = RanBefore.Of(Selection.All, [("opens", "a blank window")], project);

        Assert.False(read.First);
        Assert.True(read.Same);
        Assert.True(read.Kept);
    }

    [Fact]
    public void The_same_cases_again_say_so_rather_than_saying_nothing()
    {
        var project = Declared(remembering: true);

        RanBefore.Of(Selection.All, ["opens", "saves"], project);
        var read = RanBefore.Of(Selection.All, ["saves", "opens"], project);

        Assert.True(read.Same);
        Assert.True(read.AsFinding().Holds);
        Assert.Equal(
            "the same 2 cases ran as the last run over everything; every claim the last run made is still made.",
            read.Sentence());
    }

    [Fact]
    public void A_rename_is_named_once_and_is_the_baseline_after_that()
    {
        var project = Declared(remembering: true);

        RanBefore.Of(Selection.All, ["opens", "renames"], project);
        Assert.False(RanBefore.Of(Selection.All, ["opens", "renamed"], project).Same);

        Assert.True(RanBefore.Of(Selection.All, ["opens", "renamed"], project).Same);
    }

    [Fact]
    public void Each_selection_is_compared_with_its_own_last_run()
    {
        // A tag run holds fewer cases than a run of everything, and that is not a vanishing.
        var project = Declared(remembering: true);

        RanBefore.Of(Selection.All, ["opens", "saves", "quits"], project);
        RanBefore.Of(Selection.Tag("smoke"), ["opens"], project);

        Assert.True(RanBefore.Of(Selection.All, ["opens", "saves", "quits"], project).Same);
        Assert.True(RanBefore.Of(Selection.Tag("smoke"), ["opens"], project).Same);
    }

    [Fact]
    public void The_first_run_on_record_is_compared_against_nothing_and_says_so()
    {
        var read = RanBefore.Of(Selection.All, ["opens"], Declared(remembering: true));

        Assert.True(read.Asked);
        Assert.True(read.First);
        Assert.Null(read.AsFinding().Holds);
        Assert.Contains("compared against nothing", read.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_record_that_will_not_read_costs_one_comparison_and_says_why()
    {
        var project = Declared(remembering: true);
        Directory.CreateDirectory(Path.GetDirectoryName(project.LastRun)!);
        File.WriteAllText(project.LastRun, "{ not json");

        var read = RanBefore.Of(Selection.All, ["opens"], project);

        Assert.True(read.First);
        Assert.NotNull(read.Unreadable);
        Assert.Contains("could not be read", read.Sentence(), StringComparison.Ordinal);
        Assert.True(RanBefore.Of(Selection.All, ["opens"], project).Same);
    }

    [Fact]
    public void A_project_that_keeps_no_record_says_nothing_remembers_the_last_run()
    {
        var read = RanBefore.Of(Selection.All, ["opens"], Declared(remembering: false));

        Assert.False(read.Asked);
        Assert.Null(read.AsFinding().Holds);
        Assert.Contains($"declaring '{RanBefore.Key}'", read.Sentence(), StringComparison.Ordinal);
    }

    /// <summary>A declaration in this test's own directory, keeping a record there or not.</summary>
    private ProjectDeclaration Declared(bool remembering)
    {
        var path = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(path, remembering ? """{ "lastRun": "records/last-run.json" }""" : "{ }");
        return ProjectDeclaration.Load(path);
    }
}

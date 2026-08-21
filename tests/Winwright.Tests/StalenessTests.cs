using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW9. A build fails, the previous executable stays where it was, and the run reports on code
/// that is not in the tree. Unchecked rather than failed: what could not be evaluated is the
/// claim the caller actually came for.
/// </summary>
public class StalenessTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-stale-").FullName;

    public void Dispose()
    {
        Directory.Delete(root, recursive: true);
        GC.SuppressFinalize(this);
    }

    private string Write(string relative, DateTime writtenUtc)
    {
        var path = Path.GetFullPath(Path.Combine(root, relative));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, relative);
        File.SetLastWriteTimeUtc(path, writtenUtc);
        return path;
    }

    private static readonly DateTime Morning = new(2026, 8, 21, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Noon = new(2026, 8, 21, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void A_binary_newer_than_every_source_file_is_fresh()
    {
        Write("src/Tray/TrayIcon.cs", Morning);
        var exe = Write("bin/ClaudeTray.exe", Noon);

        var staleness = Staleness.Of(exe, Path.Combine(root, "src"));

        Assert.Equal(StalenessState.Fresh, staleness.State);
        Assert.False(staleness.IsStale);
        Assert.True(staleness.AsPrecondition().Satisfied);
    }

    [Fact]
    public void A_source_file_newer_than_the_binary_makes_the_run_about_the_previous_build()
    {
        var exe = Write("bin/ClaudeTray.exe", Morning);
        var source = Write("src/Tray/TrayIcon.cs", Noon);

        var staleness = Staleness.Of(exe, Path.Combine(root, "src"));

        Assert.Equal(StalenessState.Stale, staleness.State);
        Assert.Equal(source, staleness.NewestSource);
        Assert.Equal(Noon, staleness.Changed);
        Assert.Equal(Morning, staleness.Built);
    }

    [Fact]
    public void Being_stale_is_a_hole_and_never_a_failure()
    {
        var exe = Write("bin/ClaudeTray.exe", Morning);
        Write("src/Tray/TrayIcon.cs", Noon);

        var precondition = Staleness.Of(exe, Path.Combine(root, "src")).AsPrecondition();
        var declaration = AssertionDeclaration.Of(
            "the tray icon shows the new badge", "the notification area", Staleness.PreconditionName);

        var result = declaration.Unchecked(precondition);

        Assert.Equal(AssertionOutcome.Unchecked, result.Outcome);
        Assert.NotEqual(AssertionOutcome.Failed, result.Outcome);
        Assert.Equal(RunOutcome.Degraded, RunVerdict.Over([result]).Outcome);
    }

    [Fact]
    public void The_absence_carries_both_timestamps_so_a_reader_needs_no_second_look()
    {
        var exe = Write("bin/ClaudeTray.exe", Morning);
        Write("src/Tray/TrayIcon.cs", Noon);

        var absence = Staleness.Of(exe, Path.Combine(root, "src")).AsPrecondition().Absence;

        Assert.Contains("ClaudeTray.exe was built 2026-08-21T09:00:00Z", absence);
        Assert.Contains("TrayIcon.cs changed 2026-08-21T12:00:00Z", absence);
    }

    [Fact]
    public void A_binary_that_was_never_built_is_its_own_state()
    {
        Write("src/Tray/TrayIcon.cs", Noon);

        var staleness = Staleness.Of(Path.Combine(root, "bin", "ClaudeTray.exe"), Path.Combine(root, "src"));

        Assert.Equal(StalenessState.NotBuilt, staleness.State);
        Assert.Null(staleness.Built);
        Assert.Contains("there is no binary at", staleness.AsPrecondition().Absence);
    }

    [Fact]
    public void Build_output_inside_the_source_root_is_walked_past()
    {
        var exe = Write("src/bin/Debug/ClaudeTray.exe", Noon);
        Write("src/Tray/TrayIcon.cs", Morning);

        // Without the skip the binary is its own newest source and nothing is ever stale.
        Assert.Equal(StalenessState.Fresh, Staleness.Of(exe, Path.Combine(root, "src")).State);
        Assert.EndsWith("TrayIcon.cs", Staleness.Of(exe, Path.Combine(root, "src")).NewestSource);
    }

    [Fact]
    public void A_project_can_say_what_else_to_walk_past()
    {
        var exe = Write("bin/ClaudeTray.exe", Morning);
        Write("src/generated/Strings.g.cs", Noon);
        Write("src/Tray/TrayIcon.cs", Morning.AddHours(-1));

        Assert.Equal(StalenessState.Stale, Staleness.Of(exe, Path.Combine(root, "src")).State);
        Assert.Equal(
            StalenessState.Fresh,
            Staleness.Of(exe, Path.Combine(root, "src"), ["bin", "obj", "generated"]).State);
    }

    [Fact]
    public void A_binary_written_in_the_same_tick_as_its_source_is_fresh()
    {
        var exe = Write("bin/ClaudeTray.exe", Noon);
        Write("src/Tray/TrayIcon.cs", Noon);

        Assert.Equal(StalenessState.Fresh, Staleness.Of(exe, Path.Combine(root, "src")).State);
    }

    [Fact]
    public void A_source_root_with_nothing_in_it_leaves_the_binary_fresh()
    {
        var exe = Write("bin/ClaudeTray.exe", Morning);
        Directory.CreateDirectory(Path.Combine(root, "src"));

        var staleness = Staleness.Of(exe, Path.Combine(root, "src"));

        Assert.Equal(StalenessState.Fresh, staleness.State);
        Assert.Null(staleness.NewestSource);
    }

    [Fact]
    public void The_run_says_which_binary_it_drove_whatever_the_reading()
    {
        var exe = Write("bin/ClaudeTray.exe", Morning);
        Write("src/Tray/TrayIcon.cs", Noon);

        var sentence = Staleness.Of(exe, Path.Combine(root, "src")).Sentence();

        Assert.Contains(exe, sentence);
        Assert.Contains("built 2026-08-21T09:00:00Z", sentence);
        Assert.Contains("older than", sentence);
    }

    [Fact]
    public void A_declared_source_root_that_is_not_there_refuses_and_says_so()
    {
        var exe = Write("bin/ClaudeTray.exe", Morning);

        var refusal = Assert.Throws<DirectoryNotFoundException>(
            () => Staleness.Of(exe, Path.Combine(root, "nowhere")));

        Assert.Contains("is not there, so nothing says whether", refusal.Message);
    }

    [Fact]
    public void It_reads_both_paths_off_the_project_declaration()
    {
        Write("bin/ClaudeTray.exe", Morning);
        Write("src/Tray/TrayIcon.cs", Noon);
        File.WriteAllText(
            Path.Combine(root, ProjectDeclaration.FileName),
            """{ "executable": "bin/ClaudeTray.exe", "sourceRoot": "src" }""");

        Assert.True(Staleness.Of(ProjectDeclaration.Find(root)).IsStale);
    }
}

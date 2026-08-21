using System.Diagnostics;

using Winwright.Processes;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW10. A harness once reported that every check passed against a tray published the previous
/// afternoon, before the submenu entry being verified existed in it. Two keys, because one is not
/// enough — and the version difference is the sentence a reader can act on.
/// </summary>
public class RunningBinaryTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-attach-").FullName;
    private static readonly DateTime Morning = new(2026, 8, 21, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Noon = new(2026, 8, 21, 12, 0, 0, DateTimeKind.Utc);

    public void Dispose()
    {
        Directory.Delete(root, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static BinaryIdentity Identity(string path, string? version, DateTime written) =>
        new(path, version, written);

    private static ProcessStartInfo LongRunning()
    {
        var start = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };
        start.ArgumentList.Add("/c");
        start.ArgumentList.Add("ping -n 120 127.0.0.1");
        return start;
    }

    [Fact]
    public void Both_keys_agreeing_is_the_binary_the_run_named()
    {
        var check = RunningBinary.Check(
            Identity(@"C:\a\ClaudeTray.exe", "1.4.2.0", Noon),
            Identity(@"C:\b\ClaudeTray.exe", "1.4.2.0", Noon));

        Assert.Equal(AttachmentMatch.Same, check.Match);
        Assert.True(check.Attached);
        Assert.True(check.AsPrecondition().Satisfied);
    }

    [Fact]
    public void A_version_difference_is_reported_in_preference_to_a_write_time_one()
    {
        var check = RunningBinary.Check(
            Identity(@"C:\a\ClaudeTray.exe", "1.4.2.0", Noon),
            Identity(@"C:\b\ClaudeTray.exe", "1.3.9.0", Morning));

        Assert.Equal(AttachmentMatch.DifferentVersion, check.Match);
        Assert.Equal(
            @"C:\a\ClaudeTray.exe is 1.4.2.0 and what is running is 1.3.9.0",
            check.AsPrecondition().Absence);
    }

    [Fact]
    public void The_write_time_catches_what_the_version_cannot()
    {
        // A Debug build and an installed Release carry the same version between releases.
        var check = RunningBinary.Check(
            Identity(@"C:\src\bin\Debug\ClaudeTray.exe", "1.4.2.0", Noon),
            Identity(@"C:\Program Files\ClaudeTray\ClaudeTray.exe", "1.4.2.0", Morning));

        Assert.Equal(AttachmentMatch.DifferentBuild, check.Match);

        var absence = check.AsPrecondition().Absence;
        Assert.Contains("both are 1.4.2.0", absence);
        Assert.Contains("built 2026-08-21T12:00:00Z", absence);
        Assert.Contains("built 2026-08-21T09:00:00Z", absence);
    }

    [Fact]
    public void A_binary_with_no_version_is_told_apart_from_one_that_has_it()
    {
        var check = RunningBinary.Check(
            Identity(@"C:\a\tool.exe", null, Noon),
            Identity(@"C:\b\tool.exe", "1.0.0.0", Noon));

        Assert.Equal(AttachmentMatch.DifferentVersion, check.Match);
        Assert.Contains("is no version and what is running is 1.0.0.0", check.AsPrecondition().Absence);
    }

    [Fact]
    public void Attaching_to_what_this_run_started_recognises_it()
    {
        using var register = new ProcessRegister();
        var launched = register.Launch(LongRunning());

        var check = RunningBinary.Check(Path.Combine(System.Environment.SystemDirectory, "cmd.exe"), launched);

        Assert.Equal(AttachmentMatch.Same, check.Match);
        Assert.StartsWith("attached to ", check.Sentence());
        Assert.Contains("cmd.exe", check.Sentence());
    }

    [Fact]
    public void A_copy_of_the_same_build_with_a_different_write_time_is_caught()
    {
        // Both copies are made from one source read, because a serviced Windows can hand back a
        // different file for the same system path: copying system32\cmd.exe yields a *newer*
        // version than reading version info through that path does. Two copies of one read is the
        // only way to hold the version steady while moving the write time.
        var running = Path.Combine(root, "a", "cmd.exe");
        var named = Path.Combine(root, "b", "cmd.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(running)!);
        Directory.CreateDirectory(Path.GetDirectoryName(named)!);
        File.Copy(Path.Combine(System.Environment.SystemDirectory, "cmd.exe"), running);
        File.Copy(running, named);
        File.SetLastWriteTimeUtc(named, Morning);

        Assert.Equal(BinaryIdentity.Of(running).FileVersion, BinaryIdentity.Of(named).FileVersion);

        var start = LongRunning();
        start.FileName = running;
        using var register = new ProcessRegister();
        var launched = register.Launch(start);

        var check = RunningBinary.Check(named, launched);

        Assert.Equal(AttachmentMatch.DifferentBuild, check.Match);
        Assert.Contains("built 2026-08-21T09:00:00Z", check.AsPrecondition().Absence);
    }

    [Fact]
    public void A_pid_nothing_is_running_as_is_reported_rather_than_thrown()
    {
        var check = RunningBinary.Check(Path.Combine(System.Environment.SystemDirectory, "cmd.exe"), 0x7FFFFFFF);

        Assert.Equal(AttachmentMatch.Unreadable, check.Match);
        Assert.Contains("no process is running as pid", check.AsPrecondition().Absence);
        Assert.Contains("could not be read", check.Sentence());
    }

    [Fact]
    public void The_run_says_which_binary_it_drove_when_they_differ_too()
    {
        var sentence = RunningBinary.Check(
            Identity(@"C:\a\ClaudeTray.exe", "1.4.2.0", Noon),
            Identity(@"C:\b\ClaudeTray.exe", "1.3.9.0", Morning)).Sentence();

        Assert.Equal(
            @"named C:\a\ClaudeTray.exe (1.4.2.0, built 2026-08-21T12:00:00Z), "
            + @"and attached to C:\b\ClaudeTray.exe (1.3.9.0, built 2026-08-21T09:00:00Z).",
            sentence);
    }

    [Fact]
    public void A_binary_that_is_not_there_cannot_be_identified()
    {
        Assert.Throws<FileNotFoundException>(() => BinaryIdentity.Of(Path.Combine(root, "nothing.exe")));
    }
}

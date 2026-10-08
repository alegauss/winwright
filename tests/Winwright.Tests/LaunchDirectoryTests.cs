using System.Diagnostics;
using System.IO;

using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// Where a launch actually started, asked of the process rather than of the object that asked for
/// it. WW512.
/// <para>
/// WW508 set the launch's working directory to the project's root and let a fixture name one of its
/// own, and four cases held it — all four on the <see cref="ProcessStartInfo"/>, which is the
/// declaration about a launch and not the launch. Nothing started a process and asked what a
/// relative argument resolved to, and that is the one thing an adopter passing
/// <c>cases/fixtures/sessions.json</c> is relying on. A <c>WorkingDirectory</c> the launch quietly
/// ignored would have read green.
/// </para>
/// <para>
/// Desk-free, which is why it is worth having here rather than as a scenario. The fixture answers
/// <c>--resolve</c> on the output stream before it draws anything, exactly as <c>--profiles</c> and
/// <c>--tab-names</c> do, so the whole claim is a process started and a line read — and the gate
/// answers it on this host in seconds instead of in a guest.
/// </para>
/// </summary>
public sealed class LaunchDirectoryTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-ww512-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    /// <summary>What the application says <paramref name="path"/> resolves to, started that way.</summary>
    /// <param name="fixture">The fixture, which decides the directory.</param>
    /// <param name="against">The project's root.</param>
    /// <param name="path">The path to ask about, as a fixture argument would spell it.</param>
    private static string Resolved(FixtureDeclaration fixture, string against, string path)
    {
        var start = fixture.Starting(Fixture.Executable(), against);
        start.ArgumentList.Add($"--resolve={path}");
        start.RedirectStandardOutput = true;
        start.CreateNoWindow = true;

        using var running = Process.Start(start)
            ?? throw new InvalidOperationException("the fixture did not start");

        var said = running.StandardOutput.ReadToEnd().Trim();
        Assert.True(running.WaitForExit(30_000), "the read-out did not exit");
        Assert.Equal(0, running.ExitCode);

        return said;
    }

    [Fact]
    public void A_launch_that_declares_no_directory_really_starts_in_the_project_root()
    {
        // `.` is the directory itself, which is what WW508 made the default: not the one the test
        // runner happened to be in, which is what this process is in right now.
        Assert.Equal(
            Path.GetFullPath(root),
            Resolved(FixtureDeclaration.Of("plain"), root, "."));

        Assert.NotEqual(Path.GetFullPath(root), Path.GetFullPath(Environment.CurrentDirectory));
    }

    [Fact]
    public void An_argument_naming_a_file_in_the_project_resolves_against_that_root()
    {
        // The defect quickshell found, read back from inside the application: a fixture passing
        // `--import cases/fixtures/MobaXterm.ini` is passing a path the launched process resolves,
        // and before WW508 it resolved against whichever directory the runner was in.
        Assert.Equal(
            Path.Combine(Path.GetFullPath(root), "cases", "fixtures", "sessions.json"),
            Resolved(FixtureDeclaration.Of("plain"), root, "cases/fixtures/sessions.json"));
    }

    [Fact]
    public void A_fixture_that_names_its_own_directory_starts_in_it()
    {
        Directory.CreateDirectory(Path.Combine(root, "cases", "fixtures"));

        Assert.Equal(
            Path.Combine(Path.GetFullPath(root), "cases", "fixtures"),
            Resolved(FixtureDeclaration.Of("imported", workingDirectory: "cases/fixtures"), root, "."));
    }

    [Fact]
    public void The_staged_directory_is_where_the_token_said_it_was()
    {
        // WW509's half of the same question. The token resolves to a path on the ProcessStartInfo,
        // and this is the process agreeing that it was started there.
        File.WriteAllText(Path.Combine(root, "sessions.json"), "[]");
        var fixture = FixtureDeclaration.Of(
            "with sessions", workingDirectory: FixtureDeclaration.Staged, files: ["sessions.json"]);

        try
        {
            var into = fixture.Stage(root).Into;

            Assert.Equal(Path.GetFullPath(into), Resolved(fixture, root, "."));
            Assert.Equal(
                Path.Combine(Path.GetFullPath(into), "sessions.json"),
                Resolved(fixture, root, "sessions.json"));
        }
        finally
        {
            if (Directory.Exists(fixture.StagedInto(root)))
                Directory.Delete(fixture.StagedInto(root), recursive: true);
        }
    }

    /// <summary>
    /// WW508's own falsification, run: it said the line was falsified where a fixture argument
    /// naming a project-relative file resolved differently under two runners on the same checkout.
    /// <para>
    /// Two runners are what no case here can start, so this asks the question the other way round.
    /// What differed between those runners was the directory the starting process stood in, and
    /// this run stands in one that is not the project's — so the two answers are computed side by
    /// side: what the launched application said, and what the same relative path resolves to in
    /// this process. The launch's answer is the root's. Had it inherited the runner's, as it did
    /// before WW508, it would have been the other one.
    /// </para>
    /// <para>
    /// It does not change this process's current directory to prove that, deliberately. That is
    /// global state in a test assembly, where the next case to run is whichever one the scheduler
    /// picked — and it is one of the two workarounds WW508 and WW509 exist to remove, so reaching
    /// for it here would be this suite doing what it refuses to make an adopter do. Measured: the
    /// mutating version of this case passed alone and failed in the full run.
    /// </para>
    /// </summary>
    [Fact]
    public void A_relative_argument_resolves_against_the_project_and_not_against_the_runner()
    {
        const string named = "cases/fixtures/sessions.json";

        var said = Resolved(FixtureDeclaration.Of("plain"), root, named);
        var hereInstead = Path.GetFullPath(named);

        Assert.Equal(Path.Combine(Path.GetFullPath(root), "cases", "fixtures", "sessions.json"), said);
        Assert.NotEqual(hereInstead, said);
    }
}

using Winwright.Capturing;
using Winwright.Processes;
using Winwright.Windowing;

using Xunit;
using Xunit.Abstractions;

namespace Winwright.Tests;

/// <summary>
/// WW419. Every reading the engine takes about an application, asked while two are answering.
/// <para>
/// The engine asks an application several questions through the in-app half: what surfaces it drew,
/// where its controls are, whether this binary is already running. Each answer comes back through a
/// variable naming a file, and every case in this suite drove exactly one application while it
/// asked. That is not what a run looks like — a scenario attaches to a product that launched a
/// helper, drives two applications that talk to each other, or meets the copy of itself an earlier
/// case left behind.
/// </para>
/// <para>
/// WW387 is what that costs when it is wrong: a reading held per application, right on every host
/// run and wrong on the first guest one, because in one process the question could only be asked
/// about the suite itself. WW405 made a second application cheap and spent it on the one reading
/// that had already gone wrong. These are the rest.
/// </para>
/// <para>
/// The two are told apart by their chrome rather than by where Windows put them. A second window
/// lands where the shell decides, so two plain fixtures can be the same size on one desk and not on
/// the next — and a case that would pass because two rectangles happened to differ is a case that
/// proves nothing on the run where they do not. <c>--chromeless</c> makes the difference the
/// fixture's, which is a fact about the application and the same on every machine.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class TwoApplicationsTests(ITestOutputHelper output) : IDisposable
{
    private readonly ProcessRegister register = new();
    private readonly string root = Directory.CreateTempSubdirectory("winwright-two-").FullName;

    public void Dispose()
    {
        Attachable.StopAndSettle(register);
        register.Dispose();
        Directory.Delete(root, recursive: true);
    }

    /// <summary>One application under test: what it was told, and where it answered.</summary>
    /// <param name="Pid">The process.</param>
    /// <param name="Surfaces">The report file its own variable named.</param>
    /// <param name="Geometry">The dump file its own variable named.</param>
    private sealed record Answering(int Pid, string Surfaces, string Geometry);

    [Fact]
    public void Two_applications_answering_at_once_each_report_their_own_surfaces()
    {
        // The fault this is about is a collision: two applications writing into the file one of
        // them was told about, or into a name neither was. The reader takes the last line for a
        // name, so a collision does not come back as a mess — it comes back as one application's
        // rectangles under both readings, which is a capture asserted against a window nobody drew.
        if (Pair() is not { } both)
            return;

        var (plain, chromeless) = both;

        foreach (var one in new[] { plain, chromeless })
        {
            var window = SurfaceReport.Of(one.Surfaces, "the window");
            var panes = SurfaceReport.Of(one.Surfaces, "the panes");

            Assert.True(window.Reported, $"pid {one.Pid}: {window.Sentence()}");
            Assert.True(panes.Reported, $"pid {one.Pid}: {panes.Sentence()}");

            // Each report internally coherent, which is the half that says it is one application's:
            // two interleaved would leave the panes of one against the window of the other.
            Assert.True(
                Containment.Of(window.Surface!.Bounds, panes.Surface!).Contains,
                $"pid {one.Pid}: {panes.Sentence()}");
        }

        // And the two are different windows. Both reports holding the same rectangle is what one
        // file read twice looks like, and it is the only way this passes without the variable
        // having been honoured.
        Assert.NotEqual(
            SurfaceReport.Of(plain.Surfaces, "the window").Surface!.Bounds,
            SurfaceReport.Of(chromeless.Surfaces, "the window").Surface!.Bounds);
    }

    [Fact]
    public void Two_applications_answering_at_once_each_dump_their_own_geometry()
    {
        if (Pair() is not { } both)
            return;

        var (plain, chromeless) = both;

        var first = GeometryDump.Read(plain.Geometry);
        var second = GeometryDump.Read(chromeless.Geometry);

        foreach (var read in new[] { first, second })
        {
            Assert.NotNull(read.Root);
            Assert.True(read.Elements.Count > 10, read.Sentence());

            // Nothing unreadable, which is the shape a collision takes here: two applications
            // writing a line at a time into one file interleave mid-line.
            Assert.Equal(0, read.Unreadable);
        }

        Assert.NotEqual(first.Root!.Bounds, second.Root!.Bounds);
    }

    [Fact]
    public void The_instance_check_tells_the_two_apart_by_which_of_them_this_run_owns()
    {
        // The reading that decides whether a run may start at all, asked with two of the same
        // binary up. What it is for is a run meeting the copy of itself an earlier case left
        // behind, and until now nothing here had two to tell apart.
        if (Pair() is not { } both)
            return;

        var (plain, chromeless) = both;

        var executable = Fixture.Executable();

        // Both ours: nothing is in the way, whatever else the desk is running under another name.
        var mine = InstanceCheck.Of(executable, ours: [plain.Pid, chromeless.Pid]);
        Assert.DoesNotContain(mine.Others, one => one.Pid == plain.Pid || one.Pid == chromeless.Pid);

        // One ours: the other is another instance, showing a window, and that is what stops a run.
        var half = InstanceCheck.Of(executable, ours: [plain.Pid]);
        Assert.Contains(half.Windowed, one => one.Pid == chromeless.Pid);
        Assert.DoesNotContain(half.Others, one => one.Pid == plain.Pid);
        Assert.True(half.Refuses, half.Sentence());

        // And the other way round, because a reading that named one of them by position rather
        // than by pid would pass the check above and fail this one.
        var other = InstanceCheck.Of(executable, ours: [chromeless.Pid]);
        Assert.Contains(other.Windowed, one => one.Pid == plain.Pid);
        Assert.DoesNotContain(other.Others, one => one.Pid == chromeless.Pid);
    }

    /// <summary>
    /// Two applications up and answering, each told its own report and dump — or nothing, where
    /// this machine did not finish the writes inside the budget this suite declares.
    /// <para>
    /// A hole and not a red, for the reason <see cref="SlowMachine" /> gives: a desk too loaded to
    /// have drawn two windows and written four files has said nothing about whether the readings
    /// collide.
    /// </para>
    /// </summary>
    private (Answering Plain, Answering Chromeless)? Pair()
    {
        var plain = Started("plain");
        var chromeless = Started("chromeless", "--chromeless");

        foreach (var one in new[] { plain, chromeless })
        {
            Waits.Until(
                "draw",
                $"the fixture drew no window (pid {one.Pid})",
                () => TopLevelWindows.Largest(one.Pid) is { Title.Length: > 0 });
        }

        // Both, and waited for together: what this is about is the moment they are both answering,
        // so a pair where the first had finished before the second began would be two single
        // applications one after the other wearing this case's name.
        var waited = Waits.Trying(
            "wrote",
            () => new[] { plain, chromeless }.All(one =>
                SurfaceReport.Read(one.Surfaces).Count > 0 && GeometryDump.Read(one.Geometry).Elements.Count > 0));

        if (waited.Happened)
            return (plain, chromeless);

        // Which of the two ways it ended, proved per file and per application, for the reason
        // FixtureTests.Driven gives: a file that is not there is a machine that ran out of time, and
        // one that is there and reads as nothing is the fixture's doing and stays red.
        var unfinished = Unwritten(plain, "plain").Concat(Unwritten(chromeless, "chromeless")).ToList();
        Assert.True(
            unfinished.Count > 0,
            Waits.Missed(
                "wrote", $"pids {plain.Pid} and {chromeless.Pid} wrote what they drew and it read as nothing", waited));

        SlowMachine.Excusing("wrote", waited, true);

        // Said out loud, because a case that returns quietly is a green covering a check that never
        // ran — and the three here would be three of them, each wearing a name about collisions.
        output.WriteLine(SlowMachine.Sentence(
            "wrote",
            $"two applications writing what they drew ({string.Join(" and ", unfinished)} never arrived)",
            waited));

        return null;
    }

    /// <summary>Which of one application's two files never arrived, by the name a reader looks for.</summary>
    /// <param name="one">The application.</param>
    /// <param name="named">What this case calls it.</param>
    private static IEnumerable<string> Unwritten(Answering one, string named)
    {
        if (!File.Exists(one.Surfaces))
            yield return $"the {named} surface report";

        if (!File.Exists(one.Geometry))
            yield return $"the {named} geometry dump";
    }

    /// <summary>Launch one, with the two channels the harness reads it through.</summary>
    /// <param name="named">What this one's files are called, so a failure says which application.</param>
    /// <param name="flags">What else it is launched with.</param>
    private Answering Started(string named, params string[] flags)
    {
        var surfaces = Path.Combine(root, $"{named}-surfaces.tsv");
        var geometry = Path.Combine(root, $"{named}-geometry.tsv");

        var start = Fixture.Started(flags);
        start.Environment[SurfaceReport.PathVariable] = surfaces;
        start.Environment[GeometryDump.PathVariable] = geometry;

        return new Answering(Attachable.Launch(register, start).Pid, surfaces, geometry);
    }
}

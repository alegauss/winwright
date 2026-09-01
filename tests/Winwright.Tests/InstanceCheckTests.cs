using System.Diagnostics;
using System.Runtime.InteropServices;

using Winwright.Processes;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW12. The failure this refusal exists for returned a picture of another instance's Settings
/// window when Statistics had been asked for, printed the size it captured, and exited zero.
/// <para>
/// This process stands in for the other instance: it is a real process running a real executable,
/// and windows can be created in it, which no launched system binary allows.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class InstanceCheckTests : IDisposable
{
    private const uint WsPopup = 0x80000000;
    private const uint WsVisible = 0x10000000;

    private readonly List<nint> created = [];

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    private void Show(int width, int height)
    {
        var window = CreateWindowExW(0, "Static", "winwright settings", WsPopup | WsVisible, OffScreen.Left, OffScreen.Top, width, height, 0, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
    }

    public void Dispose()
    {
        foreach (var window in created)
            DestroyWindow(window);
    }

    private static string Me => System.Environment.ProcessPath!;

    private static ProcessStartInfo Windowless(string executable)
    {
        var start = new ProcessStartInfo(executable)
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
    public void Another_instance_showing_a_window_stops_the_run()
    {
        Show(420, 300);

        var check = InstanceCheck.Of(Me);

        Assert.True(check.Refuses);
        var refusal = Assert.Throws<AnotherInstanceException>(check.RequireSole);
        Assert.Contains($"pid {System.Environment.ProcessId}", refusal.Message);
        Assert.True(refusal.Message.Contains("winwright settings"), refusal.Message);
    }

    [Fact]
    public void The_refusal_names_the_override_that_gets_past_it()
    {
        Show(420, 300);

        var refusal = Assert.Throws<AnotherInstanceException>(InstanceCheck.Of(Me).RequireSole);

        Assert.Contains($"pass {InstanceCheck.OverrideName} to drive it anyway", refusal.Message);
    }

    [Fact]
    public void The_override_gets_past_it_and_is_named_in_the_output()
    {
        Show(420, 300);

        var check = InstanceCheck.Of(Me, allowOthers: true);

        Assert.False(check.Refuses);
        check.RequireSole();
        Assert.Contains($"allowed by {InstanceCheck.OverrideName}", check.Sentence());
    }

    [Fact]
    public void The_instance_this_run_owns_is_never_another_one()
    {
        Show(420, 300);

        var check = InstanceCheck.Of(Me, ours: [System.Environment.ProcessId]);

        Assert.False(check.Refuses);
        Assert.Empty(check.Others);
        Assert.Equal("nothing else is running this application.", check.Sentence());
    }

    [Fact]
    public void A_candidate_that_will_not_say_what_it_is_running_is_named_rather_than_passed_over_in_silence()
    {
        // WW180, provoked rather than raced. A protected system process carries a name and refuses
        // to say which binary it is running, which is exactly the shape a half-started process has
        // for the moment it has one — and the shape this reading used to drop without a word.
        var check = InstanceCheck.Of(Path.Combine(System.Environment.SystemDirectory, "csrss.exe"));

        if (check.Certain)
        {
            // This session can read them, so there is nothing to pass over. Asserted rather than
            // skipped: a case that says nothing on this arm is one that proves nothing on a machine
            // where it never fires.
            Assert.Empty(check.Unreadable);
            Assert.DoesNotContain("would not say what they are running", check.Sentence());
            return;
        }

        Assert.NotEmpty(check.Unreadable);
        Assert.All(check.Unreadable, one => Assert.True(one.Pid > 0));
        Assert.All(check.Unreadable, one => Assert.False(string.IsNullOrWhiteSpace(one.Because)));

        // Named in the sentence, which is the whole repair: the reading still passes them over, and
        // no longer reports the passing over as nothing.
        Assert.Contains("would not say what they are running", check.Sentence());
        Assert.Contains($"pid {check.Unreadable[0].Pid}", check.Sentence(), StringComparison.Ordinal);

        // And still not a refusal. Refusing on everything unreadable is refusing on an elevated
        // shell somebody left open, which is the reason the passing over was right to begin with.
        Assert.False(check.Refuses);
    }

    [Fact]
    public void A_resident_instance_showing_nothing_is_the_ordinary_case_and_never_stops_a_run()
    {
        var root = Directory.CreateTempSubdirectory("winwright-instance-").FullName;
        try
        {
            var app = Path.Combine(root, "tray.exe");
            File.Copy(Path.Combine(System.Environment.SystemDirectory, "cmd.exe"), app);

            using var running = Attachable.Settling();
            var register = running.Register;
            Attachable.Launch(register, Windowless(app));
            Attachable.Launch(register, Windowless(app));

            var check = InstanceCheck.Of(app);

            // WW180. Both are accounted for, and the assertion is that neither vanished — not that
            // both had finished starting by the time this line ran. Measured twice in eight guest
            // runs: a process created a moment earlier has not mapped its image yet, so it answered
            // nothing to the question and dropped out of a count that then read one.
            //
            // WW283. Across all three, because WW180 closed one of the two ways a candidate leaves
            // and this recurred through the other — the count read one again, in four guest runs of
            // five, with the missing process in neither list. The sentence is the message so a red
            // says which of the three it landed in and what it claimed to be running.
            //
            // WW327 stopped hardening the reading and fixed the setup instead. Both of those went
            // at the question — how a count survives a process that has not mapped its image — when
            // what was wrong is that this case asserted about processes it never waited for.
            // `Attachable.Launch` waits until Windows will say what a pid is running, which is the
            // premise every assertion below depends on, and it has been available the whole time.
            Assert.True(check.Candidates == 2, $"{check.Candidates} of 2 accounted for: {check.Sentence()}");
            Assert.Empty(check.Windowed);
            Assert.False(check.Refuses);
            check.RequireSole();

            if (!check.Certain)
            {
                // The race, caught rather than lost. What matters is that it is named: a reading
                // that passed one over in silence is what made this case flaky in the first place.
                Assert.Contains("would not say what they are running", check.Sentence());
                Assert.All(check.Unreadable, one => Assert.False(string.IsNullOrWhiteSpace(one.Because)));
                return;
            }

            Assert.Equal(2, check.Resident.Count);
            Assert.Contains("resident and showing no window", check.Sentence());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void A_window_below_the_size_floor_does_not_count_as_showing_something()
    {
        Show(4, 4);

        Assert.False(InstanceCheck.Of(Me).Refuses);
    }

    [Fact]
    public void A_different_executable_of_the_same_name_is_not_this_application()
    {
        var root = Directory.CreateTempSubdirectory("winwright-instance-").FullName;
        try
        {
            var here = Path.Combine(root, "here", "tray.exe");
            var there = Path.Combine(root, "there", "tray.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(here)!);
            Directory.CreateDirectory(Path.GetDirectoryName(there)!);
            File.Copy(Path.Combine(System.Environment.SystemDirectory, "cmd.exe"), here);
            File.Copy(here, there);

            using var running = Attachable.Settling();
            var register = running.Register;
            Attachable.Launch(register, Windowless(there));

            // WW283. The one running `there` is not an instance of `here`, and it is now said so
            // rather than skipped: asked about `here`, the reading names it as a candidate running a
            // different binary, which is the claim this case is actually about.
            //
            // WW327. Launched through the wait rather than started and asked about on the next line.
            // Measured on a guest run: this read `nothing else is running this application` — the
            // copy was not in the table yet — and the assertion below then reported the absence of a
            // premise as a failure of the claim.
            var asked = InstanceCheck.Of(here);

            Assert.Empty(asked.Others);
            Assert.True(asked.Candidates == 1, $"nothing carried the name at all: {asked.Sentence()}");
            Assert.Contains(there, asked.Sentence(), StringComparison.OrdinalIgnoreCase);

            var itself = InstanceCheck.Of(there);
            Assert.True(itself.Others.Count == 1, itself.Sentence());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Nothing_running_at_all_says_so_rather_than_saying_nothing()
    {
        var root = Directory.CreateTempSubdirectory("winwright-instance-").FullName;
        try
        {
            var app = Path.Combine(root, "absent.exe");
            File.Copy(Path.Combine(System.Environment.SystemDirectory, "cmd.exe"), app);

            var check = InstanceCheck.Of(app);

            Assert.Empty(check.Others);
            Assert.Equal("nothing else is running this application.", check.Sentence());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

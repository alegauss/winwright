using System.Diagnostics;
using System.Runtime.InteropServices;

using Winwright.Blaming;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW406. The door that opens a dump, driven against a real one.
/// <para>
/// <see cref="BlameTests" /> checks every judgement over frames written down by hand, which is the
/// only way a reading of a run nobody can reproduce gets checked. What that leaves is the
/// translation — finding the runtime in a memory image and copying the stacks out — and a
/// translation asserted by its shape is what WW420 is about: it would go on compiling after the
/// library it calls renamed the thing it reads.
/// </para>
/// <para>
/// So this starts a process with a thread parked in it, images that process, and asks the reading
/// which thread was waiting. A dump of somebody else's process is what this tool does for a living,
/// and it is also the only version of this case that can run beside anything:
/// <c>MiniDumpWriteDump</c> suspends every thread of what it images, so the first draft dumped the
/// test host itself and stalled a case pacing frames twenty to the second by two and a half
/// seconds — a red about this case, wearing another one's name.
/// </para>
/// <para>
/// About a second and eight megabytes into the temp directory, deleted after.
/// </para>
/// <para>
/// Serial by WW125's rule, which is about starting a process and not about needing a desk: a
/// process this suite launches is one that can take the foreground from whatever case is measuring
/// it, and the rule is deliberately coarse because the seven classes that broke it as a convention
/// are why it is a check. This one is also heavy in its own way — <c>MiniDumpWriteDump</c> suspends
/// every thread of what it images — so running alone is what it wants either way.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class BlameDumpTests
{
    /// <summary>
    /// Write a memory image of a process. In-box on every Windows this project targets, which is
    /// what keeps this case from being a second dependency.
    /// </summary>
    [DllImport("dbghelp.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MiniDumpWriteDump(
        IntPtr process,
        int pid,
        SafeHandle file,
        int type,
        IntPtr exception,
        IntPtr user,
        IntPtr callback);

    /// <summary>
    /// What the blame collector asks for, near enough: the private read-write pages the runtime
    /// keeps its own state in, the data segments, handles and thread information. Not the whole of
    /// memory — a full image is hundreds of megabytes, and none of what is extra is a stack.
    /// </summary>
    private const int Enough = 0x00000200 | 0x00000001 | 0x00000004 | 0x00001000 | 0x00000800;

    [Fact]
    public async Task A_dump_of_a_parked_process_names_the_thread_that_was_waiting_and_the_method_it_waits_in()
    {
        var at = Path.Combine(Path.GetTempPath(), $"winwright-blame-{Environment.ProcessId}.dmp");
        using var parked = Parking();

        try
        {
            // The line the parked process writes once its thread has really reached the wait, with
            // a deadline on it: a child that never starts has to be a red naming that rather than a
            // case that hangs the run, which is the failure this whole task is about.
            using var givingUp = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            string? said = null;
            try
            {
                said = await parked.StandardOutput.ReadLineAsync(givingUp.Token);
            }
            catch (OperationCanceledException)
            {
                // Said as an assertion below rather than as a throw, so the red names what was
                // being waited for instead of naming a token.
            }

            Assert.Equal(Parked.Ready, said);

            using (var file = File.Create(at))
            {
                var written = MiniDumpWriteDump(
                    parked.Handle, parked.Id, file.SafeFileHandle, Enough,
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                Assert.True(written, $"MiniDumpWriteDump failed: {Marshal.GetLastWin32Error()}");
            }

            // Bounded, because the failure this case must not have is the one it is about. Finding
            // the runtime in a memory image is somebody else's library looking for a debugging
            // component, and a look that goes to a symbol server on a desk that cannot reach one
            // waits with no deadline of its own. Sixty seconds against a measured second: what a
            // wedge here costs is the whole run, killed by the per-case bound with this suite's
            // output pipe still held open — which is exactly how it cost one.
            var threads = await Task.Run(() => Dump.Threads(at)).WaitAsync(TimeSpan.FromSeconds(60));

            // Every thread of a live .NET process, which is several — and the reading has to have
            // found stacks rather than a list of empty ones.
            Assert.NotEmpty(threads);

            var only = Assert.Single(
                threads,
                one => one.Frames.Any(frame => frame.Contains(Parked.Named, StringComparison.Ordinal)));

            // The whole translation, asserted: the operating system's own number for the thread, and
            // the frames in the order every judgement depends on — innermost first, so the wait is
            // above the method that entered it and not below it.
            Assert.NotEqual(0u, only.Os);
            Assert.NotNull(Waiting.Waits(only));

            var waited = At(only.Frames, "Wait");
            var entered = At(only.Frames, Parked.Named);
            Assert.True(
                waited >= 0 && entered > waited,
                $"the stack came back outermost first: {string.Join(" | ", only.Frames.Take(6))}");

            // And what it is all for: sentences, off a dump nobody wrote by hand. Both of this
            // child's threads are ours — the one parked and the one that started it — and neither
            // is under a case, so which of the two is blamed is not what this asserts. What it
            // asserts is that the parked one is named with its wait, which is the reading.
            Assert.NotNull(Waiting.Blamed(threads));
            Assert.Contains(
                Waiting.Said(threads),
                line => line.Contains($"thread {only.Os}", StringComparison.Ordinal)
                    && line.Contains("Wait", StringComparison.Ordinal));
        }
        finally
        {
            // Closed first and killed second, so the child ends the way it is built to end. Then
            // waited for: a case that leaves this running leaves something holding the handles it
            // inherited, and the run above it waits for output from a process that has gone.
            parked.StandardInput.Close();
            if (!parked.WaitForExit(5000))
                parked.Kill(entireProcessTree: true);

            parked.WaitForExit(5000);

            if (File.Exists(at))
                File.Delete(at);
        }
    }

    /// <summary>
    /// The tool itself, asked to park a thread. Started through its own executable so what is
    /// imaged is a process of the shape the reader meets — a .NET process with the runtime loaded,
    /// which is what a hang dump of a test host is.
    /// </summary>
    private static Process Parking()
    {
        var exe = Path.Combine(
            Path.GetDirectoryName(typeof(Parked).Assembly.Location)!, "Winwright.Blame.exe");

        Assert.True(File.Exists(exe), $"the reader was not built beside the suite: {exe}");

        // All three streams redirected, and the two nobody reads matter most: a child started with
        // them inherited would hold this run's own standard output open after being orphaned, and a
        // `dotnet test` waiting for the end of its output from a process that has already exited is
        // a run that never finishes. Standard input is also how it learns this case is over.
        var started = Process.Start(new ProcessStartInfo(exe, Parked.Flag)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        });

        Assert.NotNull(started);
        return started;
    }

    /// <summary>Where a word first appears in a stack, or -1 where it does not.</summary>
    /// <param name="frames">The stack, innermost first.</param>
    /// <param name="word">What to look for.</param>
    private static int At(IReadOnlyList<string> frames, string word)
    {
        for (var index = 0; index < frames.Count; index++)
            if (frames[index].Contains(word, StringComparison.Ordinal))
                return index;

        return -1;
    }
}

using System.Runtime.InteropServices;

using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW436. A window's title is read with <c>GetWindowTextW</c>, which is two calls wearing one name:
/// for another process's window it reads the caption Windows has cached, and for a window of the
/// calling process it sends <c>WM_GETTEXT</c> and waits for that window's thread to pump.
/// <para>
/// This suite hosts windows inside the test host and parks threads on purpose, so listing this
/// process's windows was a wait on a thread this process had itself stopped. It killed the run three
/// times in about fifteen guest runs, each time taking nine hundred cases with it — and both kept
/// dumps name the same frame, which is what WW406's reader was built to say.
/// </para>
/// <para>
/// The bound below is what makes this a case rather than a second way to lose a run: a listing that
/// never comes back fails here in ten seconds, where the defect it stands for ends the host.
/// </para>
/// <para>
/// Serial because WW125's rule counts a raw window as a window in front of the machine, and marked
/// as needing no desk because neither of these is shown: a popup with no WS_VISIBLE takes no
/// foreground, draws nothing, and is enumerable — which is all either case asks of it.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ParkedTitleTests
{
    private const uint WsPopup = 0x80000000;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_window_of_this_process_whose_thread_will_never_answer_is_passed_over_rather_than_waited_on()
    {
        using var parked = Parked("winwright parked");

        // Read from another thread, because the wait this is about is a wait on somebody else's: a
        // window owned by the calling thread answers out of its own procedure and never blocks, so a
        // reading taken here would prove the opposite of the defect.
        IReadOnlyList<TopLevelWindow>? listed = null;
        var reader = new System.Threading.Thread(
            () => listed = TopLevelWindows.OfProcess(System.Environment.ProcessId, smallest: 0, visibleOnly: false))
        {
            IsBackground = true,
            Name = "winwright: listing while one window is parked",
        };

        reader.Start();

        Assert.True(
            reader.Join(TimeSpan.FromSeconds(10)),
            "listing this process's windows never came back, which is the whole defect: a window whose "
                + "thread is parked was asked for its title with no deadline on the asking");

        var found = listed!.FirstOrDefault(one => one.Handle == parked.Handle);

        Assert.True(found is not null, "the parked window was not in the listing at all");
        Assert.Equal("", found.Title);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_window_of_this_process_that_answers_is_still_read_by_its_title()
    {
        // The other half, and the one a deadline could quietly break: every window of this process is
        // read through the timed send now, so a healthy one has to come back with what it is called.
        // Owned by this thread, which answers the message out of its own window procedure.
        var window = CreateWindowExW(0, "Static", Named, WsPopup, 20, 20, 240, 160, 0, 0, 0, 0);

        Assert.NotEqual(0, window);
        try
        {
            var listing = TopLevelWindows.OfProcess(System.Environment.ProcessId, smallest: 0, visibleOnly: false);
            var found = listing.FirstOrDefault(one => one.Handle == window);

            Assert.True(found is not null, "the window this case made was not in the listing at all");
            Assert.Equal(Named, found.Title);
        }
        finally
        {
            DestroyWindow(window);
        }
    }

    /// <summary>What the answering window says it is called, which is what the reading has to bring back.</summary>
    private const string Named = "winwright answering its name";

    /// <summary>
    /// A top-level window of this process on a thread that creates it and then stops, which is the
    /// state a case parking a thread puts one in. Never shown: nothing here is about the screen, and
    /// a window that took the desk would be a red in somebody else's file.
    /// </summary>
    /// <param name="title">What it would say it is called, if it ever answered.</param>
    private static ParkedWindow Parked(string title) => new(
        () => CreateWindowExW(0, "Static", title, WsPopup, 20, 20, 240, 160, 0, 0, 0, 0),
        DestroyWindow);

    /// <summary>
    /// The window and the thread that owns it, kept together so the thread is released and the window
    /// destroyed by whoever created it — which is the only thread allowed to.
    /// </summary>
    private sealed class ParkedWindow : IDisposable
    {
        private readonly System.Threading.Thread thread;
        private readonly ManualResetEventSlim release = new();

        internal ParkedWindow(Func<nint> making, Func<nint, bool> destroying)
        {
            using var ready = new ManualResetEventSlim();
            nint made = 0;

            thread = new System.Threading.Thread(() =>
            {
                made = making();
                ready.Set();

                // Parked, and never a pause with an end on it: a thread that came back on its own
                // would race this case's own fixture, and what is asserted is what a reading does
                // about a window that will never answer.
                release.Wait();
                destroying(made);
            })
            {
                IsBackground = true,
                Name = "winwright: a window nobody pumps",
            };

            thread.Start();

            if (!ready.Wait(TimeSpan.FromSeconds(10)))
                throw new InvalidOperationException("the parked window never opened");

            Handle = made;
            Assert.NotEqual(0, Handle);
        }

        /// <summary>The window, which is what a listing of this process finds.</summary>
        internal nint Handle { get; }

        public void Dispose()
        {
            release.Set();
            thread.Join(TimeSpan.FromSeconds(10));
            release.Dispose();
        }
    }
}

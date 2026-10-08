using System.Runtime.InteropServices;

using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW13. Windows refuses the foreground to a process that does not already own it, so a run
/// started from an editor drives somebody else's window. The four readings are settled here over
/// two sightings, because which window happens to be foreground on the machine running these
/// tests is exactly the thing that must not decide what they prove.
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ForegroundTests : IDisposable
{
    private const uint WsPopup = 0x80000000;

    private readonly List<nint> created = [];

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    public void Dispose()
    {
        foreach (var window in created)
            DestroyWindow(window);
    }

    /// <summary>
    /// Hidden on purpose. Creating a top-level window <em>with</em> WS_VISIBLE activates it, so
    /// there is no such thing as a visible window this process made and did not bring forward —
    /// which is what the first draft of the test below assumed, and Windows disagreed.
    /// </summary>
    private nint CreateHidden(string title)
    {
        var window = CreateWindowExW(0, "Static", title, WsPopup, 40, 40, 320, 200, 0, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
        return window;
    }

    private static readonly WindowOwner Editor = new(0x1111, 4242, "Code", "ROADMAP.md - winwright - Visual Studio Code");
    private static readonly WindowOwner UnderTest = new(0x2222, 777, "ClaudeTray", "Statistics");
    private static readonly WindowOwner ItsOwnDialog = new(0x3333, 777, "ClaudeTray", "Settings");

    /// <summary>A control inside the window under test, focused through automation: its own handle
    /// holds the desk and its top-level ancestor is the window the step was about.</summary>
    private static readonly WindowOwner ItsTextBox = new(0x4444, 777, "ClaudeTray", "", 0x2222);

    /// <summary>The same window under test, read with its root, which is itself.</summary>
    private static readonly WindowOwner Rooted = new(0x2222, 777, "ClaudeTray", "Statistics", 0x2222);

    [Fact]
    public void The_window_under_test_holding_it_is_the_only_reading_that_passes()
    {
        var foreground = Foreground.Between(UnderTest, UnderTest);

        Assert.Equal(ForegroundState.Ours, foreground.State);
        Assert.True(foreground.Ours);
        Assert.True(foreground.AsPrecondition().Satisfied);
    }

    /// <summary>
    /// WW517. Ours is reached two ways and they are not equally strong: the window holds the desk
    /// itself, or something sharing its top-level window does. The second is the allowance that
    /// keeps a focused text box from reading as somebody else's desk, and it is wider than the
    /// first — keys go to whatever inside that root has the focus.
    /// </summary>
    [Fact]
    public void A_reading_that_is_ours_says_which_finding_made_it_ours()
    {
        var itself = Foreground.Between(Rooted, Rooted);
        var itsRoot = Foreground.Between(ItsTextBox, Rooted);

        Assert.Equal(ForegroundState.Ours, itself.State);
        Assert.Equal(ForegroundState.Ours, itsRoot.State);

        Assert.Equal(OwnedBy.TheWindow, itself.Owned);
        Assert.Equal(OwnedBy.ItsRoot, itsRoot.Owned);
    }

    /// <summary>
    /// The mirror of WW245's rule, which the met reading did not have: every absence names both
    /// sides and this named neither, so a keystroke delivered to the window the step was about and
    /// one sent on a shared root left the same record. That is what left WW516 undiagnosable.
    /// </summary>
    [Fact]
    public void A_met_foreground_records_the_holder_and_the_finding()
    {
        var itself = Foreground.Between(Rooted, Rooted).AsPrecondition();
        var itsRoot = Foreground.Between(ItsTextBox, Rooted).AsPrecondition();

        Assert.True(itself.Satisfied);
        Assert.True(itsRoot.Satisfied);

        Assert.Equal($"{Rooted} holds it, which is the window under test", itself.Presence);
        Assert.Equal(
            $"{ItsTextBox} holds it, which shares a top-level window with the window under test, {Rooted}",
            itsRoot.Presence);

        // The two readings are what a reader has to be able to tell apart, so the sentences differ.
        Assert.NotEqual(itself.Presence, itsRoot.Presence);
    }

    /// <summary>
    /// A condition whose being met says everything there is to say leaves it out, which is the
    /// restraint that keeps the field worth reading: a second sentence on every line marks nothing.
    /// </summary>
    [Fact]
    public void A_reading_that_is_not_ours_says_nothing_about_how_it_would_have_been()
    {
        foreach (var holder in new[] { Editor, ItsOwnDialog, WindowOwner.None })
        {
            var reading = Foreground.Between(holder, UnderTest);

            Assert.Equal(OwnedBy.Nothing, reading.Owned);
            Assert.Equal("", reading.AsPrecondition().Presence);
        }

        Assert.Equal("", Precondition.Met("a second profile is registered").Presence);
    }

    /// <summary>
    /// WW518. An act carries one precondition and typing reads two, so the finding about the desk
    /// was dropped before the line was written. The composition keeps the later condition's name,
    /// which is the constraint rather than a detail: <c>BusyDesk</c> decides whether a hole was the
    /// desk's by that name, so inventing one would make every excusal in the suite say something
    /// untrue.
    /// </summary>
    [Fact]
    public void A_condition_read_after_another_carries_how_the_first_was_met_and_keeps_its_own_name()
    {
        var desk = Foreground.Between(ItsTextBox, Rooted).AsPrecondition();
        var focus = Precondition.Met("the control under test holds the keyboard focus");

        var both = focus.Beside(desk);

        Assert.Equal(focus.Name, both.Name);
        Assert.True(both.Satisfied);
        Assert.Equal(desk.Presence, both.Presence);

        // And where the later one has its own sentence, the two are joined rather than one winning.
        var saying = Precondition.Met(focus.Name, "the box took it on the first attempt").Beside(desk);

        Assert.Equal($"{desk.Presence}; the box took it on the first attempt", saying.Presence);
    }

    /// <summary>
    /// An absence comes back untouched: where the later condition is the one that failed, what it
    /// says is the whole content of the hole, and a sentence about something that did hold would be
    /// padding in front of the answer.
    /// </summary>
    [Fact]
    public void A_condition_that_failed_says_only_that_whatever_was_read_before_it()
    {
        var desk = Foreground.Between(Rooted, Rooted).AsPrecondition();
        var refused = Precondition.Absent("the control under test holds the keyboard focus", "the box never took it");

        var both = refused.Beside(desk);

        Assert.False(both.Satisfied);
        Assert.Equal("the box never took it", both.Absence);
        Assert.Equal("", both.Presence);

        // And the other way round: a desk nobody owned is not dressed up by a focus that held.
        var noDesk = Foreground.Between(Editor, UnderTest).AsPrecondition();

        Assert.False(noDesk.Beside(Precondition.Met("x", "y")).Satisfied);
    }

    [Fact]
    public void A_met_precondition_that_says_how_says_something()
    {
        // Rather than a blank field nobody can grep for, which is what the plain `Met` is already
        // for — two ways to spell "nothing to add" is one of them going unread.
        Assert.Throws<ArgumentException>(() => Precondition.Met("the desk", "   "));
    }

    [Fact]
    public void The_editor_holding_it_names_the_intruder()
    {
        var foreground = Foreground.Between(Editor, UnderTest);

        Assert.Equal(ForegroundState.Elsewhere, foreground.State);
        // WW245: both sides. Naming only the holder is what made a hole about the application under
        // test indistinguishable from a hole about a window nobody attached to.
        Assert.Equal(
            "the foreground belongs to Code (pid 4242) 'ROADMAP.md - winwright - Visual Studio Code', "
                + $"and the window under test is {UnderTest}",
            foreground.AsPrecondition().Absence);
    }

    [Fact]
    public void Another_window_of_the_same_process_is_its_own_reading()
    {
        var foreground = Foreground.Between(ItsOwnDialog, UnderTest);

        Assert.Equal(ForegroundState.SameProcess, foreground.State);
        Assert.Contains("another window of the same process owns it", foreground.AsPrecondition().Absence);
        Assert.Contains("'Settings'", foreground.AsPrecondition().Absence);
    }

    [Fact]
    public void A_desk_where_nothing_has_the_keyboard_says_so()
    {
        var foreground = Foreground.Between(WindowOwner.None, UnderTest);

        Assert.Equal(ForegroundState.Nobody, foreground.State);
        Assert.Equal(
            $"nothing owns the foreground, and the window under test is {UnderTest}",
            foreground.AsPrecondition().Absence);
    }

    [Fact]
    public void Losing_the_foreground_is_a_hole_and_never_a_failure()
    {
        var missing = Foreground.Between(Editor, UnderTest).AsPrecondition();
        var result = AssertionResult.Unchecked("Escape closes Settings", missing);

        Assert.Equal(AssertionOutcome.Unchecked, result.Outcome);
        Assert.Equal(RunOutcome.Degraded, RunVerdict.Over([result]).Outcome);
    }

    [Fact]
    public void Waiting_for_the_desk_is_offered_and_retrying_the_act_is_not()
    {
        // WW470 turned half of this over and the half it left is the load-bearing one. The rule
        // used to be one sentence — no wait, no poll, no retry — and a window that was still coming
        // forward was reported as a desk somebody else held because of it. A precondition polled
        // towards arriving is a read; an act sent twice is the defect, and this type still offers
        // nobody a second send.
        var named = typeof(Foreground).GetMethods().Select(method => method.Name).ToList();

        Assert.Contains("Waited", named, StringComparer.Ordinal);
        Assert.DoesNotContain(named, name => name.Contains("Retry", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Once_is_the_single_look_this_type_has_always_taken()
    {
        var mine = CreateHidden("winwright statistics");

        var looked = Foreground.Waited(mine, DeskWait.Once);

        Assert.NotEqual(ForegroundState.Ours, looked.State);
        Assert.Equal(0, looked.WaitedMs);
        Assert.DoesNotContain("later", looked.AsPrecondition().Absence, StringComparison.Ordinal);
    }

    [Fact]
    public void A_desk_that_never_comes_still_holes_and_the_absence_says_how_long_it_waited()
    {
        var mine = CreateHidden("winwright statistics");

        var waited = Foreground.Waited(mine, DeskWait.Of(200, 25));

        // WW470's other half. A wait that turned a hole into a pass would be the thing this
        // project exists to prevent, so what is asserted is that it did not — and that the sentence
        // now says which of the two absences this is.
        Assert.NotEqual(ForegroundState.Ours, waited.State);
        Assert.False(waited.AsPrecondition().Satisfied);
        Assert.True(
            waited.WaitedMs >= 200,
            $"the wait gave up after {waited.WaitedMs}ms, which is less than the 200ms it was given");

        Assert.Contains($"{waited.WaitedMs}ms later", waited.AsPrecondition().Absence, StringComparison.Ordinal);
    }

    [Fact]
    public void A_window_still_coming_forward_is_waited_for_rather_than_named_an_intruder()
    {
        using var arriving = PumpedDialog.Open("winwright late arrival");
        using var holding = PumpedDialog.Open("winwright desk holder");

        // The second window opened onto the desk, so at the moment the wait starts the first does
        // not have it — which is the whole of what the measurement in §WW470 describes.
        holding.BringToFront();
        if (BusyDesk.Excused(Foreground.Check(holding.Frame).AsPrecondition()))
            return;

        var late = new Thread(() =>
        {
            Thread.Sleep(300);
            arriving.BringToFront();
        });

        late.Start();
        var waited = Foreground.Waited(arriving.Frame, DeskWait.Of(5000, 25));
        late.Join();

        // A pass with nothing waited would be this case observing a desk that was already right,
        // which proves the old behaviour rather than the new one.
        Assert.True(
            waited.WaitedMs > 0,
            $"the window held the desk on the first look, so nothing about waiting was measured: {waited.Sentence()}");

        Assert.Equal(ForegroundState.Ours, waited.State);
        Assert.True(waited.AsPrecondition().Satisfied);
    }

    [Fact]
    public void Whatever_holds_the_foreground_right_now_is_read_from_windows()
    {
        var holder = Foreground.Now();

        // WW190. A desk where nothing owns the foreground answers `Nobody`, and the reading is
        // right to: there is no holder to ask about itself. Excused rather than red, because a
        // locked or blank session is the machine's business and not this code's.
        if (BusyDesk.Excused(Foreground.Check(holder.Window).AsPrecondition()))
            return;

        // Nothing here asserts *which* window it is: that is the desk, not the code. What is
        // asserted is that asking the real foreground about itself is the reading that passes.
        Assert.Equal(ForegroundState.Ours, Foreground.Check(holder.Window).State);
    }

    [Fact]
    public void A_window_that_cannot_hold_the_foreground_reads_as_not_holding_it()
    {
        var mine = CreateHidden("winwright statistics");

        var foreground = Foreground.Check(mine);

        Assert.NotEqual(ForegroundState.Ours, foreground.State);
        Assert.False(foreground.AsPrecondition().Satisfied);
        Assert.Contains("winwright statistics", foreground.Sentence());
    }

    [Fact]
    public void The_sentence_says_who_had_it_either_way()
    {
        Assert.Equal(
            "the foreground belongs to the window under test, ClaudeTray (pid 777) 'Statistics'.",
            Foreground.Between(UnderTest, UnderTest).Sentence());

        Assert.Equal(
            "the foreground belongs to Code (pid 4242) 'ROADMAP.md - winwright - Visual Studio Code', "
            + "and the window under test is ClaudeTray (pid 777) 'Statistics'.",
            Foreground.Between(Editor, UnderTest).Sentence());
    }
}

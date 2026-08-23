using Winwright.Acting;
using Winwright.Locating;
using Winwright.Processes;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW157. Input this run made is told from input it did not, so a person at the keyboard stops
/// reading as a defect in the code.
/// <para>
/// In the serial collection, and not because a window is created here. Every case reads whether
/// anything but this run has touched the machine, and a case running beside one that synthesises
/// input would read that input as a stranger's — a suite disturbing the very desk it is measuring,
/// which is the shape of test that agrees with itself.
/// </para>
/// <para>
/// There is no seam and none is wanted. The suite can synthesise input, which is one of the two
/// origins under test, and it can decline to, which is the other. What it cannot do is impersonate
/// a hand, so the cases that would need one assert what holds either way and say so.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ForeignInputTests : IDisposable
{
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;

    private readonly PumpedDialog dialog = PumpedDialog.Open(
        "winwright statistics",
        new PumpedDialog.ChildWindow("Edit", "alpha", WsChild | WsVisible, 20, 20, 220, 24));

    public void Dispose() => dialog.Dispose();

    private Subject On(string locator) =>
        Subject.Unguarded(dialog.Root, Locator.Parse(locator), deadlineMs: 2000, pollMs: 20);

    [Fact]
    public void Input_this_run_synthesised_does_not_read_as_somebody_else()
    {
        ForeignInput.Watch();
        Keyboard.Type(On("Edit[order=top]"), "beta");
        var read = ForeignInput.Read();

        // The heart of it, and the reason this could never have been one call. SendInput advances
        // the machine's last-input time exactly as a hand does, so a reading that asked
        // GetLastInputInfo on its own would call this run's own typing a person at the keyboard.
        //
        // Conditional on something having been sent, because the desk may have refused the act -
        // and a case that asserted regardless would go red for the very reason this task exists.
        if (read.LastSynthesised != 0)
            Assert.True(read.Alone, read.Sentence());
    }

    [Fact]
    public void The_absence_names_the_person_and_never_the_foreground()
    {
        ForeignInput.Watch();
        var read = ForeignInput.Read();

        // The task's own criterion, and it is asserted against the sentence rather than the flag.
        // Naming the foreground is the misattribution this reading exists to remove, so that word
        // turning up here is this task having failed while every other case still passed.
        Assert.DoesNotContain("foreground", read.Sentence(), StringComparison.OrdinalIgnoreCase);

        if (!read.Alone)
        {
            Assert.Contains("somebody", read.Sentence(), StringComparison.Ordinal);
            Assert.DoesNotContain("foreground", read.AsPrecondition().Absence, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Watching_again_forgets_what_came_before()
    {
        Keyboard.Type(On("Edit[order=top]"), "beta");
        ForeignInput.Watch();

        // A run is entitled to its own window on the machine. Carrying a previous run's acts into
        // this one would report a desk as this run's own on the strength of something finished.
        Assert.Equal(0u, ForeignInput.Read().LastSynthesised);
    }

    [Fact]
    public void The_precondition_agrees_with_the_reading_that_produced_it()
    {
        ForeignInput.Watch();
        var read = ForeignInput.Read();
        var condition = read.AsPrecondition();

        Assert.Equal(ForeignInput.PreconditionName, condition.Name);
        Assert.Equal(read.Alone, condition.Satisfied);

        // An absent precondition with no sentence explains nothing, and this is the one that has
        // to survive being read months later by somebody who was not at the machine.
        if (!condition.Satisfied)
            Assert.False(string.IsNullOrWhiteSpace(condition.Absence));
    }

    [Fact]
    public void A_reading_that_is_alone_offers_no_elapsed_time_to_quote()
    {
        ForeignInput.Watch();
        var read = ForeignInput.Read();

        // Zero rather than a number nobody should act on: a duration since an event that did not
        // happen is a figure a reader would put in a bug report.
        if (read.Alone)
            Assert.Equal(0u, read.Ago);
    }

    [Fact]
    public void The_sentence_and_the_flag_never_disagree()
    {
        ForeignInput.Watch();
        var read = ForeignInput.Read();

        Assert.Equal(read.Alone, !read.Sentence().Contains("somebody", StringComparison.Ordinal));
    }

    [Fact]
    public void The_preamble_carries_the_reading_beside_the_desk_and_not_inside_it()
    {
        ForeignInput.Watch();
        var preamble = Preamble.Of(AppTarget.AttachTo(Environment.ProcessId));

        // Both halves of where this belongs. In the one list a runner already asks for, so nobody
        // has to remember it; outside Desk.Conditions, so a person at the machine never trips the
        // WW156 refusal and cancels a run that could have observed everything it needed.
        Assert.NotNull(preamble.Find(ForeignInput.PreconditionName));
        Assert.DoesNotContain(preamble.Machine.Conditions, one => one.Name == ForeignInput.PreconditionName);
    }

    [Fact]
    public void A_person_at_the_machine_never_refuses_the_run()
    {
        ForeignInput.Watch();
        var preamble = Preamble.Of(AppTarget.AttachTo(Environment.ProcessId));

        // Whatever this reading turned out to be, it is not what decides whether the run proceeds.
        Assert.Equal(preamble.Machine.CanObserve, preamble.Refusal("the whole run") is null);
    }
}

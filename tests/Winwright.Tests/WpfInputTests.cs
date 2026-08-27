using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Processes;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW246. Synthesised input against a window whose controls are drawn rather than windowed.
/// <para>
/// The case this suite did not have, and its absence hid the defect for the whole of the tool's life.
/// Every case here that types, clicks or presses drives a <see cref="PumpedDialog" /> — a Win32 dialog
/// where each control is a window with its own handle. <c>Admitted.Window</c> read that handle and
/// answered zero where there was none, which is correct for Win32 and wrong for WPF: a
/// <c>TextBlock</c> or a <c>TextBox</c> has no handle, so the foreground was compared against
/// <em>nothing</em> and every act was a hole for a condition that was never absent.
/// </para>
/// <para>
/// Measured against claude-tray, whose keyboard case had never once run green: <c>the foreground
/// belongs to ClaudeTray (pid 42048) 'Settings', and the window under test is nothing</c> — naming the
/// application the run had itself launched. So this is the negative control the suite was missing, and
/// it is a launched fixture on purpose: a run that drives a WPF application is the tool's whole
/// subject, and it could not.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class WpfInputTests : IDisposable
{
    private readonly Settling settling = Attachable.Settling();
    private readonly AutomationElement? root;

    public WpfInputTests()
    {
        if (!Desk.Read().CanObserve)
            return;

        var launched = settling.Register.Launch(Fixture.Started());
        var drawn = Attempt.UntilTrue(() => TopLevelWindows.Largest(launched.Pid) is not null, 20000, 25);

        Assert.True(drawn.Happened, $"the fixture drew no window in {drawn.WaitedMs}ms");
        root = AutomationElement.FromHandle(TopLevelWindows.Largest(launched.Pid)!.Handle);
    }

    public void Dispose() => settling.Dispose();

    private Subject On(string locator) =>
        Subject.Unguarded(root!, Locator.Parse(locator), deadlineMs: 8000, pollMs: 25);

    /// <summary>
    /// A subject on the Config tab, which WPF does not build until the tab is visited — so the tab is
    /// selected first, through the pattern. A pattern act needs no foreground, which is why it is the
    /// right way to reach the state a synthesised act is then measured in.
    /// </summary>
    private Subject OnConfig(string locator)
    {
        var selected = Act.Select(On("TabItem#configPane"));
        Assert.True(selected.Attempted, selected.ToString());
        return On(locator);
    }

    [Fact]
    public void A_wpf_text_box_belongs_to_a_window_even_though_it_is_not_one()
    {
        if (root is null)
            return;

        var box = On("Button#save");

        // The premise the old reading got right, asserted so it is on the record: this element has no
        // window handle of its own. That is not a defect — it is what a framework that draws its own
        // controls is — and it is why answering zero looked reasonable.
        var element = root.FindFirst(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.AutomationIdProperty, "save"));

        Assert.NotNull(element);
        Assert.Equal(0, (nint)element.Current.NativeWindowHandle);

        // And the conclusion it got wrong: the element belongs to the window the run attached to.
        var admitted = Admitted.To(box);
        Assert.NotEqual(0, admitted.Window);
        Assert.Equal(box.Window, admitted.Window);
    }

    /// <summary>
    /// How many times the typing is driven in one case.
    /// <para>
    /// WW249. One go is what this had, and it failed about one guest run in four — so what it proved
    /// was that typing reaches a WPF box <em>sometimes</em>, and finding out which run was which cost
    /// a re-run every time. Five rounds is the same answer <c>TrayPlacementTests</c> gives to the same
    /// shape: a claim in a case's own name that holds most times is a claim the case is not making.
    /// </para>
    /// <para>
    /// Five and not fifty. Each round is a real send at a real window, so the number is what turns a
    /// one-in-four into a near-certainty rather than what makes the suite slow — and the round it
    /// failed on is in the message, which a single go could never say.
    /// </para>
    /// </summary>
    private const int Rounds = 5;

    [Fact]
    public void Typing_reaches_a_wpf_text_box_every_time_rather_than_most_times()
    {
        if (root is null)
            return;

        var box = OnConfig("Edit#profile");
        for (var round = 1; round <= Rounds; round++)
        {
            // A different string per round, so a box that kept what the last round left is a red
            // rather than a pass on somebody else's text.
            var typing = $"WW246-{round}";

            // WW249. What Windows delivered to the window before this round, so a red can say what
            // arrived during it. The recorder reads WM_CHAR below WPF entirely, which is the one
            // reading that separates a send that arrived already substituted from a text box that
            // dropped a character under load — opposite repairs, in opposite repositories.
            var before = Arrived();

            // Both records accumulate across the rounds, so what this round did is the difference
            // rather than the total — the same reason `before` is kept at all.
            var beforePackets = Packets();

            // Assigned in one step on purpose: DeskAsks traces an excused reading back to the line
            // that assigned it, so an act and its assertion split across two names is an excuse the
            // catalogue cannot see. Measured — this is what it went red about.
            var result = Keyboard.Type(box, typing).AsAssertion($"the box reads {typing} (round {round})");

            // The desk may still be somebody else's — a person typing, another window on top — and
            // that stays a hole rather than becoming a red. What must not happen is a hole naming a
            // window under test of 'nothing', which is the defect wearing the desk's clothes.
            if (BusyDesk.Excused(result))
            {
                Assert.DoesNotContain("the window under test is nothing", result.Missing!.Absence, StringComparison.Ordinal);
                return;
            }

            // The round is in all of it, because WW249's whole difficulty was a red that said which
            // value was wrong and never which attempt: `W6246` for `WW246` is one character
            // substituted rather than lost, and knowing whether that was the first send or the fifth
            // is the difference between a race at the window and a race inside the send.
            // WW249. Both readings, because the boundary they are for is between them: the packets
            // are what SendInput put on the queue and the characters are what TranslateMessage made
            // of them, so a packet already wrong and a right packet under a wrong character are
            // different defects in different places.
            var said = $"round {round} of {Rounds}: {result}."
                + $" Windows had delivered '{before}' and has now delivered '{Arrived()}',"
                + $" injected as '{Injected()}'";

            Assert.True(result.Outcome == AssertionOutcome.Passed, said);
            Assert.True(typing == box.Read().Values.Value, said);

            // And the recorder itself, asserted rather than trusted. An instrument that is only ever
            // read on a red is one nobody finds out is broken until the red arrives — which is what
            // happened on its first guest run, where it answered nothing at all and the measurement
            // it existed to take was lost.
            Assert.True(Arrived().EndsWith(typing, StringComparison.Ordinal), said);

            // WW249. The same for the second reading, in the one claim it can make. The characters
            // are not in the key message — the scan code is eight bits and a code unit is sixteen,
            // and a round that typed correctly recorded seven of them all reading zero — so what
            // this asserts is the count: one injected key per character sent, which is what would
            // change if the send lost or repeated one before the window ever saw it.
            Assert.True(typing.Length == Packets() - beforePackets, said);
        }
    }

    /// <summary>
    /// Every character Windows has delivered to the fixture's window so far, escaped where a console
    /// cannot draw it.
    /// <para>
    /// WW249. The erase before each send is Backspaces and they arrive as characters too, so they are
    /// left in rather than filtered out: a reading that quietly dropped them could not have been used
    /// to rule them out. A recorder that could not be read says so rather than answering the empty
    /// string, which would read in a red exactly like a window that was sent nothing.
    /// </para>
    /// </summary>
    private string Arrived() =>
        On("Text#arrived").Read().Facts?.Name ?? "<the recorder could not be read>";

    /// <summary>
    /// Every character this run injected, as the key it was injected as rather than as the character
    /// the window then made of it.
    /// <para>
    /// WW249. The sixth red said the window is delivered the substituted text, which ruled out WPF
    /// and the control and left the send. This was meant to split what is left, and it does not: the
    /// character was expected in the packet's scan code, and a round that typed correctly recorded
    /// seven packets whose scan code was zero. <c>WM_KEYDOWN</c> gives the scan code eight bits and
    /// a UTF-16 code unit needs sixteen, so it was never going to be there.
    /// </para>
    /// <para>
    /// What is left is the message as it arrived, which still says how many keys were injected and
    /// in what order. That is a real claim about the send — one key per character — and it is not
    /// the claim that was wanted, so the boundary this was for still needs an instrument.
    /// </para>
    /// </summary>
    private string Injected() =>
        On("Text#injected").Read().Facts?.Name ?? "<the recorder could not be read>";

    /// <summary>
    /// How many keys this run has injected so far, counted off the same record. One token per key,
    /// so an unreadable recorder counts none rather than throwing — and the round then fails on a
    /// difference of zero, which is the honest reading of an instrument that said nothing.
    /// </summary>
    private int Packets() => Injected().Count(one => one == '[');

    [Fact]
    public void A_click_reaches_a_wpf_checkbox()
    {
        if (root is null)
            return;

        var box = OnConfig("CheckBox#verbose");
        var was = box.Read().Values.Toggle;

        var result = Pointer.Click(box, PointerReason.PointerIsTheAct).AsAssertion("the box toggles");

        if (BusyDesk.Excused(result))
        {
            Assert.DoesNotContain("the window under test is nothing", result.Missing!.Absence, StringComparison.Ordinal);
            return;
        }

        Assert.True(result.Outcome == AssertionOutcome.Passed, result.ToString());

        // The pointer is the act here rather than an escalation, so what proves it landed is the
        // toggle moving — the pattern route would have moved it whether or not a click was sent.
        Assert.NotEqual(was, box.Read().Values.Toggle);
    }
}

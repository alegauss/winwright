using System.Runtime.InteropServices;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW32. A tab control builds a tab's content on its first visit, so a selection that silently
/// does not land leaves the list inside it never realised — and the case then blames a
/// forty-second scan for a tab it never opened.
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class SelectingTests : IDisposable
{
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint WsTabStop = 0x00010000;
    private const uint CbsDropDownList = 0x0003;
    private const uint CbAddString = 0x0143;

    private readonly PumpedDialog dialog;
    private readonly List<PumpedDialog> decoys = [];

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SendMessageW(nint window, uint message, nint wParam, string lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint FindWindowExW(nint parent, nint after, string? className, string? title);

    public SelectingTests()
    {
        dialog = PumpedDialog.Open(
            "winwright statistics",
            new PumpedDialog.ChildWindow(
                "ComboBox", null, WsChild | WsVisible | WsTabStop | CbsDropDownList, 20, 20, 220, 240));

        var combo = FindWindowExW(dialog.Frame, 0, "ComboBox", null);
        Assert.NotEqual(0, combo);
        foreach (var value in new[] { "Overview", "Statistics", "Advanced" })
            SendMessageW(combo, CbAddString, 0, value);
    }

    public void Dispose()
    {
        // Shut the picker first. A dropped-down combo holds the desk in a way that outlives the
        // window, and the next class to send a key finds the foreground somewhere it cannot name.
        // This is the next task's subject arriving early: an act leaves the window in a state the
        // case after it did not ask for.
        try
        {
            if (Combo.ReadOnce().Values.ExpandCollapse == "Expanded")
                Act.Collapse(Combo);
        }
        catch (NotActionableException)
        {
            // It has already gone, which is the state that was wanted.
        }

        foreach (var decoy in decoys)
            decoy.Dispose();

        dialog.Dispose();
    }

    private void Decoy()
    {
        var decoy = PumpedDialog.Open("winwright decoy");
        decoys.Add(decoy);
        Assert.Equal(ForegroundState.Ours, Foreground.Check(decoy.Frame).State);
    }

    private Subject Item(string name) =>
        new(dialog.Root, Locator.Parse($"""ListItem[name="{name}"]"""), deadlineMs: 2000, pollMs: 20);

    private Subject Combo => new(dialog.Root, Locator.Parse("ComboBox"), deadlineMs: 2000, pollMs: 20);

    /// <summary>
    /// Open the picker first. An item of a shut combo is offscreen, so actionability refuses to
    /// act on it at all — which is the check from another task doing its job, and the reason a
    /// scenario walks the route rather than reaching into a closed list.
    /// </summary>
    private Subject Opened(string name)
    {
        if (Combo.ReadOnce().Values.ExpandCollapse != "Expanded")
            Act.Expand(Combo);

        return Item(name);
    }

    [Fact]
    public void The_pattern_route_confirms_and_no_pointer_is_reached_for()
    {
        var selected = Selecting.Confirmed(Opened("Advanced"), settleMs: 1500);

        Assert.True(selected.Landed);
        Assert.Equal(SelectRoute.Pattern, selected.Route);
        Assert.False(selected.PointerTried);
        Assert.Contains("selected through the pattern and confirmed", selected.ToString());
    }

    [Fact]
    public void A_confirmation_that_cannot_pass_is_a_red_and_never_a_quiet_green()
    {
        // The second condition stands in for the pane the tab should have built. It never becomes
        // true, so the selection is not confirmed however willingly the control agreed to it.
        var selected = Selecting.Confirmed(
            Opened("Advanced"), alsoUntil: () => false, mayUseThePointer: false, settleMs: 300);

        Assert.False(selected.Landed);
        Assert.Equal(SelectRoute.Neither, selected.Route);
        Assert.Contains("the pointer was not allowed", selected.Because);
        Assert.Equal(Winwright.Tracing.StepVerdict.Failed, selected.AsTraceStep().Verdict);
    }

    [Fact]
    public void The_pointer_is_reached_for_only_after_the_confirmation_did_not_pass()
    {
        var selected = Selecting.Confirmed(Opened("Advanced"), alsoUntil: () => false, settleMs: 300);

        Assert.True(selected.PointerTried);
        Assert.False(selected.Landed);

        // Whatever the click then met — a shut picker that put the item offscreen, or a desktop
        // held elsewhere — the caller gets a sentence rather than a throw out of the fallback.
        Assert.Equal(SelectRoute.Neither, selected.Route);
        Assert.NotNull(selected.Because);
    }

    [Fact]
    public void A_click_with_the_desktop_elsewhere_says_so_rather_than_claiming_a_landing()
    {
        Decoy();

        var selected = Selecting.Confirmed(Opened("Advanced"), alsoUntil: () => false, settleMs: 300);

        Assert.False(selected.Landed);
        Assert.Equal(SelectRoute.Neither, selected.Route);
        Assert.NotNull(selected.Because);
    }

    [Fact]
    public void The_second_condition_is_what_says_the_application_agreed_and_not_only_the_control()
    {
        var reached = false;

        var selected = Selecting.Confirmed(
            Opened("Statistics"),
            alsoUntil: () => reached,
            mayUseThePointer: false,
            settleMs: 600);
        Assert.False(selected.Landed);

        reached = true;
        Assert.True(Selecting.Confirmed(Opened("Statistics"), alsoUntil: () => reached, settleMs: 600).Landed);
    }

    [Fact]
    public void Nothing_here_reports_a_landing_it_did_not_confirm()
    {
        foreach (var never in new[] { true, false })
        {
            var selected = Selecting.Confirmed(
                Opened("Overview"), alsoUntil: () => false, mayUseThePointer: never, settleMs: 250);

            Assert.False(selected.Landed);
            Assert.NotEqual(SelectRoute.Pattern, selected.Route);
        }
    }

    [Fact]
    public void Selecting_something_that_is_not_there_is_refused_before_any_of_this()
    {
        Assert.Throws<NotActionableException>(() => Selecting.Confirmed(Item("Nonexistent")));
    }
}

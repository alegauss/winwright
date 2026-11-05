using System.Runtime.InteropServices;
using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Locating;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW115. Half of this block's criterion shipped: a pointer act is its own type, nothing falls
/// back to it, and the set is countable before a run. What none of them carried was <em>why</em> it
/// is one — so the summary listed three locators and three buttons, when what a reader deciding
/// whether a scenario runs unattended wanted was that this one is a bare border with no automation
/// peer, that one a notification-area icon, and the third a segment of a custom template.
/// <para>
/// Three different futures, flattened into one number. The last case is the other half: a reason
/// stated in the file is a claim about the tree, and the tree is right here to be asked.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class DeclaredCostTests : IDisposable
{
    private const uint WsPopup = 0x80000000;
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;

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
        for (var index = created.Count - 1; index >= 0; index--)
            DestroyWindow(created[index]);
    }

    private nint Create(string className, string? title, uint style, nint parent = 0)
    {
        var window = CreateWindowExW(
            0, className, title, style,
            parent == 0 ? OffScreen.Left : 20, parent == 0 ? OffScreen.Top : 20, 320, 200, parent, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
        return window;
    }

    /// <summary>A frame with a button, which offers Invoke, and a label, which offers nothing.</summary>
    private nint Dialog()
    {
        var frame = Create("Static", "winwright statistics", WsPopup | WsVisible);
        Create("Button", "Publish", WsChild | WsVisible, frame);
        Create("Static", "a label", WsChild | WsVisible, frame);
        return frame;
    }

    private static PointerAct Act(string verb, string locator, PointerReason because, string note = "") =>
        new(verb, Locator.Parse(locator), because, note);

    [Fact]
    public void An_act_carries_the_reason_the_pattern_route_was_unavailable()
    {
        var act = Act("click", "Custom#tray", PointerReason.NotificationArea);

        Assert.Equal(PointerReason.NotificationArea, act.Because);
        Assert.Equal(Pointer.Worded(PointerReason.NotificationArea), act.Reason);
        Assert.Contains("drawn by the shell", act.Reason);
    }

    [Fact]
    public void A_note_says_what_the_reason_alone_cannot()
    {
        var act = Act("click", "Pane#chart", PointerReason.CustomTemplate, "the sparkline's third segment");

        Assert.Contains("custom template", act.Reason);
        Assert.Contains("the sparkline's third segment", act.Reason);
    }

    [Fact]
    public void The_summary_reads_as_a_list_of_reasons_rather_than_a_list_of_locators()
    {
        var declared = new List<PointerAct>
        {
            Act("click", "Custom#tray", PointerReason.NotificationArea),
            Act("right-click", "Custom#tray", PointerReason.NotificationArea),
            Act("click", "Pane#border", PointerReason.NoAutomationPeer),
        };

        var said = Pointer.Summarise(declared);

        Assert.StartsWith("3 acts need a real desktop, for 2 reasons.", said);
        Assert.Contains("2 because it is a notification-area icon", said);
        Assert.Contains("1 because the control has no automation peer", said);
    }

    [Fact]
    public void The_three_futures_are_told_apart_rather_than_counted_together()
    {
        // The point of the reasons: a bare border may get a peer, a tray icon never will, and a
        // count of three says neither.
        Assert.True(Pointer.MayGetAPeer(PointerReason.NoAutomationPeer));
        Assert.True(Pointer.MayGetAPeer(PointerReason.CustomTemplate));
        Assert.False(Pointer.MayGetAPeer(PointerReason.NotificationArea));
        Assert.False(Pointer.MayGetAPeer(PointerReason.PointerIsTheAct));
        Assert.False(Pointer.MayGetAPeer(PointerReason.PatternDidNotTake));

        var said = Pointer.Summarise([
            Act("click", "Custom#tray", PointerReason.NotificationArea),
            Act("click", "Pane#border", PointerReason.NoAutomationPeer),
        ]);

        Assert.Contains("1 of them would go away if the application drew a peer.", said);
    }

    [Fact]
    public void A_scenario_whose_acts_will_all_outlive_the_application_says_nothing_about_peers()
    {
        var said = Pointer.Summarise([Act("click", "Custom#tray", PointerReason.NotificationArea)]);

        Assert.DoesNotContain("would go away", said);
        Assert.StartsWith("1 act needs a real desktop, for 1 reason.", said);
    }

    [Fact]
    public void A_scenario_with_no_pointer_act_still_says_so()
    {
        Assert.Equal("no act here needs a real desktop.", Pointer.Summarise([]));
        Assert.Empty(Pointer.Reasons([]));
    }

    [Fact]
    public void A_stated_reason_is_checked_against_the_tree_rather_than_believed()
    {
        var root = AutomationElement.FromHandle(Dialog());

        var checked_ = Pointer.Check(root, [
            Act("click", """Text[name="a label"]""", PointerReason.NoAutomationPeer),
            Act("click", """Button[name="Publish"]""", PointerReason.NoAutomationPeer),
        ]);

        // The label offers nothing, so its declaration stands. The button offers Invoke, so the
        // file is out of date about it — and this is the one moment there is a tree to say so.
        Assert.Single(checked_.Agreed);
        var disputed = Assert.Single(checked_.Disputed);
        Assert.Equal("Publish", disputed.Element.Name);
        Assert.Contains("Invoke", disputed.Offered);
        Assert.Contains("has no automation peer", disputed.Because);
        Assert.Contains("offers Invoke", disputed.Because);
        Assert.True(checked_.Disputes);
    }

    [Fact]
    public void A_reason_that_claims_nothing_about_this_tree_is_recorded_as_unchecked()
    {
        var root = AutomationElement.FromHandle(Dialog());

        var checked_ = Pointer.Check(root, [
            // The shell owns this one, so it is not in the application's tree by definition.
            Act("click", "Custom#tray", PointerReason.NotificationArea),
            // A claim about intent, which no tree answers.
            Act("click", """Button[name="Publish"]""", PointerReason.PointerIsTheAct),
            // A claim about the tree, whose control is not in it yet.
            Act("click", "Pane#later", PointerReason.NoAutomationPeer),
        ]);

        Assert.Equal(3, checked_.Unchecked.Count);
        Assert.False(checked_.Disputes);
        Assert.Contains("3 not checked", checked_.Sentence());
        Assert.DoesNotContain("all 3", checked_.Sentence());
    }

    [Fact]
    public void A_scenario_declaring_no_reason_to_check_says_that_and_not_that_all_was_well()
    {
        var root = AutomationElement.FromHandle(Dialog());

        Assert.Equal("no pointer act declares a reason to check.", Pointer.Check(root, []).Sentence());
    }

    [Fact]
    public void Where_every_reason_holds_the_sentence_says_so_in_one_line()
    {
        var root = AutomationElement.FromHandle(Dialog());

        var checked_ = Pointer.Check(root, [Act("click", """Text[name="a label"]""", PointerReason.NoAutomationPeer)]);

        Assert.Equal("all 1 stated reasons are what the tree says too.", checked_.Sentence());
    }
}

using System.Runtime.InteropServices;
using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Processes;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW136. A pointer act states why the pattern route was unavailable, and a check reads each stated
/// reason back against the live tree — and nothing called it. That is this project's founding
/// defect wearing a new subject: a reason recorded in the file and never read back is the sort of
/// comment that is true the day it is written and quietly false a year later.
/// <para>
/// The join is the reading a run already takes before it starts. A disputed reason is not a failure
/// — the act still works, that being the point of a pointer — so it is a finding beside the six
/// conditions rather than a red, and never something an assertion may be excused by.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class JoinedReasonsTests : IDisposable
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

    /// <summary>A frame with a button that offers Invoke and a label that offers nothing.</summary>
    private AutomationElement Dialog()
    {
        var frame = Create("Static", "winwright statistics", WsPopup | WsVisible);
        Create("Button", "Publish", WsChild | WsVisible, frame);
        Create("Static", "a label", WsChild | WsVisible, frame);
        return AutomationElement.FromHandle(frame);
    }

    private static PointerAct Act(string locator, PointerReason because) =>
        new("click", Locator.Parse(locator), because);

    private static Preamble Read() => Preamble.Of(AppTarget.AttachTo(Environment.ProcessId));

    [Fact]
    public void A_run_that_declares_no_pointer_act_still_says_so_rather_than_nothing()
    {
        var checked_ = Pointer.Check(Dialog(), []);

        var read = Read().Including(checked_.AsFinding());

        var finding = Assert.Single(read.Findings);
        Assert.True(finding.Holds);
        Assert.Contains("no pointer act declares a reason to check", finding.Sentence);
    }

    [Fact]
    public void A_reason_the_tree_disputes_reaches_the_preamble()
    {
        var root = Dialog();
        var checked_ = Pointer.Check(root, [Act("""Button[name="Publish"]""", PointerReason.NoAutomationPeer)]);

        var read = Read().Including(checked_.AsFinding());

        var finding = Assert.Single(read.Differing);
        Assert.Equal(ReasonsChecked.Named, finding.Named);
        Assert.Contains("offers Invoke", finding.Sentence);
    }

    [Fact]
    public void The_finding_is_printed_where_a_reader_is_already_looking()
    {
        var root = Dialog();
        var checked_ = Pointer.Check(root, [Act("""Button[name="Publish"]""", PointerReason.NoAutomationPeer)]);

        var read = Read();
        var rendered = read.Including(checked_.AsFinding()).Render();

        // One line per measurement and then one per finding, so it is read in the same glance.
        //
        // Counted against the same reading without the finding rather than against a number. The
        // number was 8, and WW156 put six desk conditions into the preamble and made it 14 - which
        // caught this case for a change it is not about. What this asserts is that a finding adds
        // exactly one line, and that is true whatever the reading turns out to measure.
        Assert.Equal(read.Render().Count + 1, rendered.Count);
        Assert.StartsWith("  differs ", rendered[^1]);
        Assert.Contains(ReasonsChecked.Named, rendered[^1]);
    }

    [Fact]
    public void A_reason_the_tree_agrees_with_prints_as_agreeing()
    {
        var root = Dialog();
        var checked_ = Pointer.Check(root, [Act("""Text[name="a label"]""", PointerReason.NoAutomationPeer)]);

        var read = Read().Including(checked_.AsFinding());

        Assert.Empty(read.Differing);
        Assert.StartsWith("  agrees  ", read.Render()[^1]);
    }

    [Fact]
    public void The_sentence_says_what_differs_even_where_every_condition_held()
    {
        var root = Dialog();
        var checked_ = Pointer.Check(root, [Act("""Button[name="Publish"]""", PointerReason.NoAutomationPeer)]);

        var said = Read().Including(checked_.AsFinding()).Sentence();

        // Absent and checked read the same to whoever skims, which is the whole reason it is here.
        Assert.Contains("1 of 1 declared reading(s) differ from the application", said);
    }

    [Fact]
    public void A_finding_is_never_a_condition_an_assertion_may_be_excused_by()
    {
        var root = Dialog();
        var checked_ = Pointer.Check(root, [Act("""Button[name="Publish"]""", PointerReason.NoAutomationPeer)]);

        var before = Read();
        var after = before.Including(checked_.AsFinding());

        // The distinction the whole shape rests on: a disputed reason is a stale file and not a
        // desk this run could not have, so nothing may claim it did not run because of one.
        Assert.Equal(before.Conditions.Count, after.Conditions.Count);
        Assert.Equal(before.Measurements.Count, after.Measurements.Count);
        Assert.DoesNotContain(after.Conditions, one => one.Name == ReasonsChecked.Named);
        Assert.DoesNotContain(after.Absent, one => one.Name == ReasonsChecked.Named);
    }

    [Fact]
    public void Joining_nothing_leaves_the_reading_exactly_as_it_was()
    {
        var read = Read();

        Assert.Empty(read.Findings);
        Assert.Equal(read.Sentence(), read.Including().Sentence());
        Assert.Equal(read.Render().Count, read.Including().Render().Count);
    }

    [Fact]
    public void More_than_one_reading_can_be_joined_and_each_keeps_its_line()
    {
        var root = Dialog();
        var disputed = Pointer.Check(root, [Act("""Button[name="Publish"]""", PointerReason.NoAutomationPeer)]);
        var agreed = Pointer.Check(root, [Act("""Text[name="a label"]""", PointerReason.NoAutomationPeer)]);

        var read = Read().Including(disputed.AsFinding()).Including(agreed.AsFinding());

        Assert.Equal(2, read.Findings.Count);
        Assert.Single(read.Differing);
    }
}

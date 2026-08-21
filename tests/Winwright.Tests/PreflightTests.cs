using System.Runtime.InteropServices;
using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW23. Reaching for a pattern a control does not carry is a run-time failure otherwise,
/// discovered on a red run and usually far from the line that caused it.
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class PreflightTests : IDisposable
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

    private nint Create(string className, string? title, uint style, int width, int height, nint parent = 0)
    {
        var window = CreateWindowExW(0, className, title, style, 20, 20, width, height, parent, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
        return window;
    }

    /// <summary>A button, which invokes, and a label, which does not.</summary>
    private AutomationElement Dialog()
    {
        var frame = Create("Static", "winwright statistics", WsPopup | WsVisible, 420, 300);
        Create("Button", "Save", WsChild | WsVisible, 90, 28, frame);
        Create("Static", "a label", WsChild | WsVisible, 120, 20, frame);
        return AutomationElement.FromHandle(frame);
    }

    private static ActRequirement Act(string verb, string locator, string pattern) =>
        new(verb, Locator.Parse(locator), pattern);

    [Fact]
    public void What_an_element_offers_can_be_asked_for_directly()
    {
        var offered = Preflight.Offers(Dialog(), Locator.Parse("""Button[name="Save"]"""));

        Assert.NotNull(offered);
        Assert.Contains("Invoke", offered);
        Assert.Equal(offered.OrderBy(name => name, StringComparer.Ordinal), offered);
    }

    [Fact]
    public void Asking_about_something_that_is_not_there_answers_nothing_rather_than_empty()
    {
        Assert.Null(Preflight.Offers(Dialog(), Locator.Parse("""Slider[name="Volume"]""")));
    }

    [Fact]
    public void An_act_the_control_can_take_clears()
    {
        var checked_ = Preflight.Check(Dialog(), [Act("click", """Button[name="Save"]""", "Invoke")]);

        Assert.False(checked_.Refuses);
        Assert.Single(checked_.Cleared);
        Assert.Equal("every one of 1 acts can be taken by the control it addresses.", checked_.Sentence());
    }

    [Fact]
    public void An_act_reaching_for_a_pattern_the_control_lacks_is_refused_with_both_named()
    {
        var checked_ = Preflight.Check(Dialog(), [Act("click", """Text[name="a label"]""", "Invoke")]);

        var refusal = Assert.Single(checked_.Refused);
        Assert.Contains("click needs Invoke", refusal.Because);
        Assert.Contains("Text 'a label'", refusal.Because);
        Assert.DoesNotContain("Invoke,", refusal.Because[refusal.Because.IndexOf("offers", StringComparison.Ordinal)..]);
    }

    [Fact]
    public void The_refusal_happens_before_the_run_and_names_the_act()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => Preflight.Require(Dialog(), [Act("click", """Text[name="a label"]""", "Invoke")]));

        Assert.StartsWith("""click Text[name="a label"] (needs Invoke)""", refusal.Subject);
        Assert.Contains("needs Invoke", refusal.Because);
    }

    [Fact]
    public void An_act_whose_control_is_not_in_the_tree_yet_is_named_rather_than_refused()
    {
        // It may appear after an earlier step, so refusing it would make every scenario that
        // navigates unloadable — and skipping it silently would report a check that was not made.
        var checked_ = Preflight.Check(Dialog(), [Act("click", """Button[name="Publish"]""", "Invoke")]);

        Assert.False(checked_.Refuses);
        Assert.Single(checked_.Unchecked);
        Assert.Empty(checked_.Cleared);
        Assert.Contains("not checked, their controls not being in the tree yet", checked_.Sentence());
    }

    [Fact]
    public void The_sentence_never_says_every_act_was_checked_while_one_was_not()
    {
        var checked_ = Preflight.Check(Dialog(), [
            Act("click", """Button[name="Save"]""", "Invoke"),
            Act("click", """Button[name="Publish"]""", "Invoke"),
        ]);

        Assert.DoesNotContain("every", checked_.Sentence());
        Assert.Contains("1 of 2 acts cleared", checked_.Sentence());
    }

    [Fact]
    public void Every_refusal_is_reported_and_not_only_the_first()
    {
        var checked_ = Preflight.Check(Dialog(), [
            Act("click", """Text[name="a label"]""", "Invoke"),
            Act("type", """Text[name="a label"]""", "Value"),
        ]);

        Assert.Equal(2, checked_.Refused.Count);
        Assert.Contains("2 refused", checked_.Sentence());
    }

    [Fact]
    public void A_check_over_nothing_says_so()
    {
        Assert.Equal("no act declares a pattern to check.", Preflight.Check(Dialog(), []).Sentence());
    }

    [Fact]
    public void What_preflight_reads_is_what_inspect_prints()
    {
        var dialog = Dialog();
        var offered = Preflight.Offers(dialog, Locator.Parse("""Button[name="Save"]"""))!;

        var tree = Inspect.Under(dialog)!;
        var button = Assert.Single(tree.Walk(), element => element.Facts.ControlType == "Button");

        Assert.All(offered, pattern => Assert.Contains(pattern, Inspect.Line(button)));
    }
}

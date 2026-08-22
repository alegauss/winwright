using System.Runtime.InteropServices;
using System.Windows.Automation;

using Winwright.Locating;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW18. Present in the tree, not offscreen, enabled, and carrying the pattern the act needs. The
/// fourth is the one no browser has to check, and the refusal names which of the four was missing
/// because each has a different remedy.
/// <para>
/// WW127: the cases that need a real element read one this suite made. Read off the live desktop
/// they asserted about whatever the machine happened to be showing, which is a claim about the desk
/// rather than about the engine — and it went red once on a run where nothing had changed.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ActionabilityTests : IDisposable
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

    private static ElementFacts Button(
        bool offscreen = false, bool enabled = true, params string[] patterns) =>
        new("Save", "save", "Button", "Button", offscreen, enabled,
            new Winwright.Windowing.WindowBounds(120, 660, 210, 688),
            patterns.ToHashSet(StringComparer.Ordinal));

    [Fact]
    public void All_four_holding_is_the_only_reading_that_lets_an_act_run()
    {
        var check = ActionabilityCheck.Of(Button(patterns: "Invoke"), "Invoke");

        Assert.True(check.CanAct);
        Assert.Equal(Actionable.Yes, check.State);
        Assert.Empty(check.Missing);
        check.Require("#save");
    }

    [Fact]
    public void Nothing_in_the_tree_is_the_first_of_the_four()
    {
        var check = ActionabilityCheck.Of(null, "Invoke");

        Assert.Equal(Actionable.NotInTree, check.State);
        Assert.Equal("nothing matched, or what matched has gone since.", check.Sentence());
    }

    [Fact]
    public void Offscreen_names_the_remedy_that_is_its_own()
    {
        var check = ActionabilityCheck.Of(Button(offscreen: true, patterns: "Invoke"), "Invoke");

        Assert.Equal(Actionable.Offscreen, check.State);
        Assert.Contains("scroll it into view, or the window is minimised", check.Because);
    }

    [Fact]
    public void Disabled_names_a_different_remedy()
    {
        var check = ActionabilityCheck.Of(Button(enabled: false, patterns: "Invoke"), "Invoke");

        Assert.Equal(Actionable.Disabled, check.State);
        Assert.Contains("the application is not ready for this act yet", check.Because);
    }

    [Fact]
    public void The_missing_pattern_is_the_one_no_browser_has_to_check()
    {
        var check = ActionabilityCheck.Of(Button(false, true, "Value", "ScrollItem"), "Invoke");

        Assert.Equal(Actionable.PatternMissing, check.State);
        Assert.Contains("offers no Invoke pattern; it has ScrollItem, Value", check.Because);
    }

    [Fact]
    public void An_element_offering_nothing_at_all_says_so()
    {
        Assert.Contains("it has none at all", ActionabilityCheck.Of(Button(), "Invoke").Because);
    }

    [Fact]
    public void An_act_that_needs_no_pattern_is_never_refused_for_one()
    {
        Assert.True(ActionabilityCheck.Of(Button()).CanAct);
        Assert.True(ActionabilityCheck.Of(Button(), patternNeeded: null).CanAct);
    }

    [Fact]
    public void Everything_wrong_is_kept_and_the_first_leads()
    {
        var check = ActionabilityCheck.Of(Button(offscreen: true, enabled: false), "Invoke");

        Assert.Equal([Actionable.Offscreen, Actionable.Disabled, Actionable.PatternMissing], check.Missing);
        Assert.Equal(Actionable.Offscreen, check.State);
        Assert.Contains("(also disabled, missing the pattern)", check.Sentence());
    }

    [Fact]
    public void A_pattern_is_never_read_off_an_element_that_was_not_there()
    {
        var check = ActionabilityCheck.Of(null, "Invoke");

        Assert.Single(check.Missing);
        Assert.Equal(Actionable.NotInTree, check.Missing[0]);
    }

    [Fact]
    public void The_refusal_carries_the_locator_and_which_of_the_four_it_was()
    {
        var check = ActionabilityCheck.Of(Button(enabled: false, patterns: "Invoke"), "Invoke");

        var refusal = Assert.Throws<NotActionableException>(() => check.Require("Button#save"));

        Assert.Equal("Button#save", refusal.Locator);
        Assert.Equal(Actionable.Disabled, refusal.Missing);
        Assert.StartsWith("Button#save cannot take this act:", refusal.Message);
    }

    [Fact]
    public void An_element_that_has_gone_reads_as_not_in_the_tree_rather_than_throwing()
    {
        Assert.Null(ElementFacts.Of(null));
        Assert.Equal(Actionable.NotInTree, ActionabilityCheck.Of(ElementFacts.Of(null)).State);
    }

    [Fact]
    public void Facts_are_read_off_a_real_ui_automation_element_with_short_names()
    {
        // WW127: a control this suite made. Read off the desktop root, this asserted that whatever
        // the machine happened to be showing was a Pane — which is a claim about the desk and not
        // about the engine, and it went red once on a run where nothing had changed. A button is a
        // Button on every desk there is.
        var facts = Inspect.Under(AutomationElement.FromHandle(Dialog()))!
            .Walk()
            .Single(one => one.Facts.ControlType == "Button")
            .Facts;

        Assert.Equal("Button", facts.ControlType);
        Assert.DoesNotContain("ControlType.", facts.ControlType);
        Assert.All(facts.Patterns, pattern => Assert.DoesNotContain("PatternIdentifiers", pattern));
    }

    [Fact]
    public void A_real_elements_pattern_names_are_the_ones_the_locator_grammar_uses()
    {
        var button = Inspect.Under(AutomationElement.FromHandle(Dialog()))!
            .Walk()
            .Single(one => one.Facts.ControlType == "Button");

        Assert.NotEmpty(button.Facts.Patterns);
        Assert.All(button.Facts.Patterns, pattern => Assert.True(
            UiaVocabulary.IsPattern(pattern), $"'{pattern}' is not a name the locator grammar accepts"));
    }

    [Fact]
    public void An_element_this_run_did_not_choose_is_read_without_a_claim_about_what_it_is()
    {
        // The desktop root is still worth one reading, as a statement about what the engine does
        // with an element it did not choose. Whatever is under there is whatever the person or the
        // run happened to open, and a custom control in somebody else's application reports
        // whatever it likes — so nothing here says what it should be, only that reading it works.
        var facts = ElementFacts.Of(AutomationElement.RootElement);

        Assert.NotNull(facts);
        Assert.DoesNotContain("ControlType.", facts.ControlType);
        Assert.All(facts.Patterns, pattern => Assert.DoesNotContain("PatternIdentifiers", pattern));

        // And the one thing that has to hold whatever it reports: a step rendered from it parses,
        // because a control type the grammar does not accept is left out rather than written in.
        var step = facts.AsLocatorStep().ToString();
        Assert.True(Locator.TryParse(step, out _, out var because), $"'{step}' is refused: {because}");
    }

    /// <summary>
    /// A window this suite made, off the desk because these only read it. Reproducible on every
    /// machine, which is the difference between a claim about the engine and a claim about whatever
    /// happened to be open.
    /// </summary>
    private nint Dialog()
    {
        var frame = Create("Static", "winwright statistics", WsPopup | WsVisible);
        Create("Button", "Save", WsChild | WsVisible, frame);
        return frame;
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
}

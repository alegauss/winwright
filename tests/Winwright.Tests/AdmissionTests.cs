using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Locating;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW112. The actionability check landed and the chokepoint did not: a verb could read the facts,
/// judge them and require the answer — or it could resolve, take the element and press it, and
/// nothing in the types noticed. The criterion "an act never runs against an element that cannot
/// take it" was met by whoever remembered.
/// <para>
/// What these prove is the door rather than the check: the element a verb needs is handed out only
/// by something that has already judged it, so a verb that wants to skip the check has nothing to
/// call.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class AdmissionTests : IDisposable
{
    private const uint WsPopup = 0x80000000;
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint WsDisabled = 0x08000000;
    private const uint BsAutoCheckBox = 0x0003;

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
        // On the desk rather than past it: an admission judges whether the element is on screen,
        // so a fixture hidden off it would be refused by the rule these tests are about.
        var window = CreateWindowExW(0, className, title, style, 20, 20, 320, 200, parent, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
        return window;
    }

    /// <summary>A frame with one control that takes an act and one that refuses every act.</summary>
    private nint Dialog()
    {
        var frame = Create("Static", "winwright statistics", WsPopup | WsVisible);
        Create("Button", "Wrap lines", WsChild | WsVisible | BsAutoCheckBox, frame);
        Create("Button", "Retired", WsChild | WsVisible | WsDisabled | BsAutoCheckBox, frame);
        Create("Edit", "alpha", WsChild | WsVisible, frame);
        return frame;
    }

    private static Subject On(nint frame, string locator) =>
        Subject.Unguarded(AutomationElement.FromHandle(frame), Locator.Parse(locator), 2000, pollMs: 20);

    [Fact]
    public void Nothing_public_hands_out_the_element_a_verb_would_act_on()
    {
        // The whole rule in one reading: from outside the engine there is no element to be had
        // except through a door, so a verb that forgot the check would not compile.
        Assert.Null(typeof(Resolution).GetProperty("Element", BindingFlags.Public | BindingFlags.Instance));
        Assert.NotNull(typeof(Resolution).GetProperty("Element", BindingFlags.NonPublic | BindingFlags.Instance));

        var reachable = typeof(Reading).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Concat(typeof(Resolution).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(one => one.PropertyType == typeof(AutomationElement));

        Assert.Empty(reachable);
    }

    [Fact]
    public void An_admission_cannot_be_made_without_the_judgement_that_makes_one()
    {
        // The register's move: the type a caller needs is not constructible from outside the thing
        // that judges. Handing one out is the only way to have one.
        Assert.Empty(typeof(Admitted).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    public void A_disabled_control_is_refused_at_the_door_so_the_act_is_never_reached()
    {
        var frame = Dialog();

        var refused = Assert.Throws<NotActionableException>(
            () => Admitted.To(On(frame, """CheckBox[name="Retired"]"""), "Toggle"));

        Assert.Equal(Actionable.Disabled, refused.Missing);
        Assert.Contains("is disabled", refused.Because);
    }

    [Fact]
    public void A_control_offering_no_pattern_for_the_act_is_refused_before_the_pattern_is_asked_for()
    {
        var frame = Dialog();

        var refused = Assert.Throws<NotActionableException>(() => Admitted.To(On(frame, "Edit"), "Toggle"));

        Assert.Equal(Actionable.PatternMissing, refused.Missing);
        Assert.Contains("offers no Toggle pattern", refused.Because);
    }

    [Fact]
    public void A_locator_matching_nothing_is_refused_at_the_door_rather_than_at_the_dereference()
    {
        var frame = Dialog();

        var refused = Assert.Throws<NotActionableException>(() => Admitted.To(On(frame, "Slider")));

        Assert.Equal(Actionable.NotInTree, refused.Missing);
    }

    [Fact]
    public void What_came_through_the_door_carries_the_judgement_that_let_it_through()
    {
        var frame = Dialog();

        var admitted = Admitted.To(On(frame, """CheckBox[name="Wrap lines"]"""), "Toggle");

        Assert.True(admitted.Judged.CanAct);
        Assert.Equal("Toggle", admitted.Judged.PatternNeeded);
        Assert.Equal("Wrap lines", admitted.Facts.Name);
        Assert.Equal("Off", admitted.Values.Toggle);
        Assert.Contains("was admitted", admitted.ToString());
    }

    [Fact]
    public void The_act_runs_against_the_element_the_admission_was_judged_against()
    {
        var frame = Dialog();
        var checkbox = On(frame, """CheckBox[name="Wrap lines"]""");

        Admitted.To(checkbox, "Toggle").Do(
            element => ((TogglePattern)element.GetCurrentPattern(TogglePattern.Pattern)).Toggle());

        Assert.Equal("On", checkbox.ReadOnce().Values.Toggle);
    }

    [Fact]
    public void An_act_that_answers_something_answers_through_the_same_door()
    {
        var frame = Dialog();

        var named = Admitted.To(On(frame, "Edit")).Do(element => element.Current.ControlType.ProgrammaticName);

        Assert.Contains("Edit", named);
    }

    [Fact]
    public void The_window_the_element_belongs_to_is_read_at_the_door_and_not_by_each_verb()
    {
        // Four verbs wanted the top-level window and each walked to it its own way. It is one
        // reading on the admission now, which is the same door rather than four that differ.
        var frame = Dialog();

        Assert.Equal(frame, Admitted.To(On(frame, "Edit")).Window);
    }

    [Fact]
    public void An_admission_taken_against_a_look_already_in_hand_still_judges_that_look()
    {
        var frame = Dialog();
        var disabled = On(frame, """CheckBox[name="Retired"]""");

        var refused = Assert.Throws<NotActionableException>(() => Admitted.Of(disabled, disabled.Read(), "Toggle"));

        Assert.Equal(Actionable.Disabled, refused.Missing);
    }

    [Fact]
    public void Every_verb_that_acts_refuses_the_same_control_for_the_same_reason()
    {
        // The point of one door: the refusal does not depend on which verb was asked, and adding
        // a verb cannot add a route that skips it.
        var frame = Dialog();
        var disabled = On(frame, """CheckBox[name="Retired"]""");

        Assert.Equal(Actionable.Disabled, Assert.Throws<NotActionableException>(() => Act.Toggle(disabled)).Missing);
        Assert.Equal(Actionable.Disabled, Assert.Throws<NotActionableException>(() => Act.Invoke(disabled)).Missing);
        Assert.Equal(Actionable.Disabled, Assert.Throws<NotActionableException>(() => Keyboard.Type(disabled, "x")).Missing);
        Assert.Equal(Actionable.Disabled, Assert.Throws<NotActionableException>(() => Acting.Pointer.Click(disabled, PointerReason.PointerIsTheAct)).Missing);
        Assert.Equal(Actionable.Disabled, Assert.Throws<NotActionableException>(() => Pick.Value(disabled, "Alpha")).Missing);
        Assert.Equal(Actionable.Disabled, Assert.Throws<NotActionableException>(() => Traversal.Nudge(disabled)).Missing);
    }
}

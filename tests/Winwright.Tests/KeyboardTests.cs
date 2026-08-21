using System.Runtime.InteropServices;
using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW26. The windows in claude-tray accepted no keyboard input at all from the day the first one
/// shipped, while every screenshot ever taken of them looked perfect.
/// <para>
/// The fixture holds both halves of that: a text box that takes keys, and a read-only one beside
/// it that looks identical and swallows them.
/// </para>
/// <para>
/// Both are addressed by where they sit and never by what they say. A Win32 edit reports its text
/// as its UI Automation name, so a locator naming the text stops matching the moment the typing
/// works — which is the thing under test being used to find the thing under test, and it read as
/// the control having vanished. Ordering by rectangle is what a locator has for exactly this.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class KeyboardTests : IDisposable
{
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint WsPopup = 0x80000000;
    private const uint EsReadOnly = 0x0800;

    private readonly PumpedDialog dialog = PumpedDialog.Open(
        "winwright statistics",
        new PumpedDialog.ChildWindow("Edit", "alpha", WsChild | WsVisible, 20, 20, 220, 24),
        new PumpedDialog.ChildWindow("Edit", "locked", WsChild | WsVisible | EsReadOnly, 20, 60, 220, 24),
        new PumpedDialog.ChildWindow("Static", "a label", WsChild | WsVisible, 20, 100, 120, 20));

    private readonly List<nint> decoys = [];

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    public void Dispose()
    {
        foreach (var decoy in decoys)
            DestroyWindow(decoy);

        dialog.Dispose();
    }

    private void Decoy()
    {
        var window = CreateWindowExW(
            0, "Static", "winwright decoy", WsPopup | WsVisible, 60, 60, 200, 120, 0, 0, 0, 0);
        Assert.NotEqual(0, window);
        decoys.Add(window);
    }

    private Subject On(string locator) =>
        new(dialog.Root, Locator.Parse(locator), deadlineMs: 2000, pollMs: 20);

    [Fact]
    public void Text_typed_through_the_keyboard_is_read_back_from_the_control()
    {
        var edit = On("Edit[order=top]");
        Assert.Equal(ForegroundState.Ours, Foreground.Check(dialog.Frame).State);

        var typed = Keyboard.Type(edit, "beta");

        Assert.True(typed.Sent);
        Assert.Equal("beta", typed.ReadBack);
        Assert.True(typed.Arrived);
    }

    [Fact]
    public void A_control_that_swallows_every_key_is_caught_by_the_read_back()
    {
        var locked = On("Edit[order=bottom]");

        var typed = Keyboard.Type(locked, "beta");

        Assert.True(typed.Sent);
        Assert.False(typed.Arrived);
        Assert.Equal("locked", typed.ReadBack);
        Assert.True(typed.ReadOnly);
        Assert.Contains("the control says it is read-only", typed.ToString());
    }

    [Fact]
    public void The_pattern_and_the_keyboard_are_two_different_questions()
    {
        // Measured, and worth keeping: UI Automation refuses to write a read-only value, so on
        // this control the two agree. They do not have to. The pattern asks whether the value can
        // be written; typing asks whether a key can reach the control, and only the second one is
        // false on a window whose input path is dead while its value pattern works.
        Assert.Throws<InvalidOperationException>(
            () => Act.SetValue(On("Edit[order=bottom]"), "beta"));

        Assert.Equal("beta", Act.SetValue(On("Edit[order=top]"), "beta").After.Value);
    }

    [Fact]
    public void Typing_with_the_desktop_elsewhere_sends_nothing_and_names_the_intruder()
    {
        var edit = On("Edit[order=top]");
        Decoy();

        var typed = Keyboard.Type(edit, "beta");

        Assert.False(typed.Sent);
        Assert.False(typed.Foreground.Satisfied);
        Assert.Contains("winwright decoy", typed.Foreground.Absence);
        Assert.Contains("nothing was sent", typed.ToString());
        Assert.Equal("alpha", edit.ReadOnce().Values.Value);
    }

    [Fact]
    public void A_control_that_reports_no_value_is_refused_rather_than_typed_into()
    {
        var refusal = Assert.Throws<NotActionableException>(
            () => Keyboard.Type(On("""Text[name="a label"]"""), "beta"));

        Assert.Contains("reports no value, so what was typed could not be read back", refusal.Because);
        Assert.Contains("the screenshot this act exists to replace", refusal.Because);
    }

    [Fact]
    public void Typing_something_that_is_not_there_is_refused_as_not_in_the_tree()
    {
        var refusal = Assert.Throws<NotActionableException>(
            () => Keyboard.Type(On("""Edit[name="absent"]"""), "beta"));

        Assert.Equal(Actionable.NotInTree, refusal.Missing);
    }

    [Fact]
    public void What_did_not_arrive_is_a_failed_step_and_what_was_never_sent_is_a_hole()
    {
        Assert.Equal(
            Winwright.Tracing.StepVerdict.Failed,
            Keyboard.Type(On("Edit[order=bottom]"), "beta").AsTraceStep().Verdict);

        Decoy();
        Assert.Equal(
            Winwright.Tracing.StepVerdict.Unchecked,
            Keyboard.Type(On("Edit[order=top]"), "beta").AsTraceStep().Verdict);
    }

    [Fact]
    public void Typing_appends_where_the_act_says_it_does_not_replace()
    {
        var edit = On("Edit[order=top]");

        var typed = Keyboard.Run(new TypedAct("type", edit.Locator, "beta", ReplacingWhatIsThere: false), edit);

        Assert.Equal("alphabeta", typed.Expected());
        Assert.True(typed.Arrived);
    }

    [Fact]
    public void An_act_says_what_it_types_and_where()
    {
        Assert.Equal(
            """type "beta" into Edit#profile""",
            new TypedAct("type", Locator.Parse("Edit#profile"), "beta").ToString());
    }
}

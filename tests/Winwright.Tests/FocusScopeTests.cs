using System.Runtime.InteropServices;

using Winwright.Acting;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW155. The reading under every menu walk and every traversal was the focused element of the
/// whole desktop, and nothing narrowed it to the window that was asked about — so what a case
/// asserted on was whatever held the desk, and the menu was only implied.
/// <para>
/// Measured twice rather than supposed. Shipping WW143 a submenu case went red with
/// <c>Expected: "one.txt"</c> against <c>Actual: "1 Yes"</c>; shipping WW145 a shift-tab case went
/// red with <c>Expected: "alpha"</c> against <c>Actual: "Mostrar Ícones Ocultos …"</c>, which is the
/// notification area's overflow button. Neither string exists anywhere in this repository, and both
/// classes were green on the next run — a red about the application that nobody can reproduce.
/// </para>
/// <para>
/// What is asserted here is the shape of the answer rather than a reproduction of the race: an
/// element belonging to another application is not compared against a wanted entry, and what comes
/// out is a hole naming what held the desk.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class FocusScopeTests : IDisposable
{
    private readonly PumpedDialog dialog = PumpedDialog.Open(
        "winwright focus",
        new PumpedDialog.ChildWindow("Edit", "alpha", 0x40000000 | 0x10000000 | 0x00010000, 20, 20, 220, 24));

    public void Dispose() => dialog.Dispose();

    [DllImport("user32.dll")]
    private static extern nint GetDesktopWindow();

    [Fact]
    public void The_focus_is_read_against_the_application_and_not_against_the_desk()
    {
        var read = Focus.In(dialog.Frame);

        // This process owns the dialog, and this process holds the focus, so the reading is about
        // something this run is entitled to talk about.
        Assert.True(read.Inside, read.Sentence());
        Assert.NotNull(read.Held);
        Assert.Same(read.Element, read.Held);
        Assert.Equal("", read.Because);
    }

    [Fact]
    public void An_element_in_another_process_is_not_an_answer_about_this_one()
    {
        // The desktop window belongs to the shell, so a reading taken against it is asking whether
        // this process's focused element is the shell's — which is the misattribution, inverted so
        // it can be provoked on demand rather than waited for.
        var read = Focus.In(GetDesktopWindow());

        Assert.False(read.Inside);
        Assert.Null(read.Held);

        // Named and not merely refused: what took the desk is the whole of what a reader needs.
        Assert.Contains("holds the focus", read.Because, StringComparison.Ordinal);
        Assert.Contains("pid ", read.Because, StringComparison.Ordinal);
        Assert.NotNull(read.Element);
    }

    [Fact]
    public void The_sentence_says_which_way_the_reading_went_and_never_only_that_it_went()
    {
        // WW167. Every case here passed this text as the message on a failing assertion, which is
        // text a green never prints — so the one rendering the focus reading has was the only one in
        // the engine no case read back. Both ways round, because the sentence is two sentences.
        var inside = Focus.In(dialog.Frame).Sentence();

        Assert.Contains("holds the focus, and it is this application's.", inside, StringComparison.Ordinal);

        var elsewhere = Focus.In(GetDesktopWindow());

        // The other way round it is the reason and nothing else, ending in a stop like the first:
        // a reader joining these into a report gets two sentences rather than one and a fragment.
        Assert.Equal(elsewhere.Because + ".", elsewhere.Sentence());
        Assert.EndsWith(".", elsewhere.Sentence(), StringComparison.Ordinal);
        Assert.DoesNotContain("this application's", elsewhere.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_focus_that_is_not_this_applications_is_a_hole_and_never_a_failure()
    {
        // This block's criterion, one axis over: nothing about the desk is reported as a defect in
        // the code. A run that could not observe the thing it was asked about did not observe it.
        var elsewhere = Focus.In(GetDesktopWindow());
        var condition = elsewhere.AsPrecondition();

        Assert.False(condition.Satisfied);
        Assert.Equal(FocusReading.Named, condition.Name);
        Assert.Equal(elsewhere.Because, condition.Absence);
    }

    [Fact]
    public void A_window_that_names_no_process_is_said_rather_than_read_as_agreement()
    {
        // Zero is not a window, and a reading against nothing would otherwise compare two zeroes
        // and call them equal — which is the green this whole project refuses.
        var read = Focus.In(0);

        Assert.False(read.Inside);
        Assert.Null(read.Held);
        Assert.Contains("names no process", read.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_walk_whose_focus_left_the_application_is_unchecked_rather_than_red()
    {
        // Built rather than raced: what is asserted is that the answer's shape can carry the
        // misattribution at all, which is what stops it being reported as a failed comparison.
        var elsewhere = Focus.In(GetDesktopWindow());
        Assert.False(elsewhere.Inside, "the desktop window turned out to be this process's");

        var walk = Menu.To(dialog.Frame, "Nonexistent");
        Assert.True(walk.Sent, walk.ToString());

        // The real walk stayed inside, which is the control: the reading is a fact about the run
        // and not a constant, so this case would prove nothing if it never came back true.
        Assert.True(walk.Focus.Inside, walk.ToString());
        Assert.True(walk.Observed);
        Assert.Equal(AssertionOutcome.Failed, walk.AsAssertion("the menu reached it").Outcome);
    }

    [Fact]
    public void The_menu_reading_takes_the_window_it_is_about()
    {
        // The durable half. An unscoped reading is one a caller reaches by having nothing else to
        // hand, and this whole task is what that costs — so the parameter is not optional.
        var reading = typeof(Menu).GetMethod(nameof(Menu.Highlighted));

        Assert.NotNull(reading);
        var window = Assert.Single(reading.GetParameters());
        Assert.Equal(typeof(nint), window.ParameterType);
        Assert.False(window.IsOptional, "the window can be left off, so the desk-wide reading is still reachable");
    }
}

using System.Runtime.InteropServices;
using System.Windows.Automation;

using Winwright.Asserting;
using Winwright.Locating;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW166. Block F's second criterion says a red step carries its diagnosis — the control view it
/// failed to read attached to the failure, so no throwaway script is written to find out what the
/// window had. The type that builds one is bounded, budgeted and thoroughly tested, and outside its
/// own tests nothing in the engine called it.
/// <para>
/// The reason it had no caller is worth stating: every spelling of an expectation took a function
/// and never saw a window, so there was nothing to build a view from at the moment it went red. The
/// verb that holds both halves at once is the repair, and these drive it against a real window.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class DiagnosedRedTests : IDisposable
{
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;

    private readonly PumpedDialog dialog = PumpedDialog.Open(
        "winwright diagnosed",
        new PumpedDialog.ChildWindow("Edit", "alpha", WsChild | WsVisible, 20, 20, 220, 24),
        new PumpedDialog.ChildWindow("Button", "Save", WsChild | WsVisible, 20, 60, 120, 30));

    public void Dispose() => dialog.Dispose();

    private Subject On(string locator) =>
        Subject.Unguarded(AutomationElement.FromHandle(dialog.Frame), Locator.Parse(locator), deadlineMs: 700, pollMs: 40);

    [Fact]
    public void A_red_carries_the_control_view_it_failed_to_read()
    {
        // The whole point: the check had the tree in hand when it went red, and used to print a
        // sentence about it instead — so the reading had to be done twice, once by the harness and
        // once by a person writing a script to ask the same question again.
        var watched = Expect.Of(On("Edit"), "the box", "omega", one => one.Value);

        Assert.False(watched.Held, watched.Sentence());
        Assert.NotNull(watched.Explains);
        Assert.True(watched.Explains!.WasRead, watched.Explains.Absence);
        Assert.NotEmpty(watched.Explains.View);

        // And it marks the element the check was reading rather than the root: a budget that drops
        // the one line the reader came for is worse than no dump at all.
        Assert.True(watched.Explains.Marks, watched.Explains.ToString());
        Assert.Contains(watched.Explains.Render(), one => one.Contains("alpha", StringComparison.Ordinal));
    }

    [Fact]
    public void A_green_carries_none_because_a_dump_under_every_pass_is_read_by_nobody()
    {
        var watched = Expect.Of(On("Edit"), "the box", "alpha", one => one.Value);

        Assert.True(watched.Held, watched.Sentence());
        Assert.Null(watched.Explains);
    }

    [Fact]
    public void A_subject_that_resolves_to_nothing_still_says_what_the_window_held()
    {
        // The case the criterion was written for. Nothing answered the locator, so the sentence is
        // about timing — and the view underneath is the half that says the control is called
        // something else, which is what the throwaway script was written to find out.
        var watched = Expect.Of(On("Edit#nosuchid"), "the missing box", "alpha", one => one.Value);

        Assert.False(watched.Held);
        Assert.False(watched.EverSaw, "the locator resolved, so this is not the case it is about");

        Assert.NotNull(watched.Explains);
        Assert.Contains(watched.Explains!.Render(), one => one.Contains("Save", StringComparison.Ordinal));
    }

    [Fact]
    public void The_view_is_the_one_inspect_prints_and_never_a_second_spelling_of_it()
    {
        var watched = Expect.Of(On("Edit"), "the box", "omega", one => one.Value);
        var inspected = Inspect.Render(Inspect.Window(dialog.Frame)!);

        Assert.NotNull(watched.Explains);

        // Every line under the red is a line inspect would print, with the subject marker in front
        // of one of them. Two spellings of one page is what WW144 closed, and this keeps it closed.
        foreach (var line in watched.Explains!.View)
            Assert.Contains(inspected, one => one.Trim() == line.Text.Trim());
    }

    [Fact]
    public void The_diagnosis_is_reached_from_the_engine_and_not_only_from_a_test()
    {
        // The finding this task was filed over: a capability with no caller is a criterion met by
        // a type existing rather than by a run. A verb that answers one is what changes that, so
        // this asserts the verb is there rather than that the type is.
        var verb = typeof(Expect).GetMethod(nameof(Expect.Of));

        Assert.NotNull(verb);
        Assert.Equal(typeof(Expectation), verb.ReturnType);
        Assert.Contains(verb.GetParameters(), one => one.ParameterType == typeof(Subject));
    }
}

using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Scenarios;
using Winwright.Tracing;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW225. A case could name eight acts and all eight went through a control's own pattern, so the
/// half of the engine that puts input on the desk had no name a data file could write. WW78's
/// keyboard case would have migrated into `set value` and `set range` — the two pattern routes that
/// passed on the day the windows they were about took no keyboard input at all.
/// <para>
/// What is checked here is the difference itself: real keys reaching a real TextBox, and the result
/// carrying what the act needed of the machine. That second field is the one the shape turned on. An
/// act nothing performed and a control that would not move are indistinguishable from a reading, and
/// reporting the first as the second is a red about the application on a fact about the desk.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class SynthesisedActTests : IDisposable
{
    private readonly Settling settling = Attachable.Settling();
    private readonly AutomationElement root;

    public SynthesisedActTests()
    {
        // The names pane, because it draws the one editable control and a label beside it — which is
        // exactly the pair these two verbs need: something to type into, and something that resolves
        // and offers no Invoke.
        var launched = settling.Register.Launch(Fixture.Started("--names"));
        var drawn = Attempt.UntilTrue(() => TopLevelWindows.Largest(launched.Pid) is not null, 20000, 25);

        Assert.True(drawn.Happened, $"the fixture drew no window in {drawn.WaitedMs}ms");
        root = AutomationElement.FromHandle(TopLevelWindows.Largest(launched.Pid)!.Handle);
    }

    public void Dispose() => settling.Dispose();

    private Subject On(string locator) =>
        Subject.Unguarded(root, Locator.Parse(locator), 4000, pollMs: 25);

    [Fact]
    public void Typing_reaches_the_control_with_real_keys_and_reads_back()
    {
        // The observable WW78's first assertion is. `set value` beside this one writes through
        // ValuePattern and cannot fail the way this one can.
        var typed = Synthesised.Type(On("Edit#profileBox"), "beta");

        if (BusyDesk.Excused(typed.Needed!))
            return;

        Assert.True(typed.Attempted, typed.ToString());
        Assert.Equal("beta", typed.After.Value);
        Assert.True(typed.Changed, typed.ToString());
        Assert.Equal("synthesised keyboard", typed.Pattern);
    }

    [Fact]
    public void A_pattern_act_needs_nothing_of_the_machine_and_says_so_by_carrying_nothing()
    {
        // The other half of the shape, and the reason the field is nullable rather than always
        // present: nothing about the desk stops a pattern act, which is why those eight were the
        // whole vocabulary a case could name.
        var written = Act.SetValue(On("Edit#profileBox"), "gamma");

        Assert.Null(written.Needed);
        Assert.True(written.Attempted);
        Assert.Equal(StepVerdict.Ok, written.AsTraceStep().Verdict);
    }

    [Fact]
    public void A_synthesised_act_always_carries_what_it_needed_whichever_way_it_went()
    {
        // Present either way, so a reader never has to know which verbs are which to know whether
        // the answer is about the application.
        var typed = Synthesised.Type(On("Edit#profileBox"), "delta");

        Assert.NotNull(typed.Needed);
        Assert.Equal(typed.Needed!.Satisfied, typed.Attempted);

        // And the trace says the same thing the field does: ok where it ran, unchecked where it did
        // not, because a trace line reading ok over an act nothing performed is the unearned green in
        // miniature.
        Assert.Equal(
            typed.Attempted ? StepVerdict.Ok : StepVerdict.Unchecked,
            typed.AsTraceStep().Verdict);
    }

    [Fact]
    public void Clicking_carries_the_reason_the_case_had_to_write()
    {
        // A label resolves and offers no Invoke, which is why claude-tray's navigation clicked one.
        // The reason is in the route, so a report says why this act was not a pattern act.
        var clicked = Synthesised.Click(On("Text#profileLabel"), PointerReason.PointerIsTheAct);

        if (BusyDesk.Excused(clicked.Needed!))
            return;

        Assert.Contains("synthesised pointer", clicked.Pattern, StringComparison.Ordinal);
        Assert.Contains(nameof(PointerReason.PointerIsTheAct), clicked.Pattern, StringComparison.Ordinal);
    }

    [Fact]
    public void A_click_naming_no_reason_this_engine_has_is_refused_with_the_ones_it_does()
    {
        // Refused rather than defaulted: a click whose justification defaults is a click nobody had
        // to justify, and then every act quietly escalates to the pointer.
        var refused = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            {
              "cases": [
                {
                  "name": "a",
                  "steps": [
                    {
                      "locator": "Text#profileLabel",
                      "act": "click",
                      "with": "because I felt like it",
                      "expect": "Profile"
                    }
                  ]
                }
              ]
            }
            """));

        Assert.Contains("no such reason for a pointer act", refused.Because, StringComparison.Ordinal);
        foreach (var reason in Enum.GetValues<PointerReason>())
            Assert.Contains(reason.ToString(), refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_click_carrying_no_reason_at_all_is_refused_where_the_field_would_have_been()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            {
              "cases": [
                {
                  "name": "a",
                  "steps": [ { "locator": "Text#profileLabel", "act": "click", "expect": "Profile" } ]
                }
              ]
            }
            """));

        Assert.Contains("carries none", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void The_vocabulary_says_which_acts_a_busy_desk_can_take_away()
    {
        // Data rather than a note, so a report can name the acts in a case that needed the machine.
        Assert.Equal(
            ["type", "click"],
            ActVerb.All.Where(one => one.Synthesises).Select(one => one.Name));

        Assert.All(
            ActVerb.All.Where(one => !one.Synthesises),
            one => Assert.False(one.Synthesises, $"'{one.Name}' asks the control and needs no foreground"));
    }

    [Fact]
    public void A_synthesised_act_is_attempted_once_because_a_repeat_is_a_second_keystroke()
    {
        // The same rule 'toggle' is not repeatable under: typing twice into a box is not typing once,
        // and a retried click is a second press the case never asked for.
        Assert.All(
            ActVerb.All.Where(one => one.Synthesises),
            one => Assert.False(one.Repeatable, $"'{one.Name}' would be sent twice by a retry"));
    }
}

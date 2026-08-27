using System.Runtime.InteropServices;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Scenarios;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW254. <see cref="Pick"/> has walked pickers since WW28 — both routes, the anchoring at the nearer
/// end, and the count that makes a claim about one switch checkable — and a case file could not say
/// it. So the one case in claude-tray that drives a picker had no first step to write, and the two
/// assertions it carries about the round trip were void before anybody wrote them.
/// <para>
/// <c>select</c> is the verb that looks closest and is not it. It asks a single item through
/// SelectionItemPattern, and a WPF ComboBox realises its items when its popup opens — so there is
/// nothing in the tree to select until something has walked the picker.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class PickVerbTests : IDisposable
{
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint WsTabStop = 0x00010000;
    private const uint CbsDropDownList = 0x0003;
    private const uint CbAddString = 0x0143;

    private static readonly string[] Values = ["Alpha", "Bravo", "Charlie", "Delta", "Echo"];

    private readonly PumpedDialog dialog;
    private readonly Restorable asFound;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SendMessageW(nint window, uint message, nint wParam, string lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint FindWindowExW(nint parent, nint after, string? className, string? title);

    public PickVerbTests()
    {
        dialog = PumpedDialog.Open(
            "winwright picker",
            new PumpedDialog.ChildWindow(
                "ComboBox", null, WsChild | WsVisible | WsTabStop | CbsDropDownList, 20, 20, 220, 240));

        var combo = FindWindowExW(dialog.Frame, 0, "ComboBox", null);
        Assert.NotEqual(0, combo);
        foreach (var value in Values)
            SendMessageW(combo, CbAddString, 0, value);

        asFound = Surface.AsFound(Combo);
    }

    public void Dispose()
    {
        // A dropped-down combo holds the desk in a way that outlives the window, and the next class
        // to send a key then finds the foreground somewhere it cannot name.
        asFound.PutBack();
        dialog.Dispose();
    }

    private Subject Combo => Subject.Unguarded(dialog.Root, Locator.Parse("ComboBox"), deadlineMs: 2000, pollMs: 20);

    [Fact]
    public void The_vocabulary_carries_it_and_says_what_it_needs_written_beside_it()
    {
        var verb = ActVerb.Named("pick");

        Assert.Equal("pick", verb.Name);
        Assert.Equal(Takes.Text, verb.Wants);
        Assert.False(verb.Reads);

        // Free text and not a closed list, unlike 'click' and 'press': what a picker holds is the
        // application's data, so an enumeration here would be a set typed into the engine.
        Assert.Empty(verb.Accepts);

        // The rule every synthesised act is under. A retry is a second walk, and a second walk is
        // more keystrokes and a different count under whatever claim the case made about the first.
        Assert.True(verb.Synthesises);
        Assert.False(verb.Repeatable);
    }

    [Fact]
    public void It_is_the_only_act_the_engine_can_see_the_landing_of()
    {
        // Data rather than a name compared in a refusal: every other flag on a verb is a field, and
        // a rule keyed on the string "pick" would be the vocabulary written twice.
        Assert.Equal(["pick", "pick at"], ActVerb.All.Where(one => one.Reaches).Select(one => one.Name));
    }

    [Fact]
    public void The_position_verb_takes_a_position_and_refuses_anything_else_where_it_was_written()
    {
        // WW267. Its own argument kind rather than 'a number': a range takes a fraction and a
        // negative quite happily, and a position that is either is a step nobody can perform.
        var verb = ActVerb.Named("pick at");

        Assert.Equal(Takes.Position, verb.Wants);
        Assert.True(verb.Reaches);
        Assert.True(verb.Synthesises);
        Assert.False(verb.Repeatable);

        Assert.Null(verb.Refuses("0"));
        Assert.Null(verb.Refuses("12"));
        Assert.Contains("is not a whole number from 0", verb.Refuses("1.5"), StringComparison.Ordinal);
        Assert.Contains("is not a whole number from 0", verb.Refuses("-1"), StringComparison.Ordinal);
        Assert.Contains("is not a whole number from 0", verb.Refuses("second"), StringComparison.Ordinal);
        Assert.Contains("acts on a position", verb.Refuses(null), StringComparison.Ordinal);
    }

    [Fact]
    public void A_case_naming_a_position_claims_the_reading_moved_rather_than_what_it_is()
    {
        // The shape the profiles case needs, and the reason both halves had to exist: a position to
        // go to, because the values are the machine's data, and a claim about the reading changing,
        // because the case cannot name what it changed to.
        var loaded = ScenarioFile.Read(
            "picker.cases.json",
            """
            {
              "cases": [
                {
                  "name": "the picker walks off the one it opened on",
                  "steps": [
                    {
                      "locator": "ComboBox#profiles",
                      "act": "pick at",
                      "with": "1",
                      "reads": "picked",
                      "moves": true
                    }
                  ]
                }
              ]
            }
            """);

        var step = Assert.Single(Assert.Single(loaded).Steps);

        Assert.Equal("pick at", step.Verb.Name);
        Assert.Equal("1", step.Argument);
        Assert.Equal("picked", step.Reads.Name);
        Assert.True(step.Moves);
    }

    [Fact]
    public void A_pick_with_nothing_to_reach_for_is_refused_where_it_was_written()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("ComboBox#profiles", "pick", expected: "Bravo", reads: "value"));

        Assert.Contains("acts on text", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_pick_that_claims_nothing_of_what_it_reached_is_refused()
    {
        // The difference from every other act, and the reason it is a rule. A click with no
        // expectation is a navigation the next step is the check for; a pick with none is every step
        // after it read against whichever value the walk happened to stop at.
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("ComboBox#profiles", "pick", argument: "Bravo"));

        Assert.Contains("claims nothing of what it reached", refusal.Because, StringComparison.Ordinal);
        Assert.Contains("'expect'", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_pick_naming_what_it_ended_on_is_a_step()
    {
        var step = StepDeclaration.Of(
            "ComboBox#profiles", "pick", argument: "Bravo", expected: "Bravo", reads: "value");

        Assert.True(step.Checkable);
        Assert.Equal("Bravo", step.Argument);
        Assert.Equal("Bravo", step.Expected);
    }

    [Fact]
    public void A_case_file_can_name_it()
    {
        var loaded = ScenarioFile.Read(
            "picker.cases.json",
            """
            {
              "cases": [
                {
                  "name": "the picker reaches the second profile",
                  "steps": [
                    {
                      "locator": "ComboBox#profiles",
                      "act": "pick",
                      "with": "Bravo",
                      "expect": "Bravo",
                      "reads": "value"
                    }
                  ]
                }
              ]
            }
            """);

        var step = Assert.Single(Assert.Single(loaded).Steps);

        Assert.Equal("pick", step.Verb.Name);
        Assert.Equal("Bravo", step.Argument);
    }

    [Fact]
    public void The_act_reports_the_route_and_the_count_in_the_field_a_pattern_act_reports_its_pattern()
    {
        var acted = Synthesised.Pick(Combo, "Charlie");

        if (BusyDesk.Excused(acted.Needed!))
            return;

        Assert.Equal("pick", acted.Verb);
        Assert.True(acted.Attempted);

        // The whole of what the hop count is for. A claim about one switch is void when the walk made
        // several, and the person that claim is for is the one reading this line.
        Assert.Equal("SelectionItem in 1 change", acted.Pattern);
        Assert.Contains("SelectionItem in 1 change", acted.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_walk_of_several_switches_says_how_many_in_the_same_place()
    {
        var walked = Pick.Value(Combo, "Charlie", byKeyboard: true);
        if (BusyDesk.Excused(walked.AsAssertion("the count travels with the route")))
            return;

        Assert.Equal("synthesised keyboard in 3 changes", walked.Door);
        Assert.Equal("SelectionItem in 1 change", Pick.Value(Combo, "Delta").Door);
    }

    [Fact]
    public void A_value_the_picker_does_not_hold_is_refused_by_the_verb_too()
    {
        // Through the adapter rather than through Pick, because what a case writes is the verb: the
        // refusal has to reach the author of the file and not stop at the layer under it.
        var refusal = Assert.Throws<NotActionableException>(() => Synthesised.Pick(Combo, "Foxtrot"));

        Assert.Contains("holds no \"Foxtrot\"", refusal.Because, StringComparison.Ordinal);
    }
}

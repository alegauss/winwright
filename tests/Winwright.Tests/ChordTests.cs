using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Winwright.Acting;
using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW317. A command that only a chord reaches.
/// <para>
/// Found adopting this in an application whose window is a title bar and a terminal on purpose. It
/// has no menu and no toolbar, so every command is on <c>Ctrl+Shift+something</c> — and
/// <c>press</c> spelled Tab, Shift+Tab and the arrows, which is the right vocabulary for moving
/// focus and the wrong one for invoking a command. There was no <c>with</c> that named a modifier
/// and a key, so those commands could not be driven by a case at all.
/// </para>
/// <para>
/// The pane it is driven against has no button for either command, which is the whole point: a
/// surface that also offered one would let this pass through the route it exists to prove
/// unnecessary.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ChordTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-chord-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void A_command_no_button_reaches_runs_when_its_chord_is_pressed()
    {
        var verdict = Run("Ctrl+Shift+I", "imported");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_different_modifier_set_reaches_the_other_command_and_not_the_first()
    {
        // Two commands and not one, so a run that pressed anything at all cannot pass: what is
        // asserted is which of them ran.
        var verdict = Run("Ctrl+Alt+F1", "wrote a bundle");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_chord_nothing_is_bound_to_leaves_the_read_out_saying_nothing_ran()
    {
        // The control. Without it, a read-out that said "imported" on every run would pass the two
        // above, and the surface would prove that a case can press keys rather than that a case can
        // reach a command.
        var verdict = Run("Ctrl+Shift+Q", ChordsPaneNothing);
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    /// <summary>
    /// What the pane says before anything has run. Spelled here as well as in the fixture, which is
    /// the duplication the suite is under everywhere: it references that project without its
    /// assembly, so a constant over there is one this file cannot name. The two disagreeing makes
    /// the control case fail, which is the claim under test.
    /// </summary>
    private const string ChordsPaneNothing = "no command has run";

    [Fact]
    public void A_chord_is_written_back_in_one_order_however_it_was_spelled()
    {
        Assert.True(Chord.TryParse("Shift+Ctrl+I", out var one, out _));
        Assert.True(Chord.TryParse("Ctrl+Shift+I", out var two, out _));

        // One keystroke has one spelling wherever it is reported, or a trace shows two acts where
        // there was one.
        //
        // WW377: read without a bang, which is the whole of what the annotation buys. These three
        // were the sites the entry counted, and each was a place a reader had to rebuild the
        // argument the signature could have made.
        Assert.Equal("Ctrl+Shift+I", one.Text);
        Assert.Equal(one.Text, two.Text);
    }

    [Theory]
    [InlineData("Ctrl+", "empty part")]
    [InlineData("Ctrl+Shift", "no key this can press")]
    [InlineData("Ctrl+Ctrl+I", "named twice")]
    [InlineData("Meta+I", "no modifier")]
    [InlineData("Ctrl+F25", "no key this can press")]
    public void A_chord_that_is_not_one_says_which_part_is_wrong(string written, string because)
    {
        Assert.False(Chord.TryParse(written, out _, out var wrong));
        Assert.Contains(because, wrong, StringComparison.Ordinal);
    }

    [Fact]
    public void The_signature_makes_the_promise_the_body_keeps()
    {
        // WW377, and it is WW364's check one verb over. Both outs above are read without a bang, and
        // that is what this pins: the attributes are what make an answered `false` narrow the reason
        // and an answered `true` narrow the chord, and nothing else notices them going.
        //
        // The engine's own callers hid the omission rather than paying for it — one interpolates the
        // reason, the other passes the chord on as nullable — so the argument for the annotation was
        // going to be a bang somebody had already written.
        var outs = typeof(Chord)
            .GetMethod(nameof(Chord.TryParse))!
            .GetParameters()
            .Where(one => one.IsOut)
            .ToList();

        Assert.Equal(["chord", "because"], outs.Select(one => one.Name));
        Assert.Equal(
            [true, false],
            outs.Select(one => one.GetCustomAttribute<NotNullWhenAttribute>()?.ReturnValue));
    }

    [Fact]
    public void A_press_that_is_neither_a_traversal_key_nor_a_chord_is_refused_where_it_is_written()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Pane", "press", argument: "Ctrl+Nonsense", named: "a chord nobody has"));

        // Both vocabularies in the sentence, because an author who wrote a chord and is shown only
        // the traversal names has been told their chord is not a traversal key, which they knew.
        Assert.Contains("Ctrl+Shift+I", refused.Because, StringComparison.Ordinal);
        Assert.Contains("Tab", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void The_traversal_names_still_load_beside_it()
    {
        var step = StepDeclaration.Of("Pane", "press", argument: "ShiftTab", named: "the traversal half");

        Assert.Equal("press", step.Verb.Name);
        Assert.Equal("ShiftTab", step.Argument);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>Press one chord and read what ran, or null where this desk cannot observe.</summary>
    private SuiteVerdict? Run(string chord, string expected)
    {
        if (!Desk.Read().CanObserve)
            return null;

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "launch": 30000, "resolve": 4000, "act": 4000, "poll": 25 }
            }
            """);

        var cases = $$"""
            {
              "fixtures": [
                { "name": "a window whose commands are only on chords", "arguments": ["--chords", "--show"] }
              ],
              "cases": [
                {
                  "name": "the command runs when its chord is pressed",
                  "catches": "an application with no menu and no toolbar, whose every command is on a chord and none of them reachable by a case",
                  "fixture": "a window whose commands are only on chords",
                  "steps": [
                    {
                      "locator": "Text#chordRan",
                      "act": "press",
                      "with": {{System.Text.Json.JsonSerializer.Serialize(chord)}},
                      "named": "the chord is pressed at the window"
                    },
                    {
                      "locator": "Text#chordRan",
                      "act": "read",
                      "reads": "name",
                      "expect": {{System.Text.Json.JsonSerializer.Serialize(expected)}},
                      "named": "the read-out says which command ran"
                    }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(
            ScenarioFile.Read("chord.cases.json", cases), Selection.All, register, project);

        register.StopAll();
        return verdict;
    }
}

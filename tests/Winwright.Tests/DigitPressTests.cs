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
/// WW605. A press of a bare digit is a chord, and reaches the window as that digit.
/// <para>
/// Found by wintimeline (WT81): an editor cuts a multicam clip to an angle on the number keys, and
/// <c>"press", "with": "2"</c> loaded and then broke, because the press read "2" through
/// <c>Enum.TryParse</c> as the third traversal key and then found no key by that name. Driven against
/// the fixture's drags pane, which says the last digit key its window took.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class DigitPressTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-digit-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("4")]
    [InlineData("5")]
    [InlineData("6")]
    [InlineData("7")]
    [InlineData("8")]
    [InlineData("9")]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void Every_digit_is_a_chord_and_never_a_traversal_key_by_number(string digit)
    {
        Assert.NotNull(Wrote.Step("Text#dragOrder", "press", ("with", digit)));
        Assert.NotNull(Chorded(digit));
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_traversal_key_by_name_is_still_no_chord()
    {
        // The other side of the same reading: the names it has always taken stay traversal keys.
        Assert.Null(Chorded("Tab"));
        Assert.Null(Chorded("down"));
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_number_past_the_digits_is_refused_where_it_is_written()
    {
        // A number inside the traversal keys' range read as one of them; one past it reads as nothing.
        // Both are refused at load now, rather than one loading and breaking.
        Assert.Throws<ScenarioRefusedException>(() => Wrote.Step("Text#dragOrder", "press", ("with", "40")));
        Assert.Throws<ScenarioRefusedException>(() => Wrote.Step("Text#dragOrder", "press", ("with", "12")));
    }

    [Fact]
    public void A_pressed_digit_reaches_the_window_as_that_digit()
    {
        if (!Desk.Read().CanObserve)
            return;

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "launch": 30000, "resolve": 4000, "act": 4000, "poll": 25 }
            }
            """);

        var cases = """
            {
              "fixtures": [ { "name": "a window that counts its digit keys", "arguments": ["--drags", "--show"] } ],
              "cases": [
                {
                  "name": "a number key is pressed",
                  "catches": "number keys that pick an angle, with a press of a bare digit that loaded and then broke",
                  "fixture": "a window that counts its digit keys",
                  "steps": [
                    { "locator": "Text#dragOrder", "act": "press", "with": "A", "named": "a letter is pressed" },
                    { "locator": "Text#digitSaid", "act": "read", "reads": "name", "expect": "no digit", "named": "a letter is no digit" },
                    { "locator": "Text#dragOrder", "act": "press", "with": "2", "named": "2 is pressed" },
                    { "locator": "Text#digitSaid", "act": "read", "reads": "name", "expect": "digit: 2", "named": "the window took 2" }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(ScenarioFile.Read("digit.cases.json", cases), Selection.All, register, project);
        register.StopAll();
        if (BusyDesk.Excused(verdict))
            return;

        Assert.True(
            verdict.Outcome == RunOutcome.Passed,
            string.Join(
                Environment.NewLine,
                verdict.Render()
                    .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
                    .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))));
    }

    /// <summary>
    /// What the press reads an argument as, asked of the private reading it acts on rather than of a
    /// copy of it: the chord, or null where it is a traversal key.
    /// </summary>
    private static Chord? Chorded(string argument) =>
        (Chord?)typeof(ActVerb)
            .GetMethod("Chorded", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [argument]);
}

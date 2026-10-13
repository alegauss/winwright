using Winwright.Locating;
using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW525. A reading the element it found has no pattern for, said at once.
/// <para>
/// Found in quickshell (QS280): a step read <c>value</c> off a WPF ComboBox that is not editable,
/// which offers no Value pattern, polled 27 times over 5121 ms and failed saying nothing answered.
/// The reading for a picker's selection is <c>picked</c>, and nothing said so.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class UnreadableTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-unreadable-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private static ElementFacts Facts(params string[] patterns) => new(
        "Sign in with", "SignIn", "ComboBox", "ComboBox", false, true, default, patterns.ToHashSet(StringComparer.Ordinal));

    [Fact]
    public void A_reading_the_element_has_no_pattern_for_says_so_and_names_what_fits()
    {
        var why = ReadBack.Named("value").Lacking(Facts("Selection", "ExpandCollapse"));

        Assert.NotNull(why);
        Assert.Contains("no Value pattern", why, StringComparison.Ordinal);
        Assert.Contains("'picked'", why, StringComparison.Ordinal);
        Assert.Contains("'expanded'", why, StringComparison.Ordinal);
        Assert.Contains("'name'", why, StringComparison.Ordinal);
    }

    [Fact]
    public void A_reading_the_element_can_give_or_one_every_element_has_is_never_refused()
    {
        Assert.Null(ReadBack.Named("picked").Lacking(Facts("Selection")));
        Assert.Null(ReadBack.Named("name").Lacking(Facts()));
        Assert.Null(ReadBack.Named("enabled").Lacking(Facts()));
        Assert.Null(ReadBack.Named("anything").Lacking(Facts()));
    }

    [Fact]
    public void Reading_value_off_a_picker_fails_at_once_and_names_picked()
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

        const string cases = """
            {
              "fixtures": [ { "name": "two pickers", "arguments": ["--pickers", "--show"] } ],
              "cases": [
                {
                  "name": "the picker's choice",
                  "catches": "a reading the element offers no pattern for, which polled a deadline out and named no cause",
                  "fixture": "two pickers",
                  "steps": [
                    { "locator": "ComboBox#shutPicker", "act": "read", "reads": "value", "expect": "Alpha", "named": "the choice, read the wrong way" }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(ScenarioFile.Read("unreadable.cases.json", cases), Selection.All, register, project);
        register.StopAll();

        var result = Assert.Single(Assert.Single(verdict.Ran).Verdict.Results);
        Assert.Equal(AssertionOutcome.Failed, result.Outcome);
        Assert.Contains("no Value pattern", result.Detail, StringComparison.Ordinal);
        Assert.Contains("'picked'", result.Detail, StringComparison.Ordinal);

        // The step itself never waited its deadline: what it says is about the element, not timing.
        Assert.DoesNotContain("polls over", result.Detail, StringComparison.Ordinal);
    }
}

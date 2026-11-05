using Winwright.Locating;
using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Tracing;
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
    public void A_case_that_stops_short_collapses_what_it_expanded_and_one_that_finishes_does_not()
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
        var project = ProjectDeclaration.Load(declaration);

        using var settling = Attachable.Settling();
        var launched = settling.Register.Launch(Fixture.Started("--pickers"));
        var window = System.Windows.Automation.AutomationElement.FromHandle(Fixture.Drew(launched.Pid).Handle);
        var picker = Subject.Unguarded(window, Locator.Parse("ComboBox#shutPicker"), 4000);
        string? State() => ReadBack.Named("expanded").Of(picker.Read());

        // Stopped short: 'toggle' on a picker that offers no Toggle pattern throws, so the case ends
        // with the drop-down it opened still open, which QS280 found degrading the next case.
        var stopping = CaseRun.Of(
            CaseDeclaration.Of(
                "the picker is opened and the case stops",
                Wrote.Step("ComboBox#shutPicker", "expand", ("named", "the picker is opened")),
                Wrote.Step("ComboBox#shutPicker", "read", ("reads", "expanded"), ("expect", "Expanded"), ("named", "it is open")),
                Wrote.Step("ComboBox#shutPicker", "toggle", ("named", "a step that cannot run"))),
            window,
            project);

        Assert.NotEmpty(stopping.Verdict.Broke);
        Assert.Contains(stopping.Trace, one => one.Verb == "collapse what the case expanded" && one.Verdict == StepVerdict.Ok);
        Assert.Equal("Collapsed", State());

        // The control: a case that runs to its end leaves the window the way its author wrote it to.
        var finishing = CaseRun.Of(
            CaseDeclaration.Of(
                "the picker is opened and the case ends",
                Wrote.Step("ComboBox#shutPicker", "expand", ("named", "the picker is opened")),
                Wrote.Step("ComboBox#shutPicker", "read", ("reads", "expanded"), ("expect", "Expanded"))),
            window,
            project);

        Assert.True(finishing.Finished);
        Assert.DoesNotContain(finishing.Trace, one => one.Verb == "collapse what the case expanded");
        Assert.Equal("Expanded", State());
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

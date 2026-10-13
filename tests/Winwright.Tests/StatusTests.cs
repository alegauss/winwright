using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW523. The state an element publishes in words, read by a case.
/// <para>
/// Found in quickshell (QS278): each tab says Connected, Reconnecting, Disconnected or Local in UIA
/// ItemStatus, and no reading reached it, so the tab's state was proven in process. Driven against
/// the fixture's <c>--status</c> pane, where a button changes the status after the window opened.
/// The step after the press is what proves the reading follows the live value, and the neighbour
/// publishing nothing is what proves an unset status is an answer rather than a hole.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class StatusTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-status-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void Status_is_a_reading_a_case_can_name()
    {
        var step = Wrote.Step("Text#sessionState", "read", ("reads", "status"), ("expect", "Connected"));

        Assert.Equal("status", step.Reads.Name);
        Assert.False(step.Reads.Always);
    }

    [Fact]
    public void A_status_is_read_live_after_the_application_changes_it()
    {
        var verdict = Run();
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
        Assert.Equal(5, verdict.Assertions);
    }

    [Fact]
    public void A_locator_selects_by_status_and_writes_it_back_the_way_it_read_it()
    {
        var parsed = Locator.Parse("TabItem[status=\"Connected\"][order=left]");

        Assert.Equal("Connected", parsed.Steps[0].Status);
        Assert.Equal(parsed.Steps, Locator.Parse(parsed.ToString()).Steps);
    }

    [Fact]
    public void An_empty_status_is_refused_because_every_element_without_a_state_has_one()
    {
        var refused = Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("TabItem[status=\"\"]"));

        Assert.Contains("'status' is empty", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void Reading_the_status_a_locator_selected_by_is_refused_as_already_fixed()
    {
        // WW238's unearned green: the locator chose the element because it says Connected, so a
        // claim that it says Connected could never be false.
        Assert.Throws<ScenarioRefusedException>(
            () => Wrote.Step("Text[status=\"Connected\"]", "read", ("reads", "status"), ("expect", "Connected")));
    }

    [Fact]
    public void An_element_publishing_no_status_reads_as_empty_rather_than_as_nothing_observed()
    {
        // Read directly and not through a case: the format has no claim that a reading is empty —
        // a pattern matching the empty string holds for every answer and is refused — so what is
        // checked here is the reading itself, beside one that does publish a status.
        if (!Desk.Read().CanObserve)
            return;

        using var settling = Attachable.Settling();
        var launched = settling.Register.Launch(Fixture.Started("--status"));
        var window = AutomationElement.FromHandle(Fixture.Drew(launched.Pid).Handle);
        var status = ReadBack.Named("status");

        Assert.Equal("", status.Of(Subject.Unguarded(window, Locator.Parse("Text#plainState"), 4000).Read()));
        Assert.Equal("Disconnected", status.Of(Subject.Unguarded(window, Locator.Parse("Text#sessionState"), 4000).Read()));

        // And null where nothing resolved, like every reading: an element that is not there publishes
        // no status and does not publish an empty one either.
        Assert.Null(status.Of(Subject.Unguarded(window, Locator.Parse("Text#noSuchState"), 200).Read()));
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    private SuiteVerdict? Run()
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

        const string cases = """
            {
              "fixtures": [ { "name": "a session that publishes its state", "arguments": ["--status", "--show"] } ],
              "cases": [
                {
                  "name": "the session's status follows the connection",
                  "catches": "a state an application says in words for screen readers, which no reading reached",
                  "fixture": "a session that publishes its state",
                  "steps": [
                    { "locator": "Text#sessionState", "act": "read", "reads": "status", "expect": "Disconnected", "named": "it starts disconnected" },
                    { "locator": "Text[status=\"Connected\"]", "act": "read", "absent": true, "named": "no element is connected yet" },
                    { "locator": "Button#connectButton", "act": "invoke", "named": "Connect is pressed" },
                    { "locator": "Text#sessionState", "act": "read", "reads": "status", "expect": "Connected", "named": "it says connected once it is" },
                    { "locator": "Text[status=\"Connected\"]", "act": "read", "reads": "name", "expect": "prod-db", "named": "the connected one is found by its state, not its title" },
                    { "locator": "Text#sessionState", "act": "read", "reads": "status", "matches": "^Connected$", "named": "and says nothing else" }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(ScenarioFile.Read("status.cases.json", cases), Selection.All, register, project);

        register.StopAll();
        return verdict;
    }
}

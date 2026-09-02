using System.Runtime.InteropServices;
using System.Windows.Automation;

using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW59's running half. A case runs by name in seconds, and the run says what it did not run —
/// because a filtered run reporting success without qualification is the same silent pass the third
/// verdict exists to prevent, one level up.
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class SuiteRunTests : IDisposable
{
    private const uint WsPopup = 0x80000000;
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint BsAutoCheckBox = 0x0003;

    private readonly List<nint> created = [];
    private readonly string root = Directory.CreateTempSubdirectory("winwright-suite-").FullName;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    public void Dispose()
    {
        for (var index = created.Count - 1; index >= 0; index--)
            DestroyWindow(created[index]);

        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void One_case_runs_by_name_and_the_others_are_not_run_rather_than_passed()
    {
        var frame = Dialog();
        var verdict = Suite.Run(Three(), Selection.Case("the field takes a name"), Root(frame), Declared());

        Assert.Equal(RunOutcome.Passed, verdict.Outcome);
        Assert.Equal(["the field takes a name"], verdict.Ran.Select(one => one.Declared.Name));
        Assert.Equal(["the box ticks", "the field reads gamma"], verdict.Skipped.Select(one => one.Case));
        Assert.All(verdict.Skipped, one => Assert.Contains("not selected by", one.Because));
    }

    [Fact]
    public void A_filtered_run_qualifies_its_pass_before_it_states_it()
    {
        // The sentence is the whole task. A pass over one of three cases is a different claim from a
        // pass, and putting the qualification second is how it gets skimmed.
        var frame = Dialog();
        var verdict = Suite.Run(Three(), Selection.Case("the field takes a name"), Root(frame), Declared());

        Assert.Equal(
            "Passed: 1 of 3 cases, 2 not run, 1 assertion over case 'the field takes a name'.",
            verdict.Sentence());
    }

    [Fact]
    public void An_unfiltered_run_says_it_ran_all_of_them_rather_than_saying_nothing()
    {
        var frame = Dialog();
        var verdict = Suite.Run(Three(), Selection.All, Root(frame), Declared());

        Assert.Empty(verdict.Skipped);
        Assert.Equal(RunOutcome.Failed, verdict.Outcome);
        Assert.Contains("all 3 cases", verdict.Sentence());
        Assert.Contains("over everything", verdict.Sentence());
    }

    [Fact]
    public void The_whole_reading_names_every_case_that_ran_and_every_case_that_did_not()
    {
        var frame = Dialog();
        var verdict = Suite.Run(Three(), Selection.Tag("smoke"), Root(frame), Declared());

        var rendered = verdict.Render();

        Assert.Equal(4, rendered.Count);
        Assert.Contains("2 of 3 cases, 1 not run", rendered[0]);
        Assert.Contains("the field takes a name: Passed", rendered[1]);
        Assert.Contains("the box ticks: Passed", rendered[2]);
        Assert.Contains("the field reads gamma — not selected by tag smoke", rendered[3]);
    }

    [Fact]
    public void The_worst_reading_any_case_earned_is_the_runs()
    {
        var frame = Dialog();
        var verdict = Suite.Run(Three(), Selection.All, Root(frame), Declared());

        Assert.Equal(RunOutcome.Failed, verdict.Outcome);
        Assert.Equal(1, verdict.ExitCode);
        Assert.Single(verdict.Unhappy);
        Assert.Equal("the field reads gamma", verdict.Unhappy[0].Declared.Name);
    }

    [Fact]
    public void A_case_left_out_by_the_filter_never_degrades_the_run()
    {
        // A case nobody asked for is not a hole. Collapsing the two would make every filtered run
        // look damaged, and the reader would stop reading the word.
        var frame = Dialog();
        var verdict = Suite.Run(Three(), Selection.Case("the field takes a name"), Root(frame), Declared());

        Assert.Equal(RunOutcome.Passed, verdict.Outcome);
        Assert.Equal(0, verdict.ExitCode);
        Assert.NotEmpty(verdict.Skipped);
    }

    [Fact]
    public void A_name_that_selects_no_case_stops_the_run_before_the_first_case()
    {
        var frame = Dialog();

        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => Suite.Run(Three(), Selection.Case("the feild takes a name"), Root(frame), Declared()));

        Assert.Contains("no case is called that", refusal.Because);
    }

    [Fact]
    public void A_run_over_no_cases_at_all_is_refused_rather_than_reported_as_a_pass()
    {
        var frame = Dialog();

        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => Suite.Run([], Selection.All, Root(frame), Declared()));

        Assert.Contains("not the same as a run that passed", refusal.Because);
    }

    [Fact]
    public void A_file_of_cases_runs_as_the_file_declares_it()
    {
        // The other half of "run takes a file, a case or a tag": the file is the input, and the
        // cases in it need no second spelling in C#.
        var frame = Dialog();
        var path = Path.Combine(root, $"profiles{ScenarioFile.Extension}");
        File.WriteAllText(path, """
            {
              "cases": [
                {
                  "name": "the field takes a name",
                  "tags": ["smoke"],
                  "steps": [
                    { "locator": "Edit", "act": "set value", "with": "beta", "expect": "beta", "reads": "value" }
                  ]
                }
              ]
            }
            """);

        var verdict = Suite.Run(ScenarioFile.Load(path).Cases, Selection.Tag("smoke"), Root(frame), Declared());

        Assert.Equal(RunOutcome.Passed, verdict.Outcome);
        Assert.Empty(verdict.Skipped);
    }

    [Fact]
    public void A_case_name_declared_in_two_files_is_refused_because_a_name_selects_one()
    {
        File.WriteAllText(Path.Combine(root, $"one{ScenarioFile.Extension}"), Holding("the field takes a name"));
        File.WriteAllText(Path.Combine(root, $"two{ScenarioFile.Extension}"), Holding("The Field Takes A Name"));

        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.LoadAll(root));

        Assert.Contains("so its name selects two cases", refusal.Because);
    }

    [Fact]
    public void Every_case_under_a_directory_is_read_in_path_order()
    {
        File.WriteAllText(Path.Combine(root, $"beta{ScenarioFile.Extension}"), Holding("b"));
        File.WriteAllText(Path.Combine(root, $"alpha{ScenarioFile.Extension}"), Holding("a"));

        var files = ScenarioFile.LoadAll(root);

        Assert.Equal(2, files.Count);
        Assert.Equal(["a", "b"], ScenarioFile.Across(files).Select(one => one.Name));
    }

    [Fact]
    public void A_case_that_ran_without_saying_what_it_catches_is_counted_in_the_reading()
    {
        // WW63. A check nobody can justify is a check nobody dares delete and nobody dares change,
        // so the count is on the reading rather than left to whoever last looked.
        var frame = Dialog();
        var unjustified = CaseDeclaration.Of(
            "something", StepDeclaration.Of("Edit", "set value", "beta", expected: "beta", reads: "value"));

        var verdict = Suite.Run([.. Three(), unjustified], Selection.All, Root(frame), Declared());

        Assert.Equal(RunOutcome.Failed, verdict.Outcome);
        Assert.Equal(["something"], verdict.Unjustified.Select(one => one.Name));

        var rendered = verdict.Render();
        Assert.Contains(rendered, line => line.Contains("1 case that ran say", StringComparison.Ordinal));
        Assert.Contains(rendered, line => line.Contains("what deleting them would cost", StringComparison.Ordinal));
    }

    [Fact]
    public void A_case_whose_precondition_this_machine_has_not_got_is_a_hole_and_never_a_red()
    {
        // WW61. The absence is named, and the case never acts — so nothing goes red about a window
        // nobody put into the state the steps describe.
        var frame = Dialog();
        var needing = CaseDeclaration.Declared(
            "the second profile renames",
            [StepDeclaration.Of("Edit#profileTwo", "set value", "beta", expected: "beta", reads: "value")],
            needs: ["a second profile"],
            catches: "a rename that writes the first profile when the second is selected");

        var verdict = Suite.Run(
            [needing],
            Selection.All,
            Root(frame),
            Declared(),
            measured: PreconditionSet.Of(Precondition.Absent("a second profile", "this checkout registers one")));

        Assert.Equal(RunOutcome.Degraded, verdict.Outcome);
        Assert.Equal(2, verdict.ExitCode);

        var only = Assert.Single(verdict.Ran);
        var hole = Assert.Single(only.Verdict.Unchecked);
        Assert.Equal("a second profile", hole.Missing!.Name);
        Assert.Contains("this checkout registers one", hole.Detail);
        Assert.Empty(only.Verdict.Failures);
    }

    [Fact]
    public void A_case_whose_precondition_is_met_runs_exactly_as_if_it_declared_none()
    {
        var frame = Dialog();
        var needing = CaseDeclaration.Declared(
            "the field takes a name",
            [StepDeclaration.Of("Edit", "set value", "beta", expected: "beta", reads: "value")],
            needs: ["an editable field"],
            catches: "a value the control never reads back");

        var verdict = Suite.Run(
            [needing], Selection.All, Root(frame), Declared(),
            measured: PreconditionSet.Of(Precondition.Met("an editable field")));

        Assert.Equal(RunOutcome.Passed, verdict.Outcome);
        Assert.Empty(Assert.Single(verdict.Ran).Verdict.Unchecked);
    }

    [Fact]
    public void A_case_that_needs_something_nobody_measured_is_refused_rather_than_guessed_at()
    {
        var frame = Dialog();
        var needing = CaseDeclaration.Declared(
            "the second profile renames",
            [StepDeclaration.Of("Edit", "set value", "beta", expected: "beta", reads: "value")],
            needs: ["a second profile"]);

        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => Suite.Run([needing], Selection.All, Root(frame), Declared()));

        Assert.Contains("nothing measured whether this machine has any of it", refusal.Because);
    }

    [Fact]
    public void A_directory_that_is_not_there_is_refused_and_not_read_as_an_empty_suite()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => ScenarioFile.LoadAll(Path.Combine(root, "nothing-here")));

        Assert.Contains("no such directory", refusal.Because);
    }

    private static string Holding(string name) => $$"""
        {
          "cases": [
            {
              "name": {{System.Text.Json.JsonSerializer.Serialize(name)}},
              "steps": [ { "locator": "Edit", "act": "set value", "with": "b", "expect": "b" } ]
            }
          ]
        }
        """;

    /// <summary>
    /// Three cases against one window: two that pass, one that does not. All three say what they
    /// catch, so the readings here are about the filter rather than about WW63's own count.
    /// </summary>
    private static IReadOnlyList<CaseDeclaration> Three() =>
    [
        CaseDeclaration.Declared(
            "the field takes a name",
            [StepDeclaration.Of("Edit", "set value", "beta", expected: "beta", reads: "value")],
            tags: ["smoke"],
            catches: "a value written through the pattern that the control never reads back"),
        CaseDeclaration.Declared(
            "the box ticks",
            [StepDeclaration.Of("""CheckBox[name="Wrap lines"]""", "toggle", expected: "On", reads: "toggle")],
            tags: ["smoke", "tray"],
            catches: "a toggle whose peer reports the state it had before the act"),
        CaseDeclaration.Declared(
            "the field reads gamma",
            [StepDeclaration.Of("Edit", "set value", "beta", expected: "gamma", reads: "value")],
            catches: "a read-back that never arrives, which is what this one is here to be"),
    ];

    [Fact]
    public void A_capture_with_no_captures_declared_is_refused_at_the_door_rather_than_run_into()
    {
        // WW348, on the other of the two doors. This one is handed a window a caller already has, so
        // there is no launch to save — what it saves is driving the application to the step to say
        // what the file and the declaration beside it have said all along.
        var frame = Dialog();
        var capturing = CaseDeclaration.Of(
            "the dialog is photographed",
            StepDeclaration.Of("Edit", "capture", "the field"));

        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => Suite.Run([capturing], Selection.All, Root(frame), Declared()));

        Assert.Contains("the dialog is photographed", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("'capture'", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("'captures'", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_project_that_declares_captures_runs_the_step_rather_than_refusing_it()
    {
        // The half that makes the refusal a reading rather than a ban, and the one that would catch
        // a check written the wrong way round. The declaration is there, so the door lets the case
        // through and whatever the step then answers is the run's business.
        var frame = Dialog();
        var capturing = CaseDeclaration.Of(
            "the dialog is photographed",
            StepDeclaration.Of("Edit", "capture", "the field"));

        var verdict = Suite.Run([capturing], Selection.All, Root(frame), Declaring(Path.Combine(root, "pictures")));

        Assert.Single(verdict.Ran);
        Assert.Empty(verdict.Skipped);
    }

    private static AutomationElement Root(nint frame) => AutomationElement.FromHandle(frame);

    /// <summary>The same project, carrying somewhere to put pictures. WW348.</summary>
    private ProjectDeclaration Declaring(string pictures)
    {
        var path = Path.Combine(root, "declaring", ProjectDeclaration.FileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Environment.ProcessPath)}},
              "captures": {{System.Text.Json.JsonSerializer.Serialize(pictures)}},
              "timeouts": { "resolve": 600, "act": 600, "poll": 20 }
            }
            """);

        return ProjectDeclaration.Load(path);
    }

    /// <summary>A project whose waits are short, because one of these is waiting for a red.</summary>
    private ProjectDeclaration Declared()
    {
        var path = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Environment.ProcessPath)}},
              "timeouts": { "resolve": 600, "act": 600, "poll": 20 }
            }
            """);

        return ProjectDeclaration.Load(path);
    }

    private nint Create(string className, string? title, uint style, int w, int h, nint parent = 0)
    {
        var window = CreateWindowExW(0, className, title, style, 20, 20, w, h, parent, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
        return window;
    }

    private nint Dialog()
    {
        var frame = Create("Static", "winwright statistics", WsPopup | WsVisible, 480, 320);
        Create("Button", "Wrap lines", WsChild | WsVisible | BsAutoCheckBox, 140, 24, frame);
        Create("Edit", "alpha", WsChild | WsVisible, 200, 24, frame);
        return frame;
    }
}

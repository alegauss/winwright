using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW551. A file chosen through the system's common Open and Save dialogs.
/// <para>
/// Found by PhotoBoost (PB23 and PB25): opening an image and exporting it go through the common
/// dialog, a case could not reach it, and the application grew flags only its tests used. Driven
/// against the fixture's <c>--dialogs</c> pane, whose buttons open the shell's own dialogs owned by
/// the window, so the tree is the one an adopter's application shows — in whatever language the
/// guest's shell speaks, which is why every locator here is an id and never a name.
/// </para>
/// <para>
/// The control is a file that is not there: the Open dialog refuses it and stays open, so a step
/// claiming the dialog closed is red, and a run that read a chosen file whatever happened cannot pass.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class DialogTests : IDisposable
{
    /// <summary>The common dialog, from the window that owns it: a child of it in the tree.</summary>
    private const string Dialog = "Window[class=\"#32770\"]";

    private readonly string root = Directory.CreateTempSubdirectory("winwright-dialog-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void A_file_chosen_in_the_open_dialog_is_the_one_the_application_opens()
    {
        var verdict = Run(
            """
            { "locator": "Button#openFile", "act": "invoke", "named": "the Open dialog is asked for" },
            { "locator": "Window[class=\"#32770\"]", "act": "choose file", "with": "{files}/photo.png", "named": "the photo is chosen" },
            { "locator": "Window[class=\"#32770\"]", "act": "read", "absent": true, "named": "the dialog closed" },
            { "locator": "Text#dialogSaid", "act": "read", "reads": "name", "matches": "^opened: .*\\\\photo\\.png$", "named": "the application opened the photo" }
            """);
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_type_picked_in_the_save_dialog_is_the_one_the_file_is_saved_as()
    {
        var verdict = Run(
            """
            { "locator": "Button#saveFile", "act": "invoke", "named": "the Save dialog is asked for" },
            { "locator": "Window[class=\"#32770\"] > ComboBox#FileTypeControlHost", "act": "pick", "with": "WebP image", "reads": "picked", "expect": "WebP image", "named": "WebP is the type" },
            { "locator": "Window[class=\"#32770\"]", "act": "choose file", "with": "{files}/out", "named": "the name is given" },
            { "locator": "Window[class=\"#32770\"]", "act": "read", "absent": true, "named": "the dialog closed" },
            { "locator": "Text#dialogSaid", "act": "read", "reads": "name", "expect": "saved: out.webp as WebP image", "named": "the application saved it as WebP" }
            """);
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    /// <summary>
    /// WW561. The save dialog offers a default name, and on the guest it put that name back over the
    /// path the act wrote, so the application saved the default while every step was green.
    /// </summary>
    [Fact]
    public void A_name_given_to_the_save_dialog_is_saved_instead_of_its_default_name()
    {
        var verdict = Run(
            """
            { "locator": "Button#saveFile", "act": "invoke", "named": "the Save dialog is asked for" },
            { "locator": "Window[class=\"#32770\"]", "act": "choose file", "with": "{files}/given.png", "named": "a name is given" },
            { "locator": "Window[class=\"#32770\"]", "act": "read", "absent": true, "named": "the dialog closed" },
            { "locator": "Text#dialogSaid", "act": "read", "reads": "name", "expect": "saved: given.png as PNG image", "named": "the application saved the given name" }
            """);
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_file_that_is_not_there_leaves_the_open_dialog_standing_and_the_step_red()
    {
        var verdict = Run(
            """
            { "locator": "Button#openFile", "act": "invoke", "named": "the Open dialog is asked for" },
            { "locator": "Window[class=\"#32770\"]", "act": "choose file", "with": "{files}/missing.png", "named": "a missing file is chosen" },
            { "locator": "Window[class=\"#32770\"]", "act": "read", "absent": true, "named": "the dialog closed" },
            { "locator": "Text#dialogSaid", "act": "read", "reads": "name", "expect": "nothing chosen", "named": "nothing was opened" }
            """);
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Failed, Said(verdict));
    }

    [Fact]
    public void A_choice_names_its_file_and_is_found_the_way_a_file_step_finds_one()
    {
        var bare = Assert.Throws<ScenarioRefusedException>(() => Wrote.Step(Dialog, "choose file"));
        Assert.Contains("'choose file'", bare.Because, StringComparison.Ordinal);

        var step = Wrote.Step(Dialog, "choose file", ("with", "{files}/photo.png"));
        Assert.Equal(Path.Combine(root, "staged", "photo.png"), step.Rooted(root, Path.Combine(root, "staged")).Argument);
        Assert.False(step.Verb.Synthesises);
        Assert.False(step.Verb.Repeatable);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>
    /// Run <paramref name="steps"/> against the dialogs pane, with a photo staged, or null where this
    /// desk cannot observe.
    /// </summary>
    private SuiteVerdict? Run(string steps)
    {
        if (!Desk.Read().CanObserve)
            return null;

        Directory.CreateDirectory(Path.Combine(root, "data"));
        File.WriteAllBytes(Path.Combine(root, "data", "photo.png"), new byte[512]);

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "launch": 30000, "resolve": 10000, "act": 6000, "poll": 50 }
            }
            """);

        var cases = $$"""
            {
              "fixtures": [
                { "name": "the dialogs pane", "arguments": ["--dialogs", "--show"], "files": ["data/photo.png"] }
              ],
              "cases": [
                {
                  "name": "a file is chosen through the common dialog",
                  "catches": "opening and exporting through the common dialog, with no step that could reach it",
                  "fixture": "the dialogs pane",
                  "steps": [
                    {{steps}}
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(ScenarioFile.Read("dialog.cases.json", cases), Selection.All, register, project);

        register.StopAll();
        return verdict;
    }
}

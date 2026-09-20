using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW475. A label carrying a placeholder was refused to every claim, and the refusal argues about
/// equality: a tree holding `Profile: alpha` can never equal `Profile: {name}`, so asking for that
/// exactly is asking for something that cannot pass.
/// <para>
/// Two claims are not equality. The fixed part on the far side of the placeholder is the whole of
/// what a begins-with or an ends-with compares, and the refusal fired first and took them with it —
/// so the one form that could have matched was the one nobody could write. Measured migrating WW85:
/// claude-tray's submenu line for a variable naming no profile is a format string, and the case for
/// it could not be written at all.
/// </para>
/// <para>
/// Driven end to end rather than asserted on the resolver, because the resolver answering is half of
/// it: what this is about is a step in a case reading a window that draws the label filled in, which
/// is the shape every real application has and the one the fixture deliberately did not have.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class PlaceholderClaimTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-placeholder-claim-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void The_front_of_a_label_whose_placeholder_ends_it_is_read_by_a_begins_with()
    {
        var verdict = Run(
            """
            {
              "locator": "Text#filledFrontLabel",
              "act": "read",
              "reads": "name",
              "beginsWithLabel": "labels.profileName",
              "named": "the filled label begins with the fixed part of the one it was drawn from"
            }
            """);

        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void The_end_of_a_label_whose_placeholder_starts_it_is_read_by_an_ends_with()
    {
        // The mirror, and it is here so the rule is proved in both directions rather than in one and
        // assumed in the other. The fixture ships a label for each, which is the whole reason the
        // second one was added.
        var verdict = Run(
            """
            {
              "locator": "Text#filledBackLabel",
              "act": "read",
              "reads": "name",
              "endsWithLabel": "labels.inUseBy",
              "named": "the filled label ends with the fixed part of the one it was drawn from"
            }
            """);

        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void The_claim_that_reads_the_end_the_placeholder_took_is_refused_rather_than_run()
    {
        // The refusal that keeps this narrow, driven where an author would meet it. `labels.profileName`
        // ends in its placeholder, so an ends-with has nothing fixed to read — and a run that answered
        // anyway would be comparing against whatever the author happened to leave.
        //
        // Refused before the window, which is the other half: a scenario that is wrong is wrong on
        // every machine, and finding out after a launch would read as an application that did not draw
        // what was expected.
        var verdict = Run(
            """
            {
              "locator": "Text#filledFrontLabel",
              "act": "read",
              "reads": "name",
              "endsWithLabel": "labels.profileName",
              "named": "the end of a label that ends in its placeholder"
            }
            """);

        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));

        var said = Said(verdict);
        Assert.Contains("labels.profileName", said, StringComparison.Ordinal);
        Assert.Contains("reads the text after it, and there is none", said, StringComparison.Ordinal);
    }

    [Fact]
    public void Equality_against_the_same_label_is_refused_as_it_always_was()
    {
        // The arm WW475 does not touch, driven beside the ones it does. `label` is equality, the
        // window draws the placeholder filled in, and no reading of it could ever match the format
        // string — which is the sentence the original refusal makes and still the right one.
        var verdict = Run(
            """
            {
              "locator": "Text#filledFrontLabel",
              "act": "read",
              "reads": "name",
              "label": "labels.profileName",
              "named": "the filled label equals the one it was drawn from"
            }
            """);

        if (verdict is null)
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));
        Assert.Contains("could ever pass", Said(verdict), StringComparison.Ordinal);
    }

    /// <summary>
    /// The whole reading, which is what a red here is about — and what a refusal is in: `Render()`
    /// alone answers `Broken: all 1 case, 0 assertions` and names nothing, so a case asserting on
    /// the refusal's own words against it would be asserting against a sentence that never carries
    /// them.
    /// </summary>
    /// <param name="verdict">What the run concluded.</param>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>
    /// Run one step against the fixture's main window, with the fixture's own strings as the
    /// project's. Null where this desk cannot observe, which the caller returns on.
    /// </summary>
    /// <param name="step">The step, as the file spells it.</param>
    private SuiteVerdict? Run(string step)
    {
        if (!Desk.Read().CanObserve)
            return null;

        // The fixture's own files and not a copy written here: the labels this claim is about are the
        // ones it draws, and a second transcription of them would be this case agreeing with itself.
        var into = Directory.CreateDirectory(Path.Combine(root, "project")).FullName;
        var names = new List<string>();
        foreach (var file in Directory.EnumerateFiles(Fixture.StringsDirectory(), "strings.*.json"))
        {
            File.Copy(file, Path.Combine(into, Path.GetFileName(file)), overwrite: true);
            names.Add(Path.GetFileName(file));
        }

        Assert.NotEmpty(names);

        var declaration = Path.Combine(into, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "languageFiles": [{{string.Join(", ", names.Select(one => $"\"{one}\""))}}],
              "language": { "fallback": "en" },
              "timeouts": { "launch": 30000, "resolve": 4000, "act": 4000, "poll": 25 }
            }
            """);

        var cases = $$"""
            {
              "fixtures": [
                {
                  "name": "the fixture's main window, in English",
                  "arguments": ["--language=en"],
                  "language": "en"
                }
              ],
              "cases": [
                {
                  "name": "a label drawn with its placeholder filled in",
                  "catches": "a claim about the fixed part of a format string being refused before it runs, which leaves the one form that could match unwritable",
                  "fixture": "the fixture's main window, in English",
                  "steps": [
                    {{step}}
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = Winwright.Processes.ProcessRegister.For(project);

        var verdict = Suite.Launch(
            ScenarioFile.Read("placeholder.cases.json", cases),
            Selection.All,
            register,
            project);

        // Nothing left on the desk, whichever way the case went: this launches a real window per
        // fact, and a fixture left running is the next fact reading somebody else's window.
        register.StopAll();

        return verdict;
    }
}

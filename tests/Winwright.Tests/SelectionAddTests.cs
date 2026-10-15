using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW540. Rows added to and taken out of a list that holds several at once.
/// <para>
/// Found by quickshell (QS314), whose sidebar takes several rows: <c>select</c> replaces the
/// selection, so a case could hold one row and never two. Driven against the fixture's
/// <c>--pickers</c> pane, which carries a list taking several rows and one taking one — the control,
/// where an add would be a replace and is refused naming the list.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class SelectionAddTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-selection-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void Three_rows_added_are_each_selected_and_one_taken_out_is_not()
    {
        var verdict = Run("""
            { "locator": "List#severalList > ListItem[name=\"Alpha\"]", "act": "add to selection", "reads": "selected", "expect": "selected" },
            { "locator": "List#severalList > ListItem[name=\"Charlie\"]", "act": "add to selection", "reads": "selected", "expect": "selected" },
            { "locator": "List#severalList > ListItem[name=\"Echo\"]", "act": "add to selection", "reads": "selected", "expect": "selected" },
            { "locator": "List#severalList > ListItem[name=\"Alpha\"]", "act": "read", "reads": "selected", "expect": "selected", "named": "the first is still selected" },
            { "locator": "List#severalList > ListItem[name=\"Charlie\"]", "act": "remove from selection", "reads": "selected", "expect": "not selected" },
            { "locator": "List#severalList > ListItem[name=\"Echo\"]", "act": "read", "reads": "selected", "expect": "selected", "named": "the last is still selected" }
            """);
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void Adding_to_a_list_that_takes_one_row_is_refused_naming_the_list()
    {
        var verdict = Run("""
            { "locator": "List#oneList > ListItem[name=\"Bravo\"]", "act": "add to selection", "reads": "selected", "expect": "selected" }
            """);
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome != RunOutcome.Passed, Said(verdict));
        Assert.Contains("#oneList takes one selection at a time", Said(verdict), StringComparison.Ordinal);
    }

    /// <summary>One case holding <paramref name="steps"/> against the pickers pane, or null where this desk cannot observe.</summary>
    private SuiteVerdict? Run(string steps)
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
              "fixtures": [ { "name": "a list taking several rows", "arguments": ["--pickers", "--show"] } ],
              "cases": [
                {
                  "name": "rows are added to a selection",
                  "catches": "a list taking several rows that a case could only ever hold one row of",
                  "fixture": "a list taking several rows",
                  "steps": [ {{steps}} ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        var verdict = Suite.Launch(ScenarioFile.Read("selection.cases.json", cases), Selection.All, register, project);

        register.StopAll();
        return verdict;
    }

    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));
}

using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW273. Every expectation in this engine is derived — `covers`, `label`, `notLabel`, `never` and
/// `forEach` all name a key and read the string out of the language the fixture says its window is in.
/// A locator could not. It carried the words, and the words are the one thing that goes stale the day
/// somebody edits the strings file and are wrong in every other language the application ships from
/// the moment they are written.
/// <para>
/// Measured migrating `WW84`. claude-tray's settings sidebar is six bare Borders with no automation
/// peer, so the TextBlock inside is what reaches the tree and its words are the only thing that
/// addresses one. The script wrote `Nav-Settings $win (Label 'settings.nav.claudeCode')`; a case could
/// not, and the migrated keyboard case had to say in a comment that the label it names happens to be
/// the same in all four languages this application ships.
/// </para>
/// <para>
/// One shape with `WW263`'s member on purpose: `{}` is the member of the set a case repeats over and
/// `{a.key}` is a string the project declares. A reader who has seen either knows a brace is a hole
/// the run fills, and both are refused at declaration where they cannot parse with something in them.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class DeclaredLocatorTests : IDisposable
{
    private readonly Settling settling = Attachable.Settling();
    private readonly AutomationElement fixtureRoot;
    private readonly string root = Directory.CreateTempSubdirectory("winwright-declared-").FullName;

    public DeclaredLocatorTests()
    {
        var launched = settling.Register.Launch(Fixture.Started("--rows=paired"));
        var drawn = Attempt.UntilTrue(() => TopLevelWindows.Largest(launched.Pid) is not null, 20000, 25);

        Assert.True(drawn.Happened, $"the fixture drew no window in {drawn.WaitedMs}ms");
        fixtureRoot = AutomationElement.FromHandle(TopLevelWindows.Largest(launched.Pid)!.Handle);
    }

    public void Dispose()
    {
        settling.Dispose();
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void A_locator_built_out_of_a_key_finds_the_element_the_strings_name()
    {
        // The row is on the page and the case never says what it is called. Rename it in the strings
        // file and in the window and this still addresses it, which is the whole of the point.
        var verdict = Run("Group[name=\"{rows.headers.startup}\"]");
        if (verdict is null)
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));

        // The words the run actually looked for reach the trace. The key is one line away in the case
        // file, and what a red is about is what was on the desk.
        Assert.Contains("Startup", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_key_the_strings_do_not_declare_is_refused_naming_the_key_and_the_file()
    {
        // A scenario that is wrong rather than an application that is, so it refuses before anything is
        // driven — the same arm `label` goes down, for the same reason.
        if (!Desk.Read().CanObserve)
            return;

        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => Run("Group[name=\"{rows.headers.nobodyDeclaredThis}\"]"));

        Assert.Contains("rows.headers.nobodyDeclaredThis", refusal.Because, StringComparison.Ordinal);
        Assert.Contains("strings.en.json", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_locator_that_cannot_parse_with_a_string_in_it_is_refused_where_it_was_written()
    {
        // A fact about the file, judged where the locator was written and not on the run that happened
        // to resolve it — which is where `WW263` already put the same judgement for the member.
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Group[name=\"{rows.headers.startup}", "read", eachSpoken: true));

        Assert.Contains("does not parse", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_step_says_which_keys_its_locator_is_built_out_of_and_the_member_is_not_one()
    {
        var both = StepDeclaration.Of("Group[name=\"{}\"] > Button[name=\"{a.key}\"]", "read", eachSpoken: true);

        // The member is the run's own hole and not a key, so a case repeating over a set does not go
        // looking in the strings file for a string called nothing.
        Assert.Equal(["a.key"], both.Declares());
        Assert.True(both.NamesTheMember);

        var plain = StepDeclaration.Of("Group", "read", eachSpoken: true);

        Assert.Empty(plain.Declares());
        Assert.False(plain.NamesTheMember);
    }

    [Fact]
    public void Resolving_leaves_the_step_alone_where_its_locator_names_no_key()
    {
        // The substitution is not a pass over every locator: one that names its element outright is
        // returned as it was, so nothing about it can change on a machine that reads a different file.
        var plain = StepDeclaration.Of("Group#interval", "read", eachSpoken: true);

        Assert.Same(plain, plain.Naming(_ => throw new InvalidOperationException("nothing to resolve")));
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>Sweep one locator over the fixture's rows, or null where the desk cannot observe.</summary>
    /// <param name="locator">What to address the row by.</param>
    private SuiteVerdict? Run(string locator)
    {
        if (!Desk.Read().CanObserve)
            return null;

        File.WriteAllText(
            Path.Combine(root, "strings.en.json"),
            """
            {
              "rows.headers.language": "Language",
              "rows.headers.directory": "Working directory",
              "rows.headers.startup": "Startup",
              "rows.headers.interval": "Refresh interval"
            }
            """);

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "languageFiles": ["strings.en.json"],
              "language": { "fallback": "en" },
              "timeouts": { "resolve": 400, "act": 4000, "poll": 25 }
            }
            """);

        var cases = $$"""
            {
              "cases": [
                {
                  "name": "the row the strings name is on the page and everything in it is named",
                  "catches": "a case addressing a row by the words it happened to carry in one language",
                  "steps": [
                    {
                      "locator": {{System.Text.Json.JsonSerializer.Serialize(locator)}},
                      "act": "read",
                      "eachSpoken": true,
                      "named": "the row the strings name is on the page"
                    }
                  ]
                }
              ]
            }
            """;

        return Suite.Run(
            ScenarioFile.Read("declared.cases.json", cases),
            Selection.All,
            fixtureRoot,
            ProjectDeclaration.Load(declaration));
    }
}

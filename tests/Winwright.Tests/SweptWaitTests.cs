using System.Diagnostics;

using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW241. That a sweep waits, and that a sweep which gave up says whether the page was still saying
/// <em>not yet</em>.
/// <para>
/// WW236 kept the sweep out of the attempt loop on the reasoning that retrying would re-read a whole
/// tree for the same answer. The measurement that overturned it came from claude-tray: the tab control
/// a cover was about is Collapsed, and therefore absent from the tree, until the report renders — so
/// the sweep read <c>0 of 4</c> and the case beside it read three labels out of that same pane seconds
/// later and passed. A window still drawing does not have the same tree a moment later.
/// </para>
/// </summary>
public sealed class SweptWaitTests : IDisposable
{
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;

    /// <summary>The word the strings file below declares as this page's loading text.</summary>
    private const string StillCounting = "Computing your consumption pace…";

    private readonly string root = Directory.CreateTempSubdirectory("winwright-swept-").FullName;

    /// <summary>
    /// Two of the three strings the file declares, and the loading note beside them. Which is the
    /// shape the defect had: a page part-way through drawing, saying so, with a sweep reading it.
    /// </summary>
    private readonly PumpedDialog dialog = PumpedDialog.Open(
        "winwright swept",
        new PumpedDialog.ChildWindow("Static", "Overview", WsChild | WsVisible, 20, 20, 220, 20),
        new PumpedDialog.ChildWindow("Static", "Sessions", WsChild | WsVisible, 20, 50, 220, 20),
        new PumpedDialog.ChildWindow("Static", StillCounting, WsChild | WsVisible, 20, 80, 220, 20));

    public void Dispose()
    {
        dialog.Dispose();
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void A_sweep_that_cannot_hold_waits_the_resolve_budget_before_saying_so()
    {
        // The measurement, and it is about time rather than about text: reading once and comparing
        // once is fast, and a sweep that polls cannot be. 600ms declared, and the assertion is that
        // it spent most of it — not that it spent exactly it, which would be a claim about the
        // machine's scheduler rather than about this engine.
        var clock = Stopwatch.StartNew();
        var verdict = Run(resolveMs: 600, loading: false);
        clock.Stop();

        if (verdict is null)
            return;

        Assert.NotEqual(RunOutcome.Passed, verdict.Outcome);
        Assert.True(
            clock.ElapsedMilliseconds > 400,
            $"the sweep gave up after {clock.ElapsedMilliseconds}ms of a 600ms budget, so it is not polling");

        // And it says how long it waited, because "not read" about a window that was given a third of
        // a second is a different finding from the same words about a window given ten.
        Assert.Contains("Waited", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_sweep_that_gave_up_while_the_page_said_it_was_loading_names_that()
    {
        // The half that made the claude-tray red confusing. The project declared what *not yet* looks
        // like and nothing in a run read it, so a page that never finished counting failed as a set
        // that was missing — and a reader acts on those two differently.
        var verdict = Run(resolveMs: 400, loading: true);
        if (verdict is null)
            return;

        Assert.NotEqual(RunOutcome.Passed, verdict.Outcome);

        // Assert.True with the whole reading as the message rather than Assert.Contains, which
        // truncates: a red about a sentence has to show the sentence it read.
        var said = Said(verdict);
        Assert.True(said.Contains("still computing", StringComparison.Ordinal), said);
        Assert.True(said.Contains(StillCounting, StringComparison.Ordinal), said);

        // Still a failure about the set and not a hole. Nothing here excuses the check: the page being
        // slow is the application's business, and DeskFacts says so in as many words — a condition
        // about the thing under test may not excuse the assertion that was looking for its defect.
        Assert.Empty(verdict.Ran.SelectMany(one => one.Verdict.Unchecked));
    }

    [Fact]
    public void A_language_file_named_by_its_tag_alone_is_read()
    {
        // WW242. `en.json` and not `strings.en.json`, which is the layout an application reaches for
        // when the files already sit in a folder that says what they are — claude-tray ships five of
        // them under lang/. The tag walk used to stop before the first part, so a file whose whole
        // name is its tag was never tagged, and every label refused with "declares no languageFiles
        // whose names carry a language tag".
        //
        // A run and not a unit call: the refusal it produced arrived at the first step that reached
        // for a label, which is exactly why it was quiet — everything loaded first.
        var verdict = Run(resolveMs: 400, loading: true, named: "en.json");
        if (verdict is null)
            return;

        var said = Said(verdict);
        Assert.False(said.Contains("carry a language tag", StringComparison.Ordinal), said);
        Assert.True(said.Contains("still computing", StringComparison.Ordinal), said);
    }

    [Fact]
    public void A_file_whose_name_carries_no_tag_is_still_nothing()
    {
        // The other half of the same bound, and the reason it looked deliberate: a file named for what
        // it holds rather than for a language must go on answering nothing. It is Culture() that says
        // so and not the index, which is what made the index safe to move.
        var verdict = Run(resolveMs: 400, loading: true, named: "strings.json");
        if (verdict is null)
            return;

        Assert.True(
            Said(verdict).Contains("carry a language tag", StringComparison.Ordinal),
            Said(verdict));
    }

    [Fact]
    public void A_project_that_declares_no_loading_text_gets_no_sentence_about_it()
    {
        // The window is showing the same label either way. What changes is whether the project called
        // it loading text, and a sweep must not decide that for it: any word can be a caption.
        var verdict = Run(resolveMs: 400, loading: false);
        if (verdict is null)
            return;

        Assert.DoesNotContain("still computing", Said(verdict), StringComparison.Ordinal);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>
    /// Run the sweep against a strings file this test writes, or null where the desk cannot observe.
    /// </summary>
    /// <param name="resolveMs">The resolve budget, which is what a sweep now waits out.</param>
    /// <param name="loading">Whether the project declares its loading key.</param>
    /// <param name="named">What to call the strings file, which is what WW242 is about.</param>
    private SuiteVerdict? Run(int resolveMs, bool loading, string named = "strings.en.json")
    {
        if (!Desk.Read().CanObserve)
            return null;

        // 'Profiles' is declared and is not in the dialog, so the sweep cannot hold however long it
        // waits. That is the point: what is being measured is the waiting, and a sweep that could
        // succeed would measure the machine's speed instead.
        File.WriteAllText(
            Path.Combine(root, named),
            $$"""
            {
              "stats.tab.overview": "Overview",
              "stats.tab.sessions": "Sessions",
              "stats.tab.profiles": "Profiles",
              "labels.loading": {{System.Text.Json.JsonSerializer.Serialize(StillCounting)}}
            }
            """);

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "languageFiles": [{{System.Text.Json.JsonSerializer.Serialize(named)}}],

              // Named because this machine is pt-BR and the project ships one language. Without it the
              // step throws rather than fails, with a refusal naming this exact key — which is right:
              // reading a label in a language nobody declared would be answering in English and
              // calling it the application's word.
              "language": { "fallback": "en" },
              {{(loading ? "\"loading\": [\"labels.loading\"]," : "")}}
              "timeouts": { "resolve": {{resolveMs}}, "act": 4000, "poll": 25 }
            }
            """);

        return Suite.Run(
            ScenarioFile.Read("swept.cases.json", Cases),
            Selection.All,
            dialog.Root,
            ProjectDeclaration.Load(declaration));
    }

    private const string Cases = """
        {
          "cases": [
            {
              "name": "every tab the strings declare is in the tree",
              "catches": "a sweep that read a window part-way through drawing and reported the set missing",
              "steps": [ { "locator": "Text", "act": "read", "covers": "stats.tab" } ]
            }
          ]
        }
        """;
}

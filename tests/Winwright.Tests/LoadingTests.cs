using System.Windows.Automation;

using Winwright.Asserting;
using Winwright.Locating;
using Winwright.Processes;
using Winwright.Projects;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW43. Measured in claude-tray: a report on a machine with 213 recent transcript files took about
/// 25 seconds to build, and at the default wait the copy came back as a heading, a subtitle and the
/// words computing your consumption pace. Two variants captured that way are near-identical for the
/// same reason, so comparing them proves nothing, and it was caught only because somebody looked.
/// <para>
/// Driven against the fixture's own <c>--loading</c>, whose note is a declared string like every
/// other caption it shows — a fixture whose loading text nothing declares cannot drive the check it
/// exists for.
/// </para>
/// </summary>


[Collection(WindowFixture.Serial)]
public sealed class LoadingTests : IDisposable
{
    private readonly ProcessRegister register = new();
    private readonly string root = Directory.CreateTempSubdirectory("winwright-loading-").FullName;

    public void Dispose()
    {
        register.Dispose();
        Directory.Delete(root, recursive: true);
    }

    /// <summary>A project declaring the fixture's own strings and the key its loading note carries.</summary>
    private ProjectDeclaration Declared(params string[] keys)
    {
        var strings = Directory
            .EnumerateFiles(Path.Combine(Path.GetDirectoryName(Fixture.Executable())!, "strings"), "*.json")
            .Select(one => System.Text.Json.JsonSerializer.Serialize(one));

        var path = Path.Combine(root, $"winwright.{keys.Length}.{Guid.NewGuid():N}.json");
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "sourceRoot": {{System.Text.Json.JsonSerializer.Serialize(root)}},
              "languageFiles": [{{string.Join(", ", strings)}}],
              "loading": [{{string.Join(", ", keys.Select(one => System.Text.Json.JsonSerializer.Serialize(one)))}}]
            }
            """);

        return ProjectDeclaration.Load(path);
    }

    /// <summary>
    /// Which of the fixture's languages this machine's run resolves to.
    /// <para>
    /// The whole point of reading the strings from the project's files is that the run and the
    /// application are in the same language, and which one that is belongs to the machine. A case
    /// that launched the fixture in English and resolved the labels on a Portuguese desk would be
    /// asserting that two different languages do not match, which is true and about nothing.
    /// </para>
    /// </summary>
    private static string Speaking(ProjectDeclaration declaration)
    {
        var resolved = ResolvedLanguage.Resolve(declaration).Culture.Name;

        // Read off the files the fixture ships rather than typed here. The suite cannot see the
        // fixture's types on purpose, and a list of its languages copied into this file is a list
        // that goes stale the day it ships another one.
        var carried = declaration.LanguageFiles
            .Select(one => Path.GetFileNameWithoutExtension(one).Split('.').Last())
            .ToList();

        return carried.FirstOrDefault(one => string.Equals(one, resolved, StringComparison.OrdinalIgnoreCase))
            ?? carried.FirstOrDefault(
                one => resolved.StartsWith(one.Split('-')[0], StringComparison.OrdinalIgnoreCase))
            ?? "en";
    }

    private AutomationElement Window(params string[] flags)
    {
        var start = Fixture.Started(flags);
        var launched = Attachable.Launch(register, start);

        var window = Waits.Until(
            "draw",
            $"the fixture never drew a window (pid {launched.Pid})",
            () =>
            {
                var found = TopLevelWindows.Largest(launched.Pid);
                return found is not null && found.Title.Length > 0 ? found : null;
            });

        return AutomationElement.FromHandle(window.Handle);
    }

    [Fact]
    public void A_page_still_computing_says_so_rather_than_being_photographed()
    {
        // Ten seconds, which is longer than this case takes to read the tree — the point being
        // that the check answers from what the page says rather than from how long anybody waited.
        var declaration = Declared("labels.loading");
        var read = Loading.In(Window("--loading=10000", $"--language={Speaking(declaration)}"), declaration);

        Assert.True(read.Was, read.Sentence());
        Assert.False(read.Settled, read.Sentence());

        var showing = Assert.Single(read.Showing);
        Assert.Equal("labels.loading", showing.Key);

        // Named, and named with the key: a reader told the page is loading needs to know which
        // string said so, because that is the one they will grep for.
        Assert.Contains("still computing", read.Sentence(), StringComparison.Ordinal);
        Assert.Contains("labels.loading", read.Sentence(), StringComparison.Ordinal);
        Assert.Equal(AssertionOutcome.Failed, read.AsAssertion().Outcome);
    }

    [Fact]
    public void A_page_that_has_finished_says_so_rather_than_saying_nothing()
    {
        // The arm a one-sided check gets wrong. A reading that only ever spoke up while loading
        // would be indistinguishable from one nobody took.
        var declaration = Declared("labels.loading");
        var window = Window("--loading=0", $"--language={Speaking(declaration)}");

        var settled = Waits.Until(
            "cycle",
            "the fixture never finished loading",
            () =>
            {
                var read = Loading.In(window, declaration);
                return read.Settled ? read : null;
            });

        Assert.True(settled.Was);
        Assert.Contains("showing none of the 1 loading string(s)", settled.Sentence(), StringComparison.Ordinal);
        Assert.Equal(AssertionOutcome.Passed, settled.AsAssertion().Outcome);
    }

    [Fact]
    public void A_walk_that_ran_out_is_not_a_page_that_has_finished()
    {
        // WW189, and the defect this shipped with an hour earlier. Walked two deep the loading note
        // is out of reach, so nothing is found — and answering "finished" to that is a green
        // covering a check that never got to the control it was about.
        //
        // Two and not five: five reaches this fixture's note, measured. The depth that truncates is
        // a property of the tree being walked, which is exactly why a reading cannot decide from a
        // number whether its own absence means anything.
        var declaration = Declared("labels.loading");
        var window = Window("--loading=10000", $"--language={Speaking(declaration)}");

        var shallow = Loading.In(window, declaration, depth: 2);

        Assert.True(shallow.Was, shallow.Sentence());
        Assert.Empty(shallow.Showing);
        Assert.False(shallow.Whole, "the walk reached everything at depth two, so this proves nothing");
        Assert.False(shallow.Settled, shallow.Sentence());

        Assert.Contains("is not settled", shallow.Sentence(), StringComparison.Ordinal);
        Assert.Contains("element(s) were not walked", shallow.Sentence(), StringComparison.Ordinal);

        var verdict = shallow.AsAssertion();
        Assert.Equal(AssertionOutcome.Unchecked, verdict.Outcome);
        Assert.Equal(Winwright.Tracing.StepVerdict.Unchecked, shallow.AsTraceStep().Verdict);

        // And the control: the same window walked deep enough answers, which is what makes the
        // arm above a statement about the walk rather than about the page.
        var deep = Loading.In(window, declaration);

        Assert.True(deep.Whole, deep.Sentence());
        Assert.True(deep.Computing, deep.Sentence());
    }

    [Fact]
    public void Finding_the_string_is_proof_and_a_short_walk_cannot_argue_with_it()
    {
        // The asymmetry that makes this right rather than merely cautious. Not finding a string is
        // only an answer where the walk reached everything; finding one is positive evidence, and a
        // walk that stopped short somewhere else has nothing to say about it.
        var declaration = Declared("labels.loading");
        var window = Window("--loading=10000", $"--language={Speaking(declaration)}");

        // Deep enough to reach the note, and shallow enough that something else is left unwalked.
        var read = Loading.In(window, declaration, depth: 5);

        if (read.Showing.Count == 0)
            return;

        Assert.True(read.Computing, read.Sentence());
        Assert.Equal(AssertionOutcome.Failed, read.AsAssertion().Outcome);
        Assert.Equal(Winwright.Tracing.StepVerdict.Failed, read.AsTraceStep().Verdict);
    }

    [Fact]
    public void A_key_none_of_the_language_files_carries_refuses_rather_than_matching_nothing()
    {
        // The whole of the second half. A check that silently matches nothing reports a page as
        // finished forever, which is the shape of defect this path exists to stop — so a key the
        // project cannot resolve ends the run rather than quietly passing every capture after it.
        var refusal = Assert.Throws<UnusableLabelException>(
            () => Loading.In(Window("--loading=0"), Declared("labels.thereIsNoSuchKey")));

        Assert.Contains("labels.thereIsNoSuchKey", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("is not in", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_key_whose_string_carries_a_placeholder_is_refused_for_the_same_reason()
    {
        // A template nobody filled in can never match a tree holding it already filled in, so a
        // check built on one passes every page forever. Labels already refuses it; this is the
        // arm that proves the loading reading goes through Labels rather than reading the files.
        var refusal = Assert.Throws<UnusableLabelException>(
            () => Loading.In(Window("--loading=0"), Declared("labels.profileName")));

        Assert.Contains("placeholder", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_project_that_declares_no_loading_strings_is_a_reading_not_taken()
    {
        // Not a settled page. A project that never said what its application shows while computing
        // has told this nothing, and answering "it has finished" would be a green covering a check
        // nobody could run.
        var read = Loading.In(Window("--loading=10000"), Declared());

        Assert.False(read.Was);
        Assert.False(read.Settled);
        Assert.Contains("declares no loading strings", read.Sentence(), StringComparison.Ordinal);

        var verdict = read.AsAssertion();
        Assert.Equal(AssertionOutcome.Unchecked, verdict.Outcome);
        Assert.Equal(LoadingCheck.PreconditionName, verdict.Missing!.Name);
    }
}

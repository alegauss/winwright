using Winwright.Asserting;
using Winwright.Projects;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW260. `covers` derives from one well — the language files a project declares — and that is the
/// right one for the set it was built for and the wrong one for what claude-tray's menu case counts.
/// <para>
/// That case counts profiles. The script asked the application, running it with a flag that prints
/// them, and compared the submenu against what came back. Neither half of that is in a strings file:
/// the profiles are this machine's data, and the number is whatever this machine has. Typing it is
/// the defect `covers` exists to refuse, one well over — a case asserting two profile entries goes on
/// asserting two after a third is added, and says nothing when it stops covering what it was written
/// for.
/// </para>
/// </summary>
public sealed class ReportedSetTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-reported-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    /// <summary>A project declaring how the application is asked for a set, and for what.</summary>
    /// <param name="named">What the set is called, as a case names it.</param>
    /// <param name="arguments">What the application is run with, as JSON array members.</param>
    private ProjectDeclaration Declaring(string named, string arguments)
    {
        var path = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "reportedSets": { {{System.Text.Json.JsonSerializer.Serialize(named)}}: [{{arguments}}] }
            }
            """);

        return ProjectDeclaration.Load(path);
    }

    [Fact]
    public void The_expected_set_is_what_the_application_says_it_has()
    {
        // The whole point: nothing here lists the profiles, and nothing in a case would. The values
        // come back from the application, so a third profile is expected before anybody edits a file.
        var set = DerivedSet.Reported("the profiles", Declaring("profiles", "\"--profiles\""), "profiles");

        Assert.Equal(["alpha", "bravo"], set.Expected);
        Assert.Contains("reported by", set.Source, StringComparison.Ordinal);
        Assert.Contains("--profiles", set.Source, StringComparison.Ordinal);
    }

    [Fact]
    public void A_reported_set_compares_the_way_a_declared_one_does()
    {
        var set = DerivedSet.Reported("the profiles", Declaring("profiles", "\"--profiles\""), "profiles");

        Assert.True(set.Against(["alpha", "bravo"]).Held);

        // And the defect the whole shape is for: the application grew a profile the window does not
        // show, and the case says so without anybody having edited it.
        var missing = set.Against(["alpha"]);

        Assert.False(missing.Held);
        Assert.Equal(["bravo"], missing.Missing);
    }

    [Fact]
    public void A_name_declared_in_both_wells_says_which_one_it_shadowed()
    {
        // WW290. The dispatch asks `reportedSets` first, so this name derives from the application and
        // the strings key of the same name is never read. Which one wins is not the problem — a rule
        // has to pick — the silence was: a passing sweep gave the reader no way to know their strings
        // key was dead, and a red sent them to the wrong file.
        var strings = Path.Combine(root, "strings.en.json");
        File.WriteAllText(strings, """{ "profiles": { "mine": "Pessoal", "work": "VILT Group" } }""");

        var path = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "languageFiles": ["strings.en.json"],
              "reportedSets": { "profiles": ["--profiles"] }
            }
            """);

        var set = DerivedSet.Reported("the profiles", ProjectDeclaration.Load(path), "profiles");

        // Still derived from the application — the rule did not change — and the source now says the
        // other declaration is there and was passed over.
        Assert.Equal(["alpha", "bravo"], set.Expected);
        Assert.Contains("shadows the 'profiles'", set.Source, StringComparison.Ordinal);
        Assert.Contains("strings.en.json", set.Source, StringComparison.Ordinal);
    }

    [Fact]
    public void A_name_in_only_one_well_says_nothing_about_shadowing()
    {
        // The other half, and what keeps the clause meaning something: a project whose strings have no
        // such key gets no note, so a reader who sees one knows there really are two declarations.
        var strings = Path.Combine(root, "strings.en.json");
        File.WriteAllText(strings, """{ "tabs": { "one": "Panes" } }""");

        var path = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "languageFiles": ["strings.en.json"],
              "reportedSets": { "profiles": ["--profiles"] }
            }
            """);

        Assert.DoesNotContain(
            "shadows",
            DerivedSet.Reported("the profiles", ProjectDeclaration.Load(path), "profiles").Source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_strings_file_that_will_not_parse_is_not_reported_as_a_shadow()
    {
        // It is also not this reading's refusal to make. A broken strings file is what `From` refuses
        // when somebody derives from it, and borrowing that here would turn an unrelated problem into
        // a failure about a reported set that reads perfectly well.
        File.WriteAllText(Path.Combine(root, "strings.en.json"), "{ this is not json");

        var path = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "languageFiles": ["strings.en.json"],
              "reportedSets": { "profiles": ["--profiles"] }
            }
            """);

        var set = DerivedSet.Reported("the profiles", ProjectDeclaration.Load(path), "profiles");

        Assert.Equal(["alpha", "bravo"], set.Expected);
        Assert.DoesNotContain("shadows", set.Source, StringComparison.Ordinal);
    }

    [Fact]
    public void A_set_the_project_does_not_declare_is_refused_with_the_ones_it_does()
    {
        var refused = Assert.Throws<UnderivableSetException>(
            () => DerivedSet.Reported("the accounts", Declaring("profiles", "\"--profiles\""), "accounts"));

        Assert.Contains("'profiles'", refused.Message, StringComparison.Ordinal);
        Assert.Contains("accounts", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_application_that_reports_nothing_is_broken_and_never_met()
    {
        // The same rule an empty key is under, and for the same reason: an empty expected set is met
        // by an empty window, which is the hole this whole shape exists to close. `--render` writes a
        // picture and prints a receipt to a path, so asking it for a set prints nothing set-shaped.
        var refused = Assert.Throws<UnderivableSetException>(() => DerivedSet.Reported(
            "the profiles",
            Declaring("profiles", $"\"--render={System.Text.Json.JsonSerializer.Serialize(Path.Combine(root, "x.png")).Trim('"')}\", \"--sizeless\""),
            "profiles"));

        // Refused for having exited non-zero, which is the arm above the empty one: what an
        // application prints on its way to a failure is not an answer.
        //
        // WW294 widened that word. The reader is now shared with the value well, so a refusal saying
        // "is not a set" would be wrong half the time it fires — the run failed, and what it printed
        // is not an answer to either question.
        Assert.Contains("is not an answer", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_reported_set_declared_with_no_arguments_is_refused_where_it_was_written()
    {
        // Nothing says how the application is asked, so the run would have started it with no
        // arguments and read whatever it prints when it is asked nothing — which is a window.
        var path = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "reportedSets": { "profiles": [] }
            }
            """);

        var refused = Assert.Throws<ArgumentException>(() => ProjectDeclaration.Load(path));

        Assert.Contains("with no arguments", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_fixture_reports_the_same_profiles_its_store_writes()
    {
        // The half that keeps this from proving nothing. A fixture whose read-out and whose store
        // disagreed would let a case pass against a set the application does not really have, which
        // is the hardcoded expectation wearing a different coat.
        //
        // Read by running the article and looking at what it wrote, and never off its assembly: the
        // suite does not reference the fixture on purpose — an application under test is launched
        // from its own output — so a constant read from beside the harness would be this check
        // agreeing with a second transcription rather than with the application. WW200's rule.
        var store = Path.Combine(root, "store");
        using (var writing = System.Diagnostics.Process.Start(Fixture.Started($"--store={store}", "--render=" + Path.Combine(root, "ignored.png")))!)
        {
            Assert.True(writing.WaitForExit(30_000), "the fixture never finished writing its store");
        }

        var wrote = System.Text.Json.JsonSerializer.Deserialize<string[]>(
            File.ReadAllText(Path.Combine(store, "profiles.json")))!;

        var set = DerivedSet.Reported("the profiles", Declaring("profiles", "\"--profiles\""), "profiles");

        Assert.Equal(wrote, set.Expected);
    }

    /// <summary>
    /// WW466. The red this well can produce, written out. Everything above compares and reads the
    /// answer off the comparison — which is the reason the defect lived here: the sentence is where
    /// a missing value is looked up, so a sweep that held never touched it and a sweep that did not
    /// threw about a collection instead of naming what the window was missing.
    /// </summary>
    [Fact]
    public void A_reported_set_that_finds_something_missing_can_say_which()
    {
        var set = DerivedSet.Reported("the profiles", Declaring("profiles", "\"--profiles\""), "profiles");
        var said = set.Against(["alpha"]).Sentence();

        Assert.Contains("'bravo'", said, StringComparison.Ordinal);
        Assert.Contains("was not read", said, StringComparison.Ordinal);

        // And no line pretended to. The strings well points at a file, this one has no file to point
        // at, and a trace invented for a printed value would send a reader looking for a declaration
        // nobody wrote.
        Assert.DoesNotContain("'bravo' (", said, StringComparison.Ordinal);
    }

    /// <summary>
    /// WW466, the other end. `Origins` is read by position against <c>Expected</c>, so a reported set
    /// carries one per value — unknown, which is what is true of a value the application printed.
    /// </summary>
    [Fact]
    public void Every_reported_value_carries_a_provenance_that_says_it_came_from_nowhere_nameable()
    {
        var set = DerivedSet.Reported("the profiles", Declaring("profiles", "\"--profiles\""), "profiles");

        Assert.Equal(set.Expected.Count, set.Origins.Count);
        Assert.False(set.Whence("alpha").Known);
        Assert.False(set.Whence("bravo").Known);

        // The one answer that is not about the set's shape: a value that is not a member at all.
        Assert.False(set.Whence("charlie").Known);
    }

    /// <summary>
    /// WW466 as a containment claim, which is the shape claude-tray's submenu sweep makes: the
    /// entries are decorated — <c>alpha — used 41%</c> — so equality is false of every one of them,
    /// and a value in no name at all is the red that has to be printable.
    /// </summary>
    [Fact]
    public void A_containment_claim_over_reported_values_says_which_one_is_in_nothing()
    {
        var set = DerivedSet.Reported("the profiles", Declaring("profiles", "\"--profiles\""), "profiles");
        var said = set.Against(["alpha — used 41%  · active now"], SetMatch.Within).Sentence();

        Assert.Contains("'bravo'", said, StringComparison.Ordinal);
        Assert.Contains("is in nothing that was read", said, StringComparison.Ordinal);
    }

    /// <summary>
    /// WW468. The claim, and it is one neither half of can be typed: the two wells derive the same
    /// key, and the language they are asked in is the only thing that can make them disagree.
    /// </summary>
    [Fact]
    public void A_read_out_asked_for_the_runs_language_answers_what_the_strings_for_it_declare()
    {
        var project = Declaring("headers", "\"--tab-names\", \"--language={language}\"");

        foreach (var tag in new[] { "en", "pt-BR", "de" })
        {
            var speaking = System.Globalization.CultureInfo.GetCultureInfo(tag);

            var reported = DerivedSet.Reported("the headers", project, "headers", speaking);
            var declared = DerivedSet.From("the headers", Fixture.StringsFor(tag), "tabs");

            Assert.Equal(declared.Expected, reported.Expected);

            // The substitution is answered and not passed on. A reader of a green has to be able to
            // see which language it was derived in, and `{language}` where the answer goes says
            // nothing about the run that produced it.
            Assert.Contains($"--language={tag}", reported.Source, StringComparison.Ordinal);
            Assert.DoesNotContain("{language}", reported.Source, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// WW468, and the half that makes the other one mean something. This is the defect as claude-tray
    /// met it: the window in one language and the read-out in another, and a sweep whose whole
    /// purpose is that the two agree reporting a value missing from the window it was on.
    /// </summary>
    [Fact]
    public void A_read_out_asked_for_another_language_disagrees_with_the_window_it_is_compared_against()
    {
        var project = Declaring("headers", "\"--tab-names\", \"--language={language}\"");

        var drawn = DerivedSet.From("the headers", Fixture.StringsFor("en"), "tabs").Expected;
        var elsewhere = DerivedSet.Reported(
            "the headers", project, "headers", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));

        var compared = elsewhere.Against(drawn);

        Assert.False(compared.Held);
        Assert.Contains("Relatório", compared.Missing);
        Assert.Contains("Report", compared.Unexpected);
    }

    /// <summary>
    /// WW469. The claim on its own, because WW468's cases would go green over it: they compare two
    /// readings of one file, and a corruption on the pipe that is not in the file makes them differ
    /// without saying why. This names a character and reads it back.
    /// </summary>
    [Fact]
    public void A_read_out_answering_outside_ASCII_arrives_as_what_the_application_printed()
    {
        var set = DerivedSet.Reported(
            "the headers",
            Declaring("headers", "\"--tab-names\", \"--language={language}\""),
            "headers",
            System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));

        // The one the two code pages spell differently: 0xF3 is `ó` in Windows-1252 and `¾` in CP850,
        // which is the whole of the defect in one byte.
        Assert.Contains("Relatório", set.Expected);
        Assert.DoesNotContain(set.Expected, one => one.Contains('�', StringComparison.Ordinal));
    }

    /// <summary>
    /// WW468. A declaration naming no substitution is run exactly as it was written, which is every
    /// project that has ever used this well — the profile read-outs above included.
    /// </summary>
    [Fact]
    public void A_read_out_that_names_no_language_is_run_with_what_the_project_wrote()
    {
        var project = Declaring("profiles", "\"--profiles\"");

        Assert.Equal(
            DerivedSet.Reported("the profiles", project, "profiles").Expected,
            DerivedSet.Reported(
                "the profiles", project, "profiles", System.Globalization.CultureInfo.GetCultureInfo("de")).Expected);
    }
}

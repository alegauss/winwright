using System.Globalization;

using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW15. Verifying a task against a Portuguese tray with the default English produced four
/// failures for labels that were all present, in another language. There is no command line to
/// read on an attach, so the language is resolved the way the application resolves it.
/// </summary>
public sealed class ResolvedLanguageTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-lang-").FullName;
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    public void Dispose() => Directory.Delete(root, recursive: true);

    private string Preference(string json)
    {
        var path = Path.Combine(root, "settings.json");
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void The_saved_preference_answers_before_the_display_language()
    {
        var file = Preference("""{ "language": "pt-BR" }""");

        var resolved = ResolvedLanguage.Resolve(file, "language", English);

        Assert.Equal("pt-BR", resolved.Culture.Name);
        Assert.Equal(LanguageSource.SavedPreference, resolved.Source);
        Assert.Equal(file, resolved.From);
    }

    [Fact]
    public void The_display_language_answers_when_nothing_is_saved()
    {
        var resolved = ResolvedLanguage.Resolve(Preference("""{ }"""), "language", English);

        Assert.Equal("en-US", resolved.Culture.Name);
        Assert.Equal(LanguageSource.DisplayLanguage, resolved.Source);
        Assert.Contains("declares no 'language'", resolved.PreferenceMiss);
    }

    [Fact]
    public void A_project_that_declares_no_preference_file_falls_to_the_display_language_with_nothing_missed()
    {
        var resolved = ResolvedLanguage.Resolve(null, null, English);

        Assert.Equal(LanguageSource.DisplayLanguage, resolved.Source);
        Assert.Null(resolved.PreferenceMiss);
        Assert.Equal("the application is in en-US, from the display language.", resolved.Sentence());
    }

    [Fact]
    public void A_nested_key_is_reached_by_a_dotted_path()
    {
        var file = Preference("""{ "ui": { "locale": "de-DE" } }""");

        Assert.Equal("de-DE", ResolvedLanguage.Resolve(file, "ui.locale", English).Culture.Name);
    }

    [Fact]
    public void The_language_is_reported_out_loud_whatever_it_is()
    {
        var file = Preference("""{ "language": "pt-BR" }""");

        Assert.Equal(
            $"the application is in pt-BR, from {file}.",
            ResolvedLanguage.Resolve(file, "language", English).Sentence());
    }

    [Fact]
    public void A_language_explicitly_asked_for_that_the_app_is_not_in_is_a_hole()
    {
        var resolved = ResolvedLanguage.Resolve(Preference("""{ "language": "pt-BR" }"""), "language", English);

        var missing = resolved.Matching("en");
        Assert.False(missing.Satisfied);
        Assert.Contains("the scenario asks for en and the application is in pt-BR", missing.Absence);

        var declaration = AssertionDeclaration.Of(
            "the Settings label reads Settings", "the tray menu", ResolvedLanguage.PreconditionName);
        Assert.Equal(RunOutcome.Degraded, RunVerdict.Over([declaration.Unchecked(missing)]).Outcome);
    }

    [Fact]
    public void A_neutral_ask_is_content_with_any_of_its_regions()
    {
        var resolved = ResolvedLanguage.Resolve(Preference("""{ "language": "en-GB" }"""), "language", English);

        Assert.True(resolved.Matching("en").Satisfied);
        Assert.False(resolved.Matching("en-US").Satisfied);
    }

    [Fact]
    public void A_scenario_that_asks_for_nothing_is_never_held_to_a_language()
    {
        var resolved = ResolvedLanguage.Resolve(Preference("""{ "language": "pt-BR" }"""), "language", English);

        Assert.True(resolved.Matching(null).Satisfied);
        Assert.True(resolved.Matching("  ").Satisfied);
    }

    [Fact]
    public void A_preference_file_that_is_not_there_says_so_rather_than_saying_nothing()
    {
        var resolved = ResolvedLanguage.Resolve(Path.Combine(root, "absent.json"), "language", English);

        Assert.Equal(LanguageSource.DisplayLanguage, resolved.Source);
        Assert.Contains("is not there", resolved.PreferenceMiss);
        Assert.Contains("is not there", resolved.Sentence());
    }

    [Fact]
    public void A_preference_that_is_not_readable_json_is_named_rather_than_thrown()
    {
        var resolved = ResolvedLanguage.Resolve(Preference("""{ "language": """), "language", English);

        Assert.Equal(LanguageSource.DisplayLanguage, resolved.Source);
        Assert.Contains("is not readable JSON", resolved.PreferenceMiss);
    }

    [Fact]
    public void A_saved_language_windows_does_not_know_is_named_rather_than_used()
    {
        var resolved = ResolvedLanguage.Resolve(Preference("""{ "language": "zz-ZZ" }"""), "language", English);

        Assert.Equal(LanguageSource.DisplayLanguage, resolved.Source);
        Assert.Contains("which is no language Windows knows", resolved.PreferenceMiss);
    }

    [Fact]
    public void An_ask_that_is_not_a_language_never_matches()
    {
        Assert.False(ResolvedLanguage.Resolve(null, null, English).Matching("zz-ZZ").Satisfied);
    }

    [Fact]
    public void It_reads_the_preference_the_project_declared()
    {
        File.WriteAllText(Path.Combine(root, "settings.json"), """{ "language": "pt-BR" }""");
        File.WriteAllText(
            Path.Combine(root, ProjectDeclaration.FileName),
            """{ "language": { "preferenceFile": "settings.json", "preferenceKey": "language" } }""");

        var declaration = ProjectDeclaration.Find(root);

        Assert.Equal(Path.Combine(root, "settings.json"), declaration.LanguagePreferenceFile);
        Assert.Equal("language", declaration.LanguagePreferenceKey);
        Assert.Equal("pt-BR", ResolvedLanguage.Resolve(declaration).Culture.Name);
    }
}

using System.Globalization;

using Winwright.Asserting;
using Winwright.Projects;
using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW240. A derived set refused a project declaring more than one strings file, so an application
/// shipping five languages had to declare one of them and pretend the other four were not there.
/// <para>
/// Measured migrating claude-tray, which ships <c>en</c>, <c>es</c>, <c>fr</c>, <c>pt-BR</c> and
/// <c>pt-PT</c>. Declaring all five made a sweep refuse; declaring only English worked, and worked
/// <em>because</em> every fixture there launches with <c>--lang en</c>. So the answer was already
/// written down one line above and was being supplied instead by a project-wide declaration that
/// happened to agree with it.
/// </para>
/// <para>
/// A fact about the window and not about the launch, which is <c>shareable</c>'s shape rather than
/// <c>environment</c>'s: it decides nothing about how the application starts, it says what the
/// arguments produced.
/// </para>
/// </summary>
public sealed class FixtureLanguageTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-speaking-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void A_project_shipping_five_languages_derives_the_set_the_window_is_showing()
    {
        var project = Shipping();

        Assert.Equal(
            ["Overview", "Sessions"],
            DerivedSet.From("the tabs", project, "stats.tab", new CultureInfo("en")).Expected);

        Assert.Equal(
            ["Visão geral", "Sessões"],
            DerivedSet.From("the tabs", project, "stats.tab", new CultureInfo("pt-BR")).Expected);
    }

    [Fact]
    public void A_project_shipping_five_and_a_fixture_saying_nothing_is_still_refused()
    {
        // The old rule, kept and for the old reason: picking the first would derive an expectation in
        // a language nobody is looking at, which is worse than refusing.
        var refusal = Assert.Throws<UnderivableSetException>(
            () => DerivedSet.From("the tabs", Shipping(), "stats.tab"));

        Assert.Contains("no fixture said which language", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_language_the_project_ships_no_file_for_falls_back_where_one_is_declared()
    {
        // The same fallback a label takes, because it is now literally the same code: es is not
        // shipped below, and the project says what it falls back to.
        var set = DerivedSet.From("the tabs", Shipping(fallback: "en"), "stats.tab", new CultureInfo("de"));

        Assert.Equal(["Overview", "Sessions"], set.Expected);
    }

    [Fact]
    public void A_language_with_nothing_to_fall_back_to_is_refused_and_says_what_is_shipped()
    {
        var refusal = Assert.Throws<UnderivableSetException>(
            () => DerivedSet.From("the tabs", Shipping(fallback: null), "stats.tab", new CultureInfo("de")));

        Assert.Contains("this project ships", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("pt-BR", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_fixture_says_what_its_window_is_in_and_the_tag_is_judged_where_it_was_written()
    {
        var fixture = FixtureDeclaration.Of("the main window", language: "pt-BR");

        Assert.Equal("pt-BR", fixture.Language);
        Assert.Equal("pt-BR", fixture.Speaking!.Name);

        // Refused at the point of insertion, like every other field: a tag that is not a language is
        // wrong on every machine, and finding out on the run costs a launch to learn it.
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => FixtureDeclaration.Of("the main window", language: "Portuguese"));

        Assert.Contains("is not a language tag", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_fixture_that_says_nothing_speaks_nothing_rather_than_guessing()
    {
        Assert.Null(FixtureDeclaration.Of("the main window").Speaking);
        Assert.Null(FixtureDeclaration.Plain.Speaking);
    }

    [Fact]
    public void A_file_can_declare_it_and_two_fixtures_may_be_in_two_languages()
    {
        // The property the task is about: one file, two windows, two languages, neither lying.
        var loaded = ScenarioFile.Read(
            "two.cases.json",
            """
            {
              "fixtures": [
                { "name": "English", "arguments": ["--lang", "en"], "language": "en" },
                { "name": "Brazilian", "arguments": ["--lang", "pt-BR"], "language": "pt-BR" }
              ],
              "cases": [
                {
                  "name": "the tabs read in English",
                  "fixture": "English",
                  "steps": [ { "locator": "Text", "act": "read", "covers": "stats.tab" } ]
                },
                {
                  "name": "the tabs read in Portuguese",
                  "fixture": "Brazilian",
                  "steps": [ { "locator": "Text", "act": "read", "covers": "stats.tab" } ]
                }
              ]
            }
            """);

        Assert.Equal(["en", "pt-BR"], loaded.Select(one => one.Fixture.Language));
    }

    /// <summary>A project shipping five languages, which is what claude-tray actually ships.</summary>
    /// <param name="fallback">What it falls back to, or null where it declares none.</param>
    private ProjectDeclaration Shipping(string? fallback = "en")
    {
        var strings = Directory.CreateDirectory(Path.Combine(root, "lang")).FullName;
        File.WriteAllText(
            Path.Combine(strings, "en.json"),
            """{ "stats.tab.overview": "Overview", "stats.tab.sessions": "Sessions" }""");
        File.WriteAllText(
            Path.Combine(strings, "pt-BR.json"),
            """{ "stats.tab.overview": "Visão geral", "stats.tab.sessions": "Sessões" }""");
        File.WriteAllText(
            Path.Combine(strings, "pt-PT.json"),
            """{ "stats.tab.overview": "Vista geral", "stats.tab.sessions": "Sessões" }""");
        File.WriteAllText(
            Path.Combine(strings, "fr.json"),
            """{ "stats.tab.overview": "Aperçu", "stats.tab.sessions": "Sessions" }""");
        File.WriteAllText(
            Path.Combine(strings, "it.json"),
            """{ "stats.tab.overview": "Panoramica", "stats.tab.sessions": "Sessioni" }""");

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "languageFiles": ["lang/en.json", "lang/pt-BR.json", "lang/pt-PT.json", "lang/fr.json", "lang/it.json"],
              {{(fallback is null ? "" : $"\"language\": {{ \"fallback\": \"{fallback}\" }},")}}
              "timeouts": { "resolve": 400, "act": 4000, "poll": 25 }
            }
            """);

        return ProjectDeclaration.Load(declaration);
    }
}

using System.Globalization;

using Winwright.Asserting;
using Winwright.Projects;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW50. Labels are matched in English against a window rendering another language.
/// <para>
/// The first test is claude-tray's afternoon: verifying against a Portuguese tray with the default
/// English produced four failures for labels that were all present, in another language. The
/// second is the quieter half of the same defect — an English word that matches nothing in a
/// Portuguese window and is reported as a pass because nobody was asserting on it.
/// </para>
/// </summary>
public sealed class LabelTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-labels-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private ProjectDeclaration Project(string declaration, params (string Name, string Json)[] files)
    {
        foreach (var (name, json) in files)
            File.WriteAllText(Path.Combine(root, name), json);

        File.WriteAllText(Path.Combine(root, ProjectDeclaration.FileName), declaration);
        return ProjectDeclaration.Find(root);
    }

    private ProjectDeclaration Bilingual(string? fallback = null)
    {
        var language = fallback is null ? "" : ", \"language\": { \"fallback\": \"" + fallback + "\" }";
        return Project(
            $$"""{ "languageFiles": ["strings.en.json", "strings.pt.json"]{{language}} }""",
            ("strings.en.json", """{ "tray": { "quit": "Quit", "greeting": "Welcome, {0}" } }"""),
            ("strings.pt.json", """{ "tray": { "quit": "Sair", "greeting": "Bem-vindo, {0}" } }"""));
    }

    private static ResolvedLanguage Speaking(string tag) =>
        ResolvedLanguage.Resolve(null, null, CultureInfo.GetCultureInfo(tag));

    [Fact]
    public void A_portuguese_window_is_asserted_against_portuguese_words()
    {
        var label = Labels.For("tray.quit", Bilingual(), Speaking("pt-BR"));

        Assert.Equal("Sair", label.Text);
        Assert.Equal("pt", label.Culture.Name);

        // The project ships pt and the application is in pt-BR. That is the same language, so
        // nothing here fell back and the sentence does not warn about one.
        Assert.False(label.FellBack);
        Assert.Equal("'tray.quit' reads 'Sair' from strings.pt.json in pt.", label.Sentence());
    }

    [Fact]
    public void The_same_key_reads_english_against_an_english_window()
    {
        var label = Labels.For("tray.quit", Bilingual(), Speaking("en-GB"));

        // One key, one scenario, two windows. Nothing about the case says 'Quit' or 'Sair', which
        // is what stops a case being written against whichever machine happened to run it first.
        Assert.Equal("Quit", label.Text);
        Assert.Equal("en", label.Culture.Name);
        Assert.False(label.FellBack);
    }

    [Fact]
    public void A_value_carrying_a_placeholder_is_refused_and_never_skipped()
    {
        // 'Bem-vindo, Alexandre' in the tree can never equal 'Bem-vindo, {0}'. A scenario asking
        // for this is asking for something that cannot pass, and skipping it in silence is the
        // defect this whole rule exists to prevent wearing a different hat.
        var refused = Assert.Throws<UnusableLabelException>(
            () => Labels.For("tray.greeting", Bilingual(), Speaking("pt")));

        Assert.Contains("Bem-vindo, {0}", refused.Message);
        Assert.Contains("placeholder '{0}'", refused.Message);
        Assert.Contains("could ever pass", refused.Message);
    }

    [Fact]
    public void A_language_the_project_ships_nothing_for_is_refused_rather_than_answered_in_english()
    {
        // The whole symptom in one call: the application is in Japanese, the project ships en and
        // pt, and answering 'Quit' here is exactly how English gets matched against a window that
        // is not in English.
        var refused = Assert.Throws<UnusableLabelException>(
            () => Labels.For("tray.quit", Bilingual(), Speaking("ja-JP")));

        Assert.Contains("the application is in ja-JP", refused.Message);
        Assert.Contains("ships en, pt", refused.Message);
        Assert.Contains("Declare language.fallback", refused.Message);
    }

    [Fact]
    public void A_declared_fallback_is_used_and_the_label_says_it_fell_back()
    {
        var label = Labels.For("tray.quit", Bilingual(fallback: "en"), Speaking("ja-JP"));

        Assert.Equal("Quit", label.Text);
        Assert.True(label.FellBack);
        Assert.Contains("which is the fallback", label.Sentence());
        Assert.Contains("no strings for ja-JP", label.Sentence());
    }

    [Fact]
    public void A_fallback_the_project_does_not_ship_is_refused_and_names_what_it_does()
    {
        var refused = Assert.Throws<UnusableLabelException>(
            () => Labels.For("tray.quit", Bilingual(fallback: "de"), Speaking("ja-JP")));

        Assert.Contains("falls back to de", refused.Message);
        Assert.Contains("strings it ships are en, pt", refused.Message);
    }

    [Fact]
    public void A_key_the_strings_do_not_carry_is_refused_and_names_the_file_it_looked_in()
    {
        var refused = Assert.Throws<UnusableLabelException>(
            () => Labels.For("tray.settings", Bilingual(), Speaking("pt")));

        Assert.Contains("tray.settings", refused.Message);
        Assert.Contains("strings.pt.json", refused.Message);
        Assert.Contains("the pt strings", refused.Message);
    }

    [Fact]
    public void A_file_whose_name_carries_no_language_never_answers_and_the_refusal_says_so()
    {
        var project = Project(
            """{ "languageFiles": ["strings.json"] }""",
            ("strings.json", """{ "tray": { "quit": "Quit" } }"""));

        var refused = Assert.Throws<UnusableLabelException>(
            () => Labels.For("tray.quit", project, Speaking("en")));

        Assert.Contains("no languageFiles whose names carry a language tag", refused.Message);
        Assert.Contains("strings.en.json", refused.Message);
    }

    [Fact]
    public void A_flat_file_of_dotted_names_reads_the_same_label()
    {
        var project = Project(
            """{ "languageFiles": ["flat.pt.json"] }""",
            ("flat.pt.json", """{ "tray.quit": "Sair" }"""));

        Assert.Equal("Sair", Labels.For("tray.quit", project, Speaking("pt")).Text);
    }

    [Theory]
    [InlineData("Welcome, {0}")]
    [InlineData("Bem-vindo, {name}")]
    [InlineData("{{count}} items")]
    [InlineData("Hello %s")]
    [InlineData("%1$s said")]
    [InlineData("{0:N2} MB")]
    public void Every_shape_of_placeholder_a_windows_application_ships_is_recognised(string text) =>
        Assert.True(Labels.CarriesAPlaceholder(text), text);

    [Theory]
    [InlineData("Quit")]
    [InlineData("Sair")]
    [InlineData("100% done")]
    [InlineData("Save as...")]
    [InlineData("")]
    public void An_ordinary_label_is_not_mistaken_for_one(string text) =>
        Assert.False(Labels.CarriesAPlaceholder(text), text);

    [Fact]
    public void A_label_with_no_key_and_no_project_is_refused()
    {
        var project = Bilingual();

        Assert.Throws<ArgumentException>(() => Labels.For(" ", project, Speaking("en")));
        Assert.Throws<ArgumentNullException>(() => Labels.For("tray.quit", null!, Speaking("en")));
        Assert.Throws<ArgumentNullException>(() => Labels.For("tray.quit", project, null!));
    }
}

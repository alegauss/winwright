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
    public void A_strings_file_that_will_not_parse_is_refused_naming_it()
    {
        // WW196. The sixth way a label is unusable, and the one nothing drove: a file that is there
        // and will not open. It is neither a missing key nor a missing language, and a reader handed
        // "not in the file" for a file that never parsed would go looking for a key that is in it.
        var project = Project(
            """{ "languageFiles": ["strings.en.json"] }""",
            ("strings.en.json", """{ "tray": { "quit": "Quit" """));

        var refusal = Assert.Throws<UnusableLabelException>(
            () => Labels.For("tray.quit", project, Speaking("en-GB")));

        Assert.Equal(UnusableLabel.FileUnreadable, refusal.Arm);
        Assert.Contains("could not be read", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("strings.en.json", refusal.Message, StringComparison.Ordinal);
    }

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
    public void A_claim_that_reads_one_end_gets_the_fixed_part_the_placeholder_left()
    {
        // WW475. The refusal above argues about equality and two claims are not equality. `Bem-vindo,
        // {0}` has a fixed front, and every tree drawing it filled in begins with exactly that — so a
        // begins-with claim is the one form that could have matched, and it was the one nobody could
        // write.
        var label = Labels.For("tray.greeting", Bilingual(), Speaking("pt"), Anchored.Front);

        Assert.Equal("Bem-vindo,", label.Text);

        // And it says what it did, because a reader comparing this against the strings file would
        // otherwise find a longer value there and nothing explaining which part was compared.
        Assert.Equal("Bem-vindo, {0}", label.Cut);
        Assert.Contains("read past its placeholder", label.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void The_mirror_reads_the_other_side_and_each_refuses_the_end_the_placeholder_took()
    {
        // The other direction, and the two refusals that keep this narrow. A placeholder at the end
        // leaves nothing for an ends-with to read, and one at the front leaves nothing for a
        // begins-with — so each claim is refused by exactly the label the other one can use.
        var project = Project(
            """{ "languageFiles": ["strings.en.json"] }""",
            ("strings.en.json", """{ "tray": { "trailing": "Profile: {name}", "leading": "{name} is using it" } }"""));

        Assert.Equal("is using it", Labels.For("tray.leading", project, Speaking("en"), Anchored.Back).Text);

        var noFront = Assert.Throws<UnusableLabelException>(
            () => Labels.For("tray.leading", project, Speaking("en"), Anchored.Front));
        Assert.Contains("reads the text in front of it, and there is none", noFront.Message);

        var noBack = Assert.Throws<UnusableLabelException>(
            () => Labels.For("tray.trailing", project, Speaking("en"), Anchored.Back));
        Assert.Contains("reads the text after it, and there is none", noBack.Message);
    }

    [Fact]
    public void Equality_still_refuses_a_placeholder_whichever_end_it_is_at()
    {
        // The arm WW475 does not touch, written out because it is the one that was always right: a
        // filled-in tree can never equal a format string, and a `label` claim asking for one is
        // asking for something that cannot pass — from either end.
        var project = Project(
            """{ "languageFiles": ["strings.en.json"] }""",
            ("strings.en.json", """{ "tray": { "trailing": "Profile: {name}", "leading": "{name} is using it" } }"""));

        foreach (var key in new[] { "tray.trailing", "tray.leading" })
        {
            var refused = Assert.Throws<UnusableLabelException>(
                () => Labels.For(key, project, Speaking("en")));

            Assert.Equal(UnusableLabel.CarriesAPlaceholder, refused.Arm);
            Assert.Contains("could ever pass", refused.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void A_placeholder_in_the_middle_is_refused_from_both_ends()
    {
        // The third shape, and the reason this is a rule about ends rather than about placeholders.
        // A label with fixed text on both sides of something that moves has a front a begins-with
        // could read — and reading it would make the claim about a shape rather than about a string,
        // which is a different thing from what the field says it does.
        var project = Project(
            """{ "languageFiles": ["strings.en.json"] }""",
            ("strings.en.json", """{ "tray": { "middle": "Profile {name} is in use" } }"""));

        foreach (var anchored in new[] { Anchored.Front, Anchored.Back, Anchored.Whole })
            Assert.Throws<UnusableLabelException>(() => Labels.For("tray.middle", project, Speaking("en"), anchored));
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

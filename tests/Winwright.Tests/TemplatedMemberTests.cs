using Winwright.Asserting;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW118. A label whose value carries a placeholder is refused, because a tree holding
/// <c>Profile: Alexandre</c> can never equal <c>Profile: {name}</c> and skipping it in silence is
/// an assertion that did not run reported as one that passed. A derived set had the same hazard
/// with none of the guard: such a key joined the expected set, was never read from any window, and
/// landed in Missing on every run as an unfixable red — or worse, was matched by a control that
/// happened to render the literal braces.
/// <para>
/// Where the refusal goes was decided against a real strings file rather than in the abstract. The
/// fixture's own <c>labels</c> holds two ordinary strings and one templated one, so refusing the
/// whole derivation would make an otherwise good key underivable. They are excluded and said out
/// loud in the source sentence every verdict already prints.
/// </para>
/// </summary>
public sealed class TemplatedMemberTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-templated-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private string Strings(string json)
    {
        var path = Path.Combine(root, "strings.en.json");
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void A_templated_string_is_left_out_of_the_expectation_it_could_never_meet()
    {
        var file = Strings("""
            { "labels": { "heading": "winwright fixture", "profileName": "Profile: {name}" } }
            """);

        var set = DerivedSet.From("the labels", file, "labels");

        Assert.Equal(["winwright fixture"], set.Expected);
        Assert.Equal("labels.profileName", Assert.Single(set.Excluded).Key);
        Assert.Equal("Profile: {name}", set.Excluded[0].Value);
    }

    [Fact]
    public void The_exclusion_is_in_the_source_so_it_appears_under_every_run()
    {
        var file = Strings("""
            { "labels": { "heading": "winwright fixture", "profileName": "Profile: {name}" } }
            """);

        var set = DerivedSet.From("the labels", file, "labels");

        // A count that is not silent is not the defect. Source is what a comparison prints, so
        // the exclusion is read by whoever reads the verdict rather than by whoever opens the file.
        Assert.Contains("less 1 carrying a placeholder", set.Source);
        Assert.Contains("'labels.profileName'", set.Source);
        Assert.Contains("less 1 carrying a placeholder", set.Against(["winwright fixture"]).Sentence());
    }

    [Fact]
    public void What_was_left_out_carries_the_line_it_is_declared_on()
    {
        var file = Strings("""
            {
              "labels": {
                "heading": "winwright fixture",
                "profileName": "Profile: {name}"
              }
            }
            """);

        var excluded = Assert.Single(DerivedSet.From("the labels", file, "labels").Excluded);

        Assert.True(excluded.Where.Known);
        Assert.Equal(4, excluded.Where.Line);
        Assert.Contains("Profile: {name}", excluded.ToString());
    }

    [Fact]
    public void A_set_with_the_templated_one_left_out_still_holds_against_the_window()
    {
        var file = Strings("""
            { "labels": { "heading": "winwright fixture", "sub": "statistics", "profileName": "Profile: {name}" } }
            """);

        var set = DerivedSet.From("the labels", file, "labels");

        // The whole reason for excluding rather than refusing: the key stays usable, and the two
        // strings that can be asserted are asserted.
        var compared = set.Against(["winwright fixture", "statistics"]);

        Assert.True(compared.Held);
        Assert.Empty(compared.Missing);
    }

    [Fact]
    public void A_key_whose_strings_all_carry_placeholders_is_refused_rather_than_derived_empty()
    {
        var file = Strings("""
            { "toasts": { "saved": "Saved {0} items", "welcome": "Welcome, {name}" } }
            """);

        var refused = Assert.Throws<UnderivableSetException>(() => DerivedSet.From("the toasts", file, "toasts"));

        // An empty expected set is met by an empty window, which is the hole this type exists to
        // close — and the remedy differs from an empty key, so the sentence differs too.
        Assert.Contains("nothing under 'toasts'", refused.Message);
        Assert.Contains("is a string an exact read could match", refused.Message);
        Assert.Contains("Welcome, {name}", refused.Message);
    }

    [Fact]
    public void A_key_with_nothing_templated_says_nothing_about_placeholders_at_all()
    {
        var file = Strings("""{ "tabs": { "report": "Report", "status": "Status" } }""");

        var set = DerivedSet.From("the tabs", file, "tabs");

        Assert.Empty(set.Excluded);
        Assert.DoesNotContain("placeholder", set.Source);
        Assert.Equal(["Report", "Status"], set.Expected);
    }

    [Fact]
    public void Every_shape_the_label_guard_knows_is_the_shape_this_one_knows()
    {
        // Deliberately the same rule and not a second regular expression to keep in step: the
        // guard was made public at the label's altitude so this has something to call.
        var file = Strings("""
            {
              "mixed": {
                "plain": "Save",
                "dotnet": "Welcome, {0}",
                "named": "Bem-vindo, {name}",
                "printf": "Hello %s",
                "positional": "%1$s said"
              }
            }
            """);

        var set = DerivedSet.From("the mixed", file, "mixed");

        Assert.Equal(["Save"], set.Expected);
        Assert.Equal(4, set.Excluded.Count);
        Assert.All(set.Excluded, one => Assert.True(Labels.CarriesAPlaceholder(one.Value)));
    }

    [Fact]
    public void The_fixture_own_strings_are_the_file_this_was_decided_against()
    {
        // Not a synthesised case: this is the project's own strings file, and the reason the
        // refusal excludes rather than refuses the lot.
        var file = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "Winwright.Fixture", "strings", "strings.en.json");

        Assert.True(File.Exists(Path.GetFullPath(file)), $"the fixture's strings moved: {Path.GetFullPath(file)}");

        var set = DerivedSet.From("the labels", file, "labels");

        Assert.Contains("winwright fixture", set.Expected);
        Assert.DoesNotContain(set.Expected, one => one.Contains('{', StringComparison.Ordinal));
        Assert.Equal("labels.profileName", Assert.Single(set.Templated).Key);

        // WW139: and the two notes beside it, which this file writes the way JSON makes you.
        Assert.Equal(["labels.//", "labels.//2"], set.Notes.Select(one => one.Key));
    }
}

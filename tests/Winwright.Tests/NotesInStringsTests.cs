using Winwright.Asserting;
using Winwright.Projects;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW139. Read off a failing assertion while WW118 was being shipped. JSON has no comments, so a
/// strings file that wants one writes a key nobody reads — the convention is a key named <c>//</c>,
/// and this repository's own fixture uses it twice. The derivation took every string under the key,
/// so both notes joined the expectation, and the set demanded that a window somewhere display the
/// sentence "The pathological key. An exact-name read can never match this".
/// <para>
/// It is the founding defect pointing the other way. A set with two members nothing can ever read
/// is red on every run for a reason that has nothing to do with the application, and a red nobody
/// can fix is a red people learn to ignore — which is how the green that covers a missing tab
/// header gets shipped next.
/// </para>
/// </summary>
public sealed class NotesInStringsTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-notes-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private string Strings(string json)
    {
        var path = Path.Combine(root, "strings.en.json");
        File.WriteAllText(path, json);
        return path;
    }

    [Theory]
    [InlineData("//")]
    [InlineData("//2")]
    [InlineData("_comment")]
    [InlineData("$comment")]
    [InlineData("$COMMENT")]
    public void Every_spelling_of_the_convention_is_a_note_and_not_a_string(string note)
    {
        var file = Strings($$"""
            { "tabs": { "report": "Report", {{System.Text.Json.JsonSerializer.Serialize(note)}}: "why report is first" } }
            """);

        var set = DerivedSet.From("the tabs", file, "tabs");

        Assert.Equal(["Report"], set.Expected);
        Assert.Equal($"tabs.{note}", Assert.Single(set.Notes).Key);
    }

    [Fact]
    public void A_key_that_merely_starts_with_a_slash_is_still_a_note()
    {
        // "//2" is how a file carrying two of them writes the second, and a rule matching only the
        // bare "//" would take that one as something a window shows.
        var file = Strings("""
            { "tabs": { "//": "one", "//2": "two", "//and so on": "three", "report": "Report" } }
            """);

        var set = DerivedSet.From("the tabs", file, "tabs");

        Assert.Equal(["Report"], set.Expected);
        Assert.Equal(3, set.Notes.Count);
    }

    [Fact]
    public void An_ordinary_key_is_not_mistaken_for_one()
    {
        // The rule must not widen: a project naming a string "comment" or "commentary" is naming a
        // string, and taking it for a note would drop a member the window really does show.
        var file = Strings("""
            { "labels": { "comment": "Comment", "commentary": "Commentary", "//": "not a label" } }
            """);

        var set = DerivedSet.From("the labels", file, "labels");

        Assert.Equal(["Comment", "Commentary"], set.Expected);
        Assert.Equal("labels.//", Assert.Single(set.Notes).Key);
    }

    [Fact]
    public void What_was_left_out_is_named_in_the_source_every_verdict_prints()
    {
        var file = Strings("""
            { "tabs": { "//": "why report is first", "report": "Report", "greeting": "Hello {name}" } }
            """);

        var set = DerivedSet.From("the tabs", file, "tabs");

        // WW118 established that what a derivation leaves out is said in the source rather than in
        // a rule nobody sees the effect of, and the two reasons are told apart there.
        Assert.Contains("1 carrying a placeholder ('tabs.greeting')", set.Source);
        Assert.Contains("1 a note and not a string ('tabs.//')", set.Source);
        Assert.Contains("a note and not a string", set.Against(["Report"]).Sentence());
    }

    [Fact]
    public void A_note_carries_the_line_it_sits_on_like_anything_else_left_out()
    {
        var file = Strings("""
            {
              "tabs": {
                "//": "why report is first",
                "report": "Report"
              }
            }
            """);

        var note = Assert.Single(DerivedSet.From("the tabs", file, "tabs").Notes);

        Assert.True(note.Where.Known);
        Assert.Equal(3, note.Where.Line);
        Assert.Equal(LeftOutBecause.IsANote, note.Why);
    }

    [Fact]
    public void A_key_holding_only_notes_is_refused_rather_than_derived_empty()
    {
        // The same rule an all-placeholder key already had: an empty expected set is met by an
        // empty window, and that is the hole this type exists to close.
        var file = Strings("""{ "tabs": { "//": "nothing here yet", "//2": "still nothing" } }""");

        var refused = Assert.Throws<UnderivableSetException>(() => DerivedSet.From("the tabs", file, "tabs"));

        Assert.Contains("nothing under 'tabs'", refused.Message);
        Assert.Contains("nothing here yet", refused.Message);
    }

    [Fact]
    public void A_commented_file_derives_the_same_set_the_uncommented_one_does()
    {
        // The claim in the task's own words, as an equality rather than as a count.
        var plain = DerivedSet.From(
            "the tabs",
            Strings("""{ "tabs": { "report": "Report", "status": "Status" } }"""),
            "tabs");

        var commented = DerivedSet.From(
            "the tabs",
            Strings("""
                { "tabs": { "//": "the order matters", "report": "Report", "_comment": "so does this", "status": "Status" } }
                """),
            "tabs");

        Assert.Equal(plain.Expected, commented.Expected);
        Assert.Equal(plain.Keys, commented.Keys);
    }

    [Fact]
    public void The_label_reader_does_not_have_the_same_hole()
    {
        // The section asked. It does not: a label is asked for by key, so nobody arrives at a note
        // by accident — the derivation's hazard was taking every key under a prefix, and asking for
        // "labels.//" is asking for that note on purpose.
        var file = Strings("""{ "labels": { "//": "a note", "heading": "winwright" } }""");
        var project = Path.Combine(root, "winwright.json");
        File.WriteAllText(
            project,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Environment.ProcessPath)}},
              "languageFiles": [{{System.Text.Json.JsonSerializer.Serialize(file)}}],
              "language": { "fallback": "en" }
            }
            """);

        var declaration = ProjectDeclaration.Load(project);

        Assert.Equal("winwright", Labels.For("labels.heading", declaration).Text);
        Assert.Equal("a note", Labels.For("labels.//", declaration).Text);
    }
}

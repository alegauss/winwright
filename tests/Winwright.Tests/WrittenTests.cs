using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW391. What a case wrote, as the thing a step is built from — and the door that used to be
/// twenty-nine parameters. What is asserted here is the holding: a name the schema does not have,
/// and a name it says holds something else, are the caller being wrong about the format, which is a
/// harness error and never a refusal about somebody's file.
/// </summary>
public sealed class WrittenTests
{
    [Fact]
    public void A_name_the_format_does_not_have_is_refused_where_it_is_written()
    {
        // The half a misspelling needs. `expects` beside `expect` is a check the author wrote and
        // the run never made, and a bag that shrugged at it would hand that green back — the loader
        // refuses one at the file's own address, and this refuses one nothing loaded.
        var wrong = Assert.Throws<InvalidOperationException>(
            () => Written.Of(("expects", "Saved")));

        Assert.Contains("no 'expects'", wrong.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_field_written_as_the_wrong_kind_is_refused_against_what_the_schema_says()
    {
        // WW66's rule reaching the other door. The schema is what says a field is text or a flag,
        // and a caller that could write one as the other would be building a step the loader could
        // never produce.
        var wrong = Assert.Throws<InvalidOperationException>(
            () => Written.Of(("locator", "Button"), ("act", "invoke"), ("moves", "yes")));

        Assert.Contains("'moves'", wrong.Message, StringComparison.Ordinal);
        Assert.Contains("Truth", wrong.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_field_written_twice_is_refused_rather_than_silently_taking_one_of_them()
    {
        var wrong = Assert.Throws<InvalidOperationException>(
            () => Written.Of(("expect", "Saved"), ("expect", "Saving")));

        Assert.Contains("written twice", wrong.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Nothing_written_and_something_written_as_null_are_the_same_absence()
    {
        var wrote = Written.Of(("expect", null), ("moves", null));

        Assert.Null(wrote.Text("expect"));
        Assert.Null(wrote.Trimmed("expect"));
        Assert.False(wrote.Truth("moves"));
        Assert.Null(Written.None.Text("named"));
    }

    [Fact]
    public void A_field_the_shape_declares_and_nothing_reads_is_refused_naming_it()
    {
        // WW435, and the half that makes the walk a gate. The loader reads every row the schema
        // declares and hands the values to a door that takes them one by one — so a row added to the
        // schema and not to that door is read, dropped, and never heard of again: a key an author may
        // write, a tool will publish, and the run will ignore. This is the row nobody asked for,
        // named at the first load rather than the first case that trusted it.
        //
        // Over a shape of this case's own, because the two the loader walks are exactly the ones it
        // reads every field of — a made-up row is the only one that can stand for the one somebody
        // adds tomorrow.
        IReadOnlyList<Field> shape =
        [
            new("name", true, Taking.Text, "what it is called", []),
            new("later", false, Taking.Text, "the row added after the loader was written", []),
        ];

        var wrote = new Written(shape, [("name", "a shape"), ("later", "something")]);
        Assert.Equal("a shape", wrote.Text("name"));

        var wrong = Assert.Throws<InvalidOperationException>(() => wrote.Handed("case"));

        Assert.Contains("'later'", wrong.Message, StringComparison.Ordinal);
        Assert.Contains("a case", wrong.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("'name'", wrong.Message, StringComparison.Ordinal);

        // And the other way, or the check above would pass on a shape that refuses everything.
        wrote.Text("later");
        wrote.Handed("case");
    }

    [Fact]
    public void The_kinds_a_case_and_a_fixture_hold_are_read_back_as_what_they_are()
    {
        // WW435. A step's fields are text or a flag; a case carries arrays of words and an array of
        // steps, and a fixture an object of text. Each is asked of the schema before it is answered,
        // so a row saying 'tags' holds text cannot be read here as words.
        var wrote = new Written(
            ScenarioSchema.Case,
            [
                ("name", "a case"),
                ("steps", (IReadOnlyList<StepDeclaration>)[Wrote.Step("Edit", "read", ("expect", "b"))]),
                ("tags", (IReadOnlyList<string>)["smoke"]),
                ("onlyReads", true),
            ]);

        Assert.Equal(["smoke"], wrote.Words("tags"));
        Assert.Empty(wrote.Words("needs"));
        Assert.True(wrote.Truth("onlyReads"));
        Assert.Single(wrote.Shaped<StepDeclaration>("steps"));

        var launched = new Written(
            ScenarioSchema.Fixture,
            [
                ("name", "a fixture"),
                ("variables", (IReadOnlyDictionary<string, string>)new Dictionary<string, string> { ["WW"] = "1" }),
            ]);

        Assert.Equal("1", launched.Pairs("variables")["WW"]);
        Assert.Empty(launched.Words("arguments"));

        // The kind is the schema's to say, at both doors.
        Assert.Throws<InvalidOperationException>(() => wrote.Words("name"));
        Assert.Throws<InvalidOperationException>(() => wrote.Shaped<StepDeclaration>("tags"));
    }

    [Fact]
    public void Text_is_what_the_case_wrote_and_trimmed_is_what_every_rule_reads()
    {
        // Blank is nothing and never the empty string: a field a case left as spaces claimed
        // nothing. Both are here because both are read — a locator parses what was written, and
        // every rule about a claim asks whether anything was.
        var wrote = Written.Of(("expect", "  Saved  "), ("label", "   "));

        Assert.Equal("  Saved  ", wrote.Text("expect"));
        Assert.Equal("Saved", wrote.Trimmed("expect"));
        Assert.Equal("   ", wrote.Text("label"));
        Assert.Null(wrote.Trimmed("label"));
    }
}

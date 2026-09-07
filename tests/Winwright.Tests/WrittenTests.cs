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

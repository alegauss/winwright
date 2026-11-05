using Winwright.Asserting;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW52. A control announcing its glyph codepoint satisfies every check for a non-empty name.
/// <para>
/// claude-tray found two controls carrying empty names while every neighbouring button read fine,
/// because a control derives its name from its own content and both of those had none — one's
/// label was a separate text block, the other's content was a font glyph. The pair is the whole
/// task: told apart they are two different repairs, and printed carelessly they look identical.
/// </para>
/// </summary>
public sealed class NameCheckTests
{
    // U+E711, where Segoe MDL2 keeps its close icon, written as the character itself.
    // Every assertion below compares against the escaped form instead, so this constant
    // is also the check that the escaping is what a report prints and not what a source
    // file happens to hold.
    private const string Glyph = "";

    [Fact]
    public void A_control_announcing_a_font_glyph_is_not_a_named_control()
    {
        var read = Names.Of(Glyph);

        // Non-empty, non-whitespace, and useless. Every check that asked only whether a name was
        // there passed on this one.
        Assert.False(string.IsNullOrWhiteSpace(read.Name));
        Assert.False(read.IsALabel);
        Assert.Equal(Named.Glyph, read.Verdict);
    }

    [Fact]
    public void The_glyph_is_printed_as_an_escape_and_never_as_nothing()
    {
        // The half of the task that is about the report. Printed raw, the worst case in the whole
        // check arrives looking exactly like the empty case it is not.
        Assert.Equal("\\uE711", Names.Of(Glyph).Printable);
        Assert.Contains("\\uE711", Names.Of(Glyph).Sentence("the close button"));
        Assert.Contains("a font glyph and not a label", Names.Of(Glyph).Sentence("the close button"));
    }

    [Fact]
    public void The_two_controls_claude_tray_found_are_told_apart()
    {
        var noContent = Names.Of("");
        var iconContent = Names.Of(Glyph);

        // Both reported empty names to the check that started this. They are two different
        // repairs — one needs a label, the other needs its neighbouring text block associating —
        // and a run that cannot tell them apart sends somebody to the wrong one.
        Assert.Equal(Named.Missing, noContent.Verdict);
        Assert.Equal(Named.Glyph, iconContent.Verdict);
        Assert.NotEqual(noContent.Printable, iconContent.Printable);
        Assert.Contains("separate text block", noContent.Sentence("the pane"));
        Assert.DoesNotContain("separate text block", iconContent.Sentence("the pane"));
    }

    [Fact]
    public void A_name_that_is_the_automation_id_handed_back_is_not_a_label()
    {
        var echoed = Names.Of("btnQuit", "btnQuit");
        var written = Names.Of("Quit", "btnQuit");

        Assert.Equal(Named.EchoesTheId, echoed.Verdict);
        Assert.Contains("its own automation id handed back", echoed.Sentence("the quit button"));
        Assert.True(written.IsALabel);
    }

    [Fact]
    public void A_template_nobody_filled_in_is_reported_as_one()
    {
        var check = Names.Of("Welcome, {0}");

        Assert.Equal(Named.Templated, check.Verdict);
        Assert.Contains("a template nobody filled in", check.Sentence("the greeting"));
    }

    [Theory]
    [InlineData("Quit")]
    [InlineData("Sair")]
    [InlineData("Save as...")]
    [InlineData("100%")]
    [InlineData("保存")]
    [InlineData("🗑 Delete")]
    public void An_ordinary_label_passes_and_prints_as_itself(string name)
    {
        var check = Names.Of(name);

        Assert.True(check.IsALabel, check.Sentence("the control"));
        Assert.Equal(name, check.Printable);
    }

    [Fact]
    public void A_label_with_an_icon_beside_it_is_still_a_label()
    {
        // Every non-space rune has to be a glyph for the name to be one. Refusing "Save" because
        // an icon sits after it would be refusing a name somebody wrote.
        var check = Names.Of(Glyph + " Save");

        Assert.True(check.IsALabel, check.Sentence("the control"));
        Assert.Equal("\\uE711 Save", check.Printable);
    }

    [Fact]
    public void Nothing_and_the_empty_string_are_told_apart_in_print()
    {
        Assert.Equal("(no name)", Names.Printable(null));
        Assert.Equal("", Names.Printable(""));
        Assert.Equal(Named.Missing, Names.Of((string?)null).Verdict);
        Assert.Equal(Named.Missing, Names.Of("   ").Verdict);
    }

    [Fact]
    public void An_astral_private_use_codepoint_escapes_to_its_full_width()
    {
        var astral = char.ConvertFromUtf32(0xF0000);

        Assert.Equal(Named.Glyph, Names.Of(astral).Verdict);
        Assert.Equal("\\U000F0000", Names.Of(astral).Printable);
    }

    [Fact]
    public void A_control_character_is_escaped_rather_than_swallowed()
    {
        Assert.Equal("Save\\u0007", Names.Printable("Save\a"));
    }

    [Fact]
    public void The_result_a_verdict_counts_carries_the_same_sentence()
    {
        var failed = Names.Of(Glyph).AsAssertion("the close button");
        var passed = Names.Of("Close").AsAssertion("the close button");

        Assert.Equal(AssertionOutcome.Failed, failed.Outcome);
        Assert.Equal("the close button", failed.Name);
        Assert.Contains("\\uE711", failed.Detail);
        Assert.Equal(AssertionOutcome.Passed, passed.Outcome);
    }
}

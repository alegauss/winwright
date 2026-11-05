using Winwright.Asserting;
using Winwright.Capturing;
using Winwright.Locating;
using Winwright.Projects;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW196. The pairing in <see cref="RefusalArms" /> is checked against the engine in both directions,
/// at the unit a reader actually meets. WW188 asked this question of one refusal type; this asks it
/// of every refusal that carries an arm, which is what Block K's first criterion has always meant.
/// </summary>
public sealed class RefusalArmTests
{
    [Fact]
    public void Every_arm_the_engine_declares_is_provoked_by_something()
    {
        var paired = RefusalArms.Known.Select(one => one.Named).ToHashSet(StringComparer.Ordinal);

        var missing = RefusalArms.Declared().Where(one => !paired.Contains(one)).ToList();

        Assert.True(
            missing.Count == 0,
            $"{missing.Count} arm(s) are provoked by nobody:{Environment.NewLine}  "
                + string.Join($"{Environment.NewLine}  ", missing));
    }

    [Fact]
    public void Nothing_is_paired_that_the_engine_no_longer_declares()
    {
        var declared = RefusalArms.Declared().ToHashSet(StringComparer.Ordinal);

        var gone = RefusalArms.Known.Where(one => !declared.Contains(one.Named)).ToList();

        Assert.True(
            gone.Count == 0,
            $"{gone.Count} pairing(s) name an arm the engine no longer has:{Environment.NewLine}  "
                + string.Join($"{Environment.NewLine}  ", gone.Select(one => one.Named)));
    }

    [Fact]
    public void Every_case_named_is_one_this_suite_really_runs()
    {
        // The half WW160 was filed over: a name written down is a claim, and twelve of them were
        // pointing at nothing. Read out of the assembly rather than believed.
        Assert.All(
            RefusalArms.Known,
            one =>
            {
                var found = Provocation.CaseNamed(one.Case);

                Assert.True(found is not null, $"{one.Named} names {one.Case}, which this suite does not have");
                Assert.True(Provocation.IsACase(found!), $"{one.Case} is not a case this suite runs");
            });
    }

    [Fact]
    public void No_arm_is_paired_twice_and_every_reason_says_something()
    {
        var named = RefusalArms.Known.Select(one => one.Named).ToList();

        Assert.Equal(named.Count, named.Distinct(StringComparer.Ordinal).Count());
        Assert.All(RefusalArms.Known, one => Assert.False(string.IsNullOrWhiteSpace(one.Because)));
    }

    [Fact]
    public void The_sweep_finds_every_refusal_that_carries_an_arm_and_not_the_ones_that_do_not()
    {
        // A sweep that found nothing would pass the pairing by arithmetic, so both ends are named.
        var armed = RefusalArms.Armed();

        Assert.Contains(nameof(LocatorSyntaxException), armed, StringComparer.Ordinal);
        Assert.Contains(nameof(WrongCaptureException), armed, StringComparer.Ordinal);
        Assert.Contains(nameof(UnusableLabelException), armed, StringComparer.Ordinal);
        Assert.Contains(nameof(DeclarationMissingException), armed, StringComparer.Ordinal);
        Assert.Contains(nameof(NotActionableException), armed, StringComparer.Ordinal);

        // And a refusal that is genuinely one thing carries no arm and is not swept in.
        Assert.DoesNotContain(nameof(DestructiveEntryException), armed, StringComparer.Ordinal);
    }

    [Fact]
    public void Unsaid_is_left_out_because_nothing_provokes_a_refusal_nobody_described()
    {
        Assert.DoesNotContain(RefusalArms.Declared(), one => one.EndsWith($".{RefusalArms.Unsaid}", StringComparison.Ordinal));

        // And a throw that named no arm really does carry it, in each of the ones WW196 armed.
        Assert.Equal(LocatorFault.Unsaid, new LocatorSyntaxException("Button", 0, "because").Arm);
        Assert.Equal(UnusableLabel.Unsaid, new UnusableLabelException("because").Arm);
        Assert.Equal(MissingDeclaration.Unsaid, new DeclarationMissingException("k", "f", "w").Arm);
    }

    [Fact]
    public void A_refusal_says_which_arm_it_is_rather_than_leaving_it_to_the_sentence()
    {
        // Why this is keyed on the arm. Six of the thirteen locator refusals differ only in the
        // words after the caret, and a case matching a phrase starts matching a different arm the
        // day somebody rewords one.
        Assert.Equal(
            LocatorFault.UnknownControlType,
            Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("Buton#save")).Arm);

        Assert.Equal(
            LocatorFault.IndexBelowOne,
            Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("Button[index=0]")).Arm);

        Assert.Equal(
            LocatorFault.IndexNotANumber,
            Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("Button[index=second]")).Arm);

        Assert.Equal(
            LocatorFault.UnknownKey,
            Assert.Throws<LocatorSyntaxException>(() => Locator.Parse("Button[label=Save]")).Arm);
    }

    [Fact]
    public void The_grammar_carries_thirteen_arms_and_the_declaration_carries_three_of_four_throws()
    {
        // The judgement, written as arithmetic so a later reader meets it as a decision rather than
        // as an accident. Thirteen throw sites of the grammar are thirteen arms; four throw sites of
        // the declaration are three, because a missing key is one refusal carrying different keys.
        Assert.Equal(13, RefusalArms.Declared().Count(one => one.StartsWith($"{nameof(LocatorSyntaxException)}.", StringComparison.Ordinal)));
        Assert.Equal(3, RefusalArms.Declared().Count(one => one.StartsWith($"{nameof(DeclarationMissingException)}.", StringComparison.Ordinal)));
        Assert.Equal(6, RefusalArms.Declared().Count(one => one.StartsWith($"{nameof(UnusableLabelException)}.", StringComparison.Ordinal)));
    }

    [Fact]
    public void The_pairing_reads_as_a_count_and_then_a_line_each()
    {
        var rendered = RefusalArms.Render();

        Assert.Equal(RefusalArms.Known.Count + 1, rendered.Count);
        Assert.StartsWith($"{RefusalArms.Known.Count} arm(s) across ", rendered[0], StringComparison.Ordinal);
        Assert.All(rendered.Skip(1), line => Assert.StartsWith("  ", line, StringComparison.Ordinal));
    }
}

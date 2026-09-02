using System.Text.Json.Nodes;

using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW340. A claim used to be a spelling — a name written into a boolean chain, a name written into
/// a refusal's list, a name written into a schema row — and the twelfth claim added arrived in some
/// of those and not all. The symptom of a miss was never a build error: a step whose claim the chain
/// had not heard of reads as unfalsifiable, so the case carrying it is refused for saying nothing
/// while saying something, and a schema row nobody marked publishes a format that does not mention
/// the rule the run enforces.
/// <para>
/// So this is the both-way catalogue over claims. Here is every field that makes one, spelled as a
/// file spells it and beside a step that makes it. The schema's marks must be exactly these names,
/// and each of these steps must make exactly the one claim its name says — which is the check that
/// catches a claim taught to the engine and not to the format, in either direction.
/// </para>
/// </summary>
public class ClaimsTests
{
    /// <summary>
    /// Every claim a step can make, and the smallest step making it. In the order the format lists
    /// them, so a reader comparing the two reads down both.
    /// </summary>
    private static readonly (string Field, Func<StepDeclaration> Step)[] Known =
    [
        ("expect", () => StepDeclaration.Of("Text", "read", expected: "Overview")),
        // An acting verb, because a step that only reads cannot be what moved the reading.
        ("moves", () => StepDeclaration.Of("Edit", "type", argument: "beta", moves: true)),
        ("answers", () => StepDeclaration.Of("Text", "read", answers: true)),
        ("matches", () => StepDeclaration.Of("Text", "read", reads: "name", matches: @"\d{4}")),
        ("discloses", () => StepDeclaration.Of("TabItem#statusPane", "select", discloses: true)),
        ("sameAs", () => StepDeclaration.Of("Edit", "read", reads: "value", sameAs: "the start", named: "the end")),
        ("unlike", () => StepDeclaration.Of("Edit", "read", reads: "value", unlike: "the stop before", named: "the second")),
        ("sameCountdownAs", () => StepDeclaration.Of("Text#reset", "read", reads: "name", sameCountdownAs: "the first", named: "the second")),
        ("contains", () => StepDeclaration.Of("Text", "read", reads: "name", contains: "the opener", named: "the dialog")),
        ("label", () => StepDeclaration.Of("Text", "read", label: "stats.live.on")),
        ("expectReported", () => StepDeclaration.Of("Text#profile", "read", reads: "name", expectReported: "inUse")),
        ("notLabel", () => StepDeclaration.Of("Text", "read", notLabel: "stats.live.off")),
        ("beginsWithLabel", () => StepDeclaration.Of("Button", "read", beginsWithLabel: "menu.itemChecked")),
        ("absent", () => StepDeclaration.Of("Button#gone", "read", absent: true)),
        ("ownHeader", () => StepDeclaration.Of("Group", "read", ownHeader: true)),
        ("eachSpoken", () => StepDeclaration.Of("Group", "read", eachSpoken: true)),
        ("spoken", () => StepDeclaration.Of("#labelledRow", "read", spoken: true)),
        // No 'reads' beside it: the claim is about the window while the step waited rather than
        // about what the element ends up saying.
        ("never", () => StepDeclaration.Of("Text", "read", never: "labels.stale")),
        ("covers", () => StepDeclaration.Of("Text", "read", covers: "stats.tab")),
        ("coversAtLeast", () => StepDeclaration.Of("Text", "read", coversAtLeast: "stats.tab")),
        ("coversWithin", () => StepDeclaration.Of("MenuItem", "read", coversWithin: "profiles")),
    ];

    [Fact]
    public void The_format_marks_exactly_the_fields_that_make_a_claim()
    {
        Assert.Equal(
            Known.Select(one => one.Field),
            ScenarioSchema.Step.Where(field => field.Claims).Select(field => field.Name));
    }

    [Fact]
    public void Each_of_them_makes_the_one_claim_its_name_says()
    {
        Assert.All(
            Known,
            one =>
            {
                var claim = Assert.Single(one.Step().Claims);

                Assert.Equal(one.Field, claim.Field);
            });
    }

    [Fact]
    public void A_step_making_one_is_checkable_and_a_step_making_none_is_not()
    {
        Assert.All(Known, one => Assert.True(one.Step().Checkable));

        // The other half of the rule, and the half a forgotten claim breaks: a step that only acts
        // is not checkable, so a claim missing from the set is a case refused for saying nothing.
        Assert.False(StepDeclaration.Of("Button", "invoke").Checkable);
        Assert.Empty(StepDeclaration.Of("Button", "invoke").Claims);
    }

    [Fact]
    public void The_fields_beside_a_claim_are_not_claims_themselves()
    {
        // 'reads' says which reading a claim is about and 'named' says what to call the step: both
        // are written beside a claim rather than instead of one. A step carrying only 'named' has
        // claimed nothing and is a navigation — which is allowed, and is why Checkable exists to
        // tell it from a check. A step carrying only 'reads' is the same rule said louder: it took
        // a reading and asked nothing of it, so it is refused where it is written rather than left
        // to the case-level guard.
        var navigating = StepDeclaration.Of("Button", "invoke", named: "the way in");

        Assert.Empty(navigating.Claims);
        Assert.False(navigating.Checkable);

        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text", "read", reads: "value", named: "the field"));

        Assert.Contains("the reading changes nothing", refusal.Because);
    }

    [Fact]
    public void The_two_that_are_checkable_without_one_stay_out_of_the_set()
    {
        // WW258 and WW336. A tray subject and a capture are each a claim the step makes by being
        // one, and neither can collide with a second the way two fields can — so they answer
        // Checkable directly and leave the set to the fields. A set they were in would have to be
        // read as "one of these, unless it is one of those two".
        var tray = StepDeclaration.Of(null, "open tray menu", tray: "winwright under test");
        var capture = StepDeclaration.Of("Edit", "capture", "the field as it opens");

        Assert.True(tray.Checkable);
        Assert.True(capture.Checkable);
        Assert.Empty(tray.Claims);
        Assert.Empty(capture.Claims);
    }

    [Fact]
    public void A_refusal_names_the_claims_out_of_the_same_set()
    {
        // WW323's rule, read out of the list this catalogue is over: the spelling the file used and
        // never the mode the engine folded it into, so what a refusal says to delete is a key the
        // file has.
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text", "read", coversAtLeast: "stats.tab", moves: true));

        Assert.Contains("'coversAtLeast'", refusal.Because);
        Assert.Contains("'moves'", refusal.Because);
        Assert.DoesNotContain("'covers'", refusal.Because);
    }

    [Fact]
    public void A_claim_is_a_field_and_no_longer_a_line_in_the_verb_that_builds_a_step()
    {
        // WW351, and the deletion is the proof. WW340 gave the set one reader and left it built by
        // a hand-written line per claim inside `Of`, over that verb's own parameters — so a claim
        // was a field, a schema row and a line in a block, and the line was the one somebody would
        // forget. A miss has never been a build error: the step reads as unfalsifiable and the case
        // carrying it is refused for saying nothing while saying something.
        //
        // The set is read off the step now, so every one of those lines lives in the property that
        // answers it. Asserted as a position rather than a count: a line that came back inside `Of`
        // would be a claim spelled twice again, whatever the total.
        var source = File.ReadAllLines(
            Path.Combine(Checkout.Engine, "Winwright", "Scenarios", "StepDeclaration.cs"));

        var declaring = Array.FindIndex(
            source, line => Checkout.Code(line).Contains("public static StepDeclaration Of(", StringComparison.Ordinal));

        Assert.True(declaring > 0, "the verb this is about is not in that file any more");

        var spelled = source
            .Select((line, at) => (Line: Checkout.Code(line), At: at))
            .Where(one => one.Line.Contains("Claiming(", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(spelled);
        Assert.All(
            spelled,
            one => Assert.True(
                one.At < declaring,
                $"line {one.At + 1} spells a claim inside Of: {one.Line.Trim()}"));
    }

    [Fact]
    public void A_refusal_names_the_pointing_spelling_the_file_used_and_not_the_fold()
    {
        // WW351's own hazard, guarded. The four pointing spellings are one field on the step plus a
        // mode saying which, and the set is read off those two now — so the fold has to carry the
        // precedence the refusal's naming used, or a step wrong twice over is told to delete a key
        // it never wrote. WW308 wrote that warning about doing the fold too early, and this is the
        // case that would catch it.
        var unlike = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text", "read", reads: "value", unlike: "the stop", moves: true));

        Assert.Contains("'unlike'", unlike.Because);
        Assert.DoesNotContain("'sameAs'", unlike.Because);

        var ticking = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text", "read", reads: "name", sameCountdownAs: "the first", moves: true));

        Assert.Contains("'sameCountdownAs'", ticking.Because);
        Assert.DoesNotContain("'sameAs'", ticking.Because);

        var holding = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text", "read", reads: "name", contains: "the opener", moves: true));

        Assert.Contains("'contains'", holding.Because);
        Assert.DoesNotContain("'sameAs'", holding.Because);
    }

    [Fact]
    public void The_format_says_the_rule_and_not_only_the_fields()
    {
        // An author reading the published format could find the one-claim rule only by writing two
        // claims and being refused. It is a property of the fields, so it is said where they are.
        var lines = ScenarioSchema.Render();

        Assert.Contains(lines, line => line.Contains("makes exactly one of the claims marked below"));
        Assert.Contains(lines, line => line.StartsWith("  moves") && line.Contains("a claim, and a step makes exactly one"));
        Assert.DoesNotContain(lines, line => line.StartsWith("  reads") && line.Contains("a claim,"));
    }

    [Fact]
    public void The_schema_a_tool_is_handed_says_it_too()
    {
        // The reader the rule was most invisible to. An agent writing a case is handed the input
        // schema and never the prose, so a field's description is the only place it can read that
        // 'moves' and 'expect' are alternatives rather than two things it may write together.
        var step = ScenarioSchema.AsJsonSchema()
            ["properties"]![ScenarioSchema.Cases]!["items"]!["properties"]![ScenarioSchema.Steps]!["items"]!;
        var fields = step["properties"]!.AsObject();

        Assert.EndsWith(
            "— a claim, and a step makes exactly one",
            fields["moves"]!["description"]!.GetValue<string>());
        Assert.DoesNotContain("a claim,", fields["reads"]!["description"]!.GetValue<string>());
    }
}

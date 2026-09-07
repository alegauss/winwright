using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW57. A step is fields, and every one of them is judged when it is declared. What is refused
/// here is refused on every machine — an unparseable locator, an act that does not exist, an
/// argument the verb cannot use — which is exactly what tells it apart from a red on one desk.
/// </summary>
public class StepDeclarationTests
{
    [Fact]
    public void A_step_is_a_locator_a_verb_and_what_the_control_should_read()
    {
        var step = Wrote.Step("""Edit[name="Profile"]""", "set value", ("with", "beta"), ("expect", "beta"));

        Assert.Equal("beta", step.Argument);
        Assert.Equal("beta", step.Expected);
        Assert.Equal("set value", step.Verb.Name);
        Assert.Equal("anything", step.Reads.Name);
        Assert.True(step.Checkable);
    }

    [Fact]
    public void A_step_acting_on_nothing_is_refused()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => Wrote.Step("  ", "invoke"));

        Assert.Contains("names nothing", refusal.Because);
    }

    [Fact]
    public void A_locator_that_does_not_parse_is_refused_at_declaration_and_not_at_run_time()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => Wrote.Step("""Button[name=""", "invoke"));

        Assert.Contains("does not parse", refusal.Because);
        Assert.Contains("invoke Button[name=", refusal.Subject);
    }

    [Fact]
    public void An_argument_the_verb_cannot_use_is_refused_and_the_step_is_named()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => Wrote.Step("Button", "invoke", ("with", "beta"), ("named", "press Save")));

        Assert.Equal("press Save", refusal.Subject);
        Assert.Contains("takes nothing", refusal.Because);
    }

    [Fact]
    public void An_argument_the_verb_needs_and_has_not_got_is_refused()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => Wrote.Step("Slider", "set range"));

        Assert.Contains("acts on a number", refusal.Because);
    }

    [Fact]
    public void A_reading_that_does_not_exist_is_refused_with_the_ones_that_do()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => Wrote.Step("CheckBox", "toggle", ("expect", "On"), ("reads", "checked")));

        Assert.Equal("checked", refusal.Subject);
        Assert.Contains("toggle", refusal.Because);
    }

    [Fact]
    public void A_reading_named_with_nothing_expected_of_it_is_refused_as_dead_configuration()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => Wrote.Step("CheckBox", "toggle", ("reads", "toggle")));

        Assert.Contains("expects nothing of it", refusal.Because);
    }

    [Fact]
    public void A_step_may_expect_nothing_at_all_and_then_it_is_an_act_and_not_a_check()
    {
        // Opening a node so a later step can read what it contains is a step. It is not a check,
        // and the run counts it as neither passed nor failed.
        var step = Wrote.Step("TreeItem", "expand");

        Assert.Null(step.Expected);
        Assert.False(step.Checkable);
        Assert.False(step.Retryable);
    }

    [Fact]
    public void A_step_is_retryable_only_where_it_waits_for_something_and_the_verb_survives_repeating()
    {
        Assert.True(Wrote.Step("Edit", "set value", ("with", "beta"), ("expect", "beta")).Retryable);

        // Waiting for a state a second toggle would leave is how a green becomes a red about the
        // opposite state, so the engine gets one attempt whatever the expectation said.
        Assert.False(Wrote.Step("CheckBox", "toggle", ("expect", "On"), ("reads", "toggle")).Retryable);
    }

    [Fact]
    public void A_read_is_never_retried_because_the_wait_already_polled_to_the_deadline()
    {
        // WW213. A second go is the same look taken again for the same answer at three times the
        // cost, and the poll inside the wait is what a read is made of.
        var reading = Wrote.Step("Text#status", "read", ("expect", "Saved"), ("reads", "text"));

        Assert.True(reading.Verb.Repeatable);
        Assert.True(reading.Checkable);
        Assert.False(reading.Retryable);
    }

    [Fact]
    public void A_read_that_expects_nothing_is_refused_because_the_step_would_do_nothing_at_all()
    {
        // An act with no expectation is a navigation a later step is the check for. A read with none
        // touches nothing and claims nothing.
        var refusal = Assert.Throws<ScenarioRefusedException>(() => Wrote.Step("Text#status", "read"));

        Assert.Contains("'read' expects nothing", refusal.Because);
        Assert.Contains("the step does nothing at all", refusal.Because);
    }

    [Fact]
    public void A_read_takes_no_argument_the_way_the_other_wordless_verbs_do_not()
    {
        Assert.Contains(
            "takes nothing",
            Assert.Throws<ScenarioRefusedException>(
                () => Wrote.Step("Text#status", "read", ("with", "Saved"), ("expect", "Saved"))).Because);
    }

    [Fact]
    public void A_step_that_means_a_destructive_entry_says_so_in_a_field_a_reviewer_finds()
    {
        var quitting = Wrote.Step(
            "Button[name=\"Quit\"]",
            "invoke",
            ("expect", "gone"),
            ("reads", "value"),
            ("meansIt", true),
            ("named", "quit the app"));

        Assert.True(quitting.MeansIt);
        Assert.False(Wrote.Step("Button", "invoke").MeansIt);
    }

    [Fact]
    public void A_step_nobody_named_is_named_by_what_it_does()
    {
        var step = Wrote.Step("""CheckBox[name="Wrap lines"]""", "toggle", ("expect", "On"), ("reads", "toggle"));

        Assert.Equal("""toggle CheckBox[name="Wrap lines"]""", step.Name);
        Assert.Contains("→ toggle 'On'", step.ToString());
    }

    [Fact]
    public void A_field_reaches_a_step_by_its_name_and_never_by_its_position()
    {
        // WW352. The constructor took one parameter per field a step can carry — twenty-three, and
        // three nullable strings in a row means a transposed pair compiles. The worst example was in
        // the engine rather than in a test: a tray step was built from twenty-one positional
        // arguments of which most were null or false.
        //
        // Asserted as the names rather than as the absence of a defect, because that is what a
        // reader can check: three the step cannot be without, plus what the case wrote — and none of
        // the four can be transposed with another, because no two of them are the same type. A fifth
        // would be a field that had found its way back into a position.
        var built = typeof(StepDeclaration)
            .GetConstructors(System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public)
            .Where(one => one.GetParameters() is not [{ } only] || only.ParameterType != typeof(StepDeclaration))
            .ToList();

        var declaring = Assert.Single(built);

        Assert.Equal(
            ["name", "verb", "reads", "wrote"],
            declaring.GetParameters().Select(one => one.Name));
    }

    [Fact]
    public void A_field_joins_the_format_in_a_schema_row_and_a_property_and_nowhere_else()
    {
        // WW391, and the deletion is the proof. A field used to join in five places: a property, a
        // parameter on `Of`, a line in the construction under it, a schema row, and a read plus an
        // argument in the loader. Four of those said what the schema row had already said, and
        // nothing but a case held them together — so the one somebody forgot was a key that loaded
        // and did nothing, which is the failure this format exists to refuse.
        //
        // The two that are left are held to each other by the read between them: a property asks the
        // schema for its field before it answers, so a name the schema does not have is a harness
        // error on the first load rather than a field that quietly answers null forever.
        Assert.Equal(
            ["wrote"],
            typeof(StepDeclaration).GetMethod(nameof(StepDeclaration.Of))!
                .GetParameters()
                .Select(one => one.Name));

        // The loader names no field of a step. It walks the schema, which is the list that says what
        // the names are, so a row added to it is read without this file being told.
        var naming = File.ReadAllLines(Checkout.At("src", "Winwright", "Scenarios", "ScenarioFile.cs"))
            .Select((line, at) => (Line: Checkout.Code(line), At: at))
            .Where(one => one.Line.Contains("ScenarioSchema.Step, \"", StringComparison.Ordinal))
            .ToList();

        Assert.All(
            naming,
            one => Assert.Fail($"line {one.At + 1} reads a step's field by name: {one.Line.Trim()}"));
    }

    [Fact]
    public void What_a_field_is_set_by_is_shut_to_everyone_but_the_verb_that_judges_a_step()
    {
        // The half that keeps the gate. Of is where a step faces its refusals, so a caller outside
        // the engine that could write `step with { Moves = true }` would be holding a step that
        // never faced them — which is the whole of what declaring a case is for.
        //
        // WW391 made it stronger than `private init`: these are reads of what the case wrote, so
        // there is no setter to shut. A setter appearing on one of them is a field that has been
        // copied out of the format and can now disagree with it.
        var fields = new[] { "Moves", "Answers", "Expected", "Sweeps", "Absent", "Label", "Tray" };

        Assert.All(
            fields,
            one =>
            {
                var read = typeof(StepDeclaration).GetProperty(one);

                Assert.True(read is not null, $"{one} is not a property of a step any more");
                Assert.True(read.SetMethod is null, $"{one} is set rather than read off what the case wrote");
            });

        // The three the verb still writes, which are what it parsed rather than what it was given —
        // and every one of them shut to everyone but the verb.
        Assert.All(
            new[] { "Locator", "Matches", "Name" },
            one =>
            {
                var setting = typeof(StepDeclaration).GetProperty(one)?.SetMethod;

                Assert.True(setting is not null, $"{one} is not a property of a step any more");
                Assert.True(setting.IsPrivate, $"{one} can be set from outside the engine");
            });
    }
}

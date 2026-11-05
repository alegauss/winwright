using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW167. The catalogue in <see cref="Rendered" /> is checked against the engine in both directions,
/// which is the whole of it: a list somebody maintains by hand is the promise that let two unasserted
/// renderings ship. Reading the assembly makes the count arithmetic instead.
/// </summary>
public sealed class RenderedTests
{
    [Fact]
    public void Every_rendering_the_engine_answers_is_in_the_catalogue()
    {
        var answered = Rendered.Named();
        var listed = Rendered.Known.Select(one => one.Named).ToList();

        var missing = answered.Except(listed, StringComparer.Ordinal).ToList();

        Assert.True(
            missing.Count == 0,
            $"the engine answers {missing.Count} rendering(s) nothing pairs with a case: "
                + string.Join(", ", missing));
    }

    [Fact]
    public void Nothing_is_catalogued_that_the_engine_no_longer_answers()
    {
        var answered = Rendered.Named();

        var gone = Rendered.Known.Select(one => one.Named).Except(answered, StringComparer.Ordinal).ToList();

        Assert.True(
            gone.Count == 0,
            $"the catalogue names {gone.Count} rendering(s) the engine no longer answers: "
                + string.Join(", ", gone));
    }

    [Fact]
    public void No_rendering_is_paired_twice()
    {
        var listed = Rendered.Known.Select(one => one.Named).ToList();

        Assert.Equal(listed.Count, listed.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Every_pairing_names_a_case_or_says_why_it_cannot()
    {
        // Exactly one of the two, never both and never neither. An entry carrying a case and a
        // reason is one whose reason nobody will delete when the case is written.
        Assert.All(
            Rendered.Known,
            one => Assert.True(
                one.ReadBack ^ (one.Why is not null),
                $"{one.Named} names {(one.ReadBack ? "a case and a reason it has none" : "neither a case nor a reason")}"));
    }

    [Fact]
    public void Every_pairing_says_what_a_reader_gets_out_of_it()
    {
        // A pairing whose text is a restatement of the method name tells a reader nothing about
        // what the rendering is for, which is the question the catalogue is read to answer.
        Assert.All(
            Rendered.Known,
            one =>
            {
                Assert.False(string.IsNullOrWhiteSpace(one.Because), $"{one.Named} says nothing about what it renders");
                Assert.DoesNotContain(one.Named, one.Because, StringComparison.Ordinal);
            });
    }

    [Fact]
    public void The_case_a_rendering_names_is_one_this_suite_really_runs()
    {
        // Resolved out of the test assembly by name and confirmed to carry a Fact or a Theory, so a
        // renamed case is a red here rather than a pairing that quietly stopped meaning anything.
        Assert.All(
            Rendered.Known.Where(one => one.ReadBack),
            one =>
            {
                var found = Provocation.CaseNamed(one.Case);

                Assert.True(found is not null, $"{one.Named} names {one.Case}, which this suite does not have");
                Assert.True(Provocation.IsACase(found!), $"{one.Case} is not a case this suite runs");
            });
    }

    [Fact]
    public void What_nothing_reads_back_is_counted_rather_than_left_off()
    {
        // The one finding this task was filed over, kept as a number a reader sees. It is allowed to
        // be above zero — a rendering added tomorrow starts here — and it is never allowed to be a
        // rendering that is simply absent from the catalogue.
        var unpaired = Rendered.Unpaired();

        Assert.All(unpaired, one => Assert.NotNull(one.Why));
        Assert.Contains($"{unpaired.Count} not yet", Rendered.Render()[0], StringComparison.Ordinal);
    }

    [Fact]
    public void The_catalogue_reads_as_a_count_and_then_a_line_each()
    {
        var rendered = Rendered.Render();

        Assert.Equal(Rendered.Known.Count + 1, rendered.Count);
        Assert.StartsWith($"{Rendered.Known.Count} rendering(s): ", rendered[0], StringComparison.Ordinal);
        Assert.All(rendered.Skip(1), line => Assert.StartsWith("  ", line, StringComparison.Ordinal));
    }
}

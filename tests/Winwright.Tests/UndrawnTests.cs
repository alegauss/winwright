using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW409. The pairing in <see cref="Undrawn" />, read against the built fixture in both directions
/// and against this assembly for the case each row names.
/// </summary>
public sealed class UndrawnTests
{
    [Fact]
    public void Every_shape_that_draws_nothing_says_what_drives_it_instead()
    {
        var paired = Undrawn.Known.Select(one => one.Flag).ToHashSet(StringComparer.Ordinal);

        var loose = Undrawn.Declared().Where(one => !paired.Contains(one)).Order(StringComparer.Ordinal).ToList();

        Assert.True(
            loose.Count == 0,
            $"{loose.Count} shape(s) draw nothing, so the run that drives every shape steps over them, "
                + $"and nothing here says what does: --{string.Join(", --", loose)}");
    }

    [Fact]
    public void Nothing_is_paired_that_the_fixture_no_longer_calls_quiet()
    {
        var quiet = Undrawn.Declared();

        var gone = Undrawn.Known.Where(one => !quiet.Contains(one.Flag)).Select(one => one.Flag).ToList();

        Assert.True(
            gone.Count == 0,
            $"{gone.Count} shape(s) are paired here and the fixture no longer marks them as drawing "
                + $"nothing, so each is now driven twice or named wrong: --{string.Join(", --", gone)}");
    }

    [Fact]
    public void The_fixture_really_was_asked_and_not_merely_launched()
    {
        // The control. Both checks above pass on an empty reading, and a catalogue that stopped
        // parsing would take this whole pairing with it — which is the failure WW392 met from the
        // other side, when the qualifier it needed turned out to be the thing nothing read.
        var quiet = Undrawn.Declared();

        Assert.True(quiet.Count > 5, $"only {quiet.Count} shape(s) came back marked as drawing nothing");
        Assert.Contains("render", quiet);
        Assert.DoesNotContain("show", quiet);
    }

    [Fact]
    public void The_case_a_shape_names_is_one_this_suite_really_runs()
    {
        // Resolved out of the assembly and confirmed to carry a Fact, the way a criterion's is. A
        // renamed case is red here rather than a row that quietly stopped pointing at anything.
        Assert.All(
            Undrawn.Known,
            one =>
            {
                var found = Provocation.CaseNamed(one.Shown);

                Assert.True(found is not null, $"--{one.Flag} names {one.Shown}, which this suite does not have");
                Assert.True(Provocation.IsACase(found!), $"{one.Shown} is not a case this suite runs");
            });
    }

    [Fact]
    public void Every_pairing_says_what_the_shape_produces_rather_than_naming_it_again()
    {
        // A sentence about what a run gets, and not the flag restated. Held as a shape rather than
        // as a ban on the flag's own word, which is what this first was and what `--profile` was
        // right to fail: the word is the subject there, and a rule forbidding it would have been a
        // rule about prose wearing the clothes of a rule about coverage.
        Assert.All(
            Undrawn.Known,
            one =>
            {
                Assert.False(string.IsNullOrWhiteSpace(one.Does), $"--{one.Flag} says nothing about what it does");
                Assert.NotEqual(one.Flag, one.Does.Trim(), StringComparer.OrdinalIgnoreCase);
                Assert.True(
                    one.Does.Count(letter => letter == ' ') >= 3,
                    $"--{one.Flag} says '{one.Does}', which is a label rather than what a run gets");
            });
    }

    [Fact]
    public void No_shape_is_paired_twice()
    {
        var flags = Undrawn.Known.Select(one => one.Flag).ToList();

        Assert.Equal(flags.Count, flags.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void The_two_halves_of_the_catalogue_account_for_every_flag_between_them()
    {
        // The split is exhaustive or one of the two rules covers less than it reads as covering,
        // which is the whole complaint WW409 was filed about said once more about itself.
        Assert.Equal(
            Surfaces.Declared().Count,
            Surfaces.Drawing().Count + Undrawn.Declared().Count);
    }
}

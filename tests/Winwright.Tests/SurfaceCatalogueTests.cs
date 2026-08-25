using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW212. The link between a shape and the reason that justifies it — see <see cref="Surfaces" />
/// for why this is the narrow thing left after the premise turned out to be wrong.
/// </summary>
public sealed class SurfaceCatalogueTests
{
    [Fact]
    public void Every_type_the_fixture_carries_is_in_the_catalogue()
    {
        // The check the criterion was missing at this end: a pane added later is red here until
        // somebody has said which flag reaches it, and a flag is where the reason lives.
        var listed = Surfaces.Known.Select(one => one.Named).ToList();

        Assert.Empty(Surfaces.Carried().Except(listed, StringComparer.Ordinal));
    }

    [Fact]
    public void Nothing_is_catalogued_that_the_fixture_no_longer_carries()
    {
        var listed = Surfaces.Known.Select(one => one.Named).ToList();

        Assert.Empty(listed.Except(Surfaces.Carried(), StringComparer.Ordinal));
    }

    [Fact]
    public void No_type_is_catalogued_twice()
    {
        var listed = Surfaces.Known.Select(one => one.Named).ToList();

        Assert.Equal(listed.Count, listed.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void The_sweep_finds_more_than_a_handful_and_the_panes_among_them()
    {
        // A sweep that found nothing would pass both directions above by arithmetic.
        var carried = Surfaces.Carried();

        Assert.True(carried.Count > 10, $"only {carried.Count} type(s) were swept");
        Assert.Contains("NamesPane", carried, StringComparer.Ordinal);
        Assert.Contains("MainWindow", carried, StringComparer.Ordinal);
    }

    [Fact]
    public void Every_shape_names_a_flag_the_fixture_actually_declares()
    {
        // The link itself, and the whole of what this task is. The reason is on the flag; a shape
        // naming a flag nobody declares is a shape justified by nothing, whatever it says.
        var declared = Surfaces.Declared();

        Assert.True(declared.Count > 10, $"only {declared.Count} flag(s) were read from the fixture");

        Assert.All(
            Surfaces.Known.Where(one => one.Kind != Carrying.ThePlumbing),
            one => Assert.True(
                declared.ContainsKey(one.Flag),
                $"{one.Named} names --{one.Flag}, which this fixture does not declare"));
    }

    [Fact]
    public void Every_shape_is_reached_from_code_that_tests_its_flag()
    {
        // Named and then checked, rather than named and believed. An entry that says a pane is
        // reached through a flag nothing tests near it is an entry that has stopped being true.
        var gating = Surfaces.Gating();

        Assert.All(
            Surfaces.Known.Where(one => one.Kind == Carrying.AShape),
            one => Assert.True(
                gating[one.Named].Contains(one.Flag, StringComparer.Ordinal),
                $"{one.Named} is catalogued behind --{one.Flag}, and the code that reaches it tests "
                    + $"{(gating[one.Named].Count == 0 ? "no flag at all" : string.Join(", ", gating[one.Named]))}"));
    }

    [Fact]
    public void A_default_route_is_reached_from_the_file_its_flag_opens()
    {
        // The one widening, checked rather than waved through. FixedPane is what --render draws
        // where neither awkward shape was asked for, so the flag is tested in the same file and not
        // in the same member — and that is the whole of the exemption.
        var inFile = Surfaces.GatingInFile();
        var narrow = Surfaces.Gating();

        var defaults = Surfaces.Known.Where(one => one.Kind == Carrying.TheDefaultRoute).ToList();

        Assert.NotEmpty(defaults);
        Assert.All(
            defaults,
            one =>
            {
                Assert.Contains(one.Flag, inFile[one.Named], StringComparer.Ordinal);

                // And it really needed the widening: an arm claimed where the narrow rule already
                // holds is an exemption nobody is paying for, and would hide the next one.
                Assert.DoesNotContain(one.Flag, narrow[one.Named], StringComparer.Ordinal);
            });

        // Rare on purpose. The day this is most of the fixture, the rule has become the exemption.
        Assert.True(
            defaults.Count * 4 < Surfaces.Known.Count,
            $"{defaults.Count} of {Surfaces.Known.Count} types are exempted as a default route");
    }

    [Fact]
    public void The_plumbing_is_not_reached_through_a_flag_and_says_why()
    {
        // The other half, so the exemption cannot be used to park a shape. Plumbing carries no flag
        // and owes a sentence instead, and the sentence has to be one somebody wrote.
        Assert.All(
            Surfaces.Known.Where(one => one.Kind == Carrying.ThePlumbing),
            one =>
            {
                Assert.Equal("", one.Flag);
                Assert.True(one.Because.Length > 40, $"{one.Named} is exempted in {one.Because.Length} characters");
            });

        // And it is a handful rather than most of the fixture, which is what would make the rule
        // hold by exempting everything it is meant to catch.
        Assert.True(
            Surfaces.Known.Count(one => one.Kind == Carrying.AShape)
                > Surfaces.Known.Count(one => one.Kind == Carrying.ThePlumbing),
            "more of the fixture is exempted than is catalogued as a shape");
    }

    [Fact]
    public void The_reason_a_shape_inherits_is_the_one_the_built_fixture_prints()
    {
        // Joined all the way through, which is the claim the criterion actually makes: the class is
        // reached through the flag, the flag carries the reason, and the reason is the one a person
        // reads out of --flags rather than one this suite keeps a second copy of.
        var declared = Surfaces.Declared();

        foreach (var shape in Surfaces.Known.Where(one => one.Kind == Carrying.AShape))
        {
            Assert.False(
                string.IsNullOrWhiteSpace(declared[shape.Flag]),
                $"--{shape.Flag} reaches {shape.Named} and says nothing about why it is here");

            // Long enough to have said what happened, which is the bar the reason beside it already
            // has to clear — a shape inheriting a sentence nobody could act on inherits nothing.
            Assert.True(
                declared[shape.Flag].Length > 60,
                $"--{shape.Flag} reaches {shape.Named} and justifies it in "
                    + $"{declared[shape.Flag].Length} characters");
        }
    }

    [Fact]
    public void The_catalogue_reads_as_counts_and_then_a_line_each()
    {
        var rendered = Surfaces.Render();

        Assert.Equal(Surfaces.Known.Count + 1, rendered.Count);
        Assert.Contains("type(s) the fixture carries", rendered[0]);
        Assert.Contains("says why it is here", rendered[0]);
        Assert.All(rendered.Skip(1), one => Assert.StartsWith("  ", one));
    }
}

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW212. The link between a shape and the reason that justifies it — see <see cref="Surfaces" />
/// for why this is the narrow thing left after the premise turned out to be wrong.
/// </summary>
public sealed class SurfaceCatalogueTests
{
    [Fact]
    public void Every_place_a_pane_joins_the_fixture_agrees_with_the_others()
    {
        // WW392. A pane joins in five places and the only way to learn which was to add one and
        // read the reds — WW379 did, and each red arrived a run apart. Two of them were separate
        // cases here, one was a case below, and the fifth had nothing holding it at all: the value
        // a free-text flag needs where this suite drives every shape, which is found by a guest run
        // refusing `--store` for want of one.
        //
        // So they are one check, and the failure is the list. What a person adding a pane needs is
        // not which of five they missed first but all of them at once, and a case per place cannot
        // give that however many of them there are.
        var listed = Surfaces.Known.Select(one => one.Named).ToList();
        var declared = Surfaces.Declared();
        var gating = Surfaces.Gating();
        var valued = Valued();

        var missing = new List<string>();

        // The type is there and nothing says which flag reaches it.
        missing.AddRange(Surfaces.Carried()
            .Except(listed, StringComparer.Ordinal)
            .Select(one => $"{one} is a type the fixture carries and Surfaces.Known has no row for it"));

        // And the other way, which is the row that outlived what it described.
        missing.AddRange(listed
            .Except(Surfaces.Carried(), StringComparer.Ordinal)
            .Select(one => $"Surfaces.Known has a row for {one}, which the fixture no longer carries"));

        foreach (var shape in Surfaces.Known.Where(one => one.Kind != Carrying.ThePlumbing))
        {
            // The reason lives on the flag, so a shape naming one nobody declares is justified by
            // nothing whatever its row says.
            if (!declared.ContainsKey(shape.Flag))
            {
                missing.Add($"{shape.Named} names --{shape.Flag}, which this fixture does not declare");
                continue;
            }

            // The line in the window: catalogued behind a flag the code reaching it does not test.
            // The default route is exempt and checked on its own terms below.
            if (shape.Kind == Carrying.AShape && !gating[shape.Named].Contains(shape.Flag, StringComparer.Ordinal))
            {
                missing.Add(
                    $"{shape.Named} is catalogued behind --{shape.Flag}, and the code reaching it tests "
                        + $"{(gating[shape.Named].Count == 0 ? "no flag at all" : string.Join(", ", gating[shape.Named]))}");
            }
        }

        // The fifth, and the one that used to cost a guest run: a flag taking free text needs a
        // value written where every shape is driven. Three things narrow it, and the last was found
        // by this check going red rather than by anybody knowing — a flag listing its choices
        // supplies its own because the catalogue prints them, one already written needs nothing,
        // and one that draws nothing is never driven, which is what `--render` is.
        var drawing = Surfaces.Drawing();
        foreach (var (flag, takes) in Surfaces.Taking())
        {
            if (takes.Length == 0
                || takes.Contains('|', StringComparison.Ordinal)
                || valued.Contains(flag)
                || !drawing.Contains(flag))
            {
                continue;
            }

            missing.Add(
                $"--{flag} takes <{takes}>, draws a window, and FixtureTests.Value has no arm for it — "
                    + "so the run that drives every shape passes it with no value and the fixture refuses");
        }

        Assert.True(
            missing.Count == 0,
            "a pane joins this fixture in five places — its own file, a Flags.Known row, a line in "
                + "MainWindow reached behind that flag, a Surfaces.Known row, and a value in "
                + $"FixtureTests.Value where the flag takes free text. These do not agree:"
                + $"{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", missing)}");
    }

    /// <summary>
    /// The flags the suite has a value written for, read out of the switch that writes them. WW392.
    /// <para>
    /// Off the source and not by calling it: the member is private to the class that drives every
    /// shape, and making it visible to be checked would be the check changing what it checks. The
    /// arms are string literals in a switch, which is a shape a line can be read from and a shape
    /// that goes red here if somebody writes it another way — which is the right way round, because
    /// this list is only worth anything while it is the one the driver uses.
    /// </para>
    /// </summary>
    private static IReadOnlySet<string> Valued()
    {
        var source = File.ReadAllText(Checkout.At("tests", "Winwright.Tests", "FixtureTests.cs"));
        var switching = source.IndexOf("private string Value(string name) => name switch", StringComparison.Ordinal);

        Assert.True(switching > 0, "FixtureTests no longer has the switch that supplies a flag's value");

        var body = source[switching..];
        var ends = body.IndexOf("};", StringComparison.Ordinal);
        Assert.True(ends > 0, "the value switch was found and its end was not");

        return System.Text.RegularExpressions.Regex
            .Matches(body[..ends], "\"([a-z]+)\" =>")
            .Select(one => one.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
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
    public void The_readings_the_joined_check_rests_on_all_found_something()
    {
        // The control the four folded cases each carried a piece of. Every one of the joined
        // comparisons is an `Except` or a `Contains`, and all of them pass against nothing at all —
        // so a reading that stopped finding anything would turn the check above into a green about
        // an empty set, which is the shape this project refuses everywhere else.
        var declared = Surfaces.Declared();

        Assert.True(declared.Count > 10, $"only {declared.Count} flag(s) were read from the fixture");
        Assert.True(Surfaces.Gating().Count > 10, "the gating reading found almost nothing");
        Assert.Contains(Surfaces.Taking(), one => one.Value.Length > 0);

        // Both ways on the drawing reading, because either half being empty makes the value rule
        // vacuous: nothing drawing skips every flag, and everything drawing would have caught
        // `--render` — which is the case that put this line here.
        Assert.Contains("store", Surfaces.Drawing(), StringComparer.Ordinal);
        Assert.DoesNotContain("render", Surfaces.Drawing(), StringComparer.Ordinal);

        // And the value switch, which is the reading WW392 added: it is read out of a source file
        // by shape, so it is the one most able to quietly stop matching.
        Assert.Contains("store", Valued(), StringComparer.Ordinal);
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

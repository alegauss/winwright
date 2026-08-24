using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW176. The catalogue in <see cref="Criteria" /> is checked against the roadmap in both
/// directions. A criterion added later is red here until somebody says what demonstrates it or why
/// nothing does — which is the question this project answers before calling a block finished, asked
/// by a case instead of by whoever last remembered to ask it.
/// </summary>
public sealed class CriteriaTests
{
    [Fact]
    public void Every_criterion_the_roadmap_declares_is_in_the_catalogue()
    {
        var listed = Criteria.Known.Select(one => (one.Block, one.Lead)).ToHashSet();

        var missing = Criteria.Declared().Where(one => !listed.Contains(one)).ToList();

        Assert.True(
            missing.Count == 0,
            $"the roadmap declares {missing.Count} criterion(s) nothing here says anything about: "
                + string.Join("; ", missing.Select(one => $"{one.Block} {one.Lead}")));
    }

    [Fact]
    public void Nothing_is_catalogued_that_the_roadmap_no_longer_declares()
    {
        var declared = Criteria.Declared().ToHashSet();

        var gone = Criteria.Known.Where(one => !declared.Contains((one.Block, one.Lead))).ToList();

        Assert.True(
            gone.Count == 0,
            $"{gone.Count} criterion(s) here are not in the roadmap, so a lead has moved or been "
                + $"reworded: {string.Join("; ", gone.Select(one => $"{one.Block} {one.Lead}"))}");
    }

    [Fact]
    public void The_roadmap_really_was_read_and_not_merely_opened()
    {
        // The control. Every check above passes trivially against an empty reading, and a parser
        // that quietly stopped matching would take this whole catalogue with it.
        var declared = Criteria.Declared();

        Assert.True(declared.Count > 25, $"only {declared.Count} criterion(s) were read out of the roadmap");
        Assert.Contains(declared, one => one.Block == "A");
        Assert.Contains(declared, one => one.Block == "K");

        // And nothing from the neighbouring list: the non-goals are bullets of the same shape under
        // a different heading, and reading them as criteria would be a count that means nothing.
        Assert.DoesNotContain(declared, one => one.Lead == "Not cross-platform");
    }

    [Fact]
    public void No_criterion_is_paired_twice()
    {
        var listed = Criteria.Known.Select(one => (one.Block, one.Lead)).ToList();

        Assert.Equal(listed.Count, listed.Distinct().Count());
    }

    [Fact]
    public void Every_pairing_names_a_case_or_says_why_nothing_does()
    {
        Assert.All(
            Criteria.Known,
            one => Assert.True(
                one.Demonstrated ^ (one.Why is not null),
                $"{one.Block} '{one.Lead}' names {(one.Demonstrated ? "a case and a reason it has none" : "neither")}"));
    }

    [Fact]
    public void Every_pairing_says_what_it_establishes_or_what_is_missing()
    {
        Assert.All(
            Criteria.Known,
            one =>
            {
                Assert.False(string.IsNullOrWhiteSpace(one.Because), $"{one.Lead} says nothing");
                Assert.DoesNotContain(one.Lead, one.Because, StringComparison.Ordinal);
            });
    }

    [Fact]
    public void The_case_a_criterion_names_is_one_this_suite_really_runs()
    {
        // Resolved out of the test assembly and confirmed to carry a Fact, so a renamed case is red
        // here rather than a pairing that quietly stopped pointing at anything.
        Assert.All(
            Criteria.Known.Where(one => one.Demonstrated),
            one =>
            {
                var found = Provocation.CaseNamed(one.Shown);

                Assert.True(found is not null, $"'{one.Lead}' names {one.Shown}, which this suite does not have");
                Assert.True(Provocation.IsACase(found!), $"{one.Shown} is not a case this suite runs");
            });
    }

    [Fact]
    public void What_nothing_shows_is_counted_and_told_apart_from_what_is_not_built()
    {
        // The two buckets are not the same admission. A criterion about scenario files cannot be
        // demonstrated before scenario files exist; one about a capability that shipped is a debt,
        // and this project has been finding those by hand three blocks running.
        var unproven = Criteria.Unproven();

        Assert.All(unproven, one => Assert.NotNull(one.Why));
        Assert.Contains(unproven, one => one.Why == Unshown.NotBuilt);
        Assert.Contains(unproven, one => one.Why == Unshown.NotYet);

        var owed = unproven.Count(one => one.Why == Unshown.NotYet);
        Assert.Contains($"{owed} built and not read back", Criteria.Render()[0], StringComparison.Ordinal);
    }

    [Fact]
    public void A_debt_names_the_task_that_carries_it_rather_than_being_a_shrug()
    {
        // NotYet means the capability is here and nobody reads the claim back, so there is work to
        // point at. Every one of these was filed as a task by the reading this catalogue replaces.
        Assert.All(
            Criteria.Known.Where(one => one.Why == Unshown.NotYet),
            one => Assert.True(
                one.Because.Contains("WW", StringComparison.Ordinal) || one.Because.Contains("never", StringComparison.Ordinal),
                $"'{one.Lead}' is owed and names neither a task nor what has never been done: {one.Because}"));
    }

    [Fact]
    public void The_catalogue_reads_as_counts_and_then_a_line_each()
    {
        var rendered = Criteria.Render();

        Assert.Equal(Criteria.Known.Count + 1, rendered.Count);
        Assert.StartsWith($"{Criteria.Known.Count} criterion(s): ", rendered[0], StringComparison.Ordinal);
        Assert.All(rendered.Skip(1), line => Assert.StartsWith("  ", line, StringComparison.Ordinal));
    }
}

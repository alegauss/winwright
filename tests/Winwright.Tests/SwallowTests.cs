using System.Reflection;

using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW191. The boundary WW182 drew one step too late. A suite reading that swallows a failure and
/// answers a value has a third state whether or not its type can hold one, and WW181's defect lived
/// entirely on this side of the line: a list of strings, no verdict, and a case asserting the desk
/// was clean on a reading that had never looked.
/// </summary>
public sealed class SwallowTests
{
    [Fact]
    public void Every_reading_that_swallows_and_answers_a_value_says_what_it_answers_when_it_could_not_look()
    {
        var paired = Swallowing.Known.Select(one => one.Named).ToHashSet(StringComparer.Ordinal);

        var silent = Swallowing.Found().Where(one => !paired.Contains(one)).ToList();

        Assert.True(
            silent.Count == 0,
            $"{silent.Count} method(s) catch and answer a value, and nothing says what they report "
                + $"when they could not look:{Environment.NewLine}  "
                + string.Join($"{Environment.NewLine}  ", silent));
    }

    [Fact]
    public void Nothing_is_paired_that_no_longer_swallows()
    {
        var swallowing = Swallowing.Found().ToHashSet(StringComparer.Ordinal);

        var stale = Swallowing.Known.Where(one => !swallowing.Contains(one.Named)).ToList();

        // The half that decays. A catch removed in a repair leaves its reason standing here, and the
        // next reader takes the list for the state of the suite.
        Assert.True(
            stale.Count == 0,
            $"{stale.Count} pairing(s) name a method that no longer catches anything:"
                + $"{Environment.NewLine}  " + string.Join($"{Environment.NewLine}  ", stale.Select(one => one.Named)));
    }

    [Fact]
    public void The_sweep_reads_the_compilers_own_record_and_discriminates()
    {
        // A sweep that found everything or nothing would pass the pairing by arithmetic. Named
        // either way: one method that certainly catches, and one beside it that certainly does not.
        var found = Swallowing.Found();

        Assert.NotEmpty(found);
        Assert.Contains("TrayGhosts.Running", found, StringComparer.Ordinal);
        Assert.DoesNotContain("TrayGhosts.Showing", found, StringComparer.Ordinal);
        Assert.DoesNotContain("Provocation.CaseNamed", found, StringComparer.Ordinal);
    }

    [Fact]
    public void No_method_is_paired_twice_and_every_reason_says_something()
    {
        var named = Swallowing.Known.Select(one => one.Named).ToList();

        Assert.Equal(named.Count, named.Distinct(StringComparer.Ordinal).Count());
        Assert.All(Swallowing.Known, one => Assert.False(string.IsNullOrWhiteSpace(one.Because)));
    }

    [Fact]
    public void A_reading_that_carries_the_third_state_is_the_one_that_says_so()
    {
        // The kind that is not an excuse. BusyDesk.Built answers null for a desk that refused and
        // checks the reading against DeskFacts before it does, which is the third state with a door
        // rather than a value — and it is the only one here, which is the honest count.
        var carried = Swallowing.Known.Where(one => one.Kind == Swallowed.Carried).ToList();

        Assert.Equal(["BusyDesk.Built"], carried.Select(one => one.Named));
    }

    [Fact]
    public void The_rule_that_shipped_would_not_have_reached_the_defect_it_was_written_for()
    {
        // WW191 stated as arithmetic rather than as prose. TrayGhosts.Showing is what WW181 shipped;
        // it answers no verdict, so WW182's sweep cannot see it — and the reading it leans on does
        // catch, so this one can. That gap is the whole task.
        var showing = typeof(TrayGhosts).GetMethod(
            nameof(TrayGhosts.Showing), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(showing);
        Assert.Null(typeof(TrayGhosts).GetMethod("AsAssertion"));
        Assert.Contains("TrayGhosts.Running", Swallowing.Found(), StringComparer.Ordinal);
    }

    [Fact]
    public void A_catch_that_answers_nothing_is_not_this_rule()
    {
        // The boundary, said out loud. A method that catches and returns void has nowhere to report
        // the failure as an answer, so it cannot commit the confusion this is about.
        Assert.All(
            Swallowing.Found(),
            one => Assert.Contains(Swallowing.Known, paired => paired.Named == one));

        Assert.DoesNotContain("WindowFixture.Dispose", Swallowing.Found(), StringComparer.Ordinal);
    }

    [Fact]
    public void The_pairing_reads_as_a_count_and_then_a_line_each()
    {
        var rendered = Swallowing.Render();

        Assert.Equal(Swallowing.Known.Count + 1, rendered.Count);
        Assert.StartsWith(
            $"{Swallowing.Found().Count} method(s) in this suite catch and answer a value, ",
            rendered[0],
            StringComparison.Ordinal);
        Assert.All(rendered.Skip(1), line => Assert.StartsWith("  ", line, StringComparison.Ordinal));
    }
}

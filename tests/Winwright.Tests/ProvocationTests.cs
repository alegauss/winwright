using System.Diagnostics;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW132. Counted while reading this block's own criterion: the framework names nineteen refusals
/// and the fixture reached four of them. The rest are asserted against hand-built windows, against
/// arguments passed in a test, or not at all.
/// <para>
/// Some need no shape and saying so is the point. Four needed one the fixture could not take — a
/// render that lays out to nothing, a capture with no background declared, a picture nothing drew,
/// a receipt about another window — because the fixture always did the right thing. WW146 taught it
/// to do the wrong one on request, and the fourth was moved rather than faked.
/// </para>
/// <para>
/// The durable half is the pairing. The catalogue and the exception types were two lists nobody
/// compared, so a refusal added later started unprovokable and stayed that way. These read both.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ProvocationTests
{
    /// <summary>The built fixture, which is where its catalogue is read from.</summary>
    private static string Executable() => Fixture.Executable();

    [Fact]
    public void Every_refusal_the_framework_names_is_paired_with_something()
    {
        var named = Provocation.Named();
        var paired = Provocation.Known.Select(one => one.Refusal).ToList();

        // The check the criterion was missing: a refusal added later fails here until somebody says
        // which flag reaches it or why none can.
        Assert.Empty(named.Except(paired, StringComparer.Ordinal));
    }

    [Fact]
    public void Nothing_is_paired_that_the_framework_no_longer_names()
    {
        // The other direction, and the one that rots quietly: an entry for a type somebody deleted
        // reads as coverage of a refusal that is not there.
        var named = Provocation.Named();

        Assert.Empty(Provocation.Known.Select(one => one.Refusal).Except(named, StringComparer.Ordinal));
    }

    [Fact]
    public void No_refusal_is_paired_twice()
    {
        var paired = Provocation.Known.Select(one => one.Refusal).ToList();

        Assert.Equal(paired.Count, paired.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void A_flag_named_here_is_one_the_fixture_actually_has()
    {
        // The second pair of lists nobody compared. A flag renamed in the fixture would leave this
        // pairing pointing at a shape nobody can ask for — and the flags are read out of the built
        // fixture rather than a reference to it, so what is checked is what an adopter would run.
        var flags = Provocation.FlagsOf(Executable());

        Assert.NotEmpty(flags);
        foreach (var provoked in Provocation.Reachable())
            Assert.Contains(provoked.Flag, flags);
    }

    [Fact]
    public void Every_refusal_no_flag_reaches_names_the_case_that_provokes_it()
    {
        // WW160. Twelve entries said some version of "a case builds this" and nothing asserted one
        // existed. The suite did contain most of them, which is what made the gap quiet: an entry
        // whose case somebody deleted reads exactly like one whose case still runs.
        Assert.All(
            Provocation.ByACase(),
            one => Assert.False(string.IsNullOrWhiteSpace(one.Case), $"{one.Refusal} names no case"));

        // And the converse, so no entry claims both: one a flag reaches is driven by its own case
        // in ProvokedByFlagTests, and an entry naming two provocations is an entry saying neither.
        Assert.All(
            Provocation.Reachable(),
            one => Assert.True(one.Case.Length == 0, $"{one.Refusal} names a flag and a case"));
    }

    [Fact]
    public void The_case_a_refusal_names_is_one_this_suite_really_runs()
    {
        // Read out of the assembly and never believed. A rename fails here, which is the point:
        // the alternative is a pairing that goes on asserting coverage nothing provides.
        foreach (var provoked in Provocation.ByACase())
        {
            var method = Provocation.CaseNamed(provoked.Case)
                ?? throw new Xunit.Sdk.XunitException(
                    $"{provoked.Refusal} names {provoked.Case}, which this suite has not got");

            Assert.True(
                Provocation.IsACase(method),
                $"{provoked.Refusal} names {provoked.Case}, which is a method the runner never executes");
        }
    }

    [Fact]
    public void A_case_named_by_the_pairing_is_found_by_name_and_not_by_luck()
    {
        // The reading's own control. A check that answered "found" for anything would pass the one
        // above whatever the entries said, which is the shape of a green covering nothing.
        Assert.Null(Provocation.CaseNamed("NoSuchTests.No_such_case"));
        Assert.Null(Provocation.CaseNamed("ProvocationTests.No_such_case"));
        Assert.Null(Provocation.CaseNamed("nodot"));

        // And a method that is not a case is told from one that is: this class has both.
        var real = Provocation.CaseNamed($"{nameof(ProvocationTests)}.{nameof(No_refusal_is_paired_twice)}");
        Assert.NotNull(real);
        Assert.True(Provocation.IsACase(real));
    }

    [Fact]
    public void Every_pairing_says_why_in_a_sentence_somebody_can_act_on()
    {
        Assert.All(Provocation.Known, one => Assert.False(string.IsNullOrWhiteSpace(one.Because)));

        // Exactly one of the two: a flag, or a stated reason there is none. Neither, or both, is
        // an entry that says nothing.
        Assert.All(
            Provocation.Known,
            one => Assert.True(one.ThroughTheFixture ^ (one.Why is not null), one.Refusal));
    }

    [Fact]
    public void What_the_fixture_cannot_provoke_is_counted_rather_than_left_off()
    {
        // The bucket that must not grow, and WW146 emptied it. Filling it again is a decision
        // somebody makes here rather than by adding a refusal nothing can reach: this is the check
        // that makes the empty answer a measurement instead of a list nobody kept up.
        Assert.Empty(Provocation.Unreachable());
        Assert.All(Provocation.Unreachable(), one => Assert.Equal(Without.NotYet, one.Why));
    }

    [Fact]
    public void The_count_a_reader_sees_still_names_the_bucket_that_is_empty()
    {
        // An empty bucket rendered as silence is a reading nobody can distinguish from a reading
        // nobody took, which is the failure this whole pairing is about.
        Assert.Contains("0 needing a shape it cannot take", Provocation.Render()[0]);
    }

    [Fact]
    public void The_fixtures_own_refusal_is_provoked_by_running_it()
    {
        // Not in the pairing and it cannot be: the suite sees the fixture as an executable rather
        // than as an assembly, on purpose. So it is provoked the way an adopter would provoke it.
        var start = new ProcessStartInfo(Executable()) { RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add("--nosuchshape");

        using var running = Process.Start(start)!;
        var said = running.StandardError.ReadToEnd();
        Assert.True(running.WaitForExit(30_000), "the fixture did not exit after being refused");

        Assert.Equal(2, running.ExitCode);
        Assert.Contains("--nosuchshape is not a shape this fixture has", said);
        Assert.Contains("This fixture knows:", said);
    }

    [Fact]
    public void The_pairing_reads_as_a_count_and_then_a_line_each()
    {
        var rendered = Provocation.Render();

        Assert.Equal(Provocation.Known.Count + 1, rendered.Count);
        Assert.StartsWith($"{Provocation.Known.Count} refusals:", rendered[0]);
        Assert.Contains("needing a shape it cannot take", rendered[0]);
        Assert.All(rendered.Skip(1), one => Assert.StartsWith("  ", one));
    }

    [Fact]
    public void A_flag_that_provokes_nothing_named_here_is_still_a_shape_worth_having()
    {
        // Deliberately not the reverse check. Most flags exist to make a reading possible rather
        // than a refusal — a backdrop, an animation, a peerless pane — and demanding a refusal per
        // flag would be a rule that deleted the useful half of the catalogue.
        var provoking = Provocation.Reachable().Select(one => one.Flag).ToHashSet(StringComparer.Ordinal);

        Assert.True(
            provoking.Count < Provocation.FlagsOf(Executable()).Count,
            "every flag provokes a refusal, which is unexpected");
    }
}

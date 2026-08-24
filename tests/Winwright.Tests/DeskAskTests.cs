using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW190. The pairing in <see cref="DeskAsks" /> is checked against the sources in both directions.
/// A case that asks the shell, the foreground or the focus for a verdict and neither excuses the
/// desk nor says why it needs no excuse is red here — which is a run of this suite rather than a
/// run of the whole suite on a desk somebody was using.
/// </summary>
public sealed class DeskAskTests
{
    [Fact]
    public void Every_case_that_asks_and_does_not_excuse_says_why_it_needs_no_excuse()
    {
        var paired = DeskAsks.Known.Select(one => one.Case).ToHashSet(StringComparer.Ordinal);

        var missing = DeskAsks.Bare().Where(one => !paired.Contains(one)).ToList();

        // WW197: which fact, and not only which case. A case that excused one reading and asked
        // about a second is here for the second, and a reader told only the case name would go
        // looking at the excuse that is already there.
        Assert.True(
            missing.Count == 0,
            $"{missing.Count} case(s) ask about a desk fact nothing in them excused, and say nothing "
                + $"about why they need not:{Environment.NewLine}  "
                + string.Join(
                    $"{Environment.NewLine}  ",
                    missing.Select(one => $"{one} — {string.Join("; ", DeskAsks.Unexcused(one))}")));
    }

    [Fact]
    public void Nothing_is_paired_that_no_longer_asks_or_has_since_been_excused()
    {
        var bare = DeskAsks.Bare().ToHashSet(StringComparer.Ordinal);

        var stale = DeskAsks.Known.Where(one => !bare.Contains(one.Case)).ToList();

        // Both directions, and this is the half that decays. A case that was excused after being
        // written down here goes on carrying a reason it no longer needs, and the next reader takes
        // the list for the state of the suite.
        Assert.True(
            stale.Count == 0,
            $"{stale.Count} pairing(s) name a case that does not ask, or that now excuses the desk:"
                + $"{Environment.NewLine}  "
                + string.Join($"{Environment.NewLine}  ", stale.Select(one => one.Case)));
    }

    [Fact]
    public void Every_case_named_is_one_this_suite_really_runs()
    {
        Assert.All(
            DeskAsks.Known,
            one =>
            {
                var found = Provocation.CaseNamed(one.Case);

                Assert.True(found is not null, $"{one.Case} is not a case this suite has");
                Assert.True(Provocation.IsACase(found!), $"{one.Case} is not a case this suite runs");
            });
    }

    [Fact]
    public void No_case_is_paired_twice_and_every_reason_says_something()
    {
        var named = DeskAsks.Known.Select(one => one.Case).ToList();

        Assert.Equal(named.Count, named.Distinct(StringComparer.Ordinal).Count());
        Assert.All(DeskAsks.Known, one => Assert.False(string.IsNullOrWhiteSpace(one.Because)));
    }

    [Fact]
    public void Every_call_is_classified_against_a_condition_this_engine_calls_the_desks()
    {
        // What makes the judgement checkable. The list of calls is written by hand — nothing can
        // derive it — but each names a fact the engine declares, so a call filed against a condition
        // that is really about the application under test is caught rather than believed.
        Assert.All(
            DeskAsks.Calls,
            one => Assert.True(
                DeskFacts.Names(one.Fact),
                $"{one.Call} is filed under '{one.Fact}', which this engine does not call the desk's"));

        Assert.All(DeskAsks.Calls, one => Assert.False(string.IsNullOrWhiteSpace(one.Because)));
    }

    [Fact]
    public void No_call_is_written_down_twice()
    {
        var calls = DeskAsks.Calls.Select(one => one.Call).ToList();

        Assert.Equal(calls.Count, calls.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void The_scan_finds_the_cases_that_do_excuse_as_well_as_the_ones_that_do_not()
    {
        // A scan that found nothing would pass the pairing check by finding nothing to pair, which
        // is the shape of green this whole project exists to withdraw. Both counts are real, and
        // the excusing ones are the majority — WW172 saw to that.
        var asking = DeskAsks.Asking();
        var bare = DeskAsks.Bare();

        Assert.True(asking.Count > bare.Count, $"{asking.Count} ask and {bare.Count} of them are bare");
        Assert.NotEmpty(bare);
        Assert.Contains(
            "NotificationAreaTests.The_taskbar_is_found_by_its_class_and_holds_icons",
            asking,
            StringComparer.Ordinal);
    }

    [Fact]
    public void A_case_that_excuses_the_desk_is_not_asked_to_explain_itself()
    {
        // The one measured by holding the guest's desk, and the reason the rule is worth having:
        // it asks the shell for icons, and on a covered taskbar there are none to find.
        Assert.DoesNotContain(
            "NotificationAreaTests.The_taskbar_is_found_by_its_class_and_holds_icons",
            DeskAsks.Bare(),
            StringComparer.Ordinal);

        Assert.DoesNotContain(
            DeskAsks.Known,
            one => one.Case == "NotificationAreaTests.The_taskbar_is_found_by_its_class_and_holds_icons");
    }

    [Fact]
    public void A_call_named_as_text_is_not_a_call_made()
    {
        // WW191 caught this on its first guest run. A case asserting that a call is among the ones
        // this catalogue sweeps for carries the fragment in a string, and was reported as a case
        // that opens the overflow — a pairing invented for a case that asks nothing at all.
        Assert.DoesNotContain(
            "FlatteningTests.The_producers_are_swept_off_both_assemblies_and_discriminate",
            DeskAsks.Asking(),
            StringComparer.Ordinal);

        // And the stripping is not so keen that it deletes the calls beside it: this very class
        // names one in a string two lines up, and NotificationAreaTests still asks.
        Assert.Contains(
            "NotificationAreaTests.The_taskbar_is_found_by_its_class_and_holds_icons",
            DeskAsks.Asking(),
            StringComparer.Ordinal);
    }

    [Fact]
    public void An_excuse_covers_the_fact_it_names_and_not_every_fact_the_case_asks()
    {
        // WW197, as arithmetic. The old rule was "the body mentions BusyDesk anywhere", and under it
        // a case that guards its fixture and then asserts on the shell counted as covered. Both of
        // these excuse something; what makes them different is which fact.
        var tidy = "NotificationAreaTests.The_taskbar_is_found_by_its_class_and_holds_icons";
        var guarded = "TrayPlacementTests.The_fixture_leaves_the_overflow_the_way_it_found_it";

        // One excuses the reading it goes on to assert about, so it owes nothing.
        Assert.Empty(DeskAsks.Unexcused(tidy));

        // The other excuses its fixture and asks about the flyout, which is a second fact — and it
        // is here with a stated reason rather than credited for an excuse about something else.
        Assert.Contains(guarded, DeskAsks.Bare(), StringComparer.Ordinal);
        Assert.Contains(DeskAsks.Known, one => one.Case == guarded);
    }

    [Fact]
    public void A_reading_whose_answer_is_thrown_away_is_not_asked_for_a_verdict()
    {
        // The other half of the finer unit. A case that shuts the flyout on its way out has not
        // asked the shell for anything: the criterion is about a reading asked for a verdict, and a
        // value nobody looked at cannot become one.
        var tidying = "TrayPlacementTests.Two_icons_from_the_same_run_are_each_placed_before_their_own_add_returns";

        Assert.Contains(tidying, DeskAsks.Asking(), StringComparer.Ordinal);
        Assert.Empty(DeskAsks.Unexcused(tidying));
    }

    [Fact]
    public void The_pairing_reads_as_a_count_and_then_a_line_each()
    {
        var rendered = DeskAsks.Render();

        Assert.Equal(DeskAsks.Known.Count + 1, rendered.Count);
        Assert.StartsWith(
            $"{DeskAsks.Asking().Count} case(s) ask a desk-dependent reading: ",
            rendered[0],
            StringComparison.Ordinal);
        Assert.All(rendered.Skip(1), line => Assert.StartsWith("  ", line, StringComparison.Ordinal));
    }
}

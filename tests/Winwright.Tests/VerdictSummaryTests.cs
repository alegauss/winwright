using Winwright.Verdicts;

using Xunit;

using static Winwright.Tests.Fixtures;

namespace Winwright.Tests;

/// <summary>
/// WW1's other half: the degraded reading has to be legible without opening the log, which means
/// each assertion that did not run is named in the summary rather than counted in it.
/// </summary>
public class VerdictSummaryTests
{
    [Fact]
    public void The_headline_carries_the_word_the_code_and_the_tally()
    {
        var verdict = RunVerdict.Over([
            AssertionResult.Pass("the window is titled Claude"),
            AssertionResult.Unchecked("the tray menu opens", FreeNotificationArea),
        ]);

        Assert.Equal(
            "DEGRADED (exit 2) - 2 assertions: 1 passed, 0 failed, 1 unchecked",
            VerdictSummary.Headline(verdict));
    }

    [Fact]
    public void Each_assertion_that_did_not_run_is_named_with_the_precondition_that_was_absent()
    {
        var verdict = RunVerdict.Over([
            AssertionResult.Pass("the window is titled Claude"),
            AssertionResult.Unchecked("the tray menu opens", FreeNotificationArea),
            AssertionResult.Unchecked("the report renders", RegisteredProfile),
        ]);

        var summary = VerdictSummary.Render(verdict);

        Assert.Contains("  unchecked  the tray menu opens - 'a free notification area' absent: a tray is already resident", summary);
        Assert.Contains("  unchecked  the report renders - 'a registered profile' absent: no profile registered", summary);
    }

    [Fact]
    public void A_failed_run_still_names_the_holes_it_also_carried()
    {
        var verdict = RunVerdict.Over([
            AssertionResult.Fail("the report renders", "the file was never written"),
            AssertionResult.Unchecked("the tray menu opens", FreeNotificationArea),
        ]);

        var summary = VerdictSummary.Render(verdict);

        Assert.StartsWith("FAILED (exit 1) - 2 assertions: 0 passed, 1 failed, 1 unchecked", summary);
        Assert.Contains("  failed     the report renders - the file was never written", summary);
        Assert.Contains("  unchecked  the tray menu opens - 'a free notification area' absent: a tray is already resident", summary);
    }

    [Fact]
    public void A_clean_run_has_nothing_under_the_headline()
    {
        var verdict = RunVerdict.Over([AssertionResult.Pass("the window is titled Claude")]);

        Assert.Equal("PASSED (exit 0) - 1 assertion: 1 passed, 0 failed, 0 unchecked", VerdictSummary.Render(verdict));
        Assert.Empty(VerdictSummary.Detail(verdict));
    }
}

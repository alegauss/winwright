using Winwright.Asserting;
using Winwright.Processes;
using Winwright.Verdicts;

using Xunit;

using static Winwright.Tests.Fixtures;

namespace Winwright.Tests;

/// <summary>
/// WW177. The reading a run takes and the verdict it exits on were two pages that never met.
/// <c>VerdictSummary</c> mentioned <c>Preamble</c> nowhere, so a person handed an exit 2 got the
/// assertions that never ran and not one word about the machine that stopped them running.
/// </summary>
public sealed class JoinedSummaryTests
{
    private static Preamble Read() => Preamble.Of(AppTarget.AttachTo(Environment.ProcessId));

    private static RunVerdict Degraded() => RunVerdict.Over([
        AssertionResult.Pass("the window is titled Claude"),
        AssertionResult.Unchecked("the tray menu opens", FreeNotificationArea),
    ]);

    [Fact]
    public void The_page_carries_the_reading_and_then_the_verdict_it_makes_legible()
    {
        var page = VerdictSummary.Render(Degraded(), Read());

        // Both halves, whole: every line the reading answers on its own, and every line the verdict
        // answers on its own. A join that dropped one of them would be a third page.
        foreach (var line in Read().Render())
            Assert.Contains(line, page, StringComparison.Ordinal);

        Assert.Contains(VerdictSummary.Headline(Degraded()), page, StringComparison.Ordinal);
        Assert.Contains("the tray menu opens", page, StringComparison.Ordinal);
    }

    [Fact]
    public void The_reading_leads_because_it_is_what_makes_the_verdict_underneath_it_legible()
    {
        // The order is the point and not a preference. A reader who has just been told an assertion
        // never ran wants the absent precondition first, and a reading printed after the verdict is
        // one they have already stopped reading.
        var page = VerdictSummary.Render(Degraded(), Read());

        var reading = page.IndexOf(Read().Sentence(), StringComparison.Ordinal);
        var headline = page.IndexOf("DEGRADED (exit 2)", StringComparison.Ordinal);

        Assert.True(reading >= 0, "the reading is not on the page at all");
        Assert.True(headline > reading, "the verdict is printed above the reading it is explained by");
    }

    [Fact]
    public void A_reading_from_the_middle_of_a_run_is_refused_rather_than_printed_as_the_end_of_one()
    {
        // The refusal WW177 asks for. A reading that opened a store fingerprint and never closed it
        // shows the machine as it was before the run touched it, and a verdict beside it is about
        // what happened after — two moments on one page, which is worse than one page missing.
        var declaration = Declared();
        var opened = Preamble.Of(AppTarget.AttachTo(Environment.ProcessId), declaration);

        Assert.NotNull(opened.Store);
        Assert.False(opened.Closed);

        var refusal = Assert.Throws<ArgumentException>(() => VerdictSummary.Render(Degraded(), opened));

        Assert.Contains("never closed it", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("Preamble.Around", refusal.Message, StringComparison.Ordinal);

        // And the closed one goes through, which is what makes the refusal a check rather than a ban.
        var closed = opened.Closing();

        Assert.True(closed.Closed);
        Assert.Contains(StoreChange.Named, VerdictSummary.Render(Degraded(), closed), StringComparison.Ordinal);
    }

    [Fact]
    public void A_run_that_declared_no_store_has_nothing_to_close_and_is_printed()
    {
        // The arm the refusal must not catch. Most projects declare no store, and a reading that
        // never opened a fingerprint is not one that failed to close it.
        var read = Read();

        Assert.Null(read.Store);
        Assert.False(read.Closed);

        Assert.Contains(VerdictSummary.Headline(Degraded()), VerdictSummary.Render(Degraded(), read), StringComparison.Ordinal);
    }

    [Fact]
    public void Neither_half_may_be_left_out_by_passing_nothing()
    {
        Assert.Throws<ArgumentNullException>(() => VerdictSummary.Render(null!, Read()));
        Assert.Throws<ArgumentNullException>(() => VerdictSummary.Render(Degraded(), null!));
    }

    /// <summary>A project declaring a store of this case's own, so a fingerprint has something real.</summary>
    private static Winwright.Projects.ProjectDeclaration Declared()
    {
        var root = Directory.CreateTempSubdirectory("winwright-joined-").FullName;
        var store = Directory.CreateDirectory(Path.Combine(root, "store")).FullName;
        File.WriteAllText(Path.Combine(store, "settings.json"), """{ "profile": "alpha" }""");

        var path = Path.Combine(root, "winwright.json");
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Environment.ProcessPath)}},
              "sourceRoot": {{System.Text.Json.JsonSerializer.Serialize(root)}},
              "fingerprintStore": {{System.Text.Json.JsonSerializer.Serialize(store)}}
            }
            """);

        return Winwright.Projects.ProjectDeclaration.Load(path);
    }
}

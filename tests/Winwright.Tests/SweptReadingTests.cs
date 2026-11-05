using Winwright.Processes;
using Winwright.Verdicts;

using Xunit;

using static Winwright.Tests.Fixtures;

namespace Winwright.Tests;

/// <summary>
/// WW185. WW177 joined the reading to the verdict for a single run. A sweep is the same surface one
/// level up and did not get it: <c>EnvironmentRun</c> was a name and a verdict, with nowhere at all
/// for what the machine turned out to be.
/// <para>
/// A sweep is where that costs most. A single run's reader can ask the machine they are sitting at.
/// A sweep exists because the answer differs between machines, so reading one is how you find out
/// which behaved differently — and what it reported was that an assertion was unchecked in two of
/// five environments, deduped and counted, with not one word about the five.
/// </para>
/// </summary>
public sealed class SweptReadingTests
{
    private static Preamble Read() => Preamble.Of(AppTarget.AttachTo(Environment.ProcessId));

    private static RunVerdict Clean() => RunVerdict.Over([AssertionResult.Pass("the window is titled Claude")]);

    private static RunVerdict Degraded() => RunVerdict.Over([
        AssertionResult.Pass("the window is titled Claude"),
        AssertionResult.Unchecked("the tray menu opens", FreeNotificationArea),
    ]);

    [Fact]
    public void An_environment_that_had_something_to_explain_carries_the_reading_that_explains_it()
    {
        var reading = Read();
        var sweep = SweepVerdict.Over([
            new EnvironmentRun("light", Clean(), reading),
            new EnvironmentRun("dark", Degraded(), reading),
        ]);

        var page = SweepSummary.Render(sweep);

        // The environment with the hole is described; the clean one is not, because a reader
        // skimming five machines wants the ones that behaved differently and not a page each.
        Assert.Contains("on         [dark]", page, StringComparison.Ordinal);
        Assert.DoesNotContain("on         [light]", page, StringComparison.Ordinal);
        Assert.Contains(reading.Sentence(), page, StringComparison.Ordinal);
    }

    [Fact]
    public void A_sweep_that_read_some_machines_and_not_others_says_which_it_did_not()
    {
        // The arm WW185 is really about. Four described and one not is the case where the fifth
        // reads as a machine like the others, because everything on the page is about the four.
        var sweep = SweepVerdict.Over([
            new EnvironmentRun("light", Degraded(), Read()),
            new EnvironmentRun("dark", Degraded()),
        ]);

        Assert.False(sweep.Describes);
        Assert.Equal(["dark"], sweep.Undescribed);

        var page = SweepSummary.Render(sweep);

        Assert.Contains("  not read   1 environment(s): dark", page, StringComparison.Ordinal);
    }

    [Fact]
    public void A_sweep_that_described_none_claims_nothing_rather_than_reporting_them_all_unread()
    {
        // The arm that must stay quiet. A sweep that took no readings has no "on" lines for a
        // reader to mistake for the whole set, so naming every environment as unread would be
        // noise about a claim nobody made.
        //
        // Matched on the line's own prefix and not on the words: a preamble's own sentence says
        // "not read" about the measurements it could not take, and a looser assertion here caught
        // that instead of the line it was about.
        var sweep = SweepVerdict.Over([
            new EnvironmentRun("light", Degraded()),
            new EnvironmentRun("dark", Degraded()),
        ]);

        Assert.False(sweep.Describes);
        Assert.Equal(2, sweep.Undescribed.Count);

        Assert.DoesNotContain("  not read   ", SweepSummary.Render(sweep), StringComparison.Ordinal);
    }

    [Fact]
    public void A_sweep_that_read_every_machine_says_nothing_about_reading()
    {
        var sweep = SweepVerdict.Over([
            new EnvironmentRun("light", Degraded(), Read()),
            new EnvironmentRun("dark", Degraded(), Read()),
        ]);

        Assert.True(sweep.Describes);
        Assert.Empty(sweep.Undescribed);
        Assert.DoesNotContain("  not read   ", SweepSummary.Render(sweep), StringComparison.Ordinal);
    }

    [Fact]
    public void An_environment_carries_its_own_reading_and_never_the_sweeps()
    {
        // The record's own rule, one field over: a verdict is that environment's, unchanged by the
        // sweep around it. The reading is the same — two machines are two readings, and one shared
        // between them would be the collapse this type was shaped to refuse.
        var light = Read();
        var dark = Read();

        var sweep = SweepVerdict.Over([
            new EnvironmentRun("light", Clean(), light),
            new EnvironmentRun("dark", Clean(), dark),
        ]);

        Assert.Same(light, sweep.Environments[0].Reading);
        Assert.Same(dark, sweep.Environments[1].Reading);
        Assert.True(sweep.Environments[0].Described);
    }

    [Fact]
    public void An_environment_nobody_read_is_told_apart_from_one_with_nothing_to_say()
    {
        // Null is not a clean machine. A sweep that never read an environment and one that read it
        // and found it ordinary are two facts, and Described is what keeps them apart.
        var unread = new EnvironmentRun("dark", Clean());
        var read = new EnvironmentRun("light", Clean(), Read());

        Assert.False(unread.Described);
        Assert.Null(unread.Reading);
        Assert.True(read.Described);
    }
}

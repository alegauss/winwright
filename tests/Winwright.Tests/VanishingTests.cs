using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW557. WW555 made a look whose match is gone before it is read count as no look, so an act polls
/// on; and it made the refusal say a match was there and gone each time. Neither was provoked: the
/// guest's WPF picker realises its items before the first look, and a control tree without the fix
/// passed photo-boost's two steps 32 rounds of 32.
/// <para>
/// <c>--vanishing</c> builds the race instead of waiting for it. Its buttons answer the automation id
/// a search matches on and refuse their name as an element no longer available, so every look finds
/// one and cannot read it — one only for a moment, one for good.
/// </para>
/// <para>
/// The launched fixture and no dialog beside it, which is WW248's rule: an in-process window shown by
/// this thread takes the desk, so a class holding one cannot also drive a launched window.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class VanishingTests : IDisposable
{
    private readonly Settling settling = Attachable.Settling();
    private readonly AutomationElement fixtureRoot;

    public VanishingTests()
    {
        var launched = settling.Register.Launch(Fixture.Started("--vanishing"));
        fixtureRoot = AutomationElement.FromHandle(Fixture.Drew(launched.Pid).Handle);
    }

    public void Dispose() => settling.Dispose();

    private Subject Button(string id) => Subject.Unguarded(
        fixtureRoot, Locator.Parse($"Button#{id}"), deadlineMs: 2000, pollMs: 25);

    [Fact]
    public void An_act_against_a_match_gone_before_it_was_read_polls_on_until_it_can_be_read()
    {
        // Without WW555 this is refused on the first look as "nothing matched", which is the defect:
        // the button is there the whole time, and the look that found it gave up on it.
        var acted = Act.Invoke(Button("vanishingOnce"));

        Assert.True(acted.Polls > 1, $"the button was read on the first look, so nothing raced: {acted}");

        // Polled, because an invoke on a WPF button is queued to its dispatcher and lands a moment
        // after the call returns.
        var landed = Attempt.UntilTrue(() => Pressed() == "pressed", 2000, 25);
        Assert.True(landed.Happened, $"the pane still says '{Pressed()}' after the invoke");
    }

    [Fact]
    public void A_match_that_is_gone_on_every_look_is_refused_as_a_race_and_not_as_a_missing_element()
    {
        var refused = Assert.Throws<NotActionableException>(() => Act.Invoke(Button("vanishingAlways")));

        Assert.Equal(Actionable.NotInTree, refused.Missing);
        Assert.Contains("it matched on ", refused.Because, StringComparison.Ordinal);
        Assert.Contains("was gone each time before it could be read", refused.Because, StringComparison.Ordinal);
        Assert.DoesNotContain("nothing matched", refused.Because, StringComparison.Ordinal);
    }

    /// <summary>What the pane says about the button that settles, read through the engine's own reading.</summary>
    private string Pressed()
    {
        var reading = Subject.Unguarded(fixtureRoot, Locator.Parse("Text#vanishingPressed"), deadlineMs: 2000, pollMs: 25).Read();
        Assert.True(reading.Found, "the vanishing pane draws no Text#vanishingPressed");

        return reading.Facts!.Name;
    }
}

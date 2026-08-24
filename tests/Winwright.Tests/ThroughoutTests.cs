using Winwright.Capturing;
using Winwright.Processes;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW195. The region on both sides of the take. WW187 read it at the closest observable instant and
/// wrote that down as such, and the instant before is not the instant of: a window arriving between
/// the reading and the write is in the copy and in nothing else, and the receipt says the region was
/// clear because it was, a moment earlier.
/// <para>
/// Driven and not composed. A covered <see cref="Obstruction" /> cannot be built from out here — the
/// type has a private constructor, which is what stops a case inventing a reading the engine would
/// never produce — so the window that arrives inside the take is a real one, opened by the take
/// itself. Two dialogs of this process take the same rectangle, so the second stands exactly over
/// the first.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ThroughoutTests
{
    private static TopLevelWindow Found(PumpedDialog dialog) =>
        Assert.Single(
            TopLevelWindows.OfProcess(Environment.ProcessId), window => window.Handle == dialog.Frame);

    [Fact]
    public void A_region_that_went_from_clear_to_covered_is_refused_as_the_desk_changing()
    {
        // The arm WW195 exists for, and the reason it is not RegionCovered: the reading WW40 refuses
        // on is taken before the intruder exists, so on the shape this replaces the capture passed.
        using var under = PumpedDialog.Open("winwright statistics");
        var window = Found(under);

        // WW190. The region has to start clear for this to be about a window that arrived, and
        // whether anything is already over that rectangle is the desk's business.
        if (BusyDesk.Excused(
            Obstruction.Reading(under.Frame, window.Bounds).AsAssertion("the region is clear to begin with")))
            return;

        PumpedDialog? arriving = null;
        try
        {
            // The take writes nothing, which is honest here: this case is about the two readings
            // either side of it, and a receipt over a file that was never written records exactly
            // that. What the take does is open a window over the region, which is the defect.
            var refused = Assert.Throws<WrongCaptureException>(() => CaptureReceipt.Taking(
                Path.Combine(Path.GetTempPath(), "winwright-changed-never-written.png"),
                window,
                AppTarget.AttachTo(Environment.ProcessId),
                _ => arriving = PumpedDialog.Open("winwright arrived mid-capture")));

            Assert.Equal(WrongCapture.DeskChanged, refused.Arm);
            Assert.Contains(
                "clear when the capture started and not when it finished",
                refused.Message,
                StringComparison.Ordinal);
            Assert.Contains("the desk changed under it", refused.Message, StringComparison.Ordinal);

            // Named, the way WW38 insists: a reader handed this needs the window that arrived.
            Assert.Contains("winwright arrived mid-capture", refused.Message, StringComparison.Ordinal);
        }
        finally
        {
            arriving?.Dispose();
        }
    }

    [Fact]
    public void A_region_nothing_arrived_over_is_clear_at_both_ends()
    {
        // The arm the refusal must not catch, and the one every ordinary capture takes.
        using var under = PumpedDialog.Open("winwright statistics");
        var window = Found(under);

        var over = RegionThroughout.Around(under.Frame, window.Bounds, () => { });

        if (BusyDesk.Excused(over.AsAssertion("the region held still")))
            return;

        Assert.True(over.Twice);
        Assert.True(over.HeldStill);
        Assert.True(over.AsFinding().Holds);
        Assert.False(over.Changed);
        Assert.Contains("at either end of the capture", over.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_region_read_once_says_so_rather_than_standing_for_both_ends()
    {
        // The half that matters most, because it is what every caller had before this. One reading
        // is a true thing to have read and it is not the claim this type is named for, so `Clear`
        // can hold while `HeldStill` does not — and the Finding says nothing was observed.
        using var under = PumpedDialog.Open("winwright statistics");
        var once = RegionThroughout.Once(Obstruction.Reading(under.Frame, Found(under).Bounds));

        Assert.False(once.Twice);
        Assert.False(once.HeldStill);
        Assert.Null(once.AsFinding().Holds);
        Assert.Contains("read once and not again", once.Sentence(), StringComparison.Ordinal);
        Assert.Equal(AssertionOutcome.Unchecked, once.AsAssertion("the region held still").Outcome);
    }

    [Fact]
    public void The_take_runs_between_the_two_readings_and_not_before_them()
    {
        // The sequencing, asserted where a comment used to stand. This is the whole of WW195, and
        // the door holds it rather than a caller who has to remember the second reading.
        var order = new List<string>();

        var over = RegionThroughout.Around(0, new WindowBounds(0, 0, 10, 10), () => order.Add("took the picture"));

        Assert.Equal(["took the picture"], order);
        Assert.True(over.Twice);
        Assert.NotNull(over.After);
    }

    [Fact]
    public void A_reading_that_was_never_taken_does_not_refuse_a_capture_either()
    {
        // WW38's rule carried through the pair. A window handle of zero is a reading nobody could
        // take, and refusing on one would report an intruder nobody saw.
        var region = new WindowBounds(0, 0, 10, 10);
        var over = RegionThroughout.Around(0, region, () => { });

        Assert.True(over.Twice);
        Assert.False(over.Was);
        Assert.False(over.Changed);
        Assert.False(over.WasCovered);
        Assert.Null(over.AsFinding().Holds);
        Assert.Contains("could not be read on both sides", over.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void The_claim_it_is_named_for_is_the_one_a_single_reading_cannot_make()
    {
        // Why HeldStill exists beside Clear. A caller that read once and found nothing has read a
        // true thing; what it has not read is whether the region stayed that way, and the whole
        // defect was letting the first stand for the second.
        using var under = PumpedDialog.Open("winwright statistics");
        var read = Obstruction.Reading(under.Frame, Found(under).Bounds);

        if (BusyDesk.Excused(read.AsAssertion("the region is clear")))
            return;

        var once = RegionThroughout.Once(read);

        Assert.True(once.Clear);
        Assert.False(once.HeldStill);
        Assert.Equal(RegionThroughout.Named, once.AsFinding().Named);
    }
}

using Winwright.Capturing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW36. The window rectangle spans a window's invisible resize border and its drop-shadow
/// margin, so every copy of it carries a strip of whatever is behind the window down its edges.
/// <para>
/// Both shapes are here because the difference between them is the finding: an overlapped window
/// has a sizing border and trims, a bare popup has none and trims nothing. A capture that assumed
/// the trim was always there would cut into the second one.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class PaintedFrameTests : IDisposable
{
    private readonly PumpedDialog framed = PumpedDialog.OpenFramed("winwright framed");
    private readonly PumpedDialog bare = PumpedDialog.Open("winwright bare");

    public void Dispose()
    {
        bare.Dispose();
        framed.Dispose();
    }

    [Fact]
    public void A_window_with_a_sizing_border_paints_less_than_it_owns()
    {
        var frame = PaintedFrame.Of(framed.Frame);

        Assert.NotNull(frame);
        Assert.Null(frame.Because);
        Assert.True(frame.Trimmed, frame.Sentence());
        Assert.True(frame.Painted.Width < frame.Owned.Width, frame.Sentence());
        Assert.True(frame.Painted.Height < frame.Owned.Height, frame.Sentence());
    }

    [Fact]
    public void The_trim_is_asymmetric_because_the_top_has_no_invisible_border()
    {
        var frame = PaintedFrame.Of(framed.Frame)!;

        Assert.Equal(0, frame.TrimmedTop);
        Assert.True(frame.TrimmedLeft > 0, frame.Sentence());
        Assert.True(frame.TrimmedRight > 0, frame.Sentence());
        Assert.True(frame.TrimmedBottom > 0, frame.Sentence());
    }

    [Fact]
    public void The_two_sides_trim_the_same_amount_and_the_bottom_matches_them()
    {
        var frame = PaintedFrame.Of(framed.Frame)!;

        Assert.Equal(frame.TrimmedLeft, frame.TrimmedRight);
        Assert.Equal(frame.TrimmedLeft, frame.TrimmedBottom);
    }

    [Fact]
    public void A_window_with_no_border_trims_nothing_and_says_so()
    {
        var frame = PaintedFrame.Of(bare.Frame);

        Assert.NotNull(frame);
        Assert.False(frame.Trimmed);
        Assert.Equal(frame.Owned, frame.Painted);
        Assert.Contains("which is the whole window rectangle: nothing to trim", frame.Sentence());
    }

    [Fact]
    public void The_run_says_how_much_it_trimmed_because_nobody_can_see_it_in_one_file()
    {
        var frame = PaintedFrame.Of(framed.Frame)!;

        var said = frame.Sentence();

        Assert.Contains("trimmed from", said);
        Assert.Contains($"left {frame.TrimmedLeft}", said);
        Assert.Contains("top 0", said);
        Assert.Contains($"right {frame.TrimmedRight}", said);
        Assert.Contains($"bottom {frame.TrimmedBottom}", said);
    }

    [Fact]
    public void Both_rectangles_are_read_in_the_same_space_so_the_trim_is_a_real_number()
    {
        var frame = PaintedFrame.Of(framed.Frame)!;

        // The painted frame sits inside the owned one on every edge, which is only true if the
        // two came back in the same coordinate space. A DPI mismatch would put it outside.
        Assert.True(frame.Painted.Left >= frame.Owned.Left, frame.Sentence());
        Assert.True(frame.Painted.Top >= frame.Owned.Top, frame.Sentence());
        Assert.True(frame.Painted.Right <= frame.Owned.Right, frame.Sentence());
        Assert.True(frame.Painted.Bottom <= frame.Owned.Bottom, frame.Sentence());
    }

    [Fact]
    public void A_handle_that_names_no_window_answers_nothing()
    {
        Assert.Null(PaintedFrame.Of(0));
        Assert.Null(PaintedFrame.Of(0x7FFFFFFF));
    }
}

using System.Windows.Media;
using System.Windows.Media.Imaging;

using Winwright.Capturing;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW42. Measured in freewilly: a copy of the notification area came back as exactly one distinct
/// colour, with the session present, the shell running and the environment reporting an interactive
/// desktop. The display was simply not rendering anything a copy could read, and without this the
/// script wrote that file and exited zero.
/// <para>
/// Driven against real pictures written to disk rather than against a stub, because the reading
/// that matters is the one taken off a file somebody would otherwise have believed. A picture is a
/// file and no window has to exist for one, which is why this needs no fixture shape.
/// </para>
/// </summary>
public sealed class ColoursTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-colours-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    /// <summary>Write a PNG of exactly these colours, one per column, in a format with no alpha.</summary>
    private string Written(string named, PixelFormat format, params Color[] colours)
    {
        const int height = 8;
        var width = Math.Max(colours.Length, 8);

        var source = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        var stride = width * 4;
        var pixels = new byte[stride * height];

        for (var line = 0; line < height; line++)
        {
            for (var column = 0; column < width; column++)
            {
                var colour = colours[column % colours.Length];
                var at = (line * stride) + (column * 4);
                pixels[at] = colour.B;
                pixels[at + 1] = colour.G;
                pixels[at + 2] = colour.R;
                pixels[at + 3] = 255;
            }
        }

        source.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), pixels, stride, 0);

        var path = Path.Combine(root, named);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(new FormatConvertedBitmap(source, format, null, 0)));
        using var file = File.Create(path);
        encoder.Save(file);

        return path;
    }

    [Fact]
    public void A_picture_of_exactly_one_colour_is_read_as_one_colour()
    {
        var flat = Colours.In(Written("flat.png", PixelFormats.Bgr24, Colors.DimGray));

        Assert.True(flat.Counted);
        Assert.True(flat.IsFlat);
        Assert.Equal(1, flat.Distinct);
        Assert.False(flat.Capped, "counting stopped early on a picture with one colour in it");
        Assert.Contains("is one colour across all", flat.Sentence(), StringComparison.Ordinal);
        Assert.Contains("not a picture of a window", flat.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void The_reading_answers_on_a_format_with_no_alpha_which_is_the_one_that_matters()
    {
        // The whole reason this is a second reading. Every screen copy is opaque, and the alpha
        // scan says so and stops — Tellable false, nothing concluded. This one concludes.
        var path = Written("opaque.png", PixelFormats.Bgr24, Colors.DimGray);

        var ink = Pictures.Of(path);
        Assert.False(ink.Tellable, ink.Sentence());
        Assert.Contains("no alpha channel", ink.Sentence(), StringComparison.Ordinal);

        var colour = Colours.In(path);
        Assert.True(colour.Counted, colour.Sentence());
        Assert.True(colour.IsFlat);
    }

    [Fact]
    public void A_picture_with_more_than_one_colour_is_not_flat()
    {
        var varied = Colours.In(Written("two.png", PixelFormats.Bgr24, Colors.DimGray, Colors.White));

        Assert.True(varied.Counted);
        Assert.False(varied.IsFlat);
        Assert.Equal(2, varied.Distinct);
        Assert.Contains("2 distinct colour(s)", varied.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void Counting_stops_once_the_answer_cannot_change_and_says_that_it_did()
    {
        // A picture of a real window has thousands of colours and the question is whether it has
        // one, so counting them all is work nobody asked for — and a count that stopped early must
        // say so rather than reporting eight as if it were the whole answer.
        var many = new[]
        {
            Colors.Black, Colors.White, Colors.Red, Colors.Lime,
            Colors.Blue, Colors.Yellow, Colors.Cyan, Colors.Magenta, Colors.Orange, Colors.Purple,
        };

        var read = Colours.In(Written("many.png", PixelFormats.Bgr24, many));

        Assert.True(read.Capped);
        Assert.Equal(Colours.Enough, read.Distinct);
        Assert.Contains($"at least {Colours.Enough}", read.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_flat_picture_fails_and_one_that_could_not_be_counted_is_unchecked()
    {
        var flat = Colours.In(Written("verdict.png", PixelFormats.Bgr24, Colors.DimGray));

        Assert.Equal(AssertionOutcome.Failed, flat.AsAssertion().Outcome);
        Assert.Equal(Winwright.Tracing.StepVerdict.Failed, flat.AsTraceStep().Verdict);

        var varied = Colours.In(Written("verdict-two.png", PixelFormats.Bgr24, Colors.DimGray, Colors.White));

        Assert.Equal(AssertionOutcome.Passed, varied.AsAssertion().Outcome);
        Assert.Equal(Winwright.Tracing.StepVerdict.Ok, varied.AsTraceStep().Verdict);
    }

    [Fact]
    public void A_picture_that_is_not_there_is_refused_rather_than_read_as_flat()
    {
        // A missing capture is not a flat one, which is the same distinction Pictures makes for a
        // missing capture and a blank: an absence answered as a reading is a reading nobody took.
        Assert.Throws<FileNotFoundException>(() => Colours.In(Path.Combine(root, "nothing.png")));
        Assert.Throws<ArgumentException>(() => Colours.In("  "));
    }
}

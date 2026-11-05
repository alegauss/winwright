using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Winwright.Capturing;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW46. freewilly's window skill states the rule: a change meant to be invisible must produce a
/// byte-identical file, and the render is deterministic, verified by re-capturing unchanged code.
/// Three findings about theme handling in that project came from this and from nothing else, and its
/// test suite saw none of them.
/// <para>
/// Driven against the proving ground's own fixed surface, which exists for exactly this: the
/// justification on <c>--render</c> says the byte-identical comparison had nothing to be identical
/// to, because every surface available read a clock, a machine name or the desktop's theme.
/// </para>
/// </summary>
public sealed class UnchangedTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-unchanged-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    /// <summary>Render the fixture's fixed surface to a file of its own and hand back the path.</summary>
    private string Rendered(string named)
    {
        var path = Path.Combine(root, named);
        var start = Fixture.Started($"--render={path}");
        start.RedirectStandardOutput = true;
        start.UseShellExecute = false;

        using var running = Process.Start(start)!;
        running.StandardOutput.ReadToEnd();

        Assert.True(running.WaitForExit(30_000), "the render did not finish");
        Assert.Equal(0, running.ExitCode);
        Assert.True(File.Exists(path), $"the render wrote nothing to {path}");

        return path;
    }

    [Fact]
    public void Two_renders_of_unchanged_code_are_byte_identical()
    {
        // The whole assertion, and the reason it is worth having: no tolerance is chosen, so a
        // difference is a real difference rather than one somebody decided was large enough.
        var read = Unchanged.Between(Rendered("once.png"), Rendered("twice.png"));

        Assert.True(read.Was, read.Sentence());
        Assert.True(read.Identical, read.Sentence());
        Assert.Equal(-1, read.DiffersAt);
        Assert.Contains("byte-identical across all", read.Sentence(), StringComparison.Ordinal);
        Assert.Equal(AssertionOutcome.Passed, read.AsAssertion().Outcome);
    }

    [Fact]
    public void A_render_that_changed_says_which_byte_and_how_many_pixels()
    {
        var one = Rendered("before.png");
        var other = Repainted("after.png", Colors.Fuchsia);

        var read = Unchanged.Between(one, other);

        Assert.False(read.Identical, read.Sentence());
        Assert.True(read.DiffersAt >= 0);
        Assert.True(read.PixelsRead, read.Sentence());
        Assert.True(read.PixelsDiffering > 0, read.Sentence());
        Assert.Contains("draw differently in", read.Sentence(), StringComparison.Ordinal);
        Assert.Equal(AssertionOutcome.Failed, read.AsAssertion().Outcome);
    }

    [Fact]
    public void Files_that_differ_and_draw_the_same_picture_say_which_of_the_two_it_was()
    {
        // The distinction a single answer loses. An encoder writing something of its own into the
        // file changes the bytes and not the picture, and a reader told only "the render changed"
        // goes looking for a visual difference, finds none, and concludes the check is broken.
        var one = Path.Combine(root, "plain.png");
        var other = Path.Combine(root, "commented.png");

        Paint(one, Colors.SlateGray, comment: null);
        Paint(other, Colors.SlateGray, comment: "written by something that leaves a note");

        var read = Unchanged.Between(one, other);

        Assert.False(read.Identical, read.Sentence());
        Assert.True(read.SamePicture, read.Sentence());
        Assert.Equal(0, read.PixelsDiffering);
        Assert.Contains("in the file rather than in what was drawn", read.Sentence(), StringComparison.Ordinal);

        // Still a failure: the rule is about the file, and an encoder that varies is a reason the
        // rule stops working rather than a reason to stop applying it.
        Assert.Equal(AssertionOutcome.Failed, read.AsAssertion().Outcome);
    }

    [Fact]
    public void A_render_that_was_never_written_is_not_a_render_that_changed()
    {
        var read = Unchanged.Between(Rendered("there.png"), Path.Combine(root, "missing.png"));

        Assert.False(read.Was);
        Assert.False(read.Identical);
        Assert.Contains("there is no render at", read.Sentence(), StringComparison.Ordinal);

        // A hole and never a failure: nothing was compared, so nothing about the render was
        // observed either way.
        var verdict = read.AsAssertion();
        Assert.Equal(AssertionOutcome.Unchecked, verdict.Outcome);
        Assert.Equal(RenderMatch.PreconditionName, verdict.Missing!.Name);
        Assert.Equal(Winwright.Tracing.StepVerdict.Unchecked, read.AsTraceStep().Verdict);
    }

    [Fact]
    public void A_file_that_cannot_be_decoded_says_so_rather_than_claiming_the_picture_is_the_same()
    {
        // The third state on the pixel half. A file this cannot read may draw anything at all, and
        // answering zero differing pixels would be a green about a picture nobody saw.
        var one = Rendered("real.png");
        var other = Path.Combine(root, "not-a-png.png");
        File.WriteAllText(other, "this is not a picture, whatever its name says");

        var read = Unchanged.Between(one, other);

        Assert.True(read.Was, read.Sentence());
        Assert.False(read.Identical);
        Assert.False(read.PixelsRead);
        Assert.False(read.SamePicture);
        Assert.Contains("neither could be decoded", read.Sentence(), StringComparison.Ordinal);
    }

    /// <summary>The fixture's render, then the same picture painted over in one colour.</summary>
    private string Repainted(string named, Color colour)
    {
        var path = Path.Combine(root, named);
        Paint(path, colour, comment: null);
        return path;
    }

    private static void Paint(string path, Color colour, string? comment)
    {
        const int side = 24;
        var source = new WriteableBitmap(side, side, 96, 96, PixelFormats.Bgra32, null);
        var stride = side * 4;
        var pixels = new byte[stride * side];

        for (var at = 0; at < pixels.Length; at += 4)
        {
            pixels[at] = colour.B;
            pixels[at + 1] = colour.G;
            pixels[at + 2] = colour.R;
            pixels[at + 3] = 255;
        }

        source.WritePixels(new System.Windows.Int32Rect(0, 0, side, side), pixels, stride, 0);

        var frame = comment is null
            ? BitmapFrame.Create(source)
            : BitmapFrame.Create(source, null, Metadata(comment), null);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(frame);
        using var file = File.Create(path);
        encoder.Save(file);
    }

    private static BitmapMetadata Metadata(string comment)
    {
        var metadata = new BitmapMetadata("png");
        metadata.SetQuery("/tEXt/{str=Comment}", comment);
        return metadata;
    }
}

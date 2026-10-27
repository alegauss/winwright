using System.Windows.Media;
using System.Windows.Media.Imaging;

using Winwright.Asserting;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW573. A file step claiming what an image the application wrote is: its size and whether it is
/// clear anywhere.
/// <para>
/// Found by Modelado (MOD19): a PNG rendered at 2048 pixels on its long side, clear around the model,
/// whose bytes vary with the viewport on each desk, so <c>sameBytesAs</c> had nothing to compare it
/// with. None of these needs a desk: the pictures are encoded here and a file step resolves nothing.
/// </para>
/// </summary>
public sealed class ImageFileTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-image-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void A_claim_is_read_the_way_a_case_writes_it()
    {
        Assert.NotNull(ImageClaim.Parse("longSide 2048, transparent", out _));
        Assert.NotNull(ImageClaim.Parse(" width 640 ,height 480, opaque ", out _));

        Assert.All(
            ["", "big", "width", "width -2", "width 0", "longSide 2048 px", "transparent, opaque", "width 640,"],
            written => Assert.Null(ImageClaim.Parse(written, out _)));

        ImageClaim.Parse("transparent, opaque", out var wrong);
        Assert.Contains("both transparent and opaque", wrong, StringComparison.Ordinal);
    }

    [Fact]
    public void A_picture_is_read_for_its_size_and_whether_any_pixel_is_clear()
    {
        var clear = ImageClaim.Read(Png(300, 200, clearCorner: true), out _);
        var solid = ImageClaim.Read(Png(300, 200, clearCorner: false), out _);
        var jpeg = ImageClaim.Read(Jpeg(120, 340), out _);

        Assert.Equal(new ImageReading(300, 200, true), clear);
        Assert.Equal(new ImageReading(300, 200, false), solid);
        Assert.Equal(new ImageReading(120, 340, false), jpeg);

        Assert.Null(ImageClaim.Read([1, 2, 3, 4], out var why));
        Assert.False(string.IsNullOrEmpty(why));
    }

    [Theory]
    [InlineData("longSide 300, transparent", true)]
    [InlineData("width 300, height 200", true)]
    [InlineData("shortSide 200", true)]
    [InlineData("longSide 2048", false)]
    [InlineData("opaque", false)]
    public void A_claim_holds_exactly_where_every_fact_in_it_does(string claim, bool holds)
    {
        Assert.Equal(holds, ImageClaim.Parse(claim, out _)!.HeldBy(new ImageReading(300, 200, true)));
    }

    [Fact]
    public void An_image_the_step_describes_passes()
    {
        Written("out/view.png", Png(2048, 1152, clearCorner: true));

        var verdict = Run("""{ "file": "out/view.png", "act": "read", "image": "longSide 2048, transparent" }""");

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void An_image_of_another_size_fails_naming_the_size_it_read()
    {
        // The control: the same claim against a picture half the size and opaque, which a step that
        // read nothing, or read only that a file is there, would pass.
        Written("out/view.png", Png(1024, 576, clearCorner: false));

        var verdict = Run("""{ "file": "out/view.png", "act": "read", "image": "longSide 2048, transparent" }""");

        Assert.Equal(RunOutcome.Failed, verdict.Outcome);
        Assert.Contains("is 1024x576, with no transparent pixel, and the step claimed longSide 2048, transparent", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_file_that_is_no_image_fails_saying_it_could_not_be_decoded()
    {
        Written("out/view.png", [0x89, 0x50, 0x4E, 0x47, 1, 2, 3]);

        var verdict = Run("""{ "file": "out/view.png", "act": "read", "image": "opaque" }""");

        Assert.Equal(RunOutcome.Failed, verdict.Outcome);
        Assert.Contains("could not be read as an image", Said(verdict), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{ "file": "a.png", "act": "read", "image": "big" }""", "'big' is no fact about an image")]
    [InlineData("""{ "locator": "Edit", "act": "read", "image": "opaque" }""", "names that file in 'file'")]
    [InlineData("""{ "file": "a.png", "act": "read", "image": "opaque", "sameBytesAs": "b.png" }""", "a step makes one claim")]
    public void What_an_image_claim_cannot_say_is_refused_where_it_is_written(string step, string because)
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => ScenarioFile.Read("image.cases.json", $$"""{ "cases": [ { "name": "an image", "steps": [ {{step}} ] } ] }"""));

        Assert.Contains(because, refusal.Because, StringComparison.Ordinal);
    }

    /// <summary>A PNG of that size, opaque grey, with its top-left pixel clear where asked.</summary>
    private static byte[] Png(int width, int height, bool clearCorner)
    {
        var pixels = new byte[width * height * 4];
        for (var at = 0; at < pixels.Length; at += 4)
            (pixels[at], pixels[at + 1], pixels[at + 2], pixels[at + 3]) = ((byte)90, (byte)90, (byte)90, (byte)255);

        if (clearCorner)
            pixels[3] = 0;

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4)));
        return Encoded(encoder);
    }

    /// <summary>A JPEG of that size, which has no alpha at all.</summary>
    private static byte[] Jpeg(int width, int height)
    {
        var pixels = new byte[width * height * 3];
        var encoder = new JpegBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr24, null, pixels, width * 3)));
        return Encoded(encoder);
    }

    private static byte[] Encoded(BitmapEncoder encoder)
    {
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private void Written(string relative, byte[] bytes)
    {
        var path = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
    }

    /// <summary>One case holding <paramref name="step"/>, run under a project rooted in this test's directory.</summary>
    private SuiteVerdict Run(string step)
    {
        var path = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(path, """{ "timeouts": { "resolve": 300, "poll": 50 } }""");

        var declared = Assert.Single(ScenarioFile.Read("image.cases.json", $$"""
            { "cases": [ { "name": "an image", "steps": [ {{step}} ] } ] }
            """));

        // The desktop as a root, which a file step never reads, asked the way FileSubjectTests asks it.
        return Suite.Run([declared], Selection.All, FileSubjectTests.Desktop(), ProjectDeclaration.Load(path));
    }

    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));
}

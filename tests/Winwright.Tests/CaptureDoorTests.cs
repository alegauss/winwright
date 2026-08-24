using Winwright.Capturing;
using Winwright.Processes;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW187. WW38, WW41 and WW42 gave this block three readings a capture needs, and WW40 gave the
/// receipt the refusals that fire on them — all as optional arguments, so a caller who passed none
/// got a receipt that refused nothing and recorded honestly that nobody asked.
/// <para>
/// Recording it honestly is the part that worked. What did not is that nothing asked, which is the
/// argument <c>Preamble.Of</c> already makes about the readings a run takes: one reached by its own
/// call is one a runner is free to forget, and the forgotten one stops being measured while every
/// assertion that needed it starts passing.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class CaptureDoorTests : IDisposable
{
    private readonly PumpedDialog dialog = PumpedDialog.Open("winwright capture door");
    private readonly string root = Directory.CreateTempSubdirectory("winwright-door-").FullName;

    public void Dispose()
    {
        dialog.Dispose();
        Directory.Delete(root, recursive: true);
    }

    private TopLevelWindow Window() =>
        TopLevelWindows.OfProcess(Environment.ProcessId, smallest: 0).Single(one => one.Handle == dialog.Frame);

    private static AppTarget Target() => AppTarget.AttachTo(Environment.ProcessId);

    /// <summary>Write a picture of more than one colour, so the colour count has nothing to refuse.</summary>
    private static void Paint(string path)
    {
        const int side = 16;
        var source = new System.Windows.Media.Imaging.WriteableBitmap(
            side, side, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);

        var stride = side * 4;
        var pixels = new byte[stride * side];
        for (var at = 0; at < pixels.Length; at += 4)
        {
            pixels[at] = (byte)(at % 251);
            pixels[at + 1] = (byte)(at % 199);
            pixels[at + 2] = (byte)(at % 97);
            pixels[at + 3] = 0xFF;
        }

        source.WritePixels(new System.Windows.Int32Rect(0, 0, side, side), pixels, stride, 0);

        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(source));
        using var file = File.Create(path);
        encoder.Save(file);
    }

    [Fact]
    public void A_capture_taken_through_the_door_carries_the_readings_nobody_asked_for()
    {
        var path = Path.Combine(root, "asked.png");
        var taken = false;

        CaptureReceipt receipt;
        try
        {
            receipt = CaptureReceipt.Taking(
                path, Window(), Target(), one => { Paint(one); taken = true; },
                route: CaptureRoute.Forced("a case insisted on the screen for this one"));
        }
        catch (WrongCaptureException refused)
        {
            // A window somebody else left over this dialog, which is the desk and not this code —
            // and the refusal is itself the proof the door asked, because nothing here passed a
            // region reading in. Both arms assert something real, which is the point.
            Assert.True(taken, "the door refused without ever writing the file");
            Assert.Contains("stand over", refused.Message, StringComparison.Ordinal);
            return;
        }

        Assert.True(taken, "the door never called what writes the file");

        // The whole of it: not one of these was passed in, and all three are on the receipt.
        Assert.NotNull(receipt.Over);
        Assert.NotNull(receipt.Glass);
        Assert.NotNull(receipt.Colours);
    }

    [Fact]
    public void The_readings_a_render_cannot_be_hurt_by_are_not_taken_at_all()
    {
        // WW194's distinction, applied at the door rather than left to the refusal. A window
        // standing over the region and a backdrop transmitting through it are both about a copy of
        // the screen, and an off-screen render draws the visual tree with the compositor not
        // involved — so asking those questions of a render would be asking about the wrong picture.
        var path = Path.Combine(root, "rendered.png");
        var window = Window();

        var rendered = CaptureRoute.For(window, window);
        Assert.True(rendered.Renders, rendered.Sentence());

        var receipt = CaptureReceipt.Taking(path, window, Target(), Paint, route: rendered);

        Assert.Null(receipt.Over);
        Assert.Null(receipt.Glass);

        // The colour count still applies, because a flat rectangle is not a picture of a window
        // however it was got.
        Assert.NotNull(receipt.Colours);
    }

    [Fact]
    public void A_capture_the_door_took_of_one_flat_colour_is_refused_without_anyone_counting()
    {
        // The refusal firing on a reading the caller never took, which is the difference this task
        // is about: the same capture through Of with no colours passed is a receipt that refuses
        // nothing and says nobody asked.
        // Through a render route, so the region and the glass are not asked and the colour count
        // is the only question left. Asking all three would make this a case about whichever
        // refusal happened to fire first, which is a case about the desk.
        var path = Path.Combine(root, "flat.png");
        var window = Window();

        var refused = Assert.Throws<WrongCaptureException>(
            () => CaptureReceipt.Taking(
                path, window, Target(), Flat, route: CaptureRoute.For(window, window)));

        Assert.Contains("one colour across all", refused.Message, StringComparison.Ordinal);

        // And the file is there. A picture nobody may trust is still evidence about what went
        // wrong; what the refusal withdraws is the claim that it is a capture.
        Assert.True(File.Exists(path), "the door refused and left nothing to look at");
    }

    [Fact]
    public void A_take_that_wrote_nothing_is_not_a_capture_of_one_colour()
    {
        // The absence, told apart from a flat picture. Colours refuses a missing file rather than
        // answering about it, so the door asks only where there is something to ask about.
        var path = Path.Combine(root, "never-written.png");
        var window = Window();

        var receipt = CaptureReceipt.Taking(
            path, window, Target(), _ => { }, route: CaptureRoute.For(window, window));

        Assert.Null(receipt.Colours);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Nothing_may_be_left_out_by_passing_nothing()
    {
        var window = Window();
        var rendered = CaptureRoute.For(window, window);

        Assert.Throws<ArgumentNullException>(
            () => CaptureReceipt.Taking(Path.Combine(root, "x.png"), window, Target(), null!, route: rendered));
        Assert.Throws<ArgumentException>(
            () => CaptureReceipt.Taking("  ", window, Target(), Paint, route: rendered));
        Assert.Throws<ArgumentNullException>(
            () => CaptureReceipt.Taking(Path.Combine(root, "x.png"), null!, Target(), Paint, route: rendered));
    }

    /// <summary>Write a picture of exactly one colour, in a format with no alpha channel.</summary>
    private static void Flat(string path)
    {
        const int side = 16;
        var source = new System.Windows.Media.Imaging.WriteableBitmap(
            side, side, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);

        var stride = side * 4;
        var pixels = new byte[stride * side];
        for (var at = 0; at < pixels.Length; at += 4)
        {
            pixels[at] = 0x69;
            pixels[at + 1] = 0x69;
            pixels[at + 2] = 0x69;
            pixels[at + 3] = 0xFF;
        }

        source.WritePixels(new System.Windows.Int32Rect(0, 0, side, side), pixels, stride, 0);

        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(
            new System.Windows.Media.Imaging.FormatConvertedBitmap(
                source, System.Windows.Media.PixelFormats.Bgr24, null, 0)));

        using var file = File.Create(path);
        encoder.Save(file);
    }
}

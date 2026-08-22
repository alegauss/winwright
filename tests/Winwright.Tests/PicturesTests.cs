using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Winwright.Capturing;
using Winwright.InApp;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW35. A tree that failed to build, or one that was never arranged, renders as a rectangle of
/// transparent pixels — and a caller that checked only that a file was written cannot tell the two
/// apart.
/// <para>
/// A blank is rendered here on purpose and beside the drawing, because a check that has never seen
/// a blank cannot claim to tell one.
/// </para>
/// </summary>
public sealed class PicturesTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-blank-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private static T OnStaThread<T>(Func<T> work)
    {
        T? answer = default;
        Exception? threw = null;

        var thread = new Thread(() =>
        {
            try
            {
                answer = work();
            }
            catch (Exception broke)
            {
                threw = broke;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "the render thread did not finish");

        if (threw is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(threw).Throw();

        return answer!;
    }

    /// <summary>Something that draws: a sized element with a brush behind it.</summary>
    private static Border Drawing() =>
        new() { Name = "card", Width = 40, Height = 20, Background = new SolidColorBrush(Colors.Red) };

    /// <summary>The blank this check exists to tell: a sized element that paints nothing at all.</summary>
    private static Border Blank() => new() { Name = "blank", Width = 40, Height = 20 };

    /// <summary>
    /// The element is built inside the render thread, not handed to it: a FrameworkElement cannot
    /// be constructed off STA at all, so a factory is the only shape this helper can take.
    /// </summary>
    private string Rendered(Func<FrameworkElement> element, string name, Func<Brush>? background = null)
    {
        var path = Path.Combine(root, name);
        OnStaThread(() => Render.ToFile(element(), path, background: background?.Invoke()));
        return path;
    }

    [Fact]
    public void A_render_that_drew_nothing_is_a_blank()
    {
        var read = Pictures.Of(Rendered(Blank, "blank.png"));

        Assert.True(read.IsBlank);
        Assert.Equal(0, read.Drawn);
        Assert.Equal(800, read.Pixels);
        Assert.Contains("none of its 800 pixels carries an alpha of its own", read.Sentence());
    }

    [Fact]
    public void A_render_that_drew_something_carries_ink()
    {
        var read = Pictures.Of(Rendered(Drawing, "drawn.png"));

        Assert.True(read.HasInk);
        Assert.False(read.IsBlank);
        Assert.Equal(800, read.Drawn);
        Assert.Contains("carries ink: 800 of 800 pixels were drawn", read.Sentence());
    }

    [Fact]
    public void One_drawn_pixel_is_enough_to_stop_being_a_blank()
    {
        // The check is about whether anything drew, not about whether the screen is right — so a
        // mostly empty render is a picture, and saying otherwise would be a claim it cannot make.
        var path = Path.Combine(root, "corner.png");
        OnStaThread(() =>
        {
            var canvas = new Canvas { Width = 40, Height = 20 };
            var dot = new Border { Width = 1, Height = 1, Background = new SolidColorBrush(Colors.Black) };
            canvas.Children.Add(dot);
            return Render.ToFile(canvas, path);
        });

        var read = Pictures.Of(path);

        Assert.False(read.IsBlank);
        Assert.Equal(1, read.Drawn);
    }

    [Fact]
    public void A_blank_rendered_onto_a_background_is_no_longer_one_this_can_answer_for()
    {
        // Deliberate, and the boundary of this reading: composing a background makes every pixel
        // opaque whatever the tree did. Telling that one apart is the flat-colour check, not this.
        // The brush is built inside the render thread too — a Freezable belongs to the thread that
        // made it, which is the whole of WW73 and reproduces here the moment it is not.
        var read = Pictures.Of(Rendered(Blank, "onwhite.png", () => new SolidColorBrush(Colors.White)));

        Assert.False(read.IsBlank);
        Assert.Equal(read.Pixels, read.Drawn);
    }

    [Fact]
    public void A_picture_with_no_alpha_channel_is_unchecked_rather_than_passed()
    {
        var read = OnStaThread(() =>
        {
            var drawn = Render.ToBitmap(Drawing());
            return Pictures.Of(new FormatConvertedBitmap(drawn, PixelFormats.Bgr24, null, 0), "a converted render");
        });

        Assert.False(read.Tellable);
        Assert.False(read.IsBlank);
        Assert.False(read.HasInk);
        Assert.Contains("has no alpha channel", read.Sentence());

        var result = read.AsAssertion();
        Assert.Equal(AssertionOutcome.Unchecked, result.Outcome);
        Assert.Equal("an alpha channel to read", result.Missing?.Name);
    }

    [Fact]
    public void The_verdict_a_run_counts_is_a_pass_on_ink_and_a_red_on_a_blank()
    {
        var drawn = Pictures.Of(Rendered(Drawing, "verdict-drawn.png")).AsAssertion();
        var blank = Pictures.Of(Rendered(Blank, "verdict-blank.png")).AsAssertion();

        Assert.Equal(AssertionOutcome.Passed, drawn.Outcome);
        Assert.Equal(AssertionOutcome.Failed, blank.Outcome);
        Assert.Equal("the capture is not a blank", blank.Name);
    }

    [Fact]
    public void Insisting_refuses_a_blank_and_hands_back_the_reading_on_a_picture()
    {
        var blank = Rendered(Blank, "insist-blank.png");
        var drawn = Rendered(Drawing, "insist-drawn.png");

        var refused = Assert.Throws<BlankPictureException>(() => Pictures.Insist(blank));
        Assert.Contains("is a blank", refused.Message);

        Assert.True(Pictures.Insist(drawn).HasInk);
    }

    [Fact]
    public void A_capture_that_was_never_written_is_missing_and_not_blank()
    {
        // Two different findings with two different repairs, and a file that is not there reported
        // as a blank would send somebody looking at a render that never ran.
        Assert.Throws<FileNotFoundException>(() => Pictures.Of(Path.Combine(root, "never.png")));
    }

    [Fact]
    public void A_bitmap_in_hand_is_scanned_without_going_through_a_file()
    {
        var read = OnStaThread(() => Pictures.Of(Render.ToBitmap(Blank()), "the render"));

        Assert.True(read.IsBlank);
        Assert.StartsWith("the render is a blank", read.Sentence());
    }

    [Fact]
    public void The_scan_reads_the_file_on_the_thread_that_asked_for_it()
    {
        // Not run through the STA runner: a harness reading a capture off disk is not a thread
        // that owns a message queue, and a check that needed one would be a check nobody can call.
        Assert.Equal(ApartmentState.MTA, Thread.CurrentThread.GetApartmentState());

        Assert.True(Pictures.Of(Rendered(Drawing, "offthread.png")).HasInk);
    }
}

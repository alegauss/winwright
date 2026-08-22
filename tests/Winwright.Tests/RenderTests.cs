using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Winwright.InApp;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW71. The measure, the arrange, the update, the render target, the composed background and the
/// encoder are the same six steps in every project that wants a picture, and every project writes
/// all six again.
/// <para>
/// The arrange is the step that carries the defect, and it has a test of its own: a tree measured
/// and never arranged renders fully transparent at the right size, which reads as a drawing bug
/// and is a calling bug.
/// </para>
/// </summary>
public sealed class RenderTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-render-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    /// <summary>
    /// Run on a thread that owns a message queue. WW76 turns this into a shipped runner; here it
    /// is the smallest thing that lets a presentation object exist at all.
    /// </summary>
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

        // Rethrown as itself rather than wrapped: a refusal that arrives here as some other
        // exception is a refusal no test can assert the type of, which is most of their value.
        if (threw is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(threw).Throw();

        return answer!;
    }

    /// <summary>A panel with a known size and a known colour, which is a picture with known pixels.</summary>
    private static Border Card(double width = 40, double height = 20) => new()
    {
        Name = "card",
        Width = width,
        Height = height,
        Background = new SolidColorBrush(Colors.Red),
    };

    private string File(string name) => Path.Combine(root, name);

    [Fact]
    public void An_element_renders_to_a_png_the_size_it_asked_for()
    {
        var path = File("card.png");

        var picture = OnStaThread(() => Render.ToFile(Card(), path));

        Assert.Equal(path, picture.Path);
        Assert.Equal(40, picture.Width);
        Assert.Equal(20, picture.Height);
        Assert.Equal(40, picture.Pixels);
        Assert.Equal(20, picture.Lines);
        Assert.True(System.IO.File.Exists(path));
        Assert.True(new FileInfo(path).Length > 0, "the render wrote an empty file");
    }

    [Fact]
    public void The_arrange_happens_inside_so_the_picture_is_not_transparent_at_the_right_size()
    {
        // The defect this verb exists over. A caller that measured and forgot to arrange gets a
        // picture of the right size with nothing in it, which looks like the drawing being broken.
        var pixels = OnStaThread(() =>
        {
            var picture = Render.ToBitmap(Card());
            var bytes = new byte[picture.PixelWidth * picture.PixelHeight * 4];
            picture.CopyPixels(bytes, picture.PixelWidth * 4, 0);
            return bytes;
        });

        Assert.Contains(pixels.Where((_, index) => index % 4 == 3), alpha => alpha != 0);
    }

    [Fact]
    public void A_size_of_nothing_is_refused_rather_than_written_as_an_empty_file()
    {
        var path = File("nothing.png");

        var refused = Assert.Throws<UnrenderableException>(
            () => OnStaThread(() => Render.ToFile(new Border { Name = "empty" }, path)));

        Assert.Contains("there is nothing to render", refused.Message);
        Assert.Contains("only checks a file exists", refused.Message);
        Assert.False(System.IO.File.Exists(path), "a refused render still wrote a file");
    }

    [Fact]
    public void An_element_that_wants_all_the_room_it_is_given_is_refused_and_told_to_be_sized()
    {
        // A Grid with no children and no size measures to infinity against an infinite constraint,
        // which is not a size a picture can have — and rounding it to something would invent one.
        var refused = Assert.Throws<UnrenderableException>(
            () => OnStaThread(() => Render.ToBitmap(new Border { Child = new Grid(), Name = "stretchy" })));

        Assert.Contains("nothing to render", refused.Message);
    }

    [Fact]
    public void A_named_size_lays_the_element_out_at_that_size_rather_than_at_the_one_it_wants()
    {
        var picture = OnStaThread(() => Render.ToBitmap(Card(), new Size(100, 60)));

        Assert.Equal(100, picture.PixelWidth);
        Assert.Equal(60, picture.PixelHeight);
    }

    [Fact]
    public void The_resolution_scales_the_pixels_and_not_the_layout()
    {
        var picture = OnStaThread(() => Render.ToFile(Card(), File("high.png"), dpi: 192));

        Assert.Equal(40, picture.Width);
        Assert.Equal(20, picture.Height);
        Assert.Equal(80, picture.Pixels);
        Assert.Equal(40, picture.Lines);
        Assert.Equal(192, picture.Dpi);
    }

    [Fact]
    public void A_background_is_composed_behind_the_element_and_named_in_the_receipt()
    {
        var picture = OnStaThread(
            () => Render.ToFile(Card(), File("onblue.png"), new Size(60, 40), new SolidColorBrush(Colors.Blue)));

        Assert.Contains("#FF0000FF", picture.Background);
        Assert.Contains("on #FF0000FF", picture.Sentence());
    }

    [Fact]
    public void Where_the_tree_drew_nothing_the_background_is_what_is_there()
    {
        // The card is 40x20 inside a 60x40 render, so the bottom-right corner is background only.
        var corner = OnStaThread(() =>
        {
            var picture = Render.ToBitmap(Card(), new Size(60, 40), new SolidColorBrush(Colors.Blue));
            var pixel = new byte[4];
            picture.CopyPixels(new Int32Rect(59, 39, 1, 1), pixel, 4, 0);
            return pixel;
        });

        Assert.Equal(255, corner[0]);
        Assert.Equal(0, corner[1]);
        Assert.Equal(0, corner[2]);
        Assert.Equal(255, corner[3]);
    }

    [Fact]
    public void With_no_background_the_same_corner_is_transparent()
    {
        var corner = OnStaThread(() =>
        {
            var picture = Render.ToBitmap(Card(), new Size(60, 40));
            var pixel = new byte[4];
            picture.CopyPixels(new Int32Rect(59, 39, 1, 1), pixel, 4, 0);
            return pixel;
        });

        Assert.Equal(0, corner[3]);
    }

    [Fact]
    public void The_picture_is_frozen_so_it_can_cross_to_the_thread_that_wanted_it()
    {
        var picture = OnStaThread(() => Render.ToBitmap(Card()));

        Assert.True(picture.IsFrozen);

        // Read from this thread, which is not the one that drew it. Without the freeze this is the
        // exception that arrives instead, and it says nothing about which thread was wrong.
        Assert.Equal(40, picture.PixelWidth);
    }

    [Fact]
    public void A_render_from_a_thread_that_is_not_sta_is_refused_before_anything_is_drawn()
    {
        // The realistic shape: the element was made on the thread that owns the queue, and the
        // render is asked for from a worker. A FrameworkElement cannot even be constructed off
        // STA, so this is the only way the guard is ever reached — and reaching it is the point,
        // since what arrives otherwise says nothing about which thread was wrong.
        var made = OnStaThread<FrameworkElement>(() => new Border { Width = 10, Height = 10 });

        var refused = Assert.Throws<UnrenderableException>(() => Render.ToBitmap(made));

        Assert.Contains("is not STA", refused.Message);
    }

    [Fact]
    public void The_written_file_is_a_png_that_reads_back_at_the_size_it_claims()
    {
        var path = File("readback.png");
        OnStaThread(() => Render.ToFile(Card(80, 30), path, dpi: 144));

        var read = OnStaThread(() =>
        {
            var decoded = new PngBitmapDecoder(
                new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];
            return (decoded.PixelWidth, decoded.PixelHeight);
        });

        Assert.Equal(120, read.PixelWidth);
        Assert.Equal(45, read.PixelHeight);
    }

    [Fact]
    public void A_resolution_that_is_not_one_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OnStaThread(() => Render.ToBitmap(Card(), dpi: 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => OnStaThread(() => Render.ToBitmap(Card(), dpi: double.NaN)));
    }

    [Fact]
    public void The_directory_a_picture_goes_in_is_created_rather_than_demanded()
    {
        var path = Path.Combine(root, "runs", "first", "card.png");

        OnStaThread(() => Render.ToFile(Card(), path));

        Assert.True(System.IO.File.Exists(path));
    }
}

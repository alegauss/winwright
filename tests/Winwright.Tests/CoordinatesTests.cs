using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Winwright.InApp;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW121. Whoever touches the presentation stack first fixes the process awareness, it is set
/// once, and a library is always loaded afterwards — so the in-app half asks at load, because that
/// is the one case it can win, and reports what it actually got, which is the part that matters.
/// <para>
/// The defect does not fail. It agrees with whichever assembly got there first, which is why it
/// survived two shipped tasks and a whole suite passing: the orderings that happened to load the
/// engine first were green, and selecting the render tests beside the awareness tests was not.
/// </para>
/// </summary>
public sealed class CoordinatesTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-coordinates-").FullName;

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

    [Fact]
    public void The_in_app_package_asks_for_per_monitor_awareness_when_it_loads()
    {
        Assert.True(Coordinates.Trustworthy, Coordinates.Ensure());
    }

    [Fact]
    public void Asking_again_answers_that_it_was_already_set_rather_than_asking_twice()
    {
        var first = Coordinates.Ensure();

        Assert.Equal(first, Coordinates.Ensure());
        Assert.DoesNotContain("not per-monitor aware", first);
    }

    [Fact]
    public void A_render_after_the_host_declared_it_leaves_the_engine_still_per_monitor_aware()
    {
        // The reproduction. Undeclared by the host, touching WPF here fixed the awareness to
        // system first and the engine's own initializer could no longer change it.
        OnStaThread(() => Render.ToBitmap(
            new Border { Width = 10, Height = 10, Background = new SolidColorBrush(Colors.Red) }));

        Assert.Equal(DpiAwareness.PerMonitor, DisplayAwareness.Current());
    }

    [Fact]
    public void Every_receipt_carries_what_the_process_can_actually_see()
    {
        var picture = OnStaThread(
            () => Render.ToBitmap(new Border { Width = 10, Height = 10 }, new Size(10, 10)));

        Assert.Equal(10, picture.PixelWidth);

        // The reading nobody would have gone looking for, on every render rather than on a red:
        // a size drawn by a system-aware process does not mean what it says, and the file cannot
        // be told from one that does.
        var receipt = OnStaThread(
            () => Render.ToFile(new Border { Width = 10, Height = 10 }, Picture(), new Size(10, 10)));

        Assert.Equal("per-monitor aware", receipt.Sees);
        Assert.True(receipt.Trustworthy);
        Assert.Contains("this process is per-monitor aware", receipt.Sentence());
    }

    [Fact]
    public void A_receipt_that_never_read_the_awareness_does_not_claim_it()
    {
        // The default, so a receipt composed by hand cannot read as a trustworthy one.
        var handMade = new RenderedPicture("shot.png", 10, 10, 10, 10, 96, "nothing");

        Assert.False(handMade.Trustworthy);
        Assert.DoesNotContain("this process is", handMade.Sentence());
    }

    private string Picture() => Path.Combine(root, "coordinates.png");

    [Fact]
    public void Both_halves_agree_about_what_this_process_can_see()
    {
        // Two packages, two module initializers, one process. A disagreement here is a rectangle
        // that means one thing to the harness and another to the application it is driving.
        Assert.Equal(DisplayAwareness.Current() == DpiAwareness.PerMonitor, Coordinates.Trustworthy);
    }

    [Fact]
    public void A_rendered_size_is_in_the_space_the_window_lives_in()
    {
        // What the awareness is for: an unaware process is handed virtualised numbers, so a render
        // asked for at 200 device-independent units would not come back as 200 pixels at 96 dpi.
        var picture = OnStaThread(
            () => Render.ToBitmap(new Border { Width = 200, Height = 100 }, new Size(200, 100)));

        Assert.Equal(200, picture.PixelWidth);
        Assert.Equal(100, picture.PixelHeight);
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

using Winwright.Capturing;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

using InAppSurfaces = Winwright.InApp.Surfaces;

namespace Winwright.Tests;

/// <summary>
/// WW44. Verifying one task in claude-tray cost three captures and a full-screen grab, and none of
/// the three failed: the script reported success, named the right window each time, and the file
/// simply did not contain the note the flag exists to show.
/// <para>
/// The last test is that failure reproduced end to end — a real element reporting a real rectangle,
/// and a copy rectangle that honestly does not contain it.
/// </para>
/// </summary>
public sealed class ContainmentTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-contains-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private static ReportedSurface Surface(int left, int top, int right, int bottom, string name = "the report page") =>
        new(name, new WindowBounds(left, top, right, bottom));

    private static readonly WindowBounds Copy = new(0, 0, 600, 400);

    [Fact]
    public void A_surface_wholly_inside_the_copy_is_the_pass()
    {
        var read = Containment.Of(Copy, Surface(10, 20, 300, 200));

        Assert.True(read.Contains);
        Assert.Equal(Sits.Inside, read.Sits);
        Assert.Contains("the capture contains 'the report page'", read.Sentence());
        Assert.Equal(AssertionOutcome.Passed, read.AsAssertion("the note is in the picture").Outcome);
    }

    [Fact]
    public void A_surface_exactly_filling_the_copy_is_inside_it()
    {
        // The edges are exclusive on both, so a surface flush with the copy is contained rather
        // than one pixel out — which is the off-by-one this would otherwise report forever.
        Assert.True(Containment.Of(Copy, Surface(0, 0, 600, 400)).Contains);
    }

    [Fact]
    public void A_surface_the_copy_clipped_says_which_sides_and_by_how_much()
    {
        var read = Containment.Of(Copy, Surface(-12, 30, 620, 380));

        Assert.Equal(Sits.Clipped, read.Sits);
        Assert.Equal(12, read.OverLeft);
        Assert.Equal(20, read.OverRight);
        Assert.Equal(0, read.OverTop);
        Assert.Equal(0, read.OverBottom);
        Assert.Contains("sticks out of 600x400 at 0,0 by left 12, right 20", read.Sentence());
    }

    [Fact]
    public void A_surface_nowhere_near_the_copy_is_a_different_repair_and_says_so()
    {
        // Told apart from a clip on purpose: a surface clipped by eleven rows is a copy rectangle
        // that is too small, and one nowhere near it is a picture of the wrong window.
        var read = Containment.Of(Copy, Surface(900, 500, 1200, 700));

        Assert.Equal(Sits.Elsewhere, read.Sits);
        Assert.Contains("does not touch", read.Sentence());
        Assert.Contains("a picture of something else", read.Sentence());
    }

    [Fact]
    public void A_surface_touching_only_at_a_corner_is_still_a_clip_and_not_elsewhere()
    {
        var read = Containment.Of(Copy, Surface(599, 399, 700, 500));

        Assert.Equal(Sits.Clipped, read.Sits);
    }

    [Fact]
    public void A_surface_flush_against_the_outside_edge_touches_nothing()
    {
        // Right is exclusive, so a surface starting at 600 shares no pixel with a copy ending there.
        Assert.Equal(Sits.Elsewhere, Containment.Of(Copy, Surface(600, 0, 700, 100)).Sits);
    }

    [Fact]
    public void A_rectangle_of_no_area_is_its_own_finding_rather_than_being_outside()
    {
        var read = Containment.Of(Copy, Surface(10, 10, 10, 200));

        Assert.Equal(Sits.Nothing, read.Sits);
        Assert.False(read.Contains);
        Assert.Contains("which has no area", read.Sentence());
        Assert.Contains("no copy contains a rectangle nothing occupies", read.Sentence());
    }

    [Fact]
    public void A_capture_that_does_not_contain_the_surface_is_a_red_under_the_name_the_case_gave_it()
    {
        var result = Containment.Of(Copy, Surface(900, 500, 1200, 700)).AsAssertion("the note is in the picture");

        Assert.Equal(AssertionOutcome.Failed, result.Outcome);
        Assert.Equal("the note is in the picture", result.Name);
    }

    [Fact]
    public void A_surface_nothing_ever_reported_is_a_hole_and_not_a_red()
    {
        var path = Path.Combine(root, "surfaces.tsv");
        File.WriteAllText(path, "the report page\t10\t20\t130\t80\n");

        var result = SurfaceReport.Of(path, "the settings flyout").Within(Copy, "the flyout is in the picture");

        // The check never ran: nothing said where the flyout was, so calling it a failure would
        // blame the window for something the harness did not observe.
        Assert.Equal(AssertionOutcome.Unchecked, result.Outcome);
        Assert.True(result.DidNotRun);
        Assert.Equal("a reported surface named 'the settings flyout'", result.Missing?.Name);
    }

    [Fact]
    public void A_surface_the_report_does_carry_is_asserted_against_the_copy()
    {
        var path = Path.Combine(root, "surfaces.tsv");
        File.WriteAllText(path, "the report page\t10\t20\t130\t80\n");

        var inside = SurfaceReport.Of(path, "the report page").Within(Copy, "the note is in the picture");
        var outside = SurfaceReport.Of(path, "the report page")
            .Within(new WindowBounds(200, 200, 400, 400), "the note is in the picture");

        Assert.Equal(AssertionOutcome.Passed, inside.Outcome);
        Assert.Equal(AssertionOutcome.Failed, outside.Outcome);
    }

    [Fact]
    public void The_claude_tray_failure_reproduced_end_to_end()
    {
        // A real element on a real presentation source reports a real rectangle, and the copy that
        // was taken does not contain it. This is the run that reported success three times.
        var path = Path.Combine(root, "live.tsv");
        var reported = OnASurface(element => InAppSurfaces.ReportTo(path, "the note", element));
        Assert.NotNull(reported);

        // The window was photographed at the origin, and the note is at 40,30 on the desktop.
        var elsewhere = new WindowBounds(0, 0, 20, 20);

        var result = SurfaceReport.Of(path, "the note").Within(elsewhere, "the note is in the picture");

        Assert.Equal(AssertionOutcome.Failed, result.Outcome);
        Assert.Contains("the note", result.Detail);
        Assert.DoesNotContain("Passed", result.Detail);
    }

    private static T OnASurface<T>(Func<FrameworkElement, T> work)
    {
        T? answer = default;
        Exception? threw = null;

        var thread = new Thread(() =>
        {
            try
            {
                var element = new Border { Width = 120, Height = 60, Background = new SolidColorBrush(Colors.Red) };
                using var source = new HwndSource(
                    new HwndSourceParameters("winwright containment")
                    {
                        PositionX = 40,
                        PositionY = 30,
                        Width = 120,
                        Height = 60,
                    })
                {
                    RootVisual = element,
                };

                element.UpdateLayout();
                answer = work(element);
            }
            catch (Exception broke)
            {
                threw = broke;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "the reporting thread did not finish");

        if (threw is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(threw).Throw();

        return answer!;
    }
}

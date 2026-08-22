using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

using Winwright.Capturing;
using Winwright.InApp;
using Winwright.Verdicts;

using Xunit;

using InAppSurfaces = Winwright.InApp.Surfaces;

namespace Winwright.Tests;

/// <summary>
/// WW74. The application knows which rectangle it just painted and nothing has ever asked it, so a
/// harness in another process can only guess — and a guess about a popup or a page that scrolled
/// is a capture asserted against a rectangle nobody drew.
/// <para>
/// The two halves cannot reference each other, so the format is the contract. Both sides are
/// exercised here against one file, which is the only place the two can be caught disagreeing.
/// </para>
/// </summary>
public sealed class SurfaceReportTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-surfaces-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private string File(string name = "surfaces.tsv") => Path.Combine(root, name);

    /// <summary>
    /// An element on a real presentation source, which is what makes PointToScreen answer at all.
    /// A window is never shown: the source is the screen as far as the conversion is concerned.
    /// </summary>
    private static T OnASurface<T>(Func<FrameworkElement, T> work, double width = 120, double height = 60) =>
        Apartment.Run(() =>
        {
            var element = new Border
            {
                Width = width,
                Height = height,
                Background = new SolidColorBrush(Colors.Red),
            };

            using var source = new HwndSource(
                new HwndSourceParameters("winwright surfaces")
                {
                    PositionX = 40,
                    PositionY = 30,
                    Width = (int)width,
                    Height = (int)height,
                })
            {
                RootVisual = element,
            };

            element.UpdateLayout();
            return work(element);
        });

    [Fact]
    public void What_the_application_drew_is_written_as_a_name_and_four_numbers()
    {
        var path = File();

        var (reported, scale) = OnASurface(element =>
            (InAppSurfaces.ReportTo(path, "the report page", element), VisualTreeHelper.GetDpi(element)));

        Assert.NotNull(reported);
        Assert.Equal("the report page", reported.Name);

        // Scaled, not 120x60: this machine runs at 200 percent, and the first draft of this test
        // asserted the layout units — which is the mistake the format exists to make impossible.
        Assert.Equal((int)Math.Round(120 * scale.DpiScaleX), reported.Width);
        Assert.Equal((int)Math.Round(60 * scale.DpiScaleY), reported.Height);

        var line = System.IO.File.ReadAllText(path).TrimEnd('\n');
        Assert.Equal(5, line.Split('\t').Length);
        Assert.StartsWith("the report page\t", line);
    }

    [Fact]
    public void The_reader_finds_the_surface_the_writer_reported()
    {
        var path = File();
        var written = OnASurface(element => InAppSurfaces.ReportTo(path, "the report page", element));

        var read = SurfaceReport.Of(path, "the report page");

        // Compared against what the writer said rather than against a number typed here: the two
        // halves agreeing is the claim, and a literal would only assert this machine's scaling.
        Assert.True(read.Reported);
        Assert.Equal(written!.Left, read.Surface!.Bounds.Left);
        Assert.Equal(written.Top, read.Surface.Bounds.Top);
        Assert.Equal(written.Width, read.Surface.Bounds.Width);
        Assert.Equal(written.Height, read.Surface.Bounds.Height);
        Assert.Contains("the application reported 'the report page'", read.Sentence());
    }

    [Fact]
    public void The_rectangle_is_in_physical_pixels_and_not_in_layout_units()
    {
        // The whole of the format decision. PointToScreen answers in the space a copy works in,
        // so on a scaled display the reported width is the scaled one and the layout width is not.
        var (reported, layoutWidth) = OnASurface(element =>
            (InAppSurfaces.Measure("the report page", element), element.ActualWidth));

        var scale = Apartment.Run(() => VisualTreeHelper.GetDpi(new Border()).DpiScaleX);

        Assert.NotNull(reported);
        Assert.Equal((int)Math.Round(layoutWidth * scale), reported.Width);
    }

    [Fact]
    public void A_surface_redrawn_moves_because_the_last_line_wins()
    {
        var path = File();
        OnASurface(element => InAppSurfaces.ReportTo(path, "the report page", element));
        var moved = OnASurface(element => InAppSurfaces.ReportTo(path, "the report page", element), width: 200);

        var read = SurfaceReport.Of(path, "the report page");

        // A reader taking the first line would assert a capture against where the page used to be.
        Assert.Equal(moved!.Width, read.Surface!.Bounds.Width);
        Assert.Single(SurfaceReport.Read(path));
    }

    [Fact]
    public void An_element_on_no_screen_reports_nothing_and_raises_nothing()
    {
        // The never-reported arm at its source: an off-screen element has no screen rectangle, and
        // inventing one from the layout would report a rectangle at the top-left of nothing.
        var reported = Apartment.Run(() => InAppSurfaces.Measure("the report page", new Border { Width = 40, Height = 20 }));

        Assert.Null(reported);
    }

    [Fact]
    public void A_surface_nobody_reported_is_a_hole_and_never_a_failure()
    {
        var path = File();
        OnASurface(element => InAppSurfaces.ReportTo(path, "the report page", element));

        var read = SurfaceReport.Of(path, "the settings flyout");

        Assert.False(read.Reported);
        Assert.Contains("never reported 'the settings flyout'", read.Sentence());
        Assert.Contains("reports the report page", read.Sentence());

        var precondition = read.AsPrecondition();
        Assert.False(precondition.Satisfied);
        Assert.Equal("a reported surface named 'the settings flyout'", precondition.Name);
    }

    [Fact]
    public void A_report_that_was_never_written_is_the_same_arm_and_not_a_throw()
    {
        var read = SurfaceReport.Of(File("never.tsv"), "the report page");

        Assert.False(read.Reported);
        Assert.Contains("there is no report at", read.Sentence());
    }

    [Fact]
    public void A_half_written_report_keeps_the_surfaces_before_the_truncation_and_counts_the_rest()
    {
        var path = File();
        System.IO.File.WriteAllText(path, "the report page\t10\t20\t130\t80\nthe settings fly");

        var read = SurfaceReport.Read(path, out var unreadable);

        // An application that was killed mid-write still reported everything before it died.
        Assert.Single(read);
        Assert.Equal(1, unreadable);
        Assert.Contains("did not parse", SurfaceReport.Of(path, "the settings flyout").Sentence());
    }

    [Fact]
    public void A_name_that_could_forge_a_line_is_refused_at_the_source()
    {
        var refused = Assert.Throws<ArgumentException>(
            () => OnASurface(element => InAppSurfaces.ReportTo(File(), "page\t10\t20\t30\t40", element)));

        Assert.Contains("can report a rectangle nothing drew", refused.Message);
    }

    [Fact]
    public void A_name_with_spaces_survives_because_the_fields_are_tabs()
    {
        var path = File();
        OnASurface(element => InAppSurfaces.ReportTo(path, "the sessions page, expanded", element));

        Assert.True(SurfaceReport.Of(path, "the sessions page, expanded").Reported);
    }

    [Fact]
    public void The_two_halves_name_the_same_variable()
    {
        // The one thing two packages that cannot reference each other can still get wrong.
        Assert.Equal(SurfaceReport.PathVariable, InAppSurfaces.PathVariable);
    }

    [Fact]
    public void An_application_nobody_asked_to_report_writes_nothing_at_all()
    {
        // Unset means report nowhere: an application shipped to its users is not under test, and
        // one writing files because it once was is worse than one that never reported.
        Assert.Null(Environment.GetEnvironmentVariable(InAppSurfaces.PathVariable));
        Assert.Null(InAppSurfaces.Where());
        Assert.Null(OnASurface(element => InAppSurfaces.Report("the report page", element)));
    }

    [Fact]
    public void A_line_that_is_not_a_surface_parses_to_nothing_rather_than_to_a_rectangle()
    {
        Assert.Null(SurfaceReport.Parse("the report page\t10\t20\t130"));
        Assert.Null(SurfaceReport.Parse("the report page\t10\t20\t130\tnot-a-number"));
        Assert.Null(SurfaceReport.Parse("\t10\t20\t130\t80"));
        Assert.NotNull(SurfaceReport.Parse("the report page\t10\t20\t130\t80"));
    }

    [Fact]
    public void A_reported_surface_a_check_did_find_is_a_precondition_that_is_met()
    {
        var path = File();
        OnASurface(element => InAppSurfaces.ReportTo(path, "the report page", element));

        var precondition = SurfaceReport.Of(path, "the report page").AsPrecondition();

        Assert.True(precondition.Satisfied);
        Assert.Equal("", precondition.Absence);
    }
}

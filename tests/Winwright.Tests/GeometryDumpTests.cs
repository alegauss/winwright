using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

using Winwright.Capturing;
using Winwright.InApp;

using Xunit;

using InAppGeometry = Winwright.InApp.Geometry;

namespace Winwright.Tests;

/// <summary>
/// WW77. An installer page, a custom-drawn control or an immediate-mode surface has no
/// accessibility tree, so the only check available for one is reading its source — which misses
/// the caption that wrapped, the page that rendered above blank space and the button nine pixels
/// out of place.
/// <para>
/// Both halves are exercised against one file, because two packages that cannot reference each
/// other can only be caught disagreeing about the format there.
/// </para>
/// </summary>
public sealed class GeometryDumpTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-geometry-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private string File(string name = "geometry.tsv") => Path.Combine(root, name);

    /// <summary>A page with a stack of two rows in it, on a real presentation source.</summary>
    private static T OnAPage<T>(Func<Grid, T> work) => Apartment.Run(
        () =>
        {
            var page = new Grid { Name = "page", Width = 200, Height = 100 };
            var rows = new StackPanel { Name = "rows" };
            rows.Children.Add(new Border { Name = "header", Height = 30, Background = new SolidColorBrush(Colors.Red) });
            rows.Children.Add(new Border { Name = "body", Height = 40, Background = new SolidColorBrush(Colors.Blue) });
            page.Children.Add(rows);

            using var source = new HwndSource(
                new HwndSourceParameters("winwright geometry")
                {
                    PositionX = 50,
                    PositionY = 60,
                    Width = 200,
                    Height = 100,
                })
            {
                RootVisual = page,
            };

            page.UpdateLayout();
            return work(page);
        },
        named: "the geometry dump");

    [Fact]
    public void What_was_laid_out_is_dumped_with_the_root_first()
    {
        var dumped = OnAPage(page => InAppGeometry.Of(page));

        Assert.Equal(0, dumped.Elements[0].Depth);
        Assert.Equal("page", dumped.Elements[0].Name);
        Assert.Contains(dumped.Elements, one => one.Name == "header");
        Assert.Contains(dumped.Elements, one => one.Name == "body");
    }

    [Fact]
    public void The_depth_carries_the_tree_a_flat_file_would_lose()
    {
        var byName = OnAPage(page => InAppGeometry.Of(page).Elements.ToDictionary(one => one.Name, one => one.Depth));

        Assert.Equal(0, byName["page"]);
        Assert.True(byName["rows"] > byName["page"]);
        Assert.True(byName["header"] > byName["rows"]);
    }

    [Fact]
    public void Rectangles_are_in_physical_pixels_and_children_sit_inside_their_parent()
    {
        // Half of what a geometry check is for: a child against the thing containing it. Both
        // rectangles are on screen, so this is one comparison and not a coordinate conversion.
        var (page, header) = OnAPage(walked =>
        {
            var found = InAppGeometry.Of(walked).Elements;
            return (found[0], found.Single(one => one.Name == "header"));
        });

        Assert.True(header.Left >= page.Left && header.Right <= page.Right, $"{header} is not inside {page}");
        Assert.True(header.Top >= page.Top && header.Bottom <= page.Bottom, $"{header} is not inside {page}");
        Assert.True(page.Left >= 50, "the rectangle is not on the desktop where the window was put");
    }

    [Fact]
    public void The_reader_finds_what_the_writer_dumped()
    {
        var path = File();
        var written = OnAPage(page => InAppGeometry.DumpTo(path, page));

        var read = GeometryDump.Read(path);

        Assert.Equal(written.Elements.Count, read.Elements.Count);
        Assert.Equal("page", read.Root!.Name);
        Assert.Single(read.Named("header"));
        Assert.Equal(written.Elements[0].Left, read.Root.Bounds.Left);
    }

    [Fact]
    public void A_dump_replaces_the_file_because_two_layouts_in_one_are_a_tree_that_never_existed()
    {
        var path = File();
        OnAPage(page => InAppGeometry.DumpTo(path, page));
        var second = OnAPage(page => InAppGeometry.DumpTo(path, page));

        Assert.Equal(second.Elements.Count, GeometryDump.Read(path).Elements.Count);
    }

    [Fact]
    public void A_walk_that_ran_out_says_how_much_it_did_not_reach()
    {
        var path = File();
        var dumped = OnAPage(page => InAppGeometry.DumpTo(path, page, depth: 1));

        Assert.True(dumped.Elided > 0, "a walk one level deep reached everything, which this page does not allow");
        Assert.Contains("not walked because the walk ran out", dumped.Sentence());

        var read = GeometryDump.Read(path);
        Assert.Equal(dumped.Elided, read.Elided);
        Assert.Contains("not walked", read.Sentence());
        Assert.Contains("... ", read.Render()[^1]);
    }

    [Fact]
    public void A_width_that_ran_out_is_counted_too()
    {
        var dumped = OnAPage(page => InAppGeometry.Of(page, width: 1));

        Assert.True(dumped.Elided >= 1);
    }

    [Fact]
    public void An_element_on_no_screen_is_left_out_rather_than_given_a_rectangle_at_the_origin()
    {
        // A dump full of rectangles at 0,0 is worse than a shorter one: every geometry check would
        // then read a stack of elements perfectly overlapping in the corner.
        var dumped = Apartment.Run(() => InAppGeometry.Of(new Border { Width = 40, Height = 20 }));

        Assert.Empty(dumped.Elements);
        Assert.Equal("0 element(s) drawn.", dumped.Sentence());
    }

    [Fact]
    public void An_element_that_measured_nothing_is_reported_and_not_hidden()
    {
        // "Nothing measures zero" is a check somebody else makes, and it cannot be made against a
        // dump that quietly left the zero-sized element out.
        var dumped = Apartment.Run(
            () =>
            {
                var page = new Grid { Name = "page", Width = 200, Height = 100 };
                page.Children.Add(new Border { Name = "empty", Width = 0, Height = 0 });

                using var source = new HwndSource(
                    new HwndSourceParameters("winwright geometry") { Width = 200, Height = 100 })
                {
                    RootVisual = page,
                };

                page.UpdateLayout();
                return InAppGeometry.Of(page);
            },
            named: "the empty element");

        var empty = dumped.Elements.Single(one => one.Name == "empty");
        Assert.False(empty.Drawn);
        Assert.Equal(0, empty.Width);
    }

    [Fact]
    public void A_dump_that_was_never_written_reads_as_nothing_drawn()
    {
        var read = GeometryDump.Read(File("never.tsv"));

        Assert.Empty(read.Elements);
        Assert.Null(read.Root);
        Assert.Equal("the dump reports nothing drawn.", read.Sentence());
    }

    [Fact]
    public void A_line_that_is_not_an_element_is_counted_rather_than_thrown_over()
    {
        var path = File();
        System.IO.File.WriteAllText(path, "0\tGrid\tpage\t10\t20\t210\t120\nnot a line at all\n");

        var read = GeometryDump.Read(path);

        Assert.Single(read.Elements);
        Assert.Equal(1, read.Unreadable);
        Assert.Contains("1 line(s) that did not parse", read.Sentence());
    }

    [Fact]
    public void A_line_missing_a_field_or_carrying_a_bad_number_parses_to_nothing()
    {
        Assert.Null(GeometryDump.Parse("0\tGrid\tpage\t10\t20\t210"));
        Assert.Null(GeometryDump.Parse("0\tGrid\tpage\t10\t20\t210\tnope"));
        Assert.Null(GeometryDump.Parse("-1\tGrid\tpage\t10\t20\t210\t120"));
        Assert.Null(GeometryDump.Parse("0\t\tpage\t10\t20\t210\t120"));
        Assert.NotNull(GeometryDump.Parse("0\tGrid\tpage\t10\t20\t210\t120"));
    }

    [Fact]
    public void An_element_with_no_name_still_has_a_kind_to_be_read_by()
    {
        var read = GeometryDump.Parse("1\tBorder\t\t10\t20\t210\t120");

        Assert.NotNull(read);
        Assert.Equal("Border", read.Kind);
        Assert.Equal("", read.Name);
        Assert.StartsWith("Border 200x100", read.ToString());
    }

    [Fact]
    public void The_two_halves_name_the_same_variable_and_the_same_marker()
    {
        Assert.Equal(GeometryDump.PathVariable, InAppGeometry.PathVariable);
        Assert.Equal(GeometryDump.ElidedMarker, InAppGeometry.ElidedMarker);
    }

    [Fact]
    public void The_tree_renders_indented_by_the_depth_it_carried()
    {
        var path = File();
        OnAPage(page => InAppGeometry.DumpTo(path, page));

        var rendered = GeometryDump.Read(path).Render();

        Assert.StartsWith("Grid 'page'", rendered[0]);
        Assert.All(rendered.Skip(1), line => Assert.StartsWith("  ", line));
    }
}

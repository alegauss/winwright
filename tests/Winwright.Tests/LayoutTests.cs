using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

using Winwright.Asserting;
using Winwright.Capturing;
using Winwright.InApp;
using Winwright.Verdicts;

using Xunit;

using InAppGeometry = Winwright.InApp.Geometry;

namespace Winwright.Tests;

/// <summary>
/// WW51. One installer page was built four times and verified every time by reading the script,
/// and the failures that misses are the ones it had already produced: a caption that wrapped at
/// column zero, a page above a screenful of blank space, and a button nine pixels below the box it
/// belongs to — because an edit sizes itself to its font and a button does not.
/// <para>
/// Every expectation here comes off the dump. Nothing in a case names a number, so nothing goes
/// stale when the page is redesigned.
/// </para>
/// </summary>
public sealed class LayoutTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-layout-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    /// <summary>A dump written by hand, which is how a wrong layout is produced on demand.</summary>
    private static ReadGeometry Dump(params string[] lines)
    {
        var read = lines.Select(GeometryDump.Parse).Where(one => one is not null).Select(one => one!).ToList();
        return new ReadGeometry(read, 0, 0);
    }

    private static string Line(int depth, string kind, string name, int left, int top, int right, int bottom) =>
        string.Join('\t', depth, kind, name, left, top, right, bottom);

    [Fact]
    public void A_page_whose_children_sit_inside_it_and_beside_each_other_holds()
    {
        var read = Layout.Of(Dump(
            Line(0, "Grid", "page", 0, 0, 200, 100),
            Line(1, "Border", "header", 0, 0, 200, 30),
            Line(1, "Border", "body", 0, 30, 200, 100)));

        Assert.True(read.Held);
        Assert.Equal(3, read.Examined);
        Assert.Contains("3 element(s) laid out correctly", read.Sentence());
        Assert.Equal(AssertionOutcome.Passed, read.AsAssertion().Outcome);
    }

    [Fact]
    public void A_button_nine_pixels_below_the_box_it_belongs_to_is_found()
    {
        // The installer failure, as geometry: an edit sizes itself to its font and a button does
        // not, so the button ends past the row holding both of them.
        var read = Layout.Of(Dump(
            Line(0, "Grid", "page", 0, 0, 200, 100),
            Line(1, "StackPanel", "row", 0, 0, 200, 40),
            Line(2, "TextBox", "path", 0, 0, 150, 40),
            Line(2, "Button", "browse", 150, 0, 200, 49)));

        var fault = Assert.Single(read.Faults);
        Assert.Equal(Fault.EndsOutside, fault.Kind);
        Assert.Equal("browse", fault.What.Name);
        Assert.Equal("row", fault.Against!.Name);
        Assert.Contains("bottom 9", fault.Detail);
    }

    [Fact]
    public void A_caption_that_wrapped_at_column_zero_measures_nothing_and_says_so()
    {
        var read = Layout.Of(Dump(
            Line(0, "Grid", "page", 0, 0, 200, 100),
            Line(1, "TextBlock", "caption", 10, 10, 10, 30)));

        var fault = Assert.Single(read.Faults);
        Assert.Equal(Fault.MeasuresNothing, fault.Kind);
        Assert.Contains("was laid out and occupies nothing", fault.Detail);
    }

    [Fact]
    public void A_child_starting_left_of_its_parent_is_found_and_measured()
    {
        var read = Layout.Of(Dump(
            Line(0, "Grid", "page", 20, 20, 200, 100),
            Line(1, "Border", "strip", 8, 12, 180, 60)));

        var fault = Assert.Single(read.Faults, one => one.Kind == Fault.StartsOutside);
        Assert.Contains("left 12, top 8", fault.Detail);
    }

    [Fact]
    public void Two_children_of_one_parent_covering_the_same_pixels_are_found()
    {
        var read = Layout.Of(Dump(
            Line(0, "Grid", "page", 0, 0, 200, 100),
            Line(1, "Border", "header", 0, 0, 200, 40),
            Line(1, "Border", "body", 0, 30, 200, 100)));

        var fault = Assert.Single(read.Faults, one => one.Kind == Fault.Overlaps);
        Assert.Equal("header", fault.What.Name);
        Assert.Equal("body", fault.Against!.Name);
        Assert.Contains("200x10 at 0,30", fault.Detail);
    }

    [Fact]
    public void Children_of_different_parents_are_not_siblings_and_do_not_overlap_each_other()
    {
        // Two panels side by side, each with a child at the same depth. Comparing across them
        // would report an overlap between elements that are in no relation at all.
        var read = Layout.Of(Dump(
            Line(0, "Grid", "page", 0, 0, 200, 100),
            Line(1, "StackPanel", "left", 0, 0, 100, 100),
            Line(2, "Border", "a", 0, 0, 100, 50),
            Line(1, "StackPanel", "right", 100, 0, 200, 100),
            Line(2, "Border", "b", 100, 0, 200, 50)));

        Assert.True(read.Held, read.Sentence());
    }

    [Fact]
    public void Overlap_is_the_one_a_case_can_narrow_away()
    {
        // Two children of one panel covering the same pixels is a defect on an installer page and
        // ordinary on a window with an overlay, and this tool does not get to decide which it is.
        var read = Layout.Of(Dump(
            Line(0, "Grid", "page", 0, 0, 200, 100),
            Line(1, "Border", "content", 0, 0, 200, 100),
            Line(1, "Border", "overlay", 0, 0, 200, 100)));

        Assert.False(read.Held);
        Assert.True(read.Only(Fault.EndsOutside, Fault.StartsOutside, Fault.MeasuresNothing).Held);
    }

    [Fact]
    public void Narrowing_to_no_fault_at_all_is_refused()
    {
        var read = Layout.Of(Dump(Line(0, "Grid", "page", 0, 0, 200, 100)));

        var refused = Assert.Throws<ArgumentException>(() => read.Only());

        Assert.Contains("holds against every layout there is", refused.Message);
    }

    [Fact]
    public void A_dump_with_nothing_in_it_is_unchecked_and_never_a_pass()
    {
        var read = Layout.Of(Dump());

        Assert.False(read.Held);
        Assert.Equal(0, read.Examined);
        Assert.Equal("there was no geometry to check.", read.Sentence());

        var result = read.AsAssertion();
        Assert.Equal(AssertionOutcome.Unchecked, result.Outcome);
        Assert.Equal("a geometry dump to check", result.Missing?.Name);
    }

    [Fact]
    public void A_page_above_a_screenful_of_blank_space_is_the_reading_the_four_faults_cannot_make()
    {
        // Correct in every relation: the content is inside the page, nothing overlaps, nothing
        // measures zero. And the thing anybody looking would see first is the empty two thirds.
        var read = Layout.Of(Dump(
            Line(0, "Grid", "page", 0, 0, 200, 300),
            Line(1, "StackPanel", "rows", 0, 0, 200, 100)));

        Assert.True(read.Held);
        Assert.Equal(200, read.BlankBelow);

        var result = read.FillsAtLeast(0.8, "the page fills its surface");
        Assert.Equal(AssertionOutcome.Failed, result.Outcome);
        Assert.Contains("33.3% of", result.Detail);
        Assert.Contains("leaving 200 row(s) blank below it", result.Detail);
        Assert.Contains("this case asks for 80%", result.Detail);
    }

    [Fact]
    public void A_page_that_does_fill_its_surface_passes_the_same_reading()
    {
        var read = Layout.Of(Dump(
            Line(0, "Grid", "page", 0, 0, 200, 300),
            Line(1, "StackPanel", "rows", 0, 0, 200, 290)));

        Assert.Equal(AssertionOutcome.Passed, read.FillsAtLeast(0.9).Outcome);
    }

    [Fact]
    public void The_fraction_a_case_asks_for_has_to_be_one()
    {
        var read = Layout.Of(Dump(Line(0, "Grid", "page", 0, 0, 200, 300)));

        Assert.Throws<ArgumentOutOfRangeException>(() => read.FillsAtLeast(-0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => read.FillsAtLeast(1.5));
    }

    [Fact]
    public void Nothing_dumped_makes_the_fill_reading_a_hole_rather_than_a_red()
    {
        var result = Layout.Of(Dump()).FillsAtLeast(0.8);

        Assert.Equal(AssertionOutcome.Unchecked, result.Outcome);
        Assert.Equal("a surface with a height", result.Missing?.Name);
    }

    [Fact]
    public void A_real_page_laid_out_by_wpf_holds_against_its_own_dump()
    {
        // Against a live layout rather than a hand-written one: a check proved only against a file
        // somebody typed is a check that proves the file.
        var path = Path.Combine(root, "live.tsv");
        Apartment.Run(
            () =>
            {
                var page = new Grid { Name = "page", Width = 200, Height = 100 };
                var rows = new StackPanel { Name = "rows" };
                rows.Children.Add(new Border { Name = "header", Height = 30, Background = new SolidColorBrush(Colors.Red) });
                rows.Children.Add(new Border { Name = "body", Height = 40, Background = new SolidColorBrush(Colors.Blue) });
                page.Children.Add(rows);

                using var source = new HwndSource(
                    new HwndSourceParameters("winwright layout") { Width = 200, Height = 100 })
                {
                    RootVisual = page,
                };

                page.UpdateLayout();
                return InAppGeometry.DumpTo(path, page);
            },
            named: "the layout dump");

        var read = Layout.Of(path);

        Assert.True(read.Examined > 3, read.Sentence());
        Assert.DoesNotContain(read.Faults, one => one.Kind != Fault.MeasuresNothing);
    }
}

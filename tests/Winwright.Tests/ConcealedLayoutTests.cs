using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

using Winwright.Asserting;
using Winwright.Capturing;
using Winwright.InApp;

using Xunit;

using InAppGeometry = Winwright.InApp.Geometry;

namespace Winwright.Tests;

/// <summary>
/// WW130. Found by running the layout check against a real window rather than against a dump
/// somebody typed. The fixture's loading note is collapsed unless a run asks for it, so it lays out
/// to nothing — correctly, deliberately, and on every page that hides anything. The check reported
/// it as an element laid out to no size, which is the fault it exists to catch, and on a real page
/// it fired on every hidden thing at once.
/// <para>
/// Nothing distinguished the two because the dump did not carry visibility: a rectangle of no area
/// was all a reader got, and a caption that wrapped at column zero and a note the page is
/// deliberately not showing produced exactly the same line.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ConcealedLayoutTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-concealed-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    /// <summary>
    /// A page with a row the application is showing, one it collapsed, and one it hid — and a child
    /// inside the collapsed one, which is Visible in its own right and still lays out to nothing.
    /// </summary>
    private static T OnAPage<T>(Func<Grid, T> work) => Apartment.Run(
        () =>
        {
            var page = new Grid { Name = "page", Width = 200, Height = 100 };
            var rows = new StackPanel { Name = "rows" };
            rows.Children.Add(new Border { Name = "header", Height = 30, Background = new SolidColorBrush(Colors.Red) });

            var note = new StackPanel { Name = "loadingNote", Visibility = Visibility.Collapsed };
            note.Children.Add(new Border { Name = "noteBody", Height = 20, Background = new SolidColorBrush(Colors.Gray) });
            rows.Children.Add(note);

            rows.Children.Add(new Border { Name = "reserved", Height = 25, Visibility = Visibility.Hidden });
            page.Children.Add(rows);

            using var source = new HwndSource(
                new HwndSourceParameters("winwright concealed")
                {
                    PositionX = OffScreen.Left,
                    PositionY = OffScreen.Top,
                    Width = 200,
                    Height = 100,
                })
            {
                RootVisual = page,
            };

            page.UpdateLayout();
            return work(page);
        },
        named: "the concealed layout");

    private string Dumped()
    {
        var path = Path.Combine(root, "concealed.tsv");
        OnAPage(page => InAppGeometry.DumpTo(path, page));
        return path;
    }

    [Fact]
    public void The_dump_says_which_elements_the_application_is_not_showing()
    {
        var read = GeometryDump.Read(Dumped());

        Assert.Equal(Shown.Collapsed, Assert.Single(read.Named("loadingNote")).Visibility);
        Assert.Equal(Shown.Hidden, Assert.Single(read.Named("reserved")).Visibility);
        Assert.Equal(Shown.Visible, Assert.Single(read.Named("header")).Visibility);
    }

    [Fact]
    public void An_element_the_application_collapsed_is_left_alone_rather_than_called_a_fault()
    {
        var reading = Layout.Of(Dumped());

        Assert.DoesNotContain(reading.Faults, one => one.What.Name == "loadingNote");
        Assert.Contains(reading.Concealed, one => one.Name == "loadingNote");
    }

    [Fact]
    public void A_child_of_a_collapsed_parent_is_left_alone_too_because_the_parent_is_why()
    {
        // It is Visible in its own right and still measures nothing. Only the ancestry says why,
        // which is what the depth in the dump is carried for.
        var reading = Layout.Of(Dumped());

        Assert.DoesNotContain(reading.Faults, one => one.What.Name == "noteBody");
        Assert.Contains(reading.Concealed, one => one.Name == "noteBody");
    }

    [Fact]
    public void What_was_left_alone_is_counted_in_the_sentence_rather_than_dropped()
    {
        // A page hiding a note is not a page with a defect on it, and a count that is not silent is
        // not a defect either — the same rule the derived set follows about what it excluded.
        var said = Layout.Of(Dumped()).Sentence();

        Assert.Contains("the application is not showing left alone", said);
        Assert.Contains("loadingNote", said);
    }

    [Fact]
    public void A_visible_element_that_still_measures_nothing_is_the_finding_it_always_was()
    {
        // The half that must not be lost: the check exists for the caption that wrapped at column
        // zero, and that element is Visible and empty.
        var dump = new[]
        {
            new Winwright.Capturing.DrawnElement(0, "Grid", "page", new Winwright.Windowing.WindowBounds(0, 0, 200, 100)),
            new Winwright.Capturing.DrawnElement(1, "TextBlock", "caption", new Winwright.Windowing.WindowBounds(10, 10, 10, 10)),
        };

        var reading = Layout.Of(new ReadGeometry(dump, 0, 0));

        var fault = Assert.Single(reading.Faults);
        Assert.Equal(Fault.MeasuresNothing, fault.Kind);
        Assert.Equal("caption", fault.What.Name);
        Assert.Empty(reading.Concealed);
    }

    [Fact]
    public void A_dump_from_before_the_field_existed_still_reports_what_it_always_did()
    {
        // Seven fields, which is what an application shipping an older in-app half writes. Read as
        // showing everything, because the direction that stays honest is the one that keeps
        // reporting rather than the one that starts excusing.
        var older = Path.Combine(root, "older.tsv");
        System.IO.File.WriteAllText(
            older,
            "0\tGrid\tpage\t0\t0\t200\t100\n1\tTextBlock\tcaption\t10\t10\t10\t10\n");

        var reading = Layout.Of(older);

        Assert.Empty(reading.Concealed);
        Assert.Equal(Fault.MeasuresNothing, Assert.Single(reading.Faults).Kind);
    }

    [Fact]
    public void A_word_the_reader_does_not_know_is_read_as_showing_for_the_same_reason()
    {
        var strange = Path.Combine(root, "strange.tsv");
        System.IO.File.WriteAllText(strange, "0\tGrid\tpage\t0\t0\t200\t100\tSomethingElse\n");

        Assert.Equal(Shown.Visible, GeometryDump.Read(strange).Root!.Visibility);
    }

    [Fact]
    public void The_line_says_what_it_is_where_the_application_is_not_showing_it()
    {
        var read = GeometryDump.Read(Dumped());

        Assert.Contains("(collapsed)", Assert.Single(read.Named("loadingNote")).ToString());
        Assert.Contains("(hidden)", Assert.Single(read.Named("reserved")).ToString());
        Assert.DoesNotContain("(", Assert.Single(read.Named("header")).ToString());
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

using Winwright.Asserting;
using Winwright.Capturing;
using Winwright.InApp;

using Xunit;

using Drawn = Winwright.Capturing.DrawnElement;
using InAppGeometry = Winwright.InApp.Geometry;

namespace Winwright.Tests;

/// <summary>
/// WW131. Measured against the fixture's own window: four of forty-five elements are laid out
/// wrongly by every rule the check has, and every one is a part of the default tab template — a
/// selected header drawn four pixels outside the panel holding it and two past the border
/// containing it, on purpose, because that is how a selected tab lifts over the edge.
/// <para>
/// The elements are real, the rectangles are real, and the faults are true statements about what
/// was drawn. They are also not what anybody asked: a geometry check exists to catch a caption that
/// wrapped and a button nine pixels below its box, and no adopter can fix a theme.
/// </para>
/// <para>
/// Narrowing by name does not separate them — the application named the tab item and the template
/// drew it out of place. What separates them is who put the element there, which the walk knows
/// because it is standing inside the application when it happens.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class WhoseElementTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-whose-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    /// <summary>
    /// A page with an element the application declared, a templated control whose parts the
    /// framework drew, and content out of a data template the application itself wrote.
    /// </summary>
    private static T OnAPage<T>(Func<Grid, T> work) => Apartment.Run(
        () =>
        {
            var page = new Grid { Name = "page", Width = 300, Height = 200 };
            var rows = new StackPanel { Name = "rows" };

            rows.Children.Add(new Border { Name = "declared", Height = 30, Background = new SolidColorBrush(Colors.Red) });

            // A real templated control. Its parts are the framework's, whatever it is called here.
            rows.Children.Add(new ProgressBar { Name = "templated", Height = 20, Value = 40 });

            // Content the application wrote in its own data template, presented by a presenter.
            var template = new DataTemplate
            {
                VisualTree = Visual(),
            };

            rows.Children.Add(new ContentControl
            {
                Name = "presenting",
                Content = "anything",
                ContentTemplate = template,
                Height = 24,
            });

            page.Children.Add(rows);

            using var source = new HwndSource(
                new HwndSourceParameters("winwright whose")
                {
                    PositionX = OffScreen.Left,
                    PositionY = OffScreen.Top,
                    Width = 300,
                    Height = 200,
                })
            {
                RootVisual = page,
            };

            page.UpdateLayout();
            return work(page);
        },
        named: "whose element");

    private static FrameworkElementFactory Visual()
    {
        var factory = new FrameworkElementFactory(typeof(Border));
        factory.SetValue(FrameworkElement.NameProperty, "fromADataTemplate");
        factory.SetValue(FrameworkElement.HeightProperty, 12.0);
        factory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Colors.Green));
        return factory;
    }

    private string Dumped()
    {
        var path = Path.Combine(root, "whose.tsv");
        OnAPage(page => InAppGeometry.DumpTo(path, page));
        return path;
    }

    [Fact]
    public void An_element_the_application_declared_is_its_own()
    {
        var read = GeometryDump.Read(Dumped());

        Assert.Equal(Origin.Application, Assert.Single(read.Named("declared")).From);
        Assert.Equal(Origin.Application, Assert.Single(read.Named("templated")).From);
        Assert.Equal(Origin.Application, read.Root!.From);
    }

    [Fact]
    public void The_parts_a_control_template_drew_are_the_frameworks()
    {
        var read = GeometryDump.Read(Dumped());

        // The progress bar is the application's; the track and the indicator inside it are not,
        // and nothing in this repository named either of them.
        Assert.Contains(read.Elements, one => one.From == Origin.Template);
        Assert.All(
            read.Elements.Where(one => one.From == Origin.Template),
            one => Assert.DoesNotContain(one.Name, new[] { "page", "rows", "declared", "templated", "presenting" }));
    }

    [Fact]
    public void Content_out_of_a_data_template_is_the_applications_own_markup()
    {
        // The one case where a templated parent does not mean somebody else's chrome: the
        // application wrote that template, and hiding its faults would be a silent green.
        var read = GeometryDump.Read(Dumped());

        Assert.Equal(Origin.Application, Assert.Single(read.Named("fromADataTemplate")).From);
    }

    [Fact]
    public void A_fault_about_a_template_part_is_kept_apart_from_the_rest()
    {
        var page = new Drawn(0, "Grid", "page", new Winwright.Windowing.WindowBounds(0, 0, 200, 100));
        var mine = new Drawn(1, "Border", "mine", new Winwright.Windowing.WindowBounds(10, 10, 50, 50));
        var theirs = new Drawn(
            1, "Border", "innerBorder", new Winwright.Windowing.WindowBounds(10, 10, 300, 50), Shown.Visible, Origin.Template);

        var reading = Layout.Of(new ReadGeometry([page, mine, theirs], 0, 0));

        Assert.DoesNotContain(reading.Faults, one => one.What.Name == "innerBorder");
        Assert.Contains(reading.Chrome, one => one.What.Name == "innerBorder");
        Assert.Contains("left to the framework's own template", reading.Sentence());
    }

    [Fact]
    public void A_fault_between_an_element_of_each_is_the_frameworks_too()
    {
        // The tab item case exactly: the application declared the item and the template drew the
        // panel it is measured against, so the application named it and cannot move it.
        var panel = new Drawn(
            0, "TabPanel", "HeaderPanel", new Winwright.Windowing.WindowBounds(0, 0, 200, 40), Shown.Visible, Origin.Template);
        var item = new Drawn(1, "TabItem", "reportPane", new Winwright.Windowing.WindowBounds(-4, -4, 100, 44));

        var reading = Layout.Of(new ReadGeometry([panel, item], 0, 0));

        Assert.Empty(reading.Faults);
        Assert.Contains(reading.Chrome, one => one.What.Name == "reportPane");
    }

    [Fact]
    public void A_caller_that_really_means_the_framework_can_still_have_it()
    {
        var page = new Drawn(0, "Grid", "page", new Winwright.Windowing.WindowBounds(0, 0, 200, 100));
        var theirs = new Drawn(
            1, "Border", "innerBorder", new Winwright.Windowing.WindowBounds(10, 10, 300, 50), Shown.Visible, Origin.Template);

        var whole = Layout.Of(new ReadGeometry([page, theirs], 0, 0)).WithChrome();

        Assert.Contains(whole.Faults, one => one.What.Name == "innerBorder");
        Assert.Empty(whole.Chrome);
    }

    [Fact]
    public void A_dump_from_before_the_field_existed_is_all_the_applications()
    {
        // Eight fields, which is what an in-app half one version older writes. Read as the
        // application's, because the honest direction is to keep reporting rather than excuse.
        var older = Path.Combine(root, "older.tsv");
        System.IO.File.WriteAllText(
            older,
            "0\tGrid\tpage\t0\t0\t200\t100\tVisible\n1\tBorder\tover\t10\t10\t300\t50\tVisible\n");

        var reading = Layout.Of(older);

        Assert.Empty(reading.Chrome);
        Assert.Contains(reading.Faults, one => one.Kind == Fault.EndsOutside);
    }

    [Fact]
    public void The_line_says_which_ones_are_not_the_applications()
    {
        var theirs = new Drawn(
            1, "Border", "innerBorder", new Winwright.Windowing.WindowBounds(0, 0, 10, 10), Shown.Visible, Origin.Template);

        Assert.Contains("(template)", theirs.ToString());
        Assert.DoesNotContain("(template)", new Drawn(1, "Border", "mine", theirs.Bounds).ToString());
    }
}

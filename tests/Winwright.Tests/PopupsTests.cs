using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

using Winwright.InApp;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW75. A popup that closes when it loses mouse capture is right for a person and fatal for a
/// capture: the window is raised to the foreground, the popup goes, and the copy is a correct
/// picture of a window without it.
/// <para>
/// The walk is the part with the trap in it. A closed popup's child is in no visual tree, and
/// closed is exactly the state a popup has to be reached in — so a visual walk would find nothing
/// to fix and report that everything was fine.
/// </para>
/// </summary>
public sealed class PopupsTests
{
    /// <summary>A page with a closed popup on it, which is the state the walk has to reach.</summary>
    private static (Grid Page, Popup Flyout) Page()
    {
        // StaysOpen defaults to true in WPF, so the popup this host exists for is the one whose
        // author turned it off — light dismiss, which is right for a person and fatal for a copy.
        var flyout = new Popup
        {
            Name = "settingsFlyout",
            StaysOpen = false,
            Child = new Border { Width = 80, Height = 40, Background = new SolidColorBrush(Colors.Red) },
        };

        var page = new Grid();
        page.Children.Add(new TextBlock { Text = "the report" });
        page.Children.Add(flyout);
        return (page, flyout);
    }

    [Fact]
    public void A_closed_popup_is_found_because_the_walk_is_logical()
    {
        var found = Apartment.Run(() =>
        {
            var (page, flyout) = Page();
            Assert.False(flyout.IsOpen);
            return Popups.Under(page).Count;
        });

        Assert.Equal(1, found);
    }

    [Fact]
    public void The_child_of_a_closed_popup_is_reached_through_the_popup_and_not_through_the_tree()
    {
        // A popup inside a popup is real, and a closed outer one puts its whole subtree out of
        // reach of anything walking children the ordinary way.
        var found = Apartment.Run(() =>
        {
            var inner = new Popup { Name = "inner", Child = new Border { Width = 10, Height = 10 } };
            var outer = new Popup { Name = "outer", Child = new Grid { Children = { inner } } };
            var page = new Grid { Children = { outer } };

            return Popups.Under(page).Select(one => one.Name).ToList();
        });

        Assert.Equal(["outer", "inner"], found);
    }

    [Fact]
    public void Holding_makes_every_popup_stay_open()
    {
        var (stayed, changed) = Apartment.Run(() =>
        {
            var (page, flyout) = Page();
            Assert.False(flyout.StaysOpen);

            using var host = Popups.Hold(page);
            return (flyout.StaysOpen, host.Changed);
        });

        Assert.True(stayed);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void Letting_go_puts_every_popup_back_to_what_it_was()
    {
        // A host that leaves every popup pinned open has changed the application it was only
        // supposed to photograph.
        var (during, after) = Apartment.Run(() =>
        {
            var (page, flyout) = Page();
            bool held;
            using (var _ = Popups.Hold(page))
                held = flyout.StaysOpen;

            return (held, flyout.StaysOpen);
        });

        Assert.True(during);
        Assert.False(after);
    }

    [Fact]
    public void A_popup_that_already_stayed_open_is_reported_as_unchanged_and_left_that_way()
    {
        var (changed, after, named) = Apartment.Run(() =>
        {
            var (page, flyout) = Page();
            flyout.StaysOpen = true;

            int count;
            string said;
            using (var host = Popups.Hold(page))
            {
                count = host.Changed;
                said = host.Sentence();
            }

            return (count, flyout.StaysOpen, said);
        });

        Assert.Equal(0, changed);
        Assert.True(after, "putting it back set it to something it never was");
        Assert.Contains("already held", named);
    }

    [Fact]
    public void A_popup_opened_after_the_host_was_built_is_taken_on_the_next_walk()
    {
        // The one case the host exists for, arriving late: a page that builds a flyout when it is
        // first needed, which is after whatever raised the window.
        var (first, second, stayed) = Apartment.Run(() =>
        {
            var page = new Grid();
            using var host = Popups.Hold(page);
            var before = host.Held.Count;

            var late = new Popup
            {
                Name = "late",
                StaysOpen = false,
                Child = new Border { Width = 10, Height = 10 },
            };
            page.Children.Add(late);

            return (before, host.Again(), late.StaysOpen);
        });

        Assert.Equal(0, first);
        Assert.Equal(1, second);
        Assert.True(stayed);
    }

    [Fact]
    public void Walking_again_does_not_take_hold_of_the_same_popup_twice()
    {
        // It would record the second reading as what to restore, which is what this already set.
        var (again, after) = Apartment.Run(() =>
        {
            var (page, flyout) = Page();
            int taken;
            using (var host = Popups.Hold(page))
                taken = host.Again();

            return (taken, flyout.StaysOpen);
        });

        Assert.Equal(0, again);
        Assert.False(after);
    }

    [Fact]
    public void A_page_with_no_popup_says_so_rather_than_reporting_nothing()
    {
        var said = Apartment.Run(() =>
        {
            using var host = Popups.Hold(new Grid { Children = { new TextBlock { Text = "the report" } } });
            return host.Sentence();
        });

        Assert.Equal("there is no popup under this host to hold open.", said);
    }

    [Fact]
    public void What_is_being_held_is_named_on_every_run()
    {
        var said = Apartment.Run(() =>
        {
            var (page, _) = Page();
            using var host = Popups.Hold(page);
            return host.Sentence();
        });

        Assert.Contains("holding 1 popup(s) open, 1 of them changed: settingsFlyout", said);
    }

    [Fact]
    public void An_unnamed_popup_is_named_by_what_it_is_holding()
    {
        var said = Apartment.Run(() =>
        {
            var page = new Grid { Children = { new Popup { Child = new Border() } } };
            using var host = Popups.Hold(page);
            return host.Sentence();
        });

        Assert.Contains("(unnamed popup holding Border)", said);
    }

    [Fact]
    public void A_tree_that_loops_back_on_itself_is_walked_once()
    {
        // A logical parent that is also reachable as a child is not a thing a page sets out to
        // build, and a walk with no guard against it does not return.
        var found = Apartment.Run(() =>
        {
            var page = new Grid();
            var flyout = new Popup { Name = "loop" };
            page.Children.Add(flyout);
            flyout.Child = new Border { Child = new TextBlock { Text = "in" } };

            return Popups.Under(page).Count;
        });

        Assert.Equal(1, found);
    }

    [Fact]
    public void A_root_from_another_thread_is_refused_for_a_reason_about_threading()
    {
        var theirs = Apartment.Run<DependencyObject>(() => new Grid());

        Assert.Throws<ThreadBoundException>(() => Popups.Under(theirs));
    }

    [Fact]
    public void A_closed_popup_is_photographed_through_the_tree_it_holds()
    {
        // WW347, and the property that makes this the way through rather than a second copy: the
        // child is a tree this process owns whether or not the popup is open, so a preview of a
        // flyout nobody has clicked is a picture this can take and no copy of the screen ever could
        // — there is no window on the screen to copy.
        var root = Directory.CreateTempSubdirectory("winwright-flyout-shut-").FullName;
        try
        {
            var path = Path.Combine(root, "shut.png");
            var picture = Apartment.Run(() =>
            {
                var (_, flyout) = Page();
                Assert.False(flyout.IsOpen);
                return Popups.Picture(flyout, path);
            });

            Assert.True(File.Exists(path), picture.Sentence());
            Assert.Equal(80, picture.Width);
            Assert.Equal(40, picture.Height);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void A_popup_holding_nothing_is_refused_rather_than_written_as_an_empty_file()
    {
        // The refusal is about what the popup was given, which is a different sentence from the
        // render's own: that one is about what an element laid out to, and an element that is not
        // there laid out to nothing anybody can be told about.
        var refused = Assert.Throws<UnrenderableException>(() => Apartment.Run(() =>
            Popups.Picture(new Popup { Name = "empty" }, Path.Combine(Path.GetTempPath(), "never.png"))));

        Assert.Contains("empty", refused.Message, StringComparison.Ordinal);
        Assert.Contains("holding nothing", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_popup_holding_something_with_no_layout_is_refused_by_what_it_is_holding()
    {
        // A popup's child is a UIElement, and not every UIElement has a layout of its own. Named
        // rather than cast blindly: the failure otherwise is an invalid cast raised from inside a
        // render, which says nothing about the popup it came from.
        var refused = Assert.Throws<UnrenderableException>(() => Apartment.Run(() =>
            Popups.Picture(
                new Popup { Name = "odd", Child = new BareElement() },
                Path.Combine(Path.GetTempPath(), "never.png"))));

        Assert.Contains("BareElement", refused.Message, StringComparison.Ordinal);
        Assert.Contains("no layout of its own", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_popup_from_another_thread_is_refused_for_a_reason_about_threading()
    {
        var theirs = Apartment.Run(() => new Popup { Child = new Border { Width = 10, Height = 10 } });

        Assert.Throws<ThreadBoundException>(
            () => Popups.Picture(theirs, Path.Combine(Path.GetTempPath(), "never.png")));
    }

    /// <summary>A UIElement with no layout of its own, which is what a popup may be holding.</summary>
    private sealed class BareElement : UIElement;
}

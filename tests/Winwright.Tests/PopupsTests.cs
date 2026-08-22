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
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "the thread did not finish");

        if (threw is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(threw).Throw();

        return answer!;
    }

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
        var found = OnStaThread(() =>
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
        var found = OnStaThread(() =>
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
        var (stayed, changed) = OnStaThread(() =>
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
        var (during, after) = OnStaThread(() =>
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
        var (changed, after, named) = OnStaThread(() =>
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
        var (first, second, stayed) = OnStaThread(() =>
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
        var (again, after) = OnStaThread(() =>
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
        var said = OnStaThread(() =>
        {
            using var host = Popups.Hold(new Grid { Children = { new TextBlock { Text = "the report" } } });
            return host.Sentence();
        });

        Assert.Equal("there is no popup under this host to hold open.", said);
    }

    [Fact]
    public void What_is_being_held_is_named_on_every_run()
    {
        var said = OnStaThread(() =>
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
        var said = OnStaThread(() =>
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
        var found = OnStaThread(() =>
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
        var theirs = OnStaThread<DependencyObject>(() => new Grid());

        Assert.Throws<ThreadBoundException>(() => Popups.Under(theirs));
    }
}

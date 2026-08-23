using System.Windows.Automation;

using Winwright.Acting;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW31. The notification-area icon has no clickable point and no reliable right-click.
/// <para>
/// The icon here is a real one, added by this run through the shell and taken away afterwards.
/// Everything else is the real taskbar, because there is no other kind: the tray belongs to the
/// shell, and a fake one would prove things about the fake.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class NotificationAreaTests : IDisposable
{
    private readonly TrayIconFixture icon = TrayIconFixture.Add("winwright under test");

    /// <summary>What the shell calls this run's icon, which is not what any other run's is called.</summary>
    private string Tip => icon.Tip;

    public void Dispose()
    {
        NotificationArea.CloseOverflow();
        icon.Dispose();
    }

    [Fact]
    public void The_taskbar_is_found_by_its_class_and_holds_icons()
    {
        Assert.NotNull(NotificationArea.Tray());
        Assert.NotEmpty(NotificationArea.Showing());
    }

    [Fact]
    public void Asking_a_tray_icon_for_a_clickable_point_throws_which_is_why_the_rectangle_is_used()
    {
        var found = NotificationArea.Find(Tip);
        Assert.NotNull(found);

        var element = NotificationArea.ElementFor(found);
        Assert.NotNull(element);

        Assert.Throws<NoClickablePointException>(() => element.GetClickablePoint());

        // And the rectangle it is addressed by instead is a real one.
        Assert.True(found.Rectangle.Width > 0, $"the icon reported {found.Rectangle}");
        Assert.True(found.Rectangle.Height > 0);
    }

    [Fact]
    public void An_icon_added_now_hides_in_the_overflow_and_is_not_in_the_tree_until_it_is_opened()
    {
        // Measured, and the whole reason the overflow is opened first: the shell puts a new icon
        // out of sight, so looking only at the taskbar finds nothing and says the icon is gone.
        Assert.Null(NotificationArea.Find(Tip, openingTheOverflow: false));

        var found = NotificationArea.Find(Tip);

        Assert.NotNull(found);
        Assert.True(found.Hidden);
        Assert.Contains(Tip, found.Name);
    }

    [Fact]
    public void The_chevron_is_found_by_its_automation_id_and_never_by_its_position()
    {
        var chevron = NotificationArea.Chevron();

        Assert.NotNull(chevron);
        Assert.Equal(NotificationArea.ChevronAutomationId, chevron.Current.AutomationId);

        // It sits among the icons and carries the same class as they do, so nothing about where
        // it is or what it is called in this language would find it twice running.
        Assert.Equal("SystemTray.NormalButton", chevron.Current.ClassName);
        Assert.DoesNotContain(
            NotificationArea.Showing(), one => one.Facts.Bounds == default);
    }

    [Fact]
    public void The_overflow_opens_through_the_pattern_and_shuts_again()
    {
        var opened = NotificationArea.OpenOverflow();

        // WW165: the reading and not a bool, so a red on a shell that would not work the flyout
        // names what it was rather than saying only that something was expected to be true.
        Assert.True(opened.Held, opened.ToString());
        Assert.NotNull(NotificationArea.Overflow());
        Assert.NotEmpty(NotificationArea.Hidden());

        var shut = NotificationArea.CloseOverflow();

        Assert.True(shut.Held, shut.ToString());
        Assert.Null(NotificationArea.Overflow());
        Assert.False(shut.Already, "the flyout was already shut, so this proves nothing about shutting it");
    }

    [Fact]
    public void Opening_an_overflow_that_is_already_open_is_answered_rather_than_toggled()
    {
        var first = NotificationArea.OpenOverflow();
        var again = NotificationArea.OpenOverflow();

        Assert.True(first.Held, first.ToString());
        Assert.True(again.Held, again.ToString());
        Assert.NotNull(NotificationArea.Overflow());

        // The half a bool could not carry: the second call pressed nothing, and a run that opened
        // the flyout and one that found it open leave the taskbar differently.
        Assert.True(again.Already, again.ToString());
        Assert.Contains("already", again.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_shell_that_will_not_work_the_flyout_is_a_hole_naming_what_it_was()
    {
        // The reading a bool had nowhere to put. A flyout this run cannot work is a fact about the
        // desk and never a defect in the code under test, so the verdict is unchecked and the step
        // carries the reason — which is what a reader needs instead of "Expected: True".
        var opened = NotificationArea.OpenOverflow();

        var verdict = opened.AsAssertion("the overflow opens");
        var step = opened.AsTraceStep("the overflow opens");

        if (opened.Held)
        {
            Assert.Equal(Winwright.Verdicts.AssertionOutcome.Passed, verdict.Outcome);
            Assert.Equal(Winwright.Tracing.StepVerdict.Ok, step.Verdict);
            Assert.Null(opened.Because);
            NotificationArea.CloseOverflow();
            return;
        }

        // Carried rather than asserted away: this desk refused, and that is the arm the whole
        // reading exists for.
        Assert.Equal(Winwright.Verdicts.AssertionOutcome.Unchecked, verdict.Outcome);
        Assert.Equal(Winwright.Tracing.StepVerdict.Unchecked, step.Verdict);
        Assert.False(string.IsNullOrWhiteSpace(opened.Because), "the flyout was refused and said nothing about why");
        Assert.Contains(opened.Because, step.Detail!, StringComparison.Ordinal);
    }

    [Fact]
    public void An_icon_the_shell_does_not_have_is_answered_with_nothing_rather_than_a_throw()
    {
        Assert.Null(NotificationArea.Find("winwright is not here", settleMs: 800, pollMs: 40));
    }

    [Fact]
    public void Asking_a_nameless_icon_for_its_menu_says_what_it_could_not_find()
    {
        var menu = NotificationArea.OpenMenu("winwright is not here", settleMs: 800, pollMs: 40);

        Assert.False(menu.Opened);
        Assert.Contains("no icon in the notification area is called that", menu.Because);
        Assert.Equal(Winwright.Tracing.StepVerdict.Failed, menu.AsTraceStep().Verdict);
    }

    [Fact]
    public void The_route_to_a_menu_is_focus_and_the_application_key_and_it_reports_the_truth()
    {
        // This fixture's icon has no window procedure, so it shows no menu — and the answer says
        // so instead of claiming one appeared. That negative is the assertion worth having here:
        // a route that reported success on a shell showing nothing would be the false green this
        // whole project is against. An icon that does show one belongs to the fixture application
        // Block K exists to build.
        var menu = NotificationArea.OpenMenu(Tip, settleMs: 800, pollMs: 40);

        Assert.False(menu.Opened);
        Assert.NotNull(menu.Because);
        Assert.Contains("winwright under test", menu.ToString());
        Assert.Contains("focus and the application key", menu.AsTraceStep().Pattern);
    }

    [Fact]
    public void Nothing_here_reaches_for_a_synthesised_right_click()
    {
        var clicking = typeof(NotificationArea).GetMethods()
            .Select(method => method.Name)
            .Where(name => name.Contains("Click", StringComparison.OrdinalIgnoreCase)
                || name.Contains("RightClick", StringComparison.OrdinalIgnoreCase));

        Assert.Empty(clicking);
    }

    [Fact]
    public void An_icon_says_which_it_is_and_where_in_one_line()
    {
        var found = NotificationArea.Find(Tip);

        Assert.NotNull(found);
        Assert.StartsWith($"hidden tray icon '{Tip}'", found.ToString());
        Assert.Contains(" at ", found.ToString());
    }
}

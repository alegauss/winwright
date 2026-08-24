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
        Assert.True(found.Found, found.Sentence());

        var addressed = found.Icon!;
        var element = NotificationArea.ElementFor(addressed);
        Assert.NotNull(element);

        Assert.Throws<NoClickablePointException>(() => element.GetClickablePoint());

        // And the rectangle it is addressed by instead is a real one.
        Assert.True(addressed.Rectangle.Width > 0, $"the icon reported {addressed.Rectangle}");
        Assert.True(addressed.Rectangle.Height > 0);
    }

    [Fact]
    public void An_icon_added_now_hides_in_the_overflow_and_is_not_in_the_tree_until_it_is_opened()
    {
        // Measured, and the whole reason the overflow is opened first: the shell puts a new icon
        // out of sight, so looking only at the taskbar finds nothing.
        var onTheBar = NotificationArea.Find(Tip, openingTheOverflow: false);

        Assert.False(onTheBar.Found);

        // WW168, and the point of the whole reading: this is not the icon being absent. The bar was
        // all that was looked at, so the search says so rather than answering the wider question.
        Assert.False(onTheBar.Everywhere);
        Assert.Contains("told not to open the overflow", onTheBar.Because, StringComparison.Ordinal);

        var found = NotificationArea.Find(Tip);

        Assert.True(found.Found, found.Sentence());
        Assert.True(found.Everywhere);
        Assert.True(found.Icon!.Hidden);
        Assert.Contains(Tip, found.Icon.Name);
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
        var searched = NotificationArea.Find("winwright is not here", settleMs: 800, pollMs: 40);

        Assert.False(searched.Found);

        // WW168. Both places were looked at, so this one really is a statement about what is in the
        // notification area — which is what makes it a red a scenario may act on rather than a hole.
        Assert.True(searched.Everywhere, searched.Sentence());
        Assert.Contains("neither the taskbar nor the overflow", searched.Because, StringComparison.Ordinal);

        var verdict = searched.AsAssertion("the icon is in the notification area");
        Assert.Equal(Winwright.Verdicts.AssertionOutcome.Failed, verdict.Outcome);
    }

    [Fact]
    public void The_search_says_what_it_found_and_where_rather_than_only_whether()
    {
        // WW167's catalogue asked for this the moment the reading existed: a rendering every case
        // passes as a failure message is text a green never prints and nothing reads back.
        var found = NotificationArea.Find(Tip);

        Assert.Contains($"answers to '{Tip}'", found.Sentence(), StringComparison.Ordinal);
        Assert.Contains("tray icon", found.Sentence(), StringComparison.Ordinal);

        var missing = NotificationArea.Find("winwright is not here", settleMs: 800, pollMs: 40);

        // The other way round it names what was asked for and why there is none, in that order —
        // a reader who gets only "not found" is the reader WW168 was filed for.
        Assert.StartsWith(
            "nothing in the notification area answers to 'winwright is not here':",
            missing.Sentence(),
            StringComparison.Ordinal);
        Assert.Contains(missing.Because, missing.Sentence(), StringComparison.Ordinal);
        Assert.EndsWith(".", missing.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_search_the_overflow_would_not_open_for_is_a_hole_and_never_an_absent_icon()
    {
        // The two facts WW168 exists to tell apart, asserted on the same type rather than on two
        // runs that cannot be provoked in one sitting. A search that reached both places and one
        // that reached only the bar answer the same `Found`, and must not answer the same verdict.
        var everywhere = NotificationArea.Find("winwright is not here", settleMs: 800, pollMs: 40);
        var barAlone = NotificationArea.Find("winwright is not here", openingTheOverflow: false);

        Assert.False(everywhere.Found);
        Assert.False(barAlone.Found);

        Assert.Equal(Winwright.Verdicts.AssertionOutcome.Failed, everywhere.AsAssertion("it is there").Outcome);
        Assert.Equal(Winwright.Verdicts.AssertionOutcome.Unchecked, barAlone.AsAssertion("it is there").Outcome);

        // And the step behind each says the same thing, because a trace that disagreed with the
        // verdict beside it is a record a reader cannot use.
        Assert.Equal(Winwright.Tracing.StepVerdict.Failed, everywhere.AsTraceStep("it is there").Verdict);
        Assert.Equal(Winwright.Tracing.StepVerdict.Unchecked, barAlone.AsTraceStep("it is there").Verdict);
    }

    [Fact]
    public void Asking_a_nameless_icon_for_its_menu_says_what_it_could_not_find()
    {
        var menu = NotificationArea.OpenMenu("winwright is not here", settleMs: 800, pollMs: 40);

        Assert.False(menu.Opened);

        // WW168: the search's own reason rather than one typed here. This used to say the icon was
        // on neither the taskbar nor the overflow whatever had happened — a statement about the
        // application on every run where the flyout had simply not opened.
        Assert.Contains("it is on neither the taskbar nor the overflow", menu.Because);
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

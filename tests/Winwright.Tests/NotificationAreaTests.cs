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
    /// <summary>
    /// This run's own icon, or null where the desk refused to let one be placed.
    /// <para>
    /// WW179. Built through the door rather than directly, and that matters most here: this is a
    /// field initialiser, so a throw runs for every case in the class and reports fourteen broken
    /// harnesses over a shell that was covering the taskbar.
    /// </para>
    /// </summary>
    private readonly TrayIconFixture? icon =
        BusyDesk.Built(() => TrayIconFixture.Add("winwright under test"));

    /// <summary>Whether this class has the icon its cases are about.</summary>
    private bool Placed => icon is not null;

    /// <summary>What the shell calls this run's icon, which is not what any other run's is called.</summary>
    private string Tip => icon!.Tip;

    public void Dispose()
    {
        NotificationArea.CloseOverflow();
        icon?.Dispose();
    }

    [Fact]
    public void The_taskbar_is_found_by_its_class_and_holds_icons()
    {
        // WW190. Both halves are about the shell, so a desk with the taskbar covered answered this
        // with a red about this repository. Measured in the guest: it did, twice.
        if (BusyDesk.Excused(NotificationArea.Reachable()))
            return;

        Assert.NotNull(NotificationArea.Tray());
        Assert.NotEmpty(NotificationArea.Showing());
    }

    [Fact]
    public void Asking_a_tray_icon_for_a_clickable_point_throws_which_is_why_the_rectangle_is_used()
    {
        if (!Placed)
            return;

        var found = NotificationArea.Find(Tip);

        // WW190. The icon was placed, and a shell that would not open the flyout still hides it —
        // so `Found` here is a question about the desk before it is one about the addressing.
        if (BusyDesk.Excused(found.AsAssertion("this run's icon is in the notification area")))
            return;

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
        if (!Placed)
            return;

        // Measured, and the whole reason the overflow is opened first: the shell puts a new icon
        // out of sight, so looking only at the taskbar finds nothing.
        var onTheBar = NotificationArea.Find(Tip, openingTheOverflow: false);

        Assert.False(onTheBar.Found);

        // WW168, and the point of the whole reading: this is not the icon being absent. The bar was
        // all that was looked at, so the search says so rather than answering the wider question.
        Assert.False(onTheBar.Everywhere);
        Assert.Contains("told not to open the overflow", onTheBar.Because, StringComparison.Ordinal);

        var found = NotificationArea.Find(Tip);

        // WW190. The half that needs the shell to have opened the flyout, and the arm above needs
        // nothing: a search told not to open it answers the same either way.
        if (BusyDesk.Excused(found.AsAssertion("this run's icon is in the overflow")))
            return;

        Assert.True(found.Found, found.Sentence());
        Assert.True(found.Everywhere);
        Assert.True(found.Icon!.Hidden);
        Assert.Contains(Tip, found.Icon.Name);
    }

    [Fact]
    public void Whether_this_desk_places_icons_at_all_is_a_different_question_from_whether_one_is_there()
    {
        // WW217. The reading the tray fixture was missing. A search that opened the flyout and read
        // it did look everywhere, so an absent icon came out as a red — while what the desk was
        // really doing on a loaded guest was placing nobody's icon yet.
        var placing = NotificationArea.Placing();

        Assert.Equal(NotificationArea.Reachable().Name, placing.Name);
        Assert.False(string.IsNullOrWhiteSpace(placing.Satisfied ? "met" : placing.Absence));

        // The two are not the same question, and this is the case that says so: a bar with nothing
        // on it and a readable flyout with something in it is unreachable and placing.
        if (!placing.Satisfied)
            return;

        Assert.True(
            NotificationArea.Showing().Count > 0 || NotificationArea.Hidden().Count > 0,
            "this desk was called placing and holds no icon anywhere");
    }

    [Fact]
    public void Asking_whether_the_desk_places_icons_leaves_the_taskbar_as_it_found_it()
    {
        // Looking may have to open the flyout, and a flyout left standing is what the next case
        // trips on — which one of the flakes behind WW217 was exactly.
        if (BusyDesk.Excused(NotificationArea.Reachable()))
            return;

        var before = NotificationArea.Overflow() is not null;

        _ = NotificationArea.Placing();

        Assert.Equal(before, NotificationArea.Overflow() is not null);
    }

    [Fact]
    public void The_chevron_is_found_by_its_automation_id_and_never_by_its_position()
    {
        // WW190. An absent chevron is a covered taskbar and not a shell that names it differently,
        // and this went red about the naming on a desk that had neither.
        if (BusyDesk.Excused(NotificationArea.Reachable()))
            return;

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

        // WW190. WW165 gave the red a name; this stops it being a red at all. A shell that will not
        // work its own flyout is the desk, and the case beside this one asserts exactly that.
        if (BusyDesk.Excused(opened.AsAssertion("the overflow opens")))
            return;

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

        // WW190. Nothing here is about the second call until the first one worked, and a shell that
        // opened neither answered this with a red about answering rather than toggling.
        if (BusyDesk.Excused(first.AsAssertion("the overflow opens")))
            return;

        var again = NotificationArea.OpenOverflow();

        // WW217. The second reading of the pair, excused the way the first already was. A shell that
        // shuts its own flyout between one call and the next leaves nothing already open to answer
        // about — and the red that produced said the second call had toggled it, which is a claim
        // about this code made out of something the desk did.
        if (again.Held && !again.Already
            && BusyDesk.Excused(Winwright.Verdicts.Precondition.Absent(
                OverflowState.PreconditionName,
                "the shell shut the flyout between one call and the next, so the second call had "
                    + "nothing already open to answer about")))
        {
            return;
        }

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
    public void A_search_that_opened_the_flyout_waits_for_its_own_icon_and_not_for_a_stranger()
    {
        // WW220. The gate the flyout was settled against is any icon with a width, which the shell's
        // own icons satisfy on the first poll — so a search read once and answered about an icon that
        // was still arriving. Provoked rather than waited for: the flyout is shut, so this search
        // opens it from cold and has to find its own icon on the far side of that.
        if (!Placed)
            return;

        // Excused and not asserted. Whether the shell shuts its own flyout is the shell's business,
        // and this case is about what the search does once the flyout is shut — so a desk that will
        // not shut it has taken the condition away rather than failed anything.
        if (BusyDesk.Excused(NotificationArea.CloseOverflow().AsAssertion("the flyout shuts before the search")))
            return;

        var searched = NotificationArea.Find(Tip);
        if (BusyDesk.Excused(searched.AsAssertion("the icon this run added can be found")))
            return;

        Assert.True(searched.Found, searched.Sentence());
        Assert.True(searched.Everywhere, searched.Sentence());
        NotificationArea.CloseOverflow();
    }

    [Fact]
    public void An_icon_the_shell_does_not_have_is_answered_with_nothing_rather_than_a_throw()
    {
        var searched = NotificationArea.Find("winwright is not here", settleMs: 800, pollMs: 40);

        Assert.False(searched.Found);

        // WW190. `Everywhere` is the shell's answer and not this repository's: a run that could not
        // open the flyout looked at the taskbar alone, and asserting it looked at both is asserting
        // something about the desk. The arm below is what this case is really for.
        if (BusyDesk.Excused(searched.AsAssertion("the icon is in the notification area")))
            return;

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
        if (!Placed)
            return;

        // WW167's catalogue asked for this the moment the reading existed: a rendering every case
        // passes as a failure message is text a green never prints and nothing reads back.
        var found = NotificationArea.Find(Tip);

        // WW190. The found half needs the shell to have produced the icon; the missing half below
        // reads the same whatever the desk did, which is why only this one is excused.
        if (!BusyDesk.Excused(found.AsAssertion("this run's icon is in the notification area")))
        {
            Assert.Contains($"answers to '{Tip}'", found.Sentence(), StringComparison.Ordinal);
            Assert.Contains("tray icon", found.Sentence(), StringComparison.Ordinal);
        }

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

        // The bar-alone arm is the one this case can always assert: the caller narrowed the
        // question, so it is a hole whatever the desk was doing.
        Assert.Equal(Winwright.Verdicts.AssertionOutcome.Unchecked, barAlone.AsAssertion("it is there").Outcome);
        Assert.Equal(Winwright.Tracing.StepVerdict.Unchecked, barAlone.AsTraceStep("it is there").Verdict);

        // The other arm needs the shell to have opened the flyout, and a desk that would not is the
        // very fact WW168 is about — so it is excused rather than asserted past. Measured in the
        // guest: this went red as Unchecked on a run where the taskbar was covered.
        if (BusyDesk.Excused(everywhere.AsAssertion("it is there")))
            return;

        Assert.True(everywhere.Everywhere, everywhere.Sentence());
        Assert.Equal(Winwright.Verdicts.AssertionOutcome.Failed, everywhere.AsAssertion("it is there").Outcome);
        Assert.Equal(Winwright.Tracing.StepVerdict.Failed, everywhere.AsTraceStep("it is there").Verdict);
    }

    [Fact]
    public void Asking_a_nameless_icon_for_its_menu_says_what_it_could_not_find()
    {
        var menu = NotificationArea.OpenMenu("winwright is not here", settleMs: 800, pollMs: 40);

        Assert.False(menu.Opened);

        // WW190. The reason quoted below is the one a search that reached both places gives, and a
        // flyout that would not open gives a different one — about the desk.
        if (BusyDesk.Excused(menu.AsAssertion("the icon shows its menu")))
            return;

        // WW168: the search's own reason rather than one typed here. This used to say the icon was
        // on neither the taskbar nor the overflow whatever had happened — a statement about the
        // application on every run where the flyout had simply not opened.
        Assert.Contains("it is on neither the taskbar nor the overflow", menu.Because);
        Assert.Equal(Winwright.Tracing.StepVerdict.Failed, menu.AsTraceStep().Verdict);
    }

    [Fact]
    public void The_route_to_a_menu_is_focus_and_the_application_key_and_it_reports_the_truth()
    {
        if (!Placed)
            return;

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

        // WW174, and both arms assert something real. This icon has no window procedure, so on a
        // desk that let the run ask, no menu is a statement about the icon and goes red. On a desk
        // that refused the focus or moved the icon away, nothing was asked and nothing observed.
        if (menu.Missing is not null)
        {
            Assert.True(BusyDesk.Excused(menu.AsAssertion("the icon shows its menu")), menu.ToString());
            Assert.Equal(Winwright.Tracing.StepVerdict.Unchecked, menu.AsTraceStep().Verdict);
            return;
        }

        Assert.Equal(
            Winwright.Verdicts.AssertionOutcome.Failed,
            menu.AsAssertion("the icon shows its menu").Outcome);
        Assert.Equal(Winwright.Tracing.StepVerdict.Failed, menu.AsTraceStep().Verdict);
    }

    [Fact]
    public void A_menu_asked_of_an_icon_that_is_not_there_is_a_red_and_never_a_hole()
    {
        // The arm that must stay a failure. Both places were looked at and the icon is in neither,
        // so this is a statement about the notification area and a scenario may act on it — which
        // is what stops WW174 turning every unopened menu into an excuse.
        var menu = NotificationArea.OpenMenu("winwright is not here", settleMs: 800, pollMs: 40);

        Assert.False(menu.Opened);

        // WW190, and it does not weaken the claim. What must stay a failure is a search that
        // reached both places and found nothing; a search that reached only the taskbar is the very
        // reading WW174 added, so the case observes nothing rather than asserting the desk was kind.
        if (BusyDesk.Excused(menu.AsAssertion("the icon shows its menu")))
            return;

        Assert.Null(menu.Missing);
        Assert.Equal(
            Winwright.Verdicts.AssertionOutcome.Failed,
            menu.AsAssertion("the icon shows its menu").Outcome);
        Assert.Equal(Winwright.Tracing.StepVerdict.Failed, menu.AsTraceStep().Verdict);
    }

    [Fact]
    public void The_menu_answers_a_verdict_at_all_which_is_what_a_scenario_counts()
    {
        // The quieter half of WW174. TrayMenu answered no AsAssertion, so it was invisible to the
        // pairing RecordedResultTests enforces — that check fires on types that answer a verdict,
        // and a type answering none is not a type it can find. A scenario asserting that an icon's
        // menu opens had nothing to count, and whoever wrote one first would invent it at the call.
        var answering = typeof(TrayMenu).GetMethod(nameof(TrayMenu.AsAssertion));
        var recording = typeof(TrayMenu).GetMethod(nameof(TrayMenu.AsTraceStep));

        Assert.NotNull(answering);
        Assert.Equal(typeof(Winwright.Verdicts.AssertionResult), answering.ReturnType);
        Assert.NotNull(recording);
        Assert.Equal(typeof(Winwright.Tracing.TraceStep), recording.ReturnType);
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
        if (!Placed)
            return;

        var found = NotificationArea.Find(Tip);

        // WW190. "hidden tray icon 'x' at ..." is what a found icon renders as, and a desk that
        // hid it from the search renders the other sentence — which is not this case's subject.
        if (BusyDesk.Excused(found.AsAssertion("this run's icon is in the notification area")))
            return;

        Assert.NotNull(found);
        Assert.StartsWith($"hidden tray icon '{Tip}'", found.ToString());
        Assert.Contains(" at ", found.ToString());
    }
}

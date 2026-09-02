using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Windowing;

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
    public void An_icon_that_renamed_itself_is_still_the_icon_that_was_found()
    {
        if (!Placed)
            return;

        // WW82. The condition claude-tray produced on every run of its menu case and that nothing
        // here could reach: a tray icon's name is its tooltip, and an application with anything live
        // in one rewrites it between a run finding the icon and asking it for its menu. claude-tray's
        // reads `connecting…` until the first reading lands, and the act reported the icon gone —
        // a hole naming the shell, for an icon that had not moved.
        var found = NotificationArea.Find(Tip);
        if (BusyDesk.Excused(found.AsAssertion("this run's icon is in the notification area")))
            return;

        Assert.True(found.Found, found.Sentence());
        var addressed = found.Icon!;

        icon!.Rename("winwright under test, renamed");

        // The name really did move, or this case would pass on a rename that never happened.
        Assert.NotEqual(addressed.Name, Tip);
        Assert.DoesNotContain("renamed", addressed.Name, StringComparison.Ordinal);

        // Looked up again under the new name, for two reasons and neither of them the claim. It says
        // the icon is still placed, so a null below is about addressing rather than about a shell
        // that took it away — and it leaves the overflow open, which `Live` needs to see a hidden
        // icon at all. Without this the case cannot tell a stale name from a shut flyout, and the
        // first run of it did not: it went red on a fixture whose flyout had closed behind it.
        var renamed = NotificationArea.Find(Tip);
        if (BusyDesk.Excused(renamed.AsAssertion("this run's renamed icon is in the notification area")))
            return;

        Assert.True(renamed.Found, renamed.Sentence());

        // The claim. The icon found under the old name still addresses the element, because what it
        // is matched by outlives what it is called.
        var element = NotificationArea.ElementFor(addressed);

        Assert.NotNull(element);
        Assert.Contains("renamed", element.Current.Name, StringComparison.Ordinal);
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

        // WW324, and WW288's measurement is why it reads this way. `OpenOverflow` answered Held,
        // which means it had already seen the flyout standing with an icon laid out in it — and
        // this line still went red once in five guest runs, on the very next call. Nothing shuts a
        // flyout in that gap: `Overflow()` is one `FindAll` against the desktop root, and a
        // cross-process tree under load answers null for a window that is there.
        //
        // So it is looked at the way everything else in this engine looks: to a deadline. A flyout
        // that is genuinely absent still fails, and one unlucky read no longer does.
        Assert.True(
            Attempt.UntilTrue(() => NotificationArea.Overflow() is not null, 2000, 25).Happened,
            $"the flyout was not readable after {opened}");

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

        // WW288. The state the gate on the already-standing path created, and it is the desk rather
        // than this code: the flyout was there when the second call looked and had gone before it
        // could be read. Before the gate that came back as 'already open' — a true-looking answer
        // about a flyout on its way out, which is what `Find` was polling behind when WW223 recurred.
        // A hole and never a red, for the reason the arm above is one.
        if (!again.Held && again.Already && BusyDesk.Excused(again.AsAssertion("the overflow opens")))
            return;

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
    public void An_icon_that_has_a_menu_shows_it_to_focus_and_the_application_key()
    {
        // WW332. The verb's success path, which nothing here observed until now: every other case
        // around it stops at the flyout, and the only OpenMenu case asks an icon that is not there.
        // What that left unproven is the whole of the verb — three adopted cases fail on this route
        // and the question they raise could not be asked in this repository at all.
        // Its own icon and not the class's, because the class's answers no menu on purpose: the case
        // below it asserts that the verb says so rather than claiming one appeared, and an icon that
        // served both would have to be two things at once.
        using var answering = BusyDesk.Built(
            () => TrayIconFixture.Add("winwright menu", TrayMenuKind.Win32));
        if (answering is null)
            return;

        var menu = NotificationArea.OpenMenu(answering.Tip, settleMs: 4000, pollMs: 40);

        try
        {
            if (BusyDesk.Excused(menu.AsAssertion("the icon shows its menu")))
                return;

            // The icon's own count first, because the two answer different questions. This one says
            // the shell delivered the request and the application drew a menu; the verb's Opened
            // says the desk then highlighted something a reading could find. A run where the first
            // holds and the second does not is the interesting failure, and reading only the verb
            // would report it as the application never being asked.
            Assert.True(
                answering.MenusShown > 0,
                $"the icon was never asked for its menu: {menu.Because}");

            Assert.True(menu.Opened, menu.Because);
            Assert.Equal(Winwright.Tracing.StepVerdict.Ok, menu.AsTraceStep().Verdict);

            // WW339. Which of the two readings answered, said rather than left to a reader holding
            // a string — and WW350 made that a reading each rather than one of two. A menu standing
            // and an entry highlighted are different facts about the same act, and a
            // TrackPopupMenu is true of both: it is a top-level menu on the desktop and it takes
            // the focus. So what is asserted is that something answered, never which.
            Assert.True(
                menu.Standing is not null || menu.Highlighted is not null,
                "the menu opened and neither reading says what came up");

            // WW350, and this case is where the measurement it was filed on came apart. WW339 saw
            // the standing reading answer on both kinds and asserted it here; a later run of this
            // same fixture had the Win32 popup answer through the highlight instead, because which
            // question reaches a TrackPopupMenu first is a race between the shell highlighting an
            // entry and the menu window becoming enumerable.
            //
            // The race no longer decides what a trace says. Both readings are taken at the look that
            // answers, the menu gets one more poll where only the proxy answered, and Read prefers
            // the menu — which is a reading of the thing, where the highlight is a proxy for it.
            Assert.Equal(menu.Standing ?? menu.Highlighted, menu.Read);
            Assert.Contains(menu.Read!, menu.AsTraceStep().ReadBack!, StringComparison.Ordinal);

            // And the sentence carries whatever answered, so a reader is never shown one fact where
            // the run had two.
            if (menu.Standing is { } stood && menu.Highlighted is { } entry)
            {
                Assert.Contains(stood, menu.ToString(), StringComparison.Ordinal);
                Assert.Contains(entry, menu.ToString(), StringComparison.Ordinal);
            }
        }
        finally
        {
            // WW330's rule where the leak would be this suite's own: a menu left up owns the
            // foreground, and the next case in this class reads the desk. The menu first, because
            // it is the thing the act was keeping — and then the flyout and the desktop, which the
            // act took and could not give back while a menu it had opened was still standing.
            answering.DismissMenu();
            menu.PutBack();
        }
    }

    [Fact]
    public void A_menu_that_stands_without_taking_the_focus_is_seen_too()
    {
        // WW322, and it is the case that could not be written before. The verb reads what the desk
        // says holds the focus; a Win32 popup answers that and a WinForms drop-down does not, and a
        // real tray is as often one as the other. So the case above passed on every run while three
        // adopted cases failed on this route — the fixture put up the kind that answers, and the
        // reading was never taken against the kind that does not.
        //
        // Measured in the adopter before it was measured here: the tray was told, opened its menu 22
        // milliseconds after the key, held it for the whole six seconds the verb waits, and closed it
        // when the wait expired — logged from inside the application, while the engine reported that
        // nothing had been highlighted.
        using var answering = BusyDesk.Built(
            () => TrayIconFixture.Add("winwright dropdown", TrayMenuKind.DropDown));

        if (answering is null)
            return;

        var menu = NotificationArea.OpenMenu(answering.Tip, settleMs: 4000, pollMs: 40);

        try
        {
            if (BusyDesk.Excused(menu.AsAssertion("the icon shows its menu")))
                return;

            // The application was asked and drew one, which is the half that was never in doubt: the
            // adopter's own log said so while the engine said nothing was highlighted. Asserted
            // first, so a run where the shell never delivered the request is reported as that rather
            // than as the reading below failing.
            Assert.True(
                answering.MenusShown > 0,
                $"the icon was never asked for its menu: {menu.Because}");

            Assert.True(menu.Opened, menu.Because);
            Assert.Equal(Winwright.Tracing.StepVerdict.Ok, menu.AsTraceStep().Verdict);

            // WW339, and against the kind the focus cannot see: a drop-down never takes it, so this
            // is the arm where the answer has to be the menu standing rather than an entry.
            //
            // WW350 made the second line a measurement rather than a consequence. Both readings are
            // taken now, so a null highlight is this desk having answered nothing to that question —
            // where before it only meant the menu reading had got there first and the other was
            // never asked. Which makes this the case that would notice a drop-down starting to take
            // the focus, and that is worth noticing: it is the premise WW322 built the pair on.
            Assert.NotNull(menu.Standing);
            Assert.Null(menu.Highlighted);
            Assert.Contains("the menu", menu.AsTraceStep().ReadBack!, StringComparison.Ordinal);
        }
        finally
        {
            // WW330. The same pair as the case above: the menu is the case's to dismiss, and what
            // the act took is the act's to give back once there is nothing left to lose.
            answering.DismissMenu();
            menu.PutBack();
        }
    }

    [Fact]
    public void A_second_menu_with_the_same_nothing_for_a_name_is_a_second_menu()
    {
        // WW338. The reading that answers whether a menu came up compared two names, and a
        // drop-down with no accessible name is called "a menu with no name" both times — so an
        // application that put a second one up in answer reported that nothing came.
        //
        // Two icons, because that is what it takes to have two menus standing at once: the first
        // one's drop-down has AutoClose off and stays up, and the second icon answers with its own.
        // Both are unnamed, so before this the only thing telling them apart was nothing.
        using var first = BusyDesk.Built(() => TrayIconFixture.Add("winwright first menu", TrayMenuKind.DropDown));
        using var second = BusyDesk.Built(() => TrayIconFixture.Add("winwright second menu", TrayMenuKind.DropDown));

        if (first is null || second is null)
            return;

        var one = NotificationArea.OpenMenu(first.Tip, settleMs: 4000, pollMs: 40);

        try
        {
            if (BusyDesk.Excused(one.AsAssertion("the first icon shows its menu")))
                return;

            Assert.True(one.Opened, one.Because);

            // The first menu is still standing — nothing dismissed it — so the second act begins
            // with a menu on the desk that is not its own. That is the whole provocation.
            var two = NotificationArea.OpenMenu(second.Tip, settleMs: 4000, pollMs: 40);

            if (BusyDesk.Excused(two.AsAssertion("the second icon shows its menu")))
                return;

            Assert.True(
                second.MenusShown > 0,
                $"the second icon was never asked for its menu: {two.Because}");

            Assert.True(two.Opened, two.Because);
        }
        finally
        {
            second.DismissMenu();
            first.DismissMenu();
            one.PutBack();
        }
    }

    [Fact]
    public void A_menu_that_never_came_leaves_the_taskbar_as_it_found_it()
    {
        // WW330, and it stopped a session. The adopting repository's tray cases failed inside the
        // overflow in the guest, and the next run there — a different repository's suite, minutes
        // later — was refused before it started, with the desk probe reporting that the taskbar had
        // held the foreground for every look. A picture of the guest said what no exit code did: an
        // ordinary desktop, the chevron carrying the keyboard focus, its tooltip drawn beside it.
        //
        // The class's own icon is exactly this arm: it answers no menu on purpose, so the act opens
        // the flyout, focuses the icon, presses the key and has nothing to keep.
        if (!Placed || BusyDesk.Excused(NotificationArea.Reachable()))
            return;

        // Shut first, so what the act leaves is the act's. A flyout another case left standing
        // belongs to that case and this one must not shut it — which is the rule the engine follows
        // and therefore the rule that would make this assertion answer about the wrong opener.
        //
        // Excused rather than discarded: a shell that will not shut its own flyout is a desk this
        // run cannot work, and the assertion below would then be about that rather than about the
        // act. WW204's rule from the other end — a reading thrown away here is one whose cost lands
        // on the line that asserts afterwards.
        if (BusyDesk.Excused(NotificationArea.CloseOverflow().AsAssertion("the overflow starts shut")))
            return;

        var before = Foreground.Now();
        var taskbar = NotificationArea.Tray()?.Current.NativeWindowHandle ?? 0;

        var menu = NotificationArea.OpenMenu(Tip, settleMs: 1500, pollMs: 40);

        if (BusyDesk.Excused(menu.AsAssertion("the icon shows its menu")))
            return;

        Assert.False(menu.Opened, menu.Because);

        // The flyout the act opened is shut. This is the half the engine owns outright: the chevron
        // takes an invoke, needs no foreground, and a shell that refuses it is a desk this run
        // cannot work — which the excuse above has already stood the case down for.
        Assert.True(
            NotificationArea.Overflow() is null,
            "the act opened the overflow, found no menu, and left the flyout standing");

        // And the desktop is not the taskbar's. The guard is the honest half: where the shell
        // already held the desk before any of this ran, this act cannot be what left it there, and
        // asserting anyway would be reporting the desk as a defect in the code.
        if (taskbar == 0 || before.Window == taskbar)
            return;

        // Asserted this way round rather than against the window that held it before: putting a
        // foreground back is best effort — Windows refuses it to a process that does not own one —
        // and what was measured is not that some particular window lost the desk, it is that the
        // shell kept it for every run afterwards.
        Assert.True(
            Foreground.Now().Window != taskbar,
            $"the act left the taskbar holding the foreground: {Foreground.Now()}");
    }

    [Fact]
    public void The_shadow_behind_a_menu_is_not_the_largest_window_the_application_drew()
    {
        // WW346. `Largest` answers the largest window a process owns, sorted by area, and a menu's
        // drop shadow is drawn larger than the menu on every side: freewilly's menu is 188x108 and
        // the shadow behind it 190x111. So a caller asking for the window the application drew got
        // the one surface beside a menu that must never be photographed.
        //
        // Every caller in this tree means the same thing by it — twenty of them, all building an
        // automation root out of the answer — so what this asserts is that the answer is a window
        // the application drew, and the listing is printed where it is not.
        using var answering = BusyDesk.Built(
            () => TrayIconFixture.Add("winwright shadowed menu", TrayMenuKind.Win32));

        if (answering is null || BusyDesk.Excused(NotificationArea.Reachable()))
            return;

        var menu = NotificationArea.OpenMenu(answering.Tip, settleMs: 4000, pollMs: 40);

        try
        {
            if (BusyDesk.Excused(menu.AsAssertion("the icon shows its menu")))
                return;

            var windows = TopLevelWindows.OfProcess(Environment.ProcessId, smallest: 0);
            var largest = TopLevelWindows.Largest(Environment.ProcessId);

            var listing = string.Join(
                Environment.NewLine,
                windows.Select(one => $"    {one} popup={one.Popup} area={one.Bounds.Area}"));

            // The claim, and the whole of it: whatever comes back is not the shell's shadow, and no
            // listing carries one either. Named by class, because that is what the shadow is — a
            // window this process owns only because the popup in front of it does.
            Assert.True(
                largest is not null && !TopLevelWindows.DrawnByTheShell(largest.ClassName),
                $"the largest window is {largest}, which is the shadow the shell drew:{Environment.NewLine}{listing}");

            Assert.DoesNotContain(windows, one => TopLevelWindows.DrawnByTheShell(one.ClassName));

            // What this case cannot do is make the shadow the largest. It runs in this process, and
            // this process owns the suite's own windows — so the sort has real windows to put in
            // front of it, and the arm the defect lives on is a tray application whose only windows
            // are a menu and the shadow behind it. That one is provoked by a desk rather than a
            // case, and the rule it turns on is run one case below.
            Assert.True(menu.Opened, menu.Because);
        }
        finally
        {
            answering.DismissMenu();
        }
    }

    [Fact]
    public void What_the_shell_drew_is_named_rather_than_ruled_out_by_a_property()
    {
        // WW346, and the half a case can run. A shadow is not something this suite can put on the
        // desk — it is drawn by Windows behind a menu belonging to a process whose only windows are
        // that menu and that shadow — so the rule is separate from the walk, the way WW345 made the
        // desk probe's classification separate from its polling.
        Assert.True(TopLevelWindows.DrawnByTheShell("SysShadow"));
        Assert.True(TopLevelWindows.DrawnByTheShell("sysshadow"));

        // And narrow, which is the whole argument for a list. Every rule that would cover a shadow
        // covers something else: what is owned is also the menu, which is the window a tray
        // application's case is about, and what is composited is also a window the fixture draws
        // layered on purpose. So the one class the shell draws is named, and a menu, a window the
        // fixture drew and a class Windows would not answer are all left alone.
        Assert.False(TopLevelWindows.DrawnByTheShell("#32768"));
        Assert.False(TopLevelWindows.DrawnByTheShell("HwndWrapper[Winwright.Fixture;;]"));
        Assert.False(TopLevelWindows.DrawnByTheShell(""));
    }

    [Fact]
    public void Putting_the_desk_back_answers_for_the_desktop_and_not_only_the_flyout()
    {
        // WW344. The verb does two things and used to answer for one. It shuts the flyout and
        // returns a reading with a reason on it; then it calls SetForegroundWindow, whose whole
        // documented behaviour is that Windows refuses it to a process that does not own the
        // foreground, and said nothing about that at all — so a run where the shell kept the
        // desktop looked exactly like one where it did not.
        using var answering = BusyDesk.Built(
            () => TrayIconFixture.Add("winwright put back", TrayMenuKind.DropDown));

        if (answering is null || BusyDesk.Excused(NotificationArea.Reachable()))
            return;

        var menu = NotificationArea.OpenMenu(answering.Tip, settleMs: 4000, pollMs: 40);

        try
        {
            if (BusyDesk.Excused(menu.AsAssertion("the icon shows its menu")))
                return;

            var state = menu.PutBack();

            // Both halves are there, and the flyout half is the one that always was.
            Assert.Equal("shut", state.Flyout.What);

            // The desktop half is a comparison and not a return code, so it must agree with an
            // independent look at the same desk. This is the whole claim: the reading says what
            // happened rather than what was asked for.
            Assert.Equal(state.Desktop, !state.Asked || Foreground.Now().Window == state.Wanted);

            // And it says so either way. A sentence that only spoke when the desktop went back is
            // the silence WW330's investigation had to work around — a picture of a guest, taken by
            // hand, after a suite in another repository would not start.
            Assert.Contains("desktop", state.ToString(), StringComparison.Ordinal);
            Assert.Equal(state.Held, state.Flyout.Held && state.Desktop);
        }
        finally
        {
            // The menu alone. The flyout is what `PutBack` above shuts, and where the act opened
            // none there is nothing here to shut — so a CloseOverflow on this line would be a desk
            // reading thrown away for no question it could answer.
            answering.DismissMenu();
        }
    }

    [Fact]
    public void A_case_reads_the_menu_a_step_opened_and_the_case_hands_the_desk_back()
    {
        // WW343, and it is the shape of every adopter's tray case: one step opens the menu and the
        // next reads it. Both halves are asserted here because they pull against each other — a
        // restore at the step would put the foreground back while the menu stood, and a drop-down
        // goes the moment anything else takes the focus, so the tidying would dismiss the answer the
        // case came for. So: the second step still finds the menu, and the desk is given back after.
        //
        // The drop-down and not the Win32 kind, for WW322's reason: it is what freewilly and
        // claude-tray both put up, and it is the one that leaves the desk dirty because it does not
        // block the shell's thread while it stands.
        using var answering = BusyDesk.Built(
            () => TrayIconFixture.Add("winwright scenario menu", TrayMenuKind.DropDown));

        if (answering is null || BusyDesk.Excused(NotificationArea.Reachable()))
            return;

        // Shut first for the reason the case above shuts first: what the run leaves has to be the
        // run's, and a flyout somebody else left standing belongs to them.
        if (BusyDesk.Excused(NotificationArea.CloseOverflow().AsAssertion("the overflow starts shut")))
            return;

        var before = Foreground.Now();
        var taskbar = NotificationArea.Tray()?.Current.NativeWindowHandle ?? 0;

        var declared = Winwright.Scenarios.CaseDeclaration.Of(
            "the tray menu is opened and read",
            Winwright.Scenarios.StepDeclaration.Of(
                null, "open tray menu", tray: answering.Tip, named: "the icon shows its menu"),
            // WW356. MenuItem, and it is the adopters' own word: claude-tray's case reads
            // `Menu > MenuItem` and freewilly's does the same. It used to be Button here, measured
            // rather than read off the fixture's source — `ToolStripDropDown.Items.Add(string)`
            // builds a ToolStripButton, because CreateDefaultItem is ToolStrip's — and a locator
            // naming MenuItem matched nothing for three runs, saying only that nothing answered.
            //
            // So the fixture builds `ToolStripDropDownMenu` now and this case names what an adopter
            // names. That is the whole of the change: a locator proven here was one that would have
            // found nothing there.
            //
            // Named the way the engine allows, too: a locator that matched on the name fixes the
            // reading before the act runs, so claiming that name back is a step that cannot fail.
            // What is claimed instead is that the entry announces something — which is false in the
            // one way this case is about, a menu that went away between the step that opened it and
            // the step that reads it.
            // Ordered, because the menu has two entries and the engine refuses to guess between
            // them — which is the right refusal and is how this case learned the menu was standing
            // with both of them in it.
            Winwright.Scenarios.StepDeclaration.Of(
                "Menu > MenuItem[order=top]", "read", reads: "name", answers: true, named: "the first entry"));

        Winwright.Scenarios.CaseResult run;
        try
        {
            run = Winwright.Scenarios.CaseRun.Of(
                declared, AutomationElement.RootElement, TrayProject());
        }
        finally
        {
            // The menu is the case's to dismiss and the engine's restore does not close it: this
            // drop-down has AutoClose off, so nothing but this line takes it away.
            answering.DismissMenu();
        }

        // A desk that would not put up the menu at all is a hole and not a red, which is what the
        // engine already answers — so the case stands down rather than asserting about the shell.
        if (run.Verdict.Unchecked.Count > 0)
            return;

        // The step after the one that opened it read it. Before WW343 there was nowhere for the
        // restore to go that did not break this line.
        // The whole reading and not the failures alone. A case can end unpassed with nothing in that
        // list — a step that threw is a harness error and not an assertion — and a red carrying only
        // an empty string sends its reader to a debugger to find out what happened, which is a run
        // of this suite spent twice.
        Assert.True(
            run.Verdict.Outcome == Winwright.Verdicts.RunOutcome.Passed,
            string.Join(
                Environment.NewLine,
                run.Verdict.Failures.Select(one => $"  failed    {one}")
                    .Concat(run.Verdict.Broke.Select(one => $"  threw     {one}"))
                    .Concat(run.Verdict.Results.Select(one => $"  result    {one}"))
                    .Prepend($"  outcome   {run.Verdict.Outcome}")));

        // And the desk is not the taskbar's. Asserted this way round for WW330's reason: putting a
        // foreground back is best effort, and what was measured is not that one window lost the
        // desk but that the shell kept it for every run afterwards.
        if (taskbar == 0 || before.Window == taskbar)
            return;

        Assert.True(
            Foreground.Now().Window != taskbar,
            $"the case left the taskbar holding the foreground: {Foreground.Now()}");
    }

    /// <summary>
    /// A project for the case above: this run's own executable, and waits short enough that a menu
    /// that never came does not cost the class a minute. WW343.
    /// </summary>
    private static Winwright.Projects.ProjectDeclaration TrayProject()
    {
        var into = Path.Combine(Path.GetTempPath(), $"winwright-ww343-{Guid.NewGuid():N}");
        Directory.CreateDirectory(into);

        var path = Path.Combine(into, Winwright.Projects.ProjectDeclaration.FileName);
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Environment.ProcessPath)}},
              "timeouts": { "resolve": 4000, "act": 4000, "poll": 40 }
            }
            """);

        return Winwright.Projects.ProjectDeclaration.Load(path);
    }

    [Fact]
    public void An_absence_says_how_much_was_there_to_look_through()
    {
        // WW223. An empty flyout and a flyout holding four of the shell's own ended this sentence
        // the same way, and the difference is the one that says whether the shell is placing
        // anything at all — which is what an investigation of the intermittent red had to go and
        // measure by hand because the red did not carry it.
        var searched = NotificationArea.Find("winwright is not here", settleMs: 800, pollMs: 40);

        Assert.False(searched.Found);
        if (BusyDesk.Excused(searched.AsAssertion("the notification area could be looked through")))
            return;

        // Everywhere, so this is an answer about the icon and the counts are what it was measured
        // against. A search that could not look says something else entirely and is excused above.
        Assert.True(searched.Everywhere, searched.Sentence());
        Assert.Contains("on the bar", searched.Because, StringComparison.Ordinal);
        Assert.Contains("in the flyout", searched.Because, StringComparison.Ordinal);
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
    public void A_window_taking_the_foreground_shuts_the_flyout_under_whoever_was_looking_in_it()
    {
        // WW288's own question, asked as an experiment rather than waited for. Its design says
        // "measure first, then decide", and the reading it was waiting on was a rate in the excuse
        // ledger — which says how often and never what. This says what.
        //
        // Two candidates were ruled out by reading before this was written. It is not this suite
        // running two classes at once: every class that works the flyout is in the serial
        // collection, and the others name `OpenOverflow` in source they scan rather than call. And
        // it is not an application closing it, because there is no application here at all.
        var opened = NotificationArea.OpenOverflow();
        if (BusyDesk.Excused(opened.AsAssertion("the overflow opens")))
            return;

        Assert.True(opened.Held, opened.ToString());
        Assert.NotNull(NotificationArea.Overflow());

        // A window of this process, shown and therefore activated — the same event an adopting
        // application produces every time it raises a dialog, and the same one WW248 measured this
        // suite producing against its own launched fixtures.
        var before = Foreground.Now();

        using (PumpedDialog.Open("winwright dismisses the flyout"))
        {
            // The premise, read rather than assumed, and it is the whole reason this is a case and
            // not a paragraph: the first draft measured the flyout without ever checking that the
            // desktop had moved, which is a reading about an event that may not have happened —
            // the shape of green this project exists to refuse. A dialog that did not take the
            // foreground provokes nothing, so it settles nothing either way.
            var took = Foreground.Now();
            if (took == before)
            {
                Console.WriteLine(
                    $"the dialog did not take the foreground from {before}, so nothing was provoked "
                        + "and this measured nothing");

                return;
            }

            // Read once and not polled, deliberately: what is being measured is whether the flyout
            // survives the activation, and a poll would answer about the moment it chose.
            var survived = NotificationArea.Overflow() is not null;

            // Whichever way this desk answers, the reading is the finding — so it is printed rather
            // than asserted one way. A shell that dismisses its flyout on activation makes every
            // search in this engine racy against anything that raises a window; one that does not
            // leaves WW288 still looking for what shuts it.
            Console.WriteLine(
                survived
                    ? $"the flyout survived the foreground moving to {took}"
                    : $"the foreground moving to {took} shut the flyout");

            Assert.False(
                survived && NotificationArea.Hidden().Count == 0,
                "the flyout is standing and holds nothing, which is neither of the two states this measures");
        }
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


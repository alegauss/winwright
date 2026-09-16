using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Processes;
using Winwright.Projects;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// The tray menu that belongs to somebody else. WW451, and WW452 one step further in.
/// <para>
/// <c>open tray menu</c> focuses the icon and sends the application key, and the shell forwards that
/// to whoever owns the icon. Every case proving it — <c>NotificationAreaTests</c>, and the scenario
/// one beside it — adds the icon through <c>TrayIconFixture</c>, from inside the test host. So the
/// owner has always been the process driving it, and an adopter's never is: a tray application under
/// test is another process by definition.
/// </para>
/// <para>
/// What that cost is written in the verb's own source. WW322 repaired this engine for a WinForms
/// drop-down that answers no focus reading, and the paragraph above <c>Standing</c> says how it was
/// found — the adopter's tray opened its menu 22 milliseconds after the key and held it for the whole
/// six seconds the verb waits, logged from inside the application, while the engine reported that
/// nothing had been highlighted. A defect here, measured by reading somebody else's log.
/// </para>
/// <para>
/// And the second reason, which is what filed the task rather than the first. A run that looked
/// exactly like that defect turned out to be an adopter restoring a package cut four hours before
/// WW322 shipped: a suite that cannot make this shape cannot tell a stale package from a broken
/// route, and four hours went on telling them apart by hand.
/// </para>
/// <para>
/// Both kinds, because the kinds are what WW322 was about. A <c>TrackPopupMenu</c> answers the focus
/// reading and a drop-down does not, and the drop-down is the one both adopters put up — so a route
/// proven against the first alone is proven against the kind nobody ships.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class AdoptedTrayTests : IDisposable
{
    /// <summary>
    /// The backstop, and only that: each case stops its own tray before it returns, for the reason
    /// said where it does. What is left here is the flyout, which a search may have opened and which
    /// belongs to the class rather than to any one case.
    /// </summary>
    public void Dispose()
    {
        NotificationArea.CloseOverflow();
    }

    [Fact]
    public void The_menu_a_launched_tray_puts_up_is_opened_by_the_verb_an_adopter_calls() =>
        TheMenuBelongsToTheLaunchedProcess("dropdown");

    [Fact]
    public void The_other_kind_of_tray_menu_opens_across_the_same_boundary() =>
        TheMenuBelongsToTheLaunchedProcess("win32");

    /// <summary>
    /// WW451's claim: the window the menu is drawn in belongs to the launched process.
    /// <para>
    /// A method rather than a theory, for the reason <c>NotificationAreaTests</c> gives about the two
    /// kinds it drives: the spellings are the fixture's flag values, and a case taking one as a
    /// parameter reads as data where what differs is which defect is being reproduced.
    /// </para>
    /// </summary>
    /// <param name="kind">Which menu the launched fixture answers with.</param>
    private static void TheMenuBelongsToTheLaunchedProcess(string kind) =>
        WithTheMenuUp(kind, (pid, menu) =>
        {
            // What the verb read, which is the half an adopter's step prints. A drop-down answers no
            // focus reading, so this is `Standing` on one kind and either on the other — and a verb
            // that opened something and could say nothing about it is the state WW350 withdrew.
            Assert.True(menu.Read is not null, $"the menu opened and the verb read nothing of it: {menu}");

            // And the claim no case here could make before this one. Everything proving this verb
            // until now put its icon up from inside the test host, so "a menu appeared" has always
            // been compatible with the menu belonging to the harness — which is the one arrangement
            // an adopter never has.
            //
            // `Largest` and not an enumeration by hand, because WW346 is why it can be trusted with
            // this shape: the shell draws a `SysShadow` two pixels larger on every side of a menu,
            // the listing is sorted by area, and a process whose only windows are those two is
            // exactly what found that.
            var window = Waits.Until(
                "draw",
                $"the verb opened a menu and the launched tray ({pid}) owns no window",
                () => TopLevelWindows.Largest(pid));

            Assert.False(
                TopLevelWindows.DrawnByTheShell(window.ClassName),
                $"the largest window the tray owns is one the shell drew rather than its menu: {window}");
        });

    [Fact]
    public void The_entries_of_a_launched_tray_s_menu_answer_the_locator_an_adopter_writes() =>
        TheEntriesResolve("dropdown");

    [Fact]
    public void The_other_kind_s_entries_answer_the_same_locator() =>
        TheEntriesResolve("win32");

    /// <summary>
    /// WW452's claim: a locator resolves the entries of a menu this run did not put up.
    /// <para>
    /// WW369 reads both kinds as a shape and finds the same thing under each — a <c>Menu</c> holding
    /// two <c>MenuItem</c>s, named as the adopters name them. It walks <c>TrayIconFixture</c>'s menu,
    /// which the test host owns, so what it proves is that the tree is right when the harness put it
    /// there. WW451 crossed the boundary and stopped at the window.
    /// </para>
    /// <para>
    /// Resolved against the desktop and not against a window, because that is what the runner hands a
    /// tray case: a resident fixture draws nothing, so <c>Suite</c> gives its steps
    /// <c>RootElement</c>. The same root claude-tray's step gets, so a difference between the two is
    /// a difference about the application and not about how the case was arranged.
    /// </para>
    /// <para>
    /// Named and not <c>Menu &gt; MenuItem</c> bare, which is deliberate rather than tidy: a menu
    /// holds several entries, and a route step matching several is refused by <c>Walk</c> on purpose.
    /// The count is read separately below, because "the menu holds nothing" and "the menu holds two
    /// and the locator would not say which" are different findings that a single miss collapses.
    /// </para>
    /// </summary>
    /// <param name="kind">Which menu the launched fixture answers with.</param>
    private static void TheEntriesResolve(string kind) =>
        WithTheMenuUp(kind, (pid, menu) =>
        {
            var desktop = AutomationElement.RootElement;

            // Every menu standing on the desktop, counted rather than assumed to be one. The shell
            // owns menus of its own and the overflow flyout may be up, so which of them is the
            // launched tray's is precisely what a bare `Menu` step cannot say.
            var menus = Resolve.Matching(desktop, Locator.Parse("Menu").Steps[0]);
            var entries = menus
                .Select(one => Resolve.Matching(one, Locator.Parse("MenuItem").Steps[0]).Count)
                .ToList();

            var said = $"{menus.Count} menu(s) on the desktop holding [{string.Join(", ", entries)}] "
                + $"entry(ies), with the verb reporting {menu}";

            Assert.True(menus.Count > 0, $"the verb says a menu is standing and the desktop has none: {said}");
            Assert.True(entries.Exists(one => one > 0), $"every menu on the desktop is empty: {said}");

            // And the locator itself, the shape an adopter's step carries: a control type, a name,
            // and a descendant step. Resolved through the engine's own route so that a miss arrives
            // diagnosed rather than as a count that does not match.
            foreach (var entry in new[] { "winwright open", "winwright quit" })
            {
                var locator = Locator.Parse($"Menu > MenuItem[name=\"{entry}\"]");
                var resolved = Resolve.Until(desktop, locator, Timeouts.Defaults["resolve"], pollMs: 50);

                Assert.True(
                    resolved.Found,
                    $"'{locator.Text}' resolved nothing against the desktop while the tray ({pid}) held"
                        + $" its menu up — {said}{Environment.NewLine}{resolved.Miss}");

                Assert.Equal(entry, resolved.Facts!.Name);
            }
        });

    /// <summary>
    /// Launch a tray, open its menu through the verb, hand it to the caller, and take it down.
    /// <para>
    /// A register per case and not per class. The menu stands with <c>AutoClose</c> off and nothing
    /// in this process can close a control another process made, so the process that owns it is the
    /// only door there is — and a class-wide register would leave one case's menu and icon on the
    /// desk for the next case to read, which is the state this whole class is about being able to
    /// tell apart from a defect.
    /// </para>
    /// </summary>
    /// <param name="kind">Which menu the launched fixture answers with.</param>
    /// <param name="read">What to assert about it: the launched process, and what the verb answered.</param>
    private static void WithTheMenuUp(string kind, Action<int, TrayMenu> read)
    {
        // The shell, before anything is launched. A desk whose taskbar is covered answers every step
        // below with a sentence about this repository, which is WW190's misattribution.
        if (BusyDesk.Excused(NotificationArea.Reachable()))
            return;

        using var register = new ProcessRegister();

        var launched = Attachable.Launch(register, Fixture.Started($"--tray={kind}"));
        var tip = Fixture.TrayTip(launched.Pid);

        if (!Placed(tip, launched.Pid))
            return;

        // Before the key, and it is what makes every reading after it mean anything: a tray
        // application owns no window a person can look at, which is what `--resident` says about a
        // process that draws nothing. A menu already standing would make "its menu is up" true of a
        // desk nobody had acted on — the false green this project exists to withdraw.
        var before = TopLevelWindows.OfProcess(launched.Pid);
        Assert.True(
            before.Count == 0,
            $"the launched tray owns {before.Count} window(s) before anything asked it for a menu: "
                + string.Join("; ", before));

        var menu = NotificationArea.OpenMenu(tip, settleMs: 4000, pollMs: 40);

        try
        {
            if (BusyDesk.Excused(menu.AsAssertion("the launched tray shows its menu")))
                return;

            read(launched.Pid, menu);
        }
        finally
        {
            // WW330's rule, in the order the desk is left in. The menu goes first, by stopping the
            // process that owns it; then the taskbar is put back to a desktop with nothing of this
            // case's standing on it.
            register.Stop(launched);
            menu.PutBack();
        }
    }

    /// <summary>
    /// Wait until the shell has put the launched tray's icon somewhere a reading can find it.
    /// <para>
    /// WW119's measurement, met across a process boundary. The shell accepting the icon and the
    /// shell placing it and building the automation tree under it are different moments on different
    /// schedules, and a case that looks at the first is racing the second — measured there across
    /// four consecutive full-suite runs, two green and two red with two failures each, every one of
    /// them in the notification-area cases.
    /// </para>
    /// <para>
    /// The three endings are WW179's, kept apart. A shell that would not let this run look is a hole;
    /// a shell placing nobody's icon is a hole about the desk rather than about the launch; and a
    /// shell that looked everywhere and found nothing is the fixture genuinely failing, which is the
    /// only one of the three worth a red.
    /// </para>
    /// </summary>
    /// <param name="tip">What the shell calls the launched tray's icon.</param>
    /// <param name="pid">The launched tray, which the failure names.</param>
    /// <returns>False where the desk was excused, which is the caller's cue to return.</returns>
    private static bool Placed(string tip, int pid)
    {
        var last = default(TraySearch);
        var placed = Waits.Trying("placed", () =>
        {
            last = NotificationArea.Find(tip, openingTheOverflow: true, settleMs: 1000, pollMs: 25);
            return last.Icon is not null;
        });

        if (placed.Happened)
            return true;

        // The search's own sentence and the search's own verdict, neither of them restated here: a
        // shell that never opened the flyout was not asked whether the icon is there.
        if (last is { Everywhere: false }
            && BusyDesk.Excused(Precondition.Absent(TraySearch.PreconditionName, last.Because)))
        {
            return false;
        }

        if (BusyDesk.Excused(NotificationArea.Placing()))
            return false;

        Assert.Fail(Waits.Missed(
            "placed",
            $"the launched tray ({pid}) registered '{tip}' and no reading could find it — {last?.Because}",
            placed));

        return false;
    }
}

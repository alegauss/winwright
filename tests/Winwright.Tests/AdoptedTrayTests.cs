using Winwright.Acting;
using Winwright.Processes;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// The tray menu that belongs to somebody else. WW451.
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
    /// belongs to the class rather than to either case.
    /// </summary>
    public void Dispose()
    {
        NotificationArea.CloseOverflow();
    }

    [Fact]
    public void The_menu_a_launched_tray_puts_up_is_opened_by_the_verb_an_adopter_calls() =>
        TheMenuOpens("dropdown");

    [Fact]
    public void The_other_kind_of_tray_menu_opens_across_the_same_boundary() =>
        TheMenuOpens("win32");

    /// <summary>
    /// Launch a tray, open its menu through the verb, and read that the menu is the launched
    /// process's own.
    /// <para>
    /// A method rather than a theory, for the reason <c>NotificationAreaTests</c> gives about the two
    /// kinds it drives: the spellings are the fixture's flag values, and a case taking one as a
    /// parameter reads as data where what differs is which defect is being reproduced.
    /// </para>
    /// </summary>
    /// <param name="kind">Which menu the launched fixture answers with.</param>
    private static void TheMenuOpens(string kind)
    {
        // The shell, before anything is launched. A desk whose taskbar is covered answers every step
        // below with a sentence about this repository, which is WW190's misattribution.
        if (BusyDesk.Excused(NotificationArea.Reachable()))
            return;

        // A register per case and not per class. The menu stands with `AutoClose` off and nothing in
        // this process can close a control another process made, so the process that owns it is the
        // only door there is — and a class-wide register would leave the first case's menu and icon
        // on the desk for the second case to read, which is the state this whole class is about
        // being able to tell apart from a defect.
        using var register = new ProcessRegister();

        var launched = Attachable.Launch(register, Fixture.Started($"--tray={kind}"));
        var tip = Fixture.TrayTip(launched.Pid);

        if (!Placed(tip, launched.Pid))
            return;

        // Before the key, and it is what makes the reading after it mean anything: a tray application
        // owns no window a person can look at, which is what `--resident` says about a process that
        // draws nothing. A menu already standing would make "its menu is up" true of a desk nobody
        // had acted on — which is the false green this project exists to withdraw.
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

            // What the verb read, which is the half an adopter's step prints. A drop-down answers no
            // focus reading, so this is `Standing` on one kind and either on the other — and a verb
            // that opened something and could say nothing about it is the state WW350 withdrew.
            Assert.True(menu.Read is not null, $"the menu opened and the verb read nothing of it: {menu}");

            // And the claim no case here could make before this one: the window the menu is drawn in
            // belongs to the launched process. Everything proving this verb until now put its icon up
            // from inside the test host, so "a menu appeared" has always been compatible with the
            // menu belonging to the harness — which is the one arrangement an adopter never has.
            //
            // `Largest` and not an enumeration by hand, because WW346 is why it can be trusted with
            // this shape: the shell draws a `SysShadow` two pixels larger on every side of a menu,
            // the listing is sorted by area, and a process whose only windows are those two is
            // exactly what found that.
            var window = Waits.Until(
                "draw",
                $"the verb opened a menu and the launched tray ({launched.Pid}) owns no window",
                () => TopLevelWindows.Largest(launched.Pid));

            Assert.False(
                TopLevelWindows.DrawnByTheShell(window.ClassName),
                $"the largest window the tray owns is one the shell drew rather than its menu: {window}");
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

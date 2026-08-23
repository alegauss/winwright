using System.Collections.ObjectModel;
using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Verdicts;

namespace Winwright.Windowing;

/// <summary>
/// What this desktop can do, asked once and up front.
/// <para>
/// A machine with no interactive session, no input desktop to reach, a display that renders
/// nothing, or missing automation assemblies cannot observe anything at all — and every one of
/// those currently arrives dressed as a failing assertion about the code under test. Read here,
/// they arrive as what they are: a report about the machine, taken before a scenario blames a
/// repository for the desk it ran on.
/// </para>
/// <para>
/// Every answer is a <see cref="Precondition"/> and not a boolean, because that is already the one
/// thing in this project entitled to turn a check into a hole rather than a failure. On a hosted
/// runner the reading either changes nothing or names a condition that was being suffered in
/// silence, and which of the two it is has never been measured — so it is reported either way,
/// met conditions included.
/// </para>
/// </summary>
public sealed record Desk
{
    /// <summary>The names a scenario refers to these conditions by, so a hole can require one.</summary>
    public const string InteractiveSession = "an interactive session";

    /// <summary>The input desktop — absent on a locked workstation, or under a service.</summary>
    public const string InputDesktop = "an input desktop this run can reach";

    /// <summary>A display that draws. Absent on a headless runner and on a disconnected session.</summary>
    public const string RenderingDisplay = "a display that renders";

    /// <summary>Coordinates that mean what they say, which is what a click and a copy both need.</summary>
    public const string TrustworthyCoordinates = "coordinates in the space the window lives in";

    /// <summary>The UI Automation assemblies, without which nothing here can read a tree at all.</summary>
    public const string AutomationAssemblies = "the UI Automation assemblies";

    /// <summary>Something owning the keyboard. Absent says nothing does, which is a locked desk.</summary>
    public const string ForegroundToTake = "a foreground to take";

    private Desk(IReadOnlyList<Precondition> conditions, int automationLooks, int automationWaitedMs)
    {
        Conditions = conditions;
        AutomationLooks = automationLooks;
        AutomationWaitedMs = automationWaitedMs;
    }

    /// <summary>Every condition this reading took, met and absent alike, in the order it took them.</summary>
    public IReadOnlyList<Precondition> Conditions { get; }

    /// <summary>The ones this machine does not have, in the same order.</summary>
    public IReadOnlyList<Precondition> Absent =>
        new ReadOnlyCollection<Precondition>(Conditions.Where(one => !one.Satisfied).ToList());

    /// <summary>
    /// How many looks it took to settle whether UI Automation is reachable, and how long they took.
    /// <para>
    /// WW140: a reading that needed a second attempt says so. One look is the ordinary answer; more
    /// than one means this machine failed the call and then answered, which is a fact about the
    /// desk worth having in front of whoever is reading a run that behaved oddly.
    /// </para>
    /// </summary>
    public int AutomationLooks { get; }

    /// <summary>How long those looks took, in milliseconds.</summary>
    public int AutomationWaitedMs { get; }

    /// <summary>Whether reading this desk had to wait for UI Automation to settle.</summary>
    public bool AutomationSettled => AutomationLooks > 1;

    /// <summary>Whether this desk can observe anything at all.</summary>
    public bool CanObserve => Absent.Count == 0;

    /// <summary>
    /// The first thing this machine lacks, or null where it lacks nothing. First rather than
    /// worst: they are read in the order they stop each other mattering, so a desk with no
    /// interactive session is reported as that and not as six separate absences downstream of it.
    /// </summary>
    public Precondition? FirstAbsent => Absent.Count == 0 ? null : Absent[0];

    /// <summary>
    /// A reading already decided against, carrying the one condition that decided it.
    /// <para>
    /// WW156. The seam a refusal is provable through, and it opens in one direction only: a
    /// satisfied precondition is refused, so nothing here can assemble a desk that observes. That
    /// asymmetry is the whole licence for this being public in a project whose thesis is that a
    /// green is earned — a fabricated absence costs a run that would have passed, and a fabricated
    /// presence costs the run's meaning.
    /// </para>
    /// <para>
    /// It exists because none of the six is forceable in process. Coordinates are fixed by the
    /// module initialiser before anything runs, the assemblies do not break on request, and a
    /// session or an input desktop would need the workstation locked under the suite. Left with no
    /// seam, the refusal path is reachable only on a machine nobody has, which is to say never.
    /// </para>
    /// </summary>
    /// <param name="absent">What this desk does not have.</param>
    /// <exception cref="ArgumentException">Where the precondition is one this machine has.</exception>
    public static Desk Blocked(Precondition absent)
    {
        ArgumentNullException.ThrowIfNull(absent);
        if (absent.Satisfied)
        {
            throw new ArgumentException(
                $"'{absent.Name}' was met, so it decides nothing and this desk would observe",
                nameof(absent));
        }

        return new Desk(new ReadOnlyCollection<Precondition>([absent]), 1, 0);
    }

    /// <summary>
    /// The verdict a run gets on this desk, and null where the desk can observe and the run is
    /// free to proceed.
    /// <para>
    /// WW156. One result and not one per case. A suite that ran every check against a machine with
    /// no input desktop reports N holes that each rediscover the same fact, and a reader has to
    /// notice they are all the same sentence to learn there was only ever one problem.
    /// </para>
    /// <para>
    /// Degraded, never failed: nothing here is a statement about the application, and exit 1 sends
    /// a reader to a repository that has done nothing wrong. This is the reading WW1 opened the
    /// third outcome for.
    /// </para>
    /// <para>
    /// Measured against a real desk rather than a constructed one, in a Windows guest, by running
    /// the same probe twice. Given a session it read all six met and refused nothing. Denied one,
    /// two conditions went absent — the input desktop and the foreground — and the verdict came
    /// back Degraded, exit 2, one result, none run. Two absences and one hole is the part worth
    /// keeping: the refusal names what stopped the run first, not everything downstream of it.
    /// </para>
    /// </summary>
    /// <param name="run">What the run would have checked, named as a whole.</param>
    public RunVerdict? Refusal(string run)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(run);
        return CanObserve ? null : RunVerdict.Over([Excuse(run)]);
    }

    /// <summary>Read the desk now.</summary>
    public static Desk Read()
    {
        var automation = Automation();
        return new Desk(
            new ReadOnlyCollection<Precondition>(
            [
                Session(),
                Desktop(),
                Display(),
                Coordinates(),
                automation.Condition,
                Keyboard(),
            ]),
            automation.Looks,
            automation.WaitedMs);
    }

    /// <summary>
    /// The hole an assertion becomes when this desk cannot observe it. Refused where the desk can
    /// observe: a check excused on a machine that could have run it is a check nobody notices went
    /// missing, which is the green this project exists to withdraw.
    /// </summary>
    /// <param name="assertion">The assertion that will not run.</param>
    /// <exception cref="InvalidOperationException">Where nothing about this desk excuses anything.</exception>
    public AssertionResult Excuse(string assertion) =>
        FirstAbsent is null
            ? throw new InvalidOperationException(
                $"this desk has every condition, so nothing about it explains why '{assertion}' did not run")
            : AssertionResult.Unchecked(assertion, FirstAbsent);

    /// <summary>The whole reading in one sentence, said either way.</summary>
    public string Sentence()
    {
        // Said whichever way the reading went: a desk that answered on the second look is one that
        // failed on the first, and a run that behaved oddly is read by somebody looking for that.
        var settled = AutomationSettled
            ? $" UI Automation answered on look {AutomationLooks} after {AutomationWaitedMs} ms."
            : "";

        return CanObserve
            ? $"this desk can be observed: all {Conditions.Count} conditions are met.{settled}"
            : $"this desk cannot observe {(Absent.Count == 1 ? "everything" : "much")}: "
                + string.Join("; ", Absent.Select(one => $"{one.Name} — {one.Absence}")) + $".{settled}";
    }

    /// <summary>One line per condition, met ones included, for whoever prints a report.</summary>
    public IReadOnlyList<string> Render() => new ReadOnlyCollection<string>(
        Conditions
            .Select(one => one.Satisfied ? $"  ok      {one.Name}" : $"  absent  {one.Name} - {one.Absence}")
            .ToList());

    /// <summary>The report as a block of text, the sentence first.</summary>
    public override string ToString() => string.Join('\n', [Sentence(), .. Render()]);

    private static Precondition Session()
    {
        if (!Environment.UserInteractive)
            return Precondition.Absent(InteractiveSession, "this process is not running in an interactive session");

        var station = Win32.GetProcessWindowStation();
        if (station == 0)
            return Precondition.Absent(InteractiveSession, "this process has no window station");

        // A service runs on a station of its own that never has a visible desktop, and that is the
        // difference between "nothing was on screen" and "there is no screen to be on".
        if (!Win32.GetUserObjectInformationW(
                station, Win32.UserObjectFlags, out var flags, System.Runtime.InteropServices.Marshal.SizeOf<Win32.UserObjectFlagsInfo>(), out _))
        {
            return Precondition.Met(InteractiveSession);
        }

        return (flags.Flags & Win32.StationVisible) != 0
            ? Precondition.Met(InteractiveSession)
            : Precondition.Absent(InteractiveSession, "this process's window station has no visible desktop");
    }

    private static Precondition Desktop()
    {
        var desktop = Win32.OpenInputDesktop(0, false, Win32.DesktopReadObjects);
        if (desktop == 0)
        {
            return Precondition.Absent(
                InputDesktop,
                "the input desktop would not open, which is what a locked workstation and a secure "
                    + "desktop both look like from here");
        }

        Win32.CloseDesktop(desktop);
        return Precondition.Met(InputDesktop);
    }

    private static Precondition Display()
    {
        var monitors = Win32.GetSystemMetrics(Win32.MonitorCount);
        var width = Win32.GetSystemMetrics(Win32.VirtualScreenWidth);
        var height = Win32.GetSystemMetrics(Win32.VirtualScreenHeight);
        var remote = Win32.GetSystemMetrics(Win32.RemoteSession) != 0 ? ", over a remote session" : "";

        if (monitors <= 0)
            return Precondition.Absent(RenderingDisplay, $"no display is attached{remote}");

        return width > 0 && height > 0
            ? Precondition.Met(RenderingDisplay)
            : Precondition.Absent(
                RenderingDisplay, $"{monitors} display(s) are attached and the virtual screen measures {width}x{height}{remote}");
    }

    private static Precondition Coordinates()
    {
        var seen = DisplayAwareness.Current();
        return seen == DpiAwareness.PerMonitor
            ? Precondition.Met(TrustworthyCoordinates)
            : Precondition.Absent(TrustworthyCoordinates, DisplayAwareness.Ensure().ToString().TrimEnd('.'));
    }

    /// <summary>
    /// Whether a failure to reach UI Automation is one a second look could answer differently.
    /// <para>
    /// WW140, and the whole of the decision. An assembly that will not load never will: the file is
    /// not there, or the type is not in it, or the image is the wrong architecture, and no amount of
    /// waiting changes any of those. A COM call that failed is the other thing entirely — measured
    /// once in a loaded run as "Unexpected HRESULT has been returned from a call to a COM
    /// component", against a machine that answered on the next look and every look after it.
    /// </para>
    /// <para>
    /// Anything not named here is not caught at all. A reading that swallowed every exception would
    /// report a desk it never managed to look at, which is the shape of green this project exists
    /// to withdraw.
    /// </para>
    /// </summary>
    /// <param name="failure">What the call threw.</param>
    public static bool WorthAnotherLook(Exception failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return Caught(failure)
            && failure is InvalidOperationException or System.Runtime.InteropServices.COMException;
    }

    /// <summary>
    /// Whether this reading answers for a failure at all, rather than letting it out.
    /// <para>
    /// Three outcomes and not two. A failure this catches is either settled — the assemblies are
    /// not loadable and never will be — or worth another look. A failure it does not catch is
    /// neither: it leaves, because a reading that swallowed every exception would report a desk it
    /// never managed to look at, and that is the green this project exists to withdraw.
    /// </para>
    /// </summary>
    /// <param name="failure">What the call threw.</param>
    public static bool Caught(Exception failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return failure is FileNotFoundException or TypeLoadException or BadImageFormatException
            or InvalidOperationException or System.Runtime.InteropServices.COMException;
    }

    /// <summary>
    /// How long a blip gets to pass before this calls UI Automation unusable. Short, because it is
    /// read once a run and a transient COM failure answers on the next look or not at all.
    /// </summary>
    private const int SettleMs = 2000;

    /// <summary>
    /// Whether UI Automation can be reached, and how many looks it took.
    /// <para>
    /// WW140. This caught two different facts with one clause. A machine with no automation
    /// assemblies fails the call every time; a machine under load fails it once and answers the
    /// next moment — and reporting them identically is this block's own criterion pointed the wrong
    /// way, since a blip reported as a missing subsystem is a defect in the desk reported as a fact
    /// about it. Caught once in a loaded full run, against a class that passes six times of six on
    /// its own.
    /// </para>
    /// <para>
    /// So the two are told apart by which failure arrived. An assembly that will not load never
    /// will, and is answered on the first attempt rather than after a deadline; a COM call that
    /// failed is looked at again, the way every other reading here that can lose a race is.
    /// </para>
    /// </summary>
    private static (Precondition Condition, int Looks, int WaitedMs) Automation()
    {
        string? blip = null;

        var seen = Attempt.Until<Precondition>(
            () =>
            {
                try
                {
                    // Touched rather than reflected over: the assemblies load lazily, so a
                    // reference present on disk and unusable at run time reads as present.
                    return AutomationElement.RootElement is null
                        ? Precondition.Absent(AutomationAssemblies, "UI Automation answered with no root element")
                        : Precondition.Met(AutomationAssemblies);
                }
                catch (Exception missing) when (Caught(missing) && !WorthAnotherLook(missing))
                {
                    // Settled on the first look: no second attempt loads an assembly that is not
                    // there, and spending a deadline to learn it is the cost this task refuses.
                    return Precondition.Absent(
                        AutomationAssemblies,
                        $"UI Automation is not usable here: {missing.GetType().Name} - {missing.Message}");
                }
                catch (Exception unsettled) when (WorthAnotherLook(unsettled))
                {
                    blip = $"{unsettled.GetType().Name} - {unsettled.Message}";
                    return null;
                }
            },
            SettleMs,
            pollMs: 25);

        if (seen.Found)
            return (seen.Value!, seen.Polls, seen.WaitedMs);

        // Every look failed the transient way, which after a deadline is not transient any more.
        return (
            Precondition.Absent(
                AutomationAssemblies,
                $"UI Automation kept failing for {seen.WaitedMs} ms over {seen.Polls} looks: {blip}"),
            seen.Polls,
            seen.WaitedMs);
    }

    private static Precondition Keyboard()
    {
        var holder = Foreground.Now();
        return holder.Window == 0
            ? Precondition.Absent(ForegroundToTake, "nothing owns the foreground, which is a locked or switched desk")
            : Precondition.Met(ForegroundToTake);
    }
}

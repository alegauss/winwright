using System.Collections.ObjectModel;
using System.Windows.Automation;

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

    private Desk(IReadOnlyList<Precondition> conditions)
    {
        Conditions = conditions;
    }

    /// <summary>Every condition this reading took, met and absent alike, in the order it took them.</summary>
    public IReadOnlyList<Precondition> Conditions { get; }

    /// <summary>The ones this machine does not have, in the same order.</summary>
    public IReadOnlyList<Precondition> Absent =>
        new ReadOnlyCollection<Precondition>(Conditions.Where(one => !one.Satisfied).ToList());

    /// <summary>Whether this desk can observe anything at all.</summary>
    public bool CanObserve => Absent.Count == 0;

    /// <summary>
    /// The first thing this machine lacks, or null where it lacks nothing. First rather than
    /// worst: they are read in the order they stop each other mattering, so a desk with no
    /// interactive session is reported as that and not as six separate absences downstream of it.
    /// </summary>
    public Precondition? FirstAbsent => Absent.Count == 0 ? null : Absent[0];

    /// <summary>Read the desk now.</summary>
    public static Desk Read() => new(new ReadOnlyCollection<Precondition>(
    [
        Session(),
        Desktop(),
        Display(),
        Coordinates(),
        Automation(),
        Keyboard(),
    ]));

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
    public string Sentence() => CanObserve
        ? $"this desk can be observed: all {Conditions.Count} conditions are met."
        : $"this desk cannot observe {(Absent.Count == 1 ? "everything" : "much")}: "
            + string.Join("; ", Absent.Select(one => $"{one.Name} — {one.Absence}")) + ".";

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

    private static Precondition Automation()
    {
        try
        {
            // Touched rather than reflected over: the assemblies load lazily, so a reference that
            // is present on disk and unusable at run time reads as present until something asks.
            return AutomationElement.RootElement is null
                ? Precondition.Absent(AutomationAssemblies, "UI Automation answered with no root element")
                : Precondition.Met(AutomationAssemblies);
        }
        catch (Exception unusable)
            when (unusable is FileNotFoundException or TypeLoadException or BadImageFormatException
                or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            return Precondition.Absent(
                AutomationAssemblies, $"UI Automation is not usable here: {unusable.GetType().Name} - {unusable.Message}");
        }
    }

    private static Precondition Keyboard()
    {
        var holder = Foreground.Now();
        return holder.Window == 0
            ? Precondition.Absent(ForegroundToTake, "nothing owns the foreground, which is a locked or switched desk")
            : Precondition.Met(ForegroundToTake);
    }
}

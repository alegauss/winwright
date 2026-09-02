using System.Collections.ObjectModel;

using Winwright.Processes;

namespace Winwright.Windowing;

/// <summary>
/// Every top-level window a process owns, found by walking the desktop and asking each window who
/// owns it — rather than by asking the process, which names at most one and names none at all for
/// a tray application whose only windows are owned popups.
/// <para>
/// The size filter is the other half. Every process carries tool and message windows of no size
/// and no interest, and a listing full of them is one nobody reads; a toast, a balloon and a menu
/// are all comfortably above the floor, so the filter costs nothing that was wanted.
/// </para>
/// </summary>
public static class TopLevelWindows
{
    /// <summary>
    /// The smallest window worth reporting, in pixels on a side. Chosen to sit under anything a
    /// person can see and over the 0x0 and 1x1 helpers a framework leaves lying about.
    /// </summary>
    public const int SmallestInteresting = 16;

    /// <summary>
    /// Window classes the shell draws on an application's behalf, which the application never drew
    /// and no caller ever addresses. WW346.
    /// <para>
    /// <c>SysShadow</c> is the drop shadow behind a menu, and it is bigger than the menu: measured
    /// in freewilly, whose menu is 188x108 with a 190x111 shadow behind it. The listing is sorted by
    /// area, so a tray application showing a menu answered <see cref="Largest"/> with its shadow —
    /// which is the one surface beside a menu that must never be photographed, and since WW334 a
    /// capture of it is refused. So a caller that asked for the window the application drew got a
    /// refusal it did not expect, about a window it did not know it had asked for.
    /// </para>
    /// <para>
    /// A list and not a rule, and narrow on purpose. Every rule that would cover it covers something
    /// else too: skipping what is owned skips the menu, which is the window a tray application's own
    /// case is about, and skipping what <see cref="Capturing.SeeThrough"/> calls composited skips a
    /// layered window an application drew deliberately — the fixture has three of those behind
    /// <c>--layered</c>. What is true of a shadow and of nothing else is that the shell drew it, and
    /// the only way to say that is to name it.
    /// </para>
    /// </summary>
    public static IReadOnlyList<string> ShellDrawn { get; } = new ReadOnlyCollection<string>(["SysShadow"]);

    /// <summary>
    /// Whether the shell drew this window rather than the application. WW346, and separate from the
    /// walk for the reason WW345 made the desk probe's classification separate from its polling: a
    /// rule that can only be reached by enumerating a real process's real windows is a rule nothing
    /// can run, and a shadow is not something a case can put on the desk.
    /// </summary>
    /// <param name="className">The window class, as <c>GetClassNameW</c> answers it.</param>
    public static bool DrawnByTheShell(string className) =>
        ShellDrawn.Contains(className, StringComparer.OrdinalIgnoreCase);

    /// <summary>Every top-level window that process owns, largest first.</summary>
    /// <param name="pid">The process to ask about.</param>
    /// <param name="smallest">The size floor, on a side. Zero reports everything, helpers included.</param>
    /// <param name="visibleOnly">Whether to skip windows nobody can see — both the ones Windows
    /// does not consider visible and the ones the compositor has cloaked. False reports every
    /// window with its <see cref="TopLevelWindow.Cloak"/> filled in, so the difference stays
    /// readable rather than being a filter with no way back through it.</param>
    public static IReadOnlyList<TopLevelWindow> OfProcess(
        int pid, int smallest = SmallestInteresting, bool visibleOnly = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(smallest);

        var found = new List<TopLevelWindow>();
        Win32.EnumWindows(
            (window, _) =>
            {
                Win32.GetWindowThreadProcessId(window, out var owner);
                if (owner != (uint)pid)
                    return true;

                var visible = Win32.IsWindowVisible(window);
                if (visibleOnly && !visible)
                    return true;

                // The style bits are not the question a caller asking for visible windows is
                // asking. Measured on a stock Windows 11 desktop: 27 windows call themselves
                // visible and 12 of them are cloaked, so without this the listing is nearly half
                // windows that are not on the screen and cannot be photographed.
                var cloak = Cloaking.Of(window);
                if (visibleOnly && cloak != Cloak.NotCloaked)
                    return true;

                // WW346. Before the rectangle, because the rectangle is what makes it win: a shadow
                // is drawn larger than the thing it is behind, so the sort at the bottom of this
                // method puts it first. Skipped in the listing and not only in `Largest`, because a
                // caller enumerating a process's windows did not ask for the shell's either.
                var named = Win32.ClassOf(window);
                if (DrawnByTheShell(named))
                    return true;

                if (!Win32.GetWindowRect(window, out var rectangle))
                    return true;

                var bounds = new WindowBounds(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
                if (bounds.Width < smallest || bounds.Height < smallest)
                    return true;

                found.Add(new TopLevelWindow(
                    window,
                    pid,
                    Win32.TextOf(window),
                    named,
                    bounds,
                    visible,
                    Win32.GetWindow(window, Win32.GwOwner),
                    cloak,
                    Win32.IsPopup(window)));

                return true;
            },
            0);

        found.Sort((left, right) => right.Bounds.Area.CompareTo(left.Bounds.Area));
        return found;
    }

    /// <summary>The same, for something this run started.</summary>
    public static IReadOnlyList<TopLevelWindow> OfProcess(
        LaunchedProcess process, int smallest = SmallestInteresting, bool visibleOnly = true)
    {
        ArgumentNullException.ThrowIfNull(process);
        return OfProcess(process.Pid, smallest, visibleOnly);
    }

    /// <summary>
    /// The largest one, which is the frame where there is a frame — and null where the process
    /// owns nothing above the floor, which is a statement rather than a zero handle to interpret.
    /// </summary>
    public static TopLevelWindow? Largest(int pid, int smallest = SmallestInteresting, bool visibleOnly = true) =>
        OfProcess(pid, smallest, visibleOnly).FirstOrDefault();
}

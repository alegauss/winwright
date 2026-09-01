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

                if (!Win32.GetWindowRect(window, out var rectangle))
                    return true;

                var bounds = new WindowBounds(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
                if (bounds.Width < smallest || bounds.Height < smallest)
                    return true;

                found.Add(new TopLevelWindow(
                    window,
                    pid,
                    Win32.TextOf(window),
                    Win32.ClassOf(window),
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

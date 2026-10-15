using Winwright.Locating;

namespace Winwright.Windowing;

/// <summary>
/// An Explorer window a fixture opened on a folder, beside the window under test, and closed again
/// when the case is done. WW536.
/// <para>
/// Found by quickshell (QS292). A drop with <c>toDesktop</c> needs a window to land in, and for a file
/// that is an Explorer folder the case can check afterwards. A fixture launched the application under
/// test and nothing else, so <c>ExplorerDropRun</c> opened the folder from C#, found the window by its
/// class and title, put the two side by side, and closed it by its handle afterwards. Each of those is
/// here, in the same order, because each of them was measured there.
/// </para>
/// <para>
/// Opened through the shell rather than through <see cref="Processes.ProcessRegister"/>, on purpose.
/// <c>explorer.exe</c> started on a path hands the folder to the shell's own process and exits, so a
/// register holding it would record a launch that left before its case began, and the window that
/// matters belongs to a process this run did not start and must not stop. What is held is the window,
/// by its handle; what is given back is a close sent to it; and a window still standing after the
/// stop budget is named, as a process that would not stop is.
/// </para>
/// </summary>
internal sealed class FolderWindow
{
    /// <summary>The class every Explorer folder window has, and the one thing that tells one from any other window.</summary>
    public const string Cabinet = "CabinetWClass";

    private FolderWindow(string folder, nint handle, int pid, bool ours)
    {
        Folder = folder;
        Handle = handle;
        Pid = pid;
        Ours = ours;
    }

    /// <summary>The folder it shows.</summary>
    public string Folder { get; }

    /// <summary>The window, as a handle.</summary>
    public nint Handle { get; }

    /// <summary>The shell process that draws it.</summary>
    public int Pid { get; }

    /// <summary>
    /// Whether this run opened the window, rather than finding the folder already showing in one.
    /// <para>
    /// Windows 11 may open a folder as a tab of an Explorer window already standing. That window was
    /// somebody's before the case began, so a close sent to it would take their other tabs with it —
    /// it is used and left alone.
    /// </para>
    /// </summary>
    public bool Ours { get; }

    /// <summary>
    /// Open <paramref name="folder"/> in Explorer, creating it if it is not there, and wait for the
    /// window. Null where no window showing it arrived inside the deadline.
    /// </summary>
    /// <param name="folder">The folder, already resolved.</param>
    /// <param name="deadlineMs">How long the window is waited for.</param>
    /// <param name="pollMs">How often to look.</param>
    public static FolderWindow? Open(string folder, int deadlineMs, int pollMs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        System.IO.Directory.CreateDirectory(folder);
        var before = Cabinets().Select(one => one.Handle).ToHashSet();

        // The shell's own verb on the folder, which is what a person double-clicking it gets.
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(folder) { UseShellExecute = true })?.Dispose();

        // Found by its title holding the folder's name, as ExplorerDropRun found it: Windows 11 adds
        // to the title, and may answer with a tab in a window that was already there.
        var name = new System.IO.DirectoryInfo(folder).Name;
        (nint Handle, string Title) found = default;
        var arrived = Attempt.UntilTrue(
            () =>
            {
                var showing = Cabinets()
                    .Where(one => one.Title.Contains(name, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // A window this run opened wins over one that happened to be showing the same name.
                found = showing.FirstOrDefault(one => !before.Contains(one.Handle));
                if (found.Handle == 0)
                    found = showing.FirstOrDefault();

                return found.Handle != 0;
            },
            deadlineMs,
            pollMs);

        if (!arrived.Happened)
            return null;

        Win32.GetWindowThreadProcessId(found.Handle, out var pid);
        return new FolderWindow(folder, found.Handle, (int)pid, !before.Contains(found.Handle));
    }

    /// <summary>
    /// Put the window under test on the left half of the primary display's work area and this one on
    /// the right, so a drop released over this one is not released over the window it started in.
    /// <para>
    /// ExplorerDropRun did the same before every drag it made, and for the reason it gives: Explorer
    /// opens wherever it opened last, which is often over the window the drag starts in, and a release
    /// over that window is a drop onto the application rather than onto the folder.
    /// </para>
    /// </summary>
    /// <param name="underTest">The window under test.</param>
    public void Beside(System.Windows.Automation.AutomationElement underTest)
    {
        ArgumentNullException.ThrowIfNull(underTest);

        if (Win32.SystemParametersInfoRect(Win32.SpiGetWorkArea, 0, out var area, 0))
        {
            var half = (area.Right - area.Left) / 2;
            var height = area.Bottom - area.Top;

            Placed(underTest.Current.NativeWindowHandle, area.Left, area.Top, half, height);
            Placed(Handle, area.Left + half, area.Top, half, height);
        }

        // The desk handed back to the window under test. Measured in the guest: the shell activates
        // the folder's window after it is drawn, whichever was launched first, and Windows will not
        // let the application take the desk back on its own — so a drag's wait for the window under
        // test ran out at ten seconds with Explorer holding it. Asked through the window's own
        // automation peer, as a focus move is everywhere else in the engine; refused, it leaves the
        // desk to the act's own wait, which says who held it.
        try
        {
            underTest.SetFocus();
        }
        catch (Exception refused) when (refused is InvalidOperationException
            or System.Windows.Automation.ElementNotAvailableException
            or System.Runtime.InteropServices.COMException)
        {
            // The act after this names the foreground it found, which is the reading worth having.
        }
    }

    /// <summary>
    /// Close it, where this run opened it, and say whether it went.
    /// </summary>
    /// <param name="deadlineMs">How long it is given to go.</param>
    /// <param name="pollMs">How often to look.</param>
    /// <returns>True where it is gone or was never this run's to close.</returns>
    public bool Close(int deadlineMs, int pollMs)
    {
        if (!Ours || !Win32.IsWindow(Handle))
            return true;

        // By its handle, as ExplorerDropRun closed it: WindowPattern.Close answered and left the
        // window standing on the guest's desk, where the next run found it.
        Win32.PostMessageW(Handle, Win32.WmClose, 0, 0);
        return Attempt.UntilTrue(() => !Win32.IsWindow(Handle), deadlineMs, pollMs).Happened;
    }

    /// <summary>The line a survivor list shows for this window, where it would not close.</summary>
    public override string ToString() => $"the Explorer window on {Folder}";

    private static void Placed(nint window, int x, int y, int width, int height)
    {
        if (window == 0)
            return;

        if (Win32.IsZoomed(window))
            Win32.ShowWindow(window, Win32.ShowRestore);

        Win32.SetWindowPos(window, 0, x, y, width, height, Win32.PlaceOnly);
    }

    /// <summary>Every visible Explorer folder window on the desk, with its title.</summary>
    private static List<(nint Handle, string Title)> Cabinets()
    {
        var found = new List<(nint, string)>();
        Win32.EnumWindows(
            (window, _) =>
            {
                if (Win32.IsWindowVisible(window) && Win32.ClassOf(window) == Cabinet)
                    found.Add((window, Win32.TextOf(window)));

                return true;
            },
            0);

        return found;
    }
}

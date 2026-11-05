using System.Diagnostics;
using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Windowing;

namespace Winwright.Scenarios;

/// <summary>
/// The application's process as a case follows it across an exit and a relaunch. WW575.
/// <para>
/// Found by Modelado (MOD31): an update downloads the newer installer, starts it and exits, and the
/// installer starts the application again. The case's window goes with the first process, so a step
/// after the relaunch has to resolve under the new one, and the run has to stop the new one when the
/// case ends the way it stops the launch.
/// </para>
/// <para>
/// Read off the case's window rather than handed in by the run: the window is what the case was given,
/// and its process is the one whose exit the case means. The executable is read while that process is
/// still there, because a process that has exited can no longer say what it was running.
/// </para>
/// </summary>
internal sealed class Following
{
    private readonly HashSet<int> seen = [];
    private readonly List<int> relaunched = [];

    private Following(int pid, string? executable)
    {
        Pid = pid;
        Executable = executable;
        seen.Add(pid);
        foreach (var other in Others())
            seen.Add(other);
    }

    /// <summary>The process the case is about now, or zero where its window belongs to none it can follow.</summary>
    public int Pid { get; private set; }

    /// <summary>The executable that process runs, as a full path, or null where Windows would not say.</summary>
    public string? Executable { get; }

    /// <summary>Every process a <see cref="StepDeclaration.Relaunches"/> step followed, which the run then stops.</summary>
    public IReadOnlyList<int> Relaunched => relaunched;

    /// <summary>Whether a step claimed the process the case was launched with exited. WW575.</summary>
    public bool ClaimedExit { get; private set; }

    /// <summary>Follow the process the case's window belongs to.</summary>
    /// <param name="root">The case's window.</param>
    public static Following Of(AutomationElement root)
    {
        ArgumentNullException.ThrowIfNull(root);

        // The desktop is the root a resident fixture is given, and it belongs to no process a case
        // could mean.
        if (root == AutomationElement.RootElement)
            return new Following(0, null);

        int pid;
        try
        {
            pid = root.Current.ProcessId;
        }
        catch (ElementNotAvailableException)
        {
            return new Following(0, null);
        }

        return new Following(pid, PathOf(pid));
    }

    /// <summary>
    /// Wait for the process to exit, true where it did within <paramref name="deadlineMs"/>.
    /// </summary>
    public (bool Exited, int WaitedMs) Exit(int deadlineMs, int pollMs)
    {
        var watch = Stopwatch.StartNew();
        var gone = Attempt.UntilTrue(() => !Alive(Pid), deadlineMs, pollMs).Happened;
        if (gone && relaunched.Count == 0)
            ClaimedExit = true;

        return (gone, (int)watch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Wait for a new process of the same executable that draws a window, and follow it: its window
    /// is what the steps after it resolve under. Null where none arrived within the deadline.
    /// </summary>
    public (AutomationElement? Window, int Pid, int WaitedMs) Relaunch(int deadlineMs, int pollMs)
    {
        var watch = Stopwatch.StartNew();
        var found = 0;
        nint window = 0;
        Attempt.UntilTrue(
            () =>
            {
                foreach (var candidate in Others().Where(one => !seen.Contains(one)))
                {
                    if (TopLevelWindows.Largest(candidate) is { } drawn)
                    {
                        found = candidate;
                        window = drawn.Handle;
                        return true;
                    }
                }

                return false;
            },
            deadlineMs,
            pollMs);

        if (found == 0)
            return (null, 0, (int)watch.ElapsedMilliseconds);

        seen.Add(found);
        relaunched.Add(found);
        Pid = found;
        return (AutomationElement.FromHandle(window), found, (int)watch.ElapsedMilliseconds);
    }

    /// <summary>The executable, as a sentence names it.</summary>
    public string Named => Executable is null ? $"pid {Pid}" : System.IO.Path.GetFileName(Executable);

    /// <summary>Every other running process of the same executable, by pid.</summary>
    private IEnumerable<int> Others()
    {
        if (Executable is null)
            return [];

        var found = new List<int>();
        foreach (var one in Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(Executable)))
        {
            using (one)
            {
                if (one.Id != Pid && string.Equals(PathOf(one.Id), Executable, StringComparison.OrdinalIgnoreCase))
                    found.Add(one.Id);
            }
        }

        return found;
    }

    private static bool Alive(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (Exception gone) when (gone is ArgumentException or InvalidOperationException)
        {
            return false;
        }
    }

    private static string? PathOf(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return process.MainModule?.FileName;
        }
        catch (Exception unreadable) when (unreadable is ArgumentException or InvalidOperationException
            or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }
}

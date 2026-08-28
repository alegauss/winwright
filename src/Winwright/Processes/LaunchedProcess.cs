using System.Diagnostics;

namespace Winwright.Processes;

/// <summary>
/// A process this run started. There is no public constructor, and that is the whole mechanism:
/// the only way to hold one is to have gone through <see cref="ProcessRegister.Launch(string, string[])"/>,
/// so a case cannot start something the register does not know about — not even down a path
/// nobody thought about.
/// </summary>
public sealed class LaunchedProcess
{
    private readonly Process process;

    internal LaunchedProcess(Process process, string executable)
    {
        this.process = process;
        Executable = executable;
    }

    /// <summary>The process id, which is what a file-lock message names.</summary>
    public int Pid => process.Id;

    /// <summary>What was launched, as it was launched.</summary>
    public string Executable { get; }

    /// <summary>Whether it has already gone on its own.</summary>
    public bool HasExited => process.HasExited;

    /// <summary>
    /// That it had already gone when the register came to stop it, or null where it had not.
    /// <para>
    /// WW279. Written by the register and by nothing else, for the same reason nothing outside the
    /// register may start or end a process: the stop is the one moment something looks, and a second
    /// place that could set this is a second answer about whether the launch survived its case.
    /// </para>
    /// </summary>
    public Departure? Departed { get; private set; }

    /// <summary>Its exit code. Reading this while it is still running is an error, as it is on <see cref="Process"/>.</summary>
    public int ExitCode => process.ExitCode;

    /// <summary>The top-level window it owns, or zero where it has none yet.</summary>
    public nint MainWindowHandle => process.MainWindowHandle;

    /// <summary>Wait up to <paramref name="milliseconds"/> for it to exit. True where it did.</summary>
    public bool WaitForExit(int milliseconds) => process.WaitForExit(milliseconds);

    /// <summary>Re-read what Windows says about it now, rather than what it said when it started.</summary>
    public void Refresh() => process.Refresh();

    internal Process Underlying => process;

    /// <summary>
    /// Record that the register found it already gone. Kept where it was first recorded: the first
    /// look is the one that happened at the case boundary, and a later stop asking again would move
    /// the fact to a moment nobody was asserting about.
    /// </summary>
    internal void Left()
    {
        int? code;
        try
        {
            code = process.ExitCode;
        }
        catch (Exception unreadable) when (unreadable is InvalidOperationException or NotSupportedException)
        {
            // A code nobody could read is said as one rather than reported as a zero — the exit is
            // the finding, and inventing a clean one would make a crash read as a shutdown.
            code = null;
        }

        Departed ??= new Departure(Pid, Executable, code);
    }
}

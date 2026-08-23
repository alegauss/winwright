using System.Collections.ObjectModel;
using System.Diagnostics;

using Winwright.Projects;
using Winwright.Verdicts;

namespace Winwright.Processes;

/// <summary>
/// The one door every launch goes through, and therefore the one list there is. Measured in
/// claude-tray: two trays a failing case had started were still alive afterwards, the next build
/// died on a file lock naming their process ids, and the command after that ran the previous
/// executable and reported on code that was not in the tree.
/// <para>
/// The register is total by construction rather than by discipline — a <see cref="LaunchedProcess"/>
/// cannot be made any other way — so a case returning early down a path nobody thought about still
/// has its process stopped. And whatever was still alive is <em>named</em>, because a leftover
/// cleaned up in silence is one the next reader diagnoses from a file lock instead.
/// </para>
/// </summary>
public sealed class ProcessRegister : IDisposable
{
    private readonly List<LaunchedProcess> launched = [];
    private readonly int stopTimeoutMs;
    private IReadOnlyList<Survivor>? survivors;

    /// <param name="stopTimeoutMs">How long a process is given to go before it is named as refusing to.</param>
    public ProcessRegister(int stopTimeoutMs = 5000)
    {
        this.stopTimeoutMs = stopTimeoutMs > 0
            ? stopTimeoutMs
            : throw new ArgumentOutOfRangeException(
                nameof(stopTimeoutMs), stopTimeoutMs, "a process given no time to stop was never given a chance");
    }

    /// <summary>Take the stop timeout from what the project declared, which is where a number belongs.</summary>
    public static ProcessRegister For(ProjectDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        return new ProcessRegister(declaration.Timeouts.For("stop"));
    }

    /// <summary>Everything this run started, in the order it started them.</summary>
    public IReadOnlyList<LaunchedProcess> Launched => new ReadOnlyCollection<LaunchedProcess>(launched);

    /// <summary>
    /// What was still alive when the run ended, once <see cref="StopAll"/> has run. Empty until
    /// then, and empty afterwards where nothing outlived its case — so read
    /// <see cref="Stopped"/> before believing an empty list.
    /// </summary>
    public IReadOnlyList<Survivor> Survivors => survivors ?? [];

    /// <summary>
    /// Whether the roll has been taken at all.
    /// <para>
    /// WW152. <see cref="Survivors"/> answers an empty list twice over: once for a register nobody
    /// has stopped yet, and once for a run that stopped everything and found nothing left. Those
    /// are a reading not taken and a reading that came back clean, and this project does not report
    /// them the same way — a caller that could not tell them apart would print <em>nothing
    /// outlived the run</em> about a run whose processes are all still going.
    /// </para>
    /// </summary>
    public bool Stopped => survivors is not null;

    /// <summary>
    /// What outlived this run, as a finding for the run's own reading.
    /// <para>
    /// A finding and not a precondition: a leftover process excuses no assertion. A finding and not
    /// a failure either: it is a fact about this desk and never a defect in the code under test,
    /// and colouring the build red for it would be the opposite of naming it. What it is is the
    /// thing a reader wants in front of them when the next run behaves oddly — the process that
    /// would not stop is what locks the next build, and a reader never told about it diagnoses the
    /// file lock, and then the wrong executable, from scratch.
    /// </para>
    /// <para>
    /// Answered by the register rather than by a caller holding a list, because the register is the
    /// one thing that cannot be wrong about whether the roll was taken.
    /// </para>
    /// </summary>
    public Finding AsFinding() => Stopped
        ? new Finding(ProcessSummary.Named, Survivors.Count == 0, ProcessSummary.Sentence(Survivors))
        : new Finding(ProcessSummary.Named, null, "this register was never asked to stop what it launched");

    /// <summary>Start something, registered in the same statement that starts it.</summary>
    public LaunchedProcess Launch(string executable, params string[] arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);

        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = false };
        foreach (var argument in arguments ?? [])
            start.ArgumentList.Add(argument);

        return Launch(start);
    }

    /// <summary>The same, where the caller needs to say more about how it starts.</summary>
    public LaunchedProcess Launch(ProcessStartInfo start)
    {
        ArgumentNullException.ThrowIfNull(start);
        ObjectDisposedException.ThrowIf(survivors is not null, this);

        var started = Process.Start(start)
            ?? throw new InvalidOperationException($"{start.FileName} started nothing this run can hold");

        var registered = new LaunchedProcess(started, start.FileName);
        launched.Add(registered);
        return registered;
    }

    /// <summary>
    /// Stop everything still running and say what was. A window is asked to close first, because
    /// an application under test is entitled to shut down the way a person would close it; what
    /// does not take the hint is killed with its tree, since the child it spawned holds the same
    /// file lock the parent did. Idempotent: the second call answers what the first found.
    /// </summary>
    public IReadOnlyList<Survivor> StopAll()
    {
        if (survivors is not null)
            return survivors;

        var outlived = new List<Survivor>();
        foreach (var registered in launched)
        {
            registered.Refresh();
            if (registered.HasExited)
                continue;

            var pid = registered.Pid;
            outlived.Add(new Survivor(pid, registered.Executable, Stop(registered)));
        }

        survivors = new ReadOnlyCollection<Survivor>(outlived);
        return survivors;
    }

    /// <summary>Stops whatever is left, which is what makes an early return harmless.</summary>
    public void Dispose()
    {
        StopAll();
        foreach (var registered in launched)
            registered.Underlying.Dispose();
    }

    private SurvivorFate Stop(LaunchedProcess registered)
    {
        var half = Math.Max(1, stopTimeoutMs / 2);

        try
        {
            if (registered.MainWindowHandle != 0 && registered.Underlying.CloseMainWindow() && registered.WaitForExit(half))
                return SurvivorFate.Stopped;

            registered.Underlying.Kill(entireProcessTree: true);
            return registered.WaitForExit(stopTimeoutMs - half) ? SurvivorFate.Stopped : SurvivorFate.WouldNotStop;
        }
        catch (InvalidOperationException)
        {
            // It went between the check and the kill, which is the outcome that was wanted.
            return SurvivorFate.Stopped;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Access denied, or it is already dying and Windows will not say which.
            registered.Refresh();
            return registered.HasExited ? SurvivorFate.Stopped : SurvivorFate.WouldNotStop;
        }
    }
}

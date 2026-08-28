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

    /// <summary>
    /// What a per-case <see cref="Stop(LaunchedProcess)"/> found still running after it asked.
    /// <para>
    /// WW215. A case that gives its process back is not a leftover, so a stop that worked adds
    /// nothing here — the whole vocabulary of <see cref="Survivor"/> is about outliving a case, and
    /// nine cases reporting nine survivors they each cleanly ended would say the opposite of what
    /// happened. One that would not stop <em>has</em> outlived its case: the case ended and the
    /// process did not, which is the leftover that locks the next build. Kept, because
    /// <see cref="StopAll"/> would otherwise be the only reading and it runs after the run.
    /// </para>
    /// </summary>
    private readonly List<Survivor> outlived = [];

    /// <summary>
    /// Every launch a stop found already gone, in the order the stops happened.
    /// <para>
    /// WW286. WW279 put the same fact on the case's own line and that reaches only a case that owned
    /// its process — a lent fixture is held across several and stopped at none of their boundaries, so
    /// its exit was recorded on the launch and read by nothing at all. Kept here because the register
    /// is the one thing that saw every look, and because the run is the only level that can honestly
    /// report an exit no single case can claim.
    /// </para>
    /// </summary>
    private readonly List<Departure> departed = [];

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
    /// Every launch a stop found already gone, in the order the stops happened. WW286, and read
    /// through <see cref="EarlyExits" /> rather than counted here: a list has no way to say the roll
    /// was never taken, and an empty one means both <em>nothing left early</em> and <em>nobody asked
    /// yet</em>.
    /// </summary>
    public IReadOnlyList<Departure> Departures => new ReadOnlyCollection<Departure>(departed);

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
    /// Stop one process this register started, which is how a case gives back a window it owns.
    /// <para>
    /// WW215. The register stopped everything or nothing, so a suite of nine unshared cases held
    /// nine windows, nine processes and nine first paints on the desk until the last case was done —
    /// and every window after the first is another top-level window a locator could match, another
    /// candidate for the largest window a process owns, and another thing the foreground reading has
    /// to be right about. The other-instance refusal exists because a second window of the same
    /// application is a real hazard; holding eight of them on purpose was making them.
    /// </para>
    /// <para>
    /// Here rather than on the caller for the reason nothing outside the register may start a
    /// process: nothing outside it should be able to end one either. Same budget as
    /// <see cref="StopAll"/>, because it is the one the project declared for stopping a process, and
    /// a second number would be a second rule about the same thing.
    /// </para>
    /// </summary>
    /// <param name="registered">What to stop. Must be something this register launched.</param>
    /// <returns>
    /// Null where the process is gone — whether this call stopped it or it had already exited, since
    /// neither outlived its case. A <see cref="Survivor"/> where it is still running after being
    /// asked, which the register then carries into <see cref="Survivors"/> and its own finding.
    /// </returns>
    /// <exception cref="ArgumentException">Where this register did not launch it.</exception>
    /// <exception cref="ObjectDisposedException">Where the roll has already been taken.</exception>
    public Survivor? Stop(LaunchedProcess registered)
    {
        ArgumentNullException.ThrowIfNull(registered);
        ObjectDisposedException.ThrowIf(survivors is not null, this);

        if (!launched.Contains(registered))
        {
            throw new ArgumentException(
                $"pid {registered.Pid} was not launched by this register, so this is not the list it is on",
                nameof(registered));
        }

        registered.Refresh();
        if (registered.HasExited)
        {
            // WW279. Still null — nothing outlived its case, which is what this answers — and the
            // other half is now written down rather than dropped. A process that had gone before the
            // stop asked is the fact a case reporting nothing but missing locators is about, and this
            // is the one moment anything looks: at the launch it is a race, and with a wait it is the
            // window deadline WW257 removed.
            // WW286. Collected as well as recorded on the launch, so the run can read what no one
            // case can claim. At a case boundary, which is what makes this one attributable.
            departed.Add(registered.Left(DepartureSeen.WhereItsCaseEnded));
            return null;
        }

        // The pid is read before the stop: afterwards the process object may no longer answer for
        // one, and a leftover reported without the number a file lock names is a leftover a reader
        // cannot act on.
        var pid = registered.Pid;
        if (Ending(registered) == SurvivorFate.Stopped)
            return null;

        var refused = new Survivor(pid, registered.Executable, SurvivorFate.WouldNotStop);
        outlived.Add(refused);
        return refused;
    }

    /// <summary>
    /// Stop everything still running and say what was. A window is asked to close first, because
    /// an application under test is entitled to shut down the way a person would close it; what
    /// does not take the hint is killed with its tree, since the child it spawned holds the same
    /// file lock the parent did. Idempotent: the second call answers what the first found.
    /// <para>
    /// The reading starts from what a per-case <see cref="Stop(LaunchedProcess)"/> already found
    /// refusing to go. Rebuilding it from what is still alive would drop those: the case that could
    /// not give its window back is exactly the one a reader needs named, and by the time the run
    /// ends the process may have gone on its own and left no trace of having outlived anything.
    /// </para>
    /// </summary>
    public IReadOnlyList<Survivor> StopAll()
    {
        if (survivors is not null)
            return survivors;

        var left = new List<Survivor>(outlived);
        foreach (var registered in launched)
        {
            registered.Refresh();
            if (registered.HasExited)
            {
                // WW279, and the same look. This one is at the end of the run rather than at a case
                // boundary, so it is the only reading a lent launch gets — a fixture held for several
                // cases is not stopped where any one of them finishes, and none of them can say which
                // one it went during.
                // WW286. The list rather than only the launch, and `WhereTheRunEnded` because that is
                // all this look can support: a lent fixture was held across several cases and none of
                // them can say which it went during.
                if (registered.Departed is null)
                    departed.Add(registered.Left(DepartureSeen.WhereTheRunEnded));

                continue;
            }

            var pid = registered.Pid;
            left.Add(new Survivor(pid, registered.Executable, Ending(registered)));
        }

        survivors = new ReadOnlyCollection<Survivor>(left);
        return survivors;
    }

    /// <summary>Stops whatever is left, which is what makes an early return harmless.</summary>
    public void Dispose()
    {
        StopAll();
        foreach (var registered in launched)
            registered.Underlying.Dispose();
    }

    /// <summary>Ask it to go, then make it. The one place the two-stage stop is written.</summary>
    private SurvivorFate Ending(LaunchedProcess registered)
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

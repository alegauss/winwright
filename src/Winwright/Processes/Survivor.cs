namespace Winwright.Processes;

/// <summary>What became of a process that was still alive when the run ended.</summary>
public enum SurvivorFate
{
    /// <summary>It was still running, and the register stopped it.</summary>
    Stopped,

    /// <summary>It was still running, and it did not stop inside the time allowed.</summary>
    WouldNotStop,
}

/// <summary>
/// One process that outlived the case that started it. It is recorded rather than cleaned up in
/// silence, because the leftover is the thing that locks the next build — and a reader who is
/// never told about it diagnoses the file lock, and then the wrong executable, from scratch.
/// </summary>
/// <param name="Pid">The process id, which is what a file-lock message names.</param>
/// <param name="Executable">What was launched, as it was launched.</param>
/// <param name="Fate">Whether the register managed to stop it.</param>
public sealed record Survivor(int Pid, string Executable, SurvivorFate Fate)
{
    /// <summary>The one line a summary shows.</summary>
    public override string ToString() =>
        Fate == SurvivorFate.Stopped
            ? $"pid {Pid} {Executable} - outlived its case and was stopped"
            : $"pid {Pid} {Executable} - outlived its case and would not stop";
}

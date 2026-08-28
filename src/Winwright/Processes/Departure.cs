namespace Winwright.Processes;

/// <summary>
/// One process that had already gone when the register came to stop it — the counterpart of
/// <see cref="Survivor" />, and the fact a case that observed nothing is actually about.
/// <para>
/// WW279. A launch that draws no window has nothing to wait for, so a tray that exits on startup
/// used to reach the case as a run against the desktop: every step red about a locator that matched
/// nothing, and a reader sent looking for a missing icon on a desk where the application had never
/// been drawn. The reds are half right — it did fail — and they name the wrong thing.
/// </para>
/// <para>
/// Read where the run stops the process rather than at the launch, which is the shape two earlier
/// attempts got wrong: asking once at the launch is a race, because a process that exits still takes
/// a moment to do it, and asking with a wait is the window deadline that WW257 removed from every
/// tray that behaves. The stop is a moment the register already looks, so this costs nothing — and it
/// also catches the case the startup check could not: a launch that ran, was driven, and died in the
/// middle.
/// </para>
/// </summary>
/// <param name="Pid">The process id, which is what a file-lock message names.</param>
/// <param name="Executable">What was launched, as it was launched.</param>
/// <param name="Code">
/// What it exited with, or null where Windows would not say. Null rather than a zero: an exit code
/// nobody could read and a clean exit are different facts, and the second one is the one that makes
/// a case ending in a deliberate shutdown unremarkable.
/// </param>
public sealed record Departure(int Pid, string Executable, int? Code)
{
    /// <summary>
    /// The fact and nothing about what it means, which is the caller's to say: a case that ended by
    /// closing the application under test left this behind on purpose, and a phrase claiming the run
    /// was cut short would be a false finding on exactly that case.
    /// </summary>
    public override string ToString() => Code is { } code
        ? $"exited with {code}"
        : "exited, and Windows would not say with what";
}

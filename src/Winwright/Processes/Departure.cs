namespace Winwright.Processes;

/// <summary>
/// Where the register was standing when it found a launch already gone. Two answers, because they
/// support different claims and collapsing them would let a run say more than it knows.
/// <para>
/// WW286. WW279 read the departure where a case gives its process back, which says which case the
/// application went during. A lent launch is held across several cases and is not stopped at any of
/// their boundaries, so the only look it gets is the end of the run — and a reading that reported
/// the two alike would put an exit inside a case that may not have been running when it happened.
/// </para>
/// </summary>
public enum DepartureSeen
{
    /// <summary>At the boundary of the case that owned it, so the case it went during is known.</summary>
    WhereItsCaseEnded,

    /// <summary>At the end of the run, which is the only look a lent launch gets.</summary>
    WhereTheRunEnded,
}

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
/// What it exited with.
/// <para>
/// WW287. This was <c>int?</c>, for a code Windows would not name, and the arm was removed on a
/// measurement: the only state that makes <see cref="System.Diagnostics.Process.ExitCode" /> throw is
/// a handle nobody holds, and that state makes <c>HasExited</c> throw the same exception one line
/// earlier — so nothing could reach a null, and the second sentence it produced was reader-facing
/// text no run could ever show.
/// </para>
/// </param>
/// <param name="Seen">
/// Where the register was standing when it found it gone. WW286: what bounds how much anything
/// reading this may claim about when the application went.
/// </param>
public sealed record Departure(int Pid, string Executable, int Code, DepartureSeen Seen)
{
    /// <summary>
    /// The fact and nothing about what it means, which is the caller's to say: a case that ended by
    /// closing the application under test left this behind on purpose, and a phrase claiming the run
    /// was cut short would be a false finding on exactly that case.
    /// </summary>
    public override string ToString() => $"exited with {Code}";
}

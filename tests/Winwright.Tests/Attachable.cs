using System.Diagnostics;

using Winwright.Processes;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// Waiting for a launched process to be readable.
/// <para>
/// A process reports no main module for the first instants of its life, so anything that reads
/// one milliseconds after starting it is testing process startup rather than what it meant to
/// test. That flake was diagnosed once in the attach tests and left copied nowhere, which is why
/// it came back in the binary-identity ones the next time the suite's timing moved. It lives here
/// now, once.
/// </para>
/// </summary>
public static class Attachable
{
    /// <summary>Launch through the register, then wait until Windows will say what is running.</summary>
    public static LaunchedProcess Launch(ProcessRegister register, ProcessStartInfo start)
    {
        ArgumentNullException.ThrowIfNull(register);

        var launched = register.Launch(start);
        Waits.Until("readable", $"pid {launched.Pid} never said what it was running", () => Readable(launched.Pid));
        return launched;
    }

    /// <summary>
    /// Stop everything the register started and wait until it is out of the machine, not merely
    /// off the desktop.
    /// <para>
    /// WW126 waited for the windows, which is the wrong moment and was measured to be: no window of
    /// a stopped process is enumerable well before the process has exited, and a stopped
    /// application still has a presentation stack to tear down, a compositor with frames to retire
    /// and a taskbar to settle. The class that follows is usually the one asserting who owns the
    /// desktop, and it was starting into all of that.
    /// </para>
    /// <para>
    /// What this cannot fix is a machine busy for reasons outside the suite. That limit is stated
    /// here rather than left to surprise somebody reading an unreproducible red.
    /// </para>
    /// </summary>
    public static void StopAndSettle(ProcessRegister register)
    {
        ArgumentNullException.ThrowIfNull(register);

        var pids = register.Launched.Select(one => one.Pid).ToList();
        register.StopAll();

        Waits.Until(
            "gone",
            $"{string.Join(", ", pids)} were still running after the register stopped them",
            () => pids.TrueForAll(Gone));
    }

    /// <summary>
    /// A register that settles on the way out, for a case that deletes what it ran.
    /// <para>
    /// WW201. A <c>using</c> on the register stops what it started, and stopped is not gone: Windows
    /// will not delete a running image, so a class that copied a binary into its own temp directory
    /// and started it threw <c>UnauthorizedAccessException</c> out of its own <c>Dispose</c>. That
    /// reads as a broken harness — ranked above a failure because nothing past it was observed — and
    /// sends the reader to this repository over a file handle. Measured on WW196's first guest run,
    /// and four classes here have the shape.
    /// </para>
    /// <para>
    /// A door rather than a habit, for the reason WW190 gives about the desk: applying the rule
    /// everywhere it is needed today does nothing about the class written tomorrow.
    /// </para>
    /// </summary>
    internal static Settling Settling() => new();

    /// <summary>
    /// Whether that process is out of the machine. A pid nothing can open is gone; one that opens
    /// and says it has exited is gone; anything else is still on its way out and still costing the
    /// desktop something.
    /// </summary>
    private static bool Gone(int pid)
    {
        try
        {
            using var running = Process.GetProcessById(pid);
            return running.HasExited;
        }
        catch (Exception away) when (away is ArgumentException or InvalidOperationException)
        {
            return true;
        }
    }

    private static bool Readable(int pid)
    {
        try
        {
            using var reading = Process.GetProcessById(pid);
            return !string.IsNullOrEmpty(reading.MainModule?.FileName);
        }
        catch (Exception waiting)
            when (waiting is InvalidOperationException or System.ComponentModel.Win32Exception or ArgumentException)
        {
            return false;
        }
    }
}

/// <summary>
/// A <see cref="ProcessRegister" /> that waits for what it started to leave the machine before it
/// lets go. WW201: the register is sealed, so this holds one rather than extending it.
/// </summary>
internal sealed class Settling : IDisposable
{
    /// <summary>The register itself, which is what a launch is handed.</summary>
    internal ProcessRegister Register { get; } = new();

    /// <summary>Stop everything, wait until it is gone, and only then let go.</summary>
    public void Dispose()
    {
        Attachable.StopAndSettle(Register);
        Register.Dispose();
    }
}

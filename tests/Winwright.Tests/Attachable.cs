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
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (Readable(launched.Pid))
                return launched;

            Thread.Sleep(20);
        }

        Assert.Fail($"pid {launched.Pid} never said what it was running");
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

        for (var attempt = 0; attempt < 400; attempt++)
        {
            if (pids.TrueForAll(Gone))
                return;

            Thread.Sleep(20);
        }

        Assert.Fail($"{string.Join(", ", pids)} were still running after the register stopped them");
    }

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

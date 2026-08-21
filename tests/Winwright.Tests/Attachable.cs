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

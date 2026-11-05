using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace Winwright.Processes;

/// <summary>
/// What a program the application under test started was started as: its file name and its
/// arguments, read as <c>blender.exe --python cube.glb</c>. WW586.
/// <para>
/// Read through NtQueryInformationProcess and never WMI, which is a package the engine may not take.
/// The file name is the image Windows loaded rather than the first word of the command line, which a
/// program is free to write anything into; the arguments are what follows that first word.
/// </para>
/// </summary>
internal static class StartedProgram
{
    private const uint QueryLimitedInformation = 0x1000;
    private const int ProcessCommandLineInformation = 60;
    private const uint LengthMismatch = 0xC0000004;

    /// <summary>The console host Windows gives a console program, which no application asked for.</summary>
    private const string ConsoleHost = "conhost.exe";

    /// <summary>
    /// What each program <paramref name="launched"/> started and is still running reads as, by pid,
    /// the console host left out. A program that will not say is left out too: it is one this look
    /// could not read, and the wait around it looks again.
    /// </summary>
    /// <param name="launched">The launch whose job is asked.</param>
    public static IReadOnlyDictionary<int, string> Of(LaunchedProcess launched)
    {
        ArgumentNullException.ThrowIfNull(launched);

        var read = new Dictionary<int, string>();
        foreach (var pid in launched.Started)
        {
            if (Reading(pid) is { } reading && !reading.StartsWith(ConsoleHost, StringComparison.OrdinalIgnoreCase))
                read[pid] = reading;
        }

        return read;
    }

    /// <summary>The program under <paramref name="pid"/> as its file name and arguments, or null where it will not say.</summary>
    internal static string? Reading(int pid)
    {
        using var process = OpenProcess(QueryLimitedInformation, false, (uint)pid);
        if (process.IsInvalid)
            return null;

        var image = new char[1024];
        var length = image.Length;
        if (!QueryFullProcessImageName(process, 0, image, ref length))
            return null;

        var file = System.IO.Path.GetFileName(new string(image, 0, length));
        var line = CommandLine(process);
        return line is null ? null : $"{file} {Arguments(line)}".TrimEnd();
    }

    /// <summary>What follows the program in a command line: past the closing quote where it is quoted, else the first space.</summary>
    internal static string Arguments(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var trimmed = line.TrimStart();
        int end;
        if (trimmed.StartsWith('"'))
        {
            var closing = trimmed.IndexOf('"', 1);
            end = closing < 0 ? trimmed.Length : closing + 1;
        }
        else
        {
            var space = trimmed.IndexOfAny([' ', '\t']);
            end = space < 0 ? trimmed.Length : space;
        }

        return trimmed[end..].Trim();
    }

    private static string? CommandLine(SafeProcessHandle process)
    {
        var size = 1024;
        for (var tries = 0; tries < 3; tries++)
        {
            var buffer = Marshal.AllocHGlobal(size);
            try
            {
                var status = NtQueryInformationProcess(process, ProcessCommandLineInformation, buffer, size, out var needed);
                if (status == LengthMismatch && needed > size)
                {
                    size = needed;
                    continue;
                }

                if (status != 0)
                    return null;

                // A UNICODE_STRING at the start, whose characters follow it in the same buffer.
                var bytes = (ushort)Marshal.ReadInt16(buffer);
                var characters = Marshal.ReadIntPtr(buffer, IntPtr.Size);
                return Marshal.PtrToStringUni(characters, bytes / 2);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        return null;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint pid);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "QueryFullProcessImageNameW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageName(SafeProcessHandle process, uint flags, [Out] char[] name, ref int size);

    [DllImport("ntdll.dll")]
    private static extern uint NtQueryInformationProcess(SafeProcessHandle process, int infoClass, nint info, int length, out int returned);
}

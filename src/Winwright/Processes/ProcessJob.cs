using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace Winwright.Processes;

/// <summary>
/// The Windows job one launch and everything it starts belong to. WW586.
/// <para>
/// Found by modelado (MOD46): "Open in Blender" starts Blender from the window under test, and the
/// register stopped only what it launched. An application that closes cleanly leaves its children
/// running, and the tree kill misses a grandchild whose parent had already gone, so Blender outlived
/// the case, held the desk for every case after it, and was never named among the survivors.
/// </para>
/// <para>
/// A job rather than descendants found by parent id, because a parent id is a number Windows reuses:
/// an orphan's parent has gone and its id may name something else, so the walk either loses the
/// orphan or takes a stranger. Membership is decided by Windows as a process is created, which is the
/// one moment nothing can be wrong about. The walk is kept for exactly one instant, the gap between
/// the start and the assignment, and only for processes younger than the launch.
/// </para>
/// </summary>
internal sealed class ProcessJob : IDisposable
{
    private const int JobObjectBasicProcessIdList = 3;
    private const int JobObjectExtendedLimitInformation = 9;
    private const uint KillOnJobClose = 0x2000;
    private const uint ProcessTerminate = 0x0001;
    private const uint ProcessSetQuota = 0x0100;
    private const uint SnapProcess = 0x00000002;

    /// <summary>How many members one reading lists. A launch with more than this is not one a case drives.</summary>
    private const int Listed = 256;

    private readonly SafeFileHandle handle;

    private ProcessJob(SafeFileHandle handle) => this.handle = handle;

    /// <summary>
    /// A job holding <paramref name="process"/> and whatever it started before it was assigned, or
    /// null where Windows would not make or assign one — which leaves the launch held as it was before
    /// WW586, by itself and its tree, rather than refusing it.
    /// </summary>
    /// <param name="process">A process this run just started.</param>
    public static ProcessJob? Holding(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);

        var created = CreateJobObject(0, null);
        if (created.IsInvalid)
        {
            created.Dispose();
            return null;
        }

        // Closing the last handle ends every member, so a runner that dies mid-run takes its launches
        // with it instead of leaving them for the next run's desk.
        var limits = new ExtendedLimits { Basic = new BasicLimits { LimitFlags = KillOnJobClose } };
        var job = new ProcessJob(created);
        if (!SetInformationJobObject(created, JobObjectExtendedLimitInformation, ref limits, Marshal.SizeOf<ExtendedLimits>())
            || !job.Take(process.Id))
        {
            job.Dispose();
            return null;
        }

        // A child started in the gap between Process.Start and the assignment is outside the job,
        // so it is taken by hand: a descendant of the launch, and younger than it, which a reused
        // pid naming an older process can never be.
        DateTime since;
        try
        {
            since = process.StartTime;
        }
        catch (Exception gone) when (gone is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return job;
        }

        foreach (var pid in Descendants(process.Id, since))
            job.Take(pid);

        return job;
    }

    /// <summary>The processes in the job that are still running, by pid.</summary>
    public IReadOnlyList<int> Members()
    {
        var size = 8 + (Listed * IntPtr.Size);
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (!QueryInformationJobObject(handle, JobObjectBasicProcessIdList, buffer, size, out _))
                return [];

            var count = Marshal.ReadInt32(buffer, 4);
            var pids = new List<int>(count);
            for (var at = 0; at < count; at++)
                pids.Add((int)Marshal.ReadIntPtr(buffer, 8 + (at * IntPtr.Size)));

            return pids;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>
    /// End every member and answer the ones still running after <paramref name="timeoutMs"/>, which
    /// is the leftover a reader needs named.
    /// </summary>
    /// <param name="timeoutMs">How long they are given to go.</param>
    /// <param name="pollMs">How often to look.</param>
    public IReadOnlyList<int> End(int timeoutMs, int pollMs = 50)
    {
        var members = Members();
        if (members.Count == 0)
            return [];

        // The register's own two-stage stop: a member with a window is asked to close it first, the
        // way a person would, so a copy the application started of itself writes its store and lets
        // go of its files before anything is ended under it. Measured with the relaunch fixture: a
        // copy ended outright still held its directory when the case went to delete it.
        var half = Math.Max(1, timeoutMs / 2);
        // Every member is asked, so this is a list and not Any: Any would stop at the first window.
        var asked = members.Where(Closing).ToList();
        if (asked.Count > 0)
            Locating.Attempt.UntilTrue(() => Members().Count == 0, half, pollMs);

        TerminateJobObject(handle, 1);
        Locating.Attempt.UntilTrue(() => Members().Count == 0, Math.Max(1, timeoutMs - half), pollMs);
        return Members();
    }

    /// <summary>Ask one member's window to close. False where it has none, or has gone.</summary>
    private static bool Closing(int pid)
    {
        using var process = Opened(pid);
        return process is not null && process.MainWindowHandle != 0 && process.CloseMainWindow();
    }

    /// <summary>The process under <paramref name="pid"/>, or null where it has gone: nothing to close.</summary>
    private static Process? Opened(int pid)
    {
        try
        {
            return Process.GetProcessById(pid);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>Let go of the job. Its members end with it, which is what a job closed early is for.</summary>
    public void Dispose() => handle.Dispose();

    /// <summary>Put one process into the job. False where it has gone or Windows refused.</summary>
    private bool Take(int pid)
    {
        var process = OpenProcess(ProcessSetQuota | ProcessTerminate, false, (uint)pid);
        if (process.IsInvalid)
        {
            process.Dispose();
            return false;
        }

        using (process)
            return AssignProcessToJobObject(handle, process);
    }

    /// <summary>The processes under <paramref name="root"/> that started at or after <paramref name="since"/>.</summary>
    private static List<int> Descendants(int root, DateTime since)
    {
        var parents = new Dictionary<int, int>();
        using (var snapshot = CreateToolhelp32Snapshot(SnapProcess, 0))
        {
            if (snapshot.IsInvalid)
                return [];

            var entry = new ProcessEntry { Size = (uint)Marshal.SizeOf<ProcessEntry>() };
            for (var more = Process32FirstW(snapshot, ref entry); more; more = Process32NextW(snapshot, ref entry))
                parents[(int)entry.ProcessId] = (int)entry.ParentProcessId;
        }

        var found = new List<int>();
        var frontier = new Queue<int>([root]);
        while (frontier.Count > 0)
        {
            var parent = frontier.Dequeue();
            foreach (var (pid, of) in parents)
            {
                if (of != parent || pid == root || found.Contains(pid) || !Younger(pid, since))
                    continue;

                found.Add(pid);
                frontier.Enqueue(pid);
            }
        }

        return found;
    }

    private static bool Younger(int pid, DateTime since)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return process.StartTime >= since;
        }
        catch (Exception unreadable) when (unreadable is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BasicLimits
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public nuint MinimumWorkingSetSize;
        public nuint MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public nuint Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ExtendedLimits
    {
        public BasicLimits Basic;
        public IoCounters Io;
        public nuint ProcessMemoryLimit;
        public nuint JobMemoryLimit;
        public nuint PeakProcessMemoryUsed;
        public nuint PeakJobMemoryUsed;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry
    {
        public uint Size;
        public uint Usage;
        public uint ProcessId;
        public nint DefaultHeapId;
        public uint ModuleId;
        public uint Threads;
        public uint ParentProcessId;
        public int PriorityClassBase;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string ExeFile;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CreateJobObjectW")]
    private static extern SafeFileHandle CreateJobObject(nint attributes, string? name);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetInformationJobObject(SafeFileHandle job, int infoClass, ref ExtendedLimits info, int length);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryInformationJobObject(SafeFileHandle job, int infoClass, nint info, int length, out int returned);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AssignProcessToJobObject(SafeFileHandle job, SafeProcessHandle process);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TerminateJobObject(SafeFileHandle job, uint exitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint pid);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeFileHandle CreateToolhelp32Snapshot(uint flags, uint pid);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32FirstW(SafeFileHandle snapshot, ref ProcessEntry entry);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32NextW(SafeFileHandle snapshot, ref ProcessEntry entry);
}

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32;

namespace Winwright.Processes;

/// <summary>
/// Starting an application the way the shell does for a file: through the file's association, by a
/// ProgID or by a packaged application's user model ID the fixture names. WW619.
/// <para>
/// Found by Modelado (MOD75, MOD78): its claim was that a model opened through its file association
/// lands in its window, and a fixture could only start the executable with the path as an argument,
/// which is the command line the association runs and not the activation itself. This goes through
/// <c>ShellExecuteEx</c> with the class named, so what runs is what the registry says that class runs
/// for this file.
/// </para>
/// <para>
/// The class and never the file's default handler, because the default is the desk's choice: a
/// fixture activating "whatever opens .glb here" is a case whose application depends on the machine.
/// </para>
/// <para>
/// A packaged application is reached the way Explorer reaches it, through the <c>AppX…</c> ProgID its
/// package registers for the type, and not through <c>IApplicationActivationManager.ActivateForFile</c>:
/// measured in the guest, that refuses a full-trust desktop package with 0x80270254, the contract not
/// supported, which is every package Modelado ships. The activation goes through the package's broker,
/// so the process is found by its user model ID rather than by a handle the shell may not return.
/// </para>
/// </summary>
public static class Activation
{
    /// <summary>
    /// Why <paramref name="through"/> cannot activate <paramref name="file"/>, or null where it can:
    /// the extension has no association, the ProgID is not registered or has no verb to open with, it
    /// does not claim the extension, or the package a user model ID names does not declare it.
    /// </summary>
    /// <param name="file">The file, as resolved.</param>
    /// <param name="through">The ProgID or user model ID the fixture names.</param>
    public static string? Unclaimed(string file, string through)
    {
        var extension = Path.GetExtension(file);
        if (extension.Length == 0)
            return $"'{file}' has no extension, so no association can claim it";

        using var type = Registry.ClassesRoot.OpenSubKey(extension);
        if (type is null)
            return $"'{extension}' has no association on this desk";

        if (Packaged(through))
        {
            return PackageProgId(extension, through) is null
                ? $"'{through}' does not declare '{extension}': no ProgID under the type's OpenWithProgids names that "
                    + "application, so its package is not registered here or declares no association for the type"
                : null;
        }

        using var registered = Registry.ClassesRoot.OpenSubKey(through);
        if (registered is null)
            return $"'{through}' is not a registered ProgID on this desk";

        using var command = registered.OpenSubKey(@"shell\open\command");
        using var shell = registered.OpenSubKey("shell");
        if (command is null && (shell is null || shell.SubKeyCount == 0))
            return $"'{through}' is registered with no verb to open a file with";

        var claimed = string.Equals(type.GetValue(null) as string, through, StringComparison.OrdinalIgnoreCase);
        if (!claimed)
        {
            using var others = type.OpenSubKey("OpenWithProgids");
            claimed = others?.GetValueNames().Contains(through, StringComparer.OrdinalIgnoreCase) ?? false;
        }

        return claimed
            ? null
            : $"'{through}' does not claim '{extension}': it is neither the type's default nor under its OpenWithProgids";
    }

    /// <summary>
    /// Whether <paramref name="through"/> is a packaged application's user model ID rather than a
    /// ProgID: a package family name, a bang, and the application's id. A ProgID never carries one.
    /// </summary>
    /// <param name="through">What the fixture names.</param>
    public static bool Packaged(string through) => through.Contains('!', StringComparison.Ordinal);

    /// <summary>
    /// Activate <paramref name="file"/> through <paramref name="through"/>, and answer the process the
    /// activation started, or null where it started none — the activation was handed to an instance
    /// already running, or to something that answers without a process of its own.
    /// </summary>
    /// <param name="file">The file, as resolved.</param>
    /// <param name="through">The ProgID or user model ID to activate it through.</param>
    /// <param name="directory">The directory the application starts in.</param>
    /// <param name="waitMs">
    /// How long a packaged activation's process is looked for, since its broker starts it and may
    /// hand back no handle. Unused for a ProgID, whose process the shell answers at once or not at all.
    /// </param>
    /// <exception cref="Win32Exception">Where the shell refused the activation.</exception>
    public static int? Activate(string file, string through, string directory, int waitMs)
    {
        if (!Packaged(through))
            return Executed(file, through, directory);

        var progId = PackageProgId(Path.GetExtension(file), through)
            ?? throw new Win32Exception($"'{through}' declares no ProgID for '{Path.GetExtension(file)}'");

        // Taken before the activation, so a copy already running is told from the one this started:
        // an activation that lands in it starts nothing, and that is the answer, not the old copy.
        var before = Running(through);
        var started = Executed(file, progId, directory);
        if (started is { } pid && !before.Contains(pid) && Aumid(pid) == through)
            return pid;

        var found = 0;
        var came = Locating.Attempt.UntilTrue(
            () => (found = Running(through).Except(before).FirstOrDefault()) > 0,
            Math.Max(1, waitMs),
            50);

        return came.Happened ? found : null;
    }

    /// <summary>
    /// The ProgID a package registered for <paramref name="extension"/> naming <paramref name="aumid"/>,
    /// or null where none does. A package's file type association is a ProgID of its own, listed under
    /// the type's OpenWithProgids and naming the application under its Application key.
    /// </summary>
    private static string? PackageProgId(string extension, string aumid)
    {
        using var others = Registry.ClassesRoot.OpenSubKey($@"{extension}\OpenWithProgids");
        foreach (var progId in others?.GetValueNames() ?? [])
        {
            using var application = Registry.ClassesRoot.OpenSubKey($@"{progId}\Application");
            if (string.Equals(application?.GetValue("AppUserModelID") as string, aumid, StringComparison.OrdinalIgnoreCase))
                return progId;
        }

        return null;
    }

    /// <summary>ShellExecuteEx with the class named, answering the process it handed back, if any.</summary>
    private static int? Executed(string file, string progId, string directory)
    {
        var info = new ShellExecuteInfo
        {
            Size = Marshal.SizeOf<ShellExecuteInfo>(),
            Mask = ClassName | NoCloseProcess | NoAsync | FlagNoUi,
            File = file,
            Class = progId,
            Directory = directory,
            Show = ShowNormal,
        };

        if (!ShellExecuteEx(ref info))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        if (info.Process == 0)
            return null;

        try
        {
            var pid = GetProcessId(info.Process);
            return pid == 0 ? null : (int)pid;
        }
        finally
        {
            CloseHandle(info.Process);
        }
    }

    /// <summary>Every running process whose user model ID is <paramref name="aumid"/>, by pid.</summary>
    private static HashSet<int> Running(string aumid)
    {
        var found = new HashSet<int>();
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                if (string.Equals(Aumid(process.Id), aumid, StringComparison.OrdinalIgnoreCase))
                    found.Add(process.Id);
            }
        }

        return found;
    }

    /// <summary>The user model ID a process runs under, or null where it has none or will not say.</summary>
    private static string? Aumid(int pid)
    {
        var handle = OpenProcess(QueryLimitedInformation, false, (uint)pid);
        if (handle == 0)
            return null;

        try
        {
            var length = 0u;
            if (GetApplicationUserModelId(handle, ref length, null) != InsufficientBuffer || length == 0)
                return null;

            var buffer = new char[length];
            return GetApplicationUserModelId(handle, ref length, buffer) == 0
                ? new string(buffer, 0, (int)Math.Max(0, length - 1))
                : null;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    private const uint ClassName = 0x00000001;
    private const uint NoCloseProcess = 0x00000040;
    private const uint NoAsync = 0x00000100;
    private const uint FlagNoUi = 0x00000400;
    private const int ShowNormal = 1;
    private const uint QueryLimitedInformation = 0x1000;
    private const int InsufficientBuffer = 122;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShellExecuteInfo
    {
        public int Size;
        public uint Mask;
        public nint Window;
        [MarshalAs(UnmanagedType.LPWStr)] public string? Verb;
        [MarshalAs(UnmanagedType.LPWStr)] public string File;
        [MarshalAs(UnmanagedType.LPWStr)] public string? Parameters;
        [MarshalAs(UnmanagedType.LPWStr)] public string? Directory;
        public int Show;
        public nint InstApp;
        public nint IdList;
        [MarshalAs(UnmanagedType.LPWStr)] public string? Class;
        public nint ClassKey;
        public uint HotKey;
        public nint IconOrMonitor;
        public nint Process;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "ShellExecuteExW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShellExecuteEx(ref ShellExecuteInfo info);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetProcessId(nint process);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint pid);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetApplicationUserModelId(nint process, ref uint length, [Out] char[]? id);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint handle);
}

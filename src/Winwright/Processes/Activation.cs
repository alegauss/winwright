using System.ComponentModel;
using System.Runtime.InteropServices;

using Microsoft.Win32;

namespace Winwright.Processes;

/// <summary>
/// Starting an application the way the shell does for a file: through the file's association, by a
/// ProgID the fixture names. WW619.
/// <para>
/// Found by Modelado (MOD75, MOD78): its claim was that a model opened through its file association
/// lands in its window, and a fixture could only start the executable with the path as an argument,
/// which is the command line the association runs and not the activation itself. This goes through
/// <c>ShellExecuteEx</c> with the class named, so what runs is what the registry says that ProgID
/// runs for this file.
/// </para>
/// <para>
/// The ProgID and never the file's default handler, because the default is the desk's choice: a
/// fixture activating "whatever opens .glb here" is a case whose application depends on the machine.
/// </para>
/// </summary>
public static class Activation
{
    /// <summary>
    /// Why <paramref name="progId"/> cannot activate <paramref name="file"/>, or null where it can:
    /// the extension has no association, the ProgID is not registered or has no verb to open with, or
    /// it does not claim the extension.
    /// </summary>
    /// <param name="file">The file, as resolved.</param>
    /// <param name="progId">The class the fixture names.</param>
    public static string? Unclaimed(string file, string progId)
    {
        var extension = Path.GetExtension(file);
        if (extension.Length == 0)
            return $"'{file}' has no extension, so no association can claim it";

        using var type = Registry.ClassesRoot.OpenSubKey(extension);
        if (type is null)
            return $"'{extension}' has no association on this desk";

        using var registered = Registry.ClassesRoot.OpenSubKey(progId);
        if (registered is null)
            return $"'{progId}' is not a registered ProgID on this desk";

        using var command = registered.OpenSubKey(@"shell\open\command");
        using var shell = registered.OpenSubKey("shell");
        if (command is null && (shell is null || shell.SubKeyCount == 0))
            return $"'{progId}' is registered with no verb to open a file with";

        var claimed = string.Equals(type.GetValue(null) as string, progId, StringComparison.OrdinalIgnoreCase);
        if (!claimed)
        {
            using var others = type.OpenSubKey("OpenWithProgids");
            claimed = others?.GetValueNames().Contains(progId, StringComparer.OrdinalIgnoreCase) ?? false;
        }

        return claimed
            ? null
            : $"'{progId}' does not claim '{extension}': it is neither the type's default nor under its OpenWithProgids";
    }

    /// <summary>
    /// Activate <paramref name="file"/> through <paramref name="progId"/>, and answer the process the
    /// shell started, or null where it started none — the activation was handed to an instance
    /// already running, or to something that answers without a process of its own.
    /// </summary>
    /// <param name="file">The file, as resolved.</param>
    /// <param name="progId">The class to activate it through.</param>
    /// <param name="directory">The directory the application starts in.</param>
    /// <exception cref="Win32Exception">Where the shell refused the activation.</exception>
    public static int? Activate(string file, string progId, string directory)
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

    private const uint ClassName = 0x00000001;
    private const uint NoCloseProcess = 0x00000040;
    private const uint NoAsync = 0x00000100;
    private const uint FlagNoUi = 0x00000400;
    private const int ShowNormal = 1;

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
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint handle);
}

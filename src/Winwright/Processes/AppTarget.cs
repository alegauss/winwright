using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

using Winwright.Verdicts;
using Winwright.Windowing;

namespace Winwright.Processes;

/// <summary>
/// How this run reached the application under test. There are exactly two shapes and no third
/// that decides between them: attaching is never implied when a running instance is found,
/// because implying it moves the check onto a binary nobody named.
/// <para>
/// The two are different claims, and the type says which. What a launch knows — the arguments it
/// passed — an attach cannot know, so <see cref="LaunchArguments"/> comes back absent there and
/// every assertion that depended on one is a hole by construction, rather than being compared
/// against a value this process never received.
/// </para>
/// </summary>
public abstract record AppTarget
{
    /// <summary>The name every scenario refers to the launch arguments by.</summary>
    public const string LaunchArgumentsPreconditionName = "the arguments this run passed at launch";

    private protected AppTarget(int pid, BinaryIdentity binary)
    {
        Pid = pid;
        Binary = binary;
    }

    /// <summary>The process this run is driving.</summary>
    public int Pid { get; }

    /// <summary>Which binary it reached, both keys and the path.</summary>
    public BinaryIdentity Binary { get; }

    /// <summary>Whether this run started it, which is the whole difference between the two claims.</summary>
    public abstract bool WasLaunched { get; }

    /// <summary>
    /// Whether the arguments the application was started with are knowable at all. Met on a
    /// launch, absent on an attach with the pid named.
    /// </summary>
    public abstract Precondition LaunchArguments { get; }

    /// <summary>How this run reached it and what it reached, said either way.</summary>
    public abstract string Sentence();

    /// <summary>This run started it, and therefore knows what it passed.</summary>
    public static AppTarget FromLaunch(LaunchedProcess process, params string[] arguments)
    {
        ArgumentNullException.ThrowIfNull(process);
        return new LaunchedTarget(
            process.Pid,
            BinaryIdentity.Of(process.Executable),
            new ReadOnlyCollection<string>([.. arguments ?? []]));
    }

    /// <summary>Somebody else started it, and this run found it by process id.</summary>
    /// <exception cref="AttachFailedException">Where nothing is running as that pid, or it will not say what it is.</exception>
    public static AppTarget AttachTo(int pid) => new AttachedTarget(pid, Reached($"pid {pid}", pid), null);

    /// <summary>Somebody else started it, and this run found it by one of its windows.</summary>
    /// <exception cref="AttachFailedException">Where the window names no process this run can read.</exception>
    public static AppTarget AttachToWindow(nint window)
    {
        if (window == 0)
            throw new AttachFailedException("window 0", "a window handle of zero addresses nothing");

        Win32.GetWindowThreadProcessId(window, out var pid);
        if (pid == 0)
            throw new AttachFailedException($"window 0x{window:X}", "the window names no process");

        return new AttachedTarget((int)pid, Reached($"window 0x{window:X}", (int)pid), window);
    }

    private static BinaryIdentity Reached(string named, int pid)
    {
        Process process;
        try
        {
            process = Process.GetProcessById(pid);
        }
        catch (ArgumentException)
        {
            throw new AttachFailedException(named, $"no process is running as pid {pid}");
        }

        using (process)
        {
            try
            {
                var module = process.MainModule?.FileName;
                return string.IsNullOrWhiteSpace(module)
                    ? throw new AttachFailedException(named, "the process names no main module")
                    : BinaryIdentity.Of(module);
            }
            catch (Exception reading) when (reading is Win32Exception or InvalidOperationException)
            {
                throw new AttachFailedException(named, $"the process would not say what it is running: {reading.Message}");
            }
        }
    }
}

/// <summary>The application as this run started it, arguments and all.</summary>
public sealed record LaunchedTarget : AppTarget
{
    internal LaunchedTarget(int pid, BinaryIdentity binary, IReadOnlyList<string> arguments)
        : base(pid, binary) => Arguments = arguments;

    /// <summary>What this run passed. It exists on this shape and on no other.</summary>
    public IReadOnlyList<string> Arguments { get; }

    /// <inheritdoc/>
    public override bool WasLaunched => true;

    /// <inheritdoc/>
    public override Precondition LaunchArguments => Precondition.Met(LaunchArgumentsPreconditionName);

    /// <inheritdoc/>
    public override string Sentence()
    {
        var passed = Arguments.Count == 0 ? "no arguments" : string.Join(" ", Arguments);
        return $"launched {Binary} as pid {Pid} with {passed}.";
    }
}

/// <summary>
/// The application as this run found it. There is no <c>Arguments</c> here, and that absence is
/// the point: what this process never passed, it cannot report, and a list invented at this
/// altitude would be compared against as if somebody had.
/// </summary>
public sealed record AttachedTarget : AppTarget
{
    internal AttachedTarget(int pid, BinaryIdentity binary, nint? window)
        : base(pid, binary) => Window = window;

    /// <summary>The window it was reached through, or null where it was reached by pid.</summary>
    public nint? Window { get; }

    /// <inheritdoc/>
    public override bool WasLaunched => false;

    /// <inheritdoc/>
    public override Precondition LaunchArguments => Precondition.Absent(
        LaunchArgumentsPreconditionName, $"this run attached to pid {Pid} and did not start it");

    /// <inheritdoc/>
    public override string Sentence() => Window is { } window
        ? $"attached to window 0x{window:X} in pid {Pid}, running {Binary}."
        : $"attached to pid {Pid}, running {Binary}.";
}

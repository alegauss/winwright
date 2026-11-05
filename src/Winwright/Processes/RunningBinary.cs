using System.ComponentModel;
using System.Diagnostics;

using Winwright.Verdicts;

namespace Winwright.Processes;

/// <summary>How the instance that is up compares with the one the run named.</summary>
public enum AttachmentMatch
{
    /// <summary>Both keys agree: whatever the paths say, this is that build.</summary>
    Same,

    /// <summary>The file versions differ. Reported in preference, being the more useful sentence.</summary>
    DifferentVersion,

    /// <summary>The versions agree and the write times do not — a Debug against an installed Release.</summary>
    DifferentBuild,

    /// <summary>What is running could not be read at all, so nothing was compared.</summary>
    Unreadable,
}

/// <summary>
/// What attaching actually got. A harness once reported that every check passed against a tray
/// published the previous afternoon, before the submenu entry being verified existed in it — so
/// the running instance is identified rather than assumed, by version first and write time
/// second, and the difference is reported rather than assumed away.
/// </summary>
public sealed record RunningBinary
{
    /// <summary>The name every scenario refers to this condition by.</summary>
    public const string PreconditionName = "the running instance is the binary this run named";

    private RunningBinary(AttachmentMatch match, BinaryIdentity named, BinaryIdentity? running, string? unreadable)
    {
        Match = match;
        Named = named;
        Running = running;
        Unreadable = unreadable;
    }

    /// <summary>Which of the four this is.</summary>
    public AttachmentMatch Match { get; }

    /// <summary>The binary the run named — the one the scenario meant.</summary>
    public BinaryIdentity Named { get; }

    /// <summary>The binary that is actually up, or null where it could not be read.</summary>
    public BinaryIdentity? Running { get; }

    /// <summary>Why what is running could not be read, where that is what happened.</summary>
    public string? Unreadable { get; }

    /// <summary>Whether the instance that is up is the one the run named.</summary>
    public bool Attached => Match == AttachmentMatch.Same;

    /// <summary>
    /// Compare two identities. Version first: it catches the ordinary case, and a version
    /// difference is the sentence a reader can act on. Write time second, because it is the only
    /// key that separates a Debug build from an installed Release between two releases.
    /// </summary>
    public static RunningBinary Check(BinaryIdentity named, BinaryIdentity running)
    {
        ArgumentNullException.ThrowIfNull(named);
        ArgumentNullException.ThrowIfNull(running);

        var match = !string.Equals(named.FileVersion, running.FileVersion, StringComparison.Ordinal)
            ? AttachmentMatch.DifferentVersion
            : named.WrittenUtc != running.WrittenUtc
                ? AttachmentMatch.DifferentBuild
                : AttachmentMatch.Same;

        return new RunningBinary(match, named, running, null);
    }

    /// <summary>The same comparison against a process this run attached to or started.</summary>
    public static RunningBinary Check(string named, LaunchedProcess running)
    {
        ArgumentNullException.ThrowIfNull(running);
        return Check(named, running.Underlying);
    }

    /// <summary>The same comparison against a process id, which is how attaching addresses one.</summary>
    public static RunningBinary Check(string named, int pid)
    {
        try
        {
            using var found = Process.GetProcessById(pid);
            return Check(named, found);
        }
        catch (ArgumentException)
        {
            return new RunningBinary(
                AttachmentMatch.Unreadable, BinaryIdentity.Of(named), null, $"no process is running as pid {pid}");
        }
    }

    /// <summary>
    /// This reading as the precondition a scenario declares a requirement on. A run driving a
    /// different build is a hole and not a failure, for the reason a stale one is: everything
    /// that ran did pass, on a binary, and not on the one the caller came about.
    /// </summary>
    public Precondition AsPrecondition() => Match switch
    {
        AttachmentMatch.Same => Precondition.Met(PreconditionName),
        AttachmentMatch.DifferentVersion => Precondition.Absent(
            PreconditionName, $"{Named.Path} is {Named.Version} and what is running is {Running!.Version}"),
        AttachmentMatch.DifferentBuild => Precondition.Absent(
            PreconditionName,
            $"both are {Named.Version}, and {Named.Path} was built {Named.Written} "
            + $"while {Running!.Path} was built {Running.Written}"),
        _ => Precondition.Absent(PreconditionName, Unreadable ?? "what is running could not be read"),
    };

    /// <summary>Which binary this run actually drove, printed whatever the reading is.</summary>
    public string Sentence() => Match switch
    {
        AttachmentMatch.Same => $"attached to {Named}.",
        AttachmentMatch.Unreadable => $"named {Named}, and what is running could not be read: {Unreadable}.",
        _ => $"named {Named}, and attached to {Running}.",
    };

    private static RunningBinary Check(string named, Process running)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(named);
        var identity = BinaryIdentity.Of(named);

        string? module;
        try
        {
            module = running.MainModule?.FileName;
        }
        catch (Win32Exception denied)
        {
            return new RunningBinary(
                AttachmentMatch.Unreadable, identity, null, $"pid {running.Id} would not say what it is running: {denied.Message}");
        }
        catch (InvalidOperationException gone)
        {
            return new RunningBinary(AttachmentMatch.Unreadable, identity, null, $"the process is no longer there: {gone.Message}");
        }

        return string.IsNullOrWhiteSpace(module)
            ? new RunningBinary(AttachmentMatch.Unreadable, identity, null, "the process names no main module")
            : Check(identity, BinaryIdentity.Of(module));
    }
}

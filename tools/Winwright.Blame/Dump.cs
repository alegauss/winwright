using System.Collections.ObjectModel;

using Microsoft.Diagnostics.Runtime;

namespace Winwright.Blaming;

/// <summary>
/// A hang dump, as threads and frames. WW406.
/// <para>
/// The whole of what touches a dump, and it decides nothing: it opens the file, finds the runtime
/// in it and copies each managed stack out as the text the runtime spells it in.
/// <see cref="Waiting" /> is where every judgement is, over data a case can write down — which is
/// the only way a reading of a dump nobody can reproduce gets checked at all.
/// </para>
/// <para>
/// Managed stacks and not native ones. What this is asked about is a test host, and a native stack
/// needs symbols for a runtime built on somebody else's machine — where the frames that name a
/// case, and the P/Invoke it went into, are all in the managed one. The entry point of a native
/// call is in that stack too, as the marker frame the runtime leaves behind.
/// </para>
/// </summary>
public static class Dump
{
    /// <summary>How deep a stack is read. Past this a reader is looking at the test platform.</summary>
    public const int Deepest = 64;

    /// <summary>
    /// Read one, or throw saying what is wrong with it.
    /// </summary>
    /// <param name="at">The dump file.</param>
    /// <exception cref="InvalidOperationException">Where no runtime is in it.</exception>
    public static IReadOnlyList<Thread> Threads(string at)
    {
        using var target = DataTarget.LoadDump(at);
        if (target.ClrVersions.Length == 0)
        {
            throw new InvalidOperationException(
                $"'{at}' holds no .NET runtime, so there are no managed stacks in it to read");
        }

        using var runtime = target.ClrVersions[0].CreateRuntime();

        var read = new List<Thread>();
        foreach (var thread in runtime.Threads)
        {
            var frames = thread.EnumerateStackTrace()
                .Take(Deepest)
                .Select(one => one.ToString() ?? "")
                .Where(one => one.Length > 0)
                .ToList();

            // A thread with no managed stack is a runtime thread doing its own work, and there is
            // nothing about it a reader of this could act on.
            if (frames.Count > 0)
                read.Add(new Thread(thread.OSThreadId, thread.ManagedThreadId, frames));
        }

        return new ReadOnlyCollection<Thread>(read);
    }
}

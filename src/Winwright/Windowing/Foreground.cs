using System.Diagnostics;

using Winwright.Verdicts;

namespace Winwright.Windowing;

/// <summary>Who has the keyboard, relative to the window a step was about to type into.</summary>
public enum ForegroundState
{
    /// <summary>The window under test has it, so synthesized input goes where it was meant to.</summary>
    Ours,

    /// <summary>Another window of the same process has it — its own dialog, or its own toast.</summary>
    SameProcess,

    /// <summary>Something else entirely has it. Started from an editor, this is usually the editor.</summary>
    Elsewhere,

    /// <summary>Nothing has it: a locked desk, or a switch caught halfway.</summary>
    Nobody,
}

/// <summary>A window and who owns it, as one sighting.</summary>
/// <param name="Window">The handle, or zero where there is no window.</param>
/// <param name="Pid">The process that owns it, or zero.</param>
/// <param name="Process">That process's name, empty where it would not say.</param>
/// <param name="Title">The window's caption, empty where it has none.</param>
/// <param name="Root">
/// The top-level window this one belongs to. Zero where it was not read, and then the handles
/// themselves are compared instead.
/// </param>
public readonly record struct WindowOwner(nint Window, int Pid, string Process, string Title, nint Root = 0)
{
    /// <summary>Nothing at all, which is what a locked desk answers.</summary>
    public static WindowOwner None { get; } = new(0, 0, "", "");

    /// <summary>The one phrase a refusal or a summary names it by.</summary>
    public override string ToString()
    {
        if (Window == 0)
            return "nothing";

        var named = string.IsNullOrEmpty(Title) ? "(untitled)" : $"'{Title}'";
        var process = string.IsNullOrEmpty(Process) ? $"pid {Pid}" : $"{Process} (pid {Pid})";
        return $"{process} {named}";
    }
}

/// <summary>
/// Whether the window a step is about to type into is the one that will receive it.
/// <para>
/// Windows refuses the foreground to a process that does not already own it, so a run started
/// from an editor drives somebody else's window. Measured while verifying a task in claude-tray:
/// the same case failed at three different points on three runs, and passed unchanged either side
/// of stashing the code under test.
/// </para>
/// <para>
/// It is asked once and answered as a hole with the intruder named, and there is deliberately no
/// wait, no poll and no retry on this type — a case that passes on the second attempt cannot tell
/// a busy desktop from a broken build, which is the whole reason those three runs disagreed.
/// </para>
/// </summary>
public sealed record Foreground
{
    /// <summary>The name every scenario refers to this condition by.</summary>
    public const string PreconditionName = "the foreground belongs to the window under test";

    private Foreground(ForegroundState state, WindowOwner holder, WindowOwner wanted)
    {
        State = state;
        Holder = holder;
        Wanted = wanted;
    }

    /// <summary>Which of the four this is.</summary>
    public ForegroundState State { get; }

    /// <summary>What actually has the keyboard.</summary>
    public WindowOwner Holder { get; }

    /// <summary>The window the step was about to type into.</summary>
    public WindowOwner Wanted { get; }

    /// <summary>Whether synthesized input would land where it was meant to.</summary>
    public bool Ours => State == ForegroundState.Ours;

    /// <summary>Who has the keyboard right now, read straight from Windows.</summary>
    public static WindowOwner Now() => Describe(Win32.GetForegroundWindow());

    /// <summary>
    /// Ask, once, whether <paramref name="window"/> has the foreground. There is no overload that
    /// waits: see the type's own note on why retrying is the defect rather than the fix.
    /// </summary>
    public static Foreground Check(nint window) => Between(Now(), Describe(window));

    /// <summary>The same judgement over two sightings, which is the rule stated in one place.</summary>
    public static Foreground Between(WindowOwner holder, WindowOwner wanted)
    {
        // Compared at the top level, and this is a repair rather than a nicety. Focusing a
        // control through automation makes that control the foreground window as far as Windows
        // is concerned, so comparing raw handles said the desktop belonged to somebody else while
        // the keys were landing exactly where they were meant to. Measured: a text box inside the
        // window under test read as "another window of the same process".
        var sameRoot = holder.Root != 0 && wanted.Root != 0 && holder.Root == wanted.Root;

        var state = holder.Window == 0 ? ForegroundState.Nobody
            : holder.Window == wanted.Window || sameRoot ? ForegroundState.Ours
            : holder.Pid != 0 && holder.Pid == wanted.Pid ? ForegroundState.SameProcess
            : ForegroundState.Elsewhere;

        return new Foreground(state, holder, wanted);
    }

    /// <summary>
    /// This reading as the precondition a step declares a requirement on. A hole rather than a
    /// failure: the keystroke was never delivered to the application under test, so nothing about
    /// that application was observed, and calling it a failure blames the wrong repository.
    /// </summary>
    public Precondition AsPrecondition() => State switch
    {
        ForegroundState.Ours => Precondition.Met(PreconditionName),
        ForegroundState.Nobody => Precondition.Absent(PreconditionName, "nothing owns the foreground"),
        ForegroundState.SameProcess => Precondition.Absent(
            PreconditionName, $"another window of the same process owns it: {Holder}"),
        _ => Precondition.Absent(PreconditionName, $"the foreground belongs to {Holder}"),
    };

    /// <summary>Who had the keyboard when this was asked, said either way.</summary>
    public string Sentence() => Ours
        ? $"the foreground belongs to the window under test, {Wanted}."
        : $"the foreground belongs to {Holder}, and the window under test is {Wanted}.";

    private static WindowOwner Describe(nint window)
    {
        if (window == 0)
            return WindowOwner.None;

        Win32.GetWindowThreadProcessId(window, out var pid);
        return new WindowOwner(
            window, (int)pid, NameOf((int)pid), Win32.TextOf(window), Win32.GetAncestor(window, Win32.GaRoot));
    }

    private static string NameOf(int pid)
    {
        if (pid == 0)
            return "";

        try
        {
            using var process = Process.GetProcessById(pid);
            return process.ProcessName;
        }
        catch (Exception reading) when (reading is ArgumentException or InvalidOperationException)
        {
            return "";
        }
    }
}

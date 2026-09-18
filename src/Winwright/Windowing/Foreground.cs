using System.Diagnostics;

using Winwright.Locating;
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
/// How long an act waits for the desk to reach the window under test, and how often it looks
/// again while it does. WW470.
/// <para>
/// A type rather than two more integers, because every call that takes one already carries a
/// settling deadline and a poll of its own — and four bare numbers in a row is a pair a caller
/// binds to the wrong parameters with nothing to say so.
/// </para>
/// <para>
/// Required wherever it is taken, and never defaulted, for the reason <see cref="Attempt" /> gives
/// about its own deadline: a wait nobody chose is one nobody can price. <see cref="Once" /> is how
/// a caller says it wants the single look, by name rather than by a zero.
/// </para>
/// </summary>
/// <param name="DeadlineMs">How long to wait for it. Zero is a single look.</param>
/// <param name="PollMs">How often to look again while waiting.</param>
public readonly record struct DeskWait(int DeadlineMs, int PollMs)
{
    /// <summary>One look and no wait, which is what a reading about this instant asks for.</summary>
    public static DeskWait Once => default;

    /// <summary>Whether this waits at all, or is <see cref="Once" />.</summary>
    public bool Waits => DeadlineMs > 0;

    /// <summary>A wait of <paramref name="deadlineMs"/>, looking again every <paramref name="pollMs"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Where either number is not positive. A deadline of nothing is <see cref="Once" />, reached
    /// by its name.
    /// </exception>
    public static DeskWait Of(int deadlineMs, int pollMs)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(deadlineMs);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pollMs);
        return new DeskWait(deadlineMs, pollMs);
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
/// It is answered as a hole with the intruder named, and it is never retried: a case that passes on
/// the second attempt cannot tell a busy desktop from a broken build, which is the whole reason
/// those three runs disagreed.
/// </para>
/// <para>
/// WW470. Waiting is not retrying, and this type held them to be one rule until a window that was
/// still coming forward was reported as a desk somebody else held. Measured in the guest: the
/// desktop had it, <c>ClaudeTray 'Settings'</c> took it at 1453ms, and the engine looked once at
/// 69ms — a true sentence about that instant and a false one about the run. So <see cref="Waited"
/// /> polls until the window has it, over the resolve budget, which is what a read waits for,
/// rather than the attempt cap, which is about a flaky act.
/// </para>
/// <para>
/// What the wait cannot do is turn a hole into a pass. The act still runs only where the desk
/// belongs to the window under test at the moment it runs, which is a stronger claim than one look
/// made rather than a weaker one — and a desk genuinely held still holes, later, and saying how
/// long it waited.
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

    /// <summary>
    /// How long this reading waited for the desk before answering. Zero where it looked once, which
    /// is every reading but <see cref="Waited" />'s.
    /// </summary>
    public int WaitedMs { get; private init; }

    /// <summary>Whether synthesized input would land where it was meant to.</summary>
    public bool Ours => State == ForegroundState.Ours;

    /// <summary>Who has the keyboard right now, read straight from Windows.</summary>
    public static WindowOwner Now() => Describe(Win32.GetForegroundWindow());

    /// <summary>
    /// Ask, once, whether <paramref name="window"/> has the foreground. A reading about this
    /// instant, which is what everything asking who holds the desk <em>now</em> wants.
    /// </summary>
    public static Foreground Check(nint window) => Between(Now(), Describe(window));

    /// <summary>
    /// The same question, waiting for the answer to become yes. WW470.
    /// <para>
    /// The reading handed back is the last one taken, so a wait that ran out answers about the
    /// moment it gave up rather than about the moment it started — and it carries how long that
    /// was, which is the difference between a desk somebody holds and a window still arriving.
    /// </para>
    /// </summary>
    /// <param name="window">The window an act is about to be sent at.</param>
    /// <param name="wait">How long to wait, or <see cref="DeskWait.Once" /> for a single look.</param>
    public static Foreground Waited(nint window, DeskWait wait)
    {
        if (!wait.Waits)
            return Check(window);

        // False while anything but the window under test has it, which is the whole of what this
        // waits out. The reading is kept from inside the look rather than taken again after it: one
        // more Check below would be a different instant from the one that ended the wait.
        Foreground? held = null;
        var waited = Attempt.UntilTrue(
            () =>
            {
                held = Check(window);
                return held.Ours;
            },
            wait.DeadlineMs,
            wait.PollMs);

        return held! with { WaitedMs = waited.WaitedMs };
    }

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
    /// <remarks>
    /// WW245: every absence names both sides. It used to name the holder alone, and a hole reading
    /// <em>the foreground belongs to ClaudeTray (pid 49276) 'Settings'</em> — where ClaudeTray is the
    /// application under test and 49276 is the process the run launched — fits two faults and
    /// distinguishes neither: a second window the run did not attach to, or the window under test
    /// described from a handle that answered no pid, which falls through to <see cref="Wanted"/>
    /// matching nothing while the holder named is the right one. Ruling those apart cost two runs that
    /// refuted two hypotheses and still did not answer. A refusal naming one side of a comparison is
    /// one a reader has to reconstruct.
    /// </remarks>
    public Precondition AsPrecondition() => State switch
    {
        ForegroundState.Ours => Precondition.Met(PreconditionName),
        ForegroundState.Nobody => Precondition.Absent(
            PreconditionName, $"nothing owns the foreground, and the window under test is {Wanted}{Stood}"),
        ForegroundState.SameProcess => Precondition.Absent(
            PreconditionName, $"another window of the same process owns it: {Holder}, and the window under test is {Wanted}{Stood}"),
        _ => Precondition.Absent(
            PreconditionName, $"the foreground belongs to {Holder}, and the window under test is {Wanted}{Stood}"),
    };

    /// <summary>Who had the keyboard when this was asked, said either way.</summary>
    public string Sentence() => Ours
        ? $"the foreground belongs to the window under test, {Wanted}{Took}."
        : $"the foreground belongs to {Holder}, and the window under test is {Wanted}{Stood}.";

    /// <summary>
    /// What a wait that ran out adds to an absence, and nothing at all where nothing waited. WW470:
    /// a desk held for the whole budget is a different fact from a desk read once, and a hole that
    /// did not say which is one a reader has to guess at.
    /// </summary>
    private string Stood => WaitedMs > 0 ? $", and that was still true {WaitedMs}ms later" : "";

    /// <summary>The other half of the same sentence: how long the window took to come forward.</summary>
    private string Took => WaitedMs > 0 ? $", which took it after {WaitedMs}ms" : "";

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

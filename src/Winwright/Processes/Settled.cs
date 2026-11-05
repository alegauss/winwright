using System.Diagnostics;

using Winwright.Locating;
using Winwright.Tracing;
using Winwright.Verdicts;

namespace Winwright.Processes;

/// <summary>
/// Whether what a run started has left the machine, as against merely having been stopped.
/// <para>
/// WW205. <see cref="ProcessRegister.StopAll" /> stops what a run launched and says what outlived
/// it, and stopping is not the same as being gone. WW126 measured the difference — a stopped
/// application is off the desktop well before its presentation stack, its compositor frames and its
/// taskbar entry are — and WW201 measured the other cost: Windows will not delete a running image,
/// so a run that clears up after itself throws where it deletes.
/// </para>
/// <para>
/// Both were answered in the suite and neither in the engine, which is what an adopter has. They
/// would write the loop, and they would write it against <c>Process.GetProcessById</c> and
/// get it wrong the way the first draft here did: a pid that will not open is <em>gone</em>, and
/// treating that throw as anything else reports a process that has left as one still running.
/// </para>
/// <para>
/// Three states and not two, which is why this is a reading rather than a bool. Everything left;
/// something is still here and is named; and nothing was waited for at all, which is what a caller
/// gets for asking with no deadline rather than an answer it might act on.
/// </para>
/// </summary>
/// <param name="Stopped">What the register found still running and stopped.</param>
/// <param name="Lingering">The pids still in the machine when the deadline ran out.</param>
/// <param name="WaitedMs">How long it waited, which is what a reader tuning a deadline needs.</param>
/// <param name="Absence">Why nothing was waited for, where nothing was. Empty where it was.</param>
public sealed record Settled(
    IReadOnlyList<Survivor> Stopped,
    IReadOnlyList<int> Lingering,
    long WaitedMs,
    string Absence)
{
    /// <summary>What this reading is called wherever it is reported.</summary>
    public const string Named = "everything this run started has left the machine";

    /// <summary>Whether the waiting happened at all.</summary>
    public bool Was => Absence.Length == 0;

    /// <summary>Whether everything is out of the machine, which is the claim above.</summary>
    public bool Gone => Was && Lingering.Count == 0;

    /// <summary>What was read, said in whichever of the three ways it went.</summary>
    public string Sentence()
    {
        if (!Was)
            return $"nothing was waited for: {Absence}.";

        if (Lingering.Count == 0)
        {
            return Stopped.Count == 0
                ? $"nothing this run started was still running after {WaitedMs}ms."
                : $"{Stopped.Count} process(es) this run started were stopped and had left the "
                    + $"machine after {WaitedMs}ms.";
        }

        return $"{Lingering.Count} process(es) this run started were still in the machine after "
            + $"{WaitedMs}ms: {string.Join(", ", Lingering.Select(one => $"pid {one}"))}.";
    }

    /// <inheritdoc cref="Sentence" />
    public override string ToString() => Sentence();

    /// <summary>The three-state reading, so a caller can carry it rather than branch on it.</summary>
    public Finding AsFinding() => new(Named, Was ? Gone : null, Sentence());

    /// <summary>
    /// The result a verdict counts. A process that will not leave is the machine's business and not
    /// a defect in the code under test, so it is a hole rather than a failure.
    /// </summary>
    /// <param name="named">What the assertion claims, as the caller spells it.</param>
    public AssertionResult AsAssertion(string named) => Gone
        ? AssertionResult.Pass(named, Sentence())
        : AssertionResult.Unchecked(named, Precondition.Absent(Named, Sentence()));

    /// <summary>
    /// The step a trace records. Asked for by the engine's own rule, which caught this the moment it
    /// was written: a result that answers a verdict answers the step behind it, or a reader handed
    /// the verdict has nowhere to see what it was made of.
    /// </summary>
    /// <param name="named">What the assertion claims, as the caller spells it.</param>
    public TraceStep AsTraceStep(string named) => new()
    {
        Verb = "stop what this run started and wait for it to leave",
        Locator = named,
        Resolved = $"{Stopped.Count} still running when the register was asked",
        Pattern = "close the window, then kill the tree, then look until the pid will not open",
        ReadBack = Was ? $"{Lingering.Count} still here after {WaitedMs}ms" : null,
        Verdict = Gone ? StepVerdict.Ok : StepVerdict.Unchecked,
        Detail = Gone ? null : Sentence(),
    };

    /// <summary>
    /// Stop everything the register started and wait until it is out of the machine.
    /// </summary>
    /// <param name="register">What this run launched.</param>
    /// <param name="deadlineMs">How long to wait once everything has been asked to stop.</param>
    /// <param name="pollMs">How often to look.</param>
    public static Settled Of(ProcessRegister register, int deadlineMs = 8000, int pollMs = 25)
    {
        ArgumentNullException.ThrowIfNull(register);

        var pids = register.Launched.Select(one => one.Pid).ToList();
        var stopped = register.StopAll();

        if (deadlineMs <= 0)
        {
            return new Settled(
                stopped, [], 0, $"a deadline of {deadlineMs}ms is not a wait, so nothing was looked at twice");
        }

        var waited = Attempt.UntilTrue(() => pids.TrueForAll(Left), deadlineMs, pollMs);

        return new Settled(stopped, pids.Where(one => !Left(one)).ToList(), waited.WaitedMs, "");
    }

    /// <summary>
    /// Whether that process is out of the machine. A pid nothing can open is gone; one that opens
    /// and says it has exited is gone; anything else is still on its way out and still costing the
    /// desktop something.
    /// </summary>
    private static bool Left(int pid)
    {
        try
        {
            using var running = Process.GetProcessById(pid);
            return running.HasExited;
        }
        catch (Exception away) when (away is ArgumentException or InvalidOperationException)
        {
            return true;
        }
    }
}

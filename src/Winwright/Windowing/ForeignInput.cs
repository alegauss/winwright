using Winwright.Verdicts;

namespace Winwright.Windowing;

/// <summary>
/// Whether anybody but this run has touched the machine since it started watching.
/// <para>
/// WW157. The desk's own six ask whether anything can be observed here. All six are met on a desk
/// with somebody working at it, and that desk loses the foreground halfway through — after which
/// the run either goes red for a reason about the code, or produces a hole attributed to the
/// foreground. Both name the symptom the run saw rather than the fact that a person moved the
/// mouse, and a reader acting on either opens the wrong file.
/// </para>
/// <para>
/// Not one of <see cref="Desk"/>'s conditions, deliberately. A desk with a person at it can still
/// be observed, so folding this in would make a touch of the mouse refuse the whole run under
/// WW156. It is a reading beside them: it excuses an assertion, it never stops a run.
/// </para>
/// </summary>
public sealed record ForeignInput
{
    /// <summary>The name an assertion refers to this by, so a hole can require it.</summary>
    public const string PreconditionName = "no input this run did not synthesise";

    // GetTickCount's timebase, which is what GetLastInputInfo answers in. Volatile because the
    // acting half and whoever reads the desk are not promised to be the same thread.
    private static volatile uint ours;
    private static volatile uint since;

    private ForeignInput(bool alone, uint last, uint mine, uint watched)
    {
        Alone = alone;
        LastInput = last;
        LastSynthesised = mine;
        Watched = watched;
    }

    /// <summary>Whether every input since this run began watching was one it made itself.</summary>
    public bool Alone { get; }

    /// <summary>When the machine last saw input of any origin, in the tick count's own units.</summary>
    public uint LastInput { get; }

    /// <summary>When this run last synthesised input. Zero where it has synthesised none.</summary>
    public uint LastSynthesised { get; }

    /// <summary>When this run started watching.</summary>
    public uint Watched { get; }

    /// <summary>How long ago the input this run did not make arrived, in milliseconds. Zero where alone.</summary>
    public uint Ago => Alone ? 0 : unchecked((uint)Environment.TickCount - LastInput);

    /// <summary>
    /// Start watching now. Called when a run begins; called again, it forgets what came before.
    /// </summary>
    public static void Watch()
    {
        since = unchecked((uint)Environment.TickCount);
        ours = 0;
    }

    /// <summary>
    /// Record that this run has just synthesised input. Called by <see cref="Win32.SendInput"/>
    /// and by nothing else.
    /// </summary>
    internal static void Sent() => ours = unchecked((uint)Environment.TickCount);

    /// <summary>
    /// Read it now.
    /// <para>
    /// The comparison is against whichever is later of the moment this run started watching and the
    /// moment it last synthesised input, because SendInput advances the machine's last-input time
    /// exactly as a person's hand does. That is the whole reason this cannot be one call: asked on
    /// its own, GetLastInputInfo says when input last happened and never whose it was.
    /// </para>
    /// <para>
    /// What it therefore cannot answer, said plainly: a person who touched the machine <em>before</em>
    /// this run's most recent act is invisible to it, because the act moved the mark past them. It
    /// catches the case worth catching — the operator reaching for a machine that is waiting, which
    /// is where a suite spends most of a run — and it is silent about a hand that got there first.
    /// </para>
    /// </summary>
    public static ForeignInput Read()
    {
        var info = new Win32.LastInput { Size = (uint)System.Runtime.InteropServices.Marshal.SizeOf<Win32.LastInput>() };
        if (!Win32.GetLastInputInfo(ref info))
        {
            // Unreadable is not the same as alone. Reporting a desk as this run's own because the
            // question could not be asked is the shape of green this project exists to withdraw.
            return new ForeignInput(false, 0, ours, since);
        }

        var mark = Later(since, ours);
        return new ForeignInput(!After(info.Ticks, mark), info.Ticks, ours, since);
    }

    /// <summary>The reading as a precondition an assertion may be excused by.</summary>
    public Precondition AsPrecondition() =>
        Alone ? Precondition.Met(PreconditionName) : Precondition.Absent(PreconditionName, Sentence());

    /// <summary>What was read, in the line a preamble prints.</summary>
    public string Sentence()
    {
        if (Alone)
        {
            return LastSynthesised == 0
                ? "no input at all since this run began watching"
                : "every input since this run began watching was one it made itself";
        }

        // Named as a person and never as the foreground. Naming the foreground is the
        // misattribution this whole reading exists to remove, so it is the one word not used.
        return $"somebody used this machine {Ago} ms ago, and it was not this run";
    }

    /// <summary>The reading in one line.</summary>
    public override string ToString() => Sentence();

    // Tick counts wrap every 49.7 days, so the comparisons are differences and never magnitudes.
    // A run straddling the wrap would otherwise read every input as ancient and report a desk of
    // its own on the one machine that had been up longest.
    private static bool After(uint tick, uint mark) => unchecked(tick - mark) is > 0 and < int.MaxValue;

    private static uint Later(uint left, uint right) => After(left, right) ? left : right;
}

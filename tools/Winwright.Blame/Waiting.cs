using System.Collections.ObjectModel;

namespace Winwright.Blaming;

/// <summary>
/// One thread of a stopped process, as a dump holds it: what the operating system calls it, and
/// what it was in, innermost first.
/// </summary>
/// <param name="Os">The thread id the operating system gave it, which is what a debugger names.</param>
/// <param name="Managed">The runtime's own number for it, or zero where it has none.</param>
/// <param name="Frames">Its stack, innermost first, as the runtime spells each frame.</param>
public sealed record Thread(uint Os, int Managed, IReadOnlyList<string> Frames)
{
    /// <summary>What it was in when everything stopped, or nothing where it had no stack.</summary>
    public string? Innermost => Frames.Count > 0 ? Frames[0] : null;
}

/// <summary>
/// What a hang dump says, in the sentences a reader can act on. WW406.
/// <para>
/// A run that loses the test host reports a pass over the cases that answered and nothing at all
/// about the ones that never ran — which is WW117 working, and is also the whole of what anybody
/// has ever known about it. Three runs died at the same point and each was diagnosed by staring at
/// which case answered last, because the evidence was a dump written under the guest's tree that
/// the next run's sync deleted.
/// </para>
/// <para>
/// The evidence is kept now. This is the reading of it, and it is here rather than beside the
/// dump-opening because it is the half that can be checked: the frames are text, so a stack this
/// tool has never seen can be written into a case and the sentence it produces asserted. What
/// touches a dump is a translation into <see cref="Thread"/> and nothing else.
/// </para>
/// </summary>
public static class Waiting
{
    /// <summary>
    /// The prefix this project's own frames carry. A stack from a suite run is nine tenths the test
    /// platform's own machinery, and the frames worth naming are the ones somebody here wrote.
    /// </summary>
    public const string Ours = "Winwright.";

    /// <summary>The prefix a case carries, which is the one frame that names what was running.</summary>
    public const string Cases = "Winwright.Tests.";

    /// <summary>
    /// Read the threads and say what happened, one line per thing worth knowing.
    /// <para>
    /// The order is the reader's: the thread that was in this project's own code first, because
    /// that is the one an edit can reach, then every other thread that was waiting. A dump with no
    /// frame of ours in it says so rather than saying nothing — a hang inside the test platform is a
    /// finding too, and it is not the finding somebody will assume.
    /// </para>
    /// </summary>
    /// <param name="threads">Every thread the dump held.</param>
    public static IReadOnlyList<string> Said(IEnumerable<Thread> threads)
    {
        ArgumentNullException.ThrowIfNull(threads);

        var all = threads.ToList();
        var said = new List<string>();

        if (all.Count == 0)
        {
            said.Add("the dump holds no threads, so it is not a dump of a running process");
            return new ReadOnlyCollection<string>(said);
        }

        var blamed = Blamed(all);
        said.Add(blamed is null
            ? $"no thread of the {all.Count} was in {Ours.TrimEnd('.')}'s own code, so what stopped "
                + "is the test platform rather than a case"
            : Sentence(blamed));

        // Every other wait, one line each and no stack. Two threads waiting on each other is the
        // shape this list exists to make visible, and it is unreadable as two stacks.
        foreach (var thread in all)
        {
            if (!ReferenceEquals(thread, blamed) && Waits(thread) is { } waits)
                said.Add($"  thread {thread.Os} waits in {waits}");
        }

        return new ReadOnlyCollection<string>(said);
    }

    /// <summary>
    /// The thread to name, or null where none of them is ours.
    /// <para>
    /// The one carrying a case, and a thread carrying our code under no case second: a suite run
    /// has one case running at a time, and a fixture's own thread is ours without being the thread
    /// somebody has to look at.
    /// </para>
    /// </summary>
    /// <param name="threads">Every thread the dump held.</param>
    public static Thread? Blamed(IEnumerable<Thread> threads)
    {
        ArgumentNullException.ThrowIfNull(threads);

        var all = threads.ToList();
        return all.Find(one => one.Frames.Any(frame => frame.StartsWith(Cases, StringComparison.Ordinal)))
            ?? all.Find(one => one.Frames.Any(frame => frame.StartsWith(Ours, StringComparison.Ordinal)));
    }

    /// <summary>
    /// What one thread was waiting in, or null where its innermost frame is not a wait.
    /// <para>
    /// Read off the frame's own words rather than off a list of methods. The runtime spells a
    /// blocking call the same way everywhere — <c>Wait</c>, <c>Join</c>, <c>Poll</c>, <c>Read</c> —
    /// and a list of the ones seen so far is a list that stops covering the next one.
    /// </para>
    /// </summary>
    /// <param name="thread">The thread.</param>
    public static string? Waits(Thread thread)
    {
        ArgumentNullException.ThrowIfNull(thread);

        foreach (var frame in thread.Frames)
        {
            // A native call arrives as a marker frame naming the entry point in brackets, and the
            // marker on its own says nothing. The name in the brackets is the answer, so a frame
            // that carries one is read whether or not it reads as a wait.
            if (Native(frame) is { } called)
                return called;

            if (Blocking(frame))
                return Trimmed(frame);
        }

        return null;
    }

    /// <summary>
    /// The entry point a marker frame names, or null where it names none.
    /// </summary>
    /// <param name="frame">The frame, as the runtime spells it.</param>
    public static string? Native(string frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var open = frame.IndexOf('(');
        if (!frame.StartsWith('[') || open < 0)
            return null;

        var close = frame.LastIndexOf(')');
        if (close <= open + 1)
            return null;

        // The runtime writes the entry point with a leading dot for a bare import and with its
        // declaring type for a generated one. Neither is worth showing and the dot is not a name.
        var named = frame[(open + 1)..close].TrimStart('.');
        return named.Length == 0 ? null : named;
    }

    /// <summary>Whether a frame is a call that does not return until something else happens.</summary>
    /// <param name="frame">The frame, as the runtime spells it.</param>
    private static bool Blocking(string frame) =>
        Waited.Any(one => frame.Contains(one, StringComparison.Ordinal));

    /// <summary>
    /// The words a blocking call is spelled with. Not a list of methods: a method list goes stale
    /// against the runtime it describes, and these four are how that runtime names the act.
    /// </summary>
    private static readonly string[] Waited = ["Wait", "Join", "Poll", "Sleep"];

    /// <summary>The frame without the arguments, which are never what a reader is looking for.</summary>
    /// <param name="frame">The frame, as the runtime spells it.</param>
    private static string Trimmed(string frame) =>
        frame.IndexOf('(') is var open && open > 0 ? frame[..open] : frame;

    /// <summary>
    /// The sentence naming the thread that was ours: what it was in, what case it was under, and
    /// what it was waiting on.
    /// </summary>
    /// <param name="thread">The thread <see cref="Blamed"/> chose.</param>
    private static string Sentence(Thread thread)
    {
        var mine = thread.Frames.FirstOrDefault(one => one.StartsWith(Ours, StringComparison.Ordinal));
        var under = thread.Frames.LastOrDefault(one => one.StartsWith(Cases, StringComparison.Ordinal));
        var waits = Waits(thread);

        // Named the way a reader reaches it: the innermost of our own frames is the line to open,
        // the case is what to re-run, and the wait is why neither ever came back.
        var said = $"thread {thread.Os} was in {Trimmed(mine ?? thread.Innermost ?? "nothing")}";
        if (under is not null)
            said += $", under {Trimmed(under)}";

        return waits is null ? $"{said}, and it was not waiting" : $"{said}, waiting in {waits}";
    }
}

namespace Winwright.Blaming;

/// <summary>
/// A process with a thread parked in it, and a line saying when it is. WW406.
/// <para>
/// The reader needs something to read. A hang dump comes out of a run that lost its test host, and
/// that is a state nothing here can arrange — three times in about fifteen guest runs, each hours
/// apart. So the door that opens a dump was asserted by its shape, which is what WW420 is about: it
/// would go on compiling after the library it calls renamed the thing it reads.
/// </para>
/// <para>
/// This is that state made cheap. It is the same argument the fixture's <c>--pump=none</c> makes —
/// the thing under test reproduced rather than waited for — and the same one WW345 made about
/// <c>-DefineOnly</c>: a mode whose only caller is the suite is what lets a decision nobody can
/// otherwise run be checked at all.
/// </para>
/// <para>
/// Another process and never this one. <c>MiniDumpWriteDump</c> suspends every thread of what it
/// is imaging, so a suite that dumped itself would stall whatever else was running beside it —
/// measured here at 2.5 seconds against a case pacing frames twenty to the second, which is a red
/// about the dump wearing another case's name. Reading somebody else's process is also what this
/// tool does for a living.
/// </para>
/// </summary>
public static class Parked
{
    /// <summary>The flag that asks for it.</summary>
    public const string Flag = "--park";

    /// <summary>What it writes once the thread is really in its wait, and never before.</summary>
    public const string Ready = "parked";

    /// <summary>The thread's name, which is also the frame a reader of the dump must come back with.</summary>
    public const string Named = "TheThreadThatWaits";

    private static readonly ManualResetEventSlim Holding = new(false);

    /// <summary>
    /// Park a thread and say so, then wait to be killed.
    /// <para>
    /// The line goes out after the thread has reached its wait rather than after it was started:
    /// an image taken in between holds a stack in <c>Thread.StartCallback</c>, which is a true
    /// reading of a moment nobody is asking about, and it would be a flake rather than a failure.
    /// </para>
    /// </summary>
    /// <returns>Never, until the caller ends the process.</returns>
    public static int Park()
    {
        var thread = new System.Threading.Thread(TheThreadThatWaits)
        {
            IsBackground = true,
            Name = Named,
        };

        thread.Start();

        while (!thread.ThreadState.HasFlag(System.Threading.ThreadState.WaitSleepJoin))
            System.Threading.Thread.Yield();

        Console.Out.WriteLine(Ready);
        Console.Out.Flush();

        // Ends when the caller does, and never on a deadline: a process that walked out of a dump
        // being taken of it would be the one failure this mode must not have. Reading to the end of
        // standard input returns the moment the caller's handle goes, so a caller that died without
        // killing this leaves nothing behind.
        //
        // That is not a nicety. The first draft parked forever, a run lost its test host, and this
        // outlived it holding the handles it had inherited — so the `dotnet test` above it never saw
        // the end of its own output and waited for a process that was already gone. An orphan of a
        // tool for diagnosing hangs is a hang, and it cost the run it was supposed to explain.
        Console.In.ReadToEnd();
        return 0;
    }

    /// <summary>
    /// The method the frame names. Its own rather than a lambda, because a lambda is spelled as a
    /// display class and the name the case looks for would not be in the frame.
    /// </summary>
    private static void TheThreadThatWaits() => Holding.Wait();
}

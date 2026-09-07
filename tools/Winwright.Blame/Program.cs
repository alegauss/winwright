namespace Winwright.Blaming;

/// <summary>
/// Say what a hang dump says. WW406.
/// <para>
/// The test host has gone away mid-run three times in about fifteen guest runs, taking nine hundred
/// cases with it. WW117 catches that — the roll call refuses a run whose recorded count disagrees
/// with what was discovered — but catching it is all anybody has ever been able to do, because the
/// evidence was a dump written under the guest's tree and the next run's sync deletes the tree.
/// </para>
/// <para>
/// The dump is kept beside the trx now. This is what reads it, and the runner calls it on the one
/// run where there is one, so the answer arrives with the failure rather than waiting for somebody
/// to be at the keyboard for the twenty minutes the evidence exists.
/// </para>
/// </summary>
public static class Program
{
    /// <summary>What a dump that could not be read exits with.</summary>
    public const int Unreadable = 2;

    /// <summary>How to call it.</summary>
    public const string Usage = $"usage: Winwright.Blame <hangdump.dmp>  |  Winwright.Blame {Parked.Flag}";

    /// <summary>Read the dump the command line names and say what it says.</summary>
    /// <param name="args">The dump file, or <see cref="Parked.Flag"/>.</param>
    /// <returns>Zero where it was read, whatever it found.</returns>
    public static int Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args is not [{ } at])
        {
            Console.Error.WriteLine(Usage);
            return Unreadable;
        }

        // WW406. The other half of being checkable: a dump comes out of a state nothing here can
        // arrange, so the suite starts one of these and images it. See Parked.
        if (string.Equals(at, Parked.Flag, StringComparison.Ordinal))
            return Parked.Park();

        if (!File.Exists(at))
        {
            Console.Error.WriteLine($"there is no dump at '{at}'");
            return Unreadable;
        }

        try
        {
            foreach (var line in Waiting.Said(Dump.Threads(at)))
                Console.Out.WriteLine(line);

            return 0;
        }
        catch (Exception wrong) when (wrong is not (OutOfMemoryException or StackOverflowException))
        {
            // Its own exit code and never zero. A reading that could not be taken is not a reading
            // that found nothing, and a runner told the two apart the same way learns nothing from
            // either.
            Console.Error.WriteLine($"'{at}' could not be read: {wrong.Message}");
            return Unreadable;
        }
    }
}

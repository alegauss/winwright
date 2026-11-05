namespace Winwright.Concordance;

/// <summary>
/// Read the copies of the engine in play and set the exit code by it.
/// <para>
/// WW142. The agreement reading has carried an <c>ExitCode</c> since WW70, whose own comment says
/// the difference between a gate and advice is the number it leaves behind — and until now nothing
/// left it behind. Outside its own tests the whole reading was called by nobody, so the copies were
/// compared exactly as often as somebody opened the file.
/// </para>
/// <para>
/// It takes the copies as paths rather than knowing this repository's layout, because the same step
/// is what an adopting project runs: their tree, their build output, their reference to the package.
/// A gate that only understands the tree it lives in is one nobody else can stand in.
/// </para>
/// </summary>
public static class Program
{
    /// <summary>What a disagreement, or a copy that cannot be pinned at all, exits with.</summary>
    public const int Disagreed = 1;

    /// <summary>What a command line that could not be read exits with.</summary>
    public const int Unusable = 2;

    /// <summary>Compare every copy the command line names.</summary>
    /// <param name="args">See <see cref="Roster.Usage"/>.</param>
    /// <returns>Zero where every copy names the same version.</returns>
    public static int Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var roster = Roster.From(args);
        if (!roster.Readable)
        {
            // Its own exit code and never one: a line that could not be read did not find a
            // disagreement, and a gate that reports the two the same way teaches everyone to
            // re-run it with a flag removed until it goes quiet.
            Console.Error.WriteLine(roster.Complaint);
            Console.Error.WriteLine();
            Console.Error.WriteLine(Roster.Usage);
            return Unusable;
        }

        // WW239. The same line, doing the other half. A release used to rewrite five paths named in
        // YAML and then check four named on a command line, and neither list owned the other — so the
        // one that was wrong on its first run was found by the suite going red on the copy it forgot.
        // Named once here, a copy the rewrite misses is a copy the check was never told about either.
        if (roster.Raises)
        {
            var (said, raised) = roster.Raise();
            foreach (var line in said)
                (raised ? Console.Out : Console.Error).WriteLine(line);

            return raised ? 0 : Disagreed;
        }

        var read = roster.Read();

        // Asked rather than restated: the reading owns what agreement means, and a second spelling
        // of the rule here is the one that would go on saying the old thing after the first moved.
        var to = read.Agreed ? Console.Out : Console.Error;
        to.WriteLine(read.ToString());
        return read.ExitCode == 0 ? 0 : Disagreed;
    }
}

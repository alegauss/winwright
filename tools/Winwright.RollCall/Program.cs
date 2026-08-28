namespace Winwright.RollCall;

/// <summary>
/// Take the roll and set the exit code by it.
/// <para>
/// It belongs on the run rather than in a reviewer's habits: the number moved by six percent and
/// two consecutive readers, both of them the same person, took the word at face value.
/// </para>
/// </summary>
public static class Program
{
    /// <summary>What a run that lost tests exits with.</summary>
    public const int Short = 1;

    /// <summary>What a run that could not be read at all exits with.</summary>
    public const int Unreadable = 2;

    /// <summary>Compare a discovery listing with a results file, writing to the console.</summary>
    /// <param name="args">--discovered &lt;listing&gt; --results &lt;trx&gt; [--most &lt;n&gt;]</param>
    /// <returns>Zero where everyone answered.</returns>
    public static int Main(string[] args) => Take(args, Console.Out, Console.Error);

    /// <summary>
    /// The same, writing where the caller says.
    /// <para>
    /// WW149. This used to write to whatever console it found, and the suite exercises it directly
    /// — which is right, since the exit codes are the thing being asserted. So its sentences
    /// appeared in the middle of a real run, above the real one: a reader skimming a failure saw
    /// <em>4 of 4 were recorded and never ran</em> and then <em>all 957 discovered cases ran</em>,
    /// and only the second was about the run in front of them. The first was a fixture answering
    /// about four names in a temporary file.
    /// </para>
    /// <para>
    /// A writer costs one parameter and buys two things: a run that carries one verdict about
    /// itself, and cases that can assert the words a reader gets rather than leak them. Nothing
    /// checked those words before this, which is its own finding about a tool whose whole output is
    /// a sentence somebody acts on.
    /// </para>
    /// </summary>
    /// <param name="args">--discovered &lt;listing&gt; --results &lt;trx&gt; [--most &lt;n&gt;]</param>
    /// <param name="said">Where a whole roll is written.</param>
    /// <param name="wrong">Where a short roll, a refusal and the usage line go.</param>
    /// <returns>Zero where everyone answered.</returns>
    public static int Take(string[] args, TextWriter said, TextWriter wrong)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(said);
        ArgumentNullException.ThrowIfNull(wrong);

        string? listing = null;
        string? results = null;
        string? excused = null;
        string? against = null;
        var most = 25;

        for (var index = 0; index + 1 < args.Length; index += 2)
        {
            switch (args[index])
            {
                case "--discovered":
                    listing = args[index + 1];
                    break;
                case "--results":
                    results = args[index + 1];
                    break;

                // WW231. Optional, and its absence is not zero: a caller that does not pass it is a
                // caller who never asked, and the roll says so rather than reporting a clean run.
                case "--excused":
                    excused = args[index + 1];
                    break;

                // WW289. The results root every run writes a directory under, so this run's count can
                // be said beside the one before it. Optional and asked the same way `--excused` is:
                // not passing it is not asking, and a root with no earlier run in it answers "there
                // was none" rather than zero.
                case "--against":
                    against = args[index + 1];
                    break;
                case "--most" when int.TryParse(args[index + 1], out var many) && many > 0:
                    most = many;
                    break;
                default:
                    break;
            }
        }

        if (listing is null || results is null)
        {
            wrong.WriteLine(
                "usage: Winwright.RollCall --discovered <dotnet test --list-tests output> "
                    + "--results <trx> [--excused <ledger>] [--against <results root>] [--most n]");
            return Unreadable;
        }

        Roll roll;
        try
        {
            // The overload is the asking. A caller that did not pass the flag takes the two-argument
            // one and hears nothing about excuses; a caller that did takes the three-argument one, and
            // a ledger that is not there then reads as unknown rather than as none. Calling the second
            // either way is what made every run of this tool claim its excuses were unread.
            // WW289. Three overloads and three questions, and which one is called is the asking: no
            // ledger flag hears nothing about excuses, a ledger alone hears the count, and a ledger
            // with a root hears it beside the run before. `--against` without `--excused` is a caller
            // asking to compare a number nobody read, so it is ignored rather than answered.
            roll = (excused, against) switch
            {
                (null, _) => Roll.Of(Readers.DiscoveredIn(listing), Readers.RecordedIn(results)),
                (_, null) => Roll.Of(
                    Readers.DiscoveredIn(listing), Readers.RecordedIn(results), Readers.ExcusedIn(excused)),
                _ => Roll.Of(
                    Readers.DiscoveredIn(listing),
                    Readers.RecordedIn(results),
                    Readers.ExcusedIn(excused),
                    Readers.ExcusedRecently(against, Path.GetDirectoryName(Path.GetFullPath(results)) ?? against),
                    Readers.DiscoveredRecently(against, Path.GetDirectoryName(Path.GetFullPath(results)) ?? against)),
            };
        }
        catch (Exception unreadable) when (unreadable is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            // Unreadable is its own exit code and never zero: a roll call that could not be taken
            // is the one thing that must not read as a roll call that found nothing wrong.
            wrong.WriteLine($"the roll could not be taken: {unreadable.Message}");
            return Unreadable;
        }

        // Asked rather than restated. This used to spell the rule again here, and when the roll
        // learned that a recorded skip is not an answer, the exit code went on saying it was.
        foreach (var line in roll.Render(most))
            (roll.Whole ? said : wrong).WriteLine(line);

        return roll.Whole ? 0 : Short;
    }
}

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

    /// <summary>Compare a discovery listing with a results file.</summary>
    /// <param name="args">--discovered &lt;listing&gt; --results &lt;trx&gt; [--most &lt;n&gt;]</param>
    /// <returns>Zero where everyone answered.</returns>
    public static int Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        string? listing = null;
        string? results = null;
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
                case "--most" when int.TryParse(args[index + 1], out var many) && many > 0:
                    most = many;
                    break;
                default:
                    break;
            }
        }

        if (listing is null || results is null)
        {
            Console.Error.WriteLine(
                "usage: Winwright.RollCall --discovered <dotnet test --list-tests output> --results <trx> [--most n]");
            return Unreadable;
        }

        Roll roll;
        try
        {
            roll = Roll.Of(Readers.DiscoveredIn(listing), Readers.AnsweredIn(results));
        }
        catch (Exception unreadable) when (unreadable is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            // Unreadable is its own exit code and never zero: a roll call that could not be taken
            // is the one thing that must not read as a roll call that found nothing wrong.
            Console.Error.WriteLine($"the roll could not be taken: {unreadable.Message}");
            return Unreadable;
        }

        var complete = roll.Complete && roll.Unexpected.Count == 0 && roll.Discovered.Count > 0;
        foreach (var line in roll.Render(most))
            (complete ? Console.Out : Console.Error).WriteLine(line);

        return complete ? 0 : Short;
    }
}

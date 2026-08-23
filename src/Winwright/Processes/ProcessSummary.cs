namespace Winwright.Processes;

/// <summary>
/// What outlived the run, as a person reads it. It sits beside the verdict rather than inside it,
/// because a leftover process is a fact about this desk and never a defect in the code under test
/// — naming it is the point, and colouring the build red for it would be the opposite of it.
/// </summary>
public static class ProcessSummary
{
    /// <summary>
    /// What this reading is called wherever it is reported. One spelling, because a run's preamble
    /// and a run's summary naming it two ways are two things a reader has to match up by hand.
    /// </summary>
    public const string Named = "nothing outlived the run that started it";

    /// <summary>One line per process that outlived its case, or nothing where none did.</summary>
    public static IReadOnlyList<string> Detail(IReadOnlyList<Survivor> survivors)
    {
        ArgumentNullException.ThrowIfNull(survivors);
        return survivors.Select(survivor => $"  survived   {survivor}").ToList();
    }

    /// <summary>
    /// The sentence that says whether anything was left behind. Reads as a statement either way,
    /// so a clean run says so rather than saying nothing and leaving silence to be interpreted.
    /// </summary>
    public static string Sentence(IReadOnlyList<Survivor> survivors)
    {
        ArgumentNullException.ThrowIfNull(survivors);

        if (survivors.Count == 0)
            return "no process outlived the run that started it.";

        var wouldNotStop = survivors.Count(survivor => survivor.Fate == SurvivorFate.WouldNotStop);
        var stopped = survivors.Count - wouldNotStop;
        var clauses = new List<string>();
        if (stopped > 0)
            clauses.Add($"{stopped} stopped");
        if (wouldNotStop > 0)
            clauses.Add($"{wouldNotStop} would not stop");

        return $"{survivors.Count} outlived the run: {string.Join(", ", clauses)}.";
    }
}

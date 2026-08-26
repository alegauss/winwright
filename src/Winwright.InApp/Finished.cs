namespace Winwright.InApp;

/// <summary>
/// Writing a whole file so that its existence means it is finished.
/// <para>
/// WW218. Both single-shot writers here truncated the file they were about to fill:
/// <c>File.WriteAllText</c> and <c>File.Create</c> each create an empty file first, and the gap
/// before the content lands is a window in which a harness in another process sees a file that is
/// there and holds nothing. WW164 taught the reading side to distrust that, and it still cost two
/// guest runs a red at the boundary of a five-second budget — a sentence about the application
/// under test arriving through a number the suite chose.
/// </para>
/// <para>
/// So the gap is closed rather than tolerated. The content goes to a sibling and is moved over the
/// name the harness is watching, which on one volume is a replace rather than a truncate-and-fill:
/// a reader sees the old file or the new one and never a half of either. That is the marker the
/// design asked for, written last by construction rather than by anybody remembering to.
/// </para>
/// <para>
/// A sibling and not a temp directory, deliberately. The move is only atomic within a volume, and a
/// path the harness named may be anywhere — so the one place guaranteed to be on the same volume as
/// the destination is beside it.
/// </para>
/// <para>
/// Public because an adopting application writing its own artefact hits the identical race, and the
/// two writers this package ships are not special: anything a harness in another process polls for
/// has to be finished before it is findable.
/// </para>
/// </summary>
public static class Finished
{
    /// <summary>What the unfinished file is called while it is being written.</summary>
    public const string Suffix = ".writing";

    /// <summary>
    /// How many goes the move gets. A reader holding the destination open denies the delete the
    /// replace needs, and the reader is polling — so the collision is brief and losing the whole
    /// write to it would be worse than the race this closes.
    /// </summary>
    public const int Attempts = 8;

    /// <summary>How long between goes. Eight of these is the whole budget, and it is bounded.</summary>
    public const int BetweenMs = 25;

    /// <summary>
    /// Write <paramref name="full"/> by filling a sibling and moving it into place.
    /// </summary>
    /// <param name="full">The file a harness is watching. Its directory is created.</param>
    /// <param name="writing">Fills the path it is given, which is the sibling and never the destination.</param>
    /// <exception cref="ArgumentException">Where nothing is named.</exception>
    public static void Writing(string full, Action<string> writing)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(full);
        ArgumentNullException.ThrowIfNull(writing);

        var directory = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var beside = full + Suffix;
        try
        {
            writing(beside);
            Move(beside, full);
        }
        catch
        {
            // The half that did get written is deleted rather than left beside the real name: a
            // stale sibling is a file an operator would open expecting the dump.
            Delete(beside);
            throw;
        }
    }

    private static void Move(string beside, string full)
    {
        for (var attempt = 1; attempt < Attempts; attempt++)
        {
            try
            {
                File.Move(beside, full, overwrite: true);
                return;
            }
            catch (Exception collided) when (collided is IOException or UnauthorizedAccessException)
            {
                // Both, and the second is the one that matters. A reader holding the destination
                // denies the delete the replace needs, and Windows reports that refusal as access
                // denied rather than as a sharing violation — so a guard on IOException alone is a
                // retry that never runs against the only collision it was written for. The case
                // below found that on the run this shipped in.
                //
                // It is polling, so the next look is milliseconds away.
                Thread.Sleep(BetweenMs);
            }
        }

        // The last go is outside the loop and outside the catch, so a collision that never clears
        // throws what it actually was rather than being swallowed by the count running out.
        File.Move(beside, full, overwrite: true);
    }

    private static void Delete(string beside)
    {
        try
        {
            if (File.Exists(beside))
                File.Delete(beside);
        }
        catch (Exception undeletable) when (undeletable is IOException or UnauthorizedAccessException)
        {
            // Nothing to say and nothing to do: the throw on the way out is what the caller needs,
            // and losing it to a failed tidy-up would report the wrong fault.
        }
    }
}

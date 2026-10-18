using Winwright.Acting;

namespace Winwright.Verdicts;

/// <summary>
/// WW545. What a typing act reads in a control that reads as a document — a terminal, whose Text
/// pattern is its whole screen — where nothing it says is ever only the string that was typed.
/// <para>
/// Two readings, and both are arithmetic on strings, which is why they are here and not beside the
/// act that sends the keys: whether the text arrived, and whether what arrived is WW249's
/// substitution of it, the one fault this engine repairs by its signature.
/// </para>
/// </summary>
public static class Substitution
{
    /// <summary>
    /// Whether a document's reading holds what was sent, which is what arriving means for a control
    /// whose reading is all it holds rather than one value.
    /// </summary>
    /// <param name="reading">What the document reads.</param>
    /// <param name="sent">The text the keyboard was given.</param>
    public static bool Holds(string? reading, string sent)
    {
        ArgumentNullException.ThrowIfNull(sent);
        return reading is not null && reading.Contains(sent, StringComparison.Ordinal);
    }

    /// <summary>
    /// Whether a document's reading carries WW249's substitution of what was sent: somewhere in it,
    /// a run as long as the text that differs from it only by holding the last code unit sent where
    /// an earlier one belongs, and more often than the reading before the send did.
    /// <para>
    /// The signature a value is repaired by, looked for at every position rather than across the
    /// whole reading. Counted against the reading before rather than merely absent from it, so the
    /// same wrong line printed earlier by the shell neither hides a new one nor passes for it. Never
    /// where the text carries a control character, because Enter and Tab are acted on as they arrive
    /// and Backspace takes back neither; nor where it is one code unit long, which leaves the last no
    /// earlier position to land on.
    /// </para>
    /// </summary>
    /// <param name="reading">What the document reads after the send.</param>
    /// <param name="before">What it read before the send.</param>
    /// <param name="sent">The text the keyboard was given.</param>
    public static bool InDocument(string? reading, string? before, string sent)
    {
        ArgumentNullException.ThrowIfNull(sent);
        if (reading is null || sent.Length < 2 || sent.Length > reading.Length || sent.Any(char.IsControl))
            return false;

        for (var at = 0; at + sent.Length <= reading.Length; at++)
        {
            var run = reading.Substring(at, sent.Length);
            if (Keyboard.TookTheLastSent(run, sent, sent) && Count(reading, run) > Count(before, run))
                return true;
        }

        return false;
    }

    /// <summary>How many times a run occurs in a reading, overlapping ones included.</summary>
    private static int Count(string? reading, string run)
    {
        if (reading is null)
            return 0;

        var count = 0;
        for (var at = reading.IndexOf(run, StringComparison.Ordinal); at >= 0;
            at = reading.IndexOf(run, at + 1, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}

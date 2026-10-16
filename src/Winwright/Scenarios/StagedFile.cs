namespace Winwright.Scenarios;

/// <summary>
/// One file a fixture stages: where it comes from, relative to the project's root, and where it
/// lands, relative to the staged directory. WW524.
/// <para>
/// Found with WW519, from quickshell's QS217. Staging copied every file to the top of the staged
/// directory under its own name, so an application whose store sits one folder down — as
/// <c>%AppData%\quickshell\sessions.json</c> does — could not be handed that layout, and two files
/// sharing a name from different folders overwrote each other without a word.
/// </para>
/// <para>
/// A plain path still means "copy to the top": the conversion from a string is that form, so every
/// fixture written before this reads exactly as it did.
/// </para>
/// </summary>
/// <param name="From">The file, relative to the project's root.</param>
/// <param name="To">Where it lands under the staged directory, or null for the top under its own name.</param>
/// <param name="Fill">
/// Whether its text has <c>{reach0}</c> and on, and <c>{files}</c>, filled as it is staged. WW527:
/// a store naming the server a fixture reaches would otherwise carry a second spelling of the
/// address, and the probe and the client could then be talking about two servers. Off by default,
/// because most staged files are not text and are copied byte for byte.
/// </param>
/// <param name="Modified">
/// How old the staged copy says it is: null or <c>now</c> for just written, or a minus and a number
/// of s, m, h or d. WW543, read through <see cref="Age"/>.
/// </param>
public sealed record StagedFile(string From, string? To = null, bool Fill = false, string? Modified = null)
{
    /// <summary>
    /// How long ago the staged copy says it was written: zero, which is now, unless the fixture said
    /// otherwise. WW543.
    /// <para>
    /// Found by quickshell (QS335): its client opens an agent's feed only when the feed was written
    /// in the last ten minutes, and a staged copy kept the modification time its source had in the
    /// tree. The case passed on the day the fixture was made and failed the next, with nothing changed.
    /// A store this case put there is one the application was just given, so it is written now — and
    /// <c>"modified": "-1d"</c> stages a stale one on purpose, for the other side of a rule like that.
    /// </para>
    /// </summary>
    /// <exception cref="ScenarioRefusedException">Where <see cref="Modified"/> is not an age.</exception>
    public TimeSpan Age => Aged(this);

    /// <summary>The ages a fixture may write, as a refusal lists them.</summary>
    public const string AgesSpelled = "'now', or a minus and a whole number of s, m, h or d, such as '-2h' or '-1d'";

    private static TimeSpan Aged(StagedFile file)
    {
        var written = file.Modified?.Trim();
        if (written is null || written.Equals("now", StringComparison.OrdinalIgnoreCase))
            return TimeSpan.Zero;

        if (written.Length >= 3
            && written[0] == '-'
            && int.TryParse(written.AsSpan(1, written.Length - 2), System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var count))
        {
            switch (written[^1])
            {
                case 's': return TimeSpan.FromSeconds(count);
                case 'm': return TimeSpan.FromMinutes(count);
                case 'h': return TimeSpan.FromHours(count);
                case 'd': return TimeSpan.FromDays(count);
            }
        }

        throw new ScenarioRefusedException(
            file.From, $"it is staged as modified '{written}', which is no age; an age is {AgesSpelled}");
    }
    /// <summary>The plain form: copy this file to the top of the staged directory.</summary>
    /// <param name="from">The file, relative to the project's root.</param>
    public static implicit operator StagedFile(string from) => new(from);

    /// <summary>Where it lands, relative to the staged directory, with the plain form's default filled in.</summary>
    public string Lands => To ?? System.IO.Path.GetFileName(From);

    /// <summary>The one line a refusal names it by.</summary>
    public override string ToString() =>
        (To is null ? From : $"{From} -> {To}") + (Fill ? ", filled" : "") + (Modified is null ? "" : $", modified {Modified}");
}

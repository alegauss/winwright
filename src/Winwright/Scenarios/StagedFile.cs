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
public sealed record StagedFile(string From, string? To = null)
{
    /// <summary>The plain form: copy this file to the top of the staged directory.</summary>
    /// <param name="from">The file, relative to the project's root.</param>
    public static implicit operator StagedFile(string from) => new(from);

    /// <summary>Where it lands, relative to the staged directory, with the plain form's default filled in.</summary>
    public string Lands => To ?? System.IO.Path.GetFileName(From);

    /// <summary>The one line a refusal names it by.</summary>
    public override string ToString() => To is null ? From : $"{From} -> {To}";
}

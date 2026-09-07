namespace Winwright.Scenarios;

/// <summary>
/// The fields one case wrote on one step, by the names the format spells them. WW391.
/// <para>
/// A field used to join the format in five places: a property on <see cref="StepDeclaration"/>, a
/// parameter on <see cref="StepDeclaration.Of"/>, a line in the construction, a row in
/// <see cref="ScenarioSchema.Step"/>, and a read plus an argument in the loader. Four of those said
/// the same thing — <em>there is a key called this and it holds text</em> — and nothing but a case
/// held the schema and the loader together, so the one somebody forgot was a key that loaded and
/// did nothing.
/// </para>
/// <para>
/// This is the shape the loader already had and could not hand on: named fields out of a document,
/// with the schema saying what the names are. Every read here asks <see cref="ScenarioSchema.Of"/>
/// first, which is what holds a property to its schema row — a name the schema does not have, or
/// one it says holds something else, is a harness error on the first read rather than a field that
/// quietly answers null forever.
/// </para>
/// <para>
/// A step's fields and no other shape's. A case and a fixture are read through the same loader and
/// are not read through this: they hold arrays and objects, and generalising to hold two callers
/// where there is one is the abstraction that has to be undone before the second arrives.
/// </para>
/// </summary>
public sealed class Written
{
    private readonly Dictionary<string, object> wrote = new(StringComparer.Ordinal);

    /// <summary>
    /// Take what a case wrote, refusing a name the format does not have and a value of the wrong
    /// kind. Null is what the case did not write, so it is dropped rather than stored: a field
    /// nobody wrote and a field written as JSON <c>null</c> are the same absence.
    /// </summary>
    /// <param name="wrote">The fields, by name, as text or as true or false.</param>
    /// <exception cref="InvalidOperationException">
    /// Where a name is not a step's field, where the schema says it holds something other than what
    /// the value is, or where one name is written twice. All three are the caller being wrong about
    /// the format rather than a case being wrong about the application, which is why none of them is
    /// a <see cref="ScenarioRefusedException"/>.
    /// </exception>
    public Written(IEnumerable<(string Field, object? Value)> wrote)
    {
        ArgumentNullException.ThrowIfNull(wrote);

        foreach (var (field, value) in wrote)
        {
            if (value is null)
                continue;

            var holds = value switch
            {
                string => Taking.Text,
                bool => Taking.Truth,
                _ => throw new InvalidOperationException(
                    $"'{field}' was written as {value.GetType().Name}, and a step's fields are text or true or false"),
            };

            // What holds this to the schema. The kind is asked rather than assumed, so a row that
            // says 'moves' holds text cannot be read here as true or false.
            ScenarioSchema.Of(ScenarioSchema.Step, field, holds);

            if (!this.wrote.TryAdd(field, value))
                throw new InvalidOperationException($"'{field}' was written twice, and a step writes each field once");
        }
    }

    /// <summary>What a caller writing a step in code says, without building a list first.</summary>
    /// <param name="wrote">The fields, by name.</param>
    public static Written Of(params (string Field, object? Value)[] wrote) => new(wrote);

    /// <summary>Nothing written, which is what a step built by nobody carries.</summary>
    public static Written None { get; } = new([]);

    /// <summary>
    /// A text field exactly as the case wrote it, or null where it wrote none. The raw thing, for
    /// the two readers that need it: what a locator parses, and what an expectation compares.
    /// </summary>
    /// <param name="field">The key, spelled as the file spells it.</param>
    public string? Text(string field)
    {
        ScenarioSchema.Of(ScenarioSchema.Step, field, Taking.Text);
        return wrote.TryGetValue(field, out var value) ? (string)value : null;
    }

    /// <summary>
    /// The same field with the space taken off, and null where nothing but space is left.
    /// <para>
    /// WW365 put this trim in one place because thirteen locals had spelled it out. Blank is nothing
    /// and never the empty string: a field a case left as <c>""</c> claimed nothing, and a claim of
    /// nothing is what every rule reads as absent.
    /// </para>
    /// </summary>
    /// <param name="field">The key, spelled as the file spells it.</param>
    public string? Trimmed(string field) =>
        Text(field) is { } text && !string.IsNullOrWhiteSpace(text) ? text.Trim() : null;

    /// <summary>A flag, false where the case wrote none.</summary>
    /// <param name="field">The key, spelled as the file spells it.</param>
    public bool Truth(string field)
    {
        ScenarioSchema.Of(ScenarioSchema.Step, field, Taking.Truth);
        return wrote.TryGetValue(field, out var value) && (bool)value;
    }
}

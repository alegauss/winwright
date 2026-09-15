namespace Winwright.Scenarios;

/// <summary>
/// The fields one case wrote on one shape of the format, by the names the format spells them. WW391.
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
/// WW435 made it every shape's rather than a step's. It was a step's alone, because generalising to
/// hold two callers where there is one is the abstraction that has to be undone before the second
/// arrives — and then the second arrived: a case's nine fields and a fixture's eight reached the
/// loader one hand-written line each, which is the drift this closed for a step. The shape is the
/// field list, given once at the door; what changes with it is the kinds, because a case holds
/// arrays of words and of shapes where a step holds text and flags.
/// </para>
/// </summary>
public sealed class Written
{
    private readonly IReadOnlyList<Field> fields;
    private readonly Dictionary<string, object> wrote = new(StringComparer.Ordinal);
    private readonly HashSet<string> asked = new(StringComparer.Ordinal);

    /// <summary>
    /// Take what a case wrote on a step, which is the shape this was written for.
    /// </summary>
    /// <param name="wrote">The fields, by name, as text or as true or false.</param>
    /// <exception cref="InvalidOperationException">Where the caller is wrong about the format.</exception>
    public Written(IEnumerable<(string Field, object? Value)> wrote)
        : this(ScenarioSchema.Step, wrote)
    {
    }

    /// <summary>
    /// Take what a case wrote on one shape, refusing a name that shape does not have and a value of
    /// the wrong kind. Null is what the case did not write, so it is dropped rather than stored: a
    /// field nobody wrote and a field written as JSON <c>null</c> are the same absence.
    /// </summary>
    /// <param name="fields">The shape's rows — <see cref="ScenarioSchema.Case"/> and its siblings.</param>
    /// <param name="wrote">The fields, by name, each as the kind its row says it holds.</param>
    /// <exception cref="InvalidOperationException">
    /// Where a name is not one of that shape's fields, where the schema says it holds something other
    /// than what the value is, or where one name is written twice. All three are the caller being
    /// wrong about the format rather than a case being wrong about the application, which is why none
    /// of them is a <see cref="ScenarioRefusedException"/>.
    /// </exception>
    public Written(IReadOnlyList<Field> fields, IEnumerable<(string Field, object? Value)> wrote)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(wrote);

        this.fields = fields;
        foreach (var (field, value) in wrote)
        {
            if (value is null)
                continue;

            // What holds this to the schema. The kind is asked rather than assumed, so a row that
            // says 'moves' holds text cannot be read here as true or false.
            ScenarioSchema.Of(fields, field, Holding(field, value));

            if (!this.wrote.TryAdd(field, value))
                throw new InvalidOperationException($"'{field}' was written twice, and a field is written once");
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
        Asked(field, Taking.Text);
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
        Asked(field, Taking.Truth);
        return wrote.TryGetValue(field, out var value) && (bool)value;
    }

    /// <summary>An array of words, empty where the case wrote none.</summary>
    /// <param name="field">The key, spelled as the file spells it.</param>
    public IReadOnlyList<string> Words(string field)
    {
        Asked(field, Taking.Words);
        return wrote.TryGetValue(field, out var value) ? (IReadOnlyList<string>)value : [];
    }

    /// <summary>An object of text by name, empty where the case wrote none.</summary>
    /// <param name="field">The key, spelled as the file spells it.</param>
    public IReadOnlyDictionary<string, string> Pairs(string field)
    {
        Asked(field, Taking.Pairs);
        return wrote.TryGetValue(field, out var value)
            ? (IReadOnlyDictionary<string, string>)value
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The shapes a field holds — a case's steps, a file's cases — already read and judged by
    /// whoever handed them in. The kind this exists for: a row whose value is not a value, which is
    /// what makes a case a different walk from a step rather than the same one again.
    /// </summary>
    /// <typeparam name="T">What the shapes are.</typeparam>
    /// <param name="field">The key, spelled as the file spells it.</param>
    /// <exception cref="InvalidOperationException">
    /// Where the schema says the field holds a value rather than shapes, or holds shapes of another
    /// kind than <typeparamref name="T"/>.
    /// </exception>
    public IReadOnlyList<T> Shaped<T>(string field)
    {
        var row = ScenarioSchema.Row(fields, field);
        if (row.Holds is Taking.Text or Taking.Truth or Taking.Words or Taking.Pairs)
        {
            throw new InvalidOperationException(
                $"the schema says '{field}' holds {row.Holds}, which is a value and not shapes");
        }

        asked.Add(field);
        if (!wrote.TryGetValue(field, out var value))
            return [];

        return value as IReadOnlyList<T>
            ?? throw new InvalidOperationException(
                $"'{field}' was written as {value.GetType().Name}, and it is being read as {typeof(T).Name}");
    }

    /// <summary>
    /// Refuse a field this shape declares that nothing has read off what was written.
    /// <para>
    /// WW435, and the half that makes the walk a gate rather than a convenience. Reading every row
    /// the schema declares is only half of it: a row read and handed to nobody is the same key that
    /// loads and does nothing, which is the failure this format exists to refuse — an author may
    /// write it, a tool will publish it, and the run will ignore it. Asked of the shape whose fields
    /// are handed on in one place; a step's are read by its own properties as each rule needs them,
    /// and are held to the schema by the read itself.
    /// </para>
    /// </summary>
    /// <param name="shape">What to call the shape in the sentence — a case, a fixture.</param>
    /// <exception cref="InvalidOperationException">Where a field was declared and handed to nothing.</exception>
    public void Handed(string shape)
    {
        var missed = fields.Where(one => !asked.Contains(one.Name)).Select(one => $"'{one.Name}'").ToList();
        if (missed.Count == 0)
            return;

        throw new InvalidOperationException(
            $"the format says a {shape} has {string.Join(", ", missed)} and nothing read "
                + $"{(missed.Count == 1 ? "it" : "them")} off what was written, so an author may write "
                + "it, a tool will publish it, and the run would ignore it");
    }

    /// <summary>What kind a value is, as the schema names kinds.</summary>
    private static Taking Holding(string field, object value) => value switch
    {
        string => Taking.Text,
        bool => Taking.Truth,
        IReadOnlyList<string> => Taking.Words,
        IReadOnlyDictionary<string, string> => Taking.Pairs,
        IReadOnlyList<StepDeclaration> => Taking.Steps,
        IReadOnlyList<CaseDeclaration> => Taking.Cases,
        IReadOnlyList<FixtureDeclaration> => Taking.Fixtures,
        _ => throw new InvalidOperationException(
            $"'{field}' was written as {value.GetType().Name}, which is not a kind the format holds"),
    };

    /// <summary>Hold a read to its row, and record that somebody asked for it.</summary>
    private void Asked(string field, Taking holds)
    {
        ScenarioSchema.Of(fields, field, holds);
        asked.Add(field);
    }
}

using System.Collections.ObjectModel;

namespace Winwright.Scenarios;

/// <summary>One scenario file's text, under the name a refusal about it should use.</summary>
/// <param name="Named">What refusals call it, usually its path.</param>
/// <param name="Json">Its text.</param>
public sealed record ScenarioSource(string Named, string Json);

/// <summary>
/// The fixtures a whole suite has, by name, with the file each was declared in.
/// <para>
/// WW214. WW60 declared fixtures at the file and resolved a case's against its own file's, which is
/// the right scope for the refusal and the wrong one for the declaration: a suite is several files,
/// and the pt-BR launch three of them need was written three times. The second copy is where the
/// flag gains a value the first does not have, and nothing compared them — so every expectation in
/// the second file described an environment nothing put the window into, which is exactly what
/// WW60's own refusal prevents inside one file.
/// </para>
/// <para>
/// So a name resolves across the suite and a name declared twice is refused whichever files it is
/// in, naming both. That is the rule the case names already got from the directory reader; the
/// fixtures were simply outside it.
/// </para>
/// </summary>
public sealed class FixtureSet
{
    private readonly Dictionary<string, FixtureDeclaration> byName;
    private readonly Dictionary<string, string> whose;

    private FixtureSet(Dictionary<string, FixtureDeclaration> byName, Dictionary<string, string> whose)
    {
        this.byName = byName;
        this.whose = whose;
    }

    /// <summary>No fixtures at all, which is what a suite declaring none has.</summary>
    public static FixtureSet Empty { get; } = new(
        new Dictionary<string, FixtureDeclaration>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    /// <summary>Every fixture the suite declares, in the order the files declared them.</summary>
    public IReadOnlyCollection<FixtureDeclaration> All => byName.Values;

    /// <summary>How many there are.</summary>
    public int Count => byName.Count;

    /// <summary>The fixture of that name, or null where the suite declares none.</summary>
    /// <param name="name">The name a case wrote.</param>
    public FixtureDeclaration? Named(string? name) =>
        string.IsNullOrWhiteSpace(name) ? null : byName.GetValueOrDefault(name.Trim());

    /// <summary>Which file declared it, or empty where nothing did.</summary>
    /// <param name="name">The name a case wrote.</param>
    public string Whose(string? name) =>
        string.IsNullOrWhiteSpace(name) ? "" : whose.GetValueOrDefault(name.Trim(), "");

    /// <summary>
    /// The fixtures declared across every one of <paramref name="sources"/>, refusing a name two of
    /// them declare.
    /// </summary>
    /// <param name="sources">The files, each under the name refusals should use.</param>
    /// <exception cref="ScenarioRefusedException">
    /// Where a file will not parse, a fixture in one will not load, or two files declare one name.
    /// </exception>
    public static FixtureSet Across(IEnumerable<ScenarioSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        var set = Empty;
        foreach (var source in sources)
            set = set.With(source.Named, ScenarioFile.FixturesIn(source.Named, source.Json));

        return set;
    }

    /// <summary>
    /// This set plus what <paramref name="named"/> declares. A name already here from the same file
    /// is the same declaration read twice and passes; one from another file is the drift this type
    /// exists to refuse.
    /// </summary>
    /// <param name="named">The file the fixtures came from.</param>
    /// <param name="declared">What it declares.</param>
    /// <exception cref="ScenarioRefusedException">Where another file already declared one of these names.</exception>
    public FixtureSet With(string named, IEnumerable<FixtureDeclaration> declared)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(named);
        ArgumentNullException.ThrowIfNull(declared);

        var merged = new Dictionary<string, FixtureDeclaration>(byName, StringComparer.OrdinalIgnoreCase);
        var from = new Dictionary<string, string>(whose, StringComparer.OrdinalIgnoreCase);

        foreach (var one in declared)
        {
            if (from.TryGetValue(one.Name, out var already) && !string.Equals(already, named, StringComparison.Ordinal))
            {
                throw new ScenarioRefusedException(
                    one.Name,
                    $"it is declared in {already} and again in {named}, so a case naming it names two launches");
            }

            merged[one.Name] = one;
            from[one.Name] = named;
        }

        return new FixtureSet(merged, from);
    }

    /// <summary>The names there are, as a refusal lists them. Says so where there are none.</summary>
    internal string Spelled() => Count == 0
        ? "the suite declares no fixtures"
        : $"there is {string.Join(", ", byName.Values.Select(one => $"'{one.Name}'"))}";

    /// <summary>The names, in declared order.</summary>
    public IReadOnlyList<string> Names => new ReadOnlyCollection<string>(byName.Values.Select(one => one.Name).ToList());
}

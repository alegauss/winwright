using System.Collections.ObjectModel;

namespace Winwright.Scenarios;

/// <summary>
/// What a run was asked to run: nothing, which means all of it, or some cases by name and some by
/// tag.
/// <para>
/// WW59. The value of a small case is partly that it costs ten seconds when a name is what changed,
/// and there was no way to run one. What makes this more than a filter is the refusal below: a
/// selector matching nothing is refused with what there is, rather than producing a run of no cases
/// that reports a pass. That is the same silent pass the third verdict exists to prevent, one level
/// up — and a filtered run reporting success without qualification is how it arrives.
/// </para>
/// <para>
/// Names and tags are matched case-insensitively on purpose. Both are typed at a command line from
/// memory of a file, and refusing <c>--case "Renaming a profile"</c> for its first letter is a
/// refusal about the typist rather than about the suite.
/// </para>
/// </summary>
public sealed record Selection
{
    private Selection(IReadOnlyList<string> cases, IReadOnlyList<string> tags)
    {
        Cases = cases;
        Tags = tags;
    }

    /// <summary>Everything there is, which is what a run with no selection means.</summary>
    public static Selection All { get; } = new([], []);

    /// <summary>The case names asked for, in the order they were asked for.</summary>
    public IReadOnlyList<string> Cases { get; }

    /// <summary>The tags asked for, in the order they were asked for.</summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>Whether this asks for everything, which is what nothing asks for.</summary>
    public bool Unfiltered => Cases.Count == 0 && Tags.Count == 0;

    /// <summary>What was asked for, in the words a report qualifies a pass with.</summary>
    public string Asked
    {
        get
        {
            if (Unfiltered)
                return "everything";

            var parts = new List<string>();
            if (Cases.Count > 0)
                parts.Add($"case{(Cases.Count == 1 ? "" : "s")} {string.Join(", ", Cases.Select(one => $"'{one}'"))}");

            if (Tags.Count > 0)
                parts.Add($"tag{(Tags.Count == 1 ? "" : "s")} {string.Join(", ", Tags)}");

            return string.Join(" and ", parts);
        }
    }

    /// <summary>Compose one, refusing a selector that is blank or asked for twice.</summary>
    /// <param name="cases">Case names.</param>
    /// <param name="tags">Tags.</param>
    /// <exception cref="ScenarioRefusedException">Where a selector is blank or repeated.</exception>
    public static Selection Of(IEnumerable<string>? cases, IEnumerable<string>? tags = null) =>
        new(Distinct(cases, "case"), Distinct(tags, "tag"));

    /// <summary>One case by name, which is the selection this whole task is about.</summary>
    /// <param name="name">The case's name.</param>
    public static Selection Case(string name) => Of([name]);

    /// <summary>One tag.</summary>
    /// <param name="tag">The tag.</param>
    public static Selection Tag(string tag) => Of(null, [tag]);

    /// <summary>Whether <paramref name="one"/> is in this selection.</summary>
    public bool Takes(CaseDeclaration one)
    {
        ArgumentNullException.ThrowIfNull(one);
        if (Unfiltered)
            return true;

        return Cases.Contains(one.Name, StringComparer.OrdinalIgnoreCase) || Tags.Any(one.Tagged);
    }

    /// <summary>Why <paramref name="one"/> is not in this selection, or null where it is.</summary>
    public string? Leaves(CaseDeclaration one) => Takes(one) ? null : $"not selected by {Asked}";

    /// <summary>
    /// The cases this selects out of <paramref name="declared"/>, in declared order — refusing a
    /// selector that matches nothing.
    /// <para>
    /// The refusal is the point. A misspelled case name that simply selects nothing gives a run of
    /// no cases, and a run of no cases has no failure and no hole in it, so it reads as a pass. What
    /// the author asked was answered by a green about nothing.
    /// </para>
    /// </summary>
    /// <param name="declared">Every case there is.</param>
    /// <exception cref="ScenarioRefusedException">Where a name or a tag selects no case.</exception>
    public IReadOnlyList<CaseDeclaration> Over(IReadOnlyList<CaseDeclaration> declared)
    {
        ArgumentNullException.ThrowIfNull(declared);

        foreach (var name in Cases)
        {
            if (!declared.Any(one => string.Equals(one.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ScenarioRefusedException(
                    name,
                    $"no case is called that; there is {Spelled(declared.Select(one => $"'{one.Name}'"))}");
            }
        }

        foreach (var tag in Tags)
        {
            if (!declared.Any(one => one.Tagged(tag)))
            {
                var tagged = declared.SelectMany(one => one.Tags).Distinct(StringComparer.OrdinalIgnoreCase);
                throw new ScenarioRefusedException(tag, $"no case is tagged that; there is {Spelled(tagged)}");
            }
        }

        return new ReadOnlyCollection<CaseDeclaration>(declared.Where(Takes).ToList());
    }

    /// <summary>What was asked for, which is the only thing a report wants a selection to say.</summary>
    public override string ToString() => Asked;

    private static string Spelled(IEnumerable<string> what)
    {
        var listed = what.ToList();
        return listed.Count == 0 ? "none at all" : string.Join(", ", listed);
    }

    private static IReadOnlyList<string> Distinct(IEnumerable<string>? asked, string what)
    {
        var collected = new List<string>();
        foreach (var one in asked ?? [])
        {
            if (string.IsNullOrWhiteSpace(one))
                throw new ScenarioRefusedException($"<blank {what}>", $"a blank {what} selects nothing, and asking for it says nothing");

            var trimmed = one.Trim();
            if (collected.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                throw new ScenarioRefusedException(trimmed, $"that {what} is asked for twice");

            collected.Add(trimmed);
        }

        return new ReadOnlyCollection<string>(collected);
    }
}

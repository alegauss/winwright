using System.Collections.ObjectModel;

namespace Winwright.Projects;

/// <summary>
/// The entries this project says end the run, named once beside the executable and the timeouts.
/// <para>
/// The acting block's criterion said destructive entries are named in the scenario and reached
/// only by traversal. The second half shipped — walking a menu cannot invoke anything, there is no
/// such method on that surface — and the route beside it stayed wide open: the general invoke
/// pressed a menu item called Quit exactly as willingly as one called Open, and nothing anywhere
/// knew the difference.
/// </para>
/// <para>
/// Which entry quits is a fact about the application rather than about a case, so the list belongs
/// here. Safety then rests on the declaration rather than on the author of every scenario
/// remembering — which on one adopting project is one entry, and on the next a different one
/// nobody has met yet.
/// </para>
/// </summary>
public sealed class Destructive
{
    private readonly IReadOnlyList<string> entries;

    private Destructive(IReadOnlyList<string> entries)
    {
        this.entries = entries;
    }

    /// <summary>A project that declares none. Nothing is refused, and nothing pretends otherwise.</summary>
    public static Destructive None { get; } = new(new ReadOnlyCollection<string>([]));

    /// <summary>Every entry named, in the order the project named them.</summary>
    public IReadOnlyList<string> Entries => entries;

    /// <summary>Whether this project names any at all.</summary>
    public bool Any => entries.Count > 0;

    /// <summary>Read a declared list, dropping the blanks a hand-written file collects.</summary>
    /// <param name="declared">The names, or null where the project declared none.</param>
    public static Destructive Of(IEnumerable<string>? declared)
    {
        if (declared is null)
            return None;

        var named = declared
            .Where(one => !string.IsNullOrWhiteSpace(one))
            .Select(one => one.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return named.Count == 0 ? None : new Destructive(new ReadOnlyCollection<string>(named));
    }

    /// <summary>
    /// The declared entry this element is, or null where it is none of them.
    /// <para>
    /// Matched on the automation id first, that being the one field the application controls, and
    /// then on the name a person sees. A name is compared without case because a menu writes
    /// <c>Quit</c> and a declaration writes <c>quit</c>, and neither author is wrong.
    /// </para>
    /// </summary>
    /// <param name="name">What the element is called.</param>
    /// <param name="automationId">Its automation id, or empty where it has none.</param>
    public string? Matched(string? name, string? automationId)
    {
        foreach (var entry in entries)
        {
            if (!string.IsNullOrEmpty(automationId)
                && string.Equals(entry, automationId, StringComparison.Ordinal))
            {
                return entry;
            }

            if (!string.IsNullOrEmpty(name)
                && string.Equals(entry, name.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }

        return null;
    }

    /// <summary>What this project refuses, in the one sentence a report prints.</summary>
    public string Sentence() => Any
        ? $"{entries.Count} entr{(entries.Count == 1 ? "y is" : "ies are")} declared destructive: "
            + string.Join(", ", entries.Select(one => $"\"{one}\"")) + "."
        : "this project declares no destructive entry, so nothing here is refused.";

    /// <summary>The list as a report names it.</summary>
    public override string ToString() => Sentence();
}

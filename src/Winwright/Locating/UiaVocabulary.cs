using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Automation;

namespace Winwright.Locating;

/// <summary>
/// The words UI Automation itself knows, read off UI Automation rather than kept here. A list of
/// control types typed into this file is a list that drifts from the thing it describes, and the
/// drift shows up as a locator that parses and matches nothing — which is the shape of check this
/// project refuses at load rather than counts as a hole at run time.
/// </summary>
public static class UiaVocabulary
{
    /// <summary>Every control type name a step may carry, in alphabetical order.</summary>
    public static IReadOnlyList<string> ControlTypes { get; } = new ReadOnlyCollection<string>(
        typeof(ControlType)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(ControlType))
            .Select(field => field.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList());

    /// <summary>Every pattern name a step may require, in alphabetical order.</summary>
    public static IReadOnlyList<string> Patterns { get; } = new ReadOnlyCollection<string>(
        typeof(InvokePattern).Assembly
            .GetTypes()
            .Where(type => type.IsPublic && type.Name.EndsWith("Pattern", StringComparison.Ordinal))
            .Where(type => type.GetField("Pattern", BindingFlags.Public | BindingFlags.Static)?.FieldType
                == typeof(AutomationPattern))
            .Select(type => type.Name[..^"Pattern".Length])
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList());

    private static readonly HashSet<string> KnownTypes = new(ControlTypes, StringComparer.Ordinal);
    private static readonly HashSet<string> KnownPatterns = new(Patterns, StringComparer.Ordinal);

    /// <summary>Whether UI Automation has a control type by that name, spelled exactly.</summary>
    public static bool IsControlType(string name) => KnownTypes.Contains(name);

    /// <summary>Whether UI Automation has a pattern by that name, spelled exactly.</summary>
    public static bool IsPattern(string name) => KnownPatterns.Contains(name);

    /// <summary>The control type object behind a name the grammar accepted.</summary>
    /// <exception cref="ArgumentException">Where UI Automation has no such control type.</exception>
    public static ControlType ControlTypeFor(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return typeof(ControlType)
                .GetField(name, BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null) as ControlType
            ?? throw new ArgumentException($"'{name}' is no UI Automation control type", nameof(name));
    }

    /// <summary>
    /// The names closest to <paramref name="name"/> in <paramref name="vocabulary"/>, so a refusal
    /// over a typo offers the word that was meant instead of the whole list.
    /// </summary>
    public static IReadOnlyList<string> Nearest(string name, IReadOnlyList<string> vocabulary, int how_many = 3)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(vocabulary);

        return vocabulary
            .Select(candidate => (candidate, distance: Distance(name, candidate)))
            .OrderBy(pair => pair.distance)
            .ThenBy(pair => pair.candidate, StringComparer.Ordinal)
            .Take(how_many)
            .Select(pair => pair.candidate)
            .ToList();
    }

    private static int Distance(string from, string to)
    {
        var previous = new int[to.Length + 1];
        var current = new int[to.Length + 1];
        for (var column = 0; column <= to.Length; column++)
            previous[column] = column;

        for (var row = 1; row <= from.Length; row++)
        {
            current[0] = row;
            for (var column = 1; column <= to.Length; column++)
            {
                var substitute = previous[column - 1]
                    + (char.ToLowerInvariant(from[row - 1]) == char.ToLowerInvariant(to[column - 1]) ? 0 : 1);
                current[column] = Math.Min(Math.Min(current[column - 1] + 1, previous[column] + 1), substitute);
            }

            (previous, current) = (current, previous);
        }

        return previous[to.Length];
    }
}

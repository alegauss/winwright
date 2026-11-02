using System.Diagnostics;

namespace Paneproof.Projects;

/// <summary>
/// The names an adopter's suite was written against before the product was Paneproof, and the rule
/// that keeps them working for a stated period. WW591.
/// <para>
/// An existing suite names <c>winwright.json</c> and the <c>WINWRIGHT_*</c> variables. Renaming
/// them in one release breaks every adopter at once, and silently where a variable is only read
/// when it is present. So the new name is read first, the old one is accepted where the new one is
/// absent and said so in the run's report, and both at once is refused, because two values for one
/// setting is a choice this cannot make for the adopter.
/// </para>
/// <para>
/// The variables the engine hands the in-app half go the other way: the engine and
/// <c>Paneproof.InApp</c> are upgraded separately, so a launch sets both names and an application
/// still on the old in-app half keeps its report. The old names are accepted until the release
/// notes say otherwise; the period is theirs to state, not this file's.
/// </para>
/// </summary>
public static class Renamed
{
    /// <summary>The prefix every variable the product owns now carries.</summary>
    public const string Prefix = "PANEPROOF_";

    /// <summary>The prefix those variables carried before.</summary>
    public const string OldPrefix = "WINWRIGHT_";

    /// <summary>The project declaration's old file name.</summary>
    public const string OldDeclaration = "winwright.json";

    /// <summary>What every namespace, assembly and package id began with. WW597.</summary>
    public const string OldName = "Winwright";

    /// <summary>What they begin with now.</summary>
    public const string Name = "Paneproof";

    /// <summary>
    /// The old spelling of one of the product's namespaces or project files: <c>Paneproof.Acting</c>
    /// was <c>Winwright.Acting</c>. WW597.
    /// </summary>
    /// <param name="named">The name as it is spelled now.</param>
    public static string OldSpelling(string named)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(named);
        return named.StartsWith(Name + ".", StringComparison.Ordinal)
            ? OldName + named[Name.Length..]
            : throw new ArgumentException($"'{named}' is not one of the product's names", nameof(named));
    }

    /// <summary>The old spelling of one of the product's variables.</summary>
    /// <param name="variable">The variable under its new name.</param>
    public static string Old(string variable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variable);
        return variable.StartsWith(Prefix, StringComparison.Ordinal)
            ? OldPrefix + variable[Prefix.Length..]
            : throw new ArgumentException($"'{variable}' is not one of the product's variables", nameof(variable));
    }

    /// <summary>
    /// Read one of the product's variables from this process: the new name, then the old one.
    /// </summary>
    /// <param name="variable">The variable under its new name.</param>
    /// <param name="noticed">Where to say the old name was the one read, or null to say nothing.</param>
    /// <exception cref="InvalidOperationException">Where both names are set.</exception>
    public static string? Variable(string variable, ICollection<string>? noticed = null)
    {
        var old = Old(variable);
        var now = Environment.GetEnvironmentVariable(variable);
        var then = Environment.GetEnvironmentVariable(old);

        if (!string.IsNullOrWhiteSpace(now) && !string.IsNullOrWhiteSpace(then))
        {
            throw new InvalidOperationException(
                $"both {variable} ('{now}') and {old} ('{then}') are set, and they name one setting; "
                    + $"{old} is its old name, so unset it and keep {variable}");
        }

        if (!string.IsNullOrWhiteSpace(now))
            return now;

        if (string.IsNullOrWhiteSpace(then))
            return null;

        noticed?.Add($"{old} was read because {variable} is not set; {old} is the old name and is accepted for now, so rename it");
        return then;
    }

    /// <summary>
    /// Give a launch the old name of each variable it carries under the new one, so an application
    /// still on the old in-app half reads what the new one would.
    /// </summary>
    /// <param name="start">The launch.</param>
    /// <param name="variables">The variables, under their new names.</param>
    public static void Both(ProcessStartInfo start, params string[] variables)
    {
        ArgumentNullException.ThrowIfNull(start);

        foreach (var variable in variables)
        {
            var old = Old(variable);
            if (start.Environment.TryGetValue(variable, out var value) && !string.IsNullOrEmpty(value)
                && !start.Environment.ContainsKey(old))
            {
                start.Environment[old] = value;
            }
        }
    }
}

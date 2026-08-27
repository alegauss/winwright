using System.Globalization;
using System.Text.Json;

using Winwright.Verdicts;

namespace Winwright.Projects;

/// <summary>Where the language a running application is in came from.</summary>
public enum LanguageSource
{
    /// <summary>The user's saved preference, read from the file the application saves it in.</summary>
    SavedPreference,

    /// <summary>Windows' display language, which is what the application falls back to.</summary>
    DisplayLanguage,
}

/// <summary>
/// Which language the application under test is actually in.
/// <para>
/// Measured in claude-tray: verifying a task against a Portuguese tray with the default English
/// produced four failures for labels that were all present, in another language. On an attach
/// there is no command line to read, so this resolves it the way the application resolves it —
/// saved preference first, then the display language — and says so out loud whatever the answer.
/// </para>
/// <para>
/// A language a scenario explicitly asked for that the process cannot be in is a hole, not a
/// substitution: replacing it quietly is how four present labels were reported as four failures.
/// </para>
/// </summary>
public sealed record ResolvedLanguage
{
    /// <summary>The name every scenario refers to this condition by.</summary>
    public const string PreconditionName = "the application is in the language this scenario is written for";

    private ResolvedLanguage(CultureInfo culture, LanguageSource source, string from, string? preferenceMiss)
    {
        Culture = culture;
        Source = source;
        From = from;
        PreferenceMiss = preferenceMiss;
    }

    /// <summary>The language the application is in.</summary>
    public CultureInfo Culture { get; }

    /// <summary>Which of the two answered.</summary>
    public LanguageSource Source { get; }

    /// <summary>Where it came from, as a person reads it — a path, or the display language.</summary>
    public string From { get; }

    /// <summary>
    /// Why the saved preference did not answer, where a file was declared and did not. Kept
    /// because "it fell back" and "there was nothing to fall back from" are different sentences.
    /// </summary>
    public string? PreferenceMiss { get; }

    /// <summary>
    /// The language a caller already knows the window is in, taken as read.
    /// <para>
    /// WW261. Nothing is resolved here and nothing needs to be: a fixture that says its window is in
    /// <c>pt-BR</c> has answered the question this type exists to ask, and asking it again off the
    /// desk would be the guess the declaration replaced. <see cref="Resolve(ProjectDeclaration)"/> is
    /// still what an attach uses, where there was no launch to have said anything.
    /// </para>
    /// </summary>
    /// <param name="culture">What the window is in.</param>
    public static ResolvedLanguage Speaking(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        return new ResolvedLanguage(culture, LanguageSource.DisplayLanguage, "the fixture that launched it", null);
    }

    /// <summary>Resolve it from what the project declared, falling back to this machine's display language.</summary>
    public static ResolvedLanguage Resolve(ProjectDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        return Resolve(
            declaration.LanguagePreferenceFile,
            declaration.LanguagePreferenceKey,
            CultureInfo.InstalledUICulture);
    }

    /// <summary>
    /// The same resolution, spelled out. <paramref name="display"/> is passed rather than read so
    /// that what this machine happens to be set to never decides what a test proves.
    /// </summary>
    public static ResolvedLanguage Resolve(string? preferenceFile, string? preferenceKey, CultureInfo display)
    {
        ArgumentNullException.ThrowIfNull(display);

        if (string.IsNullOrWhiteSpace(preferenceFile) || string.IsNullOrWhiteSpace(preferenceKey))
            return new ResolvedLanguage(display, LanguageSource.DisplayLanguage, "the display language", null);

        var (saved, miss) = Saved(preferenceFile, preferenceKey);
        return saved is null
            ? new ResolvedLanguage(display, LanguageSource.DisplayLanguage, "the display language", miss)
            : new ResolvedLanguage(saved, LanguageSource.SavedPreference, preferenceFile, null);
    }

    /// <summary>
    /// Whether the application is in <paramref name="asked"/>. A neutral ask matches any of its
    /// regions, so a scenario written for <c>en</c> is content with <c>en-GB</c>; two different
    /// specific cultures are not each other. An ask of nothing is met — the scenario did not
    /// claim a language, and the resolution is reported rather than judged.
    /// </summary>
    public Precondition Matching(string? asked)
    {
        if (string.IsNullOrWhiteSpace(asked))
            return Precondition.Met(PreconditionName);

        return Matches(asked.Trim())
            ? Precondition.Met(PreconditionName)
            : Precondition.Absent(
                PreconditionName,
                $"the scenario asks for {asked.Trim()} and the application is in {Culture.Name} (from {From})");
    }

    /// <summary>Which language this run found, and where it read it, said whatever the answer.</summary>
    public string Sentence()
    {
        var missed = PreferenceMiss is null ? "" : $" ({PreferenceMiss})";
        return $"the application is in {Culture.Name}, from {From}{missed}.";
    }

    private bool Matches(string asked)
    {
        CultureInfo wanted;
        try
        {
            // predefinedOnly, because .NET happily manufactures a CultureInfo for any well-formed
            // tag: without it "zz-ZZ" is a culture, and an ask nobody could satisfy would match
            // an application nobody could run.
            wanted = CultureInfo.GetCultureInfo(asked, predefinedOnly: true);
        }
        catch (CultureNotFoundException)
        {
            return false;
        }

        if (string.Equals(wanted.Name, Culture.Name, StringComparison.OrdinalIgnoreCase))
            return true;

        return wanted.IsNeutralCulture
            && string.Equals(
                wanted.TwoLetterISOLanguageName, Culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase);
    }

    private static (CultureInfo? Saved, string? Miss) Saved(string file, string key)
    {
        if (!File.Exists(file))
            return (null, $"{file} is not there");

        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(
                File.ReadAllText(file),
                new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            root = document.RootElement.Clone();
        }
        catch (JsonException broken)
        {
            return (null, $"{file} is not readable JSON: {broken.Message}");
        }

        var element = root;
        foreach (var step in key.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(step, out element))
                return (null, $"{file} declares no '{key}'");
        }

        if (element.ValueKind != JsonValueKind.String)
            return (null, $"'{key}' in {file} is not a language name");

        var name = element.GetString();
        if (string.IsNullOrWhiteSpace(name))
            return (null, $"'{key}' in {file} is empty");

        try
        {
            return (CultureInfo.GetCultureInfo(name.Trim(), predefinedOnly: true), null);
        }
        catch (CultureNotFoundException)
        {
            return (null, $"'{key}' in {file} is '{name}', which is no language Windows knows");
        }
    }
}

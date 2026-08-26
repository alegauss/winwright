using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

using Winwright.Projects;

namespace Winwright.Asserting;

/// <summary>
/// The ways a label can be unusable, each of which the author fixes somewhere different.
/// <para>
/// WW196. A type thrown six times was one entry in the pairing naming one case. Two of these are
/// about a project that declares the wrong thing, two about a language it does not ship, one about
/// a key that is not there and one about a file that would not open — six errands, one entry.
/// </para>
/// </summary>
public enum UnusableLabel
{
    /// <summary>Thrown without saying which. Pairs with nothing, and the suite refuses it.</summary>
    Unsaid,

    /// <summary>The project declares no language files whose names carry a language tag.</summary>
    NoLanguageFiles,

    /// <summary>The key is not in the strings this project ships for the chosen language.</summary>
    KeyNotThere,

    /// <summary>The label carries a placeholder, so a filled-in tree could never match it exactly.</summary>
    CarriesAPlaceholder,

    /// <summary>The application is in a language this project ships and nothing says what to fall back to.</summary>
    NoFallbackDeclared,

    /// <summary>A fallback is declared and the project ships no strings for it.</summary>
    FallbackNotShipped,

    /// <summary>The strings file itself could not be read.</summary>
    FileUnreadable,
}

/// <summary>Raised where a label cannot be read, or can be read and could never match.</summary>
public sealed class UnusableLabelException : InvalidOperationException
{
    /// <summary>Say which label, and why nothing can be asserted with it.</summary>
    public UnusableLabelException(string message)
        : base(message)
    {
    }

    /// <summary>The same, saying which of the ways a label can be unusable this one is.</summary>
    /// <param name="arm">Which way.</param>
    /// <param name="message">Which label, and why nothing can be asserted with it.</param>
    public UnusableLabelException(UnusableLabel arm, string message)
        : base(message)
    {
        Arm = arm;
    }

    /// <summary>Unused. Present because an exception with no default shapes is awkward to catch.</summary>
    public UnusableLabelException()
        : base("the label could not be resolved from the project's own strings")
    {
    }

    /// <summary>Unused. Present for the same reason.</summary>
    public UnusableLabelException(string message, Exception inner)
        : base(message, inner)
    {
    }

    /// <summary>
    /// Which way this label is unusable. <see cref="UnusableLabel.Unsaid" /> where it was thrown
    /// without saying — a refusal nothing can pair, and the check says so.
    /// </summary>
    public UnusableLabel Arm { get; } = UnusableLabel.Unsaid;
}

/// <summary>One label, in the language the application is actually rendering.</summary>
/// <param name="Key">The key it was read under.</param>
/// <param name="Text">What the window should be showing.</param>
/// <param name="File">The strings file it came out of.</param>
/// <param name="Culture">The language that file is in.</param>
/// <param name="Asked">The language the application resolved to, which may not be that one.</param>
public sealed record Label(string Key, string Text, string File, CultureInfo Culture, CultureInfo Asked)
{
    /// <summary>
    /// Whether the project shipped no strings in the language the application is in. Measured on
    /// the language and not on the full tag: a project shipping <c>pt</c> to an application in
    /// <c>pt-BR</c> has not fallen back to anything, and calling that a fallback would put a
    /// warning on the one case where the window really is in the language that was asked for.
    /// </summary>
    public bool FellBack => !string.Equals(
        Culture.TwoLetterISOLanguageName, Asked.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase);

    /// <summary>Where this label came from, said whatever the answer — including when it fell back.</summary>
    public string Sentence()
    {
        var named = System.IO.Path.GetFileName(File);
        return FellBack
            ? $"'{Key}' reads '{Text}' from {named} in {Culture.Name}, which is the fallback: the project ships "
                + $"no strings for {Asked.Name}, and the application falls back the same way."
            : $"'{Key}' reads '{Text}' from {named} in {Culture.Name}.";
    }
}

/// <summary>
/// Labels read out of the project's own strings, in whatever language the application is in.
/// <para>
/// An assertion matching English words against a window rendering another language is loud when it
/// fails and silent one step over, where it matches nothing and passes. Measured in claude-tray:
/// verifying against a Portuguese tray with the default English produced four failures for labels
/// that were all present, in another language.
/// </para>
/// <para>
/// A value carrying a placeholder is refused rather than skipped. An exact-name read of a tree
/// holding <c>Bem-vindo, Alexandre</c> can never match <c>Bem-vindo, {0}</c>, so a scenario that
/// asks for one is asking for something that cannot pass — and skipping it in silence is the
/// failure this whole rule exists to prevent, wearing a different hat.
/// </para>
/// </summary>
public static class Labels
{
    /// <summary>
    /// What a placeholder looks like in the shapes a Windows application ships: composite
    /// formatting, ICU-style names, printf, and the doubled braces a templating layer leaves.
    /// </summary>
    private static readonly Regex Placeholder = new(
        @"\{\{?\s*[\w.\-]*\s*(?:,[^{}]*)?(?::[^{}]*)?\}\}?|%(?:\d+\$)?[sdfx@]",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// Read one label for the language this run resolved the application to.
    /// </summary>
    /// <param name="key">The key, dotted for a nested one.</param>
    /// <param name="declaration">The project, which is where the strings files are declared.</param>
    /// <param name="language">Which language the application is in.</param>
    /// <exception cref="UnusableLabelException">
    /// Where the project declares no strings for that language and no fallback, where the key is
    /// not in the file, or where the value carries a placeholder.
    /// </exception>
    public static Label For(string key, ProjectDeclaration declaration, ResolvedLanguage language)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentNullException.ThrowIfNull(language);

        var tagged = Tagged(declaration);
        if (tagged.Count == 0)
            throw new UnusableLabelException(
                UnusableLabel.NoLanguageFiles,
                $"'{key}' is read from the project's strings and {declaration.Path} declares no languageFiles "
                    + "whose names carry a language tag, such as strings.en.json");

        var wanted = language.Culture;
        var chosen = Nearest(tagged, wanted) ?? Fallback(tagged, declaration, key, wanted);

        var text = Read(chosen.File, key)
            ?? throw new UnusableLabelException(
                UnusableLabel.KeyNotThere,
                $"'{key}' is not in {chosen.File}, which is the {chosen.Culture.Name} strings this project ships");

        // Refused, and never skipped. A skipped label is an assertion that did not run reported
        // as one that passed, which is the defect the whole project exists over.
        var found = Placeholder.Match(text);
        if (found.Success)
            throw new UnusableLabelException(
                UnusableLabel.CarriesAPlaceholder,
                $"'{key}' reads '{text}' in {System.IO.Path.GetFileName(chosen.File)}, which carries the "
                    + $"placeholder '{found.Value}': a tree holding it already filled in can never match this "
                    + "exactly, so nothing here could ever pass");

        return new Label(key.Trim(), text, chosen.File, chosen.Culture, wanted);
    }

    /// <summary>The same, resolving the language from the project the way the application does.</summary>
    public static Label For(string key, ProjectDeclaration declaration) =>
        For(key, declaration, ResolvedLanguage.Resolve(declaration));

    /// <summary>
    /// Whether a string carries a placeholder, which is what makes it unusable for an exact read.
    /// Public because a set derived out of the same files has exactly the same hazard.
    /// </summary>
    public static bool CarriesAPlaceholder(string text) =>
        !string.IsNullOrEmpty(text) && Placeholder.IsMatch(text);

    private static (string File, CultureInfo Culture) Fallback(
        IReadOnlyList<(string File, CultureInfo Culture)> tagged,
        ProjectDeclaration declaration,
        string key,
        CultureInfo wanted)
    {
        var declared = string.Join(", ", tagged.Select(pair => pair.Culture.Name));

        // No default is invented. Falling back to whichever file happens to be first is how an
        // assertion ends up matching English against a window that is not in English.
        if (declaration.LanguageFallback is null)
            throw new UnusableLabelException(
                UnusableLabel.NoFallbackDeclared,
                $"'{key}': the application is in {wanted.Name} and this project ships {declared}. "
                    + $"Declare language.fallback in {declaration.Path} to say which one it falls back to");

        var named = Culture(declaration.LanguageFallback);
        return (named is null ? null : Nearest(tagged, named))
            ?? throw new UnusableLabelException(
                UnusableLabel.FallbackNotShipped,
                $"'{key}': {declaration.Path} falls back to {declaration.LanguageFallback}, and the strings "
                    + $"it ships are {declared}");
    }

    private static (string File, CultureInfo Culture)? Nearest(
        IReadOnlyList<(string File, CultureInfo Culture)> tagged, CultureInfo wanted)
    {
        foreach (var pair in tagged)
        {
            if (string.Equals(pair.Culture.Name, wanted.Name, StringComparison.OrdinalIgnoreCase))
                return pair;
        }

        // pt-BR is served by pt where the project ships one, which is the fallback a resource
        // loader makes and therefore the one the window in front of you already made.
        foreach (var pair in tagged)
        {
            if (string.Equals(
                pair.Culture.TwoLetterISOLanguageName,
                wanted.TwoLetterISOLanguageName,
                StringComparison.OrdinalIgnoreCase))
            {
                return pair;
            }
        }

        return null;
    }

    private static List<(string File, CultureInfo Culture)> Tagged(ProjectDeclaration declaration)
    {
        var found = new List<(string, CultureInfo)>();
        foreach (var file in declaration.LanguageFiles)
        {
            // Read right to left out of the file name — strings.pt-BR.json is pt-BR, and
            // strings.json is nothing, which is not an error and simply never answers.
            //
            // WW242: down to and including the first part, which it used to stop before. That bound
            // read like it was protecting the case named above and was not needed for it —
            // Culture("strings") already answers null, as it does for any part that is not a
            // predefined tag, and predefinedOnly is what carries that weight at every index. What it
            // did instead was make `en.json` invisible: the walk started at 0 and the condition was
            // false before the first turn. Measured against claude-tray, which ships lang/en.json and
            // four more beside it — the layout an application reaches for when the folder already
            // says what the files are.
            var parts = System.IO.Path.GetFileName(file).Split('.');
            for (var index = parts.Length - 2; index >= 0; index--)
            {
                if (Culture(parts[index]) is { } culture)
                {
                    found.Add((file, culture));
                    break;
                }
            }
        }

        return found;
    }

    private static CultureInfo? Culture(string name)
    {
        try
        {
            // predefinedOnly, for the reason ResolvedLanguage gives: without it .NET manufactures
            // a culture for any well-formed tag, and 'strings.min.json' would be a language.
            return CultureInfo.GetCultureInfo(name, predefinedOnly: true);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// One key out of one strings file. The reading itself lives in <see cref="JsonSource"/>, so
    /// the destructive guard and this answer the same question the same way; what is here is the
    /// refusal, which is a label's business and not a file reader's.
    /// </summary>
    private static string? Read(string file, string key)
    {
        try
        {
            return JsonSource.Value(file, key);
        }
        catch (Exception unreadable) when (unreadable is JsonException or IOException)
        {
            throw new UnusableLabelException(UnusableLabel.FileUnreadable, $"{file} could not be read: {unreadable.Message}");
        }
    }
}

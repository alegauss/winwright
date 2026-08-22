using System.Globalization;
using System.IO;
using System.Text.Json;

namespace Winwright.Fixture;

/// <summary>
/// The fixture's own strings, in several languages.
/// <para>
/// The label rule needs more than one language to be developed at all, and it needs one specific
/// pathological case: a key whose value carries a placeholder. An exact-name read can never match
/// that, and a rule that skipped it would report a green about a control nobody could have
/// checked. Real products have the languages and rarely have the pathological key on purpose.
/// </para>
/// <para>
/// Read from files beside the executable rather than from constants, because that is where an
/// adopting project keeps its own and the point is to be the thing they copy.
/// </para>
/// </summary>
public sealed class Strings
{
    private readonly JsonElement root;

    private Strings(JsonElement root, string culture, string file)
    {
        this.root = root;
        Culture = culture;
        File = file;
    }

    /// <summary>The language tags the fixture ships, in the order the catalogue prints them.</summary>
    public static IReadOnlyList<string> Cultures { get; } = ["en", "pt-BR", "de"];

    /// <summary>The key whose value carries a placeholder, which the label rule has to refuse.</summary>
    public const string PlaceholderKey = "labels.profileName";

    /// <summary>The language these strings are in.</summary>
    public string Culture { get; }

    /// <summary>The file they came out of, so a check can be pointed at the same one.</summary>
    public string File { get; }

    /// <summary>Where the fixture keeps them, beside its own executable.</summary>
    public static string Directory =>
        Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory, "strings");

    /// <summary>The file one language is in, whether or not it is there.</summary>
    /// <param name="culture">The language tag.</param>
    public static string FileFor(string culture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(culture);
        return Path.Combine(Directory, $"strings.{culture.Trim()}.json");
    }

    /// <summary>
    /// Load one language. A tag the fixture does not ship falls back to the first it does, and
    /// says which it loaded — a window silently in another language is the defect the whole label
    /// rule exists over.
    /// </summary>
    /// <param name="culture">The language tag to ask for.</param>
    public static Strings Load(string culture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(culture);

        var asked = culture.Trim();
        var wanted = Cultures.Contains(asked, StringComparer.OrdinalIgnoreCase) ? asked : Cultures[0];
        var file = FileFor(wanted);

        using var document = JsonDocument.Parse(System.IO.File.ReadAllText(file));
        return new Strings(document.RootElement.Clone(), wanted, file);
    }

    /// <summary>What one dotted key says, or the key itself where the file does not carry it.</summary>
    /// <param name="key">The dotted key.</param>
    public string Says(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var element = root;
        foreach (var step in key.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(step, out element))
                return key;
        }

        return element.ValueKind == JsonValueKind.String ? element.GetString() ?? key : key;
    }

    /// <summary>The one line a run prints about which language it is in.</summary>
    public string Sentence() => string.Create(
        CultureInfo.InvariantCulture, $"showing {Culture} from {Path.GetFileName(File)}");
}

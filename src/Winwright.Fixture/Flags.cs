using System.Collections.ObjectModel;

namespace Winwright.Fixture;

/// <summary>Raised where the fixture was asked for something it does not know how to be.</summary>
public sealed class UnknownFlagException : ArgumentException
{
    /// <summary>Say what was asked for and what this knows.</summary>
    public UnknownFlagException(string message)
        : base(message)
    {
    }

    /// <summary>Unused. Present because an exception with no default shapes is awkward to catch.</summary>
    public UnknownFlagException()
        : base("the fixture was asked for a shape it does not have")
    {
    }

    /// <summary>Unused. Present for the same reason.</summary>
    public UnknownFlagException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>One shape the fixture can be asked to take.</summary>
/// <param name="Name">The flag, without its dashes.</param>
/// <param name="Takes">What it takes after an equals sign, or empty where it takes nothing.</param>
/// <param name="Provokes">The refusal or the reading it exists to make possible.</param>
/// <param name="Choices">
/// The values it accepts, where it accepts a fixed set. Empty means any text. A value outside the
/// set is refused the same way an unknown flag is: a shape nobody can spell is a shape nobody
/// takes, and the run that misspells one asserts nothing and says so nowhere.
/// </param>
public sealed record Flag(
    string Name, string Takes, string Provokes, IReadOnlyList<string>? Choices = null, bool Numeric = false)
{
    /// <summary>What it accepts, or nothing where it accepts any text.</summary>
    public IReadOnlyList<string> Accepts => Choices ?? [];

    /// <summary>The one line the catalogue prints.</summary>
    public override string ToString()
    {
        var takes = Takes.Length == 0
            ? ""
            : Accepts.Count == 0 ? $"=<{Takes}>" : $"={string.Join("|", Accepts)}";

        return $"--{Name}{takes}  {Provokes}";
    }
}

/// <summary>
/// What the fixture was asked to be.
/// <para>
/// This framework's value is concentrated in its refusals, and a refusal nobody can provoke is a
/// refusal that will quietly stop working. Each one gets a flag here, so the framework's own suite
/// can assert the red — which is the only thing that keeps a refusal real rather than remembered.
/// </para>
/// <para>
/// An unknown flag is refused rather than ignored, and that is the first refusal this fixture
/// makes about itself. A misspelt flag that silently does nothing produces a run where the shape
/// was never taken, the refusal never fired, and the case went green for the worst possible
/// reason: it asserted nothing and said so nowhere.
/// </para>
/// </summary>
public sealed record Flags
{
    private readonly IReadOnlyDictionary<string, string> given;

    private Flags(IReadOnlyDictionary<string, string> given)
    {
        this.given = given;
    }

    /// <summary>
    /// Every flag this fixture knows. The list lives here rather than in each shape's own file, so
    /// a shape added later without a row is a shape nobody can find.
    /// </summary>
    public static IReadOnlyList<Flag> Known { get; } = new ReadOnlyCollection<Flag>(
    [
        new Flag("title", "text", "a window titled something other than the default, for a case driving two at once"),
        new Flag(
            "pump",
            "host",
            "the same window under a dispatcher that runs and one that never does - the difference "
                + "no picture can see and the one that decides whether a keystroke arrives",
            ["dispatcher", "none"]),
        new Flag(
            "names",
            "",
            "a pane carrying the whole naming rule at once - nothing, a glyph, an echoed id, a "
                + "label that is a neighbouring element, and a button that must keep its own text"),
        new Flag(
            "absences",
            "",
            "a pane carrying the three kinds of absence at once - a collapsed pane, a closed popup "
                + "and an unopened submenu - which the tree reports differently and nothing else has together"),
        new Flag(
            "backdrop",
            "kind",
            "a window that opted into a system backdrop, which transmits what is behind it through "
                + "the glass and which no amount of z-order reasoning can answer for",
            Backdrop.Names),
        new Flag(
            "toast",
            "way",
            "a borderless top-level window with no caption, which the process object never names - "
                + "beside the main window, or as the only window this run has at all",
            Toast.Ways),
        new Flag(
            "loading",
            "milliseconds",
            "a page that is still computing for exactly this long, so the loading refusal is asserted "
                + "at a moment the run chose rather than on a machine that happened to be slow",
            Numeric: true),
        new Flag(
            "animate",
            "milliseconds",
            "an animation of a declared length whose states announce their own place, so a frame "
                + "sequence is checked against numbers rather than against pictures somebody opened",
            Numeric: true),
        new Flag(
            "render",
            "path",
            "render the fixed surface to a file and exit, showing no window at all - which is what "
                + "gives a byte-identical comparison something to be identical to"),
        new Flag(
            "resident",
            "",
            "a process that runs and shows nothing, which is the ordinary state of a tray "
                + "application and the one thing the other-instance refusal must never fire on"),
        new Flag(
            "store",
            "directory",
            "a settings store of the fixture's own, written from constants, which a run may break "
                + "without anybody's real settings being at risk"),
        new Flag(
            "mutate",
            "",
            "leave that store changed - the same number of bytes and a different machine, which is "
                + "the accident a comparison by size or by write time calls unchanged"),
        new Flag(
            "language",
            "tag",
            "a window labelled from one of the fixture's own string files, including the one key "
                + "whose value carries a placeholder and which an exact-name read can never match",
            Strings.Cultures),
    ]);

    /// <summary>Whether the fixture was asked for that shape.</summary>
    /// <param name="name">The flag, without its dashes.</param>
    public bool Has(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return given.ContainsKey(name.Trim());
    }

    /// <summary>What it was given after the equals sign, or null where the flag is absent.</summary>
    /// <param name="name">The flag, without its dashes.</param>
    public string? Value(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return given.TryGetValue(name.Trim(), out var value) ? value : null;
    }

    /// <summary>How many shapes were asked for.</summary>
    public int Count => given.Count;

    /// <summary>
    /// Read the command line.
    /// </summary>
    /// <param name="arguments">The arguments, as the process was given them.</param>
    /// <exception cref="UnknownFlagException">
    /// Where anything is not a flag this fixture knows. The message names the catalogue, because a
    /// refusal that does not say what would have worked costs a reader the source.
    /// </exception>
    public static Flags Read(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var read = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var argument in arguments)
        {
            var text = (argument ?? "").Trim();
            if (text.Length == 0)
                continue;

            if (!text.StartsWith("--", StringComparison.Ordinal))
                throw new UnknownFlagException($"'{text}' is not a flag: every argument begins with --.{Catalogue()}");

            var body = text[2..];
            var equals = body.IndexOf('=', StringComparison.Ordinal);
            var name = equals < 0 ? body : body[..equals];
            var value = equals < 0 ? "" : body[(equals + 1)..];

            var known = Known.FirstOrDefault(one => string.Equals(one.Name, name, StringComparison.Ordinal))
                ?? throw new UnknownFlagException($"--{name} is not a shape this fixture has.{Catalogue()}");

            if (known.Takes.Length > 0 && value.Length == 0)
                throw new UnknownFlagException($"--{name} takes a value: --{name}=<{known.Takes}>.{Catalogue()}");

            if (known.Takes.Length == 0 && equals >= 0)
                throw new UnknownFlagException($"--{name} takes nothing, and it was given '{value}'.{Catalogue()}");

            if (known.Accepts.Count > 0 && !known.Accepts.Contains(value, StringComparer.Ordinal))
            {
                throw new UnknownFlagException(
                    $"--{name} does not take '{value}': it takes {string.Join(" or ", known.Accepts)}.{Catalogue()}");
            }

            // A duration that is not a number would otherwise be taken as zero, and a page asked to
            // load for 'twoseconds' that loads for none is the shape nobody can provoke again.
            if (known.Numeric
                && (!int.TryParse(value, System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out var counted)
                    || counted < 0))
            {
                throw new UnknownFlagException(
                    $"--{name} takes a whole number of {known.Takes} and was given '{value}'.{Catalogue()}");
            }

            read[name] = value;
        }

        // A flag that does nothing without another is a flag that silently does nothing, which is
        // the same green as a misspelt one and just as hard to notice.
        if (read.ContainsKey("mutate") && !read.ContainsKey("store"))
            throw new UnknownFlagException($"--mutate has nothing to change without --store=<directory>.{Catalogue()}");

        return new Flags(new ReadOnlyDictionary<string, string>(read));
    }

    /// <summary>Every flag, one per line, as a person driving the fixture by hand reads them.</summary>
    public static string Catalogue() =>
        "\nThis fixture knows:\n" + string.Join("\n", Known.Select(one => "  " + one));
}

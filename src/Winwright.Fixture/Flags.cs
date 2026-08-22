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
/// <param name="Because">
/// The real defect this shape reproduces, and where it happened. Required: a fixture that grows
/// shapes nobody can justify becomes a second product to maintain, drifts from the things it
/// stands in for, and starts producing false confidence. One that can name no defect is removed,
/// and the removal is itself a reading about what this framework no longer has to defend against.
/// </param>
/// <param name="Choices">
/// The values it accepts, where it accepts a fixed set. Empty means any text. A value outside the
/// set is refused the same way an unknown flag is: a shape nobody can spell is a shape nobody
/// takes, and the run that misspells one asserts nothing and says so nowhere.
/// </param>
public sealed record Flag(
    string Name,
    string Takes,
    string Provokes,
    string Because,
    IReadOnlyList<string>? Choices = null,
    bool Numeric = false)
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

    /// <summary>The two lines a catalogue prints: what it does, and why it is here at all.</summary>
    public string Justified() => $"{this}\n      because {Because}";
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
        new Flag(
            "flags",
            "",
            "print this catalogue and stop, so what the fixture can do is askable without having to misspell something to be told",
            "a catalogue that lives only in source is one nobody consults, and claude-tray's preview catalogue already prints its whole table on an unknown name and exits non-zero"),
        new Flag(
            "title",
            "text",
            "a window titled something other than the default, for a case driving two at once",
            "the other-instance refusal was tested by remembering to leave a second window open, which is a test nobody runs the same way twice"),
        new Flag(
            "pump",
            "host",
            "the same window under a dispatcher that runs and one that never does - the difference no picture can see",
            "claude-tray shipped windows that took no keystrokes while every screenshot of them looked perfect",
            ["dispatcher", "none"]),
        new Flag(
            "names",
            "",
            "a pane carrying the whole naming rule at once - nothing, a glyph, an echoed id, a neighbouring label, and a button that keeps its text",
            "claude-tray shipped two controls carrying empty names while every neighbouring button read fine, because a control takes its name from its own content and both had none"),
        new Flag(
            "absences",
            "",
            "a pane carrying the three kinds of absence at once - a collapsed pane, a closed popup and an unopened submenu",
            "a control on a page that is not showing cannot be found by any id, which reads exactly like one that was renamed or removed"),
        new Flag(
            "backdrop",
            "kind",
            "a window that opted into a system backdrop, which transmits what is behind it through the glass",
            "z-order reasoning cannot answer for a backdrop, so every check that decides a copy's contents by walking the windows above it is wrong about that one window",
            Backdrop.Names),
        new Flag(
            "toast",
            "way",
            "a borderless top-level window with no caption, which the process object never names",
            "a toast existed here in exactly one product and only when its notification happened to fire, which is not a schedule a check can wait on",
            Toast.Ways),
        new Flag(
            "loading",
            "milliseconds",
            "a page that is still computing for exactly this long",
            "the loading refusal was discovered on a machine that happened to be slow, and reproducing it meant finding another one",
            Numeric: true),
        new Flag(
            "animate",
            "milliseconds",
            "an animation of a declared length whose states announce their own place",
            "a frame sequence was checked by opening the frames and looking, which is the thing this framework exists to avoid",
            Numeric: true),
        new Flag(
            "render",
            "path",
            "render the fixed surface to a file and exit, showing no window at all",
            "the byte-identical comparison had nothing to be identical to, because every surface available read a clock, a machine name or the desktop's theme"),
        new Flag(
            "resident",
            "",
            "a process that runs and shows nothing, which is the ordinary state of a tray application",
            "a resident tray showing nothing runs on every developer machine here, and a refusal firing on it would need an override everybody passes always"),
        new Flag(
            "store",
            "directory",
            "a settings store of the fixture's own, written from constants, which a run may break",
            "the fingerprint check protects the store of whoever is running it, so it could not be developed against a real product without risking somebody's settings"),
        new Flag(
            "mutate",
            "",
            "leave that store changed - the same number of bytes and a different machine",
            "a settings file rewritten to the same length is the accident the fingerprint exists for: a picker repointed from one profile to another of the same name"),
        new Flag(
            "language",
            "tag",
            "a window labelled from one of the fixture's own string files",
            "the label rule needs several languages to be developed at all, and one key whose value carries a placeholder that no exact read can ever match",
            Strings.Cultures),
        new Flag(
            "intrude",
            "left,top,width,height",
            "a topmost window over exactly that rectangle in physical pixels",
            "the region check is the most intricate piece of the capture stack and was exercised by moving a window by hand and hoping"),
        new Flag(
            "peerless",
            "",
            "a pane drawn with no automation peers at all, which a locator resolves against nothing",
            "the only surface with no accessibility tree was an installer page in another repository, behind a compiler that has to be installed first"),
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
        // Read at insertion, so a rectangle nobody can parse is a refusal before any window rather
        // than an intruder placed somewhere nobody asked.
        if (read.TryGetValue("intrude", out var rectangle))
            Intruder.Read(rectangle);

        if (read.ContainsKey("mutate") && !read.ContainsKey("store"))
            throw new UnknownFlagException($"--mutate has nothing to change without --store=<directory>.{Catalogue()}");

        return new Flags(new ReadOnlyDictionary<string, string>(read));
    }

    /// <summary>Every flag, as a person driving the fixture by hand reads them.</summary>
    /// <param name="justified">
    /// Whether to say why each shape exists as well as what it does. A refusal wants to be
    /// scannable and leaves this off; a catalogue somebody asked for wants to be complete.
    /// </param>
    public static string Catalogue(bool justified = false) =>
        "\nThis fixture knows:\n"
        + string.Join("\n", Known.Select(one => "  " + (justified ? one.Justified() : one.ToString())));
}

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
public sealed record Flag(string Name, string Takes, string Provokes, IReadOnlyList<string>? Choices = null)
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

            read[name] = value;
        }

        return new Flags(new ReadOnlyDictionary<string, string>(read));
    }

    /// <summary>Every flag, one per line, as a person driving the fixture by hand reads them.</summary>
    public static string Catalogue() =>
        "\nThis fixture knows:\n" + string.Join("\n", Known.Select(one => "  " + one));
}

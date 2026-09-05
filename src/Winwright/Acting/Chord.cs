using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Winwright.Acting;

/// <summary>
/// A key pressed with modifiers held, as a case spells it — <c>Ctrl+Shift+I</c>.
/// <para>
/// WW317. Found adopting this in an application whose window is deliberately almost empty: a title
/// bar, a terminal, and nothing else. It has no menu and no toolbar on purpose, so every command is
/// on a chord. <see cref="TraversalKey" /> is Tab, Shift+Tab and the arrows, which is the right
/// vocabulary for moving focus and the wrong one for invoking a command — and there was no
/// <c>with</c> that spelled a modifier plus a key, so those commands could not be reached at all.
/// </para>
/// <para>
/// What makes it more than one adopter's inconvenience: an application with no menu is the shape
/// this engine is best placed to test, because there is nothing to click and a screenshot shows an
/// empty window. The commands <em>are</em> the application, the keyboard is the only route to them,
/// and <c>click</c> needs a target that does not exist.
/// </para>
/// <para>
/// Parsed once, at the point the case is written, like a locator and a regular expression. A chord
/// that names no key, names a key twice, or names something that is not a key is wrong on every
/// machine, and discovering it on the run that was going to press it costs a launch to learn what
/// the file already said.
/// </para>
/// </summary>
public sealed record Chord
{
    private Chord(IReadOnlyList<string> modifiers, string key, string text)
    {
        Modifiers = modifiers;
        Key = key;
        Text = text;
    }

    /// <summary>The modifiers held while the key is pressed, in the order this type writes them.</summary>
    public IReadOnlyList<string> Modifiers { get; }

    /// <summary>The key pressed, by the name this vocabulary gives it.</summary>
    public string Key { get; }

    /// <summary>The chord as a case wrote it, which is what a trace records.</summary>
    public string Text { get; }

    /// <summary>
    /// The modifiers, in the order they are held and released.
    /// <para>
    /// A fixed order rather than the author's, so two spellings of one chord are one chord: a case
    /// writing <c>Shift+Ctrl+I</c> and another writing <c>Ctrl+Shift+I</c> mean the same keystroke,
    /// and a trace that rendered them differently would read as two different acts.
    /// </para>
    /// </summary>
    private static readonly (string Name, ushort Key)[] Held =
    [
        ("Ctrl", 0x11),
        ("Alt", 0x12),
        ("Shift", 0x10),
        ("Win", 0x5B),
    ];

    /// <summary>
    /// Every key a chord may name, beyond the letters and digits.
    /// <para>
    /// A closed list, because a name outside it is a typo rather than a key this engine has not met:
    /// there is no open set of key names to fall through to, and a chord that pressed nothing would
    /// be a step that ran and did nothing.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, ushort> Named = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tab"] = 0x09,
        ["Enter"] = 0x0D,
        ["Escape"] = 0x1B,
        ["Space"] = 0x20,
        ["Backspace"] = 0x08,
        ["Delete"] = 0x2E,
        ["Insert"] = 0x2D,
        ["Home"] = 0x24,
        ["End"] = 0x23,
        ["PageUp"] = 0x21,
        ["PageDown"] = 0x22,
        ["Left"] = 0x25,
        ["Up"] = 0x26,
        ["Right"] = 0x27,
        ["Down"] = 0x28,
    };

    /// <summary>What a refusal lists, so an author is shown the vocabulary rather than told a rule.</summary>
    public static string Spelled() =>
        $"a key with modifiers, like Ctrl+Shift+I — the modifiers are "
            + $"{string.Join(", ", Held.Select(one => one.Name))}, and the key is a letter, a digit, "
            + $"F1 to F24, or {string.Join(", ", Named.Keys)}";

    /// <summary>
    /// Parse one, or say why it is not a chord.
    /// <para>
    /// WW377, and the annotation WW364 put one verb over. The two outs say what the body already
    /// does, so a caller that answered on false reads the chord without a bang — and one that reads
    /// it anyway gets a warning where it used to get a habit.
    /// </para>
    /// <para>
    /// Both engine callers were already written as if this were here: <c>ActVerb.Refuses</c>
    /// interpolates the reason on the false branch, and <c>ActVerb.Chorded</c> hands the chord on as
    /// a <c>Chord?</c>. Both are right and neither is what the signature promised, so what this
    /// closes is the next caller — the one that has to name the chord rather than pass it along, and
    /// would have written the bang before anybody argued about the annotation.
    /// </para>
    /// </summary>
    /// <param name="text">The chord as a case wrote it.</param>
    /// <param name="chord">The chord, where it parsed.</param>
    /// <param name="because">Why it did not, where it did not.</param>
    public static bool TryParse(
        string? text, [NotNullWhen(true)] out Chord? chord, [NotNullWhen(false)] out string? because)
    {
        chord = null;
        because = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            because = "it names no key at all";
            return false;
        }

        var parts = text.Split('+', StringSplitOptions.TrimEntries);
        if (parts.Any(one => one.Length == 0))
        {
            because = $"'{text}' has an empty part, so a '+' names nothing on one side of it";
            return false;
        }

        var modifiers = new List<string>();
        for (var at = 0; at < parts.Length - 1; at++)
        {
            var found = Held.FirstOrDefault(one => string.Equals(one.Name, parts[at], StringComparison.OrdinalIgnoreCase));
            if (found.Name is null)
            {
                because = $"'{parts[at]}' is no modifier; they are {string.Join(", ", Held.Select(one => one.Name))}";
                return false;
            }

            // A modifier held twice is held once, so the second is a mistake rather than a stronger
            // press — and a case that meant a different modifier is one nobody would find by reading.
            if (modifiers.Contains(found.Name, StringComparer.Ordinal))
            {
                because = $"'{found.Name}' is named twice, and a modifier held twice is held once";
                return false;
            }

            modifiers.Add(found.Name);
        }

        var last = parts[^1];
        if (Virtual(last) is null)
        {
            because = $"'{last}' is no key this can press; it takes {Spelled()}";
            return false;
        }

        // Written back in this type's own order and never the author's, so one chord has one
        // spelling wherever it is reported.
        var ordered = Held.Where(one => modifiers.Contains(one.Name, StringComparer.Ordinal))
            .Select(one => one.Name)
            .ToList();

        var key = Named.Keys.FirstOrDefault(one => string.Equals(one, last, StringComparison.OrdinalIgnoreCase))
            ?? last.ToUpperInvariant();

        chord = new Chord(
            new ReadOnlyCollection<string>(ordered),
            key,
            string.Join("+", ordered.Append(key)));

        return true;
    }

    /// <summary>The virtual keys to hold, in order, or empty where this chord holds none.</summary>
    internal IReadOnlyList<ushort> Holding() =>
        Held.Where(one => Modifiers.Contains(one.Name, StringComparer.Ordinal))
            .Select(one => one.Key)
            .ToList();

    /// <summary>The virtual key this chord presses.</summary>
    /// <exception cref="InvalidOperationException">Where the key parsed and no longer maps, which cannot happen.</exception>
    internal ushort Pressing() =>
        Virtual(Key) ?? throw new InvalidOperationException($"'{Key}' parsed as a key and maps to none");

    /// <summary>The virtual key one name means, or null where it is no key.</summary>
    /// <param name="name">The key's name, as a case wrote it.</param>
    private static ushort? Virtual(string name)
    {
        if (Named.TryGetValue(name, out var known))
            return known;

        // A letter and a digit are their own virtual keys, which is the whole of why they need no
        // entry above: VK_A is 'A' and VK_0 is '0'.
        if (name.Length == 1 && char.IsAsciiLetterOrDigit(name[0]))
            return char.ToUpperInvariant(name[0]);

        // F1 to F24 are consecutive from VK_F1, so they are arithmetic rather than twenty-four rows.
        if (name.Length >= 2
            && (name[0] == 'F' || name[0] == 'f')
            && int.TryParse(name[1..], out var which)
            && which is >= 1 and <= 24)
        {
            return (ushort)(0x70 + which - 1);
        }

        return null;
    }

    /// <summary>The chord in this type's own spelling, which is what a trace and a refusal show.</summary>
    public override string ToString() => Text;
}

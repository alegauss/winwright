using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using Winwright.Windowing;

namespace Winwright.Capturing;

/// <summary>
/// Whether the application was showing an element at all, as it said.
/// <para>
/// WW130. A collapsed element lays out to nothing correctly, deliberately, and on every page that
/// hides anything — so without this the layout check fires on every hidden thing at once, and a
/// caption that wrapped at column zero reads exactly like a note the page is not showing.
/// </para>
/// </summary>
public enum Shown
{
    /// <summary>The application is showing it, so what it measures is what a person sees.</summary>
    Visible,

    /// <summary>It reserves its space and draws nothing.</summary>
    Hidden,

    /// <summary>It is not laid out at all, which is why it measures nothing.</summary>
    Collapsed,
}

/// <summary>One element the application under test said it drew, in physical pixels.</summary>
/// <param name="Depth">How deep under the root it sat.</param>
/// <param name="Kind">Its type.</param>
/// <param name="Name">Its name, empty where it had none.</param>
/// <param name="Bounds">Where it was, in the space a copy works in.</param>
/// <param name="Visibility">
/// Whether the application was showing it. Visible where the dump did not say, which is the older
/// format: a reader that assumed otherwise would quietly stop reporting a real fault.
/// </param>
public sealed record DrawnElement(
    int Depth, string Kind, string Name, WindowBounds Bounds, Shown Visibility = Shown.Visible)
{
    /// <summary>Whether it occupies anything at all.</summary>
    public bool Drawn => Bounds.Width > 0 && Bounds.Height > 0;

    /// <summary>Whether the application was showing it.</summary>
    public bool IsShown => Visibility == Shown.Visible;

    /// <summary>The one phrase a report names it by.</summary>
    public override string ToString() =>
        $"{Kind}{(Name.Length == 0 ? "" : $" '{Name}'")} {Bounds}"
        + (IsShown ? "" : $" ({Visibility.ToString().ToLowerInvariant()})");
}

/// <summary>What reading one dump found.</summary>
/// <param name="Elements">Every element, root first, in the order the walk reached them.</param>
/// <param name="Elided">How many the walk did not reach, as the dump reported it.</param>
/// <param name="Unreadable">How many lines did not parse.</param>
public sealed record ReadGeometry(IReadOnlyList<DrawnElement> Elements, int Elided, int Unreadable)
{
    /// <summary>The root, or null where the dump was empty.</summary>
    public DrawnElement? Root => Elements.Count == 0 ? null : Elements[0];

    /// <summary>Every element with that name, in the order the walk reached them.</summary>
    public IReadOnlyList<DrawnElement> Named(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var wanted = name.Trim();
        return new ReadOnlyCollection<DrawnElement>(
            Elements.Where(one => string.Equals(one.Name, wanted, StringComparison.Ordinal)).ToList());
    }

    /// <summary>What was read, with everything it could not read said out loud.</summary>
    public string Sentence()
    {
        if (Elements.Count == 0)
            return "the dump reports nothing drawn.";

        var parts = new List<string> { $"{Elements.Count} element(s) drawn under {Root}" };
        if (Elided > 0)
            parts.Add($"{Elided} not walked");
        if (Unreadable > 0)
            parts.Add($"{Unreadable} line(s) that did not parse");

        return string.Join("; ", parts) + ".";
    }

    /// <summary>The tree as a person reads it: one indented line per element.</summary>
    public IReadOnlyList<string> Render()
    {
        var lines = Elements.Select(one => new string(' ', one.Depth * 2) + one).ToList();
        if (Elided > 0)
            lines.Add($"... {Elided} not walked");

        return new ReadOnlyCollection<string>(lines);
    }
}

/// <summary>
/// The geometry dump the application under test wrote, read back.
/// <para>
/// An installer page, a custom-drawn control or an immediate-mode surface has no accessibility
/// tree to read, so the locator grammar has nothing to resolve against and the only check left is
/// reading the source — which misses the caption that wrapped, the page that rendered above a
/// screenful of blank space and the button nine pixels out of place.
/// </para>
/// <para>
/// The two halves cannot reference each other, so the format is the contract, the same way a
/// reported surface is: tabs, physical pixels, and a depth so the tree survives a flat file. A line
/// that does not parse is counted rather than thrown over, and what the walk did not reach is
/// carried through from the dump — a listing that stops without saying so reads as a tree that
/// ends there.
/// </para>
/// </summary>
public static class GeometryDump
{
    /// <summary>The variable the harness names the dump file in, which the in-app half reads.</summary>
    public const string PathVariable = "WINWRIGHT_GEOMETRY";

    /// <summary>The line the dump uses to say what its walk did not reach.</summary>
    public const string ElidedMarker = "#elided";

    /// <summary>Read a dump. A file that is not there reads as nothing drawn.</summary>
    /// <param name="path">The dump file.</param>
    public static ReadGeometry Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var elements = new List<DrawnElement>();
        var elided = 0;
        var unreadable = 0;

        var full = Path.GetFullPath(path.Trim());
        if (!File.Exists(full))
            return new ReadGeometry(new ReadOnlyCollection<DrawnElement>(elements), 0, 0);

        foreach (var line in Lines(full))
        {
            if (line.Length == 0)
                continue;

            if (line.StartsWith(ElidedMarker, StringComparison.Ordinal))
            {
                var fields = line.Split('\t');
                if (fields.Length == 2 && int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
                    elided += count;
                else
                    unreadable++;

                continue;
            }

            if (Parse(line) is DrawnElement element)
                elements.Add(element);
            else
                unreadable++;
        }

        return new ReadGeometry(new ReadOnlyCollection<DrawnElement>(elements), elided, unreadable);
    }

    /// <summary>The same, using the file the variable names.</summary>
    public static ReadGeometry Read()
    {
        var path = Environment.GetEnvironmentVariable(PathVariable);
        return string.IsNullOrWhiteSpace(path)
            ? new ReadGeometry(new ReadOnlyCollection<DrawnElement>([]), 0, 0)
            : Read(path);
    }

    /// <summary>One line of the dump, or null where it is not one.</summary>
    public static DrawnElement? Parse(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        // Seven fields or eight. A dump written before the visibility field existed is read as
        // showing everything, which keeps whatever it would have reported rather than quietly
        // dropping findings the moment the two halves are a version apart.
        var fields = line.TrimEnd('\r').Split('\t');
        if (fields.Length is not (7 or 8) || string.IsNullOrWhiteSpace(fields[1]))
            return null;

        var numbers = new int[5];
        if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out numbers[0]) || numbers[0] < 0)
            return null;

        for (var at = 0; at < 4; at++)
        {
            if (!int.TryParse(fields[at + 3], NumberStyles.Integer, CultureInfo.InvariantCulture, out numbers[at + 1]))
                return null;
        }

        return new DrawnElement(
            numbers[0],
            fields[1],
            fields[2],
            new WindowBounds(numbers[1], numbers[2], numbers[3], numbers[4]),
            fields.Length == 8 ? Visibility(fields[7]) : Shown.Visible);
    }

    /// <summary>
    /// What the dump called an element's visibility. A word this reader does not know is Visible:
    /// the two halves may be a version apart, and the direction that stays honest is the one that
    /// keeps reporting rather than the one that starts excusing.
    /// </summary>
    private static Shown Visibility(string written) => written.Trim() switch
    {
        "Collapsed" => Shown.Collapsed,
        "Hidden" => Shown.Hidden,
        _ => Shown.Visible,
    };

    private static IEnumerable<string> Lines(string full)
    {
        try
        {
            return File.ReadAllLines(full, Encoding.UTF8);
        }
        catch (Exception unreadable) when (unreadable is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }
}

using System.Collections.ObjectModel;
using System.Globalization;

using Winwright.Verdicts;
using Winwright.Windowing;

namespace Winwright.Capturing;

/// <summary>One surface the application under test said it drew, in physical pixels.</summary>
/// <param name="Name">What the application calls it.</param>
/// <param name="Bounds">Where it was, in the space a copy already works in.</param>
public sealed record ReportedSurface(string Name, WindowBounds Bounds)
{
    /// <summary>The one phrase a report names it by.</summary>
    public override string ToString() => $"{Name} {Bounds}";
}

/// <summary>What asking the report about one surface turned out to say.</summary>
public sealed record SurfaceReading
{
    internal SurfaceReading(string name, ReportedSurface? surface, string because)
    {
        Name = name;
        Surface = surface;
        Because = because;
    }

    /// <summary>The surface that was asked about.</summary>
    public string Name { get; }

    /// <summary>Where it was, or null where the application never reported it.</summary>
    public ReportedSurface? Surface { get; }

    /// <summary>Why there is nothing, where there is nothing. Empty otherwise.</summary>
    public string Because { get; }

    /// <summary>Whether the application said anything about it at all.</summary>
    public bool Reported => Surface is not null;

    /// <summary>The whole reading, said either way.</summary>
    public string Sentence() => Reported
        ? $"the application reported '{Name}' at {Surface!.Bounds}."
        : $"the application never reported '{Name}': {Because}.";

    /// <summary>
    /// The precondition a check declares on this. A surface the application never reported is an
    /// absence of the environment and not a failure of the window: nothing was observed, so the
    /// check that needed it did not run, and calling that a red blames the wrong repository.
    /// </summary>
    public Precondition AsPrecondition() => Reported
        ? Precondition.Met($"a reported surface named '{Name}'")
        : Precondition.Absent($"a reported surface named '{Name}'", Because);

    /// <summary>
    /// Whether a capture of <paramref name="copy"/> contains this surface. The hole is threaded
    /// through here rather than left to a caller: a check that asked about a surface nothing ever
    /// reported did not run, and reporting it as a red would blame the window for the harness.
    /// </summary>
    /// <param name="copy">The rectangle the capture read, in physical pixels.</param>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult Within(WindowBounds copy, string named) =>
        Reported
            ? Containment.Of(copy, Surface!).AsAssertion(named)
            : AssertionResult.Unchecked(named, AsPrecondition());
}

/// <summary>
/// The surfaces the application under test reported, read back.
/// <para>
/// The application knows which rectangle it painted; a harness in another process can only guess,
/// and a guess about a popup or a page that scrolled is a capture asserted against a rectangle
/// nobody drew. The in-app half writes a line per surface and this reads them, in physical pixels
/// both ways — a rectangle handed over in device-independent units is right at one hundred percent
/// and wrong at every scaling a developer actually runs.
/// </para>
/// <para>
/// The two halves cannot reference each other, so the format is the contract: a name, then four
/// numbers, tab-separated. A line that does not parse is skipped and counted rather than thrown
/// over — a report half-written by an application that was killed is still a report, and the
/// surfaces before the truncation are still true.
/// </para>
/// </summary>
public static class SurfaceReport
{
    /// <summary>The variable the harness names the report file in, which the in-app half reads.</summary>
    public const string PathVariable = "WINWRIGHT_SURFACES";

    /// <summary>
    /// Every surface in the report, by name. The <em>last</em> line for a name wins: a surface
    /// redrawn moves, and a reader taking the first would assert against where it used to be.
    /// </summary>
    /// <param name="path">The report file.</param>
    public static IReadOnlyDictionary<string, ReportedSurface> Read(string path) => Read(path, out _);

    /// <summary>
    /// The same, saying how many lines it could not read. Never silent: a report whose lines were
    /// dropped without a count reads as an application that drew less than it did.
    /// </summary>
    /// <param name="path">The report file.</param>
    /// <param name="unreadable">How many lines did not parse.</param>
    public static IReadOnlyDictionary<string, ReportedSurface> Read(string path, out int unreadable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var found = new Dictionary<string, ReportedSurface>(StringComparer.Ordinal);
        unreadable = 0;

        var full = Path.GetFullPath(path.Trim());
        if (!File.Exists(full))
            return new ReadOnlyDictionary<string, ReportedSurface>(found);

        foreach (var line in ReadLines(full))
        {
            if (line.Length == 0)
                continue;

            if (Parse(line) is ReportedSurface surface)
                found[surface.Name] = surface;
            else
                unreadable++;
        }

        return new ReadOnlyDictionary<string, ReportedSurface>(found);
    }

    /// <summary>Ask the report about one surface, by name.</summary>
    /// <param name="path">The report file.</param>
    /// <param name="name">The surface the application would have called it.</param>
    public static SurfaceReading Of(string path, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var wanted = name.Trim();
        var full = Path.GetFullPath(path.Trim());
        if (!File.Exists(full))
            return new SurfaceReading(wanted, null, $"there is no report at {full}");

        var read = Read(full, out var unreadable);
        if (read.TryGetValue(wanted, out var surface))
            return new SurfaceReading(wanted, surface, "");

        var dropped = unreadable == 0 ? "" : $", and {unreadable} line(s) in it did not parse";
        return read.Count == 0
            ? new SurfaceReading(wanted, null, $"{Path.GetFileName(full)} reports no surfaces at all{dropped}")
            : new SurfaceReading(
                wanted,
                null,
                $"{Path.GetFileName(full)} reports {string.Join(", ", read.Keys.Order(StringComparer.Ordinal))}{dropped}");
    }

    /// <summary>The same, using the file the variable names. A variable nobody set reports nothing.</summary>
    /// <param name="name">The surface to ask about.</param>
    public static SurfaceReading Of(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var path = Environment.GetEnvironmentVariable(PathVariable);
        return string.IsNullOrWhiteSpace(path)
            ? new SurfaceReading(name.Trim(), null, $"nothing set {PathVariable}, so no application was asked to report")
            : Of(path, name);
    }

    /// <summary>One line of the report, or null where it is not one.</summary>
    public static ReportedSurface? Parse(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var fields = line.TrimEnd('\r').Split('\t');
        if (fields.Length != 5 || string.IsNullOrWhiteSpace(fields[0]))
            return null;

        var numbers = new int[4];
        for (var at = 0; at < 4; at++)
        {
            if (!int.TryParse(fields[at + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out numbers[at]))
                return null;
        }

        return new ReportedSurface(
            fields[0], new WindowBounds(numbers[0], numbers[1], numbers[2], numbers[3]));
    }

    private static IEnumerable<string> ReadLines(string full)
    {
        try
        {
            return File.ReadAllLines(full);
        }
        catch (Exception unreadable) when (unreadable is IOException or UnauthorizedAccessException)
        {
            // The application is still writing it, which is ordinary rather than exceptional: the
            // caller gets no surfaces and the never-reported arm says so.
            return [];
        }
    }
}

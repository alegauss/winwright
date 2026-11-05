using System.Collections.ObjectModel;

using Winwright.Projects;

namespace Winwright.Concordance;

/// <summary>
/// The copies an invocation named, or the reason it named none that can be read.
/// <para>
/// Its own type rather than lines inside <see cref="Program"/>, because the whole risk of a gate
/// like this is in the arguments: a flag that was silently dropped leaves a reading of two copies
/// where three were meant, and two copies that agree is a green. What the arguments turned into is
/// therefore something a test can hold, and not something only a process exit code reports.
/// </para>
/// </summary>
/// <param name="Copies">The copies named, in the order they were named.</param>
/// <param name="Complaint">Why this invocation cannot be read at all. Empty where it can.</param>
/// <param name="Files">
/// The copies that are files in the tree, in the order they were named. WW239: the same line that
/// says which copies to compare is what says which copies to raise, so a copy the rewrite forgets is
/// a copy the check was never told about either.
/// </param>
/// <param name="Raising">The version <c>--raise</c> asked for, or empty where it was not given.</param>
public sealed record Roster(
    IReadOnlyList<EngineCopy> Copies,
    string Complaint = "",
    IReadOnlyList<WritableCopy>? Files = null,
    string Raising = "")
{
    /// <summary>The package id assumed where an invocation names none.</summary>
    public const string DefaultPackage = "Winwright";

    /// <summary>Whether the copies here can be read against each other.</summary>
    public bool Readable => Complaint.Length == 0;

    /// <summary>What every invocation is answered with where it could not be read.</summary>
    public static string Usage =>
        """
        usage: Winwright.Concordance [--package <id>] --declared <csproj|props> --packed <nupkg|dir>
                                     --pinned <consuming csproj> --manifest <plugin dir|plugin.json>
                                     --running

          --declared  the version a source tree declares, out of its first <Version>
          --packed    the version a build actually produced, out of the nuspec inside the package
          --pinned    the version a consuming project asks for, out of its PackageReference
          --manifest  the version the Claude Code plugin carries, which is what an adopter installed
          --running   the version of the engine assembly this process loaded
          --documented the version a document tells an adopter to take, out of a reference it shows
          --package   the package id the --packed and --pinned flags after it are about
                      (default: Winwright; it is read left to right, so it can change mid-line)
          --raise     rewrite every copy above that is a file in the tree to this version, and
                      report what was written rather than comparing anything

        At least two copies, because one copy agrees with itself on every machine there is.
        Exit: 0 they agree, 1 they do not or one cannot be pinned, 2 this line could not be read.
        """;

    /// <summary>
    /// Read a command line into the copies it names.
    /// <para>
    /// An argument this does not know is refused rather than skipped. A skipped flag is a copy that
    /// silently left the reading, and a reading with a copy missing still exits zero — which is the
    /// shape of the defect this whole project was started over, arriving through a typo.
    /// </para>
    /// </summary>
    /// <param name="args">The command line, without the executable.</param>
    public static Roster From(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var copies = new List<EngineCopy>();
        var files = new List<WritableCopy>();
        var package = DefaultPackage;
        var raising = "";

        for (var index = 0; index < args.Count; index++)
        {
            var flag = args[index]?.Trim() ?? "";
            if (flag == "--running")
            {
                copies.Add(Engine.Running());
                continue;
            }

            if (flag is not ("--declared" or "--packed" or "--pinned" or "--package" or "--manifest"
                or "--documented" or "--raise"))
            {
                return Refusing($"'{args[index]}' is not one of the flags this reads");
            }

            if (index + 1 >= args.Count || string.IsNullOrWhiteSpace(args[index + 1]))
                return Refusing($"{flag} was given nothing to read");

            var value = args[++index].Trim();
            switch (flag)
            {
                case "--package":
                    package = value;
                    break;
                case "--raise":
                    raising = value;
                    break;
                case "--declared":
                    copies.Add(Engine.Declared($"the tree ({Path.GetFileName(value)})", value));
                    files.Add(new WritableCopy($"the tree ({Path.GetFileName(value)})", value));
                    break;
                case "--documented":
                    // WW239. The copy an adopter acts on, and the one a release forgot: it is prose
                    // rather than a project, so nothing that reads XML was going to find it.
                    copies.Add(Engine.Documented(
                        $"{Path.GetFileName(value)}'s reference to {package}", value, package));
                    files.Add(new WritableCopy($"{Path.GetFileName(value)}'s reference to {package}", value));
                    break;
                case "--manifest":
                    // WW65. The copy this reading was written about first and could not take until
                    // the plugin existed: what an adopter actually installed.
                    copies.Add(Engine.Manifested("the plugin an adopter installs", value));
                    files.Add(new WritableCopy("the plugin an adopter installs", Engine.ManifestIn(value)));
                    break;
                case "--packed":
                    copies.Add(Engine.Packed($"the {package} package in {value}", value, package));
                    break;
                default:
                    copies.Add(Engine.Pinned(
                        $"{Path.GetFileName(value)}'s reference to {package}", value, package));
                    files.Add(new WritableCopy($"{Path.GetFileName(value)}'s reference to {package}", value));
                    break;
            }
        }

        // Refused here and not left to Agreement.Between's exception, so a line that names one copy
        // is answered with what is wrong with the line rather than with a stack trace.
        return copies.Count < 2
            ? Refusing(
                $"{copies.Count} copy was named, and agreement needs at least two: "
                    + "one copy agrees with itself on every machine there is")
            : new Roster(
                new ReadOnlyCollection<EngineCopy>(copies),
                "",
                new ReadOnlyCollection<WritableCopy>(files),
                raising);
    }

    /// <summary>Whether this invocation was asked to raise the copies rather than compare them.</summary>
    public bool Raises => Raising.Length > 0;

    /// <summary>
    /// Rewrite every copy that is a file in the tree, and say what happened to each.
    /// <para>
    /// WW239. A copy that cannot be read is refused before anything is written, and the refusal names
    /// it: raising four files and stopping on the fifth leaves a tree that agrees with nothing, which
    /// is worse than a tree nobody touched. Copies that are not files — what a build produced, what
    /// this process loaded — are named as not raised rather than silently passed over.
    /// </para>
    /// </summary>
    /// <returns>A line per copy, and whether every writable one was left at the asked-for version.</returns>
    /// <exception cref="InvalidOperationException">Where this roster could not be read.</exception>
    public (IReadOnlyList<string> Said, bool Raised) Raise()
    {
        if (!Readable)
            throw new InvalidOperationException(Complaint);

        var files = Files ?? [];
        var writable = Copies.Where(one => files.Any(file => file.Where == one.Where)).ToList();

        // Read first, write second. Every writable copy says what it currently is before any of them
        // is changed, or a file this cannot read is discovered halfway through the rewrite and the
        // tree is left agreeing with nothing.
        if (writable.Find(one => !one.Pins) is { } unreadable)
            return ([$"nothing was written: {unreadable}"], false);

        var said = new List<string>();
        foreach (var file in files)
            said.Add(file.Raise(writable.First(one => one.Where == file.Where).Version!, Raising));

        foreach (var copy in Copies.Where(one => !files.Any(file => file.Where == one.Where)))
            said.Add($"{copy.Where} is not a file in the tree and was not raised");

        return (new ReadOnlyCollection<string>(said), !said.Exists(WritableCopy.Refused));
    }

    /// <summary>The reading these copies make, which is refused unless this roster is readable.</summary>
    /// <exception cref="InvalidOperationException">Where this roster could not be read.</exception>
    public Agreement Read() => Readable
        ? Agreement.Between(Copies)
        : throw new InvalidOperationException(Complaint);

    private static Roster Refusing(string complaint) => new([], complaint);
}

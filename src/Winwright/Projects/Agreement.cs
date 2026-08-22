using System.Collections.ObjectModel;
using System.Reflection;
using System.Xml.Linq;

namespace Winwright.Projects;

/// <summary>Whether one copy of the engine could be pinned to a version at all.</summary>
public enum Pinning
{
    /// <summary>It names one version, and that version is a number.</summary>
    Pinned,

    /// <summary>It is there and says something no single version can be read out of.</summary>
    Unpinnable,

    /// <summary>Nothing says anything, which is not the same as saying nothing was found.</summary>
    Absent,
}

/// <summary>One copy of the engine, as whatever names it names it.</summary>
/// <param name="Where">What this copy is — "the assembly being called", "the source tree".</param>
/// <param name="Version">The version, where one could be read. Null otherwise.</param>
/// <param name="Pinning">Whether it could be pinned at all.</param>
/// <param name="Because">Why it could not, where it could not. Empty on a pinned copy.</param>
public sealed record EngineCopy(string Where, string? Version, Pinning Pinning, string Because = "")
{
    /// <summary>Whether this copy names one version.</summary>
    public bool Pins => Pinning == Pinning.Pinned;

    /// <summary>The one phrase a sentence names it by.</summary>
    public override string ToString() =>
        Pins ? $"{Where} is {Version}" : $"{Where} cannot be pinned: {Because}";
}

/// <summary>What reading every copy together turned out to say.</summary>
public enum Concord
{
    /// <summary>Every copy names a version and they are the same version.</summary>
    Agreed,

    /// <summary>Every copy names a version and at least two of them differ.</summary>
    Behind,

    /// <summary>At least one copy names no version that can be read, so nothing here is settled.</summary>
    Unpinnable,
}

/// <summary>
/// Whether the copies of the engine in play agree.
/// <para>
/// The version a plugin carries, the one continuous integration gates on and the one actually
/// being called are allowed to differ, and a stale copy does not fail — it agrees with a rule that
/// has moved. That is the whole hazard: nothing goes red, and the run is answering a question
/// somebody stopped asking. The same arrives here the moment a project vendors the engine instead
/// of taking the plugin.
/// </para>
/// <para>
/// One copy that cannot be pinned makes the whole reading unpinnable, even where every other copy
/// agrees. That is deliberate and it is the rule most likely to look wrong: <em>behind</em> is a
/// claim about a known distance, and no such claim can be made while one of the copies could be
/// anything. What is not known is reported as not known, never as agreement between the two that
/// happened to be readable.
/// </para>
/// </summary>
public sealed record Agreement
{
    private Agreement(IReadOnlyList<EngineCopy> copies, Concord verdict)
    {
        Copies = copies;
        Verdict = verdict;
    }

    /// <summary>Every copy that was read, in the order it was named.</summary>
    public IReadOnlyList<EngineCopy> Copies { get; }

    /// <summary>Which of the three this is.</summary>
    public Concord Verdict { get; }

    /// <summary>Whether every copy names the same version.</summary>
    public bool Agreed => Verdict == Concord.Agreed;

    /// <summary>
    /// What a gate exits with: zero on agreement and one on either of the others, because the
    /// difference between a gate and advice is the number it leaves behind.
    /// </summary>
    public int ExitCode => Agreed ? 0 : 1;

    /// <summary>
    /// The versions that were actually named, in order and spelled the way the copies spell them.
    /// Deduplicated on the comparison and not on the spelling, so <c>1.0</c> beside <c>1.0.0</c> is
    /// one entry — a report that prints the normal form instead would name a version no file holds.
    /// The build metadata the SDK appends is dropped here and nowhere else: it is already not part
    /// of the version, so a sentence carrying it would name one copy's decoration as the answer.
    /// </summary>
    public IReadOnlyList<string> Versions => new ReadOnlyCollection<string>(
        Copies.Where(one => one.Pins)
            .DistinctBy(one => Normalized(one.Version!), StringComparer.Ordinal)
            .Select(one => WithoutMetadata(one.Version!))
            .ToList());

    /// <summary>
    /// Read the copies together.
    /// </summary>
    /// <param name="copies">Every copy in play. At least two — see the remarks.</param>
    /// <exception cref="ArgumentException">Where fewer than two copies were named.</exception>
    /// <remarks>
    /// One copy is refused rather than reported as agreement. A copy agrees with itself on every
    /// machine there has ever been, so a reading of one would be a green that means the check was
    /// pointed at nothing — which is the shape of the defect this whole project started over.
    /// </remarks>
    public static Agreement Between(IReadOnlyList<EngineCopy> copies)
    {
        ArgumentNullException.ThrowIfNull(copies);
        if (copies.Count < 2)
            throw new ArgumentException(
                "agreement needs at least two copies: one copy agrees with itself on every machine there is",
                nameof(copies));

        var read = new ReadOnlyCollection<EngineCopy>([.. copies]);
        if (read.Any(one => !one.Pins))
            return new Agreement(read, Concord.Unpinnable);

        var versions = read.Select(one => Normalized(one.Version!)).Distinct(StringComparer.Ordinal).ToList();
        return new Agreement(read, versions.Count == 1 ? Concord.Agreed : Concord.Behind);
    }

    /// <summary>The same, for a caller that spells its copies inline.</summary>
    public static Agreement Between(params EngineCopy[] copies) =>
        Between((IReadOnlyList<EngineCopy>)(copies ?? []));

    /// <summary>The whole reading in the sentence a gate prints.</summary>
    public string Sentence()
    {
        if (Agreed)
            return $"all {Copies.Count} copies of the engine are {Versions[0]}.";

        if (Verdict == Concord.Behind)
        {
            return $"the copies of the engine disagree: {string.Join("; ", Copies.Select(one => one.ToString()))}."
                + " A stale copy does not fail — it agrees with a rule that has moved.";
        }

        var unreadable = Copies.Where(one => !one.Pins).ToList();
        var known = Versions.Count switch
        {
            0 => "nothing here names a version at all",
            1 => $"what could be read is {Versions[0]}",
            _ => $"and the copies that could be read already disagree: {string.Join(", ", Versions)}",
        };

        return $"{string.Join("; ", unreadable.Select(one => one.ToString()))} — {known}.";
    }

    /// <summary>One line per copy, for whoever prints a report rather than a sentence.</summary>
    public IReadOnlyList<string> Render() => new ReadOnlyCollection<string>(
        Copies.Select(one => one.Pins ? $"  {one.Version,-12} {one.Where}" : $"  {"(unpinnable)",-12} {one}").ToList());

    /// <summary>The report as a block of text, the sentence first.</summary>
    public override string ToString() => string.Join('\n', [Sentence(), .. Render()]);

    /// <summary>
    /// Two versions compared the way a build spells them. <c>1.0</c> and <c>1.0.0</c> are one
    /// version; the <c>+sha</c> the SDK appends to an informational version is not part of it, and
    /// comparing without dropping it makes every built copy disagree with the file that declared it.
    /// </summary>
    internal static string Normalized(string version)
    {
        var text = WithoutMetadata(version);
        return System.Version.TryParse(text, out var parsed)
            ? new System.Version(parsed.Major, parsed.Minor, Math.Max(0, parsed.Build), Math.Max(0, parsed.Revision)).ToString()
            : text;
    }

    /// <summary>The version without the <c>+sha</c> the SDK appends to an informational version.</summary>
    private static string WithoutMetadata(string version)
    {
        var text = version.Trim();
        var build = text.IndexOf('+', StringComparison.Ordinal);
        return build < 0 ? text : text[..build];
    }
}

/// <summary>Reading one copy of the engine out of whatever names it.</summary>
public static class Engine
{
    /// <summary>The copy that is actually running, read off the assembly this call is in.</summary>
    public static EngineCopy Running(string where = "the assembly being called")
    {
        var assembly = typeof(Engine).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var version = informational ?? assembly.GetName().Version?.ToString();

        return string.IsNullOrWhiteSpace(version)
            ? new EngineCopy(where, null, Pinning.Absent, "the assembly carries no version at all")
            : new EngineCopy(where, version.Trim(), Pinning.Pinned);
    }

    /// <summary>
    /// The version a source tree declares, out of the first <c>&lt;Version&gt;</c> a project or
    /// props file sets. A tree that declares none is <see cref="Pinning.Absent"/> and not zero:
    /// the build then invents 1.0.0, and an invented version agrees with nothing on purpose.
    /// </summary>
    /// <param name="where">What this copy is, as a report names it.</param>
    /// <param name="projectOrProps">The .csproj or Directory.Build.props file.</param>
    public static EngineCopy Declared(string where, string projectOrProps)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(where);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectOrProps);

        var full = Path.GetFullPath(projectOrProps.Trim());
        var document = Load(full);
        if (document is null)
            return new EngineCopy(where, null, Pinning.Absent, $"{Path.GetFileName(full)} is not there, or is not readable XML");

        var declared = document.Root?
            .Elements()
            .Where(element => element.Name.LocalName == "PropertyGroup")
            .SelectMany(group => group.Elements())
            .FirstOrDefault(element => element.Name.LocalName == "Version")?
            .Value;

        if (string.IsNullOrWhiteSpace(declared))
            return new EngineCopy(where, null, Pinning.Absent, $"{Path.GetFileName(full)} declares no <Version>");

        return Reading(where, declared.Trim(), Path.GetFileName(full));
    }

    /// <summary>
    /// The version a consuming project pins the engine to, out of its reference to it. A project
    /// reference is unpinnable by construction: what it is depends on the tree it was built from,
    /// which is the hazard a project vendoring the engine takes on in exchange for the convenience.
    /// </summary>
    /// <param name="where">What this copy is, as a report names it.</param>
    /// <param name="consumingProject">The .csproj that references the engine.</param>
    /// <param name="package">The package name, where it is taken as one.</param>
    public static EngineCopy Pinned(string where, string consumingProject, string package = "Winwright")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(where);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumingProject);
        ArgumentException.ThrowIfNullOrWhiteSpace(package);

        var full = Path.GetFullPath(consumingProject.Trim());
        var document = Load(full);
        if (document is null)
            return new EngineCopy(where, null, Pinning.Absent, $"{Path.GetFileName(full)} is not there, or is not readable XML");

        var items = document.Root?.Elements().Where(element => element.Name.LocalName == "ItemGroup").SelectMany(group => group.Elements())
            ?? [];

        foreach (var item in items)
        {
            var include = item.Attribute("Include")?.Value?.Trim() ?? "";
            if (item.Name.LocalName == "ProjectReference"
                && include.EndsWith($"{package}.csproj", StringComparison.OrdinalIgnoreCase))
            {
                return new EngineCopy(
                    where,
                    null,
                    Pinning.Unpinnable,
                    $"{Path.GetFileName(full)} references it by path ({include}), so what it is depends on the tree it was built from");
            }

            if (item.Name.LocalName != "PackageReference"
                || !string.Equals(include, package, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var version = item.Attribute("Version")?.Value
                ?? item.Elements().FirstOrDefault(child => child.Name.LocalName == "Version")?.Value;

            return string.IsNullOrWhiteSpace(version)
                ? new EngineCopy(where, null, Pinning.Unpinnable, $"{Path.GetFileName(full)} references {package} with no version")
                : Reading(where, version.Trim(), Path.GetFileName(full));
        }

        return new EngineCopy(where, null, Pinning.Absent, $"{Path.GetFileName(full)} does not reference {package}");
    }

    /// <summary>
    /// The version of a package that was actually built, read out of the nupkg itself.
    /// <para>
    /// WW122. Until the two halves were packable there was nothing here but a source tree and a
    /// path, and a path is unpinnable by construction — so an adopter could neither pin the in-app
    /// half nor take it at all. This is the copy that closes the loop: what a consuming project
    /// asked for by <see cref="Pinned"/>, against what the build actually produced.
    /// </para>
    /// <para>
    /// Read from the nuspec inside the archive rather than off the file name. The name carries the
    /// normalised version and the nuspec carries the declared one, and a check about what a build
    /// produced should read what the build wrote rather than what the file system was told.
    /// </para>
    /// </summary>
    /// <param name="where">What this copy is, as a report names it.</param>
    /// <param name="nupkgOrDirectory">A .nupkg, or a directory to find one in.</param>
    /// <param name="package">The package id, where a directory holds several.</param>
    public static EngineCopy Packed(string where, string nupkgOrDirectory, string package = "Winwright")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(where);
        ArgumentException.ThrowIfNullOrWhiteSpace(nupkgOrDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(package);

        var path = Path.GetFullPath(nupkgOrDirectory.Trim());
        if (Directory.Exists(path))
        {
            // Matched on the id inside each package and not on the file name, because a name is a
            // prefix: asking a folder for Winwright by name hands back Winwright.InApp as well.
            // Symbol packages carry the same id and are not the package either — an adopter
            // references the nupkg, and picking the snupkg answers about the wrong file.
            var found = Directory
                .EnumerateFiles(path, $"{package}.*.nupkg")
                .Where(one => !one.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase))
                .Where(one => InNuspec(one, package) is not null)
                .OrderBy(one => one, StringComparer.Ordinal)
                .ToList();

            if (found.Count == 0)
                return new EngineCopy(where, null, Pinning.Absent, $"no {package} package was built into {path}");

            if (found.Count > 1)
            {
                return new EngineCopy(
                    where,
                    null,
                    Pinning.Unpinnable,
                    $"{path} holds {found.Count} builds of {package} ({string.Join(", ", found.Select(Path.GetFileName))}), "
                        + "and which one an adopter would take is not answerable from here");
            }

            path = found[0];
        }

        if (!File.Exists(path))
            return new EngineCopy(where, null, Pinning.Absent, $"{Path.GetFileName(path)} is not there");

        var declared = InNuspec(path, package);
        return declared is null
            ? new EngineCopy(where, null, Pinning.Absent, $"{Path.GetFileName(path)} carries no readable nuspec version")
            : Reading(where, declared, Path.GetFileName(path));
    }

    /// <summary>The version the nuspec inside a package declares, or null where it cannot be read.</summary>
    private static string? InNuspec(string nupkg, string package)
    {
        try
        {
            using var archive = System.IO.Compression.ZipFile.OpenRead(nupkg);
            var nuspec = archive.Entries.FirstOrDefault(entry =>
                entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase)
                && !entry.FullName.Contains('/', StringComparison.Ordinal));

            if (nuspec is null)
                return null;

            using var reading = nuspec.Open();
            var document = XDocument.Load(reading);
            var metadata = document.Root?.Elements().FirstOrDefault(one => one.Name.LocalName == "metadata");
            var id = metadata?.Elements().FirstOrDefault(one => one.Name.LocalName == "id")?.Value?.Trim();

            // The id is checked rather than assumed: a file named for one package carrying another
            // is exactly the confusion this whole reading exists to refuse.
            if (!string.Equals(id, package, StringComparison.OrdinalIgnoreCase))
                return null;

            var version = metadata?.Elements().FirstOrDefault(one => one.Name.LocalName == "version")?.Value?.Trim();
            return string.IsNullOrWhiteSpace(version) ? null : version;
        }
        catch (Exception unreadable)
            when (unreadable is IOException or UnauthorizedAccessException
                or InvalidDataException or System.Xml.XmlException)
        {
            return null;
        }
    }

    private static EngineCopy Reading(string where, string version, string file)
    {
        // A range, a wildcard or a property nobody expanded is a version this cannot resolve, and
        // resolving it wrongly would be worse than saying so: the whole answer is about what is
        // actually going to run, and "[1.0,2.0)" is a promise that it will be one of several.
        var floating = version.Contains('*', StringComparison.Ordinal)
            || version.StartsWith('[') || version.StartsWith('(')
            || version.Contains("$(", StringComparison.Ordinal);

        return floating
            ? new EngineCopy(where, null, Pinning.Unpinnable, $"{file} names '{version}', which is a range rather than a version")
            : new EngineCopy(where, version, Pinning.Pinned);
    }

    private static XDocument? Load(string path)
    {
        try
        {
            return XDocument.Load(path);
        }
        catch (Exception unreadable)
            when (unreadable is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            return null;
        }
    }
}

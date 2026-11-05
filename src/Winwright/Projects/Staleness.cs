using Winwright.Verdicts;

namespace Winwright.Projects;

/// <summary>Whether the binary on disk is the one this source tree describes.</summary>
public enum StalenessState
{
    /// <summary>The binary is at least as new as the newest source file.</summary>
    Fresh,

    /// <summary>A source file is newer, so this run would be about the previous build.</summary>
    Stale,

    /// <summary>There is no binary at all, so there is nothing to be about.</summary>
    NotBuilt,
}

/// <summary>
/// The same wrong reading as driving an unnamed binary, arrived at by accident rather than by
/// flag: a build fails, the previous executable stays where it was, and the run reports on code
/// that is not in the tree. The binary's write time is compared against the newest source file.
/// <para>
/// What it produces is a <see cref="Precondition"/> and not a failure, deliberately. Everything
/// that ran did run and did pass — on a binary. What could not be evaluated is the claim the
/// caller actually came for, and that is a hole, which is the one word this project has for it.
/// </para>
/// </summary>
public sealed record Staleness
{
    /// <summary>The name every scenario refers to this condition by.</summary>
    public const string PreconditionName = "a binary built from this source";

    private Staleness(
        StalenessState state, string executable, DateTime? built, string? newestSource, DateTime? changed)
    {
        State = state;
        Executable = executable;
        Built = built;
        NewestSource = newestSource;
        Changed = changed;
    }

    /// <summary>Which of the three this is.</summary>
    public StalenessState State { get; }

    /// <summary>The binary that was compared, resolved.</summary>
    public string Executable { get; }

    /// <summary>When it was written, in UTC. Null where there is none.</summary>
    public DateTime? Built { get; }

    /// <summary>The newest source file found, or null where the tree holds none.</summary>
    public string? NewestSource { get; }

    /// <summary>When that file changed, in UTC.</summary>
    public DateTime? Changed { get; }

    /// <summary>Whether this run would be about the previous build.</summary>
    public bool IsStale => State == StalenessState.Stale;

    /// <summary>Compare what the project declared: its executable against its source root.</summary>
    /// <exception cref="DeclarationMissingException">Where either is undeclared.</exception>
    /// <exception cref="DirectoryNotFoundException">Where the declared source root is not there.</exception>
    public static Staleness Of(ProjectDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        return Of(declaration.Executable, declaration.SourceRoot, declaration.SourceIgnore);
    }

    /// <summary>The same comparison, spelled out.</summary>
    /// <exception cref="DirectoryNotFoundException">Where <paramref name="sourceRoot"/> is not there.</exception>
    public static Staleness Of(string executable, string sourceRoot, IReadOnlyList<string>? ignore = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRoot);

        if (!Directory.Exists(sourceRoot))
            throw new DirectoryNotFoundException(
                $"the source root {sourceRoot} is not there, so nothing says whether {executable} is stale");

        var (newest, changed) = Newest(sourceRoot, ignore ?? ProjectDeclaration.DefaultSourceIgnore);

        if (!File.Exists(executable))
            return new Staleness(StalenessState.NotBuilt, executable, null, newest, changed);

        var built = File.GetLastWriteTimeUtc(executable);
        var state = changed is not null && built < changed ? StalenessState.Stale : StalenessState.Fresh;
        return new Staleness(state, executable, built, newest, changed);
    }

    /// <summary>
    /// This reading as the precondition a scenario declares a requirement on. Met where the binary
    /// is the one the tree describes; absent, with the two timestamps, where it is not.
    /// </summary>
    public Precondition AsPrecondition() => State switch
    {
        StalenessState.Fresh => Precondition.Met(PreconditionName),
        StalenessState.NotBuilt => Precondition.Absent(PreconditionName, $"there is no binary at {Executable}"),
        _ => Precondition.Absent(
            PreconditionName,
            $"{Path.GetFileName(Executable)} was built {Stamp(Built)} and {NewestSource} changed {Stamp(Changed)}"),
    };

    /// <summary>
    /// Which binary this run drove, and when it was built. Printed whatever the reading is,
    /// because a run that does not say which binary it drove is one nobody can check afterwards.
    /// </summary>
    public string Sentence() => State == StalenessState.NotBuilt
        ? $"drove nothing: there is no binary at {Executable}."
        : $"drove {Executable}, built {Stamp(Built)}{(IsStale ? $", older than {NewestSource}" : "")}.";

    private static (string? Path, DateTime? Changed) Newest(string sourceRoot, IReadOnlyList<string> ignore)
    {
        var skipped = new HashSet<string>(ignore, StringComparer.OrdinalIgnoreCase);
        string? newest = null;
        DateTime? changed = null;

        var walking = new Stack<DirectoryInfo>();
        walking.Push(new DirectoryInfo(sourceRoot));
        while (walking.Count > 0)
        {
            var directory = walking.Pop();
            foreach (var child in directory.EnumerateDirectories())
                if (!skipped.Contains(child.Name))
                    walking.Push(child);

            foreach (var file in directory.EnumerateFiles())
                if (changed is null || file.LastWriteTimeUtc > changed)
                {
                    newest = file.FullName;
                    changed = file.LastWriteTimeUtc;
                }
        }

        return (newest, changed);
    }

    private static string Stamp(DateTime? at) => at?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? "never";
}

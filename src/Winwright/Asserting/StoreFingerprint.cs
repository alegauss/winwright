using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

using Winwright.Verdicts;

namespace Winwright.Asserting;

/// <summary>Raised where a run changed something it was asked to leave alone.</summary>
public sealed class StoreTouchedException : InvalidOperationException
{
    /// <summary>Say what moved.</summary>
    public StoreTouchedException(string message)
        : base(message)
    {
    }

    /// <summary>Unused. Present because an exception with no default shapes is awkward to catch.</summary>
    public StoreTouchedException()
        : base("the run did not leave the machine as it found it")
    {
    }

    /// <summary>Unused. Present for the same reason.</summary>
    public StoreTouchedException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>What moved between two readings of the same store.</summary>
/// <param name="Appeared">Things that were not there before and are now.</param>
/// <param name="Gone">Things that were there before and are not now.</param>
/// <param name="Changed">Things that are there both times and read differently.</param>
public sealed record StoreChange(
    IReadOnlyList<string> Appeared, IReadOnlyList<string> Gone, IReadOnlyList<string> Changed)
{
    /// <summary>Whether the run left the machine as it found it.</summary>
    public bool Untouched => Appeared.Count == 0 && Gone.Count == 0 && Changed.Count == 0;

    /// <summary>How many things moved.</summary>
    public int Moved => Appeared.Count + Gone.Count + Changed.Count;

    /// <summary>
    /// What moved, named. A count alone would say a run dirtied the machine without saying which
    /// file to go and look at, and the whole value of this check is that it points somewhere.
    /// </summary>
    public string Sentence()
    {
        if (Untouched)
            return "the run left the machine as it found it.";

        var parts = new List<string>();
        if (Changed.Count > 0)
            parts.Add($"{Listed(Changed)} {Was(Changed)} rewritten");

        if (Appeared.Count > 0)
            parts.Add($"{Listed(Appeared)} {Was(Appeared)} created");

        if (Gone.Count > 0)
            parts.Add($"{Listed(Gone)} {Was(Gone)} removed");

        return $"the run changed the machine of whoever ran it: {string.Join("; ", parts)}.";
    }

    /// <summary>The result a verdict counts, carrying that sentence as its detail.</summary>
    public AssertionResult AsAssertion(string named = "the run leaves the machine as it found it") =>
        Untouched ? AssertionResult.Pass(named, Sentence()) : AssertionResult.Fail(named, Sentence());

    private static string Listed(IReadOnlyList<string> what) => string.Join(", ", what);

    private static string Was(IReadOnlyList<string> what) => what.Count == 1 ? "was" : "were";
}

/// <summary>
/// The state of a store, read closely enough that a change to it cannot hide.
/// <para>
/// A harness that drives a real picker, a real setting or a real environment variable can change
/// the machine of whoever ran it, and the change outlives the run. Fingerprinting before and
/// comparing after is the strongest form that promise can take: it is not a claim that the code
/// looks careful, it is a reading of what is actually on disk on both sides of the case.
/// </para>
/// <para>
/// Content is hashed rather than sized or dated. A settings file rewritten with the same length
/// is the exact shape of the accident this exists to catch — a picker repointed from one profile
/// to another of the same name — and a timestamp comparison would call two different files equal
/// whenever a tool preserved the write time.
/// </para>
/// </summary>
public sealed record StoreFingerprint
{
    private StoreFingerprint(IReadOnlyDictionary<string, string?> entries, IReadOnlyList<string> watched)
    {
        Entries = entries;
        Watched = watched;
    }

    /// <summary>Every thing that was read, against its digest — null where it was not there.</summary>
    public IReadOnlyDictionary<string, string?> Entries { get; }

    /// <summary>What was asked about, as it was asked about.</summary>
    public IReadOnlyList<string> Watched { get; }

    /// <summary>How many things were read, absent ones included.</summary>
    public int Count => Entries.Count;

    /// <summary>Read one or more files and directories now.</summary>
    /// <param name="paths">Files or directories. One that is not there is recorded as absent.</param>
    /// <exception cref="ArgumentException">Where nothing was named. A fingerprint of nothing is met by anything.</exception>
    public static StoreFingerprint Of(params string[] paths) => Of(paths, []);

    /// <summary>
    /// Read files, directories and environment variables now.
    /// </summary>
    /// <param name="paths">Files or directories. One that is not there is recorded as absent.</param>
    /// <param name="environment">Environment variable names, read from this process.</param>
    /// <exception cref="ArgumentException">Where nothing was named at all.</exception>
    public static StoreFingerprint Of(IReadOnlyList<string> paths, IReadOnlyList<string> environment)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(environment);

        // A fingerprint of nothing compares equal to a fingerprint of nothing, so a run watching
        // nothing would report that it left the machine alone. That is the green this file exists
        // to make impossible, and it is not allowed in through the front door either.
        if (paths.Count == 0 && environment.Count == 0)
            throw new ArgumentException(
                "a fingerprint of nothing is met by any machine at all — name what the run must not change",
                nameof(paths));

        var entries = new SortedDictionary<string, string?>(StringComparer.Ordinal);
        var watched = new List<string>();

        foreach (var path in paths)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(paths));
            var full = Path.GetFullPath(path.Trim());
            watched.Add(full);

            if (Directory.Exists(full))
            {
                foreach (var file in Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories))
                    entries[file] = Digest(file);

                continue;
            }

            entries[full] = File.Exists(full) ? Digest(full) : null;
        }

        foreach (var name in environment)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(environment));
            var key = $"%{name.Trim()}%";
            watched.Add(key);

            var value = Environment.GetEnvironmentVariable(name.Trim());
            entries[key] = value is null ? null : Hash(Encoding.UTF8.GetBytes(value));
        }

        return new StoreFingerprint(
            new ReadOnlyDictionary<string, string?>(entries), new ReadOnlyCollection<string>(watched));
    }

    /// <summary>What moved between this reading and <paramref name="after"/>.</summary>
    public StoreChange Against(StoreFingerprint after)
    {
        ArgumentNullException.ThrowIfNull(after);

        var appeared = new List<string>();
        var gone = new List<string>();
        var changed = new List<string>();

        foreach (var key in Entries.Keys.Union(after.Entries.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            var before = Entries.GetValueOrDefault(key);
            var now = after.Entries.GetValueOrDefault(key);

            if (before is null && now is not null)
                appeared.Add(key);
            else if (before is not null && now is null)
                gone.Add(key);
            else if (!string.Equals(before, now, StringComparison.Ordinal))
                changed.Add(key);
        }

        return new StoreChange(
            new ReadOnlyCollection<string>(appeared),
            new ReadOnlyCollection<string>(gone),
            new ReadOnlyCollection<string>(changed));
    }

    private static string Digest(string file)
    {
        try
        {
            using var reading = File.OpenRead(file);
            return Convert.ToHexStringLower(SHA256.HashData(reading));
        }
        catch (Exception unreadable) when (unreadable is IOException or UnauthorizedAccessException)
        {
            // Recorded rather than thrown, and recorded as something that is not a digest: a file
            // this run cannot read is still a file whose disappearance would be worth reporting.
            return $"unreadable: {unreadable.GetType().Name}";
        }
    }

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}

/// <summary>
/// Running a case with the store read on both sides of it.
/// <para>
/// The comparison wraps the case rather than living in a <c>Dispose</c>, and that is deliberate:
/// a disposer that threw would replace whatever the case itself threw, so the run would report a
/// dirty machine and lose the failure that caused it. Here an exception from the case propagates
/// untouched and no comparison is made — there is nothing to say about tidiness when the thing
/// being measured did not finish.
/// </para>
/// </summary>
public static class Untouched
{
    /// <summary>Read the store, run the case, read it again, and answer what moved.</summary>
    /// <param name="paths">Files or directories the case must not change.</param>
    /// <param name="run">The case.</param>
    public static StoreChange Around(IReadOnlyList<string> paths, Action run) => Around(paths, [], run);

    /// <summary>The same, watching environment variables too.</summary>
    /// <param name="paths">Files or directories the case must not change.</param>
    /// <param name="environment">Environment variable names the case must not change.</param>
    /// <param name="run">The case.</param>
    public static StoreChange Around(IReadOnlyList<string> paths, IReadOnlyList<string> environment, Action run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var before = StoreFingerprint.Of(paths, environment);
        run();
        return before.Against(StoreFingerprint.Of(paths, environment));
    }

    /// <summary>
    /// The same, refusing where anything moved. Reach for this around the case most likely to
    /// break the promise, and for <see cref="Around(IReadOnlyList{string}, Action)"/> around one
    /// whose changes are expected and worth reporting rather than refusing.
    /// </summary>
    /// <exception cref="StoreTouchedException">Where the case changed anything it was watching.</exception>
    public static void Insist(IReadOnlyList<string> paths, Action run)
    {
        var change = Around(paths, run);
        if (!change.Untouched)
            throw new StoreTouchedException(change.Sentence());
    }
}

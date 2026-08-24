using System.Collections.ObjectModel;

using Xunit;

namespace Winwright.Tests;

/// <summary>What a sleep is doing, where one is not a wait.</summary>
internal enum Sleeping
{
    /// <summary>The interval between looks. This is what a deadline is made of.</summary>
    Machinery,

    /// <summary>The interval is the resolution of a measurement, so a deadline would delete it.</summary>
    Resolution,

    /// <summary>The sleep is the thing being reproduced, not a way of waiting for it.</summary>
    UnderTest,

    /// <summary>
    /// A wait, and it stays one. There is nothing observable to wait on, and a condition that goes
    /// true too early is worse than the sleep it replaced because it reads as a wait that was
    /// proved. Counted here, and never silently.
    /// </summary>
    StillAWait,
}

/// <summary>
/// One file that sleeps, and why its sleeping is not a scenario waiting.
/// </summary>
/// <param name="File">The source file, by name.</param>
/// <param name="Sleeps">How many times it calls it.</param>
/// <param name="Kind">What the sleeping is doing.</param>
/// <param name="Because">Why it is that rather than a wait.</param>
internal sealed record Sleep(string File, int Sleeps, Sleeping Kind, string Because)
{
    public override string ToString() => $"{File,-22} {Sleeps}x {Kind,-11} {Because}";
}

/// <summary>
/// WW184. Block C's second criterion says no scenario carries a sleep: every wait is a deadline on
/// a condition, and how long it took is in the trace for whoever wants to tune it. Nothing checked
/// it, and seven files call <c>Thread.Sleep</c>.
/// <para>
/// Several are certainly right, and their being right is the point. <c>Attempt</c> sleeps between
/// polls, which is the deadline machinery itself. <c>FrameRun</c> paces frames, and WW143 argued
/// that case in writing: the interval is the resolution of the measurement, so turning it into a
/// deadline would delete the observation. The fixture sleeps because a thread parked without
/// pumping is the defect <c>--pump=none</c> exists to reproduce.
/// </para>
/// <para>
/// One is a wait and stays one, with the measurement written beside it. That entry is the reason
/// this is a catalogue rather than a ban: a rule that admitted no exceptions would be answered by
/// somebody spelling the sleep differently, and then nothing would know about it at all.
/// </para>
/// <para>
/// The call and never the words. <c>Thread.Sleep(</c> with its bracket, because two files discuss
/// sleeping in prose — this one and <c>Waits</c> — and a scan that counted those would report a
/// criterion broken by a comment explaining why it is not.
/// </para>
/// </summary>
internal static class Sleeps
{
    /// <summary>The call this is about, matched in the sources exactly as it is written.</summary>
    internal const string Calling = "Thread.Sleep(";

    internal static IReadOnlyList<Sleep> Known { get; } = new ReadOnlyCollection<Sleep>(
    [
        // --- the engine ---------------------------------------------------------------------------
        new("Attempt.cs", 2, Sleeping.Machinery,
            "the interval between one look and the next, in both the sighting deadline and the "
                + "condition one — a deadline that did not sleep would be a spin"),
        new("Expectation.cs", 1, Sleeping.Machinery,
            "the same interval, in the poll an expectation takes while it watches a subject"),
        new("FrameRun.cs", 1, Sleeping.Resolution,
            "frames are paced, and WW143 wrote the argument down: the interval is the resolution of "
                + "the measurement, and the last sixteen milliseconds are spun because the scheduler "
                + "overshoots by more than a third of an interval at twenty-five frames a second"),
        new("Program.cs", 1, Sleeping.UnderTest,
            "the fixture parks a thread without pumping, which is the whole of what --pump=none "
                + "reproduces: a single-threaded apartment that blocks any other way keeps answering "
                + "messages and is not dead at all"),

        // --- the suite ----------------------------------------------------------------------------
        new("FixtureTests.cs", 1, Sleeping.Resolution,
            "the sampler that watches an animation, where the interval is again the resolution of "
                + "what is being measured rather than a wait for it"),
        new("FrameRunTests.cs", 2, Sleeping.UnderTest,
            "a writer made deliberately slow so the capture falls behind — the sleeping is the "
                + "condition under test and waiting for it would be waiting for the case's own hand"),
        new("TraversalTests.cs", 1, Sleeping.StillAWait,
            "there is nothing out here to observe: the state a traversal settles into is the one "
                + "after the change, and a condition true too early reads as a wait that was proved. "
                + "It stays a sleep, with the measurement, until there is something to wait on"),
    ]);

    /// <summary>The ones that are still waits, counted rather than left off.</summary>
    internal static IReadOnlyList<Sleep> Waiting() =>
        new ReadOnlyCollection<Sleep>(Known.Where(one => one.Kind == Sleeping.StillAWait).ToList());

    /// <summary>Every file that calls it, and how many times, read out of the sources.</summary>
    internal static IReadOnlyList<Sleep> Found() => scanned.Value;

    private static readonly Lazy<IReadOnlyList<Sleep>> scanned = new(Scan);

    private static IReadOnlyList<Sleep> Scan()
    {
        var found = new List<Sleep>();
        foreach (var file in Trees().SelectMany(Sources))
        {
            // This file names the call it is looking for, so counting itself would count the naming.
            if (Path.GetFileName(file) == $"{nameof(Sleeps)}.cs")
                continue;

            var text = File.ReadAllText(file);
            var calls = Occurrences(text, Calling);
            if (calls > 0)
                found.Add(new Sleep(Path.GetFileName(file), calls, Sleeping.Machinery, ""));
        }

        return found.OrderBy(one => one.File, StringComparer.Ordinal).ToList();
    }

    /// <summary>The sources under a tree, and never what a build left beside them.</summary>
    private static IEnumerable<string> Sources(string tree) =>
        Directory
            .EnumerateFiles(tree, "*.cs", SearchOption.AllDirectories)
            .Where(one => !one.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(one => !one.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static int Occurrences(string text, string what)
    {
        var count = 0;
        var at = text.IndexOf(what, StringComparison.Ordinal);
        while (at >= 0)
        {
            count++;
            at = text.IndexOf(what, at + what.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private static IReadOnlyList<string> Trees()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Winwright.slnx")))
            directory = directory.Parent;

        Assert.NotNull(directory);
        return
        [
            Path.Combine(directory.FullName, "src"),
            Path.Combine(directory.FullName, "tests"),
        ];
    }
}

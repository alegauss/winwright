using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

namespace Winwright.Tests;

/// <summary>Why a refusal needs no shape from the fixture, where it needs none.</summary>
internal enum Without
{
    /// <summary>It is about a string, a file or an argument, so no window has to exist for it.</summary>
    NothingDrawn,

    /// <summary>It needs a shape the fixture cannot take. The bucket that must not grow.</summary>
    NotYet,
}

/// <summary>
/// One refusal, against the flag that provokes it or the reason none can.
/// </summary>
/// <param name="Refusal">The exception type, by name.</param>
/// <param name="Flag">The fixture flag that reaches it, or empty.</param>
/// <param name="Why">Where no flag reaches it, which of the two reasons that is.</param>
/// <param name="Because">The sentence a reader needs, in either case.</param>
internal sealed record Provoked(string Refusal, string Flag, Without? Why, string Because)
{
    /// <summary>Whether driving the fixture can reach it at all.</summary>
    public bool ThroughTheFixture => Flag.Length > 0;

    /// <summary>The one line the pairing prints.</summary>
    public override string ToString() => ThroughTheFixture
        ? $"{Refusal,-28} --{Flag}: {Because}"
        : $"{Refusal,-28} (no flag, {Why.ToString()!.ToLowerInvariant()}): {Because}";
}

/// <summary>
/// Every refusal this framework names, paired with what provokes it.
/// <para>
/// WW132. The framework names nineteen refusals and four of them are reached by driving the
/// fixture. The rest are asserted against hand-built windows, against arguments passed in a test,
/// or not at all — and the catalogue and the exception types were two lists nobody compared, so a
/// refusal added later started unprovokable and stayed that way.
/// </para>
/// <para>
/// Some need no shape and saying so is the point: a locator that does not parse is a string, and no
/// window has to exist for it. Others need one the fixture cannot take, and those are named as such
/// rather than left off — this framework's value is concentrated in its refusals, and a refusal
/// nobody can provoke is one that will quietly stop working.
/// </para>
/// <para>
/// The pairing is checked rather than kept: the assemblies are read for what they name, and an
/// entry here that no longer matches a type, or a type with no entry, is a red.
/// </para>
/// </summary>
internal static class Provocation
{
    /// <summary>
    /// The two halves whose refusals this pairing is about. The fixture is not among them and
    /// cannot be: the suite references it without its assembly on purpose, because an application
    /// under test is launched from its own output rather than read from beside the harness. Its own
    /// refusal about an unknown flag is provoked by running it, which is where it belongs.
    /// </summary>
    public static IReadOnlyList<Assembly> Assemblies { get; } = new ReadOnlyCollection<Assembly>(
    [
        typeof(Winwright.Locating.Subject).Assembly,
        typeof(Winwright.InApp.Coordinates).Assembly,
    ]);

    /// <summary>Every refusal, paired.</summary>
    public static IReadOnlyList<Provoked> Known { get; } = new ReadOnlyCollection<Provoked>(
    [
        new("AnotherInstanceException", "title", null,
            "a second window of the same application under another title is what the instance check counts"),
        new("UnusableLabelException", "language", null,
            "the fixture's own strings carry one key whose value is a placeholder no exact read can match"),
        new("StoreTouchedException", "mutate", null,
            "a settings file rewritten to the same length, which is the accident the fingerprint exists for"),
        new("ApartmentTimeoutException", "pump", null,
            "a window whose dispatcher never runs, so work handed to it is never taken up"),

        new("LocatorSyntaxException", "", Without.NothingDrawn,
            "a locator that does not parse is a string, and no window has to exist for one"),
        new("AmbiguousLocatorException", "", Without.NothingDrawn,
            "two elements matching one step is a tree, and a hand-built window is a tree"),
        new("AttachFailedException", "", Without.NothingDrawn,
            "attaching to a process that is not there needs no process, which is the whole point of it"),
        new("DeclarationMissingException", "", Without.NothingDrawn,
            "a project that declares nothing is a file, and the refusal is about the file"),
        new("UnreadableTraceException", "", Without.NothingDrawn,
            "a trace that is not a trace is a file, and reading one back is what this is about"),
        new("UnearnedGreenException", "", Without.NothingDrawn,
            "a verdict assembled wrongly is arithmetic on results, with no window anywhere in it"),
        new("UnderivableSetException", "", Without.NothingDrawn,
            "a strings file declaring nothing under a key is a file, and the refusal is read off it"),
        new("NotActionableException", "", Without.NothingDrawn,
            "an element disabled, offscreen or without the pattern is a control, and a hand-built window has controls"),
        new("DestructiveEntryException", "", Without.NothingDrawn,
            "a declared entry against a control by that name, both of which a test builds"),
        new("ScenarioRefusedException", "", Without.NothingDrawn,
            "a declared act whose control carries no such pattern, which is a preflight over a tree"),
        new("ThreadBoundException", "", Without.NothingDrawn,
            "a brush made on one thread and read from another, which needs two threads and no window"),

        new("WrongCaptureException", "", Without.NotYet,
            "a receipt about a window other than the one captured; nothing here hands over the wrong one"),
        new("BlankPictureException", "", Without.NotYet,
            "a picture nothing drew; the fixture always draws something, so it cannot produce one"),
        new("NoBackgroundException", "", Without.NotYet,
            "a capture of an element with no background declared anywhere above it"),
        new("UnrenderableException", "", Without.NotYet,
            "a render of a tree that lays out to nothing at all"),
    ]);

    /// <summary>One flag's name out of the line the catalogue prints for it.</summary>
    private static string FlagName(string line)
    {
        var body = line[2..];
        var ends = body.IndexOfAny([' ', '=']);
        return ends < 0 ? body : body[..ends];
    }

    /// <summary>The refusals the assemblies actually name, by type name.</summary>
    public static IReadOnlyList<string> Named() => new ReadOnlyCollection<string>(
        Assemblies
            .SelectMany(one => one.GetExportedTypes())
            .Where(one => typeof(Exception).IsAssignableFrom(one) && !one.IsAbstract)
            .Select(one => one.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(one => one, StringComparer.Ordinal)
            .ToList());

    /// <summary>The ones the fixture can reach.</summary>
    public static IReadOnlyList<Provoked> Reachable() => new ReadOnlyCollection<Provoked>(
        Known.Where(one => one.ThroughTheFixture).ToList());

    /// <summary>The ones that need a shape the fixture cannot take.</summary>
    public static IReadOnlyList<Provoked> Unreachable() => new ReadOnlyCollection<Provoked>(
        Known.Where(one => one.Why == Without.NotYet).ToList());

    /// <summary>
    /// The flags the fixture really has, read out of the article rather than out of a reference.
    /// <para>
    /// The suite cannot see the fixture's types, so the catalogue is asked for the way a person
    /// asks for it — which is the better reading anyway: it is what the built fixture says about
    /// itself, not what a compile-time reference would have said.
    /// </para>
    /// </summary>
    /// <param name="fixture">The built fixture.</param>
    public static IReadOnlyList<string> FlagsOf(string fixture)
    {
        var start = new ProcessStartInfo(fixture) { RedirectStandardOutput = true, UseShellExecute = false };
        start.ArgumentList.Add("--flags");

        using var running = Process.Start(start)!;
        var said = running.StandardOutput.ReadToEnd();
        running.WaitForExit(30_000);

        return new ReadOnlyCollection<string>(said
            .Split('\n')
            .Select(one => one.Trim())
            .Where(one => one.StartsWith("--", StringComparison.Ordinal))
            .Select(FlagName)
            .Where(one => one.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList());
    }

    /// <summary>The pairing as a person reads it, the count first.</summary>
    public static IReadOnlyList<string> Render() => new ReadOnlyCollection<string>(
        [
            $"{Known.Count} refusals: {Reachable().Count} reachable by driving the fixture, "
                + $"{Known.Count(one => one.Why == Without.NothingDrawn)} needing no window, "
                + $"{Unreachable().Count} needing a shape it cannot take.",
            .. Known.Select(one => $"  {one}"),
        ]);
}

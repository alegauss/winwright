using System.Collections.ObjectModel;

namespace Winwright.Tests;

/// <summary>Why a verb that touches the desk is not one a case has to excuse.</summary>
internal enum Touching
{
    /// <summary>It answers a fact about the machine rather than about the desk as it stands: how
    /// many monitors there are, whether a desk exists at all, what this process is running on.</summary>
    AboutTheMachine,

    /// <summary>It is the primitive's own wrapper, and the verb a case calls is the one above it.</summary>
    ThePlumbing,

    /// <summary>It reads a window a caller already has, rather than looking for one on the desk.</summary>
    AWindowInHand,

    /// <summary>It sweeps the desk and then keeps only what the caller named, so what else happens
    /// to be open cannot change the answer.</summary>
    FilteredToWhatTheCallerNamed,

    /// <summary>
    /// It puts back what an act of this engine took and claims nothing. WW330: a case calls one
    /// after it has read everything it came for, and a desk that refuses the tidying leaves the run
    /// exactly where it would have been without the call — so there is no verdict for the desk to
    /// have decided.
    /// </summary>
    PuttingItBack,
}

/// <summary>One engine verb that reaches the desk and is not in <see cref="DeskAsks.Calls" />.</summary>
/// <param name="Named">The verb, as <c>Type.Method</c>.</param>
/// <param name="Kind">Why a case calling it owes no excuse.</param>
/// <param name="Because">The sentence a reader needs.</param>
internal sealed record DeskVerb(string Named, Touching Kind, string Because)
{
    public override string ToString() => $"{Kind,-16} {Named}: {Because}";
}

/// <summary>
/// WW208. <c>DeskAsks.Calls</c> is the judgement WW190 said it was, and the judgement was typed. The
/// filing is checked — every entry names a condition <c>DeskFacts</c> declares — and nothing checked
/// the other end, so a reading the list had never heard of was a reading no case was ever asked to
/// excuse.
/// <para>
/// Found by a guest run rather than by anything here. <c>Traversal.WhoHasFocus</c> reads what holds
/// the focus anywhere on the desk, was absent from the list, and a case asserting on it went red
/// twice on a machine slowed by its own antivirus — saying the focus was not on the control this
/// suite had just put it on, which is the misattribution WW190 exists to stop.
/// </para>
/// <para>
/// So the surface is read rather than remembered, and read at the place the dependence actually
/// lives: a handful of primitives that ask the desk what is on it right now. A verb that reaches one
/// of those, directly or through something beside it in the same file, is desk-dependent whatever
/// its return type says — which is why a rule keyed on return types would have missed the very call
/// this task is about, since <c>WhoHasFocus</c> answers plain element facts.
/// </para>
/// </summary>
internal static class DeskVerbs
{
    /// <summary>
    /// The calls that ask the desk what is on it at this moment.
    /// <para>
    /// Narrow on purpose, and each narrowing is a judgement. <c>GetWindowRect</c> and
    /// <c>IsWindowVisible</c> read a window a caller already holds; <c>GetSystemMetrics</c> answers
    /// a fact about the machine. What is here either looks for something on the desk without being
    /// told where — the foreground, the focus, a window by class, the whole z order — or puts
    /// something onto it.
    /// </para>
    /// </summary>
    internal static IReadOnlyList<string> Primitives { get; } = new ReadOnlyCollection<string>(
    [
        "GetForegroundWindow",
        "FocusedElement",
        "GetLastInputInfo",
        "GetCursorPos",
        "EnumWindows",
        "FindWindowW",
        "SendInputRaw",
        "OpenInputDesktop",
    ]);

    /// <summary>The verbs that reach one and are not calls a case has to excuse, with why.</summary>
    internal static IReadOnlyList<DeskVerb> Excused { get; } = new ReadOnlyCollection<DeskVerb>(
    [
        new("Desk.Read", Touching.AboutTheMachine,
            "it asks whether there is an interactive desk at all — a window station, an input "
                + "desktop, a compositor — which is a fact about the session this run is in and not "
                + "about what happens to be on it. A case cannot excuse the absence of a desk on the "
                + "grounds of the desk, and DeskGateTests asserts it either way"),

        // WW210 found both of these, and found them by repairing the walk rather than by asking a
        // better question. OfProcess is two overloads: the one that enumerates, and the convenience
        // one beside it. The reading kept whichever came last and threw the other away — so the
        // overload that calls EnumWindows was invisible, and Largest, which calls it, with it.
        new("TopLevelWindows.OfProcess", Touching.FilteredToWhatTheCallerNamed,
            "it walks every top-level window there is and keeps the ones belonging to a pid the "
                + "caller named. A desk crowded with somebody else's windows returns the same list, "
                + "and a window this run's application has not drawn yet is what a deadline is for "
                + "rather than what an excuse is for"),
        new("TopLevelWindows.Largest", Touching.FilteredToWhatTheCallerNamed,
            "the same walk, answering the largest of them. It reaches the desk only through "
                + "OfProcess and inherits the whole of its argument"),

        new("NotificationArea.PutBack", Touching.PuttingItBack,
            "WW330. It shuts the flyout the act it belongs to opened and gives the desktop back to "
                + "whatever held it, which is housekeeping and not a reading: the verb that took "
                + "both is the one a case excuses, and this one is called after the case has "
                + "asserted everything it came for. A shell that refuses either leaves the taskbar "
                + "the way it already was, which is the state this exists to improve on rather than "
                + "a verdict it could get wrong"),
    ]);

    /// <summary>Every public verb of the engine that reaches a desk primitive.</summary>
    internal static IReadOnlyList<string> Reaching() => reaching.Value;

    /// <summary>The reading a person gets: the count first, then a line each.</summary>
    internal static IReadOnlyList<string> Render() => new ReadOnlyCollection<string>(
    [
        $"{Reaching().Count} engine verb(s) reach the desk, of which {DeskAsks.Calls.Count} are "
            + $"calls a case has to excuse and {Excused.Count} are not",
        .. Excused.Select(one => $"  {one}"),
    ]);

    private static readonly Lazy<IReadOnlyList<string>> reaching = new(Sweep);

    private static IReadOnlyList<string> Sweep() => Checkout
        .SourcesIn(Checkout.Engine)

        // The primitives themselves live here, and every one of them touches the desk by definition.
        // Excusing eight declarations one at a time would be writing down that a P/Invoke is a
        // P/Invoke; what a case calls is always the verb above them.
        .Where(one => Path.GetFileName(one) != "Win32.cs")
        .SelectMany(InFile)
        .Distinct(StringComparer.Ordinal)
        .OrderBy(one => one, StringComparer.Ordinal)
        .ToList();

    private static IEnumerable<string> InFile(string file)
    {
        var owner = Path.GetFileNameWithoutExtension(file);
        // Grouped and not indexed, because a name in a file can be two overloads. The old copy of
        // this kept the last one and threw the rest away, so an overload that touched the desk was
        // invisible whenever a quieter one was declared below it.
        var bodies = Checkout.Members(file)
            .GroupBy(one => one.Name, StringComparer.Ordinal)
            .ToDictionary(
                one => one.Key,
                one => (Body: string.Join('\n', one.Select(each => each.Body)), IsPublic: one.Any(each => each.IsPublic)),
                StringComparer.Ordinal);

        // All the way down and not one level. A verb rarely calls the primitive itself, and rarely
        // calls something that does: NotificationArea.Find asks OpenOverflow, which asks Chevron,
        // which asks Tray, which asks FindWindowW. One level found Tray and stopped, and the verb a
        // case actually writes down is the one at the top.
        var touching = bodies.Where(one => Touches(one.Value.Body)).Select(one => one.Key).ToHashSet(StringComparer.Ordinal);

        for (var grew = true; grew;)
        {
            grew = false;
            foreach (var one in bodies.Where(one => !touching.Contains(one.Key)))
            {
                if (!touching.Any(deep => one.Value.Body.Contains($"{deep}(", StringComparison.Ordinal)))
                    continue;

                touching.Add(one.Key);
                grew = true;
            }
        }

        return bodies
            .Where(one => one.Value.IsPublic && touching.Contains(one.Key))
            .Select(one => $"{owner}.{one.Key}");
    }

    private static bool Touches(string text) =>
        Primitives.Any(one => text.Contains(one, StringComparison.Ordinal));

    // The walk that reads a file member by member moved to Checkout under WW210, where a second
    // sweep needed the same one. The reading here stays per file on purpose: the public names were
    // once kept in one set across the whole engine, so a private helper sharing a name with
    // somebody else's public verb was read as public, and a sweep whose answer depends on which
    // file it read first is not a reading.
}

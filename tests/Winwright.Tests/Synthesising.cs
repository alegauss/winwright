using System.Collections.ObjectModel;

namespace Winwright.Tests;

/// <summary>How a caller of a verb learns that it put input onto the desk.</summary>
internal enum Asking
{
    /// <summary>Synthesising is what the verb is for. Nobody calls <c>Keyboard.Type</c> expecting a
    /// pattern, and the family it is in is the answer to what it needs.</summary>
    ItIsTheAct,

    /// <summary>The caller asked for it by name, in an argument that defaults to not asking.</summary>
    TheCallerOptedIn,

    /// <summary>The verb escalated without being asked, and the result it answers says which route
    /// ran and why the pattern one did not — which is the criterion's second half, in the one place
    /// a caller cannot fail to read it.</summary>
    TheResultSaysWhichRouteRan,
}

/// <summary>One public verb of the engine that reaches synthesised input.</summary>
/// <param name="Named">The verb, as <c>Type.Method</c>.</param>
/// <param name="How">How a caller finds out.</param>
/// <param name="Because">The sentence a reader needs.</param>
internal sealed record Synthesiser(string Named, Asking How, string Because)
{
    public override string ToString() => $"{How,-26} {Named}: {Because}";
}

/// <summary>
/// WW210. Block D's first criterion says the default act needs no foreground: every act that can go
/// through a pattern does, and the ones that cannot are declared as pointer acts carrying the reason.
/// The second half has teeth — <c>DeclaredCostTests</c> checks a stated reason against the tree
/// rather than believing it. The first half had one case, on one label, and otherwise held because
/// nobody had written the fallback.
/// <para>
/// Filed expecting to assert that the pattern routes reach no input synthesiser at all. Measured
/// first, and that was wrong twice over: <c>Pick.Value</c> walks a picker by keyboard when the
/// pattern does not take, and <c>Selecting.Confirmed</c> clicks. Both are deliberate, both were
/// written down where it matters, and a check demanding they reach nothing would have been a check
/// demanding the engine be worse.
/// </para>
/// <para>
/// So the reading is the honest one: every verb that reaches synthesised input is paired with
/// how a caller learns it did. What the criterion forbids is not the escalation — it is an
/// escalation nobody is told about, and that is what a missing entry now is.
/// </para>
/// <para>
/// The reach is transitive and crosses files, which is the whole difficulty. <c>Selecting.Confirmed</c>
/// calls <c>Pointer.Click</c>, which calls <c>Pointer.Run</c>, which calls <c>Win32.SendInput</c>,
/// which calls the import. A rule reading one level finds none of them.
/// </para>
/// </summary>
internal static class Synthesising
{
    /// <summary>
    /// What puts input onto the desk. Two: the send, and moving the cursor to where it will land.
    /// <para>
    /// <c>MapVirtualKeyW</c> and <c>GetCursorPos</c> are not here and are next to them in the same
    /// file. Reading a key code or asking where the pointer is synthesises nothing, and a seed list
    /// that took them would report every verb that reads the desk as one that writes to it.
    /// </para>
    /// </summary>
    internal static IReadOnlyList<string> Primitives { get; } = new ReadOnlyCollection<string>(
    [
        "SendInputRaw",
        "SetCursorPos(",
    ]);

    /// <summary>The route a default act takes, which the criterion says needs no foreground.</summary>
    internal const string TheDefaultAct = "Act";

    /// <summary>Every verb that reaches synthesised input, with how a caller learns it did.</summary>
    internal static IReadOnlyList<Synthesiser> Known { get; } = new ReadOnlyCollection<Synthesiser>(
    [
        // The families whose whole job is input. Grouped rather than argued one at a time: a caller
        // reaching for Keyboard, Pointer or Traversal has said what it wants by choosing the name.
        new("Keyboard.Type", Asking.ItIsTheAct, "typing is the act"),
        new("Keyboard.Run", Asking.ItIsTheAct, "a prepared run of keys, which is the same act in bulk"),
        new("Pointer.Click", Asking.ItIsTheAct,
            "a click, and it will not be reached without a PointerReason naming why the pattern "
                + "route was not the one taken"),
        new("Pointer.DoubleClick", Asking.ItIsTheAct, "two presses, and it says so"),
        new("Pointer.Run", Asking.ItIsTheAct, "the send under both, which is where the desk is asked"),
        new("Traversal.Press", Asking.ItIsTheAct, "a traversal key at a window, which is a keystroke"),
        new("Traversal.Nudge", Asking.ItIsTheAct, "an arrow at whatever holds the focus"),

        // WW225 and WW226. The same four acts in the shape a step of a case is answered in, so a data
        // file can
        // name them. Its own type and never on Act, because the criterion above is that the default
        // act reaches no send — and an adapter living there would have made that false. The name says
        // what it does, which is what makes ItIsTheAct the honest answer here too.
        new("Synthesised.Type", Asking.ItIsTheAct, "typing, as a step's own result"),
        new("Synthesised.Click", Asking.ItIsTheAct,
            "a click, carrying the PointerReason a case had to write to reach it"),
        new("Synthesised.Nudge", Asking.ItIsTheAct, "an arrow key at a range control, as a step"),
        new("Synthesised.Press", Asking.ItIsTheAct, "a traversal key at the window, as a step"),

        // WW254. The fifth, and the only one that reaches the send by falling back rather than by
        // being it: the selection pattern is asked first and needs nothing of the desk. Catalogued
        // as the act anyway, because what a case names is 'pick' and the keys are inside it — the
        // caller who has to know is the one reading whether the walk's own count still holds.
        new("Synthesised.Pick", Asking.ItIsTheAct,
            "reaching a value in a picker, by the pattern where that works and by the keyboard where it does not"),

        // A menu is entered the way a keyboard user enters one, and that is Block D's third
        // criterion rather than an escalation: reaching a destructive entry by invoke is refused
        // at the door, so walking is the only route there is.
        new("Menu.Enter", Asking.ItIsTheAct, "entering a menu bar is pressing the key that opens it"),
        new("Menu.To", Asking.ItIsTheAct, "walking to an entry, one traversal key at a time"),
        new("Menu.Expand", Asking.ItIsTheAct, "opening a submenu the same way"),
        new("Menu.Dismiss", Asking.ItIsTheAct, "and closing it, which is Escape"),
        new("NotificationArea.OpenMenu", Asking.ItIsTheAct,
            "an icon's context menu, reached by key because the shell offers no pattern for it"),

        // The two that are pattern routes first. These are what this task was filed about.
        new("Selecting.Confirmed", Asking.TheCallerOptedIn,
            "the one escalation in the project. The pattern is tried and the result is read back; "
                + "where it did not take, a click is sent only if the caller passed the permission, "
                + "and without it the answer is SelectRoute.Neither saying the pointer was not "
                + "allowed rather than a green"),
        new("Pick.Value", Asking.TheResultSaysWhichRouteRan,
            "a picker walked by keyboard where the pattern did not take. The caller can ask for that "
                + "route by name, and a caller that did not still learns: the result carries "
                + "PickRoute.Keyboard, the reason the pattern was refused, and the foreground as a "
                + "precondition — so a desk that would not give it up reads as unchecked and never "
                + "as a value that was picked"),
    ]);

    /// <summary>Every public verb of the engine that reaches one of the primitives, at any depth.</summary>
    internal static IReadOnlyList<string> Reaching() => sweep.Value.Public;

    /// <summary>Every member at all that does, which is what a claim about one family needs.</summary>
    internal static IReadOnlyList<string> ReachingAtAll() => sweep.Value.Reaching;

    /// <summary>The reading a person gets: the count first, then a line each.</summary>
    internal static IReadOnlyList<string> Render() => new ReadOnlyCollection<string>(
    [
        $"{Reaching().Count} verb(s) reach synthesised input: "
            + string.Join(", ", Known.GroupBy(one => one.How)
                .OrderBy(one => one.Key)
                .Select(one => $"{one.Count()} {Worded(one.Key)}")),
        .. Known.Select(one => $"  {one}"),
    ]);

    /// <summary>How a caller learns, as the clause a rendered line reads with.</summary>
    /// <param name="how">The way.</param>
    internal static string Worded(Asking how) => how switch
    {
        Asking.ItIsTheAct => "are the act itself",
        Asking.TheCallerOptedIn => "are asked for by name",
        Asking.TheResultSaysWhichRouteRan => "say which route ran in the result",
        _ => throw new ArgumentOutOfRangeException(nameof(how)),
    };

    // One field and not two. A second Lazy reading this one from its own initialiser is null at the
    // moment it is written down — the same static ordering that answered a null reference in every
    // case reading a file when Checkout.Everything shipped, and that the compiler catches here only
    // because this one is nullable-annotated.
    private static readonly Lazy<(List<string> Public, List<string> Reaching)> sweep = new(Sweep);

    /// <summary>
    /// The reach, all the way down and across files.
    /// <para>
    /// A call to another type is written with the type on it and a call inside a file is not, so an
    /// edge is taken on <c>Owner.Member(</c> anywhere, and on a bare <c>Member(</c> only within the
    /// file that declares it. A bare name matched across the whole engine would let any private
    /// helper called <c>Run</c> stand in for <c>Pointer.Run</c>, and the sweep would report verbs
    /// that synthesise nothing.
    /// </para>
    /// </summary>
    private static (List<string> Public, List<string> Reaching) Sweep()
    {
        var members = Checkout.SourcesIn(Checkout.Engine)
            .SelectMany(one => Checkout.Members(one))
            .GroupBy(one => one.Named, StringComparer.Ordinal)
            .ToDictionary(
                one => one.Key,
                one => (
                    Owner: one.First().Owner,
                    Body: string.Join('\n', one.Select(each => each.Body)),
                    IsPublic: one.Any(each => each.IsPublic)),
                StringComparer.Ordinal);

        var touching = members
            .Where(one => Primitives.Any(mark => one.Value.Body.Contains(mark, StringComparison.Ordinal)))
            .Select(one => one.Key)
            .ToHashSet(StringComparer.Ordinal);

        for (var grew = true; grew;)
        {
            grew = false;
            foreach (var one in members.Where(one => !touching.Contains(one.Key)).ToList())
            {
                if (!touching.Any(deep => Calls(one.Value.Body, one.Value.Owner, deep)))
                    continue;

                touching.Add(one.Key);
                grew = true;
            }
        }

        var all = touching.OrderBy(one => one, StringComparer.Ordinal).ToList();
        return (all.Where(one => members[one].IsPublic).ToList(), all);
    }

    private static bool Calls(string body, string owner, string named)
    {
        var dot = named.IndexOf('.', StringComparison.Ordinal);
        var type = named[..dot];
        var member = named[(dot + 1)..];

        return body.Contains($"{type}.{member}(", StringComparison.Ordinal)
            || (string.Equals(type, owner, StringComparison.Ordinal)
                && body.Contains($"{member}(", StringComparison.Ordinal));
    }
}

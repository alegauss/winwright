using System.Collections.ObjectModel;
using System.Reflection;

using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>Why a helper that consumes a three-state reading may answer a type without one.</summary>
internal enum Flattened
{
    /// <summary>It is the control: it exists to be found, so the sweep is measured rather than
    /// trusted.</summary>
    TheControl,

    /// <summary>It answers the third state through a door rather than a value — a null the caller
    /// returns on, an excuse already taken, a throw the caller is meant to meet.</summary>
    ADoor,

    /// <summary>The reading is arranged by the helper itself, so the state it could report is one
    /// this suite put there and can put there again.</summary>
    Arranged,

    /// <summary>What is taken off the reading is not the observation — a name, a rendering, a
    /// count of something the reading holds whatever it managed to look at.</summary>
    NotTheObservation,
}

/// <summary>One helper that reads a three-state reading and answers something narrower.</summary>
/// <param name="Named">The member, as <c>Type.Method</c>.</param>
/// <param name="Kind">Why the narrowing loses nothing.</param>
/// <param name="Because">The sentence a reader needs.</param>
internal sealed record Flatten(string Named, Flattened Kind, string Because)
{
    public override string ToString() => $"{Kind,-18} {Named}: {Because}";
}

/// <summary>
/// WW191, and the half that reaches the defect. <c>Swallowing</c> draws the boundary at a catch, and
/// it is worth saying plainly that WW181 had none: the overflow not opening arrived as
/// <c>OverflowState.Held</c>, a bool on a reading that knew perfectly well it had not looked. So a
/// rule about swallowed exceptions would have missed it exactly as WW182's did.
/// <para>
/// What WW181 actually was: a helper read something that carries a third state and answered a type
/// that cannot. <c>TrayGhosts.Showing</c> took <c>OpenOverflow</c>'s reading, dropped everything but
/// whether it worked, and answered <c>IReadOnlyList&lt;string&gt;</c> — and a list has two states.
/// The repair gave it <c>TrayCensus</c>, which carries all three, so it passes here now and would
/// have been red before. That is the check WW182 was reaching for.
/// </para>
/// <para>
/// Nothing about which readings are three-state is typed here. A type is one where it answers the
/// engine's own <c>Finding</c>, <c>AssertionResult</c> or <c>Precondition</c>, and the calls that
/// produce one are every method in either assembly that returns such a type — so a reading added
/// tomorrow brings its own call into this sweep without anybody remembering to add it.
/// </para>
/// <para>
/// Cases are not this. A case asserts and answers nothing; the hazard is a helper standing between
/// a reading and a case, which is where WW181 stood.
/// </para>
/// </summary>
internal static class Flattening
{
    /// <summary>Whether a type can say it did not look.</summary>
    internal static bool Carries(Type answered)
    {
        ArgumentNullException.ThrowIfNull(answered);

        if (answered == typeof(Finding) || answered == typeof(AssertionResult) || answered == typeof(Precondition))
            return true;

        // Nullable of anything is a third state by construction, which is the argument Finding's own
        // `bool? Holds` makes about itself.
        if (Nullable.GetUnderlyingType(answered) is not null)
            return true;

        if (answered
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Any(one => one.Name is "AsFinding" or "AsAssertion" or "AsPrecondition"))
            return true;

        // Or it reports one of the words this engine uses for "not observed". RunVerdict answers no
        // AsFinding and is certainly not two-state — `Degraded` is the third state with a different
        // name — and a predicate that missed it would file every verdict a case composes as a
        // narrowing. The vocabulary is small and is checked against the engine rather than assumed.
        return answered
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Select(one => one.PropertyType)
            .Any(Names);
    }

    /// <summary>What this engine calls the third state, wherever it has had to name it.</summary>
    internal static IReadOnlyList<string> Vocabulary { get; } =
        new ReadOnlyCollection<string>(["Unchecked", "Degraded", "Unread"]);

    /// <summary>Whether an enum has a member meaning nothing was observed.</summary>
    internal static bool Names(Type answered)
    {
        ArgumentNullException.ThrowIfNull(answered);

        return answered.IsEnum
            && Enum.GetNames(answered).Any(one => Vocabulary.Contains(one, StringComparer.Ordinal));
    }

    /// <summary>Every call in either assembly that answers a reading carrying a third state.</summary>
    internal static IReadOnlyList<string> Producers() => producers.Value;

    /// <summary>The helpers that consume one and answer a type that cannot carry it.</summary>
    internal static IReadOnlyList<string> Found() => found.Value;

    /// <summary>Those of them paired with why the narrowing loses nothing.</summary>
    internal static IReadOnlyList<Flatten> Known { get; } = new ReadOnlyCollection<Flatten>(
    [
        new($"{nameof(TheShapeWW181Shipped)}.{nameof(TheShapeWW181Shipped.Showing)}", Flattened.TheControl,
            "WW181's own signature, kept and called by nothing. The suite is otherwise clean under "
                + "this rule, and a rule with nothing to find passes by arithmetic — so the one "
                + "thing it must find is the defect it was written for"),
    ]);

    /// <summary>The reading a person gets: the count first, then a line each.</summary>
    internal static IReadOnlyList<string> Render() => new ReadOnlyCollection<string>(
    [
        $"{Found().Count} helper(s) read a three-state reading and answer something narrower, "
            + $"out of {Producers().Count} call(s) that produce one",
        .. Known.Select(one => $"  {one}"),
    ]);

    private static readonly Lazy<IReadOnlyList<string>> producers = new(Producing);
    private static readonly Lazy<IReadOnlyList<string>> found = new(Scan);

    private static IReadOnlyList<string> Producing() => new[]
        {
            typeof(Precondition).Assembly,
            typeof(Flattening).Assembly,
        }
        .SelectMany(one => one.GetTypes())
        .Where(one => !one.Name.Contains('<', StringComparison.Ordinal))
        .SelectMany(one => one
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Where(method => !method.Name.Contains('<', StringComparison.Ordinal))
            .Where(method => Carries(method.ReturnType))
            .Select(method => $"{one.Name}.{method.Name}("))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(one => one, StringComparer.Ordinal)
        .ToList();

    /// <summary>
    /// The suite's own members, read out of the sources, because a call graph is what this is about
    /// and metadata does not hold one.
    /// </summary>
    private static IReadOnlyList<string> Scan()
    {
        var flattening = new List<string>();
        foreach (var file in Checkout.SourcesIn(Checkout.Suite, except: $"{nameof(Flattening)}.cs"))
        {
            foreach (var member in Reading(file))
            {
                var answered = Answers(member);
                if (answered is not null && !Carries(answered) && answered != typeof(void))
                    flattening.Add(member);
            }
        }

        return flattening.Distinct(StringComparer.Ordinal).OrderBy(one => one, StringComparer.Ordinal).ToList();
    }

    /// <summary>Every member of one file that calls a producer, and is not a case.</summary>
    private static IEnumerable<string> Reading(string file)
    {
        var reading = new List<string>();
        var owner = "";
        var isACase = false;
        var member = "";
        var calls = false;

        foreach (var line in File.ReadLines(file))
        {
            if (Declares(line) is { } named)
                owner = named;

            if (line.Contains("[Fact]", StringComparison.Ordinal) || line.Contains("[Theory]", StringComparison.Ordinal))
            {
                isACase = true;
            }
            else if (Member(line) is { } next)
            {
                Close();
                member = next;
                isACase = false;
            }
            else if (member.Length > 0 && !isACase)
            {
                calls |= Producers().Any(one => line.Contains(one, StringComparison.Ordinal));
            }
        }

        Close();
        return reading;

        void Close()
        {
            if (member.Length > 0 && calls && !isACase)
                reading.Add($"{owner}.{member}");

            member = "";
            calls = false;
        }
    }

    /// <summary>What that member answers, read off the assembly. Null where nothing is called that.</summary>
    private static Type? Answers(string named)
    {
        var split = named.LastIndexOf('.');
        if (split <= 0)
            return null;

        var owner = typeof(Flattening).Assembly
            .GetTypes()
            .FirstOrDefault(one => string.Equals(one.Name, named[..split], StringComparison.Ordinal));

        var spellings = owner?
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(one => string.Equals(one.Name, named[(split + 1)..], StringComparison.Ordinal))
            .ToList();

        // An overload set narrowing in one spelling and not in another is a finding either way, so
        // the widest answer wins: if any of them carries the third state, the member does.
        if (spellings is null || spellings.Count == 0)
            return null;

        return spellings.FirstOrDefault(one => Carries(one.ReturnType))?.ReturnType ?? spellings[0].ReturnType;
    }

    private static string? Declares(string line)
    {
        if (!line.StartsWith("public ", StringComparison.Ordinal)
            && !line.StartsWith("internal ", StringComparison.Ordinal))
            return null;

        var at = line.IndexOf("class ", StringComparison.Ordinal);
        if (at < 0)
            return null;

        var rest = line[(at + 6)..].Trim();
        var end = rest.IndexOfAny([' ', ':', '(', '{', '<']);
        return end < 0 ? rest : rest[..end];
    }

    private static string? Member(string line)
    {
        if (!line.StartsWith("    public ", StringComparison.Ordinal)
            && !line.StartsWith("    private ", StringComparison.Ordinal)
            && !line.StartsWith("    internal ", StringComparison.Ordinal))
            return null;

        var bracket = line.IndexOf('(', StringComparison.Ordinal);
        if (bracket < 0)
            return null;

        var before = line[..bracket];
        var space = before.LastIndexOf(' ');
        return space < 0 ? null : before[(space + 1)..];
    }

}

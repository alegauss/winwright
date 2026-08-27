using System.Collections.ObjectModel;
using System.Reflection;

using Winwright.Verdicts;

namespace Winwright.Tests;

/// <summary>Why a reading that swallows an exception needs nowhere to say it did not look.</summary>
internal enum Swallowed
{
    /// <summary>The exception <em>is</em> the reading. What was asked is whether the thing can be
    /// read at all, and the throw answers that as directly as a value would.</summary>
    TheAnswer,

    /// <summary>It is a step inside a deadline, so a look that threw is a look that has not
    /// succeeded yet and the wait says what it was still waiting for.</summary>
    OneLook,

    /// <summary>It already answers a type that carries the third state, so the rule is met rather
    /// than excused.</summary>
    Carried,
}

/// <summary>
/// One method in this suite that catches something and answers a value anyway.
/// </summary>
/// <param name="Named">The method, as <c>Type.Method</c>.</param>
/// <param name="Kind">Why it needs no third state of its own, or how it already has one.</param>
/// <param name="Because">The sentence a reader needs.</param>
internal sealed record Swallow(string Named, Swallowed Kind, string Because)
{
    public override string ToString() => $"{Kind,-10} {Named}: {Because}";
}

/// <summary>
/// WW191. WW182 drew a rule at the wrong moment. It says every suite reading that answers a verdict
/// answers a <see cref="Finding" /> too, and it is worth stating plainly what that would have done
/// about WW181: nothing. <c>TrayGhosts.Showing</c> answered a list of strings and a static sentence
/// taking one — no verdict, no <c>AsAssertion</c>, nothing for the sweep to find — and a case
/// asserted on the list directly. The verdict arrived in the repair, so the rule keys on the thing
/// that was added <em>after</em> the defect.
/// <para>
/// The hazard begins earlier: when a reading swallows a failure and answers a value regardless.
/// That is a third state whether or not the type has anywhere to put one, and a <c>bool</c> answered
/// out of a catch block is the shape WW181 shipped — "I could not tell" spelled exactly like "no".
/// </para>
/// <para>
/// Read off the assembly and never off a list. <c>MethodBody.ExceptionHandlingClauses</c> is what a
/// compiler wrote down about the method, so a catch added tomorrow is here tomorrow, and a catch
/// somebody spelled differently is here too. A method that catches and answers <c>void</c> is not
/// this: it has nothing to report the failure as.
/// </para>
/// </summary>
internal static class Swallowing
{
    /// <summary>Every method in this suite that catches something and answers a value.</summary>
    internal static IReadOnlyList<string> Found() => found.Value;

    /// <summary>The ones paired with why they need no third state of their own.</summary>
    internal static IReadOnlyList<Swallow> Known { get; } = new ReadOnlyCollection<Swallow>(
    [
        new("Attachable.Readable", Swallowed.TheAnswer,
            "the question is literally whether the module can be read, so a read that threw is the "
                + "false this answers and there is no third state to lose"),
        new("BusyDesk.Built", Swallowed.Carried,
            "it answers null for a fixture the desk refused, and the reading that says why is "
                + "checked against DeskFacts on the way past — the caller returns rather than "
                + "asserting, which is the third state with a door rather than a value"),
        new("FixtureTests.Written", Swallowed.OneLook,
            "one look inside a deadline, and its own note says so: a dump caught half-written is a "
                + "file this run has not finished writing, and the wait around it reports what it "
                + "was still waiting for rather than turning the wait into the failure"),
        new("EncodingTests.EncodedTwice", Swallowed.TheAnswer,
            "the question is whether these bytes are valid UTF-8 for something shorter, so a strict "
                + "decode that threw is the false this answers — and the throw is the reading rather "
                + "than a failure of it, which is why the decoder is asked to throw at all"),
        new("FixtureTests.Alive", Swallowed.TheAnswer,
            "a pid the runtime will not build a process for is a pid nothing is running under, "
                + "which is the false this answers"),
        new("ProcessRegisterTests.StillRunning", Swallowed.TheAnswer,
            "the same reading in the register's own cases, and the same identity between the throw "
                + "and the answer"),
        new("ProvokedByFlagTests.Read", Swallowed.OneLook,
            "the fixture's output is read while the fixture is still writing it, so a file locked "
                + "mid-write is read again — the empty string is this look and never the verdict, "
                + "and the wait around it is what answers"),
        new("TrayGhosts.Running", Swallowed.TheAnswer,
            "whether a pid a census found still exists, where a process object that will not answer "
                + "is one that has gone — and the census's own third state is the overflow it could "
                + "not open, which WW181 was about and TrayCensus now carries"),
    ]);

    /// <summary>The reading a person gets: the count first, then a line each.</summary>
    internal static IReadOnlyList<string> Render() => new ReadOnlyCollection<string>(
    [
        $"{Found().Count} method(s) in this suite catch and answer a value, "
            + $"{Known.Count(one => one.Kind == Swallowed.Carried)} of them carrying the third state",
        .. Known.Select(one => $"  {one}"),
    ]);

    private static readonly Lazy<IReadOnlyList<string>> found = new(Sweep);

    /// <summary>
    /// Every method with a catch clause and something to answer with. Compiler-written members are
    /// left out by name: a <c>using</c> becomes a finally and an iterator becomes a state machine,
    /// and neither is a reading anybody wrote.
    /// </summary>
    private static IReadOnlyList<string> Sweep() => typeof(Swallowing).Assembly
        .GetTypes()
        .Where(one => !one.Name.Contains('<', StringComparison.Ordinal))
        .SelectMany(one => one
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.ReturnType != typeof(void))
            .Where(method => !method.Name.Contains('<', StringComparison.Ordinal))
            .Where(Catches)
            .Select(method => $"{one.Name}.{method.Name}"))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(one => one, StringComparer.Ordinal)
        .ToList();

    /// <summary>Whether the compiler wrote a catch clause into it — a finally is not one.</summary>
    private static bool Catches(MethodInfo method)
    {
        var body = method.GetMethodBody();
        return body is not null
            && body.ExceptionHandlingClauses.Any(one => one.Flags == ExceptionHandlingClauseOptions.Clause
                || one.Flags == ExceptionHandlingClauseOptions.Filter);
    }
}

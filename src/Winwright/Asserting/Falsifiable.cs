using System.Collections;
using System.Collections.ObjectModel;

using Winwright.Tracing;
using Winwright.Verdicts;

namespace Winwright.Asserting;

/// <summary>What one injection did to the check it was aimed at.</summary>
public enum Bite
{
    /// <summary>It turned the check red, which is the only outcome that proves anything.</summary>
    TurnedItRed,

    /// <summary>The check stayed green with the defect in place, so it does not cover it.</summary>
    LeftItGreen,

    /// <summary>
    /// The reading came back equal to the honest one, so nothing was ever injected. Told apart
    /// from a green because the check is not what failed here — the injection is.
    /// </summary>
    ChangedNothing,

    /// <summary>The check threw against the injected reading, which is a break and not a red.</summary>
    Threw,
}

/// <summary>One defect a case declares its check must catch.</summary>
/// <typeparam name="TReading">Whatever the check reads to settle its claim.</typeparam>
/// <param name="Name">The defect, in the words a report names it by — "the fourth tab removed".</param>
/// <param name="Apply">The reading as it would be with that defect in the window.</param>
public sealed record Injection<TReading>(string Name, Func<TReading, TReading> Apply);

/// <summary>What one injection turned out to prove, or to fail to.</summary>
/// <param name="Name">The defect that was injected.</param>
/// <param name="Outcome">What it did to the check.</param>
/// <param name="Detail">The sentence a report carries, empty where it bit and there is nothing to explain.</param>
public sealed record InjectionResult(string Name, Bite Outcome, string Detail)
{
    /// <summary>Whether this injection proved the check covers the defect it names.</summary>
    public bool Bit => Outcome == Bite.TurnedItRed;
}

/// <summary>
/// Whether a check can fail at all, read by injecting the defects it claims to catch.
/// <para>
/// A green says the window is right only if the check would have said otherwise had it been wrong,
/// and nothing in a passing run tests that. Several tasks across these repositories record an
/// assertion being watched go red before it was trusted, and one of them found the defect in the
/// check rather than in the code under test — a check that passes forever is worse than the absent
/// one it was written instead of, because it also reports that the ground is covered.
/// </para>
/// <para>
/// Opt-in, because not every assertion has a cheap injection to declare — and naming the ones that
/// do is itself a reading of what the check is really claiming.
/// </para>
/// </summary>
public sealed record Falsification
{
    private Falsification(
        string assertion, AssertionResult honest, IReadOnlyList<InjectionResult> injections)
    {
        Assertion = assertion;
        Honest = honest;
        Injections = injections;
    }

    /// <summary>The check this is about, under the name it is reported by.</summary>
    public string Assertion { get; }

    /// <summary>What the check said about the window as it actually stood.</summary>
    public AssertionResult Honest { get; }

    /// <summary>Every declared defect, against what it turned out to prove.</summary>
    public IReadOnlyList<InjectionResult> Injections { get; }

    /// <summary>The declared defects this check does not catch, named. Empty where it catches all.</summary>
    public IReadOnlyList<string> Missed =>
        new ReadOnlyCollection<string>(Injections.Where(one => !one.Bit).Select(one => one.Name).ToList());

    /// <summary>
    /// Whether the honest reading was green. False leaves the falsifiability unsettled rather than
    /// failed: an injection turning a red check red proves nothing about either.
    /// </summary>
    public bool WasGreen => Honest.Outcome == AssertionOutcome.Passed;

    /// <summary>Whether every declared defect turned the check red.</summary>
    public bool CanFail => WasGreen && Injections.Count > 0 && Missed.Count == 0;

    /// <summary>
    /// Run a check against the honest reading and then against each declared defect.
    /// </summary>
    /// <param name="named">The name the check is reported under.</param>
    /// <param name="reading">The window, file or element as it actually stands.</param>
    /// <param name="check">The check, as a function of the reading — the same one the run uses.</param>
    /// <param name="injections">The defects this check claims to catch. At least one is required.</param>
    /// <param name="sameReading">
    /// How to tell an injected reading from the honest one, where the default cannot. Records and
    /// values compare by content already; a reading held in a mutable collection compares by
    /// reference, so element-wise is tried next and a caller whose reading is neither says how.
    /// </param>
    /// <exception cref="ArgumentException">Where no defect was declared — see the remarks.</exception>
    /// <remarks>
    /// An empty list is refused rather than passing vacuously. "Every declared defect was caught"
    /// is true of a check that declared none, and that sentence under a check that cannot fail is
    /// the exact green this whole reading exists to withdraw.
    /// </remarks>
    public static Falsification Of<TReading>(
        string named,
        TReading reading,
        Func<TReading, AssertionResult> check,
        IReadOnlyList<Injection<TReading>> injections,
        IEqualityComparer<TReading>? sameReading = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(named);
        ArgumentNullException.ThrowIfNull(check);
        ArgumentNullException.ThrowIfNull(injections);

        if (injections.Count == 0)
            throw new ArgumentException(
                $"'{named}' declared no defect to inject, and a check that caught every defect it named "
                + "caught nothing at all",
                nameof(injections));

        var honest = check(reading);
        var results = new List<InjectionResult>();
        foreach (var injection in injections)
        {
            ArgumentNullException.ThrowIfNull(injection);
            results.Add(Against(named, reading, check, injection, sameReading));
        }

        return new Falsification(named.Trim(), honest, new ReadOnlyCollection<InjectionResult>(results));
    }

    /// <summary>The same, for a case that spells its defects inline.</summary>
    public static Falsification Of<TReading>(
        string named,
        TReading reading,
        Func<TReading, AssertionResult> check,
        params Injection<TReading>[] injections) =>
        Of(named, reading, check, (IReadOnlyList<Injection<TReading>>)(injections ?? []));

    /// <summary>The whole reading, in the sentence a report carries.</summary>
    public string Sentence()
    {
        if (!WasGreen)
        {
            return $"'{Assertion}' did not pass on the window as it stands, so nothing here says whether it "
                + "can fail: an injection turning a red check red proves neither of them.";
        }

        if (CanFail)
        {
            return $"'{Assertion}' was watched go red on all {VerdictSummary.Plural(Injections.Count, "declared defect")}.";
        }

        var missed = Injections.Where(one => !one.Bit).Select(one => one.Detail);
        return $"'{Assertion}' passed with a declared defect in place, so its green does not cover it: "
            + string.Join("; ", missed) + ".";
    }

    /// <summary>
    /// The result a verdict counts. A check that could not be falsified because its own reading was
    /// already red is <em>unchecked</em> and not a pass — the question was never settled, and a
    /// green here would be a green covering a check that did not run.
    /// </summary>
    public AssertionResult AsAssertion()
    {
        var named = $"{Assertion} can fail";
        if (!WasGreen)
        {
            return AssertionResult.Unchecked(
                named,
                Precondition.Absent($"a green '{Assertion}' to falsify", Sentence()));
        }

        return CanFail ? AssertionResult.Pass(named, Sentence()) : AssertionResult.Fail(named, Sentence());
    }

    /// <summary>
    /// The step a trace records. WW163: which injections bit and which the assertion swallowed is
    /// the reading here, and a record keeping only whether it can fail leaves a reader unable to
    /// say which mutation the check went on passing through.
    /// </summary>
    public TraceStep AsTraceStep() => new()
    {
        Verb = "falsify",
        Locator = Assertion,
        ReadBack = $"{Injections.Count(one => one.Outcome == Bite.TurnedItRed)} of {Injections.Count} turned it red",
        Verdict = !WasGreen ? StepVerdict.Unchecked : CanFail ? StepVerdict.Ok : StepVerdict.Failed,
        Detail = WasGreen && CanFail ? null : Sentence(),
    };

    /// <summary>
    /// Whether the injection handed back the reading it was given. The default comparer settles a
    /// record or a value; a reading held in a list compares by reference under it, and a fresh
    /// list holding the same names is exactly what an inert injection returns — so element-wise is
    /// tried before the answer is given.
    /// </summary>
    private static bool Same<TReading>(TReading before, TReading after, IEqualityComparer<TReading>? sameReading)
    {
        if (sameReading is not null)
            return sameReading.Equals(before, after);

        if (EqualityComparer<TReading>.Default.Equals(before, after))
            return true;

        return before is not string
            && before is IEnumerable left
            && after is IEnumerable right
            && left.Cast<object?>().SequenceEqual(right.Cast<object?>());
    }

    private static InjectionResult Against<TReading>(
        string named,
        TReading reading,
        Func<TReading, AssertionResult> check,
        Injection<TReading> injection,
        IEqualityComparer<TReading>? sameReading)
    {
        var defect = string.IsNullOrWhiteSpace(injection.Name) ? "(unnamed defect)" : injection.Name.Trim();

        TReading injected;
        try
        {
            injected = injection.Apply(reading);
        }
        catch (Exception broke) when (broke is not OutOfMemoryException and not StackOverflowException)
        {
            return new InjectionResult(
                defect, Bite.Threw, $"injecting '{defect}' threw {broke.GetType().Name}: {broke.Message}");
        }

        // Before the check runs, because a reading that never changed makes any verdict at all
        // meaningless — and a green from one would be reported as a check that does not cover the
        // defect, when what actually happened is that the defect was never put in front of it.
        if (Same(reading, injected, sameReading))
        {
            return new InjectionResult(
                defect,
                Bite.ChangedNothing,
                $"'{defect}' left the reading exactly as it was, so it was never put in front of the check");
        }

        try
        {
            var verdict = check(injected);
            return verdict.Outcome switch
            {
                AssertionOutcome.Failed => new InjectionResult(defect, Bite.TurnedItRed, ""),
                AssertionOutcome.Passed => new InjectionResult(
                    defect, Bite.LeftItGreen, $"'{named}' still passed with '{defect}' in place"),
                _ => new InjectionResult(
                    defect,
                    Bite.LeftItGreen,
                    $"'{named}' reported a hole rather than a red with '{defect}' in place"),
            };
        }
        catch (Exception broke) when (broke is not OutOfMemoryException and not StackOverflowException)
        {
            return new InjectionResult(
                defect,
                Bite.Threw,
                $"'{named}' threw {broke.GetType().Name} against '{defect}' rather than reporting a red: {broke.Message}");
        }
    }
}

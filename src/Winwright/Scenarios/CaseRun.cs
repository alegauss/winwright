using System.Collections.ObjectModel;
using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Asserting;
using Winwright.Locating;
using Winwright.Projects;
using Winwright.Tracing;
using Winwright.Verdicts;

namespace Winwright.Scenarios;

/// <summary>
/// What running one case produced: the verdict, the trace, and — where it stopped early — the steps
/// it never reached.
/// <para>
/// <see cref="NotReached"/> is the field a script cannot have. A harness that throws halfway
/// through leaves the rest of the file unexecuted, and a report saying only <em>broken</em> is a
/// report the reader has to count lines to understand. Naming them is also what the run-one-case
/// verb is built on: what a run did not do is an answer, in the same way "not observed" is.
/// </para>
/// </summary>
public sealed class CaseResult
{
    internal CaseResult(
        CaseDeclaration declared,
        RunVerdict verdict,
        IReadOnlyList<TraceStep> trace,
        IReadOnlyList<StepDeclaration> notReached,
        bool lent = false)
    {
        Declared = declared;
        Verdict = verdict;
        Trace = trace;
        NotReached = notReached;
        Lent = lent;
    }

    /// <summary>The case this is a run of.</summary>
    public CaseDeclaration Declared { get; }

    /// <summary>What it ran against, which is also what its expectations were read from.</summary>
    public FixtureDeclaration Against => Declared.Fixture;

    /// <summary>
    /// Whether this case borrowed a window rather than owning one.
    /// <para>
    /// A property of the run and not of the case: the same case run alone owns its process, and
    /// that is the property WW62 is careful to keep. So a report can say which of the two this
    /// reading came from, and a red on a borrowed window is a red a reader can ask about again
    /// alone.
    /// </para>
    /// </summary>
    public bool Lent { get; }

    /// <summary>What it earned, derived by <see cref="RunVerdict"/> and set by nobody.</summary>
    public RunVerdict Verdict { get; }

    /// <summary>Every step the run recorded, in order, with the ordinals the results refer to.</summary>
    public IReadOnlyList<TraceStep> Trace { get; }

    /// <summary>The declared steps the run stopped short of, in declared order. Empty where it finished.</summary>
    public IReadOnlyList<StepDeclaration> NotReached { get; }

    /// <summary>How many of the case's steps were attempted.</summary>
    public int Reached => Declared.Steps.Count - NotReached.Count;

    /// <summary>Whether every declared step was attempted.</summary>
    public bool Finished => NotReached.Count == 0;

    /// <summary>The one line a summary shows: what it was, what it earned, and what it skipped.</summary>
    public override string ToString()
    {
        var short_fall = Finished
            ? ""
            : $", stopping {NotReached.Count} step{(NotReached.Count == 1 ? "" : "s")} short";

        // Only where either is true of this run. A line saying "as it comes, owned" of every case in
        // a suite that shares nothing is two words nobody reads by the third line.
        var where = Against.Samples ? $", sampling {Against.Environment}" : "";
        var window = Lent ? ", on a borrowed window" : "";

        return $"{Declared.Name}: {Verdict.Outcome} over {Verdict.Ran} of {Declared.Checks} check(s)"
            + $"{short_fall}{where}{window}";
    }
}

/// <summary>
/// The engine. It owns the loop, the waits, the attempts and the verdict, so a case owns none of
/// them.
/// <para>
/// Every decision here is one claude-tray's harness made eight times, once per case, and made
/// differently each time: how long to wait for a read-back, whether to look again, how many goes an
/// act gets, what a missing read-back does to the exit code, and which trace ordinal a failure
/// line points at. None of them is a property of the case. All of them are properties of the run,
/// which is why they live in one place that every case is run by.
/// </para>
/// <para>
/// Three of its rules are worth saying out loud. An act is attempted again only where the verb
/// survives being repeated and the step said what it was waiting for — retrying a toggle towards
/// a state is how a green becomes a red about the opposite one. A step that throws stops the case,
/// because every step after it was written expecting the act before it to have landed, and
/// continuing produces reds about a window nobody put into the state they describe. And the
/// diagnosis is taken once, after the last attempt, rather than on every attempt that missed: the
/// dump a reader wants is the window as it stood when the run gave up.
/// </para>
/// </summary>
public static class CaseRun
{
    /// <summary>
    /// Run one case against a root element.
    /// </summary>
    /// <param name="declared">The case.</param>
    /// <param name="root">What its locators resolve under, usually the window under test.</param>
    /// <param name="project">The declaration the waits, the attempts and the refusals come from.</param>
    /// <param name="budget">How many elements a red's control view may show.</param>
    /// <param name="measured">
    /// What this machine turned out to have. A case declaring a requirement this set says is absent
    /// does not run at all, and every check in it is unchecked and names the absence. Null where the
    /// caller measured nothing, and then a case that declares a requirement is refused rather than
    /// run against an answer nobody took.
    /// </param>
    /// <param name="lent">That <paramref name="root"/> is a window this case borrowed rather than owns.</param>
    /// <returns>The verdict, the trace, and whatever the run did not reach.</returns>
    public static CaseResult Of(
        CaseDeclaration declared,
        AutomationElement root,
        ProjectDeclaration project,
        int budget = Diagnosis.DefaultBudget,
        PreconditionSet? measured = null,
        bool lent = false)
    {
        ArgumentNullException.ThrowIfNull(declared);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(project);

        if (lent && !declared.OnlyReads)
        {
            throw new ScenarioRefusedException(
                declared.Name,
                "it was lent a window and does not say it only reads, so the next case would inherit whatever it changed");
        }

        var trace = new List<TraceStep>();
        var results = new List<AssertionResult>();
        var broke = new List<HarnessError>();
        var stopped = -1;

        // WW61. Before the first act, not after the first red: a case whose precondition is absent
        // would otherwise fail on the step that could not find what the absence explains, and the
        // reader of that red goes looking for a defect in the application.
        if (Absent(declared, measured) is { } missing)
            return NotOn(declared, missing, lent);

        for (var index = 0; index < declared.Steps.Count; index++)
        {
            var step = declared.Steps[index];
            var subject = new Subject(root, step.Locator, project);
            if (step.MeansIt)
                subject = subject.MeaningIt();

            try
            {
                Perform(step, subject, project, budget, trace, results);
            }
            catch (Exception thrown) when (thrown is not (OutOfMemoryException or StackOverflowException))
            {
                trace.Add(new TraceStep
                {
                    Step = trace.Count + 1,
                    Verb = step.Verb.Name,
                    Locator = step.Locator.Text,
                    Verdict = StepVerdict.Threw,
                    Detail = thrown.Message,
                });

                broke.Add(HarnessError.At(trace.Count, step.Name, thrown));
                stopped = index;
                break;
            }
        }

        var notReached = new List<StepDeclaration>();
        if (stopped >= 0)
            for (var index = stopped + 1; index < declared.Steps.Count; index++)
                notReached.Add(declared.Steps[index]);

        return new CaseResult(
            declared,
            RunVerdict.Over(results, broke),
            new ReadOnlyCollection<TraceStep>(trace),
            new ReadOnlyCollection<StepDeclaration>(notReached),
            lent);
    }

    /// <summary>
    /// The first requirement this case declares that the machine does not have, or null where it
    /// declares none or has them all.
    /// </summary>
    /// <exception cref="ScenarioRefusedException">
    /// Where the case declares a requirement and nothing measured anything. A run answering "it
    /// needs two profiles" with silence is a run that does not know whether it looked, and reporting
    /// either a pass or a hole from there invents the half it does not have.
    /// </exception>
    private static Precondition? Absent(CaseDeclaration declared, PreconditionSet? measured)
    {
        if (declared.Needs.Count == 0)
            return null;

        if (measured is null)
        {
            throw new ScenarioRefusedException(
                declared.Name,
                $"it needs {string.Join(", ", declared.Needs)}, and nothing measured whether this machine has any of it");
        }

        return measured.FirstAbsent(declared.Name, declared.Needs);
    }

    /// <summary>
    /// The reading of a case that did not run: every check in it unchecked, naming the absence, and
    /// a trace line per step saying it was never attempted.
    /// <para>
    /// A hole and not a skip. The steps are recorded because what did not run is part of the reading
    /// — the same reason the verdict has a third word at all — and the outcome comes out degraded
    /// rather than passed, so nothing about this reads as a green.
    /// </para>
    /// </summary>
    private static CaseResult NotOn(CaseDeclaration declared, Precondition missing, bool lent)
    {
        var trace = new List<TraceStep>();
        var results = new List<AssertionResult>();

        foreach (var step in declared.Steps)
        {
            trace.Add(new TraceStep
            {
                Step = trace.Count + 1,
                Verb = step.Verb.Name,
                Locator = step.Locator.Text,
                Verdict = StepVerdict.Unchecked,
                Detail = missing.Absence,
            });

            if (step.Checkable)
                results.Add(AssertionResult.Unchecked(step.Name, missing).At(trace.Count));
        }

        return new CaseResult(
            declared,
            RunVerdict.Over(results),
            new ReadOnlyCollection<TraceStep>(trace),
            [],
            lent);
    }

    /// <summary>
    /// One step: act, wait for what it said, record both, and count the check. The act's record and
    /// the expectation's are two lines rather than one, because they are two things that can be
    /// true separately — an act that landed on an element whose value then never moved is the case
    /// this project was built to report, and one merged line loses it.
    /// </summary>
    private static void Perform(
        StepDeclaration step,
        Subject subject,
        ProjectDeclaration project,
        int budget,
        List<TraceStep> trace,
        List<AssertionResult> results)
    {
        var cap = step.Retryable ? project.Attempts : 1;
        var attempted = Retry.Bounded(() => Attempting(step, subject), one => one.Held, cap);
        var acted = attempted.Last.Acted;

        trace.Add(Retry.Recorded(acted.AsTraceStep() with { Step = trace.Count + 1 }, attempted));

        if (attempted.Last.Expected is not { } expectation)
            return;

        // Read now rather than kept from the poll that missed: what a reader wants is the window as
        // it stood when the run gave up, and the tree as it was one attempt ago is a page about a
        // moment that had not failed yet.
        if (!expectation.Held)
        {
            expectation = expectation.Explaining(
                Diagnosis.OfWindow(expectation.AsAssertion(), subject.Window, acted.Element, budget));
        }

        trace.Add(expectation.AsTraceStep() with { Step = trace.Count + 1 });
        results.Add(expectation.AsAssertion().At(trace.Count));
    }

    private static Landed Attempting(StepDeclaration step, Subject subject)
    {
        var acted = step.Verb.Perform(subject, step.Argument);
        if (step.Expected is not { } wanted)
            return new Landed(acted, null);

        // Expect.That rather than Expect.Of: the diagnosis is a window dump, and taking one per
        // missed attempt pays for three of them to report the last.
        return new Landed(
            acted,
            Expect.That(
                step.Name,
                wanted,
                () =>
                {
                    var look = subject.ReadOnce();
                    return look.Found ? step.Reads.Of(look.Values) : null;
                },
                subject.ActMs,
                subject.PollMs));
    }

    /// <summary>
    /// One go at a step: what the act reported, and what the wait afterwards saw. A step with
    /// nothing to wait for holds by having acted, which is what makes one attempt of it the cap.
    /// </summary>
    private sealed record Landed(ActResult Acted, Expectation? Expected)
    {
        internal bool Held => Expected?.Held ?? true;
    }
}

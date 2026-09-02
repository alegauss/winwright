using System.Collections.ObjectModel;
using System.Windows.Automation;

using Winwright.Asserting;
using Winwright.Locating;
using Winwright.Processes;
using Winwright.Projects;
using Winwright.Verdicts;
using Winwright.Windowing;

namespace Winwright.Scenarios;

/// <summary>One case a run left alone, and why it did.</summary>
/// <param name="Case">The case, by the name it is declared under.</param>
/// <param name="Because">Why it was not run, in the sentence a reader acts on.</param>
public sealed record NotRun(string Case, string Because)
{
    /// <summary>The one line a report shows.</summary>
    public override string ToString() => $"{Case} — {Because}";
}

/// <summary>
/// The reading of a run over several cases: the ones that ran, the ones that did not, and the
/// outcome that follows.
/// <para>
/// WW59. <see cref="Skipped"/> is why this type exists rather than a list of
/// <see cref="CaseResult"/>. A filtered run reporting success without qualification is the same
/// silent pass the third verdict exists to prevent, one level up — so what did not run is a field,
/// it is in the sentence, and the sentence says it whether or not anything failed.
/// </para>
/// <para>
/// The outcome is the worst reading any case earned, for the same reason a run's is the worst
/// reading any assertion earned. It is not degraded by a case being left out: a case nobody asked
/// for is not a hole, and collapsing the two would make every filtered run look damaged. What the
/// filter costs is stated instead.
/// </para>
/// </summary>
public sealed class SuiteVerdict
{
    internal SuiteVerdict(Selection asked, IReadOnlyList<CaseResult> ran, IReadOnlyList<NotRun> skipped)
    {
        Asked = asked;
        Ran = ran;
        Skipped = skipped;

        // The same precedence a single run reads, and not the enum's order — those differ. A run
        // that broke outranks everything, then a failure outranks a hole, and a hole is never a
        // pass. Taking the largest member value would rank a hole above a failure, which is the
        // one comparison the numbers happen to get backwards.
        var readings = ran.Select(one => one.Verdict.Outcome).ToList();
        Outcome = readings.Contains(RunOutcome.Broken) ? RunOutcome.Broken
            : readings.Contains(RunOutcome.Failed) ? RunOutcome.Failed
            : readings.Contains(RunOutcome.Degraded) ? RunOutcome.Degraded
            : readings.Count > 0 ? RunOutcome.Passed
            : RunOutcome.Degraded;
    }

    /// <summary>What the run was asked for.</summary>
    public Selection Asked { get; }

    /// <summary>Every case that ran, in the order it ran.</summary>
    public IReadOnlyList<CaseResult> Ran { get; }

    /// <summary>Every case that did not, each carrying why.</summary>
    public IReadOnlyList<NotRun> Skipped { get; }

    /// <summary>The one of four readings the whole run earned.</summary>
    public RunOutcome Outcome { get; }

    /// <summary>The process exit code, which is the outcome itself and not a second mapping.</summary>
    public int ExitCode => (int)Outcome;

    /// <summary>Every case that did not pass, in the order it ran.</summary>
    public IReadOnlyList<CaseResult> Unhappy => new ReadOnlyCollection<CaseResult>(
        Ran.Where(one => one.Verdict.Outcome != RunOutcome.Passed).ToList());

    /// <summary>How many assertions actually ran across every case.</summary>
    public int Assertions => Ran.Sum(one => one.Verdict.Ran);

    /// <summary>
    /// The one line a report opens with. It states what did not run before it states the outcome,
    /// because that is the order the reader needs them in: a pass over two of nine cases is a
    /// different claim from a pass, and putting the qualification second is how it gets skimmed.
    /// </summary>
    public string Sentence()
    {
        var total = Ran.Count + Skipped.Count;
        var of = Skipped.Count == 0
            ? $"all {total} case{(total == 1 ? "" : "s")}"
            : $"{Ran.Count} of {total} cases, {Skipped.Count} not run";

        return $"{Outcome}: {of}, {Assertions} assertion{(Assertions == 1 ? "" : "s")} over {Asked}.";
    }

    /// <summary>
    /// Every case that ran without saying what it exists to catch, in the order it ran.
    /// <para>
    /// WW63. A check nobody can justify is a check nobody dares delete and nobody dares change, so
    /// the count is on the reading rather than left to whoever last looked. It never changes the
    /// outcome: a case that ran and passed did what it said, and an unjustified pass is a problem
    /// about the suite rather than about the build.
    /// </para>
    /// </summary>
    public IReadOnlyList<CaseDeclaration> Unjustified => CaseDeclaration.Unjustified(Ran.Select(one => one.Declared));

    /// <summary>The whole reading as a report prints it: the sentence, then a line for each case.</summary>
    public IReadOnlyList<string> Render()
    {
        var lines = new List<string> { Sentence() };
        lines.AddRange(Ran.Select(one => $"  {one}"));
        lines.AddRange(Skipped.Select(one => $"  {one}"));

        if (Unjustified.Count > 0)
        {
            lines.Add(
                $"{Unjustified.Count} case{(Unjustified.Count == 1 ? "" : "s")} that ran say nothing about what "
                + "they catch, so nothing says what deleting them would cost:");
            lines.AddRange(Unjustified.Select(one => $"  {one.Name}"));
        }

        return new ReadOnlyCollection<string>(lines);
    }

    /// <summary>The sentence, which is what a caller printing one line wants.</summary>
    public override string ToString() => Sentence();
}

/// <summary>
/// Running the cases a selection asked for, and saying what it left alone.
/// <para>
/// Two doors. <see cref="Run"/> drives every case against a window the caller already has, which is
/// what a case run against something attached needs. <see cref="Launch"/> starts the application
/// under test per case, out of the fixture that case declares — and lends one window to several
/// cases only where the fixture says it may be lent, every case using it says it only reads, and the
/// invocation asked for sharing. All three, because each answers a different question and no two of
/// them are the same person's to answer.
/// </para>
/// </summary>
public static class Suite
{
    /// <summary>How long a launched window is waited for before the case is a harness error.</summary>
    public const string WindowTimeout = "launch";

    /// <summary>
    /// Launch the application under test per fixture and run what <paramref name="asked"/> selects.
    /// <para>
    /// Sharing is opted into here rather than declared by the cases, and that is the property WW62
    /// is careful about: three cases in claude-tray drive the same window and each used to pay the
    /// launch, the first layout pass and the first poll, for a window none of them leaves in a state
    /// the next would reject. Merging them into one case would have fixed the seconds and lost the
    /// thing worth having — a case that still runs alone, owning its process and its first paint.
    /// </para>
    /// </summary>
    /// <param name="declared">Every case there is, in declared order.</param>
    /// <param name="asked">What to run.</param>
    /// <param name="register">What launches and stops the processes. Nothing is started outside it.</param>
    /// <param name="project">The declaration the executable, the waits and the attempts come from.</param>
    /// <param name="sharing">That this invocation will lend one window to the cases that only read it.</param>
    /// <param name="measured">What this machine turned out to have.</param>
    /// <exception cref="ScenarioRefusedException">Where there is nothing to run, or a selector matches no case.</exception>
    public static SuiteVerdict Launch(
        IReadOnlyList<CaseDeclaration> declared,
        Selection asked,
        ProcessRegister register,
        ProjectDeclaration project,
        bool sharing = false,
        PreconditionSet? measured = null)
    {
        ArgumentNullException.ThrowIfNull(declared);
        ArgumentNullException.ThrowIfNull(asked);
        ArgumentNullException.ThrowIfNull(register);
        ArgumentNullException.ThrowIfNull(project);

        Something(declared);
        Undeclared(declared, project);
        _ = asked.Over(declared);

        var lending = sharing ? Lendable(declared, asked) : [];
        var lent = new Dictionary<string, AutomationElement>(StringComparer.Ordinal);
        var ran = new List<CaseResult>();
        var skipped = new List<NotRun>();

        foreach (var one in declared)
        {
            if (asked.Leaves(one) is { } because)
            {
                skipped.Add(new NotRun(one.Name, because));
                continue;
            }

            var fixture = one.Fixture;
            var borrowing = lending.Contains(fixture.Name);
            if (borrowing && lent.TryGetValue(fixture.Name, out var already))
            {
                ran.Add(CaseRun.Of(one, already, project, Diagnosis.DefaultBudget, measured, lent: true));
                continue;
            }

            var (window, process) = Opened(register, project, fixture, one.Name);
            if (borrowing)
                lent[fixture.Name] = window;

            // The first case through a lent fixture pays the launch and does not borrow it: it is
            // the one that owns the window until the run ends, so the reading it takes is the same
            // reading it would take alone.
            CaseResult result;
            try
            {
                result = CaseRun.Of(one, window, project, Diagnosis.DefaultBudget, measured, lent: false);
            }
            finally
            {
                // WW215. Given back here and not at the end of the run: a case that owns its process
                // owns it for as long as it is running and no longer. A lent window is the exception
                // and not the rule — it is held precisely as long as it is being lent, which is
                // until the run ends, so that is the one launch not stopped where it was made.
                //
                // In a finally for the reason the register is total by construction rather than by
                // discipline: a case that throws on the way out still finished, and the promise is
                // about the window being gone, not about how the case ended.
                if (!borrowing)
                    register.Stop(process);
            }

            // WW279. After the stop, because the stop is where the register looks: a resident launch
            // draws no window and so has nothing whose absence would have noticed it going, and a
            // process that had left before the case finished is what a reading full of missing
            // locators is actually about. A borrowed launch is not stopped here and gets this at the
            // end of the run instead, where no one case can claim it.
            ran.Add(process.Departed is { } departed ? result.After(departed) : result);
        }

        return new SuiteVerdict(asked, new ReadOnlyCollection<CaseResult>(ran), new ReadOnlyCollection<NotRun>(skipped));
    }

    /// <summary>
    /// The fixtures this invocation may lend, by name: shareable, asked for by more than one
    /// selected case, and every one of those cases saying it only reads.
    /// <para>
    /// Every one of those three. A fixture two cases share where one of them acts is a fixture that
    /// hands the second case whatever the first left behind, and the red it produces is about the
    /// order the run happened to walk them in.
    /// </para>
    /// </summary>
    private static HashSet<string> Lendable(IReadOnlyList<CaseDeclaration> declared, Selection asked)
    {
        var wanted = declared.Where(one => asked.Takes(one)).ToList();
        var lendable = new HashSet<string>(StringComparer.Ordinal);

        foreach (var group in wanted.GroupBy(one => one.Fixture.Name, StringComparer.Ordinal))
        {
            if (group.Count() > 1 && group.First().Fixture.Shareable && group.All(one => one.OnlyReads))
                lendable.Add(group.Key);
        }

        return lendable;
    }

    /// <summary>
    /// Launch that fixture and wait for the window it draws.
    /// <para>
    /// Hands back the process as well as the window. WW215: a caller that only gets the window has
    /// no way to give the launch back, and the register is the only thing allowed to end one — so
    /// discarding it here was what made a run hold every window it had ever opened.
    /// </para>
    /// </summary>
    /// <exception cref="ScenarioRefusedException">
    /// Where the launch draws no window inside the project's own launch budget. A refusal rather
    /// than a red: nothing about the case was observed, so nothing about the application was.
    /// </exception>
    private static (AutomationElement Window, LaunchedProcess Process) Opened(
        ProcessRegister register, ProjectDeclaration project, FixtureDeclaration fixture, string named)
    {
        var launched = register.Launch(fixture.Starting(project.Executable));
        var deadline = project.Timeouts.For(WindowTimeout);

        // WW257. A tray draws no window, and the window in that case is what a click on the icon is
        // supposed to produce — so waiting for one makes the thing being asserted a reason not to run.
        // The desktop is what its locators resolve against, because that is where a tray icon lives.
        //
        // What this does not do is judge whether the process is still there. Asking once at the launch
        // is a race — a process that exits takes a moment to do it — and asking with a wait is the
        // window deadline paid by every tray that behaves. A resident launch that died is reported by
        // the case's own reds, which name locators that found nothing, and telling that apart from a
        // tray that is running and wrong is its own task.
        if (fixture.Resident)
            return (AutomationElement.RootElement, launched);

        var drawn = Attempt.UntilTrue(
            () => TopLevelWindows.Largest(launched.Pid) is not null,
            deadline,
            project.Timeouts.For("poll"));

        if (!drawn.Happened)
        {
            throw new ScenarioRefusedException(
                named,
                $"'{fixture.Name}' drew no window in {deadline}ms, so nothing about this case was observed");
        }

        return (AutomationElement.FromHandle(TopLevelWindows.Largest(launched.Pid)!.Handle), launched);
    }

    private static void Something(IReadOnlyList<CaseDeclaration> declared)
    {
        if (declared.Count == 0)
            throw new ScenarioRefusedException("<no cases>", "there is nothing to run, which is not the same as a run that passed");
    }

    /// <summary>
    /// Refuse a step whose verb needs a declaration the project does not carry. WW348.
    /// <para>
    /// This format's founding rule is that a case which could not run anywhere is refused before it
    /// runs here — an unparseable locator, a verb that does not exist, an argument beside a verb that
    /// takes none. A capture step in a project with nowhere to put pictures is the same kind of fact:
    /// it is about the file and the declaration beside it rather than about the desk, and it is
    /// knowable the moment both have been read. What kept it at run time was only where the reading
    /// happens — <c>StepDeclaration.Of</c> judges a step and is handed no project, and
    /// <c>ScenarioFile.LoadAll</c> reads files and is handed no project either. This is the first
    /// place that has both, and it is still before a window is launched, which is what the rule buys.
    /// </para>
    /// <para>
    /// Over every case and not only the ones the selection takes, because that is what the rule
    /// claims: the file is wrong whoever asked for what, and a run narrowed to one case would
    /// otherwise pass while the same declaration is still missing for the next one. The run-time hole
    /// WW336 wrote stays where it is — a caller reaching <c>CaseRun.Of</c> without coming through
    /// here still gets an answer rather than a throw, and this is the earlier of two rather than a
    /// replacement for it.
    /// </para>
    /// </summary>
    /// <exception cref="ScenarioRefusedException">Where a step needs a key the project has not declared.</exception>
    private static void Undeclared(IReadOnlyList<CaseDeclaration> declared, ProjectDeclaration project)
    {
        foreach (var one in declared)
        {
            foreach (var step in one.Steps)
            {
                // Asked of the verb rather than decided here, so the suite never learns which act
                // needs which key: a second verb needing a second declaration is refused by this same
                // loop the day it is added to the vocabulary.
                if (step.Verb.Needs is not { Length: > 0 } key || project.Declares(key))
                    continue;

                throw new ScenarioRefusedException(
                    one.Name,
                    $"'{step.Name}' names '{step.Verb.Name}', which needs a '{key}' this project does not "
                        + $"declare — add it to {project.Path}, because a run would launch the application "
                        + "and drive it to that step to say what these two files already say together");
            }
        }
    }

    /// <summary>
    /// Run what <paramref name="asked"/> selects out of <paramref name="declared"/>.
    /// </summary>
    /// <param name="declared">Every case there is, in declared order.</param>
    /// <param name="asked">What to run. <see cref="Selection.All"/> for all of it.</param>
    /// <param name="root">What every case's locators resolve under.</param>
    /// <param name="project">The declaration the waits, the attempts and the refusals come from.</param>
    /// <param name="budget">How many elements a red's control view may show.</param>
    /// <param name="measured">What this machine turned out to have, read by every case declaring a requirement.</param>
    /// <exception cref="ScenarioRefusedException">
    /// Where there is nothing to run, or a selector matches no case. Both are refused rather than
    /// run, because a run of no cases has no failure in it and reads as a pass.
    /// </exception>
    public static SuiteVerdict Run(
        IReadOnlyList<CaseDeclaration> declared,
        Selection asked,
        AutomationElement root,
        ProjectDeclaration project,
        int budget = Diagnosis.DefaultBudget,
        PreconditionSet? measured = null)
    {
        ArgumentNullException.ThrowIfNull(declared);
        ArgumentNullException.ThrowIfNull(asked);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(project);

        Something(declared);
        Undeclared(declared, project);

        // Called for its refusal and not for its answer: a selector matching nothing has to stop the
        // run before the first case, or the run is a green about the cases the typo left out.
        _ = asked.Over(declared);

        var ran = new List<CaseResult>();
        var skipped = new List<NotRun>();
        foreach (var one in declared)
        {
            if (asked.Leaves(one) is { } because)
                skipped.Add(new NotRun(one.Name, because));
            else
                ran.Add(CaseRun.Of(one, root, project, budget, measured));
        }

        return new SuiteVerdict(asked, new ReadOnlyCollection<CaseResult>(ran), new ReadOnlyCollection<NotRun>(skipped));
    }
}

using System.Collections.ObjectModel;
using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Asserting;
using Winwright.Capturing;
using Winwright.Locating;
using Winwright.Processes;
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
        bool lent = false,
        int across = 1,
        Departure? departed = null)
    {
        Declared = declared;
        Verdict = verdict;
        Trace = trace;
        NotReached = notReached;
        Lent = lent;
        Across = across;
        Departed = departed;
    }

    /// <summary>
    /// How many members of a derived set this case ran over. One where it repeats over nothing.
    /// <para>
    /// WW263. A repeated case drives the window once per member, and what was visited is what the
    /// script counted apart from what was asserted — so this is where that number comes from, rather
    /// than from the case.
    /// </para>
    /// <para>
    /// WW276. It is no longer a multiplier on the checks. A case walking six panels asserts one rule
    /// six times and those are one claim, gathered into one result over the whole walk — so the line
    /// reads <em>3 of 3 check(s) across 6 member(s)</em>, and the count of checks is the case's own.
    /// </para>
    /// </summary>
    public int Across { get; }

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

    /// <summary>
    /// That the application had already gone when the run came to stop it, or null where it had not.
    /// <para>
    /// WW279. The reading is not changed by it and the line is: a launch that exited really did fail,
    /// so the reds stay reds, and what was wrong was that they named locators on a desk where nothing
    /// had ever been drawn. Left out of the outcome for the other reason — a case whose last step
    /// closes the application ends with the process gone on purpose, and degrading that would make the
    /// shutdown case impossible to write.
    /// </para>
    /// </summary>
    public Departure? Departed { get; }

    /// <summary>Every step the run recorded, in order, with the ordinals the results refer to.</summary>
    public IReadOnlyList<TraceStep> Trace { get; }

    /// <summary>The declared steps the run stopped short of, in declared order. Empty where it finished.</summary>
    public IReadOnlyList<StepDeclaration> NotReached { get; }

    /// <summary>How many of the case's steps were attempted.</summary>
    public int Reached => Declared.Steps.Count - NotReached.Count;

    /// <summary>Whether every declared step was attempted.</summary>
    public bool Finished => NotReached.Count == 0;

    /// <summary>
    /// The same reading, saying that the application had gone before the run asked it to stop.
    /// <para>
    /// WW279. Read after the stop and therefore added afterwards, which is why this exists rather
    /// than a field the constructor takes: the run's own look at the process happens where the case
    /// finishes, and by then the result has been built.
    /// </para>
    /// </summary>
    /// <param name="departure">What the register found.</param>
    internal CaseResult After(Departure departure) =>
        new(Declared, Verdict, Trace, NotReached, Lent, Across, departure);

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

        var over = Across == 1
            ? $"{Declared.Checks} check(s)"
            : $"{Declared.Checks} check(s) across {Across} member(s)";

        // WW279. Before the outcome, because a reader who meets "Failed over 0 of 3 check(s)" first
        // goes to the locator lines, and every one of those names a control on a desk the application
        // had already left. The clause says the exit and never what it caused — "exited with 0" beside
        // "Passed" is a case that closed the thing under test on purpose, and the same clause reads
        // correctly there without a second branch nothing in the suite would exercise.
        var died = Departed is null ? "" : $"the application {Departed} — ";

        return $"{Declared.Name}: {died}{Verdict.Outcome} over {Verdict.Ran} of {over}"
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
    /// What a `never` whose absence rests on a truncated walk is missing. WW189's rule as a
    /// precondition: seeing the string is evidence a short walk cannot take away, and not seeing it
    /// is only an answer where the walk reached everything.
    /// </summary>
    private const string WalkedWhole = "the whole window could be walked";

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

        // WW255. Which steps a later one claims its reading is back to, worked out once. Only these
        // are read again after they run: remembering every step's reading would pay for a resolve per
        // step on every case in the suite to serve the two or three that ever point backwards.
        var pointedAt = declared.Steps
            .Select(one => one.PointsAt)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
        var recalled = new Dictionary<string, string?>(StringComparer.Ordinal);

        // WW240. What language the window this case was launched against is in, where the fixture
        // said. A derived set then reads the strings that window is actually showing, rather than the
        // one file a project had to pretend was the only one it ships.
        var speaking = declared.Fixture.Speaking;

        // WW263. The steps as they will actually be run: once as declared, or once per member of the
        // set the case repeats over, with the member substituted into every locator that names it.
        // Derived before the first act for the reason the precondition below is — a set that cannot be
        // derived is a case that is wrong, and finding that out halfway through is a window driven for
        // nothing.
        var members = Members(declared, project, speaking);
        var running = new List<StepDeclaration>();

        // WW295. One per run, so a locator asking the application what to select costs one launch
        // however many steps and members carry it.
        var reported = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var member in members)
        {
            foreach (var step in declared.Steps)
                running.Add(Naming(member is null ? step : step.For(member), project, speaking, reported));
        }

        // WW61. Before the first act, not after the first red: a case whose precondition is absent
        // would otherwise fail on the step that could not find what the absence explains, and the
        // reader of that red goes looking for a defect in the application.
        if (Absent(declared, measured) is { } missing)
            return NotOn(declared, missing, lent);

        // WW276. Which claim each result belongs to, recorded as the run goes: a step adds none, one
        // or two results depending on what it claimed and how far it got, so nothing after the fact
        // can work it out by counting.
        var claims = new List<string>();

        // WW336. This case's own folder for the pictures it asks for, or null where the project says
        // nowhere. Once for the case and not per step, because it is named for the case — and asked
        // for even where no step captures, which costs one dictionary lookup and keeps the branch
        // out of the step loop.
        var pictures = project.Declares("captures")
            ? System.IO.Path.Combine(project.Captures, Filed(declared.Name))
            : null;

        // WW343. Every tray menu this case opened, so the desk can be given back where the case ends
        // rather than where the step does. A menu is opened by one step and read by the next, and a
        // restore at the step would set the foreground back while the menu was standing — which
        // closes a drop-down, so the tidying would dismiss the answer the case came for.
        var opened = new List<TrayMenu>();

        try
        {
            Stepping(
                running, root, project, budget, pictures, trace, results, broke, claims,
                pointedAt, recalled, speaking, opened, out stopped);
        }
        finally
        {
            // In a finally for the reason the step's own flyout close is in one: the promise is
            // about the desk and not about how the case ended. A case that threw took the same
            // foreground as one that passed.
            HandedBack(opened, project, trace);
        }

        var notReached = new List<StepDeclaration>();
        if (stopped >= 0)
            for (var index = stopped + 1; index < running.Count; index++)
                notReached.Add(running[index]);

        return new CaseResult(
            declared,
            RunVerdict.Over(Walked(results, claims, members.Count), broke),
            new ReadOnlyCollection<TraceStep>(trace),
            new ReadOnlyCollection<StepDeclaration>(notReached),
            lent,
            members.Count);
    }

    /// <summary>
    /// Give back what the case took from the desk. WW343.
    /// <para>
    /// In reverse, because each menu recorded who held the foreground before <em>it</em> ran: putting
    /// the first one back last is what leaves the desk where the case found it rather than where its
    /// second-to-last act did.
    /// </para>
    /// <para>
    /// Best effort, and the failure is not silent. WW330's rule is that a desk this run could not
    /// work is not worth failing a case that has already read what it came for — and the session that
    /// rule came out of was lost to a leak nobody could see, so a restore that did not hold is
    /// recorded as a hole in the trace. A restore that held is not: a tidy that worked is not news.
    /// </para>
    /// </summary>
    /// <param name="opened">The menus this case opened, in the order it opened them.</param>
    /// <param name="project">The declaration, for how long shutting the flyout may take.</param>
    /// <param name="trace">Where a restore that did not hold is recorded.</param>
    private static void HandedBack(
        List<TrayMenu> opened, ProjectDeclaration project, List<TraceStep> trace)
    {
        if (opened.Count == 0)
            return;

        var settleMs = project.Timeouts.For("resolve");
        var pollMs = project.Timeouts.For("poll");

        for (var index = opened.Count - 1; index >= 0; index--)
        {
            var state = opened[index].PutBack(settleMs, pollMs);
            if (state.Held)
                continue;

            trace.Add(new TraceStep
            {
                Step = trace.Count + 1,
                Verb = "put the desk back",

                // The taskbar and not a locator, because that is what was acted on and this step
                // had none: a trace row with an empty subject reads as a step whose locator was
                // lost rather than as one that never had one.
                Locator = "the taskbar",
                Verdict = StepVerdict.Unchecked,
                Detail = state.Because ?? "the desk would not take it back and did not say why",
            });
        }
    }

    /// <summary>
    /// Run the case's steps, stopping at the first one whose act was never sent or which threw.
    /// <para>
    /// Its own method since WW343, which put the loop inside a <c>finally</c>: a body this long
    /// under a <c>try</c> is one where the reader has to hold both the stepping and the tidying at
    /// once, and the tidying is four lines that must not be read past.
    /// </para>
    /// </summary>
    /// <param name="running">The steps as they will actually be run.</param>
    /// <param name="root">The case's own window.</param>
    /// <param name="project">The declaration.</param>
    /// <param name="budget">How much of a diagnosis a failure may print.</param>
    /// <param name="pictures">This case's own folder for pictures, or null.</param>
    /// <param name="trace">Where each step is recorded.</param>
    /// <param name="results">Where each claim's result is added.</param>
    /// <param name="broke">Where a step that threw is recorded.</param>
    /// <param name="claims">Which claim each result belongs to.</param>
    /// <param name="pointedAt">Which steps a later one reads again.</param>
    /// <param name="recalled">What those steps read.</param>
    /// <param name="speaking">What language the window is in.</param>
    /// <param name="opened">Where a tray menu this case opened is kept, for the restore.</param>
    /// <param name="stopped">The index of the step that stopped the case, or -1.</param>
    private static void Stepping(
        List<StepDeclaration> running,
        AutomationElement root,
        ProjectDeclaration project,
        int budget,
        string? pictures,
        List<TraceStep> trace,
        List<AssertionResult> results,
        List<HarnessError> broke,
        List<string> claims,
        HashSet<string> pointedAt,
        Dictionary<string, string?> recalled,
        System.Globalization.CultureInfo? speaking,
        List<TrayMenu> opened,
        out int stopped)
    {
        stopped = -1;
        for (var index = 0; index < running.Count; index++)
        {
            var step = running[index];

            // WW258. The second kind of subject, answered before a subject is built rather than
            // inside one: the notification area is in the shell's tree and not in the window's, and a
            // tray icon has no clickable point either, so there is no locator to resolve and nothing
            // for `Subject` to hold. A step here never reaches any of the claims below, which is why
            // every one of them may still assume a locator.
            if (step.Tray is { } icon)
            {
                if (Trayed(step, icon, project, trace, results) is { } menu)
                    opened.Add(menu);

                while (claims.Count < results.Count)
                    claims.Add(step.Claimed);

                continue;
            }

            var subject = new Subject(root, step.Locator!, project);
            if (step.MeansIt)
                subject = subject.MeaningIt();

            try
            {
                // WW244. A step whose act was never sent stops the case, for the reason a step that
                // throws does: everything after it was written assuming it landed, and running those
                // produces reds about a window nobody put into the state they describe. Measured in
                // claude-tray, where a click that was never delivered left the case red about a text
                // box on a page that had never been opened.
                var went = Perform(
                step, subject, project, budget, pictures, trace, results, root, pointedAt, recalled, speaking);
                while (claims.Count < results.Count)
                    claims.Add(step.Claimed);

                if (!went)
                {
                    stopped = index;
                    break;
                }
            }
            catch (Exception thrown) when (thrown is not (OutOfMemoryException or StackOverflowException))
            {
                trace.Add(new TraceStep
                {
                    Step = trace.Count + 1,
                    Verb = step.Verb.Name,
                    Locator = step.Addressed,
                    Verdict = StepVerdict.Threw,
                    Detail = thrown.Message,
                });

                broke.Add(HarnessError.At(trace.Count, step.Name, thrown));
                while (claims.Count < results.Count)
                    claims.Add(step.Claimed);

                stopped = index;
                break;
            }
        }
    }

    /// <summary>
    /// A step that photographs the window its locator is inside. WW336.
    /// <para>
    /// The claim is the receipt and the whole of it: this picture is of this window, out of this
    /// process, with nothing standing over the region and nothing showing through the glass or the
    /// layer. Six ways of being wrong, every one of which a file on disk looks exactly the same as,
    /// and none of which a caller has to remember — <see cref="CaptureReceipt.Taking"/> asks them
    /// all around the write.
    /// </para>
    /// <para>
    /// Only the copy is performed here, and a window that wants a render is a hole rather than a
    /// red. A render draws the application's own visual tree, which is the safer picture and the one
    /// nothing outside that process can take; the in-app half exists for it, and saying so is more
    /// use than reaching for the screen and refusing on what the screen turned out to hold.
    /// </para>
    /// </summary>
    /// <param name="step">The step, whose argument is what to call the picture.</param>
    /// <param name="subject">What its locator resolves, which is inside the window being captured.</param>
    /// <param name="project">The declaration, for the sentence a missing directory names.</param>
    /// <param name="pictures">
    /// This case's own folder under the directory the project declared, or null where it declared
    /// none. Computed once for the case rather than per step, because it is named for the case: a
    /// step knows what it is called and not what case it is in, and two cases asking for "the menu"
    /// must not answer each other.
    /// </param>
    /// <param name="root">The case's own window, which is what "the main one" means here.</param>
    /// <param name="trace">Where the step is recorded.</param>
    /// <param name="results">Where its one result is added.</param>
    private static void Captured(
        StepDeclaration step,
        Subject subject,
        ProjectDeclaration project,
        string? pictures,
        AutomationElement root,
        List<TraceStep> trace,
        List<AssertionResult> results)
    {
        // Named before anything is read, so every arm below reports under the same words the case
        // wrote rather than under whichever the branch happened to build.
        var named = step.Name;

        if (pictures is null)
        {
            Hole(
                trace,
                results,
                named,
                Precondition.Absent(
                    "a directory to write pictures into",
                    $"{project.Path} declares no 'captures', and a case does not say where a picture "
                        + "goes — a path written into one is a case that means something else on the "
                        + "next machine"));

            return;
        }

        var admitted = Admitted.To(subject);
        var handle = admitted.Window;

        // The process is the window's own and never the case root's, which is measured rather than
        // tidied: a resident fixture draws no window, so its cases resolve against the desktop — and
        // the desktop's process is the shell's. Reading the pid off the root would then enumerate
        // explorer and find nothing, on exactly the shape this verb exists for.
        var target = AppTarget.AttachToWindow(handle);
        var found = Winwright.Windowing.TopLevelWindows
            .OfProcess(target.Pid, smallest: 0, visibleOnly: false)
            .FirstOrDefault(one => one.Handle == handle);

        if (found is null)
        {
            // The element resolved and the window it is in is not in the enumeration, which is a
            // shell rearranging itself rather than the application being wrong.
            Hole(
                trace,
                results,
                named,
                Precondition.Absent(
                    "the window the locator is inside",
                    $"{step.Addressed} resolved and the window holding it was not in the enumeration "
                        + "a moment later"));

            return;
        }

        // The case's own window is what "the main one" means, and never the largest of the process:
        // WW346 measured a tray whose largest window is the shadow behind its menu. A case resolving
        // against the desktop has no window of its own, which is the overload WW320 added — and the
        // one every tray case takes.
        var mine = root.Current.NativeWindowHandle;
        var route = mine == 0
            ? CaptureRoute.For(found)
            : CaptureRoute.For(found, found with { Handle = mine });

        if (route.Renders)
        {
            Hole(
                trace,
                results,
                named,
                Precondition.Absent(
                    "a surface the screen is the only way to",
                    $"{route.Sentence()} A render is the application's own to take — Winwright.InApp "
                        + "does it — and this engine cannot draw another process's tree"));

            return;
        }

        var into = System.IO.Path.Combine(pictures, $"{Filed(step.Argument!)}.png");
        var frame = PaintedFrame.Of(handle);
        var region = frame?.Painted ?? found.Bounds;

        try
        {
            var receipt = CaptureReceipt.Taking(
                into, found, target, file => ScreenCopy.Into(region, file), frame, route);

            trace.Add(receipt.AsTraceStep() with { Step = trace.Count + 1, Asserted = named });
            results.Add(AssertionResult.Pass(named, receipt.Sentence()).At(trace.Count));
        }
        catch (WrongCaptureException wrong)
        {
            // The arm decides which verdict, because the engine already has an opinion about each
            // and they are not the same opinion. A region somebody stood over and a window whose own
            // glass or layer transmits are facts about the desk and the window; a picture of another
            // process, of something nothing is drawing, or of one flat colour is the capture being
            // wrong about the application. Read off the arm and never off the sentence, for the
            // reason WW188 gave the arms in the first place.
            // And under the reading's own name where it is a hole, never one invented here: a
            // precondition is a thing a reader recognises across runs and a catalogue can hold, and
            // a name composed at the throw site is in no list at all — which is WW190's finding
            // about exactly this shape.
            var absent = wrong.Arm switch
            {
                Capturing.WrongCapture.RegionCovered or Capturing.WrongCapture.DeskChanged =>
                    Obstruction.PreconditionName,
                Capturing.WrongCapture.GlassTransmits => Glass.PreconditionName,
                Capturing.WrongCapture.LayerTransmits => SeeThrough.PreconditionName,
                _ => null,
            };

            trace.Add(new TraceStep
            {
                Step = trace.Count + 1,
                Verb = $"capture ({route})",
                Locator = step.Addressed,
                Resolved = found.ToString(),
                ReadBack = into,
                Verdict = absent is null ? StepVerdict.Failed : StepVerdict.Unchecked,
                Detail = wrong.Message,
                Asserted = named,
            });

            results.Add(
                (absent is null
                    ? AssertionResult.Fail(named, wrong.Message)
                    : AssertionResult.Unchecked(named, Precondition.Absent(absent, wrong.Message)))
                .At(trace.Count));
        }
    }

    /// <summary>One step that could not be taken, recorded as the hole it is. WW336.</summary>
    /// <param name="trace">Where the step is recorded.</param>
    /// <param name="results">Where its one result is added.</param>
    /// <param name="named">What the case calls it.</param>
    /// <param name="absent">What was missing.</param>
    private static void Hole(
        List<TraceStep> trace, List<AssertionResult> results, string named, Precondition absent)
    {
        trace.Add(new TraceStep
        {
            Step = trace.Count + 1,
            Verb = "capture",
            Locator = named,
            Verdict = StepVerdict.Unchecked,
            Detail = absent.Absence,
            Asserted = named,
        });

        results.Add(AssertionResult.Unchecked(named, absent).At(trace.Count));
    }

    /// <summary>
    /// What a name is called on disk: its words, with everything a path cannot carry replaced.
    /// <para>
    /// WW336. A case names a picture in the words a report shows it under — "the menu as it opens" —
    /// and those are not a file name. Replaced rather than stripped, so two names that differ only
    /// in punctuation stay two files.
    /// </para>
    /// </summary>
    /// <param name="named">The name the case wrote.</param>
    private static string Filed(string named)
    {
        var safe = named.Trim().ToCharArray();
        var bad = System.IO.Path.GetInvalidFileNameChars();

        for (var at = 0; at < safe.Length; at++)
            if (Array.IndexOf(bad, safe[at]) >= 0)
                safe[at] = '-';

        return new string(safe);
    }

    /// <summary>
    /// A step whose subject is a notification-area icon: ask whether the shell is showing it, and
    /// record the three-way answer the search already gives.
    /// <para>
    /// WW258. The claim is <em>this icon can be found</em> and nothing else, which is why no
    /// expectation is read here — <see cref="NotificationArea.Find"/> matches by the name the shell
    /// gives an icon, so an expectation about that name would compare the search's argument with its
    /// own answer.
    /// </para>
    /// <para>
    /// The three ways are the search's own, and taken from it rather than re-derived: found is a pass,
    /// not found having looked everywhere is a failure the case asked about, and not found because the
    /// overflow could not be opened is a hole naming the desk. That last one is WW168, and re-deriving
    /// it here is exactly how it would come back.
    /// </para>
    /// </summary>
    /// <returns>
    /// The menu this step opened, for the case to give the desk back with once every step that reads
    /// it has run, or null where it opened none. WW343.
    /// </returns>
    private static TrayMenu? Trayed(
        StepDeclaration step,
        string icon,
        ProjectDeclaration project,
        List<TraceStep> trace,
        List<AssertionResult> results)
    {
        var settleMs = project.Timeouts.For("resolve");
        var pollMs = project.Timeouts.For("poll");

        // WW258. Whether a flyout was standing before this step, so the step can put the desk back the
        // way it found it. `Find` opens the overflow to look in it and does not shut it again — which
        // is right for a caller that wants to keep looking, and wrong for a case: a flyout left
        // standing is the next case's flake, and this block's criterion is that a run leaves the
        // machine as it found it. Measured, and by the case that says so: `TrayPlacementTests`
        // went red on a flyout this step had opened and walked away from.
        var stood = NotificationArea.Overflow() is not null;

        // WW258. Asking for the menu is the other act a tray takes, and it answers its own three ways
        // for the same reason the search does — WW174 put the desk apart from the application here,
        // so a shell that would not open the flyout is a hole and not a menu that failed to appear.
        try
        {
            if (step.OpensTheTrayMenu)
            {
                var menu = NotificationArea.OpenMenu(icon, settleMs, pollMs);

                trace.Add(menu.AsTraceStep() with { Step = trace.Count + 1, Asserted = step.Name });
                results.Add(menu.AsAssertion(step.Name).At(trace.Count));

                // WW343. Handed up only where a menu is actually standing. Every arm that opened
                // none has already put the desk back itself — the act can, there, because there is
                // nothing left to lose — and handing those up would put it back twice.
                return menu.Opened ? menu : null;
            }

            var search = NotificationArea.Find(icon, openingTheOverflow: true, settleMs, pollMs);

            trace.Add(search.AsTraceStep(step.Name) with { Step = trace.Count + 1 });
            results.Add(search.AsAssertion(step.Name).At(trace.Count));
            return null;
        }
        finally
        {
            // Only what this step opened, which is the rule `Reachable` already keeps one floor down:
            // a flyout that was already standing belongs to whoever opened it, and shutting that one
            // would answer their next look with a closed desk. In a finally because the promise is
            // about the desk and not about how the step ended.
            //
            // WW343 asked whether this dismisses a menu the step just opened — the chevron takes an
            // invoke, the shell shuts the flyout, and a drop-down standing over it might well go
            // with it. Measured, by taking the line out and watching: the case below reads both
            // entries of the menu on the step after this one either way. So the flyout close stays
            // here where it belongs, and it is only the foreground that had nowhere to go.
            if (!stood && NotificationArea.Overflow() is not null)
                NotificationArea.CloseOverflow(settleMs, pollMs);
        }
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
                Locator = step.Addressed,
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
    /// One step: act, wait for what it said, record both, and count the check.
    /// <para>
    /// An act's record and its expectation's are two lines rather than one, because they are two
    /// things that can be true separately — an act that landed on an element whose value then never
    /// moved is the case this project was built to report, and one merged line loses it.
    /// </para>
    /// <para>
    /// A read is one line, for the same reason read the other way: there is no act for it to be
    /// separately true of. What tells the two apart in a trace is the pattern, which an act names
    /// and a read leaves empty.
    /// </para>
    /// </summary>
    /// <returns>
    /// Whether the case may go on. False where the act was never sent: WW244, and the whole of it is
    /// that the next step would then be read against a window nothing had moved.
    /// </returns>
    private static bool Perform(
        StepDeclaration step,
        Subject subject,
        ProjectDeclaration project,
        int budget,
        string? pictures,
        List<TraceStep> trace,
        List<AssertionResult> results,
        AutomationElement root,
        HashSet<string> pointedAt,
        Dictionary<string, string?> recalled,
        System.Globalization.CultureInfo? speaking)
    {
        var went = Performing(step, subject, project, budget, pictures, trace, results, root, recalled, speaking);

        // WW255. Read after the step rather than kept from inside it, and only for a step something
        // points back at. What a later step compares against is what this one left the window reading,
        // which is not the same as what it claimed: the claim can be `answers`, and then the value is
        // one nothing in the file ever named.
        if (went && pointedAt.Contains(step.Name))
            recalled[step.Name] = step.Reads.Of(subject.Read());

        return went;
    }

    /// <summary>The step itself, with the recall around it in <see cref="Perform"/>.</summary>
    private static bool Performing(
        StepDeclaration step,
        Subject subject,
        ProjectDeclaration project,
        int budget,
        string? pictures,
        List<TraceStep> trace,
        List<AssertionResult> results,
        AutomationElement root,
        Dictionary<string, string?> recalled,
        System.Globalization.CultureInfo? speaking)
    {
        // WW336. A capture is an act on the window the locator is inside rather than on the element,
        // and its claim is the receipt — so it goes the way the sweeps go and not through the attempt
        // loop: there is no reading at the end of it to retry towards, and a picture taken twice is
        // two pictures rather than one taken again.
        if (step.Verb.Captures)
        {
            Captured(step, subject, project, pictures, root, trace, results);
            return true;
        }

        // WW236. A sweep is one claim over many elements, so it does not go through the attempt loop —
        // it has its own wait, over the resolve budget, which WW241 gave it.
        if (step.Sweeps is { } key)
        {
            Swept(step, key, subject, project, root, trace, results, speaking);
            return true;
        }

        // WW256. A claim about the wait rather than about what it ended on, so it has its own wait for
        // the reason a sweep does: what the attempt loop retries towards is a reading, and this one
        // has nothing to read at the end — the answer is everything it saw on the way.
        if (step.Never is { } forbidden)
        {
            Watched(step, forbidden, subject, project, root, trace, results);
            return true;
        }

        // WW253. One claim over the elements under the locator, so it has its own wait for the reason
        // a sweep does: what it waits for is a subtree that has finished drawing, and there is no one
        // reading at the end of it to retry towards.
        if (step.Spoken)
        {
            Spoke(step, subject, trace, results);
            return true;
        }

        // WW262. A sweep over elements rather than over the strings a key declares, so it goes the way
        // `covers` goes and not through the attempt loop: what it waits for is a page that has finished
        // drawing, and there is no one reading at the end of it to retry towards.
        if (step.EachSpoken)
        {
            EachSpoke(step, subject, root, trace, results);
            return true;
        }

        // WW264. The pairing, which is a sweep over rows and everything inside them — so it goes the
        // way the other sweeps go and not through the attempt loop.
        if (step.OwnHeader)
        {
            Paired(step, subject, root, trace, results);
            return true;
        }

        // WW261 and WW270. Resolved once, before the attempts, and out of the language the fixture
        // said its window is in. A key that cannot be read is a scenario that is wrong rather than an
        // application that is: the refusal names the key and the file, and it arrives before anything
        // is compared rather than as a reading that answered nothing.
        string? declared = null;
        if ((step.Label ?? step.NotLabel ?? step.BeginsWithLabel) is { } declaring)
        {
            try
            {
                // The fixture's word where it gave one, and the way the application resolves it where
                // nothing did — which is what an attach has to do, there being no launch to have said.
                var language = speaking is null
                    ? ResolvedLanguage.Resolve(project)
                    : ResolvedLanguage.Speaking(speaking);

                declared = Labels.For(declaring, project, language).Text;
            }
            catch (UnusableLabelException unusable)
            {
                throw new ScenarioRefusedException(step.Name, unusable.Message);
            }
        }

        // WW294. Resolved beside the label above and for the same reason: both turn a name into the
        // value an expectation is about, and both must be answered before anything is compared. The
        // difference is only the well — one reads the strings the project ships, the other asks the
        // application what this machine is doing.
        //
        // Once and not per attempt: asking the application three times for one expectation is three
        // launches for one answer, and a value that changed between them would make a retry compare
        // against a moving target.
        if (step.ExpectReported is { } reported)
        {
            try
            {
                declared = DerivedSet.ReportedValue(step.Name, project, reported);
            }
            catch (UnderivableSetException underivable)
            {
                throw new ScenarioRefusedException(step.Name, underivable.Message);
            }
        }

        // WW255. Looked up once, before the attempts: the value a round trip is about was read when
        // the earlier step ran, and a lookup inside the retry would be the same answer fetched three
        // times. Absent only where that step stopped the case, which is a state this never reaches.
        var backTo = step.PointsAt is { } back && recalled.TryGetValue(back, out var read)
            ? read
            : null;

        var cap = step.Retryable ? project.Attempts : 1;
        var attempted = Retry.Bounded(() => Attempting(step, subject, backTo, declared), one => one.Held, cap);
        var landed = attempted.Last;

        if (landed.Acted is { } acted)
        {
            trace.Add(Retry.Recorded(acted.AsTraceStep() with { Step = trace.Count + 1 }, attempted));

            // WW225 and WW229. An act that was never attempted is a hole and never a red, and this is
            // where that becomes true of the verdict rather than only of the trace line. Measured: a
            // case naming 'nudge' under a run that did not own the foreground failed on the
            // expectation, which reports a control that would not move about a machine that never
            // pressed the key — the exact inversion this whole project exists to refuse.
            //
            // Before the expectation and not after it: what the step claimed was never put to the
            // application, so there is nothing for a diagnosis to be a page about.
            //
            // WW244: whether or not the step is checkable, which it used to require. A step with no
            // expectation is a navigation, and a navigation is exactly the one whose job is to put the
            // window into the state the next step reads — so a hole there was recorded in the trace
            // and in nothing the verdict counts. Measured in claude-tray: a click that was never
            // delivered left the case red about a text box on a page that had never been opened, and
            // the reader was sent to the application's own XAML to look for it.
            if (!acted.Attempted)
            {
                results.Add(AssertionResult.Unchecked(step.Name, acted.Needed!).At(trace.Count));
                return false;
            }
        }

        if (landed.Expected is not { } expectation)
            return true;

        // Read now rather than kept from the poll that missed: what a reader wants is the window as
        // it stood when the run gave up, and the tree as it was one attempt ago is a page about a
        // moment that had not failed yet.
        if (!expectation.Held)
        {
            expectation = expectation.Explaining(
                Diagnosis.OfWindow(expectation.AsAssertion(), subject.Window, landed.Saw, budget));
        }

        var recorded = expectation.AsTraceStep() with { Step = trace.Count + 1 };
        if (landed.Acted is null)
        {
            // The read's own line: the verb it was, the locator it read, and what it claimed. Not
            // the expectation's default shape, which names the assertion where the locator goes and
            // would leave a read with no line saying what it looked at.
            recorded = Retry.Recorded(
                recorded with
                {
                    Verb = step.Verb.Name,
                    Locator = step.Addressed,
                    Resolved = landed.Saw?.ToString(),
                    Asserted = step.Name,
                },
                attempted);
        }

        trace.Add(recorded);
        results.Add(expectation.AsAssertion().At(trace.Count));
        return true;
    }

    /// <summary>
    /// A step that covers a key: derive the set from the project's own strings, read the name of every
    /// element the locator matched, and compare.
    /// <para>
    /// The derivation is the point. A case listing the strings is a case that stops covering what it
    /// was written for the day the application grows one — which is the defect this exists for, and it
    /// reported <em>all three tab headers read</em> against a four-tab window.
    /// </para>
    /// <para>
    /// A set that cannot be derived is a refusal and never a failure: nothing about the application
    /// was observed, so nothing about it is being reported.
    /// </para>
    /// </summary>
    private static void Swept(
        StepDeclaration step,
        string key,
        Subject subject,
        ProjectDeclaration project,
        AutomationElement root,
        List<TraceStep> trace,
        List<AssertionResult> results,
        System.Globalization.CultureInfo? speaking)
    {
        DerivedSet derived;
        try
        {
            // WW260. Which well the set comes out of is the project's business and never the case's,
            // exactly as which strings file it is has always been: a case naming the flag that prints
            // the profiles would be a case that runs on one checkout. A name the project declares as
            // reported is asked of the application; anything else is a key in its strings.
            derived = project.ReportedSets.ContainsKey(key)
                ? DerivedSet.Reported(step.Name, project, key)
                : DerivedSet.From(step.Name, project, key, speaking);
        }
        catch (UnderivableSetException underivable)
        {
            throw new ScenarioRefusedException(step.Name, underivable.Message);
        }

        // WW241. Polled, and this used to read once. The reasoning for reading once was that retrying
        // would re-read a whole tree for the same answer — and it is not the same answer: a window
        // still drawing has a different tree a moment later, so reading once made a sweep a race.
        //
        // Measured against claude-tray, where the tab control the sweep is about is Collapsed, and
        // therefore absent from the tree, until the report renders. The sweep read `0 of 4` and the
        // case beside it read three labels out of that same pane seconds later and passed.
        //
        // The resolve budget and not the attempt cap: a sweep is a read, and what a read waits for is
        // the element to be there. `Attempts` is about a flaky act, which this is not.
        SetComparison compared = default!;
        var waited = Attempt.UntilTrue(
            () =>
            {
                // The last step of the locator, because that is the one the matches are of: a sweep
                // over `TabItem` under `Window#main > TabItem` is about the tab items, and the frame
                // above them is how they were reached.
                var read = Sweeping(root, subject.Locator)
                    .Select(one => ElementFacts.Of(one)?.Says)
                    .OfType<string>()
                    .ToList();

                // WW275 and WW292. Which claim this is belongs to the step and never to the set: the
                // same derived values are compared for equality both ways, for equality one way, or
                // for appearing inside what was read — and the step is the only thing that knows.
                compared = derived.Against(read, step.Matching);
                return compared.Held;
            },
            project.Timeouts.For("resolve"),
            project.Timeouts.For("poll"));

        // What the window was doing when the wait gave up, and only then: a sweep that held says
        // nothing about loading text, and reading the whole tree again to find that out would be the
        // cost the old comment was worried about, paid on every green.
        var detail = waited.Happened ? compared.Sentence() : Explaining(compared, project, root, waited.WaitedMs);

        trace.Add(new TraceStep
        {
            Step = trace.Count + 1,
            Verb = step.Verb.Name,
            Locator = step.Addressed,
            Asserted = step.Name,
            ReadBack = string.Join(", ", compared.Matched),
            Detail = waited.Happened ? null : detail,
            Verdict = waited.Happened ? StepVerdict.Ok : StepVerdict.Failed,
        });

        var result = waited.Happened
            ? AssertionResult.Pass(step.Name, detail)
            : AssertionResult.Fail(step.Name, detail);

        results.Add(result.At(trace.Count));
    }

    /// <summary>
    /// The members a case runs over: one null where it repeats over nothing, or every string the key
    /// it names declares.
    /// <para>
    /// WW263. An empty derivation is refused rather than run, which is the guard the script wrote by
    /// hand before its own walk: zero panels makes every assertion inside run zero times and report
    /// nothing at all — a clean run over nothing, which is this project's founding defect reached
    /// through a language flag.
    /// </para>
    /// </summary>
    /// <exception cref="ScenarioRefusedException">Where the set cannot be derived, or is empty.</exception>
    private static IReadOnlyList<string?> Members(
        CaseDeclaration declared, ProjectDeclaration project, System.Globalization.CultureInfo? speaking)
    {
        if (declared.ForEach is not { } key)
            return [null];

        DerivedSet derived;
        try
        {
            // WW260, and the same dispatch a sweep makes: a set is a set, and a case repeating once
            // per profile is the shape the environment sweep already needs. An asymmetry where one
            // field could reach the reported well and the other could not is one a reader trips on.
            derived = project.ReportedSets.ContainsKey(key)
                ? DerivedSet.Reported(declared.Name, project, key)
                : DerivedSet.From(declared.Name, project, key, speaking);
        }
        catch (UnderivableSetException underivable)
        {
            throw new ScenarioRefusedException(declared.Name, underivable.Message);
        }

        return derived.Expected.Count == 0
            ? throw new ScenarioRefusedException(
                declared.Name,
                $"it runs once for each string under '{key}' and that key declares none, so every step "
                    + "in it would run zero times and the case would report a clean run over nothing")
            : derived.Expected.Cast<string?>().ToList();
    }

    /// <summary>
    /// A step claiming no control inside a row its locator matches announces another row's header.
    /// <para>
    /// WW264. The headers are derived and never listed: they are the names of the rows the locator
    /// matched, so a row added to the page joins the set with no edit to the case. A control that
    /// announces its own row's header is right, and one that keeps its own text is right — that is
    /// the branch of the rule that must not fire, and the only one that can produce the duplicate.
    /// </para>
    /// <para>
    /// A row that announces nothing is passed over rather than failed: this claim is about pairing,
    /// and whether a row has a header at all is what `eachSpoken` is for. Two claims in one check is
    /// how a red stops saying which of them went wrong.
    /// </para>
    /// </summary>
    private static void Paired(
        StepDeclaration step,
        Subject subject,
        AutomationElement root,
        List<TraceStep> trace,
        List<AssertionResult> results)
    {
        var wrong = new List<string>();
        var paired = 0;
        var rows = 0;

        var waited = Attempt.UntilTrue(
            () =>
            {
                wrong.Clear();
                paired = 0;

                var found = Sweeping(root, subject.Locator);
                rows = found.Count;

                var headers = found
                    .Select(one => ElementFacts.Of(one)?.Says)
                    .OfType<string>()
                    .ToHashSet(StringComparer.Ordinal);

                foreach (var row in found)
                {
                    if (ElementFacts.Of(row)?.Says is not { } header)
                        continue;

                    var inside = row.FindAll(TreeScope.Descendants, Condition.TrueCondition)
                        .Cast<AutomationElement>()
                        .Select(one => ElementFacts.Of(one))
                        .OfType<ElementFacts>()
                        .Where(one => one.Says is not null)
                        .ToList();

                    if (inside.Count > 0)
                        paired++;

                    foreach (var one in inside)
                    {
                        // Its own row's header is right and its own text is right. What is left is a
                        // name that is some other row's header, which is the rule pairing the wrong
                        // two things — and it is a real label somebody wrote, on the wrong control.
                        if (!string.Equals(one.Says, header, StringComparison.Ordinal)
                            && headers.Contains(one.Says!))
                        {
                            wrong.Add($"{one} sits in the '{header}' row and announces \"{one.Says}\"");
                        }
                    }
                }

                return rows > 0 && paired > 0 && wrong.Count == 0;
            },
            subject.DeadlineMs,
            subject.PollMs);

        // WW272. Nothing matched is the third verdict rather than a red, unless the locator was built
        // out of a string the project declares — see `SweptNothing`.
        var swept = rows > 0 ? default : SweptNothing(step, "row", waited.WaitedMs);

        var detail = (rows, paired, wrong.Count) switch
        {
            (0, _, _) => swept.Said,
            (_, 0, _) => $"{step.Addressed} matched {rows} row(s) and not one of them holds a control "
                + $"that announces anything, so no pairing was checked. Waited {waited.WaitedMs}ms.",
            (_, _, > 0) => $"{wrong.Count} control(s) announce another row's header: "
                + $"{string.Join("; ", wrong)}. Waited {waited.WaitedMs}ms.",
            _ => $"across {rows} row(s), {paired} of them pair a control with their own header and none "
                + "announces another row's",
        };

        trace.Add(new TraceStep
        {
            Step = trace.Count + 1,
            Verb = step.Verb.Name,
            Locator = step.Addressed,
            Asserted = step.Name,
            WaitedMs = waited.WaitedMs,
            Polls = rows,
            Detail = waited.Happened ? null : detail,
            Verdict = Reached(swept.Hole, waited.Happened),
        });

        results.Add(Settled(step, swept.Hole, waited.Happened, detail).At(trace.Count));
    }

    /// <summary>
    /// The results of a repeated case, gathered so each claim is counted once over the whole walk.
    /// <para>
    /// WW276. A case walking six panels asserts one rule six times, and those are one claim. Left
    /// apart, claude-tray's About panel — which holds prose and links and not one settings row — makes
    /// the run degraded on every machine forever, so the suite around it goes red on a page behaving
    /// exactly as designed or goes green over a hole. Neither is a reading anybody acts on.
    /// </para>
    /// <para>
    /// The rule is the one the script this replaces wrote by hand: red where any member that carried
    /// the claim failed, a hole only where <em>no</em> member carried it — the vacuous walk, which is
    /// `WW263`'s empty-derivation guard one level in — and otherwise a pass saying how many of the
    /// members carried it. That count is the half `WW263`'s design asked for and did not get: what was
    /// visited, reported apart from what was asserted.
    /// </para>
    /// <para>
    /// Nothing is collapsed for a case that runs once, and the trace is untouched either way: a reader
    /// who wants the panel a red came from reads the line, and a summary that named six results for
    /// one rule is a summary that says the rule was checked six times.
    /// </para>
    /// </summary>
    /// <param name="results">Every result the run produced, in the order it produced them.</param>
    /// <param name="claims">Which claim each of them belongs to, by the name the case gave it.</param>
    /// <param name="members">How many members the case ran over. One where it repeats over nothing.</param>
    private static List<AssertionResult> Walked(
        List<AssertionResult> results, List<string> claims, int members)
    {
        if (members <= 1 || results.Count == 0)
            return results;

        var order = new List<string>();
        var gathered = new Dictionary<string, List<AssertionResult>>(StringComparer.Ordinal);
        for (var index = 0; index < results.Count; index++)
        {
            var claim = index < claims.Count ? claims[index] : results[index].Name;
            if (!gathered.TryGetValue(claim, out var already))
            {
                order.Add(claim);
                gathered[claim] = already = [];
            }

            already.Add(results[index]);
        }

        return order.Select(claim => Over(claim, gathered[claim], members)).ToList();
    }

    /// <summary>One claim's verdict over every member the walk made it against.</summary>
    /// <param name="claim">What the case called it, without the member a repeated run adds.</param>
    /// <param name="over">What each member answered, in the order they were visited.</param>
    /// <param name="members">How many members the case ran over.</param>
    private static AssertionResult Over(string claim, List<AssertionResult> over, int members)
    {
        var failed = over.Where(one => one.Outcome == AssertionOutcome.Failed).ToList();
        var holes = over.Where(one => one.DidNotRun).ToList();
        var carried = over.Count - holes.Count;

        // Which member, always: "the rule is wrong somewhere" sends a reader through six panels and
        // "the slider on Display says Language" sends them at one.
        var said = $"{carried} of {members} member(s) carried this claim";
        var aside = holes.Count == 0 ? "" : $"; {holes.Count} had nothing to check — {Listed(holes)}";
        var step = over[0].Step;

        AssertionResult settled;
        if (failed.Count > 0)
            settled = AssertionResult.Fail(claim, $"{Listed(failed)}. {said}{aside}.");
        else if (carried == 0)
            settled = AssertionResult.Unchecked(
                claim,
                Precondition.Absent(
                    $"one of the {members} member(s) carries '{claim}'",
                    $"not one of the {members} member(s) had anything to check — {Listed(holes)}"));
        else
            settled = AssertionResult.Pass(claim, $"{said}{aside}.");

        return step > 0 ? settled.At(step) : settled;
    }

    /// <summary>What some of the members answered, each named by the member it was about.</summary>
    /// <param name="these">The results to say.</param>
    private static string Listed(List<AssertionResult> these) =>
        string.Join("; ", these.Select(one => $"{one.Name}: {one.Detail}"));

    /// <summary>
    /// One step with every key in its locator replaced by the string the project declares for it.
    /// <para>
    /// WW273. Out of the language the fixture said its window is in, and resolved here rather than at
    /// declaration for the reason `label` is: the declaration knows the case and not the project, and
    /// a locator resolved against the wrong language is a locator that finds nothing and reports it as
    /// an application defect. A key that cannot be read is the scenario being wrong, so it refuses,
    /// naming the key and the file — and it refuses before anything is driven.
    /// </para>
    /// </summary>
    /// <param name="step">The step, already carrying its member where its case repeats.</param>
    /// <param name="project">The project, which is where the strings files are declared.</param>
    /// <param name="speaking">What the fixture said its window is in, or null where nothing did.</param>
    /// <param name="reported">What this run has already asked the application, so it is asked once.</param>
    /// <exception cref="ScenarioRefusedException">Where a key cannot be read, or the result will not parse.</exception>
    private static StepDeclaration Naming(
        StepDeclaration step,
        ProjectDeclaration project,
        System.Globalization.CultureInfo? speaking,
        Dictionary<string, string> reported)
    {
        if (step.Declares().Count == 0)
            return step;

        // The fixture's word where it gave one, and the way the application resolves it where nothing
        // did — which is what an attach has to do, there being no launch to have said.
        var language = speaking is null
            ? ResolvedLanguage.Resolve(project)
            : ResolvedLanguage.Speaking(speaking);

        try
        {
            return step.Naming(key => Substituted(step, project, language, key, reported));
        }
        catch (UnusableLabelException unusable)
        {
            throw new ScenarioRefusedException(step.Name, unusable.Message);
        }
        catch (UnderivableSetException underivable)
        {
            throw new ScenarioRefusedException(step.Name, underivable.Message);
        }
    }

    /// <summary>What one brace in a locator resolves to, out of whichever well its spelling names.</summary>
    /// <param name="step">The step, for the refusals.</param>
    /// <param name="project">The project, which declares both wells.</param>
    /// <param name="language">What the window is in, for the strings well.</param>
    /// <param name="key">The brace's content, prefix and all.</param>
    /// <param name="reported">What this run has already asked the application, so it is asked once.</param>
    private static string Substituted(
        StepDeclaration step,
        ProjectDeclaration project,
        ResolvedLanguage language,
        string key,
        Dictionary<string, string> reported)
    {
        // WW295. Two wells and two spellings, and the prefix is what keeps them apart. `{a.key}` is
        // answered out of the strings before the window is — the same value on every desk — and
        // `{report:name}` is answered by asking the application, which is the point: the entry a check
        // mark should be on is whichever profile this machine's icon follows, and no case can name it.
        //
        // Opt-in by syntax rather than by lookup order. A locator that quietly became run-time
        // because a name happened to exist in the other well is the drift WW290 is about, and here it
        // would change when a selector resolves rather than only what it resolves to.
        if (!key.StartsWith(Reports, StringComparison.Ordinal))
            return Labels.For(key, project, language).Text;

        var name = key[Reports.Length..].Trim();

        // Asked once per run and remembered. A locator carrying one is resolved per step and per
        // member of a repeated case, and launching the application for each of those is a process per
        // substitution — and a value that moved between two of them would leave one step selecting
        // something the next one does not.
        if (!reported.TryGetValue(name, out var value))
        {
            value = DerivedSet.ReportedValue(step.Name, project, name);
            reported[name] = value;
        }

        return value;
    }

    /// <summary>How a locator spells the well that asks the application. WW295.</summary>
    private const string Reports = "report:";

    /// <summary>
    /// Every element a sweep matches: its locator's last step, under the route the steps before it
    /// name, and nothing at all where that route does not resolve.
    /// <para>
    /// WW277. The last step is the one the matches are of — `Panel#general &gt; ComboBox` is about the
    /// pickers — and it was being matched against the whole window, so every step before it was
    /// decoration. A case scoping a sweep to one panel got a sweep of the window with the panel's name
    /// written beside it: a pass over more than was asked for, or a red about a control on a page the
    /// case had never opened. The documentation had said the opposite of what the code did since the
    /// first sweep shipped.
    /// </para>
    /// </summary>
    /// <param name="root">The window, or whatever the case was launched against.</param>
    /// <param name="locator">The whole locator, last step included.</param>
    private static IReadOnlyList<AutomationElement> Sweeping(AutomationElement root, Locator locator) =>
        Resolve.Beneath(root, locator)
            .SelectMany(under => Resolve.Matching(under, locator.Steps[^1]))
            .Distinct()
            .ToList();

    /// <summary>
    /// What a sweep needed before there was anything to sweep: one element under its locator.
    /// <para>
    /// WW272. A red was wrong here, and the sentence it carried borrowed `covers`' reason — an empty
    /// set is met by an empty window. That reason does not transfer. `covers` derives its expected set
    /// from what the project <em>declares</em>, so an empty one is a fact about the file and wrong on
    /// every machine. These two derive nothing: they match against the <em>window</em>, and empty means
    /// this window has none of these right now, which is sometimes a defect and sometimes the page.
    /// </para>
    /// <para>
    /// Measured migrating `WW84`. claude-tray's About panel holds prose and links and not one settings
    /// row, so a walk over every panel the navigation declares reds on a page behaving exactly as
    /// designed — and the script it replaced said so in as many words: a panel is allowed to have no
    /// row the rule applies to. A pass would be the unearned green and a red is a lie about the
    /// application, so what is left is the verdict this whole tool is built on: counted, named, and
    /// carrying the locator that matched nothing.
    /// </para>
    /// <para>
    /// The one exception is a locator built out of a declared string, which a repeated case does. That
    /// set comes from the file after all, so nothing matched is `WW263`'s disagreement — the strings
    /// say this row is there and the window does not draw it — and that is a red naming both.
    /// </para>
    /// </summary>
    /// <param name="step">The step whose locator swept nothing.</param>
    /// <param name="kind">What it was sweeping for, as the sentence should say it.</param>
    /// <param name="waitedMs">How long it waited for one to arrive.</param>
    /// <returns>The absence where this is a hole and null where it is a red, and the sentence either way.</returns>
    private static (Precondition? Hole, string Said) SweptNothing(StepDeclaration step, string kind, long waitedMs)
    {
        if (step.Carries is { } declared)
        {
            return (null, $"{step.Addressed} matched no {kind} in {waitedMs}ms, and it was built out "
                + $"of \"{declared}\" — a string the project declares and this window does not draw");
        }

        var hole = Precondition.Absent(
            $"{step.Addressed} matches at least one {kind}",
            $"{step.Addressed} matched no {kind} in {waitedMs}ms, so this swept nothing at all — "
                + "which is a fact about the window rather than about the claim, and a sweep that "
                + "swept nothing did not run");

        return (hole, hole.Absence);
    }

    /// <summary>How the trace reports a sweep: a hole where it swept nothing, otherwise the reading.</summary>
    /// <param name="swept">The absence, where the locator matched nothing.</param>
    /// <param name="held">Whether the claim held.</param>
    private static StepVerdict Reached(Precondition? swept, bool held) => swept is not null
        ? StepVerdict.Unchecked
        : held ? StepVerdict.Ok : StepVerdict.Failed;

    /// <summary>The same three ways, as the result a summary counts.</summary>
    /// <param name="step">The step being reported.</param>
    /// <param name="swept">The absence, where the locator matched nothing.</param>
    /// <param name="held">Whether the claim held.</param>
    /// <param name="detail">What was read, for the two outcomes that read something.</param>
    private static AssertionResult Settled(
        StepDeclaration step,
        Precondition? swept,
        bool held,
        string detail) => swept is not null
        ? AssertionResult.Unchecked(step.Name, swept)
        : held ? AssertionResult.Pass(step.Name, detail) : AssertionResult.Fail(step.Name, detail);

    /// <summary>
    /// A step claiming that every element its locator matches announces a name.
    /// <para>
    /// WW262. The last step of the locator, because that is the one the matches are of — the same rule
    /// a sweep over strings goes by, and for the same reason: `Panel#general > ComboBox` is about the
    /// pickers, and the panel above them is how they were reached.
    /// </para>
    /// <para>
    /// A locator matching nothing is neither held nor failed but counted as the hole it is, for the
    /// reason <see cref="SweptNothing"/> gives: a page with no rows is a fact about the application.
    /// </para>
    /// </summary>
    private static void EachSpoke(
        StepDeclaration step,
        Subject subject,
        AutomationElement root,
        List<TraceStep> trace,
        List<AssertionResult> results)
    {
        var spoken = new List<string>();
        var wrong = new List<NameCheck>();
        var matched = 0;

        var waited = Attempt.UntilTrue(
            () =>
            {
                spoken.Clear();
                wrong.Clear();

                var found = Sweeping(root, subject.Locator);
                matched = found.Count;
                foreach (var one in found)
                {
                    if (ElementFacts.Of(one) is not { } facts)
                        continue;

                    var check = Names.Of(facts);
                    if (check.IsALabel)
                        spoken.Add(check.Printable);
                    else
                        wrong.Add(check);
                }

                return matched > 0 && wrong.Count == 0;
            },
            subject.DeadlineMs,
            subject.PollMs);

        // WW272. Nothing matched is the third verdict rather than a red, unless the locator was built
        // out of a string the project declares — see `SweptNothing`.
        var swept = matched > 0 ? default : SweptNothing(step, "element", waited.WaitedMs);

        var detail = (matched, wrong.Count) switch
        {
            (0, _) => swept.Said,
            (_, > 0) => $"{wrong.Count} of the {matched} element(s) {step.Addressed} matches announce "
                + $"something that is not a name. {string.Join(" ", wrong.Select(one => one.Sentence("one of them")))} "
                + $"Waited {waited.WaitedMs}ms.",
            _ => $"all {matched} element(s) {step.Addressed} matches announce a name, e.g. \"{spoken[0]}\".",
        };

        trace.Add(new TraceStep
        {
            Step = trace.Count + 1,
            Verb = step.Verb.Name,
            Locator = step.Addressed,
            Asserted = step.Name,
            ReadBack = spoken.Count == 0 ? null : string.Join(", ", spoken),
            WaitedMs = waited.WaitedMs,
            Detail = waited.Happened ? null : detail,
            Verdict = Reached(swept.Hole, waited.Happened),
        });

        var result = Settled(step, swept.Hole, waited.Happened, detail);

        results.Add(result.At(trace.Count));
    }

    /// <summary>
    /// A step claiming that everything under the locator which announces anything announces a name,
    /// and that something does.
    /// <para>
    /// WW253. Two halves, both count-free. The script asserted four or more named descendants on a
    /// conversation row, and a typed count is the stale literal a derived set exists to refuse — the
    /// row grows a column and the case goes on asserting four. So: something under here speaks, which
    /// a row of pictures fails, and nothing under here announces a glyph, a template or its own
    /// automation id, which a row of codepoints fails.
    /// </para>
    /// <para>
    /// Polled to the resolve budget for the reason a sweep is: a subtree still being drawn has a
    /// different tree a moment later, and reading once would make this a race. What it waits for is
    /// both halves holding at once, so a name that arrives late is waited out and a glyph that is
    /// never going to change costs the budget and then fails.
    /// </para>
    /// </summary>
    private static void Spoke(
        StepDeclaration step, Subject subject, List<TraceStep> trace, List<AssertionResult> results)
    {
        var spoken = new List<string>();
        var wrong = new List<NameCheck>();
        var under = 0;
        var arrived = false;

        var waited = Attempt.UntilTrue(
            () =>
            {
                spoken.Clear();
                wrong.Clear();

                var element = subject.ResolveOnce().Element;
                arrived = element is not null;
                var beneath = element is null
                    ? []
                    : element.FindAll(TreeScope.Descendants, Condition.TrueCondition).Cast<AutomationElement>();

                under = 0;
                foreach (var one in beneath)
                {
                    under++;
                    if (ElementFacts.Of(one) is not { } facts)
                        continue;

                    var check = Names.Of(facts);

                    // Missing is not counted against it. A panel, a border, a layout element under a
                    // row legitimately announces nothing, and demanding a name off every one of them
                    // would be a claim about how the application nests its containers.
                    if (check.IsALabel)
                        spoken.Add(check.Printable);
                    else if (check.Verdict != Named.Missing)
                        wrong.Add(check);
                }

                return spoken.Count > 0 && wrong.Count == 0;
            },
            subject.DeadlineMs,
            subject.PollMs);

        // A locator that matched nothing is a failure about the locator and never a claim about a
        // subtree. Reporting it as "nothing under it speaks" is true of a window that never drew the
        // thing at all, and sends a reader looking at names when what is missing is the element.
        var detail = (arrived, spoken.Count, wrong.Count) switch
        {
            (false, _, _) => $"{step.Addressed} never arrived in {waited.WaitedMs}ms, so nothing under it "
                + "was ever looked at",
            (_, 0, 0) => $"nothing under {step.Addressed} announces a name: {under} element(s), and not one "
                + $"of them says anything a screen reader could read. Waited {waited.WaitedMs}ms.",
            (_, _, > 0) => $"{wrong.Count} of the {under} element(s) under {step.Addressed} announce something "
                + $"that is not a name. {string.Join(" ", wrong.Select(one => one.Sentence("one of them")))} "
                + $"Waited {waited.WaitedMs}ms.",
            _ => $"all {spoken.Count} of the {under} element(s) under {step.Addressed} that announce anything "
                + $"announce a name, e.g. \"{spoken[0]}\".",
        };

        trace.Add(new TraceStep
        {
            Step = trace.Count + 1,
            Verb = step.Verb.Name,
            Locator = step.Addressed,
            Asserted = step.Name,
            ReadBack = spoken.Count == 0 ? null : string.Join(", ", spoken),
            WaitedMs = waited.WaitedMs,
            Detail = waited.Happened ? null : detail,
            Verdict = waited.Happened ? StepVerdict.Ok : StepVerdict.Failed,
        });

        var result = waited.Happened
            ? AssertionResult.Pass(step.Name, detail)
            : AssertionResult.Fail(step.Name, detail);

        results.Add(result.At(trace.Count));
    }

    /// <summary>
    /// A step whose claim is about the wait: while the locator was being waited for, the string the
    /// project declares under <paramref name="key"/> was never showing anywhere in the window.
    /// <para>
    /// WW256. Every other claim is read once the waiting is over, and this one cannot be: the line it
    /// is about is gone by then, which is what passing looks like and also what a switch that flashed
    /// one looks like. Measured on claude-tray, where coming back to a profile seen seconds ago showed
    /// its report at 12ms with the cache and showed the <em>no readings yet</em> line at 162ms without
    /// it — the same window, a second apart, indistinguishable to anything reading the end state.
    /// </para>
    /// <para>
    /// The locator is what says when to stop looking rather than what to look at, so a locator that
    /// never arrives is a failure and not a pass: a claim that nothing was seen during a wait that
    /// never ended is a claim about a window that never got where the case was taking it.
    /// </para>
    /// <para>
    /// WW189's rule applies and is the reason a truncated walk is a hole. Seeing the string is
    /// positive evidence a short walk cannot take away; <em>not</em> seeing it is only an answer where
    /// the walk reached everything, and reporting the other case as never-seen would be a green over a
    /// look that never got to the control it was about.
    /// </para>
    /// </summary>
    private static void Watched(
        StepDeclaration step,
        string key,
        Subject subject,
        ProjectDeclaration project,
        AutomationElement root,
        List<TraceStep> trace,
        List<AssertionResult> results)
    {
        Label watched;
        try
        {
            watched = Labels.For(key, project);
        }
        catch (UnusableLabelException unusable)
        {
            throw new ScenarioRefusedException(step.Name, unusable.Message);
        }

        string? seen = null;
        var whole = true;
        var looks = 0;

        var waited = Attempt.UntilTrue(
            () =>
            {
                // The window first and the locator second, in that order. The poll that finds the
                // locator is the last one there will be, so looking at the end state first would let
                // whatever was on screen at that moment go unlooked-at — which is the one moment this
                // claim is most likely to be false at.
                looks++;
                var showing = Loading.Sighted(root, watched);
                seen ??= showing.Text;
                whole = whole && showing.Whole;
                return subject.ResolveOnce().Found;
            },
            subject.DeadlineMs,
            subject.PollMs);

        var (verdict, detail) = (seen, waited.Happened, whole) switch
        {
            (not null, _, _) => (
                StepVerdict.Failed,
                $"'{watched.Text}' ({watched.Key}) was showing {seen} while this step waited, "
                    + $"{waited.WaitedMs}ms over {looks} look(s)."),
            (_, false, _) => (
                StepVerdict.Failed,
                $"{step.Addressed} never arrived in {waited.WaitedMs}ms, so nothing waited for "
                    + $"'{watched.Text}' ({watched.Key}) not to show."),
            (_, _, false) => (
                StepVerdict.Unchecked,
                $"'{watched.Text}' ({watched.Key}) was not seen, and at least one of the {looks} look(s) "
                    + "did not reach the whole window, so its absence is not an absence."),
            _ => (
                StepVerdict.Ok,
                $"'{watched.Text}' ({watched.Key}) never showed while this step waited, "
                    + $"{waited.WaitedMs}ms over {looks} look(s)."),
        };

        trace.Add(new TraceStep
        {
            Step = trace.Count + 1,
            Verb = step.Verb.Name,
            Locator = step.Addressed,
            Asserted = step.Name,
            WaitedMs = waited.WaitedMs,
            Polls = looks,
            Detail = verdict == StepVerdict.Ok ? null : detail,
            Verdict = verdict,
        });

        var result = verdict switch
        {
            StepVerdict.Ok => AssertionResult.Pass(step.Name, detail),
            StepVerdict.Unchecked => AssertionResult.Unchecked(
                step.Name, Precondition.Absent(WalkedWhole, detail)),
            _ => AssertionResult.Fail(step.Name, detail),
        };

        results.Add(result.At(trace.Count));
    }

    /// <summary>
    /// Why a sweep that waited still did not hold: the comparison, how long it waited, and — where the
    /// project declared what <em>not yet</em> looks like — whether the page was saying so the whole
    /// time.
    /// <para>
    /// WW241. The project already declares its loading strings and nothing in a run read them, so a
    /// page that never finished counting failed as a set that was missing. The two are not the same
    /// finding and a reader acts on them differently: one is a defect in the window, the other is a
    /// window that was never asked at a moment it could answer.
    /// </para>
    /// </summary>
    private static string Explaining(SetComparison compared, ProjectDeclaration project, AutomationElement root, int waitedMs)
    {
        var said = $"{compared.Sentence()} Waited {waitedMs}ms.";
        if (project.Loading.Count == 0)
            return said;

        var loading = Loading.In(root, project);
        return loading.Computing ? $"{said} {loading.Sentence()}" : said;
    }

    private static Landed Attempting(
        StepDeclaration step, Subject subject, string? backTo = null, string? declared = null)
    {
        // WW229. Read before the act and only where a step claims movement, because that is the one
        // claim whose other half is a moment that has already gone. Everything else compares a
        // reading to a string the case wrote down, and needs no before.
        var was = step.Moves ? step.Reads.Of(subject.Read()) : null;

        // WW251. The other half of the same idea, and read before the act for the same reason: the
        // subtree a disclosure is about is the one that existed a moment ago, and there is no reading
        // it back afterwards.
        var under = step.Discloses ? Beneath(subject.Resolve()) : -1;

        // A read never goes through Act: an act must have found something to press and a read need
        // not, so the element that was not there comes out as an expectation nothing answered rather
        // than a throw about a pattern the reader has to trace back to a locator.
        var acted = step.Verb.Reads ? null : step.Verb.Perform(subject, step.Argument);

        if (step.Moves)
            return Moved(step, subject, acted, was);

        if (step.Answers)
            return Answered(step, subject, acted);

        if (step.Absent)
            return Missing(step, subject, acted);

        if (step.Matches is not null)
            return Matched(step, subject, acted);

        if (step.Discloses)
            return Disclosed(step, subject, acted, under);

        if (step.PointsAt is not null)
            return Returned(step, subject, acted, backTo);

        // WW261 and WW270. Both are about one declared string, so both are answered in one place: the
        // positive is the expectation `expect` makes against a value the engine derived, and the
        // negative says so through the same trick `discloses` uses to state a negative to a machine
        // that compares for equality.
        if (step.Label is not null || step.NotLabel is not null || step.BeginsWithLabel is not null
            || step.ExpectReported is not null)
        {
            return Against(step, subject, acted, declared);
        }

        if (step.Expected is not { } wanted)
            return new Landed(acted, null, acted?.Element);

        // Expect.That rather than Expect.Of: the diagnosis is a window dump, and taking one per
        // missed attempt pays for three of them to report the last.
        var saw = acted?.Element;
        var expectation = Expect.That(
            step.Name,
            wanted,
            () =>
            {
                var look = subject.ReadOnce();
                saw = look.Facts ?? saw;
                return look.Found ? step.Reads.Of(look) : null;
            },
            subject.ActMs,
            subject.PollMs);

        return new Landed(acted, expectation, saw);
    }

    /// <summary>
    /// A step that claims its locator matches nothing.
    /// <para>
    /// WW318. The wait runs the other way round from every other one here: it polls until the
    /// locator matches <em>nothing</em> rather than until it matches, so a control on its way out is
    /// waited for and one that never leaves fails naming what it found. A single look would have made
    /// the claim a race against whatever the act had just done.
    /// </para>
    /// <para>
    /// The region is read first, and it is what keeps this from being the easiest unearned green in
    /// the format. <c>Pane#capture &gt; Button#close</c> matching nothing means the pane is up and
    /// holds no such button — the claim — or that the pane never opened, which answers nothing about
    /// the button. The second is reported as what it is: a claim that could not be evaluated, naming
    /// the region rather than passing.
    /// </para>
    /// </summary>
    private static Landed Missing(StepDeclaration step, Subject subject, ActResult? acted)
    {
        const string wanted = "nothing here";
        var saw = acted?.Element;

        var expectation = Expect.That(
            step.Name,
            wanted,
            () =>
            {
                // Read every time rather than once before the poll: a region that is arriving is the
                // ordinary case after an act, and answering about the first instant would report a
                // window that had not finished opening.
                if (!subject.RegionIsThere)
                    return $"nothing to look in — {subject.Locator} has no region to be absent from";

                var look = subject.ReadOnce();
                saw = look.Facts ?? saw;

                // Its own words where something is there, so the failure says what it found rather
                // than that the claim did not hold.
                return look.Found ? look.Facts?.ToString() ?? "something the tree would not describe" : wanted;
            },
            subject.ActMs,
            subject.PollMs);

        return new Landed(acted, expectation, saw);
    }

    /// <summary>
    /// A step that claims the reading says something, waited for the way every other expectation is.
    /// <para>
    /// WW237. The wanted value is the sentence, so a failure reads as what it is — <em>wanted
    /// something rather than nothing, last read nothing</em> — and an empty answer is nothing: a
    /// control saying <c>""</c> is what this claim exists to catch.
    /// </para>
    /// </summary>
    private static Landed Answered(StepDeclaration step, Subject subject, ActResult? acted)
    {
        const string wanted = "something rather than nothing";
        var saw = acted?.Element;
        var expectation = Expect.That(
            step.Name,
            wanted,
            () =>
            {
                var look = subject.ReadOnce();
                saw = look.Facts ?? saw;
                var now = look.Found ? step.Reads.Of(look) : null;
                return string.IsNullOrWhiteSpace(now) ? now : wanted;
            },
            subject.ActMs,
            subject.PollMs);

        return new Landed(acted, expectation, saw);
    }

    /// <summary>
    /// A step whose expectation is a string the project declares, waited for the way every other one
    /// is — either that the reading <em>is</em> it, or that it is not.
    /// <para>
    /// WW261 and WW270. What makes both worth having is that the value is never typed in the case. A
    /// label written into a step is the hardcoded set with one member: it goes stale the day somebody
    /// edits the string, and it is wrong in every other language the application ships from the moment
    /// it is written. The key is the same declaration the project already makes.
    /// </para>
    /// <para>
    /// The wanted sentence carries the key and the string, so a failure reads as what it is —
    /// <em>wanted 'settings.general.interval' — Refresh interval, last read Intervalo</em> — and a
    /// reader can tell a control announcing the wrong label from one announcing the right label in
    /// the wrong language.
    /// </para>
    /// </summary>
    private static Landed Against(StepDeclaration step, Subject subject, ActResult? acted, string? declared)
    {
        var saw = acted?.Element;
        var key = step.Label ?? step.NotLabel ?? step.BeginsWithLabel ?? step.ExpectReported;

        // WW294 joins the positive arm: it is `expect` with the value read from the application, so
        // the comparison is the one `label` already makes and only where the value came from differs.
        // The sentence says which well it was, because a reader of a red needs to know whether to go
        // to a strings file or to the machine.
        //
        // WW83 joins it too, and only the comparison below differs: it is the same declared string,
        // claimed of the front of the reading rather than of the whole of it.
        var wanted = (step.NotLabel, step.BeginsWithLabel, step.ExpectReported) switch
        {
            (not null, _, _) => $"anything but '{key}' — {declared}",
            (_, not null, _) => $"a reading beginning with '{key}' — {declared}",
            (_, _, not null) => $"the '{key}' this application reports — {declared}",
            _ => $"'{key}' — {declared}",
        };

        var expectation = Expect.That(
            step.Name,
            wanted,
            () =>
            {
                var look = subject.ReadOnce();
                saw = look.Facts ?? saw;
                var now = look.Found ? step.Reads.Of(look) : null;

                // A reading that answered nothing holds neither claim. For the positive that is
                // obvious; for the negative it matters more — an element that says nothing is not
                // evidence the application is out of the state this names, it is evidence nobody read
                // it, and answering "anything but" to that would be the unearned green.
                if (now is null)
                    return null;

                // The wanted sentence handed back where the claim holds, which is the trick `matches`
                // and `discloses` both use to say something other than equality to a machine that
                // compares for it — and what keeps the key in the sentence a failure carries.
                //
                // WW83's arm compares the front of the reading. A declared string that is empty would
                // hold of every reading there is, so it is refused rather than answered: a key
                // declaring nothing is the file being wrong, and the unearned green it would buy is
                // the one `matches` refuses an empty pattern for.
                if (step.BeginsWithLabel is not null)
                {
                    var begins = !string.IsNullOrEmpty(declared)
                        && now.StartsWith(declared, StringComparison.Ordinal);

                    return begins ? wanted : now;
                }

                var same = string.Equals(now, declared, StringComparison.Ordinal);
                return (step.NotLabel is null) == same ? wanted : now;
            },
            subject.ActMs,
            subject.PollMs);

        return new Landed(acted, expectation, saw);
    }

    /// <summary>
    /// A step that claims its reading is back to what an earlier one read, waited for the way every
    /// other expectation is.
    /// <para>
    /// WW255. The wanted value is the earlier reading itself, so a failure reads as what it is —
    /// <em>wanted the 41% that 'the first stop' read, last read 63%</em> — and nothing in the file
    /// typed either number. A round trip is the case this exists for and the value is exactly the one
    /// no case can know.
    /// </para>
    /// <para>
    /// An earlier step that read nothing is a failure and never a match. Two nothings are equal, and a
    /// green resting on that would say the round trip held on a window that never answered at either
    /// end — which is the same unearned green a pattern matching the empty string would be.
    /// </para>
    /// </summary>
    /// <summary>
    /// Whether two readings of a counting-down caption are the same one, a tick apart.
    /// <para>
    /// WW269. The numbers and never the words: `2d 4h` and `3h 20m` carry their units as literal ASCII
    /// in every language this was measured against, so a comparison that ignores everything but the
    /// digits is language-independent for a reason rather than by luck. Every number has to match
    /// except the last, which is the one that ticks — and it may be lower by one, never higher, because
    /// a caption that counted <em>up</em> across a round trip is the window having turned over and not
    /// the clock having moved.
    /// </para>
    /// <para>
    /// A different count of numbers is a different caption, not a tick: `3h 20m` reading later as `45m`
    /// is a window that changed shape, and the claim is about it having stayed put.
    /// </para>
    /// <para>
    /// The limit, said out loud because it is the one a reader will meet: this does not span a unit
    /// rolling over. `3h 00m` reading later as `2h 59m` is a minute apart and two of its numbers moved,
    /// so it fails. That is one minute in every sixty of the ones this exists to tolerate, and the
    /// alternative is teaching the engine what `h` means — a format it would then have to be kept in
    /// step with, which is what the whole derived-expectation rule refuses.
    /// </para>
    /// </summary>
    /// <param name="before">What the earlier step read.</param>
    /// <param name="now">What this one read.</param>
    private static bool Ticked(string before, string now)
    {
        var was = Digits.Matches(before).Select(one => one.Value).ToList();
        var is_ = Digits.Matches(now).Select(one => one.Value).ToList();

        if (was.Count == 0 || was.Count != is_.Count)
            return false;

        for (var index = 0; index < was.Count - 1; index++)
        {
            if (!string.Equals(was[index], is_[index], StringComparison.Ordinal))
                return false;
        }

        // Parsed rather than compared as text, so `20` and `19` are a tick and `20` and `02` are not.
        // Either side that will not parse is not a countdown, and a claim about one is not settled by
        // guessing what it might have been.
        if (!int.TryParse(was[^1], out var last) || !int.TryParse(is_[^1], out var ticked))
            return false;

        return ticked == last || ticked == last - 1;
    }

    /// <summary>Every run of digits in a reading, which is all a countdown comparison looks at.</summary>
    private static readonly System.Text.RegularExpressions.Regex Digits =
        new(@"\d+", System.Text.RegularExpressions.RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private static Landed Returned(StepDeclaration step, Subject subject, ActResult? acted, string? backTo)
    {
        var saw = acted?.Element;
        var pointed = step.PointsAt;
        var same = step.Pointing == Pointing.Same;
        var ticking = step.Pointing == Pointing.Countdown;
        var holding = step.Pointing == Pointing.Contains;

        if (string.IsNullOrEmpty(backTo))
        {
            // WW268. True of both claims and for the same reason: a step that read nothing left no
            // value, and neither *is it back to that* nor *is it unlike that* is a claim anybody can
            // settle against nothing. Answering the second one true would be the unearned green.
            var never = Expect.That(
                step.Name,
                $"the '{step.Reads.Name}' that '{pointed}' read",
                () => "nothing: that step read nothing, so there is no value to compare with",
                subject.ActMs,
                subject.PollMs);

            return new Landed(acted, never, saw);
        }

        var wanted = (same, ticking, holding) switch
        {
            (_, true, _) => $"the '{step.Reads.Name}' that '{pointed}' read, give or take a tick — {backTo}",
            (_, _, true) => $"a '{step.Reads.Name}' holding the {backTo} that '{pointed}' read",
            (true, _, _) => $"the '{step.Reads.Name}' that '{pointed}' read — {backTo}",
            _ => $"a '{step.Reads.Name}' other than the {backTo} that '{pointed}' read",
        };

        var expectation = Expect.That(
            step.Name,
            wanted,
            () =>
            {
                var look = subject.ReadOnce();
                saw = look.Facts ?? saw;
                var now = look.Found ? step.Reads.Of(look) : null;

                // A reading that answered nothing settles neither claim, which matters most for the
                // negative: an element that says nothing is not evidence the value changed, it is
                // evidence nobody read it.
                if (now is null)
                    return null;

                if (ticking)
                    return Ticked(backTo, now) ? wanted : now;

                // WW326. Containment and never equality, and the earlier reading is what is looked
                // for rather than the other way round: the dialog quotes the thing it opened for, so
                // the longer string is this step's. A step whose reading is exactly the earlier one
                // holds it too, which is right — `sameAs` is the stronger claim and is there to be
                // made where it is the one meant.
                if (holding)
                    return now.Contains(backTo, StringComparison.Ordinal) ? wanted : now;

                return string.Equals(now, backTo, StringComparison.Ordinal) == same ? wanted : now;
            },
            subject.ActMs,
            subject.PollMs);

        return new Landed(acted, expectation, saw);
    }

    /// <summary>
    /// How many elements are under a resolved subject, or -1 where nothing resolved.
    /// <para>
    /// WW251. Descendants and not children: a call tree unfolds as a tree, and counting one level of it
    /// would report a disclosure that added a row and miss one that added a hundred under an existing
    /// row. -1 rather than 0 for a subject that is not there, because a subtree of nothing and a
    /// subtree of no elements are different facts and only the second one can grow.
    /// </para>
    /// </summary>
    private static int Beneath(Resolution resolution) =>
        resolution.Element is { } element
            ? element.FindAll(TreeScope.Descendants, Condition.TrueCondition).Count
            : -1;

    /// <summary>
    /// A step that claims the act disclosed something, waited for the way every other expectation is.
    /// <para>
    /// WW251. The wanted value carries the count it was measured against, so a failure reads as what it
    /// is — <em>wanted more than the 4 element(s) that were under it, last read 4</em> — and the number
    /// is the engine's own reading rather than anything a case typed.
    /// </para>
    /// </summary>
    private static Landed Disclosed(StepDeclaration step, Subject subject, ActResult? acted, int under)
    {
        var wanted = $"more than the {under} element(s) that were under it";
        var saw = acted?.Element;
        var expectation = Expect.That(
            step.Name,
            wanted,
            () =>
            {
                var resolution = subject.ResolveOnce();
                saw = resolution.Facts ?? saw;
                var now = Beneath(resolution);
                return now > under ? wanted : $"{now}";
            },
            subject.ActMs,
            subject.PollMs);

        return new Landed(acted, expectation, saw);
    }

    /// <summary>
    /// A step that claims the reading matches a pattern, waited for the way every other expectation is.
    /// <para>
    /// WW250. The wanted value is the pattern in a sentence, so the failure reads as what it is —
    /// <em>wanted something matching '\d{4}-\d{2}-\d{2}', last read 'no date here'</em> — and the
    /// reading itself is what comes back on a miss, because what a reader needs is what the control
    /// actually said and not that it failed to match.
    /// </para>
    /// </summary>
    private static Landed Matched(StepDeclaration step, Subject subject, ActResult? acted)
    {
        var wanted = $"something matching '{step.Matches}'";
        var saw = acted?.Element;
        var expectation = Expect.That(
            step.Name,
            wanted,
            () =>
            {
                var look = subject.ReadOnce();
                saw = look.Facts ?? saw;
                var now = look.Found ? step.Reads.Of(look) : null;
                return now is not null && step.Matches!.IsMatch(now) ? wanted : now;
            },
            subject.ActMs,
            subject.PollMs);

        return new Landed(acted, expectation, saw);
    }

    /// <summary>
    /// A step that claims the reading moved, waited for the way every other expectation is.
    /// <para>
    /// The wanted value is the sentence rather than a sentinel, so a failure reads as what it is:
    /// <em>wanted something other than '5', last read '5'</em>. A reader is then told the value that
    /// would not budge, which is the half of the answer a boolean would have thrown away.
    /// </para>
    /// <para>
    /// A reading that went to nothing counts as moved, and that is deliberate: it did change, and the
    /// sentence records every reading on the way, so a case whose element vanished says so rather
    /// than passing quietly on a comparison that happened to differ.
    /// </para>
    /// </summary>
    private static Landed Moved(StepDeclaration step, Subject subject, ActResult? acted, string? was)
    {
        var wanted = $"something other than '{was ?? "nothing"}'";
        var saw = acted?.Element;
        var expectation = Expect.That(
            step.Name,
            wanted,
            () =>
            {
                var look = subject.ReadOnce();
                saw = look.Facts ?? saw;
                var now = look.Found ? step.Reads.Of(look) : null;
                return string.Equals(now, was, StringComparison.Ordinal) ? now : wanted;
            },
            subject.ActMs,
            subject.PollMs);

        return new Landed(acted, expectation, saw);
    }

    /// <summary>
    /// One go at a step: what the act reported where there was one, what the wait saw, and the
    /// element the last look found. A step with nothing to wait for holds by having acted, which is
    /// what makes one attempt of it the cap.
    /// </summary>
    private sealed record Landed(ActResult? Acted, Expectation? Expected, ElementFacts? Saw)
    {
        internal bool Held => Expected?.Held ?? true;
    }
}

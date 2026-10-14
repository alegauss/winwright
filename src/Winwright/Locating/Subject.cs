using System.Windows.Automation;

using Winwright.Projects;

namespace Winwright.Locating;

/// <summary>Everything one look at a subject saw, taken at one instant.</summary>
/// <param name="Resolution">What resolving found, or the diagnosed miss.</param>
/// <param name="Values">What its patterns read, as values. Empty where nothing resolved.</param>
public sealed record Reading(Resolution Resolution, PatternValues Values)
{
    /// <summary>Whether anything was there.</summary>
    public bool Found => Resolution.Found;

    /// <summary>What UI Automation said about it, or null where nothing was there.</summary>
    public ElementFacts? Facts => Resolution.Facts;

    /// <summary>Why nothing was there, diagnosed. Null where something was.</summary>
    public LocatorMiss? Miss => Resolution.Miss;
}

/// <summary>
/// What a scenario is about: a locator under a root, and never an element.
/// <para>
/// An element handle is a promise about a tree that has since moved. This holds the locator
/// instead and resolves it again for every act, so an act against something that went away is a
/// diagnosed miss rather than an exception from a handle nobody holds — and two readings taken
/// either side of an act are two sets of values, not one live view compared with itself.
/// </para>
/// </summary>
public sealed class Subject
{
    private readonly AutomationElement root;
    private readonly Timeouts? timeouts;
    private readonly int deadlineMs;
    private readonly int pollMs;

    /// <summary>
    /// A subject against a declared project, which is the only constructor there is.
    /// <para>
    /// WW135. There used to be one taking a bare <see cref="Timeouts"/>, and it was the one a
    /// scenario author would have reached for: with a project in hand you write the timeouts out of
    /// it, because that is what the type is for. A subject made that way carried no destructive
    /// list and refused nothing, with no line anywhere saying so — the guard was declined by
    /// whichever constructor happened to be easier to type.
    /// </para>
    /// <para>
    /// This project has closed the same shape twice: a process cannot be launched outside the
    /// register, and an act cannot reach an element without an admission. Both work because the
    /// weaker route does not exist, so the weaker route here does not exist either. What is left is
    /// <see cref="Unguarded"/>, which is not a constructor and says in its name what it gives up.
    /// </para>
    /// </summary>
    /// <param name="root">What to resolve under.</param>
    /// <param name="locator">What this subject is.</param>
    /// <param name="declaration">The project.</param>
    public Subject(AutomationElement root, Locator locator, ProjectDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(locator);
        ArgumentNullException.ThrowIfNull(declaration);

        this.root = root;
        timeouts = declaration.Timeouts;
        Locator = locator;
        Destructive = declaration.Destructive;
    }

    private Subject(Subject was, bool meansIt)
    {
        root = was.root;
        timeouts = was.timeouts;
        deadlineMs = was.deadlineMs;
        pollMs = was.pollMs;
        Locator = was.Locator;
        Destructive = was.Destructive;
        MeansIt = meansIt;
    }

    private Subject(Subject was, Locator locator, AutomationElement? under = null)
    {
        root = under ?? was.root;
        timeouts = was.timeouts;
        deadlineMs = was.deadlineMs;
        pollMs = was.pollMs;
        Locator = locator;
        Destructive = was.Destructive;
        MeansIt = was.MeansIt;
    }

    private Subject(AutomationElement root, Locator locator, int deadlineMs, int pollMs)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(locator);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(deadlineMs);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pollMs);

        this.root = root;
        this.deadlineMs = deadlineMs;
        this.pollMs = pollMs;
        Locator = locator;
    }

    /// <summary>
    /// A subject with the deadline spelled out and no project behind it, which is what a test of
    /// the locating machinery uses.
    /// <para>
    /// Named rather than offered as a constructor, and named for what it gives up: it carries no
    /// destructive list, so nothing about it is ever refused. A scenario driving a real application
    /// wants the constructor above; this is for a caller that has no declaration and is saying so.
    /// </para>
    /// </summary>
    /// <param name="root">What to resolve under.</param>
    /// <param name="locator">What this subject is.</param>
    /// <param name="deadlineMs">How long resolving waits.</param>
    /// <param name="pollMs">How often it looks again.</param>
    public static Subject Unguarded(AutomationElement root, Locator locator, int deadlineMs, int pollMs = 25) =>
        new(root, locator, deadlineMs, pollMs);

    /// <summary>What this subject is, as the scenario wrote it.</summary>
    public Locator Locator { get; }

    /// <summary>
    /// Whether the region this subject's last step is looked for under is there at all.
    /// <para>
    /// WW318. It is what keeps a claim of absence from being the easiest unearned green in the
    /// format. <c>Pane#capture &gt; Button#close</c> matching nothing means one of two opposite
    /// things — the pane is up and holds no such button, which is the claim, or the pane never
    /// opened, which answers nothing about the button. A step that could not tell them apart would
    /// pass hardest on a window that never rendered.
    /// </para>
    /// <para>
    /// True for a one-step locator, and that is not an exception: the region is then the root the
    /// case was launched against, which a fixture waits for before any step runs. So the claim is
    /// <em>not in this window</em>, and the window is there to be absent from.
    /// </para>
    /// </summary>
    public bool RegionIsThere => Locator.Steps.Count == 1 || Locating.Resolve.Beneath(root, Locator).Count > 0;

    /// <summary>
    /// The entries this project says end the run. Empty for a subject built without a declaration,
    /// and then nothing about this subject is refused.
    /// </summary>
    public Destructive Destructive { get; } = Destructive.None;

    /// <summary>
    /// Whether the scenario has said out loud that it means the destructive entry. False unless
    /// <see cref="MeaningIt"/> was called, which is the sentence a reviewer is looking for.
    /// </summary>
    public bool MeansIt { get; }

    /// <summary>
    /// The same subject, having said that it means the entry it names.
    /// <para>
    /// It sits on the subject rather than on each verb on purpose: a flag per verb is five flags
    /// that drift, and what a reviewer wants to find is one sentence at the place the dangerous
    /// thing is named — <c>Act.Invoke(quit.MeaningIt())</c> — rather than a parameter buried in
    /// the call that presses it.
    /// </para>
    /// </summary>
    public Subject MeaningIt() => new(this, meansIt: true);

    /// <summary>
    /// A second subject in the same window, under the same project. WW520.
    /// <para>
    /// For the one act that is about two elements: a drag starts on this subject and ends on that
    /// one. Built from this subject rather than from a root a caller holds, so the second element
    /// cannot be looked for in a different window, under different timeouts, or with the
    /// destructive guard dropped.
    /// </para>
    /// </summary>
    /// <param name="locator">The other element.</param>
    public Subject Elsewhere(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        return new(this, locator);
    }

    /// <summary>
    /// A second subject looked for from the desktop, under the same project. WW530.
    /// <para>
    /// For a drag that ends in another process's window: a file dragged onto an Explorer folder is
    /// released over an element no locator under the window under test can reach. The root is the
    /// desktop, the way a resident fixture's is, so the locator's first step names that window. The
    /// timeouts and the destructive guard still come from this subject, for
    /// <see cref="Elsewhere"/>'s reason.
    /// </para>
    /// </summary>
    /// <param name="locator">The other element, from the desktop down.</param>
    public Subject OnDesktop(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        return new(this, locator, AutomationElement.RootElement);
    }

    /// <summary>
    /// How long an act on this subject waits for what it did to show up, in milliseconds. Input
    /// is delivered to a queue and processed by another thread, so reading back the instant after
    /// sending it reads the value from before — which is a race, not a result.
    /// </summary>
    public int ActMs => timeouts?.For("act") ?? deadlineMs;

    /// <summary>How often that wait looks again.</summary>
    public int PollMs => timeouts?.For("poll") ?? pollMs;

    /// <summary>How long resolving this subject waits, which is what a read against it waits.</summary>
    public int DeadlineMs => timeouts?.For("resolve") ?? deadlineMs;

    /// <summary>
    /// The window this subject resolves under, or zero where the root is not a window.
    /// <para>
    /// WW166. Needed by the one verb that attaches a control view to a red: the diagnosis reads the
    /// tree under a window, and the subject is what already knows which window that is.
    /// </para>
    /// </summary>
    public nint Window
    {
        get
        {
            var found = WindowOf(ResolveOnce().Element);
            return found != 0 ? found : WindowOf(root);
        }
    }

    /// <summary>
    /// The top-level window an element is drawn in, walking up until something owns one. WW453.
    /// <para>
    /// This used to ask the root and nothing else, which is right for every case that has a window
    /// and wrong for the one shape an adopter has. A tray application draws none, so its cases
    /// resolve against the desktop — and the desktop answers no handle, so a step that needed one
    /// was told there was no window a key could be sent to. Measured against a launched tray whose
    /// menu was standing, holding the entry the step had just resolved.
    /// </para>
    /// <para>
    /// The walk is what makes it work rather than the change of root. A <c>MenuItem</c> is not a
    /// window — a <c>ToolStripMenuItem</c> owns no handle at all — and the thing that is one is the
    /// menu above it, so asking the element alone would have answered zero just as surely. Bounded,
    /// because a walk up a tree that is being rebuilt is a loop nobody wants in a diagnosis.
    /// </para>
    /// <para>
    /// Nothing changes for a case that already worked: an element inside an application's window
    /// walks up to that window, which is what the root would have answered. What changes is the case
    /// that was answered zero.
    /// </para>
    /// </summary>
    /// <param name="element">Where to start, or null where nothing resolved.</param>
    /// <param name="levels">How far up to look before giving up.</param>
    private static nint WindowOf(AutomationElement? element, int levels = 8)
    {
        var walker = TreeWalker.ControlViewWalker;

        try
        {
            for (var here = element; here is not null && levels > 0; here = walker.GetParent(here), levels--)
            {
                var handle = (nint)here.Current.NativeWindowHandle;
                if (handle != 0)
                    return Winwright.Windowing.Win32.GetAncestor(handle, Winwright.Windowing.Win32.GaRoot);
            }
        }
        catch (Exception gone)
            when (gone is ElementNotAvailableException or InvalidOperationException)
        {
            return 0;
        }

        return 0;
    }

    /// <summary>
    /// Resolve it again, now. Every act calls this: nothing here caches an element, so there is
    /// no handle to go stale between one act and the next.
    /// </summary>
    public Resolution Resolve() => timeouts is not null
        ? Locating.Resolve.Until(root, Locator, timeouts)
        : Locating.Resolve.Until(root, Locator, deadlineMs, pollMs);

    /// <summary>Resolve it with a single look, which is what asking whether it has gone needs.</summary>
    public Resolution ResolveOnce() => Locating.Resolve.Once(root, Locator);

    /// <summary>
    /// Resolve it and read everything about it into values, at one instant. Two readings taken
    /// either side of an act can be compared, which is the whole reason nothing live is returned.
    /// </summary>
    public Reading Read()
    {
        var resolution = Resolve();
        return new Reading(resolution, PatternValues.Of(resolution.Element, resolution.Facts));
    }

    /// <summary>The same, with a single look.</summary>
    public Reading ReadOnce()
    {
        var resolution = ResolveOnce();
        return new Reading(resolution, PatternValues.Of(resolution.Element, resolution.Facts));
    }
}

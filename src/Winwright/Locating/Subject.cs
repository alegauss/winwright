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
            try
            {
                var handle = (nint)root.Current.NativeWindowHandle;
                return handle == 0 ? 0 : Winwright.Windowing.Win32.GetAncestor(handle, Winwright.Windowing.Win32.GaRoot);
            }
            catch (Exception gone)
                when (gone is System.Windows.Automation.ElementNotAvailableException or InvalidOperationException)
            {
                return 0;
            }
        }
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

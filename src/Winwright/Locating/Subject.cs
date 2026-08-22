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

    /// <summary>A subject resolved against what the project declared.</summary>
    public Subject(AutomationElement root, Locator locator, Timeouts timeouts)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(locator);
        ArgumentNullException.ThrowIfNull(timeouts);

        this.root = root;
        this.timeouts = timeouts;
        Locator = locator;
    }

    /// <summary>
    /// A subject that knows the whole declaration, which is the timeouts and the entries this
    /// project says end the run. The only shape that carries the refusal, and therefore the one
    /// worth reaching for.
    /// </summary>
    /// <param name="root">What to resolve under.</param>
    /// <param name="locator">What this subject is.</param>
    /// <param name="declaration">The project.</param>
    public Subject(AutomationElement root, Locator locator, ProjectDeclaration declaration)
        : this(root, locator, (declaration ?? throw new ArgumentNullException(nameof(declaration))).Timeouts)
    {
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

    /// <summary>A subject with the deadline spelled out, for a caller that has no declaration.</summary>
    public Subject(AutomationElement root, Locator locator, int deadlineMs, int pollMs = 25)
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

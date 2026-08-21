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

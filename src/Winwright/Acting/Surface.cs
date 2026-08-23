using System.Collections.ObjectModel;

using Winwright.Locating;
using Winwright.Tracing;

namespace Winwright.Acting;

/// <summary>What putting one surface back did.</summary>
/// <param name="Locator">What was put back, as the scenario wrote it.</param>
/// <param name="Was">What it read when the case found it.</param>
/// <param name="Now">What it reads now.</param>
/// <param name="Moved">Whether it had moved at all. Nothing is touched that did not.</param>
/// <param name="PutBack">Whether it is back where it was.</param>
/// <param name="Because">Why it is not, where it is not.</param>
/// <param name="Pressing">
/// What pressing a toggle back to where it was took, where one was pressed at all. Null for every
/// other pattern, which is not a retry: a position and a value have setters and go back in one act.
/// <para>
/// WW147. The count existed for the length of one expression and was then dropped, so a control
/// that only ever comes round on the third press was invisible — which is the finding this block's
/// criterion asks to see, thrown away by the one site in the engine that retries.
/// </para>
/// </param>
public sealed record Restoration(
    string Locator,
    string? Was,
    string? Now,
    bool Moved,
    bool PutBack,
    string? Because,
    Attempted<string?>? Pressing = null)
{
    /// <summary>Whether putting this one back took more than the first press.</summary>
    public bool TookMoreThanOnePress => Pressing?.NeededMoreThanOne == true;

    /// <summary>The one line a report shows.</summary>
    public override string ToString()
    {
        if (!Moved)
            return $"{Locator} was left as it was found.";

        // The count goes in the sentence rather than beside it, because this line is what a report
        // prints and a number that only a caller reading the record can reach is a number nobody
        // reads. A first-time press says nothing: every restore would then carry a count of one.
        var took = TookMoreThanOnePress ? $", and it {Pressing}" : ".";

        return PutBack
            ? $"{Locator} was put back to \"{Was}\"{took}"
            : $"{Locator} is on \"{Now}\" and was found on \"{Was}\": {Because}.";
    }
}

/// <summary>
/// Surfaces recorded as a case found them, and put back when it is done with them.
/// <para>
/// Disposing puts them back, so a case that scopes this hands the window over the way it was
/// given it — which is the whole point: a popup is a toggle and a tab is a position, and the next
/// case sharing that window asked for neither.
/// </para>
/// </summary>
public sealed class Restorable : IDisposable
{
    private readonly List<(Subject Subject, PatternValues Found)> surfaces;
    private IReadOnlyList<Restoration>? done;

    internal Restorable(List<(Subject, PatternValues)> surfaces) => this.surfaces = surfaces;

    /// <summary>What was put back, once it has been. Empty until then.</summary>
    public IReadOnlyList<Restoration> Restorations => done ?? [];

    /// <summary>Whether everything that moved is back where it was.</summary>
    public bool HandedBackClean => Restorations.All(one => !one.Moved || one.PutBack);

    /// <summary>
    /// Put every surface back. Idempotent: the second call answers what the first did, so a case
    /// that restores explicitly and is also scoped does not fight itself.
    /// </summary>
    public IReadOnlyList<Restoration> PutBack()
    {
        if (done is not null)
            return done;

        var restorations = new List<Restoration>();
        foreach (var (subject, found) in surfaces)
            restorations.Add(Surface.Restore(subject, found));

        done = new ReadOnlyCollection<Restoration>(restorations);
        return done;
    }

    /// <summary>
    /// The steps this restore offers a trace, one per surface that actually moved.
    /// <para>
    /// WW147 asked whether a restore belongs in a trace at all, being the harness tidying up rather
    /// than the scenario acting. It does, and the reason is the failure the type above exists over:
    /// a surface that did not go back is what makes the <em>next</em> case fail, and a trace that
    /// omits the tidying is a trace whose reader cannot reach the cause from the effect. A surface
    /// nothing touched is not a step — nothing was attempted, and a step saying so would be a
    /// record of an act that never happened.
    /// </para>
    /// <para>
    /// Offered rather than written, because this type owns no writer and one handed to it would be
    /// a second way to reach a trace. The runner that has the writer appends these, and the count
    /// is stamped on by the same <see cref="Retry.Recorded{T}" /> every other traced act would use.
    /// </para>
    /// </summary>
    public IReadOnlyList<TraceStep> Steps()
    {
        var steps = new List<TraceStep>();
        foreach (var restoration in Restorations.Where(one => one.Moved))
        {
            var step = new TraceStep
            {
                Verb = "restore",
                Locator = restoration.Locator,
                ReadBack = restoration.Now,
                Verdict = restoration.PutBack ? StepVerdict.Ok : StepVerdict.Failed,
                Detail = restoration.Because,
            };

            steps.Add(restoration.Pressing is null ? step : Retry.Recorded(step, restoration.Pressing));
        }

        return new ReadOnlyCollection<TraceStep>(steps);
    }

    /// <summary>What was handed back, in one sentence.</summary>
    public string Sentence()
    {
        var moved = Restorations.Where(one => one.Moved).ToList();
        if (moved.Count == 0)
            return "nothing on this window was moved.";

        var stuck = moved.Where(one => !one.PutBack).ToList();
        return stuck.Count == 0
            ? $"{moved.Count} surface(s) were put back: {string.Join("; ", moved)}"
            : $"{moved.Count - stuck.Count} of {moved.Count} surface(s) were put back; "
                + $"{string.Join("; ", stuck)}";
    }

    /// <summary>Put them back, which is what makes scoping this the whole of the discipline.</summary>
    public void Dispose() => PutBack();
}

/// <summary>
/// Handing a window back the way it was found.
/// <para>
/// Restoring is what makes one window safe to lend to several cases. Without it, sharing produces
/// order-dependent failures that appear only when the whole suite runs and vanish when the case is
/// run alone — which is the most expensive kind of failure there is, because the first thing it
/// teaches a reader is that re-running is how you find out.
/// </para>
/// </summary>
public static class Surface
{
    /// <summary>How many times a toggle is pressed looking for the state it was found in.</summary>
    public const int MostToggles = 3;

    /// <summary>
    /// Record what these surfaces read now. Scope it with <c>using</c>, and they go back when the
    /// case is done whichever way it leaves.
    /// </summary>
    public static Restorable AsFound(params Subject[] surfaces)
    {
        ArgumentNullException.ThrowIfNull(surfaces);

        var recorded = new List<(Subject, PatternValues)>(surfaces.Length);
        foreach (var surface in surfaces)
        {
            ArgumentNullException.ThrowIfNull(surface);
            recorded.Add((surface, surface.ReadOnce().Values));
        }

        return new Restorable(recorded);
    }

    internal static Restoration Restore(Subject subject, PatternValues found)
    {
        var now = subject.ReadOnce();
        var locator = subject.Locator.Text;
        if (!now.Found)
            return new Restoration(locator, found.Reading(), null, true, false, "it is no longer in the tree");

        var moved = now.Values != found;
        if (!moved)
            return new Restoration(locator, found.Reading(), now.Values.Reading(), false, true, null);

        Attempted<string?>? pressing;
        try
        {
            pressing = Put(subject, found, now.Values);
        }
        catch (Exception refused)
            when (refused is NotActionableException or InvalidOperationException)
        {
            // No count on this arm, and that is the honest answer rather than a zero: an act that
            // threw is one the bounded run never got to answer for.
            return new Restoration(
                locator, found.Reading(), subject.ReadOnce().Values.Reading(), true, false, refused.Message);
        }

        var after = subject.ReadOnce().Values;
        return after == found
            ? new Restoration(locator, found.Reading(), after.Reading(), true, true, null, pressing)
            : new Restoration(
                locator,
                found.Reading(),
                after.Reading(),
                true,
                false,
                "it did not go back to what it was",
                pressing);
    }

    /// <summary>
    /// Put one surface back, and hand over what the one act that retries took. Answered rather than
    /// dropped: the count is a finding about the control, and the caller is the only thing left
    /// that can put it anywhere a reader will see it.
    /// </summary>
    /// <param name="subject">The surface.</param>
    /// <param name="found">What it read when the case found it.</param>
    /// <param name="now">What it reads now.</param>
    /// <returns>What pressing the toggle took, or null where no toggle was pressed.</returns>
    private static Attempted<string?>? Put(Subject subject, PatternValues found, PatternValues now)
    {
        if (found.ExpandCollapse is { } wasOpen && now.ExpandCollapse != wasOpen)
        {
            if (wasOpen == "Expanded")
                Act.Expand(subject);
            else
                Act.Collapse(subject);
        }

        Attempted<string?>? pressing = null;
        if (found.Toggle is { } wasToggled && now.Toggle != wasToggled)
        {
            // A toggle has no setter, so it is pressed until it comes round to what it was —
            // bounded, because a control with more states than it admits to would loop forever.
            pressing = Retry.Bounded(
                () =>
                {
                    Act.Toggle(subject);
                    return subject.ReadOnce().Values.Toggle;
                },
                state => state == wasToggled,
                MostToggles);
        }

        if (found.IsSelected == true && now.IsSelected != true)
            Act.Select(subject);

        if (found.Range is { } wasAt && now.Range != wasAt)
            Act.SetRange(subject, wasAt);

        if (found.Value is { } wasSaying && now.Value != wasSaying && found.IsReadOnly != true)
            Act.SetValue(subject, wasSaying);

        return pressing;
    }
}

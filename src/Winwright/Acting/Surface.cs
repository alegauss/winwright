using System.Collections.ObjectModel;

using Winwright.Locating;

namespace Winwright.Acting;

/// <summary>What putting one surface back did.</summary>
/// <param name="Locator">What was put back, as the scenario wrote it.</param>
/// <param name="Was">What it read when the case found it.</param>
/// <param name="Now">What it reads now.</param>
/// <param name="Moved">Whether it had moved at all. Nothing is touched that did not.</param>
/// <param name="PutBack">Whether it is back where it was.</param>
/// <param name="Because">Why it is not, where it is not.</param>
public sealed record Restoration(
    string Locator, string? Was, string? Now, bool Moved, bool PutBack, string? Because)
{
    /// <summary>The one line a report shows.</summary>
    public override string ToString()
    {
        if (!Moved)
            return $"{Locator} was left as it was found.";

        return PutBack
            ? $"{Locator} was put back to \"{Was}\"."
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

        try
        {
            Put(subject, found, now.Values);
        }
        catch (Exception refused)
            when (refused is NotActionableException or InvalidOperationException)
        {
            return new Restoration(
                locator, found.Reading(), subject.ReadOnce().Values.Reading(), true, false, refused.Message);
        }

        var after = subject.ReadOnce().Values;
        return after == found
            ? new Restoration(locator, found.Reading(), after.Reading(), true, true, null)
            : new Restoration(
                locator, found.Reading(), after.Reading(), true, false, "it did not go back to what it was");
    }

    private static void Put(Subject subject, PatternValues found, PatternValues now)
    {
        if (found.ExpandCollapse is { } wasOpen && now.ExpandCollapse != wasOpen)
        {
            if (wasOpen == "Expanded")
                Act.Expand(subject);
            else
                Act.Collapse(subject);
        }

        if (found.Toggle is { } wasToggled && now.Toggle != wasToggled)
        {
            // A toggle has no setter, so it is pressed until it comes round to what it was —
            // bounded, because a control with more states than it admits to would loop forever.
            Retry.Bounded(
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
    }
}

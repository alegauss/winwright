using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Winwright.InApp;

/// <summary>One popup a host is holding open, and what it was set to before.</summary>
/// <param name="Name">What to call it in a report.</param>
/// <param name="Was">Whether it already stayed open, which is what putting it back restores.</param>
public sealed record HeldPopup(string Name, bool Was)
{
    /// <summary>Whether holding it changed anything at all.</summary>
    public bool Changed => !Was;

    /// <summary>The one phrase a report names it by.</summary>
    public override string ToString() => Changed ? Name : $"{Name} (already held)";
}

/// <summary>
/// Every popup under a root, held open for as long as this lives and put back afterwards.
/// <para>
/// A popup that closes when it loses mouse capture is right for a person and fatal for a capture:
/// the window is raised to the foreground, the popup goes, and the copy is a correct picture of a
/// window without it. Fixing that at one call site left the next popup preview to rediscover it, so
/// the rule belongs to the host — a preview has no hand to click with, and nothing about which page
/// happens to own a popup should decide whether it survives being photographed.
/// </para>
/// <para>
/// Worth knowing before reading the counts: <c>StaysOpen</c> is true by default, so most popups
/// need nothing and the one this exists for is the one whose author turned it off — light dismiss,
/// which is exactly the behaviour that is right for a person and fatal for a copy. A host that
/// reported holding twelve popups and changing none has still done its job.
/// </para>
/// <para>
/// Put back on disposal, because a host that leaves every popup pinned open has changed the
/// application it was only supposed to photograph.
/// </para>
/// </summary>
public sealed class PopupsHeld : IDisposable
{
    private readonly List<(Popup Popup, bool Was)> held = [];
    private bool released;

    internal PopupsHeld(DependencyObject root)
    {
        Root = root;
        Again();
    }

    /// <summary>What was walked.</summary>
    public DependencyObject Root { get; }

    /// <summary>Every popup being held, in the order the walk found them.</summary>
    public IReadOnlyList<HeldPopup> Held =>
        new ReadOnlyCollection<HeldPopup>(held.Select(one => new HeldPopup(Popups.Named(one.Popup), one.Was)).ToList());

    /// <summary>How many were changed rather than already staying open.</summary>
    public int Changed => held.Count(one => !one.Was);

    /// <summary>
    /// Walk again and hold whatever is new. A page that opens a popup after the host was built is
    /// ordinary, and a host that only ever looked once would photograph the one case it exists for
    /// exactly wrong.
    /// </summary>
    /// <returns>How many popups this walk newly took hold of.</returns>
    public int Again()
    {
        ObjectDisposedException.ThrowIf(released, this);

        var taken = 0;
        foreach (var popup in Popups.Under(Root))
        {
            if (held.Any(one => ReferenceEquals(one.Popup, popup)))
                continue;

            held.Add((popup, popup.StaysOpen));
            popup.StaysOpen = true;
            taken++;
        }

        return taken;
    }

    /// <summary>What is being held, said either way. Printed on every run rather than on a red.</summary>
    public string Sentence()
    {
        if (held.Count == 0)
            return "there is no popup under this host to hold open.";

        var names = string.Join(", ", Held.Select(one => one.ToString()));
        return $"holding {held.Count} popup(s) open, {Changed} of them changed: {names}.";
    }

    /// <summary>Put every popup back to what it was set to.</summary>
    public void Dispose()
    {
        if (released)
            return;

        released = true;
        foreach (var (popup, was) in held)
        {
            // Guarded one at a time: a popup whose window went while this was alive must not stop
            // the rest being put back, and a host that half-restored is worse than one that did not.
            try
            {
                popup.StaysOpen = was;
            }
            catch (InvalidOperationException)
            {
                // Its thread is gone. Nothing left to restore, and nothing worth raising over.
            }
        }
    }
}

/// <summary>Finding the popups under a root, and holding them open.</summary>
public static class Popups
{
    /// <summary>
    /// Every popup under <paramref name="root"/>, walked down the <em>logical</em> tree.
    /// <para>
    /// Logical and not visual, and that is the whole of it: a closed popup's child is not in the
    /// visual tree at all, and closed is exactly the state a popup has to be reached in — a walk
    /// that only found the open ones would find nothing to fix.
    /// </para>
    /// </summary>
    /// <param name="root">The element to walk under.</param>
    public static IReadOnlyList<Popup> Under(DependencyObject root)
    {
        ArgumentNullException.ThrowIfNull(root);
        Freezables.Insist(root, "the element being walked for popups");

        var found = new List<Popup>();
        Walk(root, found, new HashSet<DependencyObject>(ReferenceEqualityComparer.Instance));
        return new ReadOnlyCollection<Popup>(found);
    }

    /// <summary>Hold every popup under a root open until the answer is disposed.</summary>
    /// <param name="root">The element to walk under.</param>
    public static PopupsHeld Hold(DependencyObject root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return new PopupsHeld(root);
    }

    /// <summary>What a report calls one popup: its name, or what it is holding.</summary>
    internal static string Named(Popup popup)
    {
        if (!string.IsNullOrEmpty(popup.Name))
            return popup.Name;

        return popup.Child is null ? "(unnamed popup)" : $"(unnamed popup holding {popup.Child.GetType().Name})";
    }

    private static void Walk(DependencyObject node, List<Popup> found, HashSet<DependencyObject> seen)
    {
        if (!seen.Add(node))
            return;

        if (node is Popup popup)
        {
            found.Add(popup);

            // The child is reached through the popup rather than through the walk: a closed one is
            // in no tree the enumeration below would reach, and a popup inside a popup is real.
            if (popup.Child is DependencyObject child)
                Walk(child, found, seen);
        }

        foreach (var branch in LogicalTreeHelper.GetChildren(node))
        {
            if (branch is DependencyObject next)
                Walk(next, found, seen);
        }
    }
}

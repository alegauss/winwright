using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

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

    /// <summary>
    /// A picture of one popup's own tree, which is the surface nothing outside the process can
    /// photograph. WW347.
    /// <para>
    /// An open popup is its own top-level window, and a framework that draws a drop shadow behind it
    /// draws that shadow itself: WPF's is <c>style=0x96000000 ex=0x08080088</c> — WS_POPUP with no
    /// caption, layered with an alpha per pixel. So a harness routes it to the screen copy, which is
    /// the only capture out there that reaches a popup at all, and the copy is then refused because
    /// the soft edge it carries is a strip of whatever the popup is standing in front of. Both
    /// readings are right, and together they leave a real surface with no way to be photographed.
    /// </para>
    /// <para>
    /// This is the way through, and it is one only the application has. The child is an ordinary
    /// element in a tree this process owns, so it draws the way a window's does — no compositor, no z
    /// order, no shadow and no edge that is the desktop. Whether the popup is open does not come into
    /// it, which is the same property read from the other side: the tree is there either way, and a
    /// preview of a flyout nobody has clicked is a picture this can take and a copy never could.
    /// </para>
    /// </summary>
    /// <param name="popup">The popup to photograph.</param>
    /// <param name="path">Where to write the PNG.</param>
    /// <param name="background">What to compose behind it, or null to leave it transparent.</param>
    /// <param name="dpi">The resolution to draw at.</param>
    /// <exception cref="UnrenderableException">
    /// Where the popup is holding nothing, or holding something that is not an element with a
    /// layout — a picture of neither is an empty file, and an empty file is a successful render to
    /// everything that only checks one exists.
    /// </exception>
    public static RenderedPicture Picture(
        Popup popup, string path, Brush? background = null, double dpi = Render.DefaultDpi)
    {
        ArgumentNullException.ThrowIfNull(popup);
        Freezables.Insist(popup, "the popup being photographed");

        var element = Drawable(popup);
        return Render.ToFile(element, path, Settled(element), background, dpi);
    }

    /// <summary>
    /// The same, stopping at the bitmap, for a caller that has somewhere else to put it.
    /// </summary>
    /// <param name="popup">The popup to photograph.</param>
    /// <param name="background">What to compose behind it, or null to leave it transparent.</param>
    /// <param name="dpi">The resolution to draw at.</param>
    public static System.Windows.Media.Imaging.BitmapSource Bitmap(
        Popup popup, Brush? background = null, double dpi = Render.DefaultDpi)
    {
        ArgumentNullException.ThrowIfNull(popup);
        Freezables.Insist(popup, "the popup being photographed");

        var element = Drawable(popup);
        return Render.ToBitmap(element, Settled(element), background, dpi);
    }

    /// <summary>
    /// The size an open popup's child has already settled on, and null where it has none.
    /// <para>
    /// A closed popup's child is in no tree and has never been laid out, so the render takes the
    /// size it asks for — which is what that verb is for. An open one has been laid out by the
    /// popup's own root, and handing that size back is what keeps the picture the surface the
    /// application is showing: a child that stretches to fill would otherwise be measured against
    /// infinite room and refused for wanting all of it, which is a refusal about the render and not
    /// about the popup. Measured on an open WPF popup: the root arranges its child at the origin at
    /// exactly this size, so asking for it again changes nothing that was drawn.
    /// </para>
    /// </summary>
    private static Size? Settled(FrameworkElement element) =>
        element.IsArrangeValid && element.RenderSize is { Width: > 0, Height: > 0 }
            ? element.RenderSize
            : null;

    /// <summary>What a report calls one popup: its name, or what it is holding.</summary>
    internal static string Named(Popup popup)
    {
        if (!string.IsNullOrEmpty(popup.Name))
            return popup.Name;

        return popup.Child is null ? "(unnamed popup)" : $"(unnamed popup holding {popup.Child.GetType().Name})";
    }

    /// <summary>
    /// The element a popup is holding, refused where it is not one that can be drawn. Named
    /// separately from the render because the two refusals say different things: this one is about
    /// what the popup was given, and <see cref="UnrenderableException" /> from the render below is
    /// about what that element laid out to.
    /// </summary>
    private static FrameworkElement Drawable(Popup popup)
    {
        if (popup.Child is null)
            throw new UnrenderableException($"{Named(popup)} is holding nothing, so there is no tree to draw");

        if (popup.Child is not FrameworkElement element)
        {
            throw new UnrenderableException(
                $"{Named(popup)} is holding a {popup.Child.GetType().Name}, which has no layout of its own — "
                    + "a popup is photographed through the element it holds, and this is not one");
        }

        return element;
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

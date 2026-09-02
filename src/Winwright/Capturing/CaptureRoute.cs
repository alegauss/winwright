using Winwright.Windowing;

namespace Winwright.Capturing;

/// <summary>The two ways a picture of a window can be got.</summary>
public enum Route
{
    /// <summary>
    /// The application renders its own visual tree with no window shown. The default, because a
    /// render cannot photograph anything else: there is no foreground, no z order and no second
    /// instance to be confused with.
    /// </summary>
    OffScreenRender,

    /// <summary>
    /// A rectangle of the screen is copied. Reached only where a render cannot go, and always
    /// carrying the reason — a copy of a rectangle is a copy of whatever is in it.
    /// </summary>
    ScreenCopy,
}

/// <summary>Why a render cannot reach a window, where it cannot.</summary>
public enum OutOfReach
{
    /// <summary>It can. This window is a visual tree the application can be asked to render.</summary>
    Renderable,

    /// <summary>A menu. Its own top-level window, drawn by the system and in nobody's tree.</summary>
    Menu,

    /// <summary>A tooltip or a notification balloon, the same way.</summary>
    Balloon,

    /// <summary>
    /// A popup: a flyout, a combo box drop-down, a context surface. Its own top-level window,
    /// drawn by a framework rather than laid out in a tree the application can hand over — whether
    /// or not another window owns it (WW87).
    /// </summary>
    OwnedPopup,
}

/// <summary>
/// Which way a capture goes, and why.
/// <para>
/// A screen copy can photograph anything that happens to be in the rectangle — the window that
/// stole the foreground, the notification that arrived, the editor the run was started from. A
/// render of a visual tree cannot: there is no foreground, no z order and no second instance to be
/// confused with. So the render is the default, and it is the default by construction rather than
/// by convention: the screen copy is reachable only through a route that says why.
/// </para>
/// <para>
/// The one case a render cannot reach is a surface that is its own top-level window and in no
/// tree the application can hand over — a context menu, a balloon, a popup a framework drew. A
/// second window of the application is <em>not</em> one of those: it has a visual tree of its own,
/// so it is rendered like the first one and not photographed.
/// </para>
/// </summary>
public sealed record CaptureRoute
{
    private CaptureRoute(Route taken, OutOfReach reach, string because)
    {
        Taken = taken;
        Reach = reach;
        Because = because;
    }

    /// <summary>Which of the two this capture takes.</summary>
    public Route Taken { get; }

    /// <summary>Why a render cannot reach it, on a screen copy that was routed rather than forced.</summary>
    public OutOfReach Reach { get; }

    /// <summary>Why this route and not the other, in the words a receipt carries.</summary>
    public string Because { get; }

    /// <summary>Whether this capture takes the default.</summary>
    public bool Renders => Taken == Route.OffScreenRender;

    /// <summary>
    /// Route a capture of <paramref name="window"/> in an application whose main window is
    /// <paramref name="main"/>.
    /// </summary>
    /// <param name="window">The window to photograph.</param>
    /// <param name="main">The application's main window, which is what "owned by it" is measured against.</param>
    public static CaptureRoute For(TopLevelWindow window, TopLevelWindow main)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(main);

        return window.Handle == main.Handle
            ? Render("it is the application's own window, and its visual tree is renderable")
            : For(window);
    }

    /// <summary>
    /// Route a capture of <paramref name="window"/> in an application that has no main window to
    /// measure it against.
    /// <para>
    /// WW320. The main window answers one question — is this it — and every other answer is read
    /// off the window's own class and ownership. An application showing only a menu has nothing to
    /// pass as the second argument, and both ways round it are wrong: the menu as its own main
    /// answers Render, which is the one thing a menu is not, and <see cref="Forced" /> records
    /// Renderable, claiming a render would have worked and somebody chose otherwise.
    /// </para>
    /// <para>
    /// Measured in freewilly, whose menu verb draws a menu with no icon and no window behind it on
    /// purpose — which is the surface the screen copy exists for and the one thing that had no
    /// route to it.
    /// </para>
    /// </summary>
    /// <param name="window">The window to photograph.</param>
    public static CaptureRoute For(TopLevelWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        // Before the ownership test, because a menu and a balloon are both owned and neither is a
        // popup anybody put in a tree: naming them as popups would send a reader looking for one.
        if (IsMenu(window.ClassName))
            return Copy(OutOfReach.Menu, window);

        if (IsBalloon(window.ClassName))
            return Copy(OutOfReach.Balloon, window);

        // WW87. Owned, or drawn as a popup and owned by nothing. The second half is what freewilly's
        // menu is: a WinForms drop-down shown with no form behind it, so GW_OWNER answers zero and
        // the ownership test alone called it a window of the application and routed it to a render
        // that has no tree to draw. Both arms are the same surface — a framework put it up, and
        // nothing the application can hand over holds it.
        if (window.IsOwned || window.Popup)
            return Copy(OutOfReach.OwnedPopup, window);

        // A window of the application, which has a visual tree of its own. The render reaches it by
        // being pointed at that tree rather than at the first one.
        return Render($"{window} is a window of the application, so its own visual tree is renderable");
    }

    /// <summary>
    /// A screen copy the caller insisted on. The reason is required and it is the whole of the
    /// safeguard: a copy taken where a render would have worked is a decision somebody made, and
    /// one nobody can find later is one nobody made.
    /// </summary>
    /// <param name="because">Why a render would not do here.</param>
    /// <exception cref="ArgumentException">Where no reason was given.</exception>
    public static CaptureRoute Forced(string because)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(because);
        return new CaptureRoute(Route.ScreenCopy, OutOfReach.Renderable, because.Trim());
    }

    /// <summary>
    /// What can still photograph a surface the layer refusal turned away, where anything can. WW347.
    /// <para>
    /// A popup a framework drew is layered for the drop shadow it draws itself — WPF's reads
    /// <c>ex=0x08080088</c>, an alpha per pixel — so the two readings this route composes with are
    /// both right and together they close every way in: the render cannot reach a surface in no tree
    /// the application can hand over, and the copy that can reach it is refused for the soft edge it
    /// would carry.
    /// </para>
    /// <para>
    /// The narrowing is real and this is where it is said out loud, because a refusal an adopter
    /// cannot act on is the half of it that was avoidable. The way through is the application's own:
    /// a popup's child is an ordinary element in a tree that process owns, and the in-app half draws
    /// it with nothing composited behind it. Empty for a window that is not a popup, which is a
    /// window the render already reaches.
    /// </para>
    /// </summary>
    /// <param name="window">The window that was refused.</param>
    public static string StillReachable(TopLevelWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        return window.Popup || window.IsOwned
            ? " It is a popup, so the way through is the application's own half: a popup's child is an "
                + "element in a tree that process owns, and Winwright.InApp's Popups.Picture draws it "
                + "with nothing composited behind it."
            : "";
    }

    /// <summary>The route in the sentence a receipt carries, said either way.</summary>
    public string Sentence() => Renders
        ? $"rendered off-screen: {Because}."
        : $"copied from the screen: {Because}.";

    /// <summary>The one phrase a trace names it by.</summary>
    public override string ToString() => Renders ? "off-screen render" : "screen copy";

    private static CaptureRoute Render(string because) =>
        new(Route.OffScreenRender, OutOfReach.Renderable, because);

    private static CaptureRoute Copy(OutOfReach reach, TopLevelWindow window)
    {
        var what = reach switch
        {
            OutOfReach.Menu => "a menu",
            OutOfReach.Balloon => "a tooltip or balloon",
            _ => window.IsOwned ? "a popup owned by another window" : "a popup a framework drew",
        };

        return new CaptureRoute(
            Route.ScreenCopy,
            reach,
            $"{window} is {what}, which is its own top-level window and in no tree the application can render");
    }

    /// <summary>The system menu class. Every menu in Windows is one of these, whoever opened it.</summary>
    private static bool IsMenu(string className) => string.Equals(className, "#32768", StringComparison.Ordinal);

    private static bool IsBalloon(string className) =>
        className.StartsWith("tooltips_class32", StringComparison.OrdinalIgnoreCase);
}

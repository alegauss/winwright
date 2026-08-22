using System.Windows;
using System.Windows.Controls;

using Winwright.InApp;

namespace Winwright.Fixture;

/// <summary>
/// The surface protocol, implemented once.
/// <para>
/// It exists in one product and would be copied into the next, which is exactly how two
/// implementations of one line format come to disagree. Here it has an owner: this framework's own
/// suite drives this implementation, so an adopting project has something to copy that is known to
/// be current rather than something that was current when it was copied.
/// </para>
/// <para>
/// Every part of it is conditional on the harness having asked. An application shipped to its
/// users reports nothing, dumps nothing and writes no file — which is what makes the protocol safe
/// to leave in a release rather than something to remember to take out.
/// </para>
/// </summary>
public static class Protocol
{
    /// <summary>What the fixture calls the surfaces it reports, so a case has names to ask for.</summary>
    public static IReadOnlyList<string> Surfaces { get; } = ["the window", "the panes", "the report page"];

    /// <summary>
    /// Report what was drawn and dump the geometry, once the window has a source and a layout.
    /// </summary>
    /// <param name="window">The window, laid out.</param>
    /// <param name="panes">The tab control on it.</param>
    /// <param name="page">The pane currently showing, or null where none is realised.</param>
    /// <returns>How many surfaces were reported, which is zero where nothing asked.</returns>
    public static int Report(Window window, FrameworkElement panes, FrameworkElement? page)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(panes);

        // The layout has to be settled before anything is measured: a rectangle read mid-arrange
        // is a rectangle nothing ever drew, and it would be reported as confidently as a real one.
        window.UpdateLayout();

        var reported = 0;
        reported += Winwright.InApp.Surfaces.Report("the window", window) is null ? 0 : 1;
        reported += Winwright.InApp.Surfaces.Report("the panes", panes) is null ? 0 : 1;

        if (page is not null)
            reported += Winwright.InApp.Surfaces.Report("the report page", page) is null ? 0 : 1;

        Geometry.Dump(window);
        return reported;
    }

    /// <summary>
    /// Hold every popup under the window open for as long as the run lasts.
    /// <para>
    /// The host owns this rule and not the page: a preview has no hand to click with, and fixing
    /// it at one call site leaves the next popup to rediscover it.
    /// </para>
    /// </summary>
    /// <param name="window">The window to walk.</param>
    public static PopupsHeld Hold(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        return Popups.Hold(window);
    }

    /// <summary>What a capture of this application should be drawn on, as the application says.</summary>
    /// <param name="element">The element about to be rendered.</param>
    public static ChosenBackground Background(FrameworkElement element) => Backgrounds.Insist(element);
}

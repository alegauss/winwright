using Winwright.Acting;

namespace Winwright.Tests;

/// <summary>
/// WW181's original signature, kept exactly, because a sweep that finds nothing is a green about an
/// empty set and this repository does not accept one.
/// <para>
/// Nothing calls it and nothing should. It reads the flyout, keeps the one bit saying whether that
/// worked, and answers a list — which has two states, so the run where the shell would not open is
/// reported as the run where the tray was clean. That is the defect preserved rather than described,
/// so <see cref="Flattening" /> is measured against a real one instead of against an argument.
/// </para>
/// <para>
/// The alternative was a comment claiming the rule would have caught it. WW191 exists because
/// WW182's rule carried exactly that claim and it was false.
/// </para>
/// </summary>
internal static class TheShapeWW181Shipped
{
    /// <summary>The tips an ended run left, or so this would have a reader believe.</summary>
    internal static IReadOnlyList<string> Showing()
    {
        var names = new List<string>(NotificationArea.Showing().Select(one => one.Name));

        if (NotificationArea.OpenOverflow().Held)
            names.AddRange(NotificationArea.Hidden().Select(one => one.Name));

        return TrayGhosts.Among(names, TrayGhosts.Running);
    }
}

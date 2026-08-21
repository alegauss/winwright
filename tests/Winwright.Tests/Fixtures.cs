using Winwright.Verdicts;

namespace Winwright.Tests;

/// <summary>
/// The two absences these tests keep reaching for, both taken from claude-tray: a machine whose
/// notification area already has a resident tray, and one where no profile was ever registered.
/// </summary>
public static class Fixtures
{
    /// <summary>Absent because a tray is already resident, so the menu case has to refuse.</summary>
    public static Precondition FreeNotificationArea { get; } =
        Precondition.Absent("a free notification area", "a tray is already resident");

    /// <summary>Absent because nothing registered a profile, so no report can be rendered.</summary>
    public static Precondition RegisteredProfile { get; } =
        Precondition.Absent("a registered profile", "no profile registered");
}

using System.Windows;
using System.Windows.Media;

using Winwright.InApp;

namespace Adopter;

/// <summary>
/// What an application takes the in-app half for, in one file.
/// <para>
/// It is deliberately not a demonstration. Every call here is one an adopting application really
/// makes, and the point of compiling them from outside this repository is that the package carries
/// them — a type that is public in the source tree and absent from the package is a difference
/// nothing in this repository would otherwise notice.
/// </para>
/// </summary>
public static class Adopted
{
    /// <summary>Whether this application's coordinates mean what they say.</summary>
    public static bool CoordinatesAreTrustworthy => Coordinates.Trustworthy;

    /// <summary>What the display awareness turned out to be, in the sentence the package writes.</summary>
    public static string DisplaySentence() => Coordinates.Sentence();

    /// <summary>A brush that can cross threads, which is what a harness reading one needs.</summary>
    public static Brush Shareable() => Freezables.Shared(new SolidColorBrush(Colors.SlateBlue));

    /// <summary>Run work on the application's own dispatcher, bounded.</summary>
    public static string OnTheApartment(Window window) =>
        Apartment.Run(() => window.Title, within: Apartment.DefaultLimit, named: "the adopter's title");

    /// <summary>
    /// Dump the geometry of a live tree, which is the other half of a capture. Spelled in full
    /// because the package's own type shares a name with the presentation stack's, which is a
    /// thing an adopting application meets on its first line and not in a note.
    /// </summary>
    public static GeometryDumped Geometry(UIElement root) => Winwright.InApp.Geometry.Of(root);
}

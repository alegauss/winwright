using System.Windows;
using System.Windows.Media;

namespace Winwright.InApp;

/// <summary>Raised where nothing in the application said what a capture should be drawn on.</summary>
public sealed class NoBackgroundException : InvalidOperationException
{
    /// <summary>Say what was looked for and where.</summary>
    public NoBackgroundException(string message)
        : base(message)
    {
    }

    /// <summary>Unused. Present because an exception with no default shapes is awkward to catch.</summary>
    public NoBackgroundException()
        : base("nothing in this application says what a capture should be drawn on")
    {
    }

    /// <summary>Unused. Present for the same reason.</summary>
    public NoBackgroundException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>Which of the two sources answered.</summary>
public enum Backdrop
{
    /// <summary>The application's own theme, by the key it declares its capture background under.</summary>
    Theme,

    /// <summary>The window the element is in, read off the brush it is actually painted with.</summary>
    ObservedWindow,

    /// <summary>Neither. Not a colour, and never quietly turned into one.</summary>
    Unanswered,
}

/// <summary>
/// What a capture is to be drawn on, and which source said so.
/// </summary>
public sealed record ChosenBackground
{
    internal ChosenBackground(Backdrop from, Brush? brush, string key, string because)
    {
        From = from;
        Brush = brush;
        Key = key;
        Because = because;
    }

    /// <summary>Which source answered.</summary>
    public Backdrop From { get; }

    /// <summary>The brush to compose behind the render, frozen. Null where nothing answered.</summary>
    public Brush? Brush { get; }

    /// <summary>The key that was looked for, whether or not it was there.</summary>
    public string Key { get; }

    /// <summary>Where the answer came from, or why there is none.</summary>
    public string Because { get; }

    /// <summary>Whether anything answered at all.</summary>
    public bool Answered => From != Backdrop.Unanswered;

    /// <summary>The colour, where the answer was a flat one. Null for a gradient or an image.</summary>
    public Color? Colour => Brush is SolidColorBrush solid ? solid.Color : null;

    /// <summary>
    /// Which of the two answered, in the words a run prints. Printed on every run and not only on
    /// a failure, because the difference between the two is a picture nobody can read — and nobody
    /// looking at one can tell which source produced it.
    /// </summary>
    public string Sentence() => From switch
    {
        Backdrop.Theme => $"the background is the theme's own '{Key}': {Painted()}.",
        Backdrop.ObservedWindow => $"the background is the observed window colour, {Painted()} — "
            + $"nothing declares '{Key}', so it was read off what the window is painted with.",
        _ => $"nothing says what to draw the capture on: {Because}.",
    };

    /// <summary>The one phrase a receipt names it by.</summary>
    public override string ToString() => From switch
    {
        Backdrop.Theme => $"{Painted()} (the theme's '{Key}')",
        Backdrop.ObservedWindow => $"{Painted()} (the observed window colour)",
        _ => "nothing",
    };

    private string Painted() =>
        Colour?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? Brush?.GetType().Name ?? "nothing";
}

/// <summary>
/// Where a capture's background comes from.
/// <para>
/// The classic system palette was the obvious source and is measurably wrong: it answers white on
/// a machine whose application window is dark, so the first capture taken that way came back as
/// pale text on nothing — correct in every respect and unreadable. It is not consulted here at
/// all, and that omission is the whole design rather than an oversight.
/// </para>
/// <para>
/// The application's own key is asked first, because an application that theme-switches knows what
/// it is painting on and the desktop does not. The observed window colour is the fallback, read
/// off the brush the window is actually painted with. Which of the two answered is printed on
/// every run: the difference between them is a picture nobody can read, and nobody looking at one
/// can tell which source produced it.
/// </para>
/// </summary>
public static class Backgrounds
{
    /// <summary>
    /// The resource key an application declares its capture background under. Named rather than
    /// inferred: a key this looked up by guessing is one an application cannot deliberately set.
    /// </summary>
    public const string DefaultKey = "WinwrightCaptureBackground";

    /// <summary>
    /// Choose a background for a render of <paramref name="element"/>.
    /// </summary>
    /// <param name="element">The element about to be rendered, or null to ask the application alone.</param>
    /// <param name="key">The resource key the application declares its capture background under.</param>
    public static ChosenBackground Choose(FrameworkElement? element, string key = DefaultKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var wanted = key.Trim();

        var unshareable = "";

        if (Painted(Resource(element, wanted)) is Brush themed)
        {
            // A brush that cannot cross to a capture thread is not an answer a capture can use, so
            // it falls through to the window rather than being handed back as a background of null.
            if (Frozen(themed) is Brush shared)
                return new ChosenBackground(Backdrop.Theme, shared, wanted, $"declared under '{wanted}'");

            unshareable = $"'{wanted}' is a {themed.GetType().Name}, which cannot be frozen for a capture thread; ";
        }

        var window = element is null ? MainWindow() : Window.GetWindow(element) ?? MainWindow();
        if (Painted(window?.Background) is Brush observed && Frozen(observed) is Brush shareable)
        {
            return new ChosenBackground(
                Backdrop.ObservedWindow, shareable, wanted, "read off the window's own brush");
        }

        return new ChosenBackground(
            Backdrop.Unanswered,
            null,
            wanted,
            unshareable + (window is null
                ? $"nothing declares '{wanted}' and this element is in no window to read a colour off"
                : $"nothing declares '{wanted}' and the window is painted with no brush this capture can use"));
    }

    /// <summary>
    /// The same, refusing where nothing answered. Reach for this rather than composing on nothing:
    /// a render on no background is the unreadable capture this whole reading exists over, and it
    /// is a file that looks exactly as successful as a readable one.
    /// </summary>
    /// <param name="element">The element about to be rendered.</param>
    /// <param name="key">The resource key the application declares its capture background under.</param>
    /// <exception cref="NoBackgroundException">Where neither source answered.</exception>
    public static ChosenBackground Insist(FrameworkElement? element, string key = DefaultKey)
    {
        var chosen = Choose(element, key);
        return chosen.Answered
            ? chosen
            : throw new NoBackgroundException(
                $"{chosen.Sentence()} Declare a brush under '{key}' in the application's resources, or give the "
                    + "window a background of its own.");
    }

    private static object? Resource(FrameworkElement? element, string key)
    {
        // The element first and the application second, and that is the order WPF itself resolves
        // in: a page that overrides the key for its own capture is overriding it on purpose.
        try
        {
            return element?.TryFindResource(key) ?? Application.Current?.TryFindResource(key);
        }
        catch (Exception unresolvable) when (unresolvable is InvalidOperationException or ResourceReferenceKeyNotFoundException)
        {
            return null;
        }
    }

    private static Window? MainWindow()
    {
        try
        {
            return Application.Current?.MainWindow;
        }
        catch (InvalidOperationException)
        {
            // An application still starting up has no main window yet, and asking says so by
            // throwing rather than by answering null.
            return null;
        }
    }

    /// <summary>
    /// A resource read as something to paint with. A colour is accepted as well as a brush,
    /// because a theme that declares its palette as colours is a theme, not a mistake.
    /// </summary>
    private static Brush? Painted(object? value) => value switch
    {
        SolidColorBrush { Color.A: 0 } => null,
        Brush brush => brush,
        Color colour when colour.A != 0 => new SolidColorBrush(colour),
        _ => null,
    };

    /// <summary>
    /// A copy that any capture thread may share. One rule, in one place: the brush found in the
    /// resources belongs to the application, and freezing that one would make its own theme switch
    /// throw later.
    /// </summary>
    private static Brush? Frozen(Brush brush) => Freezables.Shareable(brush);
}

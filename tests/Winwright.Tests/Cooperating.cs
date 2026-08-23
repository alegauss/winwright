using System.Collections.ObjectModel;
using System.Reflection;
using Winwright.Locating;

namespace Winwright.Tests;

/// <summary>What a verb needs from the application before it can answer.</summary>
internal enum Cooperation
{
    /// <summary>Nothing. It reads what any Windows application already offers.</summary>
    None,

    /// <summary>
    /// An artefact the in-app half produced — a geometry dump, a reported surface. The engine
    /// cannot call the in-app half at all, so this is never an API call: it is the application
    /// having written something down before the harness came looking.
    /// </summary>
    TheInAppHalf,
}

/// <summary>One verb, and what it needs before it can answer.</summary>
/// <param name="Named">The verb, as Type.Method.</param>
/// <param name="Needs">What the application has to have done.</param>
/// <param name="NeedsTheDesk">
/// Whether it synthesises input, which is a different axis and a different fixture: a verb can need
/// no cooperation from the application and still need a foreground nobody can promise it.
/// </param>
/// <param name="Because">The sentence a reader needs.</param>
internal sealed record VerbNeeds(string Named, Cooperation Needs, bool NeedsTheDesk, string Because)
{
    /// <summary>Whether one bare window with no help from anybody is enough to run it.</summary>
    public bool RunsAgainstAnything => Needs == Cooperation.None && !NeedsTheDesk;

    /// <summary>The one line the catalogue prints.</summary>
    public override string ToString()
    {
        var needs = Needs == Cooperation.None ? "nothing" : "the in-app half";
        var desk = NeedsTheDesk ? ", and a desk" : "";
        return $"{Named,-30} needs {needs}{desk}: {Because}";
    }
}

/// <summary>
/// Every verb this engine offers, against what it needs from the application to answer.
/// <para>
/// WW141. This block's criterion says every verb needing no cooperation runs against an application
/// that references nothing, which is what keeps this usable on a product nobody here owns. Hundreds
/// of cases do exactly that — they build a bare Win32 window and drive it — so the criterion held
/// by accident of how the fixtures were written, and no case anywhere stated it.
/// </para>
/// <para>
/// The list is the durable half. A rule met by whoever remembers is met by nobody: the day a verb
/// quietly starts reading something only the in-app half provides, every one of those hundreds
/// still passes against fixtures that happen to have it, and the first person to find out owns a
/// product this cannot drive.
/// </para>
/// <para>
/// Its opposite is here for the same reason read the other way. An adopter deciding what the
/// package buys them can read which verbs go dark without it, which is a shorter list than they
/// would guess.
/// </para>
/// </summary>
internal static class Cooperating
{
    /// <summary>The namespaces a verb lives in. Reading and acting, and nothing else.</summary>
    public static IReadOnlyList<string> Namespaces { get; } =
        new ReadOnlyCollection<string>(["Winwright.Acting", "Winwright.Locating"]);

    /// <summary>Every verb, and what it needs.</summary>
    public static IReadOnlyList<VerbNeeds> Known { get; } = new ReadOnlyCollection<VerbNeeds>(
    [
        // The readings. Nothing here touches an application that did not already exist.
        new("Resolve.Once", Cooperation.None, false, "one look for a locator under a root"),
        new("Resolve.Until", Cooperation.None, false, "the same, polled to a deadline"),
        new("Resolve.Matching", Cooperation.None, false, "every element one step matches"),
        new("Inspect.Window", Cooperation.None, false, "the control view under a window handle"),
        new("Inspect.Under", Cooperation.None, false, "the same under an element already in hand"),
        new("Inspect.Render", Cooperation.None, false, "that tree as lines a person reads"),
        new("Inspect.Rendered", Cooperation.None, false, "the same lines with their parts kept"),
        new("Inspect.Line", Cooperation.None, false, "one element as its own line"),
        new("Inspect.CopyableStep", Cooperation.None, false, "the step a line opens with, where it has one"),
        new("ElementFacts.Of", Cooperation.None, false, "what UI Automation says about one element"),
        new("PatternValues.Of", Cooperation.None, false, "what its patterns read, as values"),
        new("ActionabilityCheck.Of", Cooperation.None, false, "whether an element can take an act"),
        new("ActionabilityCheck.Worded", Cooperation.None, false, "one of the four as a person says it"),
        new("Admitted.To", Cooperation.None, false, "the door an act reaches its element through"),
        new("Admitted.Of", Cooperation.None, false, "the same against a look already taken"),
        new("Subject.Unguarded", Cooperation.None, false, "a subject with no project behind it"),
        new("Locator.Parse", Cooperation.None, false, "a locator out of its text"),
        new("Locator.TryParse", Cooperation.None, false, "the same without throwing"),
        new("UiaVocabulary.IsControlType", Cooperation.None, false, "whether a name is a control type"),
        new("UiaVocabulary.IsPattern", Cooperation.None, false, "whether a name is a pattern"),
        new("UiaVocabulary.ControlTypeFor", Cooperation.None, false, "the control type one name means"),
        new("UiaVocabulary.Nearest", Cooperation.None, false, "the nearest name to a misspelt one"),
        new("Attempt.Once", Cooperation.None, false, "one look, which is what asking whether a thing has gone needs"),
        new("Attempt.Until", Cooperation.None, false, "a deadline on a sighting"),
        new("Attempt.UntilTrue", Cooperation.None, false, "a deadline on a condition"),
        new("Retry.Bounded", Cooperation.None, false, "an act attempted to a cap and counted"),
        new("Retry.Recorded", Cooperation.None, false, "that count stamped onto the step a trace records"),
        new("Focus.In", Cooperation.None, false, "what holds the focus, read against the application under test"),
        new("Focus.Held", Cooperation.None, false, "the same, or nothing where the focus went somewhere else"),
        new("Preflight.Check", Cooperation.None, false, "what each declared act needs against the tree"),
        new("Preflight.Offers", Cooperation.None, false, "what one locator's element offers"),
        new("Preflight.Require", Cooperation.None, false, "the same, stopping on a refusal"),

        // The pattern acts. They ask the control rather than the desktop, which is the whole
        // reason they exist and the reason none of them needs a foreground.
        new("Act.Invoke", Cooperation.None, false, "press it through its own accessibility peer"),
        new("Act.Toggle", Cooperation.None, false, "flip it through the toggle pattern"),
        new("Act.SetValue", Cooperation.None, false, "write through the value pattern"),
        new("Act.SetRange", Cooperation.None, false, "move through the range pattern"),
        new("Act.Select", Cooperation.None, false, "select through the selection-item pattern"),
        new("Act.Expand", Cooperation.None, false, "open through the expand-collapse pattern"),
        new("Act.Collapse", Cooperation.None, false, "shut through the same"),
        new("Selecting.Confirmed", Cooperation.None, false, "select and confirm, escalating only if allowed"),
        new("Surface.AsFound", Cooperation.None, false, "put the controls back the way they were"),
        new("Pick.Values", Cooperation.None, false, "every value a picker holds"),

        // The declared readings about pointer acts, which read a tree and never send anything.
        new("Pointer.Check", Cooperation.None, false, "each declared reason against the tree"),
        new("Pointer.Reasons", Cooperation.None, false, "what needs a desktop, grouped by why"),
        new("Pointer.Summarise", Cooperation.None, false, "the same as one block of text"),
        new("Pointer.Worded", Cooperation.None, false, "one reason as a person says it"),
        new("Pointer.MayGetAPeer", Cooperation.None, false, "whether a peer would make the act unnecessary"),

        // The desk is a different axis. None of these needs the application to cooperate; every
        // one of them needs a foreground Windows does not always grant.
        new("Pointer.Click", Cooperation.None, true, "synthesised mouse input"),
        new("Pointer.DoubleClick", Cooperation.None, true, "two of the same"),
        new("Pointer.Run", Cooperation.None, true, "a declared pointer act"),
        new("Keyboard.Type", Cooperation.None, true, "synthesised keys into a control"),
        new("Keyboard.Run", Cooperation.None, true, "a declared typing act"),
        new("Traversal.Press", Cooperation.None, true, "a traversal key at a window"),
        new("Traversal.Nudge", Cooperation.None, true, "a range moved by a key"),
        new("Traversal.WhoHasFocus", Cooperation.None, false, "what holds the focus, asked and not pressed"),
        new("Pick.Value", Cooperation.None, true, "reach a value, by keyboard where the pattern will not"),
        new("Menu.Enter", Cooperation.None, true, "enter a menu bar the way a keyboard user does"),
        new("Menu.To", Cooperation.None, true, "walk to an entry"),
        new("Menu.Expand", Cooperation.None, true, "open a submenu"),
        new("Menu.Dismiss", Cooperation.None, true, "close what was opened"),
        new("Menu.Highlighted", Cooperation.None, false, "what a menu is highlighting, read and not pressed"),
        new("NotificationArea.Tray", Cooperation.None, false, "the taskbar's notification area"),
        new("NotificationArea.Overflow", Cooperation.None, false, "the flyout, where it is open"),
        new("NotificationArea.Chevron", Cooperation.None, false, "the button that opens it"),
        new("NotificationArea.Showing", Cooperation.None, false, "the icons on the bar"),
        new("NotificationArea.Hidden", Cooperation.None, false, "the icons in the flyout"),
        new("NotificationArea.ElementFor", Cooperation.None, false, "one icon as an element"),
        new("NotificationArea.Find", Cooperation.None, true, "an icon by name, opening the flyout to look"),
        new("NotificationArea.OpenOverflow", Cooperation.None, true, "open the flyout"),
        new("NotificationArea.CloseOverflow", Cooperation.None, true, "shut it again"),
        new("NotificationArea.OpenMenu", Cooperation.None, true, "an icon's context menu, by key"),
    ]);

    /// <summary>The verbs a bare window is enough for, which is what the run drives.</summary>
    public static IReadOnlyList<VerbNeeds> AgainstAnything() => new ReadOnlyCollection<VerbNeeds>(
        Known.Where(one => one.RunsAgainstAnything).ToList());

    /// <summary>The verbs that go dark without the in-app half, for an adopter deciding.</summary>
    public static IReadOnlyList<VerbNeeds> NeedingTheHalf() => new ReadOnlyCollection<VerbNeeds>(
        Known.Where(one => one.Needs == Cooperation.TheInAppHalf).ToList());

    /// <summary>
    /// Every verb the engine actually offers, by Type.Method.
    /// <para>
    /// Every public static method the two namespaces export, and not a signature the reflection
    /// judges. A rule reading parameters looked cleaner and was wrong twice over: it missed the
    /// verbs that take nothing and read the live desktop — the notification area's own five — and
    /// it missed the one taking an array of subjects. Naming the namespaces instead means a method
    /// added to either has to be classified before this passes, which is the whole point.
    /// </para>
    /// </summary>
    public static IReadOnlyList<string> Named() => new ReadOnlyCollection<string>(
        typeof(Resolve).Assembly
            .GetExportedTypes()
            .Where(one => Namespaces.Contains(one.Namespace))
            .SelectMany(one => one.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Select(method => $"{one.Name}.{method.Name}"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(one => one, StringComparer.Ordinal)
            .ToList());

    /// <summary>The catalogue as a person reads it, the counts first.</summary>
    public static IReadOnlyList<string> Render() => new ReadOnlyCollection<string>(
        [
            $"{Known.Count} verbs: {AgainstAnything().Count} run against any application, "
                + $"{Known.Count(one => one.NeedsTheDesk)} also need a desk, "
                + $"{NeedingTheHalf().Count} need the in-app half.",
            .. Known.Select(one => $"  {one}"),
        ]);
}

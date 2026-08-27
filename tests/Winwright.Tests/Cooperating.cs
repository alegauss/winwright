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
    /// <summary>
    /// What makes a file one a verb can live in: it reaches the application under test.
    /// <para>
    /// WW209. The scope was two namespaces, typed, and the case above it says <em>every verb the
    /// engine offers</em>. The engine has ten namespaces and about a hundred and fifty public
    /// statics outside those two — most of them plainly not verbs in this sense, since composing a
    /// verdict or rendering a sentence needs nothing from any application. That is what the
    /// catalogue would have recorded if asked, and it was never asked.
    /// </para>
    /// <para>
    /// Derived rather than declared. A verb is something that reaches the thing under test, and a
    /// file that reaches it says so by naming an automation element, a window handle or a process.
    /// Three namespaces — <c>Verdicts</c>, <c>Tracing</c> and <c>Projects</c> — name none of the
    /// three in any of their files, which is a measurement rather than a promise, and
    /// <c>NoCooperationTests</c> takes it again on every run.
    /// </para>
    /// <para>
    /// It was four until WW57. <c>Scenarios</c> held only declarations, and then it gained the
    /// engine that runs one — which resolves locators under a root element and is a verb by every
    /// reading here. The measurement is what found it, on the run after the file was written, which
    /// is the whole reason it is taken rather than promised.
    /// </para>
    /// </summary>
    public static IReadOnlyList<string> Reaching { get; } = new ReadOnlyCollection<string>(
        ["AutomationElement", "Win32.", "System.Diagnostics.Process", "Process.Start"]);

    /// <summary>
    /// Where a verb lives by default: reading and acting.
    /// <para>
    /// The floor and not the whole, which is the correction WW209 made. Scoping only by what reaches
    /// the application would have dropped fifteen entries — <c>Locator.Parse</c>,
    /// <c>Attempt.Until</c>, the vocabulary, the retries — and each of those is a verb an adopter
    /// calls whose answer is that it needs nothing. Recording that is what the catalogue is for, so
    /// the two namespaces stay and the derived half is added to them.
    /// </para>
    /// </summary>
    public static IReadOnlyList<string> Namespaces { get; } =
        new ReadOnlyCollection<string>(["Winwright.Acting", "Winwright.Locating"]);

    /// <summary>Every engine type whose own file reaches the application under test.</summary>
    public static IReadOnlyList<string> Driving() => driving.Value;

    private static readonly Lazy<IReadOnlyList<string>> driving = new(() => new ReadOnlyCollection<string>(
        Checkout.SourcesIn(Checkout.Engine)
            .Where(one => File.ReadLines(one).Select(Checkout.Code)
                .Any(line => Reaching.Any(mark => line.Contains(mark, StringComparison.Ordinal))))
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .OrderBy(one => one, StringComparer.Ordinal)
            .ToList()));

    /// <summary>Every verb, and what it needs.</summary>
    public static IReadOnlyList<VerbNeeds> Known { get; } = new ReadOnlyCollection<VerbNeeds>(
    [
        // The readings. Nothing here touches an application that did not already exist.
        new("Resolve.Once", Cooperation.None, false, "one look for a locator under a root"),
        new("Resolve.Until", Cooperation.None, false, "the same, polled to a deadline"),
        new("Resolve.Matching", Cooperation.None, false, "every element one step matches"),
        new("Resolve.Beneath", Cooperation.None, false, "every element a locator's steps before its last one reach"),
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

        // WW225. The same three as a step answers them, so the axis is the same: nothing from the
        // application, and a foreground Windows does not always grant — which is why each one carries
        // what it needed rather than leaving a reader to read it off a value that did not move.
        new("Synthesised.Type", Cooperation.None, true, "typing, as a step's own result"),
        new("Synthesised.Click", Cooperation.None, true, "a click a case had to say the reason for"),
        new("Synthesised.Nudge", Cooperation.None, true, "an arrow key at a range control, as a step"),
        new("Synthesised.Press", Cooperation.None, true, "a traversal key at the window, as a step"),
        new("Pick.Value", Cooperation.None, true, "reach a value, by keyboard where the pattern will not"),

        // WW267. Told a position rather than a value, for a picker holding the machine's own data.
        new("Pick.At", Cooperation.None, true, "reach whatever sits at a position, by the same two routes"),

        // WW254. The same walk as a step answers it. On the desk axis with the four above rather than
        // beside the pattern acts: the pattern route needs nothing, but the fallback is keys, and a
        // verb filed as needing nothing would promise that for the runs where it does.
        new("Synthesised.Pick", Cooperation.None, true, "reaching a value in a picker, as a step"),
        new("Synthesised.PickAt", Cooperation.None, true, "reaching a position in a picker, as a step"),
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
        new("NotificationArea.Reachable", Cooperation.None, false, "whether there is a notification area to look at"),
        new("NotificationArea.Placing", Cooperation.None, false,
            "whether this desk is placing icons at all, wherever they land — the question a case asks "
                + "after failing to find its own, and the one that tells a slow shell from an absent icon"),
        new("NotificationArea.Find", Cooperation.None, true, "an icon by name, opening the flyout to look"),
        new("NotificationArea.OpenOverflow", Cooperation.None, true, "open the flyout"),
        new("NotificationArea.CloseOverflow", Cooperation.None, true, "shut it again"),
        new("NotificationArea.OpenMenu", Cooperation.None, true, "an icon's context menu, by key"),

        // --- WW209, and the whole of what widening the scope found -----------------------------------
        // Eighteen verbs an adopter can call that this catalogue had never been shown. Every one of
        // them reaches the application or the desk, which is why the derived half of the scope finds
        // them, and not one needs the in-app half — which is the answer the criterion wanted and
        // could not have got from a list that stopped at two namespaces.
        new("TopLevelWindows.OfProcess", Cooperation.None, false, "every top-level window a process owns"),
        new("TopLevelWindows.Largest", Cooperation.None, false, "the largest of them, which is the frame where there is one"),
        new("Foreground.Now", Cooperation.None, false, "who holds the keyboard, read straight from Windows"),
        new("Foreground.Check", Cooperation.None, false, "whether a named window holds it"),
        new("Foreground.Between", Cooperation.None, false, "the same judgement over two sightings a caller already has"),
        new("ForeignInput.Watch", Cooperation.None, false, "start a window in which this run owns the machine"),
        new("ForeignInput.Read", Cooperation.None, false, "whether anybody else used it in that window"),
        new("Desk.Read", Cooperation.None, false, "whether there is an interactive desk to drive at all"),
        new("Desk.Blocked", Cooperation.None, false, "the reading as the one line a refusal prints"),
        new("Desk.Caught", Cooperation.None, false, "whether a throw is the desk refusing rather than the code failing"),
        new("Desk.WorthAnotherLook", Cooperation.None, false, "whether a refusal is one a second look could answer"),
        new("AppTarget.AttachTo", Cooperation.None, false, "a target from a pid this run did not start"),
        new("AppTarget.AttachToWindow", Cooperation.None, false, "the same from a window handle"),
        new("AppTarget.FromLaunch", Cooperation.None, false, "a target from a process this run launched, arguments kept"),
        new("ProcessRegister.For", Cooperation.None, false, "a register with the stopping budget a project declares"),
        new("Obstruction.Reading", Cooperation.None, false, "what stands over a region, read off the z order"),
        new("PaintedFrame.Of", Cooperation.None, false, "what a window actually paints inside the rectangle it owns"),
        // Filed as needing the in-app half on the first attempt, and the case above put it right in
        // one run: the engine assembly carries no reference to that half, so no verb here can be
        // waiting on one. What this reads is the tree, against a label resolved from the project's
        // own strings — which is a declaration and not an artefact the application wrote down.
        new("Loading.In", Cooperation.None, false,
            "whether a page has finished computing, read off the tree against the project's own "
                + "loading label — and a page it could not walk answers that it did not look"),

        // WW256. The same walk asked of one string a case named rather than of the ones the project
        // declared as its loading text, and on the same axis for the same reason: it reads the tree
        // against a declaration, and needs nothing of the application beyond being drawn.
        new("Loading.Sighted", Cooperation.None, false,
            "whether one declared string is showing right now, and whether the look reached the "
                + "whole window — which is what makes an absence an absence"),

        // --- WW57, the one verb a whole case is run by -----------------------------------------------
        // It needs nothing the acts it dispatches do not need, which is the answer that matters: a
        // case declared as data drives a product nobody here owns, exactly as far as the pattern
        // acts underneath it do.
        new("CaseRun.Of", Cooperation.None, false,
            "one case, run: the loop, the waits, the attempts and the verdict, none of which the "
                + "case itself carries"),

        // --- WW59, WW60 and WW62 ---------------------------------------------------------------------
        new("Suite.Run", Cooperation.None, false,
            "the cases a selection asked for, run against a window the caller already has, with what "
                + "it left alone named rather than counted"),
        new("Suite.Launch", Cooperation.None, false,
            "the same, launching the application under test per fixture — and lending one window to "
                + "the cases that only read it where the invocation asked for that"),
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
            .Where(one => Namespaces.Contains(one.Namespace)
                || Driving().Contains(one.Name, StringComparer.Ordinal))
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

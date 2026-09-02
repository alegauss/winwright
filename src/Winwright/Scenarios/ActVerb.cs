using System.Collections.ObjectModel;
using System.Globalization;

using Winwright.Acting;
using Winwright.Locating;

namespace Winwright.Scenarios;

/// <summary>What a verb needs said alongside it, which is what decides whether a field is missing.</summary>
public enum Takes
{
    /// <summary>Nothing. An argument written next to one of these is a field the verb cannot use.</summary>
    Nothing,

    /// <summary>Text, put through the control's own value rather than through the keyboard.</summary>
    Text,

    /// <summary>A number the control says it accepts.</summary>
    Number,

    /// <summary>
    /// A position in something, counted from zero.
    /// <para>
    /// WW267. Its own kind rather than <see cref="Number"/>, because the two are refused for
    /// different things: a range takes a fraction and a negative quite happily, and a position that
    /// is either is a step nobody can perform. Refused where it is written, like every other field.
    /// </para>
    /// </summary>
    Position,
}

/// <summary>
/// The acts a scenario may name, as data.
/// <para>
/// This is the closed half of <see cref="Act"/>. A script picks the method, so the vocabulary is
/// whatever the author remembered and the argument arity is whatever the compiler happened to
/// enforce; a case is a data file, so the vocabulary has to be enumerable and the arity has to be
/// a field. What the author writes is a name, and a name that is not one of these is refused with
/// the list in the refusal — which is the difference between a typo caught at load and a step that
/// resolves nothing at run time.
/// </para>
/// <para>
/// <see cref="Repeatable"/> is the field that exists because the engine owns the retries. Three
/// attempts is right for an act whose consequence a shell sometimes drops, and wrong for
/// <c>toggle</c>: flipping twice arrives back where it started, so a retried toggle turns a red
/// into a red about the opposite state. An author writing a script has to remember which acts
/// survive being repeated. An author writing a case does not, because the verb says.
/// </para>
/// </summary>
public sealed record ActVerb
{
    private static readonly ActVerb[] Vocabulary =
    [
        new("read", Takes.Nothing, repeatable: true, null),
        new("invoke", Takes.Nothing, repeatable: false, (subject, _) => Act.Invoke(subject)),
        new("toggle", Takes.Nothing, repeatable: false, (subject, _) => Act.Toggle(subject)),
        new("set value", Takes.Text, repeatable: true, (subject, argument) => Act.SetValue(subject, argument!)),
        new("set range", Takes.Number, repeatable: true, (subject, argument) => Act.SetRange(subject, Number(argument!))),
        new("select", Takes.Nothing, repeatable: true, (subject, _) => Act.Select(subject)),
        new("expand", Takes.Nothing, repeatable: true, (subject, _) => Act.Expand(subject)),
        new("collapse", Takes.Nothing, repeatable: true, (subject, _) => Act.Collapse(subject)),

        // WW225. The three that synthesise input, which is the half of the engine a case could not
        // name. Each has a pattern act beside it that reads almost the same and proves something
        // else: 'set value' writes through ValuePattern and 'type' presses keys, and the difference
        // is the whole of what an interaction loop is for.
        //
        // 'nudge' arrived one task later than the other two. It was left out until WW226 drew a range
        // control for it, because a verb with nothing driving it is the shape this project refuses
        // everywhere else — and its own branch that flips direction at the end of a range needs a
        // control already sitting there to provoke it.
        new(
            "type",
            Takes.Text,
            repeatable: false,
            (subject, argument) => Synthesised.Type(subject, argument!),
            synthesises: true),
        new(
            "click",
            Takes.Text,
            repeatable: false,
            (subject, argument) => Synthesised.Click(subject, Because(argument!)),
            synthesises: true,
            accepts: Enum.GetValues<PointerReason>().Select(one => one.ToString()).ToList()),
        new("nudge", Takes.Nothing, repeatable: false, (subject, _) => Synthesised.Nudge(subject), synthesises: true),
        // WW317. Two kinds of keystroke through one verb, because a case writing it means one thing
        // either way: send this key at the window. What differs is the claim underneath — a
        // traversal key moves the focus and is read for that, and a chord invokes a command whose
        // consequence the next step is the check for.
        //
        // Not a second verb, which is where WW267 drew the line for `pick at`: that one exists
        // because a picker may hold a value spelled '1', so one argument would mean two different
        // things depending on what the application happened to contain. Nothing about the
        // application decides this — 'Tab' is a traversal key on every machine and 'Ctrl+Shift+I' is
        // a chord on every machine, and the two vocabularies cannot collide because a chord's key is
        // always its last part and no traversal name holds a '+'.
        new(
            "press",
            Takes.Text,
            repeatable: false,
            (subject, argument) => Chorded(argument!) is { } chord
                ? Synthesised.Press(subject, chord)
                : Synthesised.Press(subject, Traversing(argument!)),
            synthesises: true,
            accepts: Enum.GetValues<TraversalKey>().Select(one => one.ToString()).ToList(),
            alsoTakes: Chord.Spelled()),

        // WW254. The picker walk, which the engine has done since WW28 and no case could name — so
        // the one case in claude-tray that drives a picker had no first step to write. 'select' is
        // the verb that looks closest and is not it: it asks a single item through
        // SelectionItemPattern, and a WPF ComboBox realises its items when its popup opens, so there
        // is nothing there to select until something has walked the picker.
        //
        // Filed under the acts a busy desk can take away even though it tries not to be one: the
        // pattern route is attempted first and needs nothing, and the keyboard fallback exists
        // precisely because that route sometimes refuses. Listing it beside the acts nothing about
        // the desk stops would be a promise about the half of the runs that take the keys.
        new(
            "pick",
            Takes.Text,
            repeatable: false,
            (subject, argument) => Synthesised.Pick(subject, argument!),
            synthesises: true,
            reaches: true),

        // WW267. The same walk, told where to go rather than what to reach. It is a second verb and
        // not a second meaning for 'with' because a picker may hold a value spelled '1', and a step
        // whose argument means two different things depending on what the application happens to
        // contain is one nobody can read.
        new(
            "pick at",
            Takes.Position,
            repeatable: false,
            (subject, argument) => Synthesised.PickAt(subject, Position(argument!)),
            synthesises: true,
            reaches: true),

        // WW259. The fifth pair, and the one whose pattern half cannot ask the question: an empty
        // WinForms submenu exposes no ExpandCollapse, so a case naming 'expand' against the menu this
        // was filed for asks a pattern that is not there and reports a control rather than the gesture.
        // Right is how a keyboard user opens it, and nothing a step could write reached the walk.
        //
        // Not repeatable, and that is the rule 'toggle' is not repeatable under rather than a separate
        // judgement: Right again walks deeper into the submenu instead of arriving where it already is,
        // so a retry is a different gesture and its red would be about the wrong menu.
        new(
            Synthesised.ExpandsMenu,
            Takes.Nothing,
            repeatable: false,
            (subject, _) => Synthesised.ExpandMenu(subject),
            synthesises: true),

        // WW258. The one act a tray icon takes, and it carries no delegate because there is no subject
        // to hand one: an icon is a rectangle and a tooltip in the shell's tree, so the engine reaches
        // `NotificationArea.OpenMenu` by name where a step's subject is a tray. Synthesising, because
        // the route is focus and the application key — a synthesised right-click opens nothing at all
        // on this shell, which is why there is no pointer half of this pair to name.
        new(
            "open tray menu",
            Takes.Nothing,
            repeatable: false,
            null,
            synthesises: true,
            onATray: true),

        // WW336. The one act that produces a file, and the reason it took a task of its own is the
        // file rather than the act: every other field a case carries is derived so the case means
        // the same thing on the next machine, and a path typed into one is the plainest way to break
        // that. So the argument is what to CALL the picture and never where to put it — the project
        // declares that, the case's own name is the folder, and the two together are a path no case
        // had to know.
        //
        // No delegate, like the tray act above and for its reason: there is no control to hand one.
        // A capture is about the window the locator resolves inside, and the engine reaches
        // `CaptureReceipt.Taking` by name where a step's verb is this one — which is also what keeps
        // the six readings a capture owes from being a caller's to remember.
        new(
            "capture",
            Takes.Text,
            repeatable: false,
            null,
            captures: true,
            needs: "captures"),
    ];

    private readonly Func<Subject, string?, ActResult>? doing;
    private readonly bool onATray;

    private ActVerb(
        string name,
        Takes takes,
        bool repeatable,
        Func<Subject, string?, ActResult>? doing,
        bool synthesises = false,
        IReadOnlyList<string>? accepts = null,
        bool reaches = false,
        bool onATray = false,
        string alsoTakes = "",
        bool captures = false,
        string needs = "")
    {
        AlsoTakes = alsoTakes;
        Name = name;
        Wants = takes;
        Repeatable = repeatable;
        Synthesises = synthesises;
        Accepts = accepts ?? [];
        Reaches = reaches;
        Captures = captures;
        Needs = needs;
        this.doing = doing;
        this.onATray = onATray;
    }

    /// <summary>
    /// What the project must declare before a step naming this verb can run, and empty where the
    /// verb asks the project for nothing. WW348.
    /// <para>
    /// The key rather than the verb, which is what keeps <see cref="Suite" /> out of the vocabulary.
    /// A capture with nowhere to put pictures used to be answered as a hole on the run that reached
    /// the step, having launched the application to learn what the file and the declaration beside it
    /// already said between them. Refusing it earlier meant somebody had to know that <c>capture</c>
    /// is the verb that needs <c>captures</c> — and the one place a fact about a verb is allowed to
    /// live is here, so the suite asks whether a step needs anything and never which verb it is.
    /// </para>
    /// <para>
    /// It is the same shape as <see cref="Accepts" /> one level out. That one made a closed list of
    /// arguments the vocabulary's business rather than the act's; this makes a required declaration
    /// the vocabulary's business rather than the runner's, and a second verb needing a second key
    /// gets the refusal without anybody writing it.
    /// </para>
    /// </summary>
    public string Needs { get; } = "";

    /// <summary>
    /// Whether this act writes a picture of the window its subject is in. WW336.
    /// <para>
    /// Data rather than a name compared at the call, for the reason <see cref="OnATray"/> is: the
    /// vocabulary is the one place an act is declared, and a second answer about the same word is
    /// how the two come to disagree.
    /// </para>
    /// </summary>
    public bool Captures { get; }

    /// <summary>
    /// Every verb there is, in the order a reader is shown them. This is what a refusal lists and
    /// what a tool describing the format reads its enumeration off.
    /// </summary>
    public static IReadOnlyList<ActVerb> All { get; } = new ReadOnlyCollection<ActVerb>(Vocabulary);

    /// <summary>The name a case writes, and the same word <see cref="ActResult.Verb"/> reports.</summary>
    public string Name { get; }

    /// <summary>What has to be written next to it.</summary>
    public Takes Wants { get; }

    /// <summary>
    /// Whether it synthesises input rather than asking the control.
    /// <para>
    /// WW225. A pattern act asks a control through its own accessibility peer and nothing about the
    /// desk stops it. One of these goes through the keyboard or the pointer, so it needs the window
    /// in the foreground — which means it can come back not attempted, and the reader has to be told
    /// that rather than shown a reading that did not move. Data, so a report can say which acts in a
    /// case were the ones a busy desk could take away.
    /// </para>
    /// </summary>
    public bool Synthesises { get; }

    /// <summary>
    /// Everything the argument may be, where it is a closed list. Empty where it takes free text.
    /// <para>
    /// WW225. <c>click</c> carries the reason no pattern would express it, and the reasons are a
    /// closed set — so a name outside it has to be refused where the author wrote it, not on the run
    /// that reaches the step. Measured: the first version validated the reason inside the act, and a
    /// case with a reason nobody recognises loaded, passed <c>winwright_check</c>, and refused
    /// halfway through a run — which is exactly the linter-shaped failure WW58 exists to replace.
    /// </para>
    /// </summary>
    public IReadOnlyList<string> Accepts { get; }

    /// <summary>
    /// What this verb takes besides <see cref="Accepts"/>, in the words a refusal lists it with, and
    /// empty where the closed list is the whole vocabulary.
    /// <para>
    /// WW317. <c>press</c> takes a traversal name or a chord, and a chord is not a closed list — so
    /// a refusal that printed only the names would tell an author their chord is no key, having just
    /// been given one. Data, like <see cref="Accepts"/>, so the sentence a refusal builds and the
    /// vocabulary the act runs cannot drift.
    /// </para>
    /// </summary>
    public string AlsoTakes { get; } = "";

    /// <summary>
    /// Whether doing it twice means the same as doing it once. False for the acts whose second
    /// go undoes or repeats the first, and the engine attempts one of those exactly once.
    /// </summary>
    public bool Repeatable { get; }

    /// <summary>
    /// Whether the act is told what to reach and the engine can read whether it got there.
    /// <para>
    /// WW254. Every other act asks a control to do something, and what it ended up on is whatever the
    /// control then reads. A pick is handed a value by name and can be asked what the picker settled
    /// on — so a step naming one and claiming nothing has thrown away the one answer the engine had.
    /// A click with no expectation is a navigation the next step is the check for; a pick with none
    /// is every step after it read against whichever value the walk happened to stop at.
    /// </para>
    /// </summary>
    public bool Reaches { get; }

    /// <summary>
    /// Whether this verb reads and never acts.
    /// <para>
    /// WW213. The vocabulary was seven acts, so a case checking a label after a save had to name an
    /// act to get there — and selecting a Text element to read it says the case moved something and
    /// turns a check into a harness error on a control that does not offer the pattern. Reading is a
    /// step, so it is in the vocabulary rather than borrowed from an act that means nothing.
    /// </para>
    /// <para>
    /// The engine takes the look itself for one of these rather than going through
    /// <see cref="Act"/>: an act must have found something to press and a read need not, so the
    /// element that was not there comes out as an expectation nothing answered rather than a throw.
    /// That also means a read never passes the destructive guard, which is right — reading the name
    /// of the entry that ends the run does not press it.
    /// </para>
    /// </summary>
    /// <remarks>
    /// WW336 added the third exception. A capture carries no delegate either — there is no control
    /// to hand one — and it is emphatically not a read: it takes the foreground's word for what the
    /// screen holds and writes a file, so every rule that lets a read through because a read touches
    /// nothing would be letting this through on a claim that is false of it.
    /// </remarks>
    public bool Reads => doing is null && !onATray && !Captures;

    /// <summary>
    /// Whether a step whose subject is a tray icon may name it.
    /// <para>
    /// WW258. A tray icon is not an element, so none of the verbs that ask a control through its
    /// patterns applies to one and <see cref="Perform"/> could not be handed a subject for it. What is
    /// left is the two a shell exposes: reading whether the icon is showing, and asking for its menu.
    /// A verb outside this set is refused where the author wrote it rather than on the run that would
    /// have had nothing to act on.
    /// </para>
    /// </summary>
    public bool OnATray => onATray || Reads;

    /// <summary>
    /// The verb of that name, or a refusal listing the ones there are.
    /// </summary>
    /// <exception cref="ScenarioRefusedException">Where nothing is named, or nothing matches.</exception>
    public static ActVerb Named(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ScenarioRefusedException("<unnamed act>", $"a step acts, and this one names no verb; there is {Spelled()}");

        var wanted = name.Trim();
        foreach (var verb in Vocabulary)
            if (string.Equals(verb.Name, wanted, StringComparison.OrdinalIgnoreCase))
                return verb;

        throw new ScenarioRefusedException(wanted, $"there is no such act; there is {Spelled()}");
    }

    /// <summary>Whether the argument this step carries is the one this verb needs.</summary>
    /// <returns>Null where it is, and the sentence saying what is wrong where it is not.</returns>
    public string? Refuses(string? argument)
    {
        var written = string.IsNullOrWhiteSpace(argument) ? null : argument.Trim();
        return (Wants, written) switch
        {
            (Takes.Nothing, not null) => $"'{Name}' takes nothing, and this one carries '{written}'",
            (Takes.Nothing, null) => null,
            (_, null) => $"'{Name}' acts on {Wanting()}, and this one carries none",

            // WW225. The closed list is checked here and not inside the act, because here is where
            // the author is: a word nobody recognises has to cost a corrected field and never a run
            // that gets halfway and stops. Worded off the verb rather than per verb, so a third one
            // with a closed list gets the sentence without anybody writing it.
            // WW317. The closed list first, then whatever else the verb takes — and where it takes
            // something else, the refusal has to say why the argument is neither. A chord that does
            // not parse carries its own sentence, which is more use than the list of traversal names
            // it is not one of.
            (Takes.Text, _) when Accepts.Count > 0 && !Accepts.Contains(written, StringComparer.OrdinalIgnoreCase) =>
                AlsoTakes.Length == 0
                    ? $"'{Name}' does not take '{written}'; it takes {string.Join(", ", Accepts)}"
                    : Chord.TryParse(written, out _, out var wrong)
                        ? null
                        : $"'{Name}' does not take '{written}': {wrong}. It takes "
                            + $"{string.Join(", ", Accepts)}, or {AlsoTakes}",

            (Takes.Text, _) => null,
            (Takes.Number, _) => Numeric(written) ? null : $"'{Name}' acts on a number, and '{written}' is not one",

            // WW267. Refused here for the reason every other field is: a position that is a fraction
            // or below zero is wrong on every machine, and discovering it on the run that was going
            // to walk with it costs a launch to learn what the file already said.
            (Takes.Position, _) => Whole(written)
                ? null
                : $"'{Name}' acts on a position, and '{written}' is not a whole number from 0",

            _ => null,
        };
    }

    /// <summary>Do it. The subject is resolved, judged and read either side by <see cref="Act"/>.</summary>
    /// <exception cref="InvalidOperationException">Where this verb reads and there is no act to run.</exception>
    internal ActResult Perform(Subject subject, string? argument) =>
        doing is null
            ? throw new InvalidOperationException(
                $"'{Name}' reads and never acts, so the engine takes the look rather than calling this")
            : doing(subject, argument);

    /// <summary>
    /// The reason a click carries, out of the name a case wrote.
    /// <para>
    /// Refused with the list rather than defaulted, for the reason <see cref="Pointer"/> makes the
    /// reason a required field: a click whose justification defaults is a click nobody had to
    /// justify, and then every act quietly escalates to the pointer and the suite is driving the
    /// desktop instead of asking controls.
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Where the name is not one of the reasons there are. A harness error and never a scenario
    /// refusal: <see cref="Refuses"/> already turned that away at the point of insertion, so reaching
    /// this with a name outside the list means the two lists disagree and nothing about the author's
    /// file is wrong.
    /// </exception>
    private static PointerReason Because(string argument)
    {
        var wanted = argument.Trim();
        foreach (var reason in Enum.GetValues<PointerReason>())
            if (string.Equals(reason.ToString(), wanted, StringComparison.OrdinalIgnoreCase))
                return reason;

        throw new InvalidOperationException(
            $"'{wanted}' reached the act and is not a reason, so the load accepted what the act cannot run");
    }

    /// <summary>
    /// The traversal key a step named. Refused at the point of insertion by <see cref="Accepts"/>, so
    /// reaching here with anything else means the two lists disagree.
    /// </summary>
    /// <exception cref="InvalidOperationException">Where the name is not a traversal key.</exception>
    private static TraversalKey Traversing(string argument)
    {
        var wanted = argument.Trim();
        foreach (var key in Enum.GetValues<TraversalKey>())
            if (string.Equals(key.ToString(), wanted, StringComparison.OrdinalIgnoreCase))
                return key;

        throw new InvalidOperationException(
            $"'{wanted}' reached the act and is not a traversal key, so the load accepted what the act cannot run");
    }

    /// <summary>What this verb wants said, as the refusal words it.</summary>
    private string Wanting() => Wants switch
    {
        Takes.Text => "text",
        Takes.Number => "a number",
        Takes.Position => "a position",
        _ => "nothing",
    };

    /// <summary>Whether the argument is a position: a whole number, counted from zero.</summary>
    /// <param name="argument">What the step wrote.</param>
    private static bool Whole(string argument) =>
        int.TryParse(argument, NumberStyles.None, CultureInfo.InvariantCulture, out var at) && at >= 0;

    /// <summary>
    /// The position a step named. Refused at the point of insertion by <see cref="Refuses"/>, so
    /// reaching here with anything else means the two disagree.
    /// </summary>
    /// <param name="argument">What the step wrote.</param>
    /// <exception cref="InvalidOperationException">Where it is not a position after all.</exception>
    private static int Position(string argument) =>
        int.TryParse(argument.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var at) && at >= 0
            ? at
            : throw new InvalidOperationException(
                $"'{argument}' reached the act and is not a position, so the load accepted what the act cannot run");

    private static bool Numeric(string argument) =>
        double.TryParse(argument, NumberStyles.Float, CultureInfo.InvariantCulture, out _);

    private static double Number(string argument) =>
        double.Parse(argument, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static string Spelled() => string.Join(", ", Vocabulary.Select(verb => verb.Name));

    /// <summary>
    /// The chord an argument spells, or null where it spells a traversal key instead.
    /// <para>
    /// WW317. Asked of the argument rather than decided by a flag, because the two vocabularies do
    /// not overlap: <see cref="Refuses"/> has already turned away everything that is neither, so what
    /// reaches here is one or the other and this says which.
    /// </para>
    /// </summary>
    private static Chord? Chorded(string argument) =>
        Enum.TryParse<TraversalKey>(argument.Trim(), ignoreCase: true, out _)
            ? null
            : Chord.TryParse(argument, out var chord, out _) ? chord : null;
}

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
        new(
            "press",
            Takes.Text,
            repeatable: false,
            (subject, argument) => Synthesised.Press(subject, Traversing(argument!)),
            synthesises: true,
            accepts: Enum.GetValues<TraversalKey>().Select(one => one.ToString()).ToList()),

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
    ];

    private readonly Func<Subject, string?, ActResult>? doing;

    private ActVerb(
        string name,
        Takes takes,
        bool repeatable,
        Func<Subject, string?, ActResult>? doing,
        bool synthesises = false,
        IReadOnlyList<string>? accepts = null,
        bool reaches = false)
    {
        Name = name;
        Wants = takes;
        Repeatable = repeatable;
        Synthesises = synthesises;
        Accepts = accepts ?? [];
        Reaches = reaches;
        this.doing = doing;
    }

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
    public bool Reads => doing is null;

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
            (Takes.Text, _) when Accepts.Count > 0 && !Accepts.Contains(written, StringComparer.OrdinalIgnoreCase) =>
                $"'{Name}' does not take '{written}'; it takes {string.Join(", ", Accepts)}",

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
}

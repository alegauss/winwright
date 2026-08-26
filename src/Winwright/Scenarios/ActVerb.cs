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
    ];

    private readonly Func<Subject, string?, ActResult>? doing;

    private ActVerb(string name, Takes takes, bool repeatable, Func<Subject, string?, ActResult>? doing)
    {
        Name = name;
        Wants = takes;
        Repeatable = repeatable;
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
    /// Whether doing it twice means the same as doing it once. False for the acts whose second
    /// go undoes or repeats the first, and the engine attempts one of those exactly once.
    /// </summary>
    public bool Repeatable { get; }

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
            (_, null) => $"'{Name}' acts on {(Wants == Takes.Text ? "text" : "a number")}, and this one carries none",
            (Takes.Text, _) => null,
            (Takes.Number, _) => Numeric(written) ? null : $"'{Name}' acts on a number, and '{written}' is not one",
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

    private static bool Numeric(string argument) =>
        double.TryParse(argument, NumberStyles.Float, CultureInfo.InvariantCulture, out _);

    private static double Number(string argument) =>
        double.Parse(argument, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static string Spelled() => string.Join(", ", Vocabulary.Select(verb => verb.Name));
}

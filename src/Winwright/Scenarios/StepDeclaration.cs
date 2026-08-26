using Winwright.Locating;

namespace Winwright.Scenarios;

/// <summary>
/// One step of a case, as fields: what to act on, what to do to it, what to say alongside, and what
/// the control should read afterwards.
/// <para>
/// What is deliberately not here is the loop. A step does not know how long to wait, how many
/// attempts it gets, whether the window is in the foreground, or what a failed read-back does to
/// the verdict — <see cref="CaseRun"/> owns all of that. claude-tray's harness is 2,732 lines for
/// eight cases because every case answers those four questions again, and every case answers them
/// slightly differently. Here the only thing a case can vary is the data.
/// </para>
/// <para>
/// Every field is judged when the step is declared, so a case that could not run anywhere is
/// refused before it runs here. An unparseable locator, a verb that does not exist, an argument
/// beside a verb that takes none, a reading named for an expectation that was never written: all of
/// those are properties of the file, and reporting one of them as a red on somebody's desk sends
/// the reader looking for a defect in the application.
/// </para>
/// </summary>
public sealed record StepDeclaration
{
    private StepDeclaration(
        string name, Locator locator, ActVerb verb, string? argument, string? expected, ReadBack reads, bool meansIt)
    {
        Name = name;
        Locator = locator;
        Verb = verb;
        Argument = argument;
        Expected = expected;
        Reads = reads;
        MeansIt = meansIt;
    }

    /// <summary>What a report calls this step. The verb and the locator where the case named none.</summary>
    public string Name { get; }

    /// <summary>What it acts on, parsed at declaration and never re-parsed at run time.</summary>
    public Locator Locator { get; }

    /// <summary>What it does.</summary>
    public ActVerb Verb { get; }

    /// <summary>What the verb was given, or null where it takes nothing.</summary>
    public string? Argument { get; }

    /// <summary>
    /// What <see cref="Reads"/> should say once the act has landed, or null where this step is an
    /// act and nothing else — a navigation whose consequence a later step is the check for.
    /// </summary>
    public string? Expected { get; }

    /// <summary>Which reading the expectation is about. <see cref="ReadBack.Anything"/> by default.</summary>
    public ReadBack Reads { get; }

    /// <summary>
    /// Whether this step has said out loud that it means a destructive entry, which is the sentence
    /// <see cref="Subject.MeaningIt"/> is looking for. False by default, and then a step whose
    /// locator matches something the project declared destructive is refused when it runs.
    /// </summary>
    public bool MeansIt { get; }

    /// <summary>
    /// Whether this step says anything a run could find false. A step that expects nothing produces
    /// no assertion result, which is why a case made only of these is refused by
    /// <see cref="CaseDeclaration"/> rather than run to a green it did not earn.
    /// </summary>
    public bool Checkable => Expected is not null;

    /// <summary>
    /// Whether the engine may attempt this step again where its read-back did not arrive. Both
    /// halves are needed: there is nothing to retry towards without an expectation, and a verb that
    /// does not survive being repeated gets one attempt whatever the expectation said.
    /// </summary>
    public bool Retryable => Checkable && Verb.Repeatable;

    /// <summary>
    /// Declare one, refusing every field that is wrong about the file rather than about the desk.
    /// </summary>
    /// <param name="locator">What to act on, in the locator grammar.</param>
    /// <param name="verb">Which act, by the name <see cref="ActVerb.All"/> lists.</param>
    /// <param name="argument">What the verb needs said, where it needs anything.</param>
    /// <param name="expected">What the reading should be afterwards.</param>
    /// <param name="reads">Which reading, by the name <see cref="ReadBack.All"/> lists.</param>
    /// <param name="meansIt">That this step means a destructive entry it names.</param>
    /// <param name="named">What a report should call it, where the verb and locator will not do.</param>
    /// <exception cref="ScenarioRefusedException">Where any field could not run on any machine.</exception>
    public static StepDeclaration Of(
        string locator,
        string verb,
        string? argument = null,
        string? expected = null,
        string? reads = null,
        bool meansIt = false,
        string? named = null)
    {
        var called = string.IsNullOrWhiteSpace(named) ? null : named.Trim();
        var subject = called ?? Describing(verb, locator);

        if (string.IsNullOrWhiteSpace(locator))
            throw new ScenarioRefusedException(subject, "a step acts on something, and this one names nothing");

        // Parsed here rather than at run time on purpose: a locator that does not parse is wrong on
        // every machine, and the reader of a red about one is opening the wrong repository.
        if (!Locator.TryParse(locator, out var parsed, out var because))
            throw new ScenarioRefusedException(subject, $"its locator does not parse — {because}");

        var act = ActVerb.Named(verb);
        if (act.Refuses(argument) is { } wrong)
            throw new ScenarioRefusedException(subject, wrong);

        var wanted = expected;
        var reading = ReadBack.Named(reads);
        if (wanted is null && !string.IsNullOrWhiteSpace(reads))
        {
            throw new ScenarioRefusedException(
                subject, $"it reads '{reading.Name}' and expects nothing of it, so the reading changes nothing");
        }

        return new StepDeclaration(
            called ?? Describing(act.Name, parsed!.Text),
            parsed!,
            act,
            string.IsNullOrWhiteSpace(argument) ? null : argument.Trim(),
            wanted,
            reading,
            meansIt);
    }

    /// <summary>The one line a trace and a refusal both name it by.</summary>
    public override string ToString() => Expected is null
        ? Name
        : $"{Name} → {Reads.Name} '{Expected}'";

    private static string Describing(string? verb, string locator) =>
        $"{(string.IsNullOrWhiteSpace(verb) ? "<no verb>" : verb.Trim())} {locator.Trim()}";
}

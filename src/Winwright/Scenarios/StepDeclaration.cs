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
        string name,
        Locator locator,
        ActVerb verb,
        string? argument,
        string? expected,
        ReadBack reads,
        bool meansIt,
        bool moves,
        string? covers,
        bool answers,
        System.Text.RegularExpressions.Regex? matches,
        bool discloses,
        string? sameAs)
    {
        Name = name;
        Locator = locator;
        Verb = verb;
        Argument = argument;
        Expected = expected;
        Reads = reads;
        MeansIt = meansIt;
        Moves = moves;
        Covers = covers;
        Answers = answers;
        Matches = matches;
        Discloses = discloses;
        SameAs = sameAs;
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

    /// <summary>
    /// The pattern <see cref="Reads"/> should match once the act has landed, or null where the step
    /// makes one of the other claims.
    /// <para>
    /// WW250. Between naming the value and saying only that there is one, and a real claim sits there.
    /// Measured on claude-tray's list-price note: it interpolates the date its rate card was read, so
    /// no case can name what it says — and a note that lost that date is the defect, while still
    /// answering. `expect` could not be written and `answers` would have read as covered.
    /// </para>
    /// <para>
    /// Compiled at declaration, like the locator, and for the same reason: a pattern that does not
    /// parse is wrong on every machine. It carries a timeout, because a regular expression is the one
    /// field of this format that can be made to cost a run rather than fail it.
    /// </para>
    /// </summary>
    public System.Text.RegularExpressions.Regex? Matches { get; }

    /// <summary>
    /// Whether this step claims the act put something under the locator that was not in the tree
    /// before it.
    /// <para>
    /// WW251. A disclosure is not one reading moving. Measured migrating claude-tray's sessions case:
    /// clicking a conversation row unfolds the call tree that produced it, and what says so is more
    /// elements under the row than there were. <see cref="Moves"/> is one reading of one element and
    /// <see cref="Covers"/> is a derived set — neither says <em>there is more here than there was</em>.
    /// </para>
    /// <para>
    /// Never a count the case types. `at least four fields` is the same stale literal as a hand-written
    /// set: the row grows a field and the case goes on asserting four. The subtree is compared against
    /// itself a moment earlier, which is what <see cref="Moves"/> does for a single value.
    /// </para>
    /// </summary>
    public bool Discloses { get; }

    /// <summary>
    /// The earlier step this one claims its reading is back to, or null where it makes another claim.
    /// <para>
    /// WW255. <see cref="Moves"/> compares a reading against the same reading a moment earlier, in the
    /// same step, across the same act. That is one shape of <em>changed</em>, and a round trip is the
    /// other: a value that changed and then came back. Measured migrating claude-tray's profiles case,
    /// which walks a picker 0 → 1 → 0 and asserts that the third stop reads what the first one did —
    /// a claim about a step several steps back and about no act at all.
    /// </para>
    /// <para>
    /// Never a value typed here, for the reason <see cref="Moves"/> takes none: the case cannot know
    /// what the number is, only that it is the one from before. The defect it was written for
    /// repainted the panes with the profile being left behind, so coming back showed another account's
    /// figures while every reading, taken on its own, looked perfectly healthy.
    /// </para>
    /// </summary>
    public string? SameAs { get; }

    /// <summary>Which reading the expectation is about. <see cref="ReadBack.Anything"/> by default.</summary>
    public ReadBack Reads { get; }

    /// <summary>
    /// Whether this step has said out loud that it means a destructive entry, which is the sentence
    /// <see cref="Subject.MeaningIt"/> is looking for. False by default, and then a step whose
    /// locator matches something the project declared destructive is refused when it runs.
    /// </summary>
    public bool MeansIt { get; }

    /// <summary>
    /// Whether this step claims the reading moved, rather than what it moved to.
    /// <para>
    /// WW229. Measured migrating claude-tray's keyboard case. Its fourth assertion was an arrow key
    /// driving a slider, which is a claim about movement: the script read the value, pressed the key,
    /// read it again and compared, because the starting value belongs to the application's own
    /// settings and no case can know it. <see cref="Expected"/> compares a reading to a string, so the
    /// migration had to put the control at a known floor first and expect the one value that could
    /// follow — which worked only because that application's bounds are constants in its own source.
    /// </para>
    /// <para>
    /// The workaround also costs something where it is available: two steps instead of one, a write
    /// through the pattern before the key press that is the point, and an expectation that goes stale
    /// the day the tick frequency changes. This is the claim the script was actually making.
    /// </para>
    /// </summary>
    public bool Moves { get; }

    /// <summary>
    /// The key whose every declared string must be read somewhere this step's locator matches, or
    /// null where this step is about one element.
    /// <para>
    /// WW236. The engine has derived a set from a project's own strings since block F, with provenance
    /// and a comparison naming what was read and never declared and the other way round — and nothing
    /// in a data file could name one. So block F's first criterion, that every set a scenario checks
    /// against is derived rather than typed, was unfalsifiable of scenarios: no case could try it.
    /// </para>
    /// <para>
    /// Measured on claude-tray's panes case. It derives its tab headers from the strings and says why
    /// in its own comment: it listed three by hand, the window grew a fourth, and it reported <em>all
    /// three tab headers read</em> against a four-tab window. A list stops covering what it was
    /// written for and says nothing when it does.
    /// </para>
    /// <para>
    /// One claim over many elements, which is why it is not an <see cref="Expected"/>. The key is all
    /// a case says: which strings file it comes out of is the project's business, and a case naming
    /// one would be a case that runs on one checkout.
    /// </para>
    /// </summary>
    public string? Covers { get; }

    /// <summary>
    /// Whether this step claims the reading it names says something rather than nothing.
    /// <para>
    /// WW237. claude-tray's panes case asserts a pane's body is attached by reading a number from
    /// inside it — a percentage the application computed, so the claim is that it reads at all, and
    /// the reset caption and the live headline beside it are the same shape. <see cref="Expected"/>
    /// compares a reading to a string, so making that claim meant writing down a value the case
    /// cannot know, which goes stale the day the application computes a different one.
    /// </para>
    /// <para>
    /// Never a default: a step that expects nothing is a navigation, and turning silence into a claim
    /// would have every act asserting something nobody wrote. And never true of an empty answer — a
    /// control saying nothing is what this exists to catch, which is the distinction
    /// <see cref="ReadBack.Anything"/> already draws by answering null.
    /// </para>
    /// </summary>
    public bool Answers { get; }

    /// <summary>
    /// Whether this step says anything a run could find false. A step that expects nothing, claims no
    /// movement and covers no set produces no assertion result, which is why a case made only of these
    /// is refused by <see cref="CaseDeclaration"/> rather than run to a green it did not earn.
    /// </summary>
    public bool Checkable =>
        Expected is not null || Moves || Answers || Covers is not null || Matches is not null || Discloses;

    /// <summary>
    /// Whether the engine may attempt this step again where its read-back did not arrive.
    /// <para>
    /// All three clauses earn their place. There is nothing to retry towards without an expectation;
    /// a verb that does not survive being repeated gets one attempt whatever the expectation said;
    /// and a read is never retried at all, because the wait already polled to the deadline and a
    /// second go is the same look taken again for the same answer at three times the cost.
    /// </para>
    /// </summary>
    public bool Retryable => Checkable && Verb.Repeatable && !Verb.Reads;

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
    /// <param name="moves">That the reading should end up different from what it was.</param>
    /// <param name="covers">The key whose every declared string this step's locator must read.</param>
    /// <param name="answers">That the reading should say something rather than nothing.</param>
    /// <param name="matches">The pattern the reading should match, where no case can name its value.</param>
    /// <param name="discloses">That the act put something under the locator that was not there before.</param>
    /// <param name="sameAs">The earlier step this one claims its reading is back to.</param>
    /// <exception cref="ScenarioRefusedException">Where any field could not run on any machine.</exception>
    public static StepDeclaration Of(
        string locator,
        string verb,
        string? argument = null,
        string? expected = null,
        string? reads = null,
        bool meansIt = false,
        string? named = null,
        bool moves = false,
        string? covers = null,
        bool answers = false,
        string? matches = null,
        bool discloses = false,
        string? sameAs = null)
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

        // WW236, and it is computed here rather than below because the two rules under this one would
        // otherwise fire first and say the wrong thing: a sweep expects nothing of one reading on
        // purpose, so "the reading changes nothing" and "the step does nothing at all" are both false
        // of it — and a refusal that names the wrong field is a refusal somebody fixes the wrong way.
        var sweeping = string.IsNullOrWhiteSpace(covers) ? null : covers.Trim();

        // WW250, computed here for the same reason and with the same history: the two rules under this
        // one do not know about it, so a step whose only claim is a pattern would be refused as a step
        // that claims nothing — a refusal naming the wrong field, which somebody then fixes wrongly.
        var pattern = string.IsNullOrWhiteSpace(matches) ? null : Compiled(subject, matches.Trim());

        // WW255, computed here with the two above it and for the same reason — and then made the one
        // local the rules below ask, because the clause they each carried had grown to six negations
        // and a claim any of them had not heard of is a refusal naming the wrong field.
        var back = string.IsNullOrWhiteSpace(sameAs) ? null : sameAs.Trim();
        var claims = wanted is not null || moves || answers || sweeping is not null || pattern is not null
            || discloses || back is not null;

        // WW229. Two claims and never both: 'expect' names what the reading becomes, which already
        // says it moved where it was something else. A step asserting both would owe two assertion
        // results, and a trace line that stands for two things is one a reader has to take apart.
        if (wanted is not null && moves)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it expects '{wanted}' and also that the reading moved; naming the value says both, "
                    + "so 'moves' is for the claim that cannot name one");
        }

        if (!claims && !string.IsNullOrWhiteSpace(reads))
        {
            throw new ScenarioRefusedException(
                subject, $"it reads '{reading.Name}' and expects nothing of it, so the reading changes nothing");
        }

        // WW213. An act with no expectation is a navigation a later step is the check for. A read
        // with no expectation is nothing at all: it touches nothing and claims nothing, so a case
        // carrying one is a case with a step in it that could not fail.
        if (!claims && act.Reads)
            throw new ScenarioRefusedException(subject, $"'{act.Name}' expects nothing, so the step does nothing at all");

        // WW254. The one act whose landing the engine can see. It was handed a value by name and can
        // read what the picker settled on, so a step that walks a picker and claims nothing has thrown
        // that answer away — and every step after it is then read against whichever value the walk
        // happened to stop at. That is WW244's failure with the act delivered rather than dropped, and
        // the migration this verb exists for made exactly this claim in the script: the picker walked
        // one label to another and back, checked at each stop.
        if (act.Reaches && !claims)
        {
            throw new ScenarioRefusedException(
                subject,
                $"'{act.Name}' is told what to reach and claims nothing of what it reached; name the "
                    + "value in 'expect', because a walk that stopped somewhere else is every step "
                    + "after this one reading the wrong thing");
        }

        // A read moves nothing by construction, so a read claiming movement is a claim about whatever
        // else is happening on the desk rather than about this step.
        if (moves && act.Reads)
        {
            throw new ScenarioRefusedException(
                subject, $"'{act.Name}' reads and never acts, so it cannot be what moved a reading");
        }

        // The reading has to be able to answer nothing, or the claim cannot be false. 'focused' says
        // 'not focused' for every element that resolved, so a step claiming it answers is a step that
        // holds whenever the locator matched — which is existence wearing the words of a reading.
        if (answers && reading.Always)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it claims '{reading.Name}' answers, and that reading answers for every element that "
                    + "resolved at all; the claim could never be false, so it says nothing");
        }

        if (pattern is not null)
        {
            // One claim per step, like the other three. 'expect' names the value and this names the
            // shape of it, and a step holding both owes two assertion results.
            if (wanted is not null || moves || answers || sweeping is not null)
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it matches '{pattern}' and also makes another claim; 'matches' is for the reading "
                        + "whose value a case cannot name");
            }

            // The unearned green this field is easiest to write. A pattern that matches the empty
            // string matches every answer there is, so the step holds wherever the reading answered at
            // all — which is what 'answers' says, in a field that reads as though it checked more.
            // The same shape WW237 and WW238 each closed once.
            if (pattern.IsMatch(""))
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"'{pattern}' matches the empty string, so it holds for every answer there is; "
                        + "say 'answers' if that is the claim");
            }

            // Deliberately no rule about an always-answering reading here, unlike 'answers'. A pattern
            // over 'focused' picks one of its two states, which is a claim that can be false — the
            // problem 'answers' has with that reading is that it asks only whether there was an answer,
            // and this asks which.
        }

        if (discloses)
        {
            // One claim per step, as everywhere else.
            if (wanted is not null || moves || answers || sweeping is not null || pattern is not null)
            {
                throw new ScenarioRefusedException(
                    subject,
                    "it claims a disclosure and also makes another claim; 'discloses' is about the tree "
                        + "under the locator rather than about a reading of it");
            }

            // A read discloses nothing. The claim is that an act put something there, so a step whose
            // verb only looks would be asserting that a window changed while nobody touched it — which
            // is either a race or a lie, and green either way.
            if (act.Reads)
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it claims a disclosure and '{act.Name}' only reads, so nothing it does could "
                        + "have disclosed anything");
            }

            // And no reading beside it, because the subject is the subtree. A 'reads' here would look
            // like it narrowed the claim and would narrow nothing.
            if (!string.IsNullOrWhiteSpace(reads))
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it claims a disclosure and names the '{reads.Trim()}' reading; a disclosure is "
                        + "about what is under the locator and not about what it says");
            }
        }

        if (back is not null)
        {
            // One claim per step, as everywhere else. 'expect' names the value and this is the claim
            // for a value the case cannot name — it only knows which earlier step read the same one.
            if (wanted is not null || moves || answers || sweeping is not null || pattern is not null || discloses)
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it claims its reading is back to '{back}' and also makes another claim; 'sameAs' "
                        + "is for the value a case cannot name and can only point at");
            }

            // A step comparing itself to itself holds by construction. It is the one spelling of this
            // field that could never be false, and it is also the easy typo: the round trip's third
            // stop and its first read the same element under the same verb, so a case that left both
            // unnamed would have written this by accident.
            if (string.Equals(back, subject, StringComparison.Ordinal))
            {
                throw new ScenarioRefusedException(
                    subject,
                    "it claims its reading is back to itself, which holds whatever the window did; "
                        + "name the earlier step in 'named' and point at that");
            }

            // Which reading, said out loud. The comparison is between two readings and the default is
            // whichever one the element happens to answer first, so a step that left it out would
            // compare a value to a name on the day the control gained a pattern.
            if (string.IsNullOrWhiteSpace(reads))
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it claims its reading is back to '{back}' and does not say which reading; 'sameAs' "
                        + "compares two of them, so the default would compare whichever answered first");
            }
        }

        // WW238. The other half of the same rule: a reading the locator already selected by is fixed
        // before the act runs, so a step reading it asserts what chose the element. Refused whatever
        // the claim is — 'expect' repeats the locator, 'answers' holds because the locator matched,
        // and both are the step passing on its own selection.
        //
        // Naming the element some other way and reading its name is the useful shape, so the sentence
        // says which locator field to move rather than that the reading is wrong.
        if (reading.PinnedBy(parsed!.Steps[^1]) is { } already && (wanted is not null || moves || answers))
        {
            throw new ScenarioRefusedException(
                subject,
                $"it reads '{reading.Name}' and its locator already matched on that — '{already}' — so the "
                    + "reading is fixed before the act runs; select the element another way to claim "
                    + "anything about it");
        }

        // WW237. One claim per step, for the reason the other two are: a trace line standing for two
        // things is one a reader has to take apart, and naming the value already says it answered.
        if (answers && (wanted is not null || moves))
        {
            throw new ScenarioRefusedException(
                subject,
                "it claims the reading answers something and also says what it is or that it moved; "
                    + "'answers' is for the claim that cannot name a value");
        }

        // One claim over many elements, and every other field on a step is about one.
        if (sweeping is not null)
        {
            if (!act.Reads)
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it covers '{sweeping}' and acts with '{act.Name}'; a sweep reads every element its "
                        + "locator matches, and one act over many of them is not a claim");
            }

            // 'moves' is not tested for here and that is not an omission: a sweep needs a verb that
            // reads, and a read claiming movement is already refused above. Naming it twice would be
            // a branch no file can reach.
            if (wanted is not null)
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it covers '{sweeping}' and also expects '{wanted}' of one reading; a set and a "
                        + "value are two claims, and a step answers one");
            }

            if (!string.IsNullOrWhiteSpace(reads))
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it covers '{sweeping}' and reads '{reading.Name}'; a sweep compares the names the "
                        + "locator matched against the strings, and a pattern reading is not one of them");
            }

            if (answers)
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it covers '{sweeping}' and claims one reading answers; a set is already the claim "
                        + "that every string under the key was read");
            }
        }

        return new StepDeclaration(
            called ?? Describing(act.Name, parsed.Text),
            parsed,
            act,
            string.IsNullOrWhiteSpace(argument) ? null : argument.Trim(),
            wanted,
            reading,
            meansIt,
            moves,
            sweeping,
            answers,
            pattern,
            discloses,
            back);
    }

    /// <summary>The one line a trace and a refusal both name it by.</summary>
    public override string ToString() => (Expected, Moves) switch
    {
        (not null, _) => $"{Name} → {Reads.Name} '{Expected}'",
        (_, true) => $"{Name} → {Reads.Name} moves",
        _ => Name,
    };

    /// <summary>
    /// The pattern, compiled, or a refusal naming what is wrong with it.
    /// <para>
    /// WW250. The timeout is the point of doing this here rather than inline: a regular expression is
    /// the one field of this format a file can use to cost a run instead of failing it, and a match
    /// that never returns is a case nobody can report on.
    /// </para>
    /// </summary>
    /// <exception cref="ScenarioRefusedException">Where it does not parse.</exception>
    private static System.Text.RegularExpressions.Regex Compiled(string subject, string pattern)
    {
        try
        {
            return new System.Text.RegularExpressions.Regex(
                pattern,
                System.Text.RegularExpressions.RegexOptions.CultureInvariant,
                TimeSpan.FromSeconds(1));
        }
        catch (ArgumentException wrong)
        {
            throw new ScenarioRefusedException(subject, $"its pattern does not parse - {wrong.Message}");
        }
    }

    private static string Describing(string? verb, string locator) =>
        $"{(string.IsNullOrWhiteSpace(verb) ? "<no verb>" : verb.Trim())} {locator.Trim()}";
}

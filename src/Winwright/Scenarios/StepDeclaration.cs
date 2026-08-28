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
        Locator? locator,
        string? tray,
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
        string? sameAs,
        string? never,
        bool spoken,
        string? label,
        string? notLabel,
        string? unlike,
        string? sameCountdownAs,
        string? expectReported,
        bool eachSpoken,
        bool ownHeader,
        Asserting.SetMatch matching = Asserting.SetMatch.Exactly)
    {
        Matching = matching;
        Name = name;
        Claimed = name;
        Locator = locator;
        Tray = tray;
        Verb = verb;
        Argument = argument;
        Expected = expected;
        Reads = reads;
        MeansIt = meansIt;
        Moves = moves;
        Sweeps = covers;
        Answers = answers;
        Matches = matches;
        Discloses = discloses;
        SameAs = sameAs;
        Never = never;
        Spoken = spoken;
        Label = label;
        NotLabel = notLabel;
        Unlike = unlike;
        SameCountdownAs = sameCountdownAs;
        ExpectReported = expectReported;
        EachSpoken = eachSpoken;
        OwnHeader = ownHeader;
    }

    /// <summary>What a report calls this step. The verb and the locator where the case named none.</summary>
    public string Name { get; private init; }

    /// <summary>
    /// What it acts on, parsed at declaration and never re-parsed at run time — with the one exception
    /// <see cref="For"/> is: a case repeating over a derived set substitutes the member and parses the
    /// result, which is a different locator and faces the same door.
    /// <para>
    /// WW258. Null on a step whose subject is <see cref="Tray"/>, which is the whole of what a second
    /// kind of subject means: the notification area is in the shell's tree rather than the window's,
    /// and a tray icon has no clickable point either, so there is no locator that reaches one. Read
    /// <see cref="Addressed"/> where what is wanted is the sentence a report shows — that one answers
    /// for both, and every claim that resolves a locator is reached only down the locator branch.
    /// </para>
    /// </summary>
    public Locator? Locator { get; private init; }

    /// <summary>
    /// The notification-area icon this step is about, by the name the shell gives it. Null on a step
    /// addressed by <see cref="Locator"/>, and exactly one of the two is always set.
    /// </summary>
    public string? Tray { get; }

    /// <summary>
    /// What this step is about, as a sentence — the locator's text, or the icon's name. WW258: what a
    /// trace line and a refusal want, and the one accessor that cannot be wrong about which kind of
    /// subject a step has.
    /// </summary>
    public string Addressed => Locator?.Text ?? $"tray icon '{Tray}'";

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

    /// <summary>
    /// The key whose strings must never be showing while this step waits for its locator, or null
    /// where it makes another claim.
    /// <para>
    /// WW256. Every other claim is read after the wait. This one is about the wait: coming back to a
    /// profile seen seconds ago shows its report without ever showing the <em>no readings yet</em>
    /// line, because that line means the per-profile cache did not put the report back. A read taken
    /// afterwards cannot see it — the line is gone by then, which is what passing looks like and also
    /// what a switch that flashed one looks like.
    /// </para>
    /// <para>
    /// A key and never the text, for the reason the project's loading strings are keys: a phrase
    /// written in a case is one a translation rewrites, and a check comparing against it starts
    /// matching nothing the day somebody ships another language. This is that same declaration turned
    /// around — a state that must not be seen at all, rather than one that must be over before
    /// anybody reads.
    /// </para>
    /// </summary>
    public string? Never { get; }

    /// <summary>
    /// Whether this step claims everything under the locator that announces anything announces a
    /// name — and that something does.
    /// <para>
    /// WW253. <see cref="Discloses"/> says there is more under the locator than there was. It does not
    /// say that what is under it <em>reads</em>, and that is the claim the script made about a
    /// conversation row before it ever clicked one: what a screen reader gets from a row is text or it
    /// is a picture, and no capture can tell the two apart.
    /// </para>
    /// <para>
    /// Never a count. The script asserted four or more named descendants, which is the stale literal a
    /// derived set exists to refuse — the row grows a column and the case goes on asserting four. Two
    /// halves instead, both count-free and both falsifiable: something under here speaks, so a row of
    /// pictures fails, and nothing under here announces a glyph, a template or its own automation id,
    /// so a row of codepoints fails. <see cref="Asserting.Names"/> is what tells those apart, and the last three
    /// all satisfy non-empty while being silent to a screen reader.
    /// </para>
    /// <para>
    /// Not <see cref="Answers"/> on the locator, which is worse than nothing here: measured on this
    /// desk, every row in that list announces <c>ClaudeTray.SessionListRow</c> — the CLR type name —
    /// so a step reading the row's own name and claiming it says something passes on a row whose every
    /// field is unreadable.
    /// </para>
    /// </summary>
    public bool Spoken { get; }

    /// <summary>
    /// Whether this step claims every element its locator matches announces a name.
    /// <para>
    /// WW262. <see cref="Covers"/> is one claim over many elements and it is about <em>strings</em>:
    /// every value a key declares reads somewhere the locator matched. This is the other axis — every
    /// element the locator matches announces a label, where the case cannot know and should not care
    /// which. <see cref="Spoken"/> is neither: that is about what sits <em>under</em> one element.
    /// </para>
    /// <para>
    /// The predicate is not <em>non-empty</em>, and the engine already knows the difference: a font
    /// glyph, a template nobody filled in and an automation id handed back all satisfy non-empty while
    /// being silent, or worse, to a screen reader.
    /// </para>
    /// <para>
    /// What it is for is the shape a settings page repeats: thirty-odd rows across six panels under
    /// one naming rule, with the assertion written against three controls of one panel. Naming those
    /// three covers the rule where it was already known to work, and a row added to a panel nobody
    /// listed is covered by nothing — the hardcoded-list defect wearing element clothes.
    /// </para>
    /// </summary>
    public bool EachSpoken { get; }

    /// <summary>
    /// Whether this step claims no control inside a row its locator matches announces a different
    /// row's header.
    /// <para>
    /// WW264. <see cref="EachSpoken"/> proves a control announces something. It cannot prove the
    /// something is its own row's header, because it has no idea which row the control is in — and the
    /// failure hiding there is worse than the one it catches. A rule that pairs the wrong two things
    /// gives several controls one name, and a screen reader reads the same label over each of them.
    /// </para>
    /// <para>
    /// Structural rather than textual, and it takes no list. The headers are the names of the rows the
    /// locator matched, so the set is derived from the page: a control announcing its own row's header
    /// is right, one keeping its own text is right — that is the branch of the rule that must
    /// <em>not</em> fire, and the only one that can produce a duplicate — and one announcing a header
    /// belonging to another row is the defect.
    /// </para>
    /// </summary>
    public bool OwnHeader { get; }

    /// <summary>
    /// The key whose declared string this step's reading should be, or null where it makes another
    /// claim.
    /// <para>
    /// WW261. <see cref="Expected"/> takes a literal, which for a label is the hardcoded set with one
    /// member: it goes stale the day somebody edits the string, and it is wrong in every other
    /// language the application ships from the moment it is written. <see cref="Covers"/> derives the
    /// strings <em>under</em> a key and is no answer for one control — a leaf key has no children, so
    /// the derivation comes back empty and the sweep is broken rather than failed.
    /// </para>
    /// <para>
    /// Not <see cref="Covers"/> with one member either. That claims a set was read <em>somewhere</em>
    /// the locator matched; this claims <em>this</em> element says it, which is the difference between
    /// a panel holding a label and a control announcing one.
    /// </para>
    /// </summary>
    public string? Label { get; }

    /// <summary>
    /// The key whose declared string this step's reading must not be, or null where it makes another
    /// claim.
    /// <para>
    /// WW270. The mirror, and it exists because some states an application has a word for are states
    /// it must not be in. Measured on claude-tray's live strip: the headline is a reading either way,
    /// and what tells a working one from a broken one is whether it is the <em>throughput
    /// unavailable</em> label — which means the tail was disposed on the way out of a profile and
    /// never restarted. No reading of a value could catch that; the numbers are all present.
    /// </para>
    /// <para>
    /// A key rather than the words, for the reason every other declaration here is one: a phrase typed
    /// in a case is one a translation rewrites. <see cref="Matches"/> cannot do it — a negative
    /// lookahead would have to name the English, and a pattern matching the empty string is already
    /// refused, which is what the naive spelling of <em>not this</em> becomes.
    /// </para>
    /// </summary>
    public string? NotLabel { get; }

    /// <summary>
    /// The earlier step this one claims its reading differs from, or null where it makes another
    /// claim.
    /// <para>
    /// WW268. <see cref="SameAs"/>'s other half, and the profiles case needs both for the same reason
    /// it needed the first: two accounts can read the same percentage, so a switch is judged on the
    /// pair rather than on either alone. The script wrote it as <em>the report follows the picker</em>
    /// — an identical reading at both stops means the panes were never repainted, or were repainted
    /// with the profile being left behind.
    /// </para>
    /// <para>
    /// <see cref="Moves"/> is the near miss and cannot answer it. That compares a reading across one
    /// act in one step; this compares two steps with a walk and a wait between them, and the reading
    /// is not the one the act was about — the picker moved, and what has to have moved with it is a
    /// number somewhere else on the page.
    /// </para>
    /// </summary>
    public string? Unlike { get; }

    /// <summary>
    /// The earlier step this one claims its reading is back to, for a reading that counts down while
    /// the case is running.
    /// <para>
    /// WW269. <see cref="SameAs"/> compares exactly, and the reset caption on claude-tray's Statistics
    /// page cannot go through it: it names when a quota window turns over and counts down while the
    /// window is open, so a run that crosses a minute boundary reads it one minute lower and nothing
    /// about the application is wrong. Dropping the claim is worse than tolerating the minute — an
    /// hour of drift is another profile's window, which is the defect WW81 was filed against.
    /// </para>
    /// <para>
    /// The numbers and never the words, which was measured rather than assumed. The task said the
    /// script's parser keyed on `d`, `h` and `m` and that those letters differ in the four other
    /// languages this application ships; they do not. Both of its formatters write them as literal
    /// ASCII and only `dur.now` is translated — so ignoring everything that is not a digit is
    /// language-independent here for a reason, and not by luck.
    /// </para>
    /// <para>
    /// Its own field and never a tolerance on <see cref="SameAs"/>, which is the line the task drew: a
    /// percentage is the same number or it is not, and a general tolerance would soften every exact
    /// claim in every adopting project to serve one caption. The name says the shape it is licensed
    /// for, so nobody reaches for it to quiet a comparison that ought to be exact.
    /// </para>
    /// </summary>
    public string? SameCountdownAs { get; }

    /// <summary>
    /// The name whose value the application reports and this step's reading should be, or null where
    /// the step makes another claim.
    /// <para>
    /// WW294. `expect` with the value read from the application rather than typed. <see cref="Label"/>
    /// is the near miss and answers a different question: that derives from the project's
    /// <em>strings</em>, which is right for a word the product ships and wrong for a fact about this
    /// machine. Which profile a tray icon follows is neither typed nor translated — it is what this
    /// desk happens to be doing, and a case naming it passes here and fails everywhere else.
    /// </para>
    /// <para>
    /// The scalar beside `covers` and its two relatives, which all compare a set. Measured reading
    /// claude-tray's check script: it pulls eight facts out of one read-out and only the first is a
    /// set — the rest are single values, and there was nowhere for them to be declared.
    /// </para>
    /// </summary>
    public string? ExpectReported { get; }

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
    public string? Covers => Sweeps is { } key && Matching == Asserting.SetMatch.Exactly ? key : null;

    /// <summary>
    /// The same set, claimed one way: every declared string is read here, and a value that is not in
    /// the set is allowed rather than a failure.
    /// <para>
    /// WW275. <see cref="Covers"/> holds where nothing is missing <em>and</em> nothing is unexpected,
    /// which is right for the tab set it was built for and cannot express the claim a shared container
    /// needs. Measured migrating WW84: the claim wanted was <em>the sidebar offers every settings panel
    /// the strings declare</em>, all six matched, and it failed on nine strangers — a section heading, a
    /// value caption, Save, Cancel — because the panel beside the sidebar is full of Texts and no
    /// locator separates the two. There was no one-way form to fall back on, so the claim went
    /// unwritten.
    /// </para>
    /// <para>
    /// Its own field and not a flag beside <see cref="Covers"/>, for the reason WW267 gave `pick at`:
    /// one key that means two different claims depending on a second field is a step a reader has to
    /// hold two things in mind to understand. The strangers are still counted and still named in the
    /// sentence — allowed is not the same as unrecorded.
    /// </para>
    /// </summary>
    public string? CoversAtLeast => Sweeps is { } key && Matching == Asserting.SetMatch.AtLeast ? key : null;

    /// <summary>
    /// The same set, claimed by containment: each declared value appears inside the name of something
    /// the locator matched rather than equalling it. WW292, for the entry that decorates what it is
    /// about — a profile's menu entry reads <c>Pessoal  active now</c>, and equality is false of it.
    /// </summary>
    public string? CoversWithin => Sweeps is { } key && Matching == Asserting.SetMatch.Within ? key : null;

    /// <summary>The set this step sweeps, whichever way it claims it. Null where it sweeps none.</summary>
    public string? Sweeps { get; }

    /// <summary>
    /// Which of the three ways this step compares its set. WW275 and WW292: one choice, so the three
    /// properties above are a view of it rather than three things that could disagree.
    /// </summary>
    public Asserting.SetMatch Matching { get; }

    /// <summary>Whether a value read here that the set does not declare fails this step. WW275.</summary>
    public bool SweepsExactly => Matching == Asserting.SetMatch.Exactly;

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
    /// <para>
    /// WW258. A tray step is checkable while carrying none of the fields below, and that is not an
    /// exception to the rule but the rule read properly: the claim is <em>this icon can be found</em>,
    /// which the search answers pass, fail or hole. The unearned-green guard is about a step that
    /// produces no assertion result, and this one always produces exactly one — so a case made of
    /// tray steps has earned whatever it reads.
    /// </para>
    /// </summary>
    public bool Checkable =>
        Tray is not null
        || Expected is not null || Moves || Answers || Sweeps is not null || Matches is not null || Discloses
        || SameAs is not null || Never is not null || Spoken
        || Label is not null || NotLabel is not null || Unlike is not null || SameCountdownAs is not null
        || ExpectReported is not null
        || EachSpoken || OwnHeader;

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
    /// <param name="never">The key whose strings must never show while this step waits for its locator.</param>
    /// <param name="spoken">That everything under the locator which says anything says a name.</param>
    /// <param name="label">The key whose declared string the reading should be.</param>
    /// <param name="notLabel">The key whose declared string the reading should not be.</param>
    /// <param name="unlike">The earlier step this one claims its reading differs from.</param>
    /// <param name="expectReported">
    /// The name whose value the application reports and the reading should be, declared in the
    /// project's <c>reportedValues</c>. WW294, and never beside <paramref name="expected"/>.
    /// </param>
    /// <param name="sameCountdownAs">
    /// The earlier step this one claims its reading is back to, allowing the last number in it to have
    /// ticked down by one. WW269, and at most one of this, <paramref name="sameAs"/> and
    /// <paramref name="unlike"/>.
    /// </param>
    /// <param name="eachSpoken">That every element the locator matches announces a name.</param>
    /// <param name="ownHeader">That no control in a row announces another row's header.</param>
    /// <exception cref="ScenarioRefusedException">Where any field could not run on any machine.</exception>
    /// <param name="tray">
    /// The notification-area icon this step is about, where that is its subject rather than a locator.
    /// WW258: exactly one of the two, which <see cref="ScenarioSchema.Miscarried"/> enforces at the
    /// point of insertion and this refuses again for a caller that did not come through the loader.
    /// </param>
    /// <param name="coversAtLeast">
    /// The key whose every declared string must be read somewhere the locator matches, allowing values
    /// the set does not declare. WW275, and at most one of this and <paramref name="covers"/>.
    /// </param>
    /// <param name="coversWithin">
    /// The key whose every declared string must appear <em>inside</em> the name of something the
    /// locator matched. WW292, and at most one of the three ways of claiming a set.
    /// </param>
    public static StepDeclaration Of(
        string? locator,
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
        string? sameAs = null,
        string? never = null,
        bool spoken = false,
        string? label = null,
        string? notLabel = null,
        string? unlike = null,
        string? sameCountdownAs = null,
        string? expectReported = null,
        bool eachSpoken = false,
        bool ownHeader = false,
        string? tray = null,
        string? coversAtLeast = null,
        string? coversWithin = null)
    {
        // WW275 and WW292. At most one of the three, and refused where they are written: they are one
        // set claimed three different ways, and a step naming two would have the run honour whichever
        // the code reads first.
        var ways = new List<string>();
        if (!string.IsNullOrWhiteSpace(covers))
            ways.Add("'covers'");
        if (!string.IsNullOrWhiteSpace(coversAtLeast))
            ways.Add("'coversAtLeast'");
        if (!string.IsNullOrWhiteSpace(coversWithin))
            ways.Add("'coversWithin'");

        if (ways.Count > 1)
        {
            throw new ScenarioRefusedException(
                string.IsNullOrWhiteSpace(named) ? "<a step>" : named.Trim(),
                $"it carries {string.Join(" and ", ways)}; those are the same set claimed different "
                    + "ways, so name the one this step means");
        }

        // All three derive the same set through the same door, so everything below reads it as
        // `covers` and only the comparison is told which claim it is.
        var matching = !string.IsNullOrWhiteSpace(coversAtLeast) ? Asserting.SetMatch.AtLeast
            : !string.IsNullOrWhiteSpace(coversWithin) ? Asserting.SetMatch.Within
            : Asserting.SetMatch.Exactly;

        covers = ways.Count == 0 ? covers : (covers ?? coversAtLeast ?? coversWithin);

        var called = string.IsNullOrWhiteSpace(named) ? null : named.Trim();
        var named_tray = string.IsNullOrWhiteSpace(tray) ? null : tray.Trim();
        var subject = called ?? Describing(verb, locator ?? $"tray icon '{named_tray}'");

        // WW258. Both arms, and here as well as in the schema: the loader comes through
        // `ScenarioSchema.Miscarried`, and a caller building a declaration in code does not — so the
        // rule that a step addresses exactly one thing lives on the type that cannot be bypassed.
        if (string.IsNullOrWhiteSpace(locator) && named_tray is null)
            throw new ScenarioRefusedException(subject, "a step acts on something, and this one names nothing");

        if (!string.IsNullOrWhiteSpace(locator) && named_tray is not null)
        {
            throw new ScenarioRefusedException(
                subject,
                "a step acts on one thing, and this one names a locator and a tray icon; a tray icon is "
                    + "not in the window's tree, so no locator reaches it and naming both names two subjects");
        }

        if (named_tray is not null)
            return Trayed(subject, named_tray, verb, argument, expected, reads, meansIt, moves, covers, answers, matches, discloses, sameAs, never, spoken, label, notLabel, unlike, sameCountdownAs, expectReported, eachSpoken, ownHeader);

        // WW263 and WW273. A locator with a brace in it has to parse with something in it as well as
        // with the placeholder, and both are facts about the file rather than about a run. Probed here
        // so the refusal arrives where the locator was written, not on the member — or the string —
        // that happened to expose it.
        // Not null from here: the two guards above returned for a tray step and threw for a step that
        // named neither, so what is left named a locator.
        if (Braced.IsMatch(locator!)
            && !Locator.TryParse(Braced.Replace(locator!, "probe"), out _, out var wrongly))
        {
            throw new ScenarioRefusedException(
                subject,
                $"it is built out of something the run substitutes and does not parse with one in it: {wrongly}");
        }

        // Parsed here rather than at run time on purpose: a locator that does not parse is wrong on
        // every machine, and the reader of a red about one is opening the wrong repository.
        if (!Locator.TryParse(locator!, out var parsed, out var because))
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

        // WW268, the same shape as the one above it: the rules that ask whether a step claims
        // anything have to know about this one before they can name the right field.
        var apart = string.IsNullOrWhiteSpace(unlike) ? null : unlike.Trim();

        // WW269, and the same again. It is `sameAs` for a reading that ticks while the case runs, so
        // every rule below that names one of the three has to know about it or it names the wrong one.
        var ticking = string.IsNullOrWhiteSpace(sameCountdownAs) ? null : sameCountdownAs.Trim();

        // WW294, computed with the others for the reason all of them are: a step whose only claim is
        // this one must not be refused as a step that claims nothing.
        var reportedly = string.IsNullOrWhiteSpace(expectReported) ? null : expectReported.Trim();

        // It is `expect` with the value read from the application, so the two together are one claim
        // written twice — and which of them the run would honour is whichever the code reads first.
        if (reportedly is not null && !string.IsNullOrWhiteSpace(expected))
        {
            throw new ScenarioRefusedException(
                string.IsNullOrWhiteSpace(named) ? "<a step>" : named.Trim(),
                $"it expects '{expected}' and also the value the application reports under "
                    + $"'{reportedly}'; a step answers one thing, and these are two");
        }

        if (ticking is not null && (back ?? apart) is { } alsoPointing)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it claims its reading is back to '{ticking}' give or take a tick and also compares it "
                    + $"with '{alsoPointing}'; a step answers one thing, and these are two");
        }

        // WW256, and the same again: a claim about the wait is still a claim, so a step making only
        // this one must not be refused as a step that makes none.
        var forbidden = string.IsNullOrWhiteSpace(never) ? null : never.Trim();

        // WW261 and WW270, computed with the others and for the same reason: the rules that ask
        // whether a step claims anything must know about a claim before it can name the right field.
        var declared = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
        var undeclared = string.IsNullOrWhiteSpace(notLabel) ? null : notLabel.Trim();

        var claims = wanted is not null || moves || answers || sweeping is not null || pattern is not null
            || discloses || back is not null || apart is not null || forbidden is not null || spoken
            || declared is not null || undeclared is not null || ticking is not null || reportedly is not null
            || eachSpoken || ownHeader;

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

        // WW268. Both point at a step and both are refused for the same three things, so they are
        // judged together: two copies of these rules is where the second one goes on saying the old
        // thing after the first moves.
        if (back is not null && apart is not null)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it claims its reading is back to '{back}' and also that it differs from '{apart}'; "
                    + "a step answers one thing, and these are two");
        }

        if ((back ?? apart ?? ticking) is { } pointed)
        {
            var field = back is not null ? "sameAs" : apart is not null ? "unlike" : "sameCountdownAs";
            var claim = apart is not null ? "differs from" : "is back to";

            // One claim per step, as everywhere else. 'expect' names the value and this is the claim
            // for a value the case cannot name — it only knows which earlier step to compare with.
            if (wanted is not null || moves || answers || sweeping is not null || pattern is not null || discloses)
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it claims its reading {claim} '{pointed}' and also makes another claim; '{field}' "
                        + "is for the value a case cannot name and can only point at");
            }

            // A step comparing itself to itself is answered before the window is: `sameAs` holds
            // whatever it did and `unlike` fails whatever it did, and neither is a reading. It is also
            // the easy typo — a round trip's stops read the same element under the same verb, so a
            // case that left them unnamed would have written this by accident.
            if (string.Equals(pointed, subject, StringComparison.Ordinal))
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it claims its reading {claim} itself, which is answered before the window is; "
                        + "name the earlier step in 'named' and point at that");
            }

            // Which reading, said out loud. The comparison is between two readings and the default is
            // whichever one the element happens to answer first, so a step that left it out would
            // compare a value to a name on the day the control gained a pattern.
            if (string.IsNullOrWhiteSpace(reads))
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it claims its reading {claim} '{pointed}' and does not say which reading; "
                        + $"'{field}' compares two of them, so the default would compare whichever "
                        + "answered first");
            }
        }

        if (declared is not null && undeclared is not null)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it claims the reading is '{declared}' and also that it is not '{undeclared}'; a step "
                    + "answers one thing, and these are two");
        }

        // One claim per step, as everywhere else. 'expect' names the value and this names the key the
        // value comes from, and a step holding both owes two assertion results.
        if ((declared is not null || undeclared is not null)
            && (wanted is not null || moves || answers || sweeping is not null || pattern is not null
                || discloses || back is not null || forbidden is not null || spoken))
        {
            throw new ScenarioRefusedException(
                subject,
                $"it names the '{declared ?? undeclared}' string and also makes another claim; the "
                    + "key is where the value comes from rather than a second thing to check");
        }

        if (ownHeader)
        {
            // One claim per step, as everywhere else.
            if (wanted is not null || moves || answers || sweeping is not null || pattern is not null
                || discloses || back is not null || apart is not null || forbidden is not null || spoken
                || declared is not null || undeclared is not null || eachSpoken)
            {
                throw new ScenarioRefusedException(
                    subject,
                    "it claims each row's controls announce that row and also makes another claim; the "
                        + "pairing is one claim over the rows the locator matched");
            }

            // The same rule a sweep is under: this reads every row the locator matches and everything
            // inside them, and one act over all of that is not a claim about any of it.
            if (!act.Reads)
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it claims each row's controls announce that row and acts with '{act.Name}'; the "
                        + "pairing reads every row its locator matches, and one act over many of them "
                        + "is not a claim");
            }

            if (!string.IsNullOrWhiteSpace(reads))
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it claims each row's controls announce that row and names the '{reads.Trim()}' "
                        + "reading; the claim is about what those controls announce, which is their name");
            }
        }

        if (eachSpoken)
        {
            // One claim per step, as everywhere else.
            if (wanted is not null || moves || answers || sweeping is not null || pattern is not null
                || discloses || back is not null || apart is not null || forbidden is not null || spoken
                || declared is not null || undeclared is not null)
            {
                throw new ScenarioRefusedException(
                    subject,
                    "it claims every element it matches is named and also makes another claim; a sweep "
                        + "is one claim over many elements, and every other field on a step is about one");
            }

            // The same rule `covers` is under, and for its reason: a sweep reads every element its
            // locator matches, and one act over many of them is not a claim about any of them.
            if (!act.Reads)
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it claims every element it matches is named and acts with '{act.Name}'; a sweep "
                        + "reads every element its locator matches, and one act over many of them is "
                        + "not a claim");
            }

            // And no reading beside it, for the reason a disclosure takes none: what these elements
            // announce is their name, always, and a 'reads' here would narrow nothing.
            if (!string.IsNullOrWhiteSpace(reads))
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it claims every element it matches is named and names the '{reads.Trim()}' "
                        + "reading; the claim is about what those elements announce, which is their name");
            }
        }

        if (spoken)
        {
            // One claim per step, as everywhere else. This one is about the subtree, and the others
            // are about a reading of the element the locator matched.
            if (wanted is not null || moves || answers || sweeping is not null || pattern is not null
                || discloses || back is not null || forbidden is not null)
            {
                throw new ScenarioRefusedException(
                    subject,
                    "it claims everything under the locator is named and also makes another claim; "
                        + "'spoken' is about the tree under it rather than about what it says");
            }

            // And no reading beside it, for the reason a disclosure takes none: the subject is the
            // subtree, and a 'reads' here would look like it narrowed the claim and would narrow
            // nothing. What the elements under it announce is their name, always.
            if (!string.IsNullOrWhiteSpace(reads))
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it claims what is under the locator is named and names the '{reads.Trim()}' reading; "
                        + "the claim is about what those elements announce, which is their name");
            }
        }

        if (forbidden is not null)
        {
            // One claim per step, as everywhere else — and here for a reason of its own. What ends the
            // wait this claim is about is the locator resolving, so a second claim beside it would be
            // read at a moment this one chose, and a reader could not tell which of the two the step
            // is reporting.
            if (wanted is not null || moves || answers || sweeping is not null
                || pattern is not null || discloses || back is not null)
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it claims '{forbidden}' never shows and also makes another claim; what ends the "
                        + "wait a 'never' is about is the locator arriving, so the two would be read "
                        + "at one moment and reported as one line");
            }

            // A reading beside it narrows nothing. The claim is about the window, not about this
            // element: the string may show anywhere, and the locator is what says when to stop
            // looking rather than what to look at.
            if (!string.IsNullOrWhiteSpace(reads))
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it claims '{forbidden}' never shows and names the '{reads.Trim()}' reading; the "
                        + "claim is about the window while this step waited, and the locator is what "
                        + "says when the waiting is over");
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
            null,
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
            back,
            forbidden,
            spoken,
            declared,
            undeclared,
            apart,
            ticking,
            reportedly,
            eachSpoken,
            ownHeader,
            matching);
    }

    /// <summary>
    /// A step whose subject is a notification-area icon.
    /// <para>
    /// WW258. The claim a tray step makes is <em>this icon can be found</em>, and that is the whole of
    /// it: <see cref="Acting.NotificationArea.Find"/> matches by the name the shell gives an icon, so
    /// an expectation about the name would be comparing the search's own argument with its own answer.
    /// Everything else a step can claim is about a reading taken through a control's patterns, and a
    /// tray icon has none — it is a rectangle and a tooltip.
    /// </para>
    /// <para>
    /// So every one of those fields is refused rather than accepted and ignored. That is this format's
    /// founding rule pointed at its own newest field: a key that loads and does nothing is a check the
    /// author wrote and the run never made, and the second subject would ship with a dozen of them.
    /// </para>
    /// </summary>
    private static StepDeclaration Trayed(
        string subject,
        string tray,
        string verb,
        string? argument,
        string? expected,
        string? reads,
        bool meansIt,
        bool moves,
        string? covers,
        bool answers,
        string? matches,
        bool discloses,
        string? sameAs,
        string? never,
        bool spoken,
        string? label,
        string? notLabel,
        string? unlike,
        string? sameCountdownAs,
        string? expectReported,
        bool eachSpoken,
        bool ownHeader)
    {
        var act = ActVerb.Named(verb);
        if (!act.OnATray)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it is about a tray icon and names '{act.Name}'; an icon is not an element, so the acts "
                    + $"that ask a control through its patterns do not apply — it takes "
                    + $"{string.Join(" or ", ActVerb.All.Where(one => one.OnATray).Select(one => $"'{one.Name}'"))}");
        }

        if (act.Refuses(argument) is { } wrong)
            throw new ScenarioRefusedException(subject, wrong);

        // Named one by one rather than as a count, so the refusal says which field to delete.
        var claimed = new List<string>();
        foreach (var (field, written) in new (string, bool)[]
        {
            ("expect", expected is not null),
            ("reads", reads is not null),
            ("moves", moves),
            ("answers", answers),
            ("matches", matches is not null),
            ("discloses", discloses),
            ("sameAs", sameAs is not null),
            ("unlike", unlike is not null),
            ("sameCountdownAs", sameCountdownAs is not null),
            ("expectReported", expectReported is not null),
            ("label", label is not null),
            ("notLabel", notLabel is not null),
            ("never", never is not null),
            ("covers", covers is not null),
            ("spoken", spoken),
            ("eachSpoken", eachSpoken),
            ("ownHeader", ownHeader),
            ("meansIt", meansIt),
        })
        {
            if (written)
                claimed.Add($"'{field}'");
        }

        if (claimed.Count > 0)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it is about a tray icon and carries {string.Join(", ", claimed)}; those are claims about "
                    + "a reading taken through a control's patterns, and an icon has none — it is a "
                    + "rectangle and a tooltip, so the claim a tray step makes is that it can be found");
        }

        return new StepDeclaration(
            subject, null, tray, act, null, null, ReadBack.Named(null), false, false, null, false, null,
            false, null, null, false, null, null, null, null, null, false, false);
    }

    /// <summary>
    /// Whether this step asks a tray icon for its menu rather than only whether it is showing. WW258,
    /// and asked off the verb rather than stored: the vocabulary is the one place a tray act is
    /// declared, and a second flag would be a second answer about the same word.
    /// </summary>
    public bool OpensTheTrayMenu => Tray is not null && !Verb.Reads;

    /// <summary>What a locator writes where the member of a repeated case belongs.</summary>
    public const string Member = "{}";

    /// <summary>
    /// This step with <see cref="Member"/> replaced by one member of the set its case repeats over.
    /// <para>
    /// WW263. The locator is re-parsed rather than patched, because a locator is parsed once at
    /// declaration on purpose and a substituted one is a different locator: it has to face the same
    /// door. A member that makes it unparseable is refused naming both, which is the only run-time
    /// refusal this feature can have and is why the declaration proves a probe substitutes first.
    /// </para>
    /// <para>
    /// The name carries the member too. Twelve steps across four panels reported under four identical
    /// names is a trace a reader has to count lines in to use.
    /// </para>
    /// </summary>
    /// <param name="member">One string of the derived set.</param>
    /// <exception cref="ScenarioRefusedException">Where the substituted locator does not parse.</exception>
    public StepDeclaration For(string member)
    {
        ArgumentNullException.ThrowIfNull(member);

        // WW258. A repeated case calls this on every step it holds, not only the ones naming the
        // member — so a tray step reaches here and has no locator to substitute into. It takes the
        // member into its reported name and nothing else, which is what a step that does not name the
        // member gets anyway.
        if (Locator is null)
            return this with { Name = $"{Name} [{member}]" };

        var text = Locator.Text.Replace(Member, member, StringComparison.Ordinal);
        if (!Locator.TryParse(text, out var parsed, out var because))
            throw new ScenarioRefusedException($"{Name} [{member}]", $"'{text}' does not parse: {because}");

        return this with
        {
            Locator = parsed!,
            Name = $"{Name} [{member}]",
            // The last step and not the whole locator: that is the one a sweep's matches are of, so
            // `Group[name="{}"]` finding nothing is the strings and the window disagreeing, and
            // `Group[name="{}"] > ComboBox` finding nothing is a row that holds no picker (WW276).
            Carries = Locator.Steps[^1].Mentions(Member) ? member : null,
        };
    }

    /// <summary>
    /// The name this step is reported under before a repeated case adds the member, which is the same
    /// thing for a case that runs once.
    /// <para>
    /// WW276. It is what says two results are the same claim. A case walking six panels asserts one
    /// rule six times and those are one claim — red where any panel that carried it failed, a hole
    /// only where none of them did — and the run needs a key to gather them by that a member's name
    /// cannot be.
    /// </para>
    /// </summary>
    public string Claimed { get; private init; }

    /// <summary>
    /// Whether this step's locator names the member of a repeated case. False for a tray step, which
    /// has no locator to name it in — WW258.
    /// </summary>
    public bool NamesTheMember => Locator is { } one && one.Text.Contains(Member, StringComparison.Ordinal);

    /// <summary>
    /// Anything a run substitutes into a locator: <c>{}</c> for the member, <c>{a.key}</c> for a
    /// string the project declares. Empty braces and a key are one shape on purpose — a reader who
    /// has seen either knows a brace is a hole the run fills.
    /// </summary>
    private static readonly System.Text.RegularExpressions.Regex Braced = new(
        @"\{([^{}]*)\}",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// The keys this step's locator is built out of, in the order they are written. Empty where it
    /// names its element in words.
    /// </summary>
    public IReadOnlyList<string> Declares() => Locator is null ? [] : Braced.Matches(Locator.Text)
        .Select(one => one.Groups[1].Value)
        .Where(one => one.Length > 0)
        .Distinct(StringComparer.Ordinal)
        .ToList();

    /// <summary>
    /// This step with every <c>{a.key}</c> in its locator replaced by the string that key declares.
    /// <para>
    /// WW273. Every other expectation in this engine is derived and a locator was not: it carried the
    /// words, and the words go stale the day somebody edits the strings file and are wrong in every
    /// other language the application ships from the moment they are written. claude-tray's settings
    /// sidebar is six bare Borders with no automation peer, so the words are the only thing that
    /// addresses one — and the migrated keyboard case had to say in a comment that the label it names
    /// happens to be the same in all four languages, which is the defect holding still.
    /// </para>
    /// <para>
    /// The locator is re-parsed for the reason <see cref="For"/> re-parses: a substituted locator is a
    /// different locator and has to face the same door. The text becomes the substituted one, so what
    /// the trace records is the words the run actually looked for — which is the reading a red is
    /// about. The key stays in the case file, one line away, for the reader who wants to know why.
    /// </para>
    /// </summary>
    /// <param name="reading">What a key declares, or a throw saying why it cannot be read.</param>
    /// <exception cref="ScenarioRefusedException">
    /// Where a key declares nothing, or the substituted locator does not parse. Both are the scenario
    /// being wrong rather than the application, so both arrive before anything is driven.
    /// </exception>
    public StepDeclaration Naming(Func<string, string> reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        if (Declares().Count == 0)
            return this;

        // Not null here: `Declares()` answers empty for a tray step, so the return above took it.
        var text = Braced.Replace(Locator!.Text, one => one.Groups[1].Value.Length == 0
            ? one.Value
            : reading(one.Groups[1].Value));

        if (!Locator.TryParse(text, out var parsed, out var because))
            throw new ScenarioRefusedException(Name, $"'{text}' does not parse: {because}");

        return this with { Locator = parsed! };
    }

    /// <summary>
    /// The declared string this step's locator was built out of, or null where it was built out of none.
    /// <para>
    /// WW272. It is what separates the two ways a sweep can match nothing. A locator carrying a string
    /// the project declares and matching nothing is the disagreement `WW263` exists to produce — the
    /// file says this row is there and the window does not draw it, which is wrong on every machine.
    /// A locator carrying no such string and matching nothing says only that this window has none of
    /// these right now, and that is sometimes the page.
    /// </para>
    /// <para>
    /// Set only where the locator actually named the member: a repeated case renames all of its steps
    /// so the trace says which run each line came from, and a step that does not reach the member is
    /// not made a claim about it by being repeated alongside one that does.
    /// </para>
    /// </summary>
    public string? Carries { get; private init; }

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

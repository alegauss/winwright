using Winwright.Locating;

namespace Winwright.Scenarios;

/// <summary>
/// Which of the three ways a step compares its reading with an earlier step's.
/// <para>
/// WW308. Three and not two booleans, for the reason <see cref="Asserting.SetMatch"/> gives about
/// itself: they are one choice. The three fields a case writes — `sameAs`, `unlike` and
/// `sameCountdownAs` — stay three, because WW296 read them and kept them: the suffix carries the
/// mode in the name, so no step can omit one and fall into a claim it never made. What changes here
/// is underneath, where the engine had them as three loose strings and rebuilt this choice out of
/// them at five call sites.
/// </para>
/// </summary>
public enum Pointing
{
    /// <summary>The reading is claimed to be back to what the earlier step read. `sameAs`.</summary>
    Same,

    /// <summary>The reading is claimed to differ from it. `unlike`.</summary>
    Unlike,

    /// <summary>
    /// The same claim as <see cref="Same"/> for a reading that counts down while the case runs: the
    /// numbers must match except the last, which may have ticked by one. `sameCountdownAs`.
    /// </summary>
    Countdown,

    /// <summary>
    /// The reading is claimed to hold the earlier step's inside it. `contains`.
    /// <para>
    /// WW326. The other three compare two readings whole, which is right for a percentage read at two
    /// stops of a round trip and false of the commonest thing a dialog does: it names the thing it
    /// opened for. Measured on pportal's capture prompt — it quotes the pad's name, and neither
    /// string can be typed in a case, because one is whatever controller is plugged into this desk
    /// and the other is built out of it.
    /// </para>
    /// </summary>
    Contains,
}

/// <summary>
/// One thing a step claims: the field the case wrote, and what that field claims. WW340.
/// <para>
/// A claim was a spelling before this — a name in a boolean chain, a name in a schema row, a name
/// in whichever refusal remembered it — and every claim added so far arrived in some of those and
/// not all. WW323 made the refusal read one list; this is that list, kept rather than dropped, so
/// the other readers ask it instead of writing their own.
/// </para>
/// <para>
/// The field and never the mode the engine folded it into. Three ways of claiming one set are one
/// claim and three spellings, and a refusal that named the fold would tell an author to delete a
/// key their file does not have.
/// </para>
/// </summary>
/// <param name="Field">The key, spelled as the file spells it.</param>
/// <param name="Says">What it claims, in the words a refusal reads it in.</param>
public sealed record Claim(string Field, string Says)
{
    /// <summary>The one phrase a refusal names it by.</summary>
    public override string ToString() => $"'{Field}' ({Says})";
}

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
    /// <summary>
    /// The three a step cannot be without, and what the case wrote. WW352, WW391.
    /// <para>
    /// This took twenty-three parameters, one per field a step can carry, and every field added
    /// reached it by hand. Three nullable strings in a row is a signature where a transposed pair of
    /// positional arguments compiles, and the worst example was in this file rather than in a test:
    /// a tray step was built from twenty-one positional arguments of which eighteen were
    /// <c>null</c> or <c>false</c>, so the one that mattered was findable only by counting commas.
    /// </para>
    /// <para>
    /// WW352 left the fields to be set by name and WW391 took the last of them out: what a case
    /// wrote arrives whole, and every field below is a read of it. <see cref="Verb" /> and
    /// <see cref="Reads" /> stay here because they are the two <see cref="Of" /> has already
    /// judged — <c>act</c> and <c>reads</c> are refused against a closed vocabulary before a step
    /// exists, so recomputing them on each read would be a refusal that fires at some later moment
    /// or never.
    /// </para>
    /// <para>
    /// A fourth parameter and not a fourth field. Nothing here can be transposed with anything
    /// beside it — one string among three unrelated types — and a fifth would be a field that had
    /// found its way back into a position.
    /// </para>
    /// </summary>
    private StepDeclaration(string name, ActVerb verb, ReadBack reads, Written wrote)
    {
        Name = name;
        Claimed = name;
        Verb = verb;
        Reads = reads;
        Wrote = wrote;
    }

    /// <summary>
    /// What the case wrote, by the names the format spells them. WW391.
    /// <para>
    /// Kept rather than consumed, and <c>reads</c> is why. Every other field of the format is
    /// answered by a property below, but a step that named no reading and one that named the
    /// default both carry <see cref="ReadBack.Anything" /> — so the step cannot say which it was,
    /// and half the refusals in <see cref="Of" /> turn on exactly that. Keeping what the case wrote
    /// answers it, and answers the two folded families at the same time: <see cref="Sweeps" /> and
    /// <see cref="PointsAt" /> each hold one of several spellings, and a refusal names the field the
    /// file used rather than the mode the engine folded it into.
    /// </para>
    /// </summary>
    public Written Wrote { get; }

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
    public string? Tray => Wrote.Trimmed("tray");

    /// <summary>
    /// What this step is about, as a sentence — the locator's text, or the icon's name. WW258: what a
    /// trace line and a refusal want, and the one accessor that cannot be wrong about which kind of
    /// subject a step has.
    /// </summary>
    public string Addressed => Locator?.Text ?? $"tray icon '{Tray}'";

    /// <summary>
    /// The popup inside this step's window whose tree the capture is of, by the name the
    /// application's own author gave it. Null on every step but a capture that names one. WW372.
    /// <para>
    /// Not a second subject and not a locator. The step still addresses a window the ordinary way,
    /// and this says which surface <em>inside</em> it the picture is of — which is why it is a
    /// field on the capture rather than a third arm of the group WW258 opened.
    /// </para>
    /// <para>
    /// It is the surface an outside process cannot photograph at all, which is what makes the ask
    /// worth a field. WW347: an open popup is its own layered window whose soft edge is a strip of
    /// whatever it stands in front of, so a screen copy is refused; a closed one has no window to
    /// copy. To the application it is an element in a tree it owns either way, so a preview of a
    /// flyout nobody has clicked is a picture this can ask for and no copy ever could.
    /// </para>
    /// </summary>
    public string? Popup => Wrote.Trimmed("popup");

    /// <summary>What it does.</summary>
    public ActVerb Verb { get; }

    /// <summary>What the verb was given, or null where it takes nothing.</summary>
    public string? Argument => Wrote.Trimmed("with");

    /// <summary>
    /// What <see cref="Reads"/> should say once the act has landed, or null where this step is an
    /// act and nothing else — a navigation whose consequence a later step is the check for.
    /// </summary>
    public string? Expected => Wrote.Text("expect");

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
    public System.Text.RegularExpressions.Regex? Matches { get; private init; }

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
    public bool Discloses => Wrote.Truth("discloses");

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
    public string? SameAs => PointsAt is { } step && Pointing == Pointing.Same ? step : null;

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
    public string? Never => Wrote.Trimmed("never");

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
    public bool Spoken => Wrote.Truth("spoken");

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
    public bool EachSpoken => Wrote.Truth("eachSpoken");

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
    public bool OwnHeader => Wrote.Truth("ownHeader");

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
    public string? Label => Wrote.Trimmed("label");

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
    public string? NotLabel => Wrote.Trimmed("notLabel");

    /// <summary>
    /// The key whose declared string this step's reading must begin with, or null where it makes
    /// another claim.
    /// <para>
    /// WW83. The third of the same family, and the one an announcement needs. <see cref="Label"/>
    /// compares the whole reading, which is right for a control that announces a label and nothing
    /// else. An application that has to say a <em>state</em> as words writes the word in front of a
    /// sentence — claude-tray's tray entries read <c>Checked · C:\Users\…\.claude · the tray only</c>
    /// — so equality is false of a state that is correctly announced.
    /// </para>
    /// <para>
    /// A prefix and never a containment, and that is the application's own rule rather than a
    /// convenience: the state is written in front precisely because the sentence behind it is free
    /// text that can hold either word — <em>turning it off puts the variable back</em> — so a
    /// containment would report a switch as on because its explanation says the word "on".
    /// </para>
    /// </summary>
    public string? BeginsWithLabel => Wrote.Trimmed("beginsWithLabel");

    /// <summary>
    /// Whether this step claims its locator matches nothing.
    /// <para>
    /// WW318. Every other claim reads a subject, and a locator matching nothing has no subject to
    /// read — so <c>"expect": "absent"</c> came out as <em>nothing answered to it in 109 polls</em>,
    /// which is word for word what a genuinely broken read produces. The two were indistinguishable
    /// in a report, and one of them was the pass.
    /// </para>
    /// <para>
    /// Found adopting this in an application whose window makes its argument by what is not in it —
    /// no toolbar, no status bar, no sidebar, and not hidden ones waiting to be switched on. Absence
    /// from the accessibility tree is the strongest form of that claim, because it is what a screen
    /// reader would find, and it catches what a tree walk cannot: chrome a theme or a host draws
    /// without the window's own tree holding it.
    /// </para>
    /// <para>
    /// The unearned green it would otherwise be is refused two ways. <see cref="Subject.RegionIsThere"/>
    /// keeps the claim from holding because the region was never there, and the wait runs the other
    /// way round: it polls until the locator matches nothing rather than until it matches, so a
    /// control on its way out is waited for and one that never leaves fails naming what it found.
    /// </para>
    /// </summary>
    public bool Absent => Wrote.Truth("absent");

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
    public string? Unlike => PointsAt is { } step && Pointing == Pointing.Unlike ? step : null;

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
    public string? SameCountdownAs =>
        PointsAt is { } step && Pointing == Pointing.Countdown ? step : null;

    /// <summary>
    /// The earlier step whose reading this one claims to hold inside its own, or null where it makes
    /// another claim.
    /// <para>
    /// WW326. The fourth of the family and the one a dialog needs: it quotes the thing it opened for,
    /// so equality is false of it and neither string can be typed — one is what this desk happens to
    /// have and the other is built out of it.
    /// </para>
    /// <para>
    /// Every near miss answers a different question. <see cref="SameAs"/> is equality.
    /// <see cref="Matches"/> takes a pattern the case types, which is the value no case can know.
    /// <c>coversWithin</c> is WW292's and compares a <em>derived set</em> against many elements
    /// rather than one reading against one earlier reading. And <see cref="BeginsWithLabel"/> is
    /// containment at the front of a string against the project's own strings, not against a step.
    /// </para>
    /// </summary>
    public string? Contains => PointsAt is { } step && Pointing == Pointing.Contains ? step : null;

    /// <summary>
    /// The earlier step this one compares its reading with, however it claims it. Null where it
    /// points at none.
    /// <para>
    /// WW308. The three properties above are a view of this and <see cref="Pointing"/>, so they
    /// cannot disagree with each other — and the run asks this rather than rebuilding it. Before, the
    /// shape was reassembled at five sites by a chain of <c>??</c>, with the field's own name worked
    /// out again by ternary at the last of them. Each was right; together they were one idea spelled
    /// five times, and the sixth was the one that would have spelled it differently.
    /// </para>
    /// </summary>
    public string? PointsAt => Compared?.Step;

    /// <summary>
    /// Which of the three ways this step compares with the step <see cref="PointsAt"/> names. WW308:
    /// meaningless where that is null, in the same way <see cref="Matching"/> is where nothing sweeps.
    /// </summary>
    public Pointing Pointing => Compared?.Means ?? Pointing.Same;

    /// <summary>
    /// The comparison this step wrote: the field the case spelled, the step it names, and what it
    /// claims of it. Null where the step compares itself with nothing. WW391.
    /// <para>
    /// One walk over <see cref="Comparisons"/> answers all three, so <see cref="PointsAt"/>,
    /// <see cref="Pointing"/>, the claim set and the refusal that needs the spelling cannot disagree
    /// about which of the four the file used. The order is the one WW308's fold took and the one the
    /// refusal's own naming used before there was a fold, so nothing about it is new here.
    /// </para>
    /// </summary>
    private (string Field, string Step, Pointing Means)? Compared
    {
        get
        {
            // `named` and not `field`, which C# 14 has taken as a keyword inside an accessor.
            foreach (var (named, means) in Comparisons)
                if (Wrote.Trimmed(named) is { } step)
                    return (named, step, means);

            return null;
        }
    }

    /// <summary>
    /// The four spellings of one comparison, in the order the fold takes them, each beside what it
    /// claims. WW391: the list WW308 wrote as a switch and two ternary chains, kept once.
    /// </summary>
    private static readonly (string Field, Pointing Means)[] Comparisons =
    [
        ("sameAs", Pointing.Same),
        ("unlike", Pointing.Unlike),
        ("sameCountdownAs", Pointing.Countdown),
        ("contains", Pointing.Contains),
    ];

    /// <summary>
    /// The three spellings of one sweep, in the order the fold takes them, each beside how it
    /// compares. WW391, and the same shape as <see cref="Comparisons"/> for the same reason.
    /// </summary>
    private static readonly (string Field, Asserting.SetMatch Means)[] Sweepings =
    [
        ("covers", Asserting.SetMatch.Exactly),
        ("coversAtLeast", Asserting.SetMatch.AtLeast),
        ("coversWithin", Asserting.SetMatch.Within),
    ];

    /// <summary>
    /// The sweep this step wrote: the field the case spelled, the key it names, and how it compares.
    /// Null where the step sweeps nothing. WW391, and <see cref="Compared"/>'s twin.
    /// </summary>
    private (string Field, string Key, Asserting.SetMatch Means)? Swept
    {
        get
        {
            foreach (var (named, means) in Sweepings)
                if (Wrote.Trimmed(named) is { } key)
                    return (named, key, means);

            return null;
        }
    }

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
    public string? ExpectReported => Wrote.Trimmed("expectReported");

    /// <summary>Which reading the expectation is about. <see cref="ReadBack.Anything"/> by default.</summary>
    public ReadBack Reads { get; }

    /// <summary>
    /// The reading as the case named it, or null where it named none. WW391.
    /// <para>
    /// The one field of the format <see cref="Reads"/> cannot answer for: a step that named no
    /// reading and one that named <c>anything</c> carry the same <see cref="ReadBack"/>, and half
    /// the refusals in <see cref="Of"/> turn on which of the two it was. It travelled beside the
    /// step as a parameter until the step carried what the case wrote.
    /// </para>
    /// </summary>
    private string? NamedReading => Wrote.Trimmed("reads");

    /// <summary>
    /// Whether this step has said out loud that it means a destructive entry, which is the sentence
    /// <see cref="Subject.MeaningIt"/> is looking for. False by default, and then a step whose
    /// locator matches something the project declared destructive is refused when it runs.
    /// </summary>
    public bool MeansIt => Wrote.Truth("meansIt");

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
    public bool Moves => Wrote.Truth("moves");

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
    public string? Sweeps => Swept?.Key;

    /// <summary>
    /// Which of the three ways this step compares its set. WW275 and WW292: one choice, so the three
    /// properties above are a view of it rather than three things that could disagree.
    /// </summary>
    public Asserting.SetMatch Matching => Swept?.Means ?? Asserting.SetMatch.Exactly;

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
    public bool Answers => Wrote.Truth("answers");

    /// <summary>
    /// Whether this step says anything a run could find false. A step that expects nothing, claims no
    /// movement and covers no set produces no assertion result, which is why a case made only of these
    /// is refused by <see cref="CaseDeclaration"/> rather than run to a green it did not earn.
    /// <para>
    /// WW258. A tray step is checkable while making no <see cref="Claims"/> at all, and that is not
    /// an exception to the rule but the rule read properly: the claim is <em>this icon can be found</em>,
    /// which the search answers pass, fail or hole. The unearned-green guard is about a step that
    /// produces no assertion result, and this one always produces exactly one — so a case made of
    /// tray steps has earned whatever it reads.
    /// </para>
    /// </summary>
    public bool Checkable =>
        Tray is not null
        // WW336. A capture is checkable making no claim of its own, for the reason a tray step is:
        // the claim is the receipt — this picture is of this window, out of this process,
        // started this way, with nothing standing over it and nothing showing through it — and the
        // receipt answers pass, fail or hole on its own.
        || Verb.Captures
        // WW340. Nineteen ORs over the same fields the refusal already reads, and the twentieth was
        // always the one somebody would forget: a claim missing from that chain is a step that reads
        // as unfalsifiable, and the case carrying it is then refused for saying nothing when it said
        // something. One list, asked twice.
        || Claims.Count > 0;

    /// <summary>
    /// Every claim this step makes, in the order the format lists them. WW340.
    /// <para>
    /// What <see cref="Checkable"/> asks and what a refusal names out of, so the two cannot disagree
    /// about what a claim is — and a twelfth joins both by being declared once.
    /// </para>
    /// <para>
    /// WW351. Read off this step's own fields rather than passed in by <see cref="Of"/>, which is
    /// the half WW340 left open. That verb built the set with a hand-written line per claim over its
    /// own parameters, so a claim was a field, a schema row <em>and</em> a line in a block — and the
    /// forgotten line made the step read as unfalsifiable, which is the failure the one list was
    /// meant to end. Every spelling the block resolved is recoverable here: the fold that turned
    /// three <c>covers</c> spellings into one field left <see cref="Matching" /> behind to say which,
    /// and the same is true of the pointing and label families.
    /// </para>
    /// <para>
    /// Computed on each read rather than cached in a field. A record carries its fields through
    /// <c>with</c>, and a cache would carry the answer for the step before the change — which on
    /// <see cref="Absent" /> or <see cref="BeginsWithLabel" />, both set after construction, is a
    /// claim set describing a step that no longer exists. Fourteen comparisons and a small list is
    /// not a cost worth that.
    /// </para>
    /// </summary>
    public IReadOnlyList<Claim> Claims
    {
        get
        {
            var claiming = new List<Claim>();

            void Claiming(bool made, string wrote, string says)
            {
                if (made)
                    claiming.Add(new Claim(wrote, says));
            }

            // The field the case actually wrote, for each family whose several spellings the engine
            // has already folded into one. A refusal names what to go and delete, so it says the
            // spelling the file used and never the mode the fold produced.
            //
            // WW391. Taken off the field the case wrote rather than mapped back out of the mode: the
            // mode is derived from that field, so reading it back was the derivation run backwards
            // and a fourth spelling had to be added to both halves or the refusal named the wrong
            // key. The fallback is unreachable — neither claim below is made where nothing was
            // written — and it is the family's first spelling, which is what the map answered.
            var sweptAs = Swept?.Field ?? "covers";
            var pointedAs = Compared?.Field ?? "sameAs";

            var stringedAs = Label is not null ? "label"
                : NotLabel is not null ? "notLabel" : "beginsWithLabel";

            Claiming(Absent, "absent", "its locator matches nothing");
            Claiming(Expected is not null, "expect", $"the reading is '{Expected}'");
            Claiming(Moves, "moves", "the reading ended up different");
            Claiming(Answers, "answers", "the reading says something rather than nothing");
            Claiming(Sweeps is not null, sweptAs, $"the '{Sweeps}' set is read here");
            Claiming(Matches is not null, "matches", $"the reading matches {Matches}");
            Claiming(Discloses, "discloses", "the act put something under the locator");
            Claiming(PointsAt is not null, pointedAs, $"the reading is compared with '{PointsAt}'");
            Claiming(Never is not null, "never", $"'{Never}' is never shown while this waits");
            Claiming(Spoken, "spoken", "everything under the locator that speaks is named");
            Claiming(
                (Label ?? NotLabel ?? BeginsWithLabel) is not null,
                stringedAs,
                $"the reading is the '{Label ?? NotLabel ?? BeginsWithLabel}' string");
            Claiming(
                ExpectReported is not null,
                "expectReported",
                $"the reading is the value reported under '{ExpectReported}'");
            Claiming(EachSpoken, "eachSpoken", "every element it matches announces a name");
            Claiming(OwnHeader, "ownHeader", "each row's controls announce that row");

            return claiming.AsReadOnly();
        }
    }

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
    /// Declare one out of what a case wrote, refusing every field that is wrong about the file
    /// rather than about the desk.
    /// <para>
    /// WW391. This took twenty-nine parameters, one per field of the format, and a field added
    /// reached it by hand — a parameter, an argument at the loader's call site, and a line in the
    /// construction beneath, none of which said anything the schema row had not already said. What
    /// arrives now is what the loader already had: the fields by their own names, with
    /// <see cref="ScenarioSchema.Step"/> saying what those names are. A field joins the format in a
    /// schema row and a property, and the two are held to each other by the read between them.
    /// </para>
    /// <para>
    /// The locals this verb still keeps are the three things a case writes and a step does not
    /// hold: the parsed locator, the compiled pattern, and the verb and reading resolved against
    /// their closed vocabularies. Each of those is a refusal that has to fire here — a locator that
    /// does not parse and an act nobody recognises are wrong on every machine — so they are computed
    /// once at the door rather than on each read.
    /// </para>
    /// </summary>
    /// <param name="wrote">The fields, by the names the format spells them.</param>
    /// <exception cref="ScenarioRefusedException">Where any field could not run on any machine.</exception>
    public static StepDeclaration Of(Written wrote)
    {
        ArgumentNullException.ThrowIfNull(wrote);

        RefusesTwoSetClaims(wrote);

        var called = wrote.Trimmed("named");
        var tray = wrote.Trimmed("tray");
        var locator = wrote.Text("locator");
        var verb = wrote.Text("act");
        var subject = called ?? Describing(verb, locator ?? $"tray icon '{tray}'");

        RefusesTwoSubjects(subject, locator, tray);

        // Not null past the guard above, which refuses a step that named nothing to act on — and
        // blank is refused by the verb itself, naming the vocabulary there is.
        var act = ActVerb.Named(verb!);

        // WW391. The tray's own refusal about the verb, kept ahead of everything else a case wrote:
        // an icon that named a pressing act is refused for that and not for whichever claim the
        // reader would then delete. It stood at the top of a verb of its own until this task, and
        // the order is what this suite asserts rather than where the code sat.
        RefusesTrayVerb(subject, tray, act);

        // Null for a tray step, which has no locator to parse, and never null for the other kind:
        // the guard above threw for a step that named neither. Parsed before the argument is judged,
        // which is the order a step with two faults is refused in.
        var parsed = tray is null ? Parsed(subject, locator!) : null;

        if (act.Refuses(wrote.Text("with")) is { } wrong)
            throw new ScenarioRefusedException(subject, wrong);

        var reading = ReadBack.Named(wrote.Text("reads"));

        // WW250. Compiled at the door for the timeout's sake, and because a pattern that does not
        // parse is wrong on every machine — so the step carries the compiled one rather than
        // recompiling a regular expression on every read of it.
        var pattern = wrote.Trimmed("matches") is { } written ? Compiled(subject, written) : null;

        RefusesTwoComparisons(subject, wrote);

        // The name a report shows: what the case called it, or the verb and what it acts on. A tray
        // step has no locator to describe, and `subject` is what that case's own name was built from
        // at the top of this verb — so it is what stands in.
        //
        // WW323, WW340, WW351. Built before the refusals below rather than after them, which is what
        // keeps their order: every one of them was written against the claim set that already existed
        // at this point, and this suite asserts which refusal wins where a step is wrong twice over.
        var step = new StepDeclaration(
            called ?? (parsed is null ? subject : Describing(act.Name, parsed.Text)), act, reading, wrote)
        {
            Locator = parsed,
            Matches = pattern,
        };

        // WW391. A tray step is finished here, and what finishes it is the same step every other
        // refusal reads. It used to be a verb of its own carrying twenty-five parameters and a
        // hand-written list of twenty-one field names — the second copy of exactly what WW351 made
        // this step able to answer for itself, and the copy that would have gone stale the next time
        // a claim was added.
        //
        // What comes back is this step and not a smaller one built beside it. Every claim field is
        // refused above, so a tray step that survives carries a subject, a verb and an icon — which
        // is what WW352 said a tray step is, reached now by having nothing else rather than by
        // being constructed without it.
        if (tray is not null)
        {
            RefusesTrayClaims(step, subject);
            return step;
        }

        // WW365. Every refusal from here reads the step rather than the locals that built it, which
        // is what lets each family be a method instead of six hundred lines in one verb. They ran in
        // this order before they moved and they run in it now: this suite asserts which refusal wins
        // where a step is wrong twice over, so the sequence is a fact about the format rather than
        // an accident of how the verb grew.
        //
        // WW391. `reads` used to travel beside the step because it was the one thing the step could
        // not say — a step that named no reading and one that named the default carry the same
        // ReadBack, and half of these turn on which of the two it was. The step carries what the
        // case wrote now, so it says that itself and the parameter is gone.
        RefusesClaimBesideAbsence(step, subject);
        RefusesCapturingClaim(step, subject);
        RefusesPopupBesideAnythingElse(step, subject);
        RefusesClaimCount(step, subject);
        RefusesClaimAgainstVerb(step, subject);
        RefusesTwoPointings(step, subject);
        RefusesPointingWithoutReading(step, subject);
        RefusesTwoStringClaims(step, subject);
        RefusesReadingBesideClaim(step, subject);
        RefusesPinnedReading(step, subject);
        RefusesSweepingStep(step, subject);

        // WW351. The step every refusal above was asked about, handed back. It was built before them
        // rather than after, because the set they read is its own — and nothing between here and
        // there changes a field, so what is returned is what was judged.
        return step;
    }

    /// <summary>
    /// One set claimed one way. WW275, WW292, WW365 moved it here, and WW391 left it the refusal
    /// alone: which of the three the case wrote is <see cref="Swept"/>'s answer now, off the same
    /// fields, so the fold and the rule are no longer two readings of one list.
    /// </summary>
    /// <param name="wrote">The fields the case wrote.</param>
    private static void RefusesTwoSetClaims(Written wrote)
    {
        // WW275 and WW292. At most one of the three, and refused where they are written: they are one
        // set claimed three different ways, and a step naming two would have the run honour whichever
        // the code reads first.
        var ways = Sweepings
            .Where(one => wrote.Trimmed(one.Field) is not null)
            .Select(one => $"'{one.Field}'")
            .ToList();

        if (ways.Count > 1)
        {
            throw new ScenarioRefusedException(
                wrote.Trimmed("named") ?? "<a step>",
                $"it carries {string.Join(" and ", ways)}; those are the same set claimed different "
                    + "ways, so name the one this step means");
        }
    }

    /// <summary>
    /// A step acts on exactly one thing. WW258, and WW365 moved it here.
    /// <para>
    /// Both arms, and here as well as in the schema: the loader comes through
    /// <see cref="ScenarioSchema.Miscarried"/>, and a caller building a declaration in code does not
    /// — so the rule that a step addresses exactly one thing lives on the type that cannot be
    /// bypassed.
    /// </para>
    /// </summary>
    /// <param name="subject">What a refusal calls this step.</param>
    /// <param name="locator">The locator, where the step named one.</param>
    /// <param name="tray">The notification-area icon, where the step named one.</param>
    private static void RefusesTwoSubjects(string subject, string? locator, string? tray)
    {
        if (string.IsNullOrWhiteSpace(locator) && tray is null)
            throw new ScenarioRefusedException(subject, "a step acts on something, and this one names nothing");

        if (!string.IsNullOrWhiteSpace(locator) && tray is not null)
        {
            throw new ScenarioRefusedException(
                subject,
                "a step acts on one thing, and this one names a locator and a tray icon; a tray icon is "
                    + "not in the window's tree, so no locator reaches it and naming both names two subjects");
        }
    }

    /// <summary>
    /// The locator, parsed where the case wrote it. WW263, WW273, and WW365 moved it here.
    /// </summary>
    /// <param name="subject">What a refusal calls this step.</param>
    /// <param name="locator">The locator as the case wrote it, braces and all.</param>
    private static Locator Parsed(string subject, string locator)
    {
        // WW263 and WW273. A locator with a brace in it has to parse with something in it as well as
        // with the placeholder, and both are facts about the file rather than about a run. Probed here
        // so the refusal arrives where the locator was written, not on the member — or the string —
        // that happened to expose it.
        if (Braced.IsMatch(locator)
            && !Locator.TryParse(Braced.Replace(locator, "probe"), out _, out var wrongly))
        {
            throw new ScenarioRefusedException(
                subject,
                $"it is built out of something the run substitutes and does not parse with one in it: {wrongly}");
        }

        // Parsed here rather than at run time on purpose: a locator that does not parse is wrong on
        // every machine, and the reader of a red about one is opening the wrong repository.
        if (!Locator.TryParse(locator, out var parsed, out var because))
            throw new ScenarioRefusedException(subject, $"its locator does not parse — {because}");

        return parsed;
    }

    /// <summary>
    /// One comparison per step, refused before the four spellings are read as one. WW323, WW326,
    /// WW365 moved it here, and WW391 has it ask what the case wrote rather than four locals.
    /// </summary>
    /// <param name="subject">What a refusal calls this step.</param>
    /// <param name="wrote">The fields the case wrote.</param>
    private static void RefusesTwoComparisons(string subject, Written wrote)
    {
        var back = wrote.Trimmed("sameAs");
        var apart = wrote.Trimmed("unlike");
        var ticking = wrote.Trimmed("sameCountdownAs");
        var holding = wrote.Trimmed("contains");

        // WW323. `expectReported` beside `expect` was refused here and beside nothing else, which is
        // the hole that task was filed for. Both are now claims in one set below, so this pair is
        // refused by the same rule as every other pair rather than by a line of its own.
        if (holding is not null && (back ?? apart ?? ticking) is { } alsoHolding)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it claims its reading holds what '{holding}' read and also compares it with "
                    + $"'{alsoHolding}'; a step answers one thing, and these are two");
        }

        if (ticking is not null && (back ?? apart) is { } alsoPointing)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it claims its reading is back to '{ticking}' give or take a tick and also compares it "
                    + $"with '{alsoPointing}'; a step answers one thing, and these are two");
        }
    }

    /// <summary>
    /// A capture claims its receipt and nothing beside it. WW336, and WW365 moved it here.
    /// </summary>
    /// <param name="step">The step, built and asked rather than rebuilt out of locals.</param>
    /// <param name="subject">What a refusal calls this step.</param>
    private static void RefusesCapturingClaim(StepDeclaration step, string subject)
    {
        if (!step.Verb.Captures)
            return;

        var reads = step.NamedReading;

        // WW336. A capture's claim is the receipt, and it is the whole of it. Every field in the
        // claim set is a reading of an element the locator matched; a capture is about the window
        // that element is in, and the two are answered at different moments by different machinery —
        // so a step carrying both would owe two results and a trace line standing for two things.
        if (step.Claims is [var claimed, ..])
        {
            throw new ScenarioRefusedException(
                subject,
                $"it captures and also claims {claimed}; a capture's claim is the picture — "
                    + "that it is of this window, out of this process and with nothing showing "
                    + "through it — and a reading of an element is a second thing to check");
        }

        if (reads is not null)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it captures and names the '{reads}' reading; a capture is about the "
                    + "window the locator is inside rather than about what that element says");
        }
    }

    /// <summary>
    /// A claim about nothing is the whole of what a step can claim. WW318, and WW378 moved it here.
    /// <para>
    /// The sharpest case of the one-claim rule: every other claim reads a subject and this one says
    /// there is none, so a second beside it would be a reading of the element the step is asserting
    /// is not there.
    /// </para>
    /// <para>
    /// WW365 left this family where it was, because it ran before the step existed and could only
    /// read the parameters. What it read was a seventeen-term chain — one term per claim the format
    /// had on the day it was written — and it was already a claim behind: <c>contains</c> joined in
    /// WW326 and never joined the chain, so a step claiming absence beside it was told it made two
    /// claims rather than that a claim about nothing is a claim about something.
    /// </para>
    /// <para>
    /// That is WW323's shape, which WW340 and WW351 each closed once, and this was the last
    /// hand-written copy of the set. Asking <see cref="Claims"/> costs nothing and cannot fall
    /// behind: a claim joins by being a field. Nothing between the old position and this one throws,
    /// so every refusal still fires in the order it always did.
    /// </para>
    /// </summary>
    /// <param name="step">The step, built and asked rather than rebuilt out of locals.</param>
    /// <param name="subject">What a refusal calls this step.</param>
    private static void RefusesClaimBesideAbsence(StepDeclaration step, string subject)
    {
        if (!step.Absent)
            return;

        // Absence is itself one of them, so more than one is absence and something else.
        if (step.Claims.Count > 1)
        {
            throw new ScenarioRefusedException(
                subject,
                "it claims its locator matches nothing and also makes a claim about what it "
                    + "matched; there is no reading of an element that is not there");
        }

        if (step.NamedReading is { } named)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it claims its locator matches nothing and names the '{named}' reading; "
                    + "an element that is not there answers no reading, and naming one would "
                    + "look like it narrowed the claim");
        }

        // An act on a thing the step says is not there is a step that cannot run: the act
        // resolves the subject first, so it would fail on the absence this is asserting.
        if (!step.Verb.Reads)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it claims its locator matches nothing and acts with '{step.Verb.Name}'; "
                    + "an act resolves what it acts on, so this would fail on the very absence "
                    + "it is asserting");
        }
    }

    /// <summary>
    /// A popup is a capture's field and no other verb's. WW372.
    /// <para>
    /// Every other act reaches an element a locator matched; a popup nobody has clicked has no
    /// element in this run's reach, which is the whole reason the picture is asked for rather than
    /// taken. So a step naming one under any other verb is a case that would load and mean nothing
    /// — the key that loads and does nothing being the failure this format is built to refuse.
    /// </para>
    /// </summary>
    /// <param name="step">The step, built and asked rather than rebuilt out of locals.</param>
    /// <param name="subject">What a refusal calls this step.</param>
    private static void RefusesPopupBesideAnythingElse(StepDeclaration step, string subject)
    {
        if (step.Popup is not { } popup || step.Verb.Captures)
            return;

        throw new ScenarioRefusedException(
            subject,
            $"it names the '{popup}' popup and acts with '{step.Verb.Name}'; a popup is the surface a "
                + "capture asks the application to draw, and every other act reaches an element the "
                + "locator matched — which a popup nobody has opened is not");
    }

    /// <summary>
    /// One claim, and at least one. WW213, WW254, WW323, and WW365 moved them here.
    /// </summary>
    /// <param name="step">The step, built and asked rather than rebuilt out of locals.</param>
    /// <param name="subject">What a refusal calls this step.</param>
    private static void RefusesClaimCount(StepDeclaration step, string subject)
    {
        var claiming = step.Claims;
        var claims = claiming.Count > 0;

        // Both idioms the older refusals used, because they are what a reader recognises and what
        // this suite matches on — and because they are both true of the finding: the step made
        // another claim, and a step answers one thing.
        if (claiming.Count > 1)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it claims {claiming[0]} and also makes another claim, {string.Join(", ", claiming.Skip(1))}; "
                    + $"a step answers one thing and this one makes {claiming.Count} — name the one it "
                    + "means and delete the rest");
        }

        if (!claims && step.NamedReading is not null)
        {
            throw new ScenarioRefusedException(
                subject, $"it reads '{step.Reads.Name}' and expects nothing of it, so the reading changes nothing");
        }

        // WW213. An act with no expectation is a navigation a later step is the check for. A read
        // with no expectation is nothing at all: it touches nothing and claims nothing, so a case
        // carrying one is a case with a step in it that could not fail.
        if (!claims && step.Verb.Reads)
        {
            throw new ScenarioRefusedException(
                subject, $"'{step.Verb.Name}' expects nothing, so the step does nothing at all");
        }

        // WW254. The one act whose landing the engine can see. It was handed a value by name and can
        // read what the picker settled on, so a step that walks a picker and claims nothing has thrown
        // that answer away — and every step after it is then read against whichever value the walk
        // happened to stop at. That is WW244's failure with the act delivered rather than dropped, and
        // the migration this verb exists for made exactly this claim in the script: the picker walked
        // one label to another and back, checked at each stop.
        if (step.Verb.Reaches && !claims)
        {
            throw new ScenarioRefusedException(
                subject,
                $"'{step.Verb.Name}' is told what to reach and claims nothing of what it reached; name the "
                    + "value in 'expect', because a walk that stopped somewhere else is every step "
                    + "after this one reading the wrong thing");
        }
    }

    /// <summary>
    /// The claims a verb or a reading cannot carry. WW237, WW238, and WW365 moved them here.
    /// </summary>
    /// <param name="step">The step, built and asked rather than rebuilt out of locals.</param>
    /// <param name="subject">What a refusal calls this step.</param>
    private static void RefusesClaimAgainstVerb(StepDeclaration step, string subject)
    {
        // A read moves nothing by construction, so a read claiming movement is a claim about whatever
        // else is happening on the desk rather than about this step.
        if (step.Moves && step.Verb.Reads)
        {
            throw new ScenarioRefusedException(
                subject, $"'{step.Verb.Name}' reads and never acts, so it cannot be what moved a reading");
        }

        // The reading has to be able to answer nothing, or the claim cannot be false. 'focused' says
        // 'not focused' for every element that resolved, so a step claiming it answers is a step that
        // holds whenever the locator matched — which is existence wearing the words of a reading.
        if (step.Answers && step.Reads.Always)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it claims '{step.Reads.Name}' answers, and that reading answers for every element that "
                    + "resolved at all; the claim could never be false, so it says nothing");
        }

        // The unearned green this field is easiest to write. A pattern that matches the empty string
        // matches every answer there is, so the step holds wherever the reading answered at all —
        // which is what 'answers' says, in a field that reads as though it checked more. The same
        // shape WW237 and WW238 each closed once.
        //
        // Deliberately no rule about an always-answering reading beside it, unlike 'answers'. A
        // pattern over 'focused' picks one of its two states, which is a claim that can be false —
        // the problem 'answers' has with that reading is that it asks only whether there was an
        // answer, and this asks which.
        if (step.Matches is { } pattern && pattern.IsMatch(""))
        {
            throw new ScenarioRefusedException(
                subject,
                $"'{pattern}' matches the empty string, so it holds for every answer there is; "
                    + "say 'answers' if that is the claim");
        }

        if (!step.Discloses)
            return;

        // A read discloses nothing. The claim is that an act put something there, so a step whose
        // verb only looks would be asserting that a window changed while nobody touched it — which
        // is either a race or a lie, and green either way.
        if (step.Verb.Reads)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it claims a disclosure and '{step.Verb.Name}' only reads, so nothing it does could "
                    + "have disclosed anything");
        }

        // And no reading beside it, because the subject is the subtree. A 'reads' here would look
        // like it narrowed the claim and would narrow nothing.
        if (step.NamedReading is { } named)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it claims a disclosure and names the '{named}' reading; a disclosure is "
                    + "about what is under the locator and not about what it says");
        }
    }

    /// <summary>
    /// Back to one step and different from another are two claims. WW268, and WW391 made it a rule
    /// like the ones around it.
    /// <para>
    /// It was the one refusal in this verb that could not ask the step: <see cref="Compared"/> reads
    /// the first spelling a case wrote, so by the time a step existed, one that wrote both looked
    /// like one that wrote <c>sameAs</c> — and the fold is right to do that, because every step that
    /// survives wrote one. What the step carries now is what the case wrote, so the pair is
    /// answerable off it and this rule reads the step like its neighbours.
    /// </para>
    /// <para>
    /// Its own rule rather than a clause of <see cref="RefusesTwoComparisons"/>, which runs before
    /// the step is built. The order is what this suite asserts: a step that writes both and also
    /// claims something else is refused for the something else first, which is where it stood as a
    /// block of this verb.
    /// </para>
    /// </summary>
    /// <param name="step">The step, built and asked rather than rebuilt out of locals.</param>
    /// <param name="subject">What a refusal calls this step.</param>
    private static void RefusesTwoPointings(StepDeclaration step, string subject)
    {
        if (step.Wrote.Trimmed("sameAs") is { } back && step.Wrote.Trimmed("unlike") is { } apart)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it claims its reading is back to '{back}' and also that it differs from '{apart}'; "
                    + "a step answers one thing, and these are two");
        }
    }

    /// <summary>
    /// A comparison against an earlier step, and what it needs said. WW365 moved it here.
    /// <para>
    /// The field the case wrote is the field the case wrote: <see cref="Compared"/> hands back the
    /// spelling it read, so nothing here maps a mode back to a name. A refusal names what to go and
    /// delete, and telling a file that said <c>sameAs</c> to delete <c>unlike</c> sends its author
    /// looking for a key their step does not carry.
    /// </para>
    /// </summary>
    /// <param name="step">The step, built and asked rather than rebuilt out of locals.</param>
    /// <param name="subject">What a refusal calls this step.</param>
    private static void RefusesPointingWithoutReading(StepDeclaration step, string subject)
    {
        if (step.Compared is not { } comparison)
            return;

        var (field, pointed, _) = comparison;

        var claim = step.Pointing switch
        {
            Pointing.Unlike => "differs from",
            Pointing.Contains => "holds what",
            _ => "is back to",
        };

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
        if (step.NamedReading is null)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it claims its reading {claim} '{pointed}' and does not say which reading; "
                    + $"'{field}' compares two of them, so the default would compare whichever "
                    + "answered first");
        }
    }

    /// <summary>
    /// One declared string claimed one way. WW83, and WW365 moved it here.
    /// </summary>
    /// <param name="step">The step, built and asked rather than rebuilt out of locals.</param>
    /// <param name="subject">What a refusal calls this step.</param>
    private static void RefusesTwoStringClaims(StepDeclaration step, string subject)
    {
        // WW83. Three ways of claiming one declared string — is it, is it not, does it begin with it —
        // and at most one of them, named one by one so the refusal says which field to delete.
        var strings = new List<string>();
        if (step.Label is not null)
            strings.Add("'label'");
        if (step.NotLabel is not null)
            strings.Add("'notLabel'");
        if (step.BeginsWithLabel is not null)
            strings.Add("'beginsWithLabel'");

        if (strings.Count > 1)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it carries {string.Join(" and ", strings)}; those are one declared string claimed "
                    + "different ways, so name the one this step means");
        }
    }

    /// <summary>
    /// The four claims that take no reading beside them, and the two that take no act. WW365 moved
    /// them here, and they stay in one method because they stay in one order.
    /// </summary>
    /// <param name="step">The step, built and asked rather than rebuilt out of locals.</param>
    /// <param name="subject">What a refusal calls this step.</param>
    private static void RefusesReadingBesideClaim(StepDeclaration step, string subject)
    {
        // The reading as the case spelled it, which is both the question these four ask and the word
        // each of them quotes back.
        var named = step.NamedReading;

        if (step.OwnHeader)
        {
            // The same rule a sweep is under: this reads every row the locator matches and everything
            // inside them, and one act over all of that is not a claim about any of it.
            if (!step.Verb.Reads)
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it claims each row's controls announce that row and acts with '{step.Verb.Name}'; the "
                        + "pairing reads every row its locator matches, and one act over many of them "
                        + "is not a claim");
            }

            if (named is not null)
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it claims each row's controls announce that row and names the '{named}' "
                        + "reading; the claim is about what those controls announce, which is their name");
            }
        }

        if (step.EachSpoken)
        {
            // The same rule `covers` is under, and for its reason: a sweep reads every element its
            // locator matches, and one act over many of them is not a claim about any of them.
            if (!step.Verb.Reads)
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it claims every element it matches is named and acts with '{step.Verb.Name}'; a sweep "
                        + "reads every element its locator matches, and one act over many of them is "
                        + "not a claim");
            }

            // And no reading beside it, for the reason a disclosure takes none: what these elements
            // announce is their name, always, and a 'reads' here would narrow nothing.
            if (named is not null)
            {
                throw new ScenarioRefusedException(
                    subject,
                    $"it claims every element it matches is named and names the '{named}' "
                        + "reading; the claim is about what those elements announce, which is their name");
            }
        }

        // And no reading beside it, for the reason a disclosure takes none: the subject is the
        // subtree, and a 'reads' here would look like it narrowed the claim and would narrow
        // nothing. What the elements under it announce is their name, always.
        if (step.Spoken && named is not null)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it claims what is under the locator is named and names the '{named}' reading; "
                    + "the claim is about what those elements announce, which is their name");
        }

        // A reading beside it narrows nothing. The claim is about the window, not about this
        // element: the string may show anywhere, and the locator is what says when to stop
        // looking rather than what to look at.
        if (step.Never is { } forbidden && named is not null)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it claims '{forbidden}' never shows and names the '{named}' reading; the "
                    + "claim is about the window while this step waited, and the locator is what "
                    + "says when the waiting is over");
        }
    }

    /// <summary>
    /// A reading the locator already selected by. WW238, and WW365 moved it here.
    /// <para>
    /// The other half of the same rule: a reading the locator already selected by is fixed before
    /// the act runs, so a step reading it asserts what chose the element. Refused whatever the claim
    /// is — 'expect' repeats the locator, 'answers' holds because the locator matched, and both are
    /// the step passing on its own selection.
    /// </para>
    /// <para>
    /// Naming the element some other way and reading its name is the useful shape, so the sentence
    /// says which locator field to move rather than that the reading is wrong.
    /// </para>
    /// </summary>
    /// <param name="step">The step, built and asked rather than rebuilt out of locals.</param>
    /// <param name="subject">What a refusal calls this step.</param>
    private static void RefusesPinnedReading(StepDeclaration step, string subject)
    {
        if (step.Locator is not { } located
            || step.Reads.PinnedBy(located.Steps[^1]) is not { } already
            || (step.Expected is null && !step.Moves && !step.Answers))
        {
            return;
        }

        throw new ScenarioRefusedException(
            subject,
            $"it reads '{step.Reads.Name}' and its locator already matched on that — '{already}' — so the "
                + "reading is fixed before the act runs; select the element another way to claim "
                + "anything about it");
    }

    /// <summary>
    /// A sweep is one claim over many elements, and every other field on a step is about one. WW365
    /// moved it here.
    /// </summary>
    /// <param name="step">The step, built and asked rather than rebuilt out of locals.</param>
    /// <param name="subject">What a refusal calls this step.</param>
    private static void RefusesSweepingStep(StepDeclaration step, string subject)
    {
        if (step.Sweeps is not { } sweeping)
            return;

        if (!step.Verb.Reads)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it covers '{sweeping}' and acts with '{step.Verb.Name}'; a sweep reads every element its "
                    + "locator matches, and one act over many of them is not a claim");
        }

        if (step.NamedReading is not null)
        {
            throw new ScenarioRefusedException(
                subject,
                $"it covers '{sweeping}' and reads '{step.Reads.Name}'; a sweep compares the names the "
                    + "locator matched against the strings, and a pattern reading is not one of them");
        }
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
    /// <param name="subject">What a refusal calls this step.</param>
    /// <param name="tray">The icon it is about, or null where it is about a locator.</param>
    /// <param name="act">The verb it named.</param>
    private static void RefusesTrayVerb(string subject, string? tray, ActVerb act)
    {
        if (tray is null || act.OnATray)
            return;

        throw new ScenarioRefusedException(
            subject,
            $"it is about a tray icon and names '{act.Name}'; an icon is not an element, so the acts "
                + $"that ask a control through its patterns do not apply — it takes "
                + $"{string.Join(" or ", ActVerb.All.Where(one => one.OnATray).Select(one => $"'{one.Name}'"))}");
    }

    /// <summary>
    /// Every claim a tray step may not make, refused by name. WW258, read off the step since WW391.
    /// <para>
    /// The list used to be written here — twenty-one field names in a fixed order, added to by hand
    /// whenever a claim was — which is the second copy of the thing <see cref="Claims" /> exists to
    /// be. A claim added to the format and not to that list would have loaded on a tray step and
    /// done nothing, which is this format's founding failure pointed at its own oldest guard.
    /// </para>
    /// <para>
    /// Two fields are named beside it and neither is a claim. <c>reads</c> is here because a tray
    /// icon answers no reading at all, and <see cref="NamedReading" /> is what tells a step that
    /// named one from a step that named none — the <see cref="ReadBack" /> cannot. And
    /// <c>meansIt</c> is a permission rather than a claim — it says a destructive entry was meant,
    /// which an icon has no way to be.
    /// </para>
    /// <para>
    /// What the refusal names is the field the case wrote, which is what the claim set already
    /// carries: a step writing <c>coversWithin</c> is told to delete that and not <c>covers</c>.
    /// </para>
    /// </summary>
    /// <param name="step">The step as the case wrote it.</param>
    /// <param name="subject">What a refusal calls it.</param>
    private static void RefusesTrayClaims(StepDeclaration step, string subject)
    {
        var claimed = step.Claims.Select(one => $"'{one.Field}'").ToList();

        if (step.NamedReading is not null)
            claimed.Add("'reads'");

        if (step.MeansIt)
            claimed.Add("'meansIt'");

        if (claimed.Count == 0)
            return;

        throw new ScenarioRefusedException(
            subject,
            $"it is about a tray icon and carries {string.Join(", ", claimed)}; those are claims about "
                + "a reading taken through a control's patterns, and an icon has none — it is a "
                + "rectangle and a tooltip, so the claim a tray step makes is that it can be found");
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
            Locator = parsed,
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

        return this with { Locator = parsed };
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

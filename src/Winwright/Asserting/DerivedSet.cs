using System.Collections.ObjectModel;
using System.Text.Json;

using Winwright.Projects;
using Winwright.Tracing;
using Winwright.Verdicts;

namespace Winwright.Asserting;

/// <summary>Raised where a set cannot be derived, so nothing is asserted against a guess.</summary>
public sealed class UnderivableSetException : InvalidOperationException
{
    /// <summary>Say what could not be derived and from where.</summary>
    public UnderivableSetException(string message)
        : base(message)
    {
    }

    /// <summary>Unused. Present because an exception with no default shapes is awkward to catch.</summary>
    public UnderivableSetException()
        : base("the expected set could not be derived from the project's own strings")
    {
    }

    /// <summary>Unused. Present for the same reason.</summary>
    public UnderivableSetException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>Why a string under the key is not a member of the set derived from it.</summary>
public enum LeftOutBecause
{
    /// <summary>It carries a placeholder, so no exact read could ever match it.</summary>
    CarriesAPlaceholder,

    /// <summary>
    /// It is a note rather than a string. JSON has no comments, so a strings file that wants one
    /// writes a key nobody reads — and the derivation took it as something a window should show.
    /// </summary>
    IsANote,
}

/// <summary>A string the strings declare that the derived set does not take.</summary>
/// <param name="Key">The key it sits under.</param>
/// <param name="Value">What it says, placeholder and all.</param>
/// <param name="Where">The file and line it is declared on.</param>
/// <param name="Why">Which of the two reasons it was left out for.</param>
public sealed record LeftOut(string Key, string Value, Provenance Where, LeftOutBecause Why)
{
    /// <summary>The one line a source or a refusal names it by.</summary>
    public override string ToString() =>
        Where.Known ? $"'{Key}' = '{Value}' ({Where})" : $"'{Key}' = '{Value}'";
}

/// <summary>
/// How a derived set is compared with what was read.
/// <para>
/// WW292. Three and not a pair of booleans, because they are one choice: a step names the claim it
/// means, and a reader of the sentence is told which of the three it was rather than working it out
/// from two flags.
/// </para>
/// </summary>
public enum SetMatch
{
    /// <summary>
    /// Every declared value was read, and nothing else was. What `covers` has always meant: the tab
    /// set it was built for is the whole of what a `TabItem` locator matches, so one more tab than the
    /// expectation had heard of is the defect it exists to catch.
    /// </summary>
    Exactly,

    /// <summary>
    /// Every declared value was read, and a value the set does not declare is allowed. WW275, for the
    /// container no locator separates from its neighbours.
    /// </summary>
    AtLeast,

    /// <summary>
    /// Every declared value appears <em>inside</em> the name of something that was read, and a name
    /// holding none of them is allowed.
    /// <para>
    /// WW292. For the reading that decorates what it is about: a menu entry for the profile `Pessoal`
    /// renders as <c>Pessoal  active now</c>, or carries `pinned` or `sign-in needed`, so equality is
    /// false of every entry and both claims above are unwritable. One-way only — a submenu also
    /// carries toggles that are about no profile at all, and demanding every name hold a declared
    /// value would fail on the two entries the script counted separately.
    /// </para>
    /// </summary>
    Within,
}

/// <summary>What a set derived from the project's strings turned out to be, compared with what was read.</summary>
/// <param name="Set">The set that was expected, and where it came from.</param>
/// <param name="Matched">Values that were expected and read, in the set's own order.</param>
/// <param name="Missing">Values the strings declare that nothing read.</param>
/// <param name="Unexpected">Values that were read and the strings do not declare.</param>
/// <param name="Match">Which of the three claims this comparison is. WW275 and WW292.</param>
public sealed record SetComparison(
    DerivedSet Set,
    IReadOnlyList<string> Matched,
    IReadOnlyList<string> Missing,
    IReadOnlyList<string> Unexpected,
    SetMatch Match = SetMatch.Exactly)
{
    /// <summary>
    /// Whether everything the set declares was accounted for — and, where the claim is the exact one,
    /// that nothing else was read.
    /// </summary>
    public bool Held => Missing.Count == 0 && (Match != SetMatch.Exactly || Unexpected.Count == 0);

    /// <summary>
    /// What was expected, what was read, and which way they differ — never the word <em>all</em>
    /// while anything is missing, since that is the sentence claude-tray printed against a window
    /// carrying one more tab than the expectation had ever heard of.
    /// </summary>
    public string Sentence()
    {
        if (Held)
        {
            // WW275. What was passed over is said on the pass, not left out of it: a one-way claim
            // that held over nine strangers held over nine strangers, and a reader who is not told
            // cannot tell this from the exact claim holding.
            var passed = Match == SetMatch.Exactly || Unexpected.Count == 0
                ? ""
                : $" {Unexpected.Count} other value(s) were read here and are declared nowhere, which "
                    + "this claim allows";

            // WW292. Said, because a reader who is told "all four were read" of a set matched inside
            // decorated names would take it for equality — and the two claims are not the same
            // evidence about the application.
            var how = Match == SetMatch.Within ? " inside what was read" : "";

            return $"{Set.Named}: all {Matched.Count} of {Listed(Matched)} were read{how}, {Set.Source}.{passed}";
        }

        var parts = new List<string>();

        // The missing ones carry their line and the unexpected ones cannot: a value the strings
        // declare has somewhere to be looked at, and one that was only ever read has nowhere.
        //
        // WW275. Both verbs agree with the count, and both used to be written for the singular: the
        // sentence read "'a', 'b', 'c' were read and is declared nowhere".
        if (Missing.Count > 0)
        {
            // WW292. "was not read" is false of a containment claim — the value may be nowhere, or it
            // may be somewhere no matched name holds it, and the reader's next move differs.
            var absent = Match == SetMatch.Within ? "is in nothing that was read" : "was not read";
            var absents = Match == SetMatch.Within ? "are in nothing that was read" : "were not read";

            parts.Add(Missing.Count == 1
                ? $"{Traced(Missing)} is declared and {absent}"
                : $"{Traced(Missing)} are declared and {absents}");
        }

        if (Unexpected.Count > 0)
        {
            parts.Add(Unexpected.Count == 1
                ? $"{Listed(Unexpected)} was read and is declared nowhere"
                : $"{Listed(Unexpected)} were read and are declared nowhere");
        }

        return $"{Set.Named}: {string.Join("; ", parts)} — {Matched.Count} of {Set.Expected.Count} matched, {Set.Source}.";
    }

    /// <summary>The result a verdict counts, carrying this sentence as its detail.</summary>
    public AssertionResult AsAssertion() =>
        Held ? AssertionResult.Pass(Set.Named, Sentence()) : AssertionResult.Fail(Set.Named, Sentence());

    /// <summary>
    /// The step a trace records. WW163: a set derived from the project's own strings is a reading a
    /// run took, and a verdict is the conclusion of readings — one that can answer only the
    /// conclusion leaves a reader with nothing to reach the observation by.
    /// </summary>
    public TraceStep AsTraceStep() => new()
    {
        Verb = "derive",
        Locator = Set.Named,
        Resolved = Set.Source,
        ReadBack = $"{Matched.Count} matched, {Missing.Count} missing, {Unexpected.Count} unexpected",
        From = Set.Origin,
        Verdict = Held ? StepVerdict.Ok : StepVerdict.Failed,
        Detail = Held ? null : Sentence(),
    };

    private static string Listed(IReadOnlyList<string> values) => string.Join(", ", values.Select(value => $"'{value}'"));

    /// <summary>The same, each value followed by the file and line the strings declare it on.</summary>
    private string Traced(IReadOnlyList<string> values) => string.Join(
        ", ",
        values.Select(value =>
        {
            var from = Set.Whence(value);
            return from.Known ? $"'{value}' ({from})" : $"'{value}'";
        }));
}

/// <summary>
/// An expected set read out of the project's own strings.
/// <para>
/// A hardcoded expected set silently stops covering the thing it was written for. claude-tray's
/// panes case named three tab headers by hand and the window had carried four for some time, so it
/// reported all three tab headers read against a four-tab window and the newest pane had never
/// been asked whether it was in the tree at all.
/// </para>
/// <para>
/// The set is derived from the strings and <em>not</em> from the tree, and that is the whole
/// design rather than a convenience: the tree is what is being asserted, so an expectation read
/// out of it agrees with whatever is there and could never notice a header that had gone missing.
/// There is deliberately no way to build one of these from what was read — the only source is a
/// file, and <see cref="Against"/> is the only door readings come in by.
/// </para>
/// </summary>
public sealed record DerivedSet
{
    private DerivedSet(
        string named,
        string source,
        IReadOnlyList<string> keys,
        IReadOnlyList<string> expected,
        Provenance origin,
        IReadOnlyList<Provenance> origins,
        IReadOnlyList<LeftOut> excluded)
    {
        Named = named;
        Source = source;
        Keys = keys;
        Expected = expected;
        Origin = origin;
        Origins = origins;
        Excluded = excluded;
    }

    /// <summary>What this is a set of, as the scenario names it.</summary>
    public string Named { get; }

    /// <summary>Where it was derived from, as a sentence prints it. Named apart from the factory
    /// above so the file a set came from and the door it came through never read as one thing.</summary>
    public string Source { get; }

    /// <summary>The keys it came from, in the order the file spells them.</summary>
    public IReadOnlyList<string> Keys { get; }

    /// <summary>The values, in the same order.</summary>
    public IReadOnlyList<string> Expected { get; }

    /// <summary>
    /// The file and line the whole set was derived from, as a field rather than as prose. It is
    /// what lets a reader check that the expectation came from anywhere at all without opening the
    /// strings file to find out.
    /// </summary>
    public Provenance Origin { get; }

    /// <summary>
    /// Where each expected value is declared, in the same order as <see cref="Expected"/>. A value
    /// whose line could not be numbered still names its file, since the point of the reading is
    /// that no value in the set is one nobody can trace.
    /// </summary>
    public IReadOnlyList<Provenance> Origins { get; }

    /// <summary>
    /// The strings under this key that carry a placeholder, left out of the expectation and
    /// recorded rather than dropped.
    /// <para>
    /// A tree holding <c>Profile: Alexandre</c> can never equal <c>Profile: {name}</c>, so such a
    /// value in the set is a member nothing reads: an unfixable red on every run, or worse a green
    /// where some control happens to render the literal braces. Refusing the whole derivation was
    /// the other reading and it was measured against a real strings file: the fixture's own
    /// <c>labels</c> holds two ordinary strings and one templated one, and refusing would make
    /// that key underivable for the sake of a value nobody could have asserted anyway.
    /// </para>
    /// <para>
    /// So they are excluded and said out loud — in <see cref="Source"/>, which every verdict
    /// sentence carries, so the exclusion appears under each run rather than in this comment. A
    /// count that is not silent is not the defect this project exists about.
    /// </para>
    /// </summary>
    public IReadOnlyList<LeftOut> Excluded { get; }

    /// <summary>The ones left out for carrying a placeholder.</summary>
    public IReadOnlyList<LeftOut> Templated => new ReadOnlyCollection<LeftOut>(
        Excluded.Where(one => one.Why == LeftOutBecause.CarriesAPlaceholder).ToList());

    /// <summary>The ones left out for being a note rather than a string.</summary>
    public IReadOnlyList<LeftOut> Notes => new ReadOnlyCollection<LeftOut>(
        Excluded.Where(one => one.Why == LeftOutBecause.IsANote).ToList());

    /// <summary>Where one expected value came from, or <see cref="Provenance.Unknown"/> for a value not in the set.</summary>
    public Provenance Whence(string value)
    {
        var at = Expected.ToList().IndexOf(value);
        return at < 0 ? Provenance.Unknown : Origins[at];
    }

    /// <summary>
    /// Derive one from a language file, taking every string under <paramref name="under"/>.
    /// </summary>
    /// <param name="named">What the set is of, as the scenario names it.</param>
    /// <param name="languageFile">The JSON file the project ships its strings in.</param>
    /// <param name="under">The key the values sit under, dotted for a nested one.</param>
    /// <exception cref="UnderivableSetException">
    /// Where the file is missing or unreadable, or where the key yields nothing — an empty
    /// expected set passes against an empty window, which is the failure this type exists to stop
    /// and not one it is allowed to reintroduce by deriving nothing quietly.
    /// </exception>
    public static DerivedSet From(string named, string languageFile, string under)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(named);
        ArgumentException.ThrowIfNullOrWhiteSpace(languageFile);
        ArgumentException.ThrowIfNullOrWhiteSpace(under);

        var full = Path.GetFullPath(languageFile);
        if (!File.Exists(full))
            throw new UnderivableSetException($"{named} is derived from {full}, which is not there");

        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(
                File.ReadAllText(full),
                new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            root = document.RootElement.Clone();
        }
        catch (JsonException broken)
        {
            throw new UnderivableSetException($"{full} is not readable JSON: {broken.Message}");
        }

        var key = under.Trim();
        var found = Nested(root, key) ?? Flat(root, key);
        var where = $"derived from '{key}' in {Path.GetFileName(full)}";

        if (found is null || found.Count == 0)
            throw new UnderivableSetException(
                $"{named}: '{key}' in {full} declares no strings, and an empty expected set is met by an "
                    + "empty window — which is the hole this set exists to close");

        var declared = found.Select(pair => pair.Key).ToList();

        // One pass over the file for every key at once: a lookup per value would read a strings
        // file as many times as it has strings, which is the cost this whole rule exists to avoid.
        var lines = JsonSource.LinesOf(full, [key, .. declared]);

        var excluded = found
            .Where(pair => Why(pair.Key, pair.Value) is not null)
            .Select(pair => new LeftOut(
                pair.Key,
                pair.Value,
                Provenance.InFile(full, lines.GetValueOrDefault(pair.Key), pair.Key),
                Why(pair.Key, pair.Value)!.Value))
            .ToList();

        var kept = found.Where(pair => Why(pair.Key, pair.Value) is null).ToList();
        if (kept.Count == 0)
        {
            // The same rule as an empty key, said as what it is: nothing under here is a string a
            // window could show, so the set that survives is empty and an empty set is met by an
            // empty window. Named apart from "declares no strings" because the remedy differs.
            throw new UnderivableSetException(
                $"{named}: nothing under '{key}' in {full} is a string an exact read could match "
                    + $"({string.Join("; ", excluded.Select(one => one.ToString()))})");
        }

        var keys = kept.Select(pair => pair.Key).ToList();

        return new DerivedSet(
            named.Trim(),
            Derivation(where, excluded),
            new ReadOnlyCollection<string>(keys),
            new ReadOnlyCollection<string>(kept.Select(pair => pair.Value).ToList()),
            Provenance.InFile(full, lines.GetValueOrDefault(key), key),
            new ReadOnlyCollection<Provenance>(
                keys.Select(one => Provenance.InFile(full, lines.GetValueOrDefault(one), one)).ToList()),
            new ReadOnlyCollection<LeftOut>(excluded));
    }

    /// <summary>
    /// The same, from the language file that answers for the window under test.
    /// <para>
    /// WW240. Where <paramref name="language"/> says which language the window is in, the file is
    /// chosen exactly as a label's is — one owner for the question, which is what this did not have.
    /// Measured migrating claude-tray, which ships five languages: declaring all five made a sweep
    /// refuse, and declaring only English worked <em>because</em> every fixture there launches with
    /// `--lang en`. The answer was already written down one line above, in the fixture, and was being
    /// supplied instead by a project-wide declaration that happened to agree with it.
    /// </para>
    /// <para>
    /// With no language named it is the old rule and for the old reason: one file is the answer, and
    /// several is a question nothing here can settle. Picking the first would derive an expectation
    /// in a language nobody is looking at, which is worse than refusing.
    /// </para>
    /// </summary>
    /// <param name="named">What the set is, as a report names it.</param>
    /// <param name="declaration">The project, for the files it ships.</param>
    /// <param name="under">The key whose strings the set is.</param>
    /// <param name="language">What the window is in, or null where nothing said.</param>
    public static DerivedSet From(
        string named, ProjectDeclaration declaration, string under, System.Globalization.CultureInfo? language = null)
    {
        ArgumentNullException.ThrowIfNull(declaration);

        if (language is not null)
        {
            try
            {
                return From(named, Labels.FileFor(declaration, language, named).File, under);
            }
            catch (UnusableLabelException unusable)
            {
                // Its own kind of refusal, because a caller catching one of these is deciding what a
                // set that could not be derived means — and a label's exception reaching them would
                // be about a key nobody asked for.
                throw new UnderivableSetException(unusable.Message);
            }
        }

        return declaration.LanguageFiles.Count switch
        {
            0 => throw new UnderivableSetException(
                $"{named} is derived from the project's strings and {declaration.Path} declares no languageFiles"),
            1 => From(named, declaration.LanguageFiles[0], under),
            _ => throw new UnderivableSetException(
                $"{named}: {declaration.Path} declares {declaration.LanguageFiles.Count} language files and no "
                    + "fixture said which language the window is in, so which of them it is showing is not "
                    + "answerable here — name the language on the fixture, or the file here"),
        };
    }

    /// <summary>
    /// One value the application reports about itself, read by running it the way the project says.
    /// <para>
    /// WW294. The scalar beside <see cref="Reported"/>, and it exists because most of what an
    /// application knows about itself is not a set: which profile an icon follows, which one the
    /// environment selects, whether a toggle is on. A case can type none of them — they are this
    /// machine's state, so a case naming one passes on the desk it was written on and fails on every
    /// other, which is the defect the derived expectation exists to refuse.
    /// </para>
    /// <para>
    /// One line and exactly one. A read-out that answers several is a set and belongs in the other
    /// well, and one that answers none has told the case nothing — both are refused rather than
    /// guessed at, for the reason an empty set is: what a run cannot read, it cannot claim.
    /// </para>
    /// </summary>
    /// <param name="named">What the value is, as a report names it.</param>
    /// <param name="declaration">The project, for the executable and what it declared.</param>
    /// <param name="under">The name of the reported value, as the project declares it.</param>
    /// <exception cref="UnderivableSetException">Where the project declares no such value, or it cannot be read.</exception>
    public static string ReportedValue(string named, ProjectDeclaration declaration, string under)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(named);
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentException.ThrowIfNullOrWhiteSpace(under);

        var key = under.Trim();
        if (!declaration.ReportedValues.TryGetValue(key, out var arguments))
        {
            var has = declaration.ReportedValues.Count == 0
                ? "it declares no reportedValues at all"
                : $"it declares {string.Join(", ", declaration.ReportedValues.Keys.Select(one => $"'{one}'"))}";

            throw new UnderivableSetException(
                $"{named} is derived from what the application reports under '{key}', and {declaration.Path}: {has}");
        }

        var lines = Printed(named, declaration, key, arguments);

        return lines.Count switch
        {
            1 => lines[0],
            0 => throw new UnderivableSetException(
                $"{named}: {declaration.Executable} reported nothing under '{key}', so there is no value to "
                    + "compare against"),
            _ => throw new UnderivableSetException(
                $"{named}: {declaration.Executable} reported {lines.Count} lines under '{key}' and a value is "
                    + "one — declare it as a reportedSet where it is a set"),
        };
    }

    /// <summary>
    /// The set the application reports about itself, derived by running it the way the project says
    /// and reading one value per line.
    /// <para>
    /// WW260. The second well, and it exists because the first one is the wrong place for some sets:
    /// `covers` derives from the language files, which is right for every tab header the strings
    /// declare and wrong for what claude-tray's menu case counts. Profiles are this machine's data and
    /// the number is whatever this machine has, so neither half is in a strings file — and typing it
    /// is the defect this whole shape exists to refuse, one well over.
    /// </para>
    /// <para>
    /// One value per line, blank lines dropped, order preserved. Nothing is parsed beyond that: a
    /// format with structure in it is a second thing to keep in step with the application, and the
    /// only thing the expectation needs is which values there are.
    /// </para>
    /// <para>
    /// An empty report is refused for the reason an empty key is: a set with no members is met by an
    /// empty window, which is the hole this whole shape exists to close. So is a run that failed —
    /// what an application prints on its way to a non-zero exit is not a set.
    /// </para>
    /// </summary>
    /// <param name="named">What the set is, as a report names it.</param>
    /// <param name="declaration">The project, for the executable and what it declared.</param>
    /// <param name="under">The name of the reported set, as the project declares it.</param>
    /// <exception cref="UnderivableSetException">Where the project declares no such set, or it cannot be read.</exception>
    public static DerivedSet Reported(string named, ProjectDeclaration declaration, string under)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(named);
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentException.ThrowIfNullOrWhiteSpace(under);

        var key = under.Trim();
        if (!declaration.ReportedSets.TryGetValue(key, out var arguments))
        {
            var has = declaration.ReportedSets.Count == 0
                ? "it declares no reportedSets at all"
                : $"it declares {string.Join(", ", declaration.ReportedSets.Keys.Select(one => $"'{one}'"))}";

            throw new UnderivableSetException(
                $"{named} is derived from what the application reports under '{key}', and {declaration.Path}: {has}");
        }

        // Distinct here and not in the reader: a value well reading two identical lines has been asked
        // for one thing and answered twice, which is a refusal rather than a set of one.
        var values = Printed(named, declaration, key, arguments).Distinct(StringComparer.Ordinal).ToList();

        if (values.Count == 0)
        {
            throw new UnderivableSetException(
                $"{named}: {declaration.Executable} reported nothing under '{key}', and an empty expected "
                    + "set is met by an empty window — which is the hole this set exists to close");
        }

        var how = $"reported by {System.IO.Path.GetFileName(declaration.Executable)} "
            + $"{string.Join(" ", arguments)}";

        // WW290. A name declared in both wells resolves here and shadowed the strings key in silence.
        // Which one wins is not the problem — a rule has to pick — the silence is: the reader of a
        // passing sweep could not know their strings key was dead, and the reader of a red went to the
        // wrong file. Said in the source rather than refused, because a collision is not necessarily a
        // mistake: `profiles` is exactly the kind of name that is both a data set and a UI label, and
        // refusing would break a project whose two are legitimately unrelated.
        if (ShadowedIn(declaration, key) is { } shadowed)
            how += $", which shadows the '{key}' declared in {shadowed}";

        var members = new ReadOnlyCollection<string>(values);

        return new DerivedSet(
            named,
            how,

            // The values are their own keys: a reported set has no key-to-string layer, which is the
            // whole difference between this well and the strings one.
            members,
            members,

            // Unknown, and nothing left out. A reported value has no line in a file to point at, and
            // the two reasons a declared string is excluded — a placeholder, a note — are both facts
            // about a strings file, so claiming either here would be inventing one.
            Provenance.Unknown,
            new ReadOnlyCollection<Provenance>([]),
            new ReadOnlyCollection<LeftOut>([]));
    }

    /// <summary>
    /// Compare the set with what was read. The only door readings come in by, which is what keeps
    /// the expectation from being derived from the thing it is asserting.
    /// </summary>
    /// <param name="read">What the tree actually held.</param>
    /// <param name="match">
    /// Which of the three claims to make. WW275 and WW292: the strangers are counted and said under
    /// every one of them — allowed is not the same as unrecorded.
    /// </param>
    public SetComparison Against(IEnumerable<string> read, SetMatch match = SetMatch.Exactly)
    {
        ArgumentNullException.ThrowIfNull(read);

        var seen = read.Where(value => value is not null).ToList();

        // WW292. Containment is its own arithmetic and not a looser equality: a declared value is
        // accounted for where some name holds it, and a name is a stranger where it holds none of
        // them. Ordinal, like every other comparison here — a case-insensitive match would pass an
        // application that renders the wrong capitalisation of somebody's account name.
        if (match == SetMatch.Within)
        {
            var inside = Expected
                .Where(value => seen.Exists(one => one.Contains(value, StringComparison.Ordinal)))
                .ToList();

            return new SetComparison(
                this,
                new ReadOnlyCollection<string>(inside),
                new ReadOnlyCollection<string>(Expected.Where(one => !inside.Contains(one, StringComparer.Ordinal)).ToList()),
                new ReadOnlyCollection<string>(
                    seen.Where(one => !Expected.Any(value => one.Contains(value, StringComparison.Ordinal)))
                        .Distinct(StringComparer.Ordinal)
                        .ToList()),
                match);
        }

        var wanted = new HashSet<string>(Expected, StringComparer.Ordinal);
        var found = new HashSet<string>(seen, StringComparer.Ordinal);

        return new SetComparison(
            this,
            new ReadOnlyCollection<string>(Expected.Where(found.Contains).ToList()),
            new ReadOnlyCollection<string>(Expected.Where(value => !found.Contains(value)).ToList()),
            new ReadOnlyCollection<string>(seen.Where(value => !wanted.Contains(value)).Distinct(StringComparer.Ordinal).ToList()),
            match);
    }

    /// <summary>
    /// Why one string is not a member, or null where it is one.
    /// <para>
    /// Two reasons and no third. A placeholder can never be matched by an exact read; a note is not
    /// a string the application shows at all — JSON has no comments, so a strings file that wants
    /// one writes a key nobody reads, and the derivation took both notes in this repository's own
    /// fixture as things a window should display.
    /// </para>
    /// </summary>
    private static LeftOutBecause? Why(string key, string value)
    {
        if (IsANote(key))
            return LeftOutBecause.IsANote;

        return Labels.CarriesAPlaceholder(value) ? LeftOutBecause.CarriesAPlaceholder : null;
    }

    /// <summary>
    /// Whether a key is the convention for a comment rather than a name for a string.
    /// <para>
    /// Read off the last segment, since a nested key arrives here dotted. Four spellings of one
    /// convention: <c>//</c> is the common one and <c>//2</c> the way a file carrying two of them
    /// writes the second, and the underscore and dollar forms are the same idea from other
    /// ecosystems. A project that genuinely ships a label under a key called <c>//</c> does not
    /// exist, and one that did would be told by the source sentence rather than left guessing.
    /// </para>
    /// </summary>
    private static bool IsANote(string key)
    {
        var last = key.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) is { Length: > 0 } steps
            ? steps[^1]
            : key.Trim();

        return last.StartsWith("//", StringComparison.Ordinal)
            || string.Equals(last, "_comment", StringComparison.OrdinalIgnoreCase)
            || string.Equals(last, "$comment", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Where the set came from, with what it left out and why. In the source rather than in a
    /// comment: every verdict prints this sentence, so the exclusion is read under each run.
    /// </summary>
    private static string Derivation(string where, IReadOnlyList<LeftOut> excluded)
    {
        if (excluded.Count == 0)
            return where;

        var parts = new List<string>();
        foreach (var why in new[] { LeftOutBecause.CarriesAPlaceholder, LeftOutBecause.IsANote })
        {
            var these = excluded.Where(one => one.Why == why).ToList();
            if (these.Count == 0)
                continue;

            var said = why == LeftOutBecause.CarriesAPlaceholder ? "carrying a placeholder" : "a note and not a string";
            parts.Add($"{these.Count} {said} ({string.Join("; ", these.Select(one => $"'{one.Key}'"))})");
        }

        return $"{where}, less {string.Join(" and ", parts)}";
    }

    /// <summary>
    /// Run the application the way the project says and hand back the lines it printed, blank ones
    /// dropped and order kept.
    /// <para>
    /// WW260 and WW294. One reader for both wells: a set is the lines and a value is the single line,
    /// and everything before that — starting it, reading the pipe before waiting so a full buffer
    /// cannot deadlock, and refusing a run that failed — is the same question asked once.
    /// </para>
    /// <para>
    /// Nothing is parsed beyond the split. A format with structure in it is a second thing to keep in
    /// step with the application, and the only thing an expectation needs is what the values are.
    /// </para>
    /// </summary>
    /// <param name="named">What is being derived, for the refusals.</param>
    /// <param name="declaration">The project, for the executable.</param>
    /// <param name="key">The name being asked for, for the refusals.</param>
    /// <param name="arguments">What the application is run with.</param>
    /// <exception cref="UnderivableSetException">Where it will not start, or exits non-zero.</exception>
    private static List<string> Printed(
        string named, ProjectDeclaration declaration, string key, IReadOnlyList<string> arguments)
    {
        var start = new System.Diagnostics.ProcessStartInfo(declaration.Executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };

        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);

        string printed;
        int code;
        try
        {
            using var running = System.Diagnostics.Process.Start(start)
                ?? throw new UnderivableSetException(
                    $"{named}: {declaration.Executable} started nothing that could be asked for '{key}'");

            // Read before the wait: a process filling a redirected pipe nobody is reading blocks on
            // the write, and this would then wait out an application that had already done its work.
            printed = running.StandardOutput.ReadToEnd();
            running.WaitForExit();
            code = running.ExitCode;
        }
        catch (Exception refused) when (refused is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            throw new UnderivableSetException(
                $"{named}: {declaration.Executable} could not be asked for '{key}' — {refused.Message}");
        }

        if (code != 0)
        {
            throw new UnderivableSetException(
                $"{named}: {declaration.Executable} exited {code} when asked for '{key}', so what it printed "
                    + "is not an answer");
        }

        return printed.Split('\n').Select(one => one.Trim()).Where(one => one.Length > 0).ToList();
    }

    /// <summary>
    /// The strings file that also declares this key, or null where none does.
    /// <para>
    /// WW290. Asked of every declared file rather than of one, which is what makes it cheap enough to
    /// ask at all: picking a file is what refuses where a project ships several and no fixture said
    /// which language the window is in, and this picks none — it asks whether any of them has the key.
    /// A project declaring no language files pays nothing, which is most projects using this well.
    /// </para>
    /// <para>
    /// A file that will not parse is not a shadow. This exists to tell a reader where a second
    /// declaration is, and a broken file is `From`'s refusal to make when somebody derives from it —
    /// borrowing it here would turn an unrelated strings problem into a failure about a reported set.
    /// </para>
    /// </summary>
    private static string? ShadowedIn(ProjectDeclaration declaration, string key)
    {
        foreach (var file in declaration.LanguageFiles)
        {
            try
            {
                using var document = JsonDocument.Parse(
                    File.ReadAllText(file),
                    new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

                var root = document.RootElement;
                if ((Nested(root, key) ?? Flat(root, key)) is { Count: > 0 })
                    return System.IO.Path.GetFileName(file);
            }
            catch (Exception unreadable) when (unreadable is JsonException or IOException or UnauthorizedAccessException)
            {
                // Not a shadow, and not this reading's refusal to make. See above.
            }
        }

        return null;
    }

    private static List<KeyValuePair<string, string>>? Nested(JsonElement root, string key)
    {
        var element = root;
        foreach (var step in key.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(step, out element))
                return null;
        }

        if (element.ValueKind != JsonValueKind.Object)
            return null;

        return element.EnumerateObject()
            .Where(property => property.Value.ValueKind == JsonValueKind.String)
            .Select(property => new KeyValuePair<string, string>(
                $"{key}.{property.Name}", property.Value.GetString()!))
            .ToList();
    }

    private static List<KeyValuePair<string, string>>? Flat(JsonElement root, string key)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return null;

        // The other shape a strings file comes in: one flat object with dotted names. Reached
        // only when the nested walk found nothing, so a file using both is read the way it nests.
        var prefix = $"{key}.";
        return root.EnumerateObject()
            .Where(property => property.Value.ValueKind == JsonValueKind.String
                && property.Name.StartsWith(prefix, StringComparison.Ordinal))
            .Select(property => new KeyValuePair<string, string>(property.Name, property.Value.GetString()!))
            .ToList();
    }
}

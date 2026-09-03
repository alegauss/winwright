using System.Collections.ObjectModel;

namespace Winwright.Fixture;

/// <summary>
/// The ways the fixture can be asked for something it does not know how to be.
/// <para>
/// WW200. WW196 armed four refusals of five and left this one, because it lives here rather than in
/// the engine and the suite references this fixture without its assembly on purpose — an application
/// under test is launched from its own output, not read from beside the harness. So the shape WW196
/// built, an enum swept by reflection, had nothing to reflect over.
/// </para>
/// <para>
/// The boundary is crossed the way WW146 crossed it: by running the article and reading what it
/// says. The arm is named in the refusal and the whole list is printed with the catalogue, so the
/// suite pairs against what an adopter would actually see rather than against a type it cannot load.
/// </para>
/// <para>
/// Ten arms across eleven throw sites, and the arithmetic is the judgement rather than an oversight.
/// A flag given a value that is not a whole number and a rectangle field that is not one are the
/// same refusal carrying a different value: the reader writes a number either way.
/// </para>
/// </summary>
public enum UnknownFlag
{
    /// <summary>Thrown without saying which. Pairs with nothing, and the suite refuses it.</summary>
    Unsaid,

    /// <summary>An argument that does not begin with two dashes, so it names no flag at all.</summary>
    NotAFlag,

    /// <summary>A flag spelled correctly and belonging to no shape this fixture has.</summary>
    NoSuchShape,

    /// <summary>A flag that takes a value, given none.</summary>
    NeedsAValue,

    /// <summary>A flag that takes nothing, given something.</summary>
    TakesNothing,

    /// <summary>A flag with a fixed set of values, given one outside it.</summary>
    ValueNotAccepted,

    /// <summary>A value that has to be a whole number and is not one.</summary>
    NotAWholeNumber,

    /// <summary>A flag that means nothing without another, asked for on its own.</summary>
    NeedsACompanion,

    /// <summary>Two shapes that each want to be the thing drawn, asked for together.</summary>
    TwoRendersAtOnce,

    /// <summary>A rectangle that is not four comma-separated fields.</summary>
    MalformedRectangle,

    /// <summary>A rectangle of no area, which would be a shape that provokes nothing.</summary>
    CoversNothing,
}

/// <summary>Raised where the fixture was asked for something it does not know how to be.</summary>
public sealed class UnknownFlagException : ArgumentException
{
    /// <summary>Say what was asked for and what this knows.</summary>
    public UnknownFlagException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// The same, saying which of the ways it was asked wrongly this one is. The arm leads the
    /// message, because a person reading a refusal is who this is for and a name they can grep is
    /// worth more to them than a phrase they have to quote exactly.
    /// </summary>
    /// <param name="arm">Which way.</param>
    /// <param name="message">What was asked for and what this knows.</param>
    public UnknownFlagException(UnknownFlag arm, string message)
        : base($"{Named(arm)}: {message}")
    {
        Arm = arm;
    }

    /// <summary>How an arm is spelled wherever the fixture prints one.</summary>
    /// <param name="arm">The arm.</param>
    public static string Named(UnknownFlag arm) => $"refused {arm}";

    /// <summary>
    /// Which way it was asked wrongly. <see cref="UnknownFlag.Unsaid" /> where it was thrown without
    /// saying — a refusal nothing can pair, and the check says so.
    /// </summary>
    public UnknownFlag Arm { get; } = UnknownFlag.Unsaid;

    /// <summary>Unused. Present because an exception with no default shapes is awkward to catch.</summary>
    public UnknownFlagException()
        : base("the fixture was asked for a shape it does not have")
    {
    }

    /// <summary>Unused. Present for the same reason.</summary>
    public UnknownFlagException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>One shape the fixture can be asked to take.</summary>
/// <param name="Name">The flag, without its dashes.</param>
/// <param name="Takes">What it takes after an equals sign, or empty where it takes nothing.</param>
/// <param name="Provokes">The refusal or the reading it exists to make possible.</param>
/// <param name="Because">
/// The real defect this shape reproduces, and where it happened. Required: a fixture that grows
/// shapes nobody can justify becomes a second product to maintain, drifts from the things it
/// stands in for, and starts producing false confidence. One that can name no defect is removed,
/// and the removal is itself a reading about what this framework no longer has to defend against.
/// </param>
/// <param name="Draws">
/// Whether asking for this shape puts something on the screen. False for the three that
/// deliberately do not, and said out loud in the catalogue: when a case fails, the fastest way to
/// understand it is to look at the thing it is talking about, and a flag that quietly shows
/// nothing costs somebody a minute finding that out.
/// </param>
/// <param name="Needs">
/// Another flag this one is meaningless without, or empty. Said in the catalogue, because a
/// person driving by hand should not have to launch a shape and read a refusal to learn it.
/// </param>
/// <param name="Alone">
/// What it has nothing to do without <paramref name="Needs" />, in the word the refusal uses.
/// Carried on the row rather than written out per flag: the pair that was checked by hand once is
/// the pair the next shape added forgets, which is the silent nothing this whole record exists to
/// stop.
/// </param>
/// <param name="Exits">
/// What a run asking for this shape ends with, where it is not the ordinary zero.
/// <para>
/// WW161. A shape that provokes the refusal it exists to provoke exits 3, and nothing a person or
/// a case read said so — the code was learnt by reading the host, and the suite carried its own
/// copy of the number. Said on the row, so a case asserting 3 reads it off the article the way it
/// already reads the flag names.
/// </para>
/// </param>
/// <param name="Choices">
/// The values it accepts, where it accepts a fixed set. Empty means any text. A value outside the
/// set is refused the same way an unknown flag is: a shape nobody can spell is a shape nobody
/// takes, and the run that misspells one asserts nothing and says so nowhere.
/// </param>
public sealed record Flag(
    string Name,
    string Takes,
    string Provokes,
    string Because,
    bool Draws = true,
    string Needs = "",
    string Alone = "",
    int Exits = 0,
    IReadOnlyList<string>? Choices = null,
    bool Numeric = false)
{
    /// <summary>What it accepts, or nothing where it accepts any text.</summary>
    public IReadOnlyList<string> Accepts => Choices ?? [];

    /// <summary>The one line the catalogue prints.</summary>
    public override string ToString()
    {
        var takes = Takes.Length == 0
            ? ""
            : Accepts.Count == 0 ? $"=<{Takes}>" : $"={string.Join("|", Accepts)}";

        var alone = Needs.Length == 0 ? "" : $" [needs --{Needs}]";
        var ends = Exits == 0 ? "" : $" [exits {Exits}]";
        return $"--{Name}{takes}  {Provokes}{alone}{ends}{(Draws ? "" : " [draws nothing]")}";
    }

    /// <summary>The two lines a catalogue prints: what it does, and why it is here at all.</summary>
    public string Justified() => $"{this}\n      because {Because}";
}

/// <summary>
/// What the fixture was asked to be.
/// <para>
/// This framework's value is concentrated in its refusals, and a refusal nobody can provoke is a
/// refusal that will quietly stop working. Each one gets a flag here, so the framework's own suite
/// can assert the red — which is the only thing that keeps a refusal real rather than remembered.
/// </para>
/// <para>
/// An unknown flag is refused rather than ignored, and that is the first refusal this fixture
/// makes about itself. A misspelt flag that silently does nothing produces a run where the shape
/// was never taken, the refusal never fired, and the case went green for the worst possible
/// reason: it asserted nothing and said so nowhere.
/// </para>
/// </summary>
public sealed record Flags
{
    private readonly IReadOnlyDictionary<string, string> given;

    private Flags(IReadOnlyDictionary<string, string> given)
    {
        this.given = given;
    }

    /// <summary>
    /// Every flag this fixture knows. The list lives here rather than in each shape's own file, so
    /// a shape added later without a row is a shape nobody can find.
    /// </summary>
    public static IReadOnlyList<Flag> Known { get; } = new ReadOnlyCollection<Flag>(
    [
        new Flag(
            "flags",
            "",
            "print this catalogue and stop, so what the fixture can do is askable without misspelling something to be told",
            "a catalogue that lives only in source is one nobody consults, and claude-tray's preview catalogue already prints its whole table on an unknown name and exits non-zero",
            Draws: false),
        new Flag(
            "show",
            "",
            "bring the window to the front, which is what a person driving this by hand wants and what a suite raising it thirty times a run must never do",
            "the fixture stops taking the desk so the suite's foreground checks can run, and a person launching it by hand then gets a window behind whatever they were reading"),
        new Flag(
            "title",
            "text",
            "a window titled something other than the default, for a case driving two at once",
            "the other-instance refusal was tested by remembering to leave a second window open, which is a test nobody runs the same way twice"),
        new Flag(
            "pump",
            "host",
            "the same window under a dispatcher that runs and one that never does - the difference no picture can see",
            "claude-tray shipped windows that took no keystrokes while every screenshot of them looked perfect",
            Choices: ["dispatcher", "none"]),
        new Flag(
            "names",
            "",
            "a pane carrying the whole naming rule at once - nothing, a glyph, an echoed id, a neighbouring label, and a button that keeps its text",
            "claude-tray shipped two controls carrying empty names while every neighbouring button read fine, because a control takes its name from its own content and both had none"),
        new Flag(
            "announces",
            "",
            "a pane of rows that carry their own state in their text and say which state in a sentence beside their name - one marked, one not, one whose explanation contains the word for marked, and one that says nothing",
            "claude-tray's tray entries lose the toggle pattern to the custom accessible object that carries their sentence, so the check for which profile the icon follows had to read a word in front of free text and no case could address a row whose name ends in a reading"),
        new Flag(
            "chords",
            "",
            "a pane whose two commands have no button, no menu entry and no toolbar - only a chord, one per modifier set",
            "quickshell's window is a title bar and a terminal on purpose, so every command is on Ctrl+Shift+something - and press spelled Tab and the arrows, which is the right vocabulary for moving focus and the wrong one for invoking a command"),
        new Flag(
            "absences",
            "",
            "a pane carrying the three kinds of absence at once - a collapsed pane, a closed popup and an unopened submenu",
            "a control on a page that is not showing cannot be found by any id, which reads exactly like one that was renamed or removed"),
        new Flag(
            "ranges",
            "",
            "a pane carrying the three answers a key pressed at a range has - room either way, already at the maximum, and no room at all",
            "Traversal.Nudge chose its direction from where the control already sat and nothing here drove it, so the branch that presses the other way at the end of a range had never run against a real control"),
        new Flag(
            "rows",
            "how",
            "a pane of settings rows, either paired correctly or with one control wearing the row next door's label",
            "a rule that pairs a row's control with the wrong row's header gives several controls one name, and every check that asks whether a name exists passes it",
            Choices: RowsPane.Names),
        new Flag(
            "pickers",
            "",
            "a pane carrying the two answers 'what does this picker hold' has - one shut, whose items are in no tree until it opens, and one already dropped down",
            "Pick was driven only against a Win32 combo, which holds its items either way, so the walk read 'it holds nothing' about a WPF picker holding two and refused the case it was built for"),
        new Flag(
            "backdrop",
            "kind",
            "a window that opted into a system backdrop, which transmits what is behind it through the glass",
            "z-order reasoning cannot answer for a backdrop, so every check that decides a copy's contents by walking the windows above it is wrong about that one window",
            Choices: Backdrop.Names),
        new Flag(
            "chromeless",
            "",
            "a window with no title bar and no border, which is what an application draws when it means to draw its own",
            "a route that tells a renderable window from a drop-down by its style bits was suspected of calling this one a drop-down, and a suspicion nothing drives is one nobody can settle"),
        new Flag(
            "layered",
            "how",
            "a window made see-through by its layer rather than by its backdrop - at half alpha, at full alpha, or with a colour key",
            "a layered window reports the auto backdrop truthfully while being as much a window on to the desktop as an acrylic one, and the shadow Windows draws behind every menu is one",
            Choices: Layered.Names),
        new Flag(
            "cloak",
            "",
            "a window the application has asked the compositor to stop drawing, which keeps every style bit saying it is visible",
            "a suspended packaged application cloaks itself, and a capture of one is a blank file that looks exactly like a capture",
            Draws: false),
        new Flag(
            "toast",
            "way",
            "a borderless top-level window with no caption, which the process object never names",
            "a toast existed here in exactly one product and only when its notification happened to fire, which is not a schedule a check can wait on",
            Choices: Toast.Ways),
        new Flag(
            "loading",
            "milliseconds",
            "a page that is still computing for exactly this long",
            "the loading refusal was discovered on a machine that happened to be slow, and reproducing it meant finding another one",
            Numeric: true),
        new Flag(
            "animate",
            "milliseconds",
            "an animation of a declared length whose states announce their own place",
            "a frame sequence was checked by opening the frames and looking, which is the thing this framework exists to avoid",
            Numeric: true),
        new Flag(
            "render",
            "path",
            "render the fixed surface to a file and exit, showing no window at all",
            "the byte-identical comparison had nothing to be identical to, because every surface available read a clock, a machine name or the desktop's theme",
            Draws: false),
        new Flag(
            "resident",
            "",
            "a process that runs and shows nothing, which is the ordinary state of a tray application",
            "a resident tray showing nothing runs on every developer machine here, and a refusal firing on it would need an override everybody passes always",
            Draws: false),
        new Flag(
            "shadowed",
            "",
            "a process with no window at all but a menu, and the shadow the shell draws behind it - which is larger than the menu on every side",
            "the largest window a tray process owns is that shadow, so the convenience verb answered the one surface beside a menu that must never be photographed, and no fixture here could be a process whose only windows are those two",
            Draws: false),
        new Flag(
            "profiles",
            "",
            "print the profiles this application has, one per line, and exit - the set a case derives from what the application reports rather than from a strings file",
            "the menu case counts profile entries against the application's own read-out, and profiles are the machine's data rather than the product's vocabulary - so there was no strings file to derive the expected set from and the number would have been typed into the case",
            Draws: false),
        new Flag(
            "profile",
            "",
            "print the one profile this application is currently using, and exit - the single value a case compares against, where the set flag prints them all",
            "the states a tray submenu marks - which profile the icon follows, which one the environment selects - are single facts about this machine rather than a list, and a case naming one passes on the desk it was written on and fails on every other",
            Draws: false),
        new Flag(
            "dies",
            "",
            "a launch that exits on startup, drawing nothing and leaving nothing for a step to find",
            "a tray that crashed on startup reached the case as a run against the desktop, and every step in it reported a locator that matched nothing - so the reds named missing controls on a desk where the application had never existed",
            Draws: false,
            Exits: Program.Died),
        new Flag(
            "store",
            "directory",
            "a settings store of the fixture's own, written from constants, which a run may break",
            "the fingerprint check protects the store of whoever is running it, so it could not be developed against a real product without risking somebody's settings"),
        new Flag(
            "mutate",
            "",
            "leave that store changed - the same number of bytes and a different machine",
            "a settings file rewritten to the same length is the accident the fingerprint exists for: a picker repointed from one profile to another of the same name",
            Needs: "store",
            Alone: "change"),
        new Flag(
            "sizeless",
            "",
            "render a page whose every row is collapsed, which lays out to nothing at all",
            "a page that renders empty writes a file, and an empty file is a successful render to everything that only checks a file exists",
            Draws: false,
            Needs: "render",
            Alone: "lay out",
            Exits: Program.Refused),
        new Flag(
            "blank",
            "",
            "render a page that is the right size and paints nothing, on no background",
            "a tree measured and never arranged draws a fully transparent picture of exactly the right size, which looks like a drawing bug and is a calling bug",
            Draws: false,
            Needs: "render",
            Alone: "draw"),
        new Flag(
            "unbacked",
            "",
            "render before the application exists, so nothing anywhere says what to draw the capture on",
            "a capture taken during startup was drawn on a colour somebody guessed, and the classic palette guesses white on a desk whose windows are dark",
            Draws: false,
            Needs: "render",
            Alone: "draw",
            Exits: Program.Refused),
        new Flag(
            "language",
            "tag",
            "a window labelled from one of the fixture's own string files",
            "the label rule needs several languages to be developed at all, and one key whose value carries a placeholder that no exact read can ever match",
            Choices: Strings.Cultures),
        new Flag(
            "intrude",
            "left,top,width,height",
            "a topmost window over exactly that rectangle in physical pixels",
            "the region check is the most intricate piece of the capture stack and was exercised by moving a window by hand and hoping"),
        new Flag(
            "peerless",
            "",
            "a pane drawn with no automation peers at all, which a locator resolves against nothing",
            "the only surface with no accessibility tree was an installer page in another repository, behind a compiler that has to be installed first"),
    ]);

    /// <summary>The shapes a person driving this by hand can actually look at.</summary>
    public static IReadOnlyList<Flag> Drawn { get; } =
        new ReadOnlyCollection<Flag>(Known.Where(one => one.Draws).ToList());

    /// <summary>Whether the fixture was asked for that shape.</summary>
    /// <param name="name">The flag, without its dashes.</param>
    public bool Has(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return given.ContainsKey(name.Trim());
    }

    /// <summary>What it was given after the equals sign, or null where the flag is absent.</summary>
    /// <param name="name">The flag, without its dashes.</param>
    public string? Value(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return given.TryGetValue(name.Trim(), out var value) ? value : null;
    }

    /// <summary>How many shapes were asked for.</summary>
    public int Count => given.Count;

    /// <summary>
    /// Read the command line.
    /// </summary>
    /// <param name="arguments">The arguments, as the process was given them.</param>
    /// <exception cref="UnknownFlagException">
    /// Where anything is not a flag this fixture knows. The message names the catalogue, because a
    /// refusal that does not say what would have worked costs a reader the source.
    /// </exception>
    public static Flags Read(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var read = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var argument in arguments)
        {
            var text = (argument ?? "").Trim();
            if (text.Length == 0)
                continue;

            if (!text.StartsWith("--", StringComparison.Ordinal))
                throw new UnknownFlagException(UnknownFlag.NotAFlag, $"'{text}' is not a flag: every argument begins with --.{Catalogue()}");

            var body = text[2..];
            var equals = body.IndexOf('=', StringComparison.Ordinal);
            var name = equals < 0 ? body : body[..equals];
            var value = equals < 0 ? "" : body[(equals + 1)..];

            var known = Known.FirstOrDefault(one => string.Equals(one.Name, name, StringComparison.Ordinal))
                ?? throw new UnknownFlagException(UnknownFlag.NoSuchShape, $"--{name} is not a shape this fixture has.{Catalogue()}");

            if (known.Takes.Length > 0 && value.Length == 0)
                throw new UnknownFlagException(UnknownFlag.NeedsAValue, $"--{name} takes a value: --{name}=<{known.Takes}>.{Catalogue()}");

            if (known.Takes.Length == 0 && equals >= 0)
                throw new UnknownFlagException(UnknownFlag.TakesNothing, $"--{name} takes nothing, and it was given '{value}'.{Catalogue()}");

            if (known.Accepts.Count > 0 && !known.Accepts.Contains(value, StringComparer.Ordinal))
            {
                throw new UnknownFlagException(
                    UnknownFlag.ValueNotAccepted,
                    $"--{name} does not take '{value}': it takes {string.Join(" or ", known.Accepts)}.{Catalogue()}");
            }

            // A duration that is not a number would otherwise be taken as zero, and a page asked to
            // load for 'twoseconds' that loads for none is the shape nobody can provoke again.
            if (known.Numeric
                && (!int.TryParse(value, System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out var counted)
                    || counted < 0))
            {
                throw new UnknownFlagException(
                    UnknownFlag.NotAWholeNumber,
                    $"--{name} takes a whole number of {known.Takes} and was given '{value}'.{Catalogue()}");
            }

            read[name] = value;
        }

        // Read at insertion, so a rectangle nobody can parse is a refusal before any window rather
        // than an intruder placed somewhere nobody asked.
        if (read.TryGetValue("intrude", out var rectangle))
            Intruder.Read(rectangle);

        Accompanied(read);
        return new Flags(new ReadOnlyDictionary<string, string>(read));
    }

    /// <summary>
    /// The flag the render shapes hang off. Naming it is what makes them exclusive: what a run
    /// renders is one thing, and a run asking for two of them gets whichever the host checks first.
    /// </summary>
    private const string TheRender = "render";

    /// <summary>
    /// Refuse a shape that was asked for without the one it is meaningless without, and a run that
    /// asked for two shapes of the same render.
    /// <para>
    /// A flag that does nothing without another is a flag that silently does nothing, which is the
    /// same green as a misspelt one and just as hard to notice. Read off the catalogue rather than
    /// written out per flag: this was one hand-written pair until three more arrived.
    /// </para>
    /// </summary>
    /// <param name="read">What the command line asked for.</param>
    /// <exception cref="UnknownFlagException">Where a companion is missing, or two shapes compete.</exception>
    private static void Accompanied(IReadOnlyDictionary<string, string> read)
    {
        var asked = Known.Where(one => read.ContainsKey(one.Name)).ToList();

        foreach (var flag in asked.Where(one => one.Needs.Length > 0 && !read.ContainsKey(one.Needs)))
        {
            var companion = Known.First(one => string.Equals(one.Name, flag.Needs, StringComparison.Ordinal));
            var takes = companion.Takes.Length == 0 ? "" : $"=<{companion.Takes}>";

            throw new UnknownFlagException(
                UnknownFlag.NeedsACompanion,
                $"--{flag.Name} has nothing to {flag.Alone} without --{companion.Name}{takes}.{Catalogue()}");
        }

        var competing = asked
            .Where(one => string.Equals(one.Needs, TheRender, StringComparison.Ordinal))
            .Select(one => $"--{one.Name}")
            .ToList();

        if (competing.Count > 1)
        {
            throw new UnknownFlagException(
                UnknownFlag.TwoRendersAtOnce,
                $"a render draws one thing, and {string.Join(" and ", competing)} are two of them: ask for one."
                    + Catalogue());
        }
    }

    /// <summary>Every flag, as a person driving the fixture by hand reads them.</summary>
    /// <param name="justified">
    /// Whether to say why each shape exists as well as what it does. A refusal wants to be
    /// scannable and leaves this off; a catalogue somebody asked for wants to be complete.
    /// </param>
    public static string Catalogue(bool justified = false) =>
        "\nThis fixture knows:\n"
        + string.Join("\n", Known.Select(one => "  " + (justified ? one.Justified() : one.ToString())))
        + "\n" + Codes() + "\n" + Arms();

    /// <summary>
    /// Every way this fixture refuses being driven, listed once.
    /// <para>
    /// WW200. The suite cannot load this assembly and so cannot sweep the enum, which is why the
    /// list is printed: the pairing is checked against what an adopter would see rather than against
    /// a type nothing here can reach. <see cref="UnknownFlag.Unsaid" /> is left off — it is what a
    /// throw that named no arm carries, and nothing provokes a refusal nobody described.
    /// </para>
    /// <para>
    /// Read off the enum rather than typed, for the reason the whole catalogue exists: a list
    /// written down twice is a list that drifts from the thing it describes.
    /// </para>
    /// </summary>
    public static string Arms() =>
        $"It refuses (exit {Program.UnknownFlag}):\n"
        + string.Join(
            "\n",
            Enum.GetValues<UnknownFlag>()
                .Where(one => one != UnknownFlag.Unsaid)
                .Select(one => $"  {UnknownFlagException.Named(one)}"));

    /// <summary>
    /// What a run of this fixture ends with, listed once.
    /// <para>
    /// WW161. The rows say which shapes end in a refusal; this says what the numbers mean, and it
    /// prints on a refusal as well as on the catalogue somebody asked for — a person who has just
    /// been handed a 2 is exactly the person who needs to know that it means the fixture was driven
    /// wrong rather than that it did something.
    /// </para>
    /// <para>
    /// Read off the host's own constants rather than typed, for the reason the whole catalogue
    /// exists: a number written down twice is a number that drifts from the thing it describes.
    /// </para>
    /// </summary>
    public static string Codes() =>
        "It exits:\n"
        + $"  0  the shape it was asked for did what it does\n"
        + $"  {Program.UnknownFlag}  a shape this fixture does not have, or one asked for wrongly\n"
        + $"  {Program.Refused}  a shape that ended in the refusal it exists to provoke";
}

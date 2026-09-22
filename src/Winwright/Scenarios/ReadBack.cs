using System.Collections.ObjectModel;
using System.Globalization;

using Winwright.Locating;

namespace Winwright.Scenarios;

/// <summary>
/// Which of an element's readings an expectation is about, as a name rather than a lambda.
/// <para>
/// <see cref="Asserting.Expect"/> takes a function, which is the right door for a script and the wrong one
/// for a file: a case cannot carry a delegate, and the reading is exactly the field that decides
/// whether the expectation is about the text in a box or the state of a checkbox. Naming it also
/// closes a hole a lambda leaves open — an expectation reading a pattern the element does not offer
/// reads null forever, and null is not a value, so the failure sentence says <em>nothing answered
/// to it</em> rather than naming the reading that was never there.
/// </para>
/// <para>
/// <see cref="Anything"/> is the default and is <see cref="PatternValues.Reading"/>: the one value
/// worth showing, in the order a reader looks at them. It is what a case that just wants to know
/// what the control says should name, and it is the only one that answers for an element whose
/// pattern the author has not looked up.
/// </para>
/// </summary>
public sealed record ReadBack
{
    private static readonly ReadBack[] Vocabulary =
    [
        new("anything", "the one value worth showing, whichever pattern the element happens to offer.", read => read.Values.Reading()),
        new("value", "what a control holding a value says it holds, through its ValuePattern.", read => read.Values.Value),
        new("range", "where a slider or a progress bar sits, as a number.", read => read.Values.Range is { } range ? range.ToString(CultureInfo.InvariantCulture) : null),
        new("toggle", "whether a checkbox or a switch is on, off, or neither.", read => read.Values.Toggle),
        new("selected", "whether this element is the chosen one, which is a claim about the element.", read => read.Values.IsSelected switch
        {
            true => "selected",
            false => "not selected",
            null => null,
        }),
        // WW266. The other half of 'selected', and the one a claim about a picker is actually about:
        // that one asks whether this element is chosen, and this asks which one the container chose.
        // Measured missing on claude-tray's profile picker, which offers no ValuePattern — so 'value'
        // answered nothing, 'name' answered the picker's own label, and a round trip comparing either
        // would have held on every machine whatever the picker did.
        new("picked", "which one a container chose, which is a claim about the container and the pair to selected.", read => read.Values.Picked),

        new("expanded", "whether something that opens and closes is open.", read => read.Values.ExpandCollapse),
        new("text", "what a text-editing control holds, through its TextPattern; a label offers none and answers nothing.", read => read.Values.Text),

        // WW238, and it is here because it was measured to be missing. A WPF label was read through
        // the seven above and answered nothing to all of them: its words are in its name, exactly as
        // with a Win32 Static, and a tool whose subject is what a window shows could not check what a
        // label said.
        //
        // Not a pattern reading, like 'focused' below, and null where nothing resolved for the same
        // reason. Not Always either: an element whose name is blank answers nothing, so 'this label
        // says something' stays a claim that can be false.
        // WW83: `nameStarts` pins it too. The decoration behind the prefix is the part a locator did
        // not choose, and no claim on this vocabulary is about a suffix — `answers` holds because a
        // prefix is not empty, and `expect` writes the whole label the locator half-named.
        new("name", "what the element announces itself as, which for a label is the words on it.", read => read.Facts?.Says, pinned: step => step.Name ?? step.NameStarts),

        // WW83. What an element says beside its name, which is where an application puts what it
        // cannot fit in a label — and, where a framework's own accessible object had to be replaced to
        // carry it, the state it stopped exposing as a pattern.
        //
        // Measured missing on claude-tray's tray menu. A checked entry there announces the word for
        // "checked" in front of its own sentence and offers no TogglePattern at all, so 'toggle'
        // answered nothing, 'name' answered the entry's decorated text, and the one check in that
        // application about which profile the icon follows had no reading to make.
        //
        // Not Always: an element with nothing to add says nothing, so "this entry announces something"
        // stays a claim that can be false.
        new("description", "what the element says about itself beyond its name.", read => read.Facts?.Explains),

        // WW325. Whether the control will take input at all, which is the state a half-finished form
        // is supposed to be in — and the one an application gets wrong by leaving a command enabled
        // before its precondition is met.
        //
        // Measured missing on pportal's mapping screen, whose whole third case is that Update stays
        // off until something is bound. No reading answered it: `toggle` is a pattern a Button does
        // not offer, `focused` is about the desk, and `anything` walks the patterns a disabled Button
        // answers through none of. Meanwhile `ElementFacts` had read the property on every look since
        // block A.
        //
        // Not a locator predicate, which is the near miss: a locator selecting only enabled controls
        // makes the disabled case match nothing, and "not there" and "there and greyed" are opposite
        // findings about a form — the line WW318 had just drawn in the other direction.
        //
        // Always, like 'focused' and for its reason: every element that resolved is enabled or is
        // not, so a step claiming this reading *answers* could never be false. Declared here, so the
        // refusal follows the reading rather than being a rule written somewhere else.
        new(
            "enabled",
            "whether the element would take an act at all.",
            read => read.Facts?.IsEnabled switch
            {
                true => "enabled",
                false => "not enabled",
                null => null,
            },
            always: true),

        // WW225. The one reading that is not about a pattern. It is here because "Tab moved the focus
        // off this box" is a claim a case has to be able to make, and it was the one assertion of the
        // keyboard case that could not be written at all — the other two could be written and would
        // have gone through the patterns that passed on the day of the bug.
        //
        // Null where nothing resolved, exactly as the seven above: an element that was not there
        // holds no focus and does not hold it either, and answering "not focused" would be an
        // expectation met by an absence.
        new(
            "focused",
            "whether the keyboard is in this element, which is the one reading that answers for anything that resolved.",
            read => read.Facts?.HasKeyboardFocus switch
            {
                true => "focused",
                false => "not focused",
                null => null,
            },
            always: true),
    ];

    private readonly Func<Reading, string?> reading;
    private readonly Func<LocatorStep, string?>? pinned;

    private ReadBack(
        string name,
        string means,
        Func<Reading, string?> reading,
        bool always = false,
        Func<LocatorStep, string?>? pinned = null)
    {
        Always = always;
        Name = name;
        Means = means;
        this.reading = reading;
        this.pinned = pinned;
    }

    /// <summary>Every reading a case may name, in the order a reader is shown them.</summary>
    public static IReadOnlyList<ReadBack> All { get; } = new ReadOnlyCollection<ReadBack>(Vocabulary);

    /// <summary>The reading a step gets by naming none: whatever the element says it says.</summary>
    public static ReadBack Anything { get; } = Vocabulary[0];

    /// <summary>The name a case writes.</summary>
    public string Name { get; }

    /// <summary>
    /// What this reading is about, in the sentence an author picking between two of them needs.
    /// WW501.
    /// <para>
    /// Twelve words reach a page that publishes the closed list, and nothing beside them said what
    /// any one was. <c>value</c>, <c>text</c> and <c>name</c> are guessable; <c>selected</c> and
    /// <c>picked</c> are not, and they are the pair an author gets wrong — WW266 measured it on a
    /// picker offering no ValuePattern, where <c>value</c> answered nothing and <c>name</c> answered
    /// the picker's own label.
    /// </para>
    /// <para>
    /// Data rather than a comment above the entry, for the reason <see cref="Always"/> is data: the
    /// reasoning in a comment cannot be published and a rename does not move it. Choosing wrong is
    /// not a red that names the mistake — a reading the element does not offer answers null forever,
    /// and the failure sentence says nothing answered to it — so the sentence has to reach the
    /// author before they choose.
    /// </para>
    /// </summary>
    public string Means { get; }

    /// <summary>
    /// Whether this reading answers something for every element that resolved at all.
    /// <para>
    /// True of <c>focused</c> alone, and measured rather than assumed: a label was read through every
    /// reading in this vocabulary and the only one that said anything was that one, with <em>not
    /// focused</em>. Which means a step claiming that reading answers is a step that cannot fail while
    /// the element is there — an unearned green by construction, and it arrived with WW225 and WW237
    /// two tasks apart without either noticing.
    /// </para>
    /// <para>
    /// Data rather than a list somewhere else, so a reading added tomorrow declares this where it is
    /// written and the refusal follows it.
    /// </para>
    /// </summary>
    public bool Always { get; }

    /// <summary>
    /// What <paramref name="step"/> has already fixed this reading to, or null where it has fixed
    /// nothing.
    /// <para>
    /// WW238. Some of what UI Automation says about an element is also what a locator selects by, and
    /// a step reading one of those is at risk of asserting what chose the element: <c>name</c> read off
    /// <c>Text[name="Profile"]</c> can only ever answer <em>Profile</em>, because <see cref="Resolve"/>
    /// matches a name by equality. That is the same unearned green <see cref="Always"/> is about, one
    /// step removed — it depends on the locator rather than on the reading alone.
    /// </para>
    /// <para>
    /// A function rather than a flag because the refusal has to name the value the locator pinned, and
    /// beside the reading rather than in the rule so that a reading added for a property the grammar
    /// also matches on says so where it is written.
    /// </para>
    /// </summary>
    public string? PinnedBy(LocatorStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        return pinned?.Invoke(step);
    }

    /// <summary>
    /// The reading of that name, or a refusal listing the ones there are. Nothing named is
    /// <see cref="Anything"/>, because a case that says only what it expects has said enough.
    /// </summary>
    /// <exception cref="ScenarioRefusedException">Where a name is written and nothing matches.</exception>
    public static ReadBack Named(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Anything;

        var wanted = name.Trim();
        foreach (var candidate in Vocabulary)
            if (string.Equals(candidate.Name, wanted, StringComparison.OrdinalIgnoreCase))
                return candidate;

        throw new ScenarioRefusedException(
            wanted,
            $"there is no such reading; there is {string.Join(", ", Vocabulary.Select(one => one.Name))}");
    }

    /// <summary>
    /// Take this reading off what one look answered.
    /// <para>
    /// The whole look and not its pattern values alone: <c>focused</c> is a property of the element
    /// rather than of a pattern, and the two have to come out of the same look or a case comparing
    /// them is comparing two moments.
    /// </para>
    /// </summary>
    public string? Of(Reading read)
    {
        ArgumentNullException.ThrowIfNull(read);
        return reading(read);
    }
}

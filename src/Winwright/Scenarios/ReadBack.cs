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
        new("anything", read => read.Values.Reading()),
        new("value", read => read.Values.Value),
        new("range", read => read.Values.Range is { } range ? range.ToString(CultureInfo.InvariantCulture) : null),
        new("toggle", read => read.Values.Toggle),
        new("selected", read => read.Values.IsSelected switch
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
        new("picked", read => read.Values.Picked),

        new("expanded", read => read.Values.ExpandCollapse),
        new("text", read => read.Values.Text),

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
        new("name", read => read.Facts?.Says, pinned: step => step.Name ?? step.NameStarts),

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
        new("description", read => read.Facts?.Explains),

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
        Func<Reading, string?> reading,
        bool always = false,
        Func<LocatorStep, string?>? pinned = null)
    {
        Always = always;
        Name = name;
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

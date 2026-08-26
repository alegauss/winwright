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
        new("expanded", read => read.Values.ExpandCollapse),
        new("text", read => read.Values.Text),

        // WW225. The one reading that is not about a pattern. It is here because "Tab moved the focus
        // off this box" is a claim a case has to be able to make, and it was the one assertion of the
        // keyboard case that could not be written at all — the other two could be written and would
        // have gone through the patterns that passed on the day of the bug.
        //
        // Null where nothing resolved, exactly as the seven above: an element that was not there
        // holds no focus and does not hold it either, and answering "not focused" would be an
        // expectation met by an absence.
        new("focused", read => read.Facts?.HasKeyboardFocus switch
        {
            true => "focused",
            false => "not focused",
            null => null,
        }),
    ];

    private readonly Func<Reading, string?> reading;

    private ReadBack(string name, Func<Reading, string?> reading)
    {
        Name = name;
        this.reading = reading;
    }

    /// <summary>Every reading a case may name, in the order a reader is shown them.</summary>
    public static IReadOnlyList<ReadBack> All { get; } = new ReadOnlyCollection<ReadBack>(Vocabulary);

    /// <summary>The reading a step gets by naming none: whatever the element says it says.</summary>
    public static ReadBack Anything { get; } = Vocabulary[0];

    /// <summary>The name a case writes.</summary>
    public string Name { get; }

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

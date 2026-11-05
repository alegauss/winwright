using System.Collections.ObjectModel;

namespace Winwright.RollCall;

/// <summary>
/// What the runs before this one said, which is the only thing that makes this run's numbers worth
/// anything.
/// <para>
/// One type and not three parameters. WW289 added a count, WW298 made it a series, WW299 added a
/// second series and WW248 wants a third reading — each justified where it sits, and four arguments
/// deep <c>Roll.Of</c> would be a signature nobody reads. WW296 is the same shape one floor up: a
/// grammar grows sideways when no single addition is wrong. These are one idea, so they are one
/// argument.
/// </para>
/// <para>
/// Every field is empty where nobody asked or nothing was found, and empty is <em>unknown</em> and
/// never <em>none</em> — the rule the ledger itself is under. Whether anybody asked is
/// <see cref="Roll.Comparing"/>, which is the overload that was called and not a value in here.
/// </para>
/// </summary>
public sealed record Earlier
{
    /// <summary>Nothing known about any earlier run, which a first run on a fresh checkout is.</summary>
    public static readonly Earlier Nothing = new([], [], []);

    /// <summary>What the runs before this one said.</summary>
    /// <param name="excused">How many checks each excused, oldest first.</param>
    /// <param name="discovered">How many cases each discovered, oldest first.</param>
    /// <param name="always">The cases every one of them excused, which is what recurs.</param>
    /// <param name="often">
    /// How often each case was excused across the ledgers on disk. WW363, and defaulted because a
    /// caller that never asked is the state this type has a word for: nothing read, which is not
    /// the same as nothing found.
    /// </param>
    public Earlier(
        IEnumerable<int> excused,
        IEnumerable<int> discovered,
        IEnumerable<string> always,
        HowOften? often = null)
    {
        ArgumentNullException.ThrowIfNull(excused);
        ArgumentNullException.ThrowIfNull(discovered);
        ArgumentNullException.ThrowIfNull(always);

        Excused = new ReadOnlyCollection<int>(excused.ToList());
        Discovered = new ReadOnlyCollection<int>(discovered.ToList());
        Always = new ReadOnlyCollection<string>(always.ToList());
        Often = often ?? HowOften.Nothing;
    }

    /// <summary>
    /// How many checks each of the runs before this one excused, oldest first.
    /// <para>
    /// WW289, WW298. Measured: a guest run passed having excused 49 checks where every run before it
    /// excused 8, because a notification toast held the foreground. Several and not one, so a desk
    /// that stays busy for two runs cannot make the second read as a steady state.
    /// </para>
    /// </summary>
    public IReadOnlyList<int> Excused { get; }

    /// <summary>
    /// How many cases each of the runs before this one discovered, oldest first.
    /// <para>
    /// WW299. The roll weighs discovered against recorded and both come from the same run, so a run
    /// where discovery itself fell short is whole by its own measure.
    /// </para>
    /// </summary>
    public IReadOnlyList<int> Discovered { get; }

    /// <summary>
    /// The cases every one of those runs excused, which is the difference between structure and
    /// circumstance.
    /// <para>
    /// WW248. A dialog this process shows takes the foreground, so a launched fixture in the same
    /// class is left without it and every synthesised act against it is a hole — correctly reported,
    /// for a reason nobody wrote down. What separates that from a desk somebody else was using is not
    /// visible in one run: an excuse that arrives every time is structural, and one run cannot say
    /// every time.
    /// </para>
    /// <para>
    /// Said as what was measured and never as the word "structural". Four runs are what this reads,
    /// and four runs agreeing is evidence a reader weighs rather than a verdict this tool reaches.
    /// </para>
    /// </summary>
    public IReadOnlyList<string> Always { get; }

    /// <summary>
    /// How often each case was excused across the ledgers on disk, and over how many. WW363.
    /// <para>
    /// The reading none of the three above could make. A count compared with the one before it and a
    /// list of what recurs both call a case excused for the first time ordinary — which it is, once.
    /// Five runs excused 8, 8, 8, 9 and 10, each rise a different tray case, and every clause the
    /// report had was true and none of them said the set was growing.
    /// </para>
    /// <para>
    /// Reported and never a threshold, which is the decision this task turned on. Block B's own
    /// criterion is that nothing about the desk is reported as a defect in the code, and a rate that
    /// refused a run would be exactly that — a machine's slow shell making a green go away. The
    /// recurrence reading beside it already says the same thing about its own window: a threshold is
    /// a number somebody tunes the day it refuses. So this is evidence a reader weighs.
    /// </para>
    /// </summary>
    public HowOften Often { get; }
}

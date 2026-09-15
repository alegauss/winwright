namespace Winwright.Typing;

/// <summary>
/// How long a run has to be before any verdict here is worth reading. WW410.
/// <para>
/// Every runner ends by composing a sentence off what it read, and each has a careful arm for a run
/// that saw nothing — <c>sweep</c> says it says nothing at all, <c>provoke</c> says it has no rate
/// for the halves to have inherited, <c>transfer</c> says a row of zeros is the count being too
/// small. Each is right and each is reached only when nothing faulted.
/// </para>
/// <para>
/// So the shape they all had was: one round and no fault reads as a careful sentence, and one round
/// and one fault reads as a confident one. The second is the false green this project's whole
/// verdict block exists to refuse — a rate of one in one, ranked, attributed, and printed under a
/// heading somebody will quote. It is also the more likely of the two to be believed, because it is
/// the one that says something.
/// </para>
/// <para>
/// The floor is where the runners' own arithmetic stops working. Nothing seen in n rounds puts the
/// rate under about 3/n; the rates every arm here is measuring are one to three percent, so below
/// thirty rounds the bound is looser than ten percent and rules out nothing any of them is about.
/// Above it the careful sentences say what they always said.
/// </para>
/// <para>
/// A number and a sentence rather than a switch per runner: the guard belongs at the one place each
/// runner turns readings into prose, and the sentence it prints has to be the same words wherever a
/// reader meets it — which is also what lets one case assert it of every arm at once.
/// </para>
/// <para>
/// WW426: and the floor was the weakest one that would have worked. Every arm is not about one to
/// three percent — <c>transfer</c> is about one in twelve hundred and <c>sweep</c> about WW310's
/// band — so the rate is now declared on each arm's row and the floor derived from it here. Thirty
/// stays for the bare run, and as the least any arm may ask for.
/// </para>
/// </summary>
public static class Enough
{
    /// <summary>Below this many rounds no runner here concludes anything.</summary>
    public const int Rounds = 30;

    /// <summary>The words every runner prints where the run was too short to conclude.</summary>
    public const string TooFew = "too short to conclude anything";

    /// <summary>
    /// How many faults an attribution needs on the side it is leaning on. WW413.
    /// <para>
    /// WW397 measured this guest's floor: four substitutions in 3600 rounds of a control that does
    /// nothing, through a cold boot, twice. A cell of a few hundred rounds therefore expects about
    /// half a fault from the machine alone — so a band of two against nothing, ranked and
    /// attributed, is a shape made of two events either of which the desk supplies.
    /// </para>
    /// <para>
    /// `transfer` refuses at the control, which is the strongest form and needs an arm that does
    /// nothing. `sweep` has none — all three of its arms type — and `provoke` has one and already
    /// asks it. What neither has is this: the counts being large enough for the difference between
    /// them to be the experiment rather than the room.
    /// </para>
    /// </summary>
    public const int Faults = 5;

    /// <summary>
    /// What a run's own control read, which is this desk's floor measured by the run being judged
    /// against it. WW429.
    /// </summary>
    /// <param name="Faulted">How many of the control's rounds substituted.</param>
    /// <param name="Rounds">How many rounds it ran.</param>
    /// <param name="Named">What the arm is called, so the sentence says which reading it used.</param>
    public sealed record DeskFloor(int Faulted, int Rounds, string Named);

    /// <summary>
    /// How many faults an attribution needs on the side it is leaning on, off this desk rather than
    /// off the one the tool was written on. WW429.
    /// <para>
    /// <see cref="Faults" /> is WW397's number: four substitutions in 3600 rounds of this project's
    /// guest, twice, which made five the bar. It is a fact about one machine in a tool an adopter
    /// runs, and the property it is about — what a desk does to a send — is the one most likely to
    /// differ between desks. A machine ten times noisier passes that bar on its own noise; a quiet
    /// one is refused a real reading of four.
    /// </para>
    /// <para>
    /// A run that has a control knows better. A control that faulted nowhere over its own rounds
    /// bounds this desk's rate at about 3 over them — the arithmetic <see cref="Floor(double)"/> is
    /// already built on — so what it can supply to a cell is that bound across the cell's rounds, and
    /// a leading side above it is more than the desk. One that faulted measures the rate rather than
    /// bounding it, and then twice what it supplies is the bar: the counts are the reading either
    /// way, and it is the shape drawn over them that has to clear the machine.
    /// </para>
    /// <para>
    /// Two, at the least, whatever the arithmetic says. A difference of one event is not a shape on
    /// any desk.
    /// </para>
    /// </summary>
    /// <param name="floor">What the run's control read, or null where the run has no control.</param>
    /// <param name="cell">How many rounds the side being leaned on ran.</param>
    public static int Needed(DeskFloor? floor, int cell)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cell);

        if (floor is null || floor.Rounds <= 0)
            return Faults;

        var supplies = floor.Faulted == 0
            ? 3.0 * cell / floor.Rounds
            : 2.0 * floor.Faulted * cell / floor.Rounds;

        return Math.Max(2, (int)Math.Ceiling(supplies - 1e-9));
    }

    /// <summary>
    /// The attribution, or the refusal to make one off counts this small. WW413, and WW429 made the
    /// number this desk's where the run measured one.
    /// </summary>
    /// <param name="leading">How many faults the sentence would be leaning on.</param>
    /// <param name="counted">The counts as the verdict already spells them, so the numbers survive.</param>
    /// <param name="floor">What this run's control read, or null where it has none.</param>
    /// <param name="cell">How many rounds the side being leaned on ran.</param>
    /// <param name="reading">The attribution, deferred.</param>
    public static string Attributed(int leading, string counted, DeskFloor? floor, int cell, Func<string> reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        return leading < Needed(floor, cell) ? TooFewFaults(leading, counted, floor, cell) : reading();
    }

    /// <summary>
    /// The refusal on its own, for a verdict whose branches are further down than the guard. WW413.
    /// </summary>
    /// <param name="leading">How many faults the sentence would have been leaning on.</param>
    /// <param name="counted">The counts as the verdict already spells them.</param>
    /// <param name="floor">What this run's control read, or null where it has none.</param>
    /// <param name="cell">How many rounds the side being leaned on ran.</param>
    public static string TooFewFaults(int leading, string counted, DeskFloor? floor, int cell)
    {
        var needed = Needed(floor, cell);

        // Where the number came from, always: a bar this desk measured and one taken off another
        // machine are different claims, and a reader deciding whether to run it longer needs to know
        // which of them refused them.
        var whose = floor is null || floor.Rounds <= 0
            ? $"This run has no control to measure this desk with, so the bar is WW397's: 4 in 3600"
                + " rounds of a control that does nothing, read on this project's guest twice."
            : floor.Faulted == 0
                ? $"`{floor.Named}` faulted nowhere in {floor.Rounds} round(s) here, which puts this"
                    + $" desk's floor under about 3 in {floor.Rounds} — so {cell} round(s) can carry"
                    + $" about {needed} from the machine alone."
                : $"`{floor.Named}` read {floor.Faulted} of {floor.Rounds} here, so this desk supplies"
                    + $" about {(double)floor.Faulted * cell / floor.Rounds:F1} to {cell} round(s) and"
                    + " a shape has to clear twice that.";

        return $"Too few to attribute: {counted}. {whose} A difference resting on {leading} fault(s)"
            + $" is inside what the machine supplies — the counts are the reading and the shape is"
            + $" not. {needed} on the leading side is where that stops being true.";
    }

    /// <summary>
    /// How many rounds a run needs before a clean one rules out a rate this size, and never fewer than
    /// <see cref="Rounds" />. WW426.
    /// <para>
    /// The shared arithmetic, kept in one place: nothing seen over n rounds puts a rate under about
    /// 3/n, so the rounds that rule out a rate are three over it. What moves from arm to arm is the
    /// rate, which each arm declares against its own measurement — the arithmetic does not move, and
    /// a second spelling of it per runner is how five floors would come to disagree about 3.
    /// </para>
    /// </summary>
    /// <param name="about">The rate, as a fraction of the unit its rounds are counted in.</param>
    /// <exception cref="ArgumentOutOfRangeException">Where the rate is not a fraction above zero.</exception>
    public static int Floor(double about)
    {
        if (!(about > 0 && about <= 1))
        {
            throw new ArgumentOutOfRangeException(
                nameof(about), about, "a rate is a fraction above zero, and a floor for none is every round there is");
        }

        // Less a hair before the ceiling, because a rate declared as one over a count divides back to
        // a count plus the last bit of a double: three over a hundred-and-fiftieth is 450 and a few
        // hundred-quadrillionths, and a floor of 451 is a number nobody argued for.
        return Math.Max(Rounds, (int)Math.Ceiling((3 / about) - 1e-9));
    }

    /// <summary>
    /// The runner's verdict, or the refusal to reach one, against the floor its arm's rate sets. WW426.
    /// <para>
    /// The sentence names the arm's rate rather than the one-to-three percent the shared one says,
    /// because that sentence is false of three arms out of five: a refusal telling a reader what would
    /// have worked has to be about the experiment they ran.
    /// </para>
    /// </summary>
    /// <param name="rounds">How many rounds this run was asked for.</param>
    /// <param name="about">The rate the arm is about, which sets how many rounds it needs.</param>
    /// <param name="reading">The verdict, deferred, and not called below the floor.</param>
    public static string Concluded(int rounds, double about, Func<string> reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        var floor = Floor(about);
        return rounds < floor
            ? $"{rounds} round(s) is {TooFew}: nothing seen over this many puts a rate under about"
                + $" {3.0 / Math.Max(rounds, 1):P0}, and this experiment is about a rate near {about:P1},"
                + $" which only {floor} rounds can rule out. Run it at {floor} rounds or more for a"
                + " sentence, or read this one as proof that the runner ran and nothing else."
            : reading();
    }

    /// <summary>
    /// The bare run's verdict, or the refusal to reach one, against the shared floor.
    /// <para>
    /// WW426 kept this one for the run with no arm, which is the one experiment with no row to put a
    /// rate on: it measures the engine's own send, whose repair fires at one to three percent.
    /// </para>
    /// </summary>
    /// <param name="rounds">How many rounds this run was asked for.</param>
    /// <param name="reading">
    /// The verdict, deferred. Not called below the floor, so a runner cannot spend the work either.
    /// </param>
    public static string Concluded(int rounds, Func<string> reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        return rounds < Rounds
            ? $"{rounds} round(s) is {TooFew}: nothing seen over this many puts a rate under about"
                + $" {3.0 / Math.Max(rounds, 1):P0}, and every arm here is measuring one to three"
                + $" percent. Run it at {Rounds} rounds or more for a sentence, or read this one as"
                + " proof that the runner ran and nothing else."
            : reading();
    }
}

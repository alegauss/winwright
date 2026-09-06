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
/// </summary>
public static class Enough
{
    /// <summary>Below this many rounds no runner here concludes anything.</summary>
    public const int Rounds = 30;

    /// <summary>The words every runner prints where the run was too short to conclude.</summary>
    public const string TooFew = "too short to conclude anything";

    /// <summary>
    /// The runner's verdict, or the refusal to reach one.
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

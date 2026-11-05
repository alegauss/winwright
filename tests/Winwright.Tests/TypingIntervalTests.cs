using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW395. The interval WW381's ladder moves is the engine's own, and it is typed in three places.
/// <para>
/// The <c>guard</c> rung is <c>settle</c> with the engine's pause spent above the send instead of
/// below it, and the whole reading rests on it being the <em>same</em> interval: what the pair
/// answers is where the milliseconds go, and a rung sleeping a different number would be answering
/// how many.
/// </para>
/// <para>
/// The engine's copy is <c>Keys.FirstLookMs</c> and it is internal, so the ladder has a
/// <c>GuardMs</c> of its own and <c>run-typing.cmd</c> says the number in words. Move the engine's
/// and the ladder goes on running, printing rows, and reporting a placement at a price the engine
/// stopped paying — a wrong answer with no red anywhere.
/// </para>
/// <para>
/// Read out of the sources rather than through the field, which is the smaller ask of the two the
/// design named: making the constant visible to this assembly changes the engine's shape for a
/// check, and this suite already holds a probe's list, a fixture's shapes and a runner's arms to
/// each other by reading the files that carry them. It also reaches the third spelling, which no
/// amount of visibility would — the prose is prose.
/// </para>
/// </summary>
public sealed class TypingIntervalTests
{
    /// <summary>The engine's own pause, taken after the send. <c>Keys.FirstLookMs</c>.</summary>
    private static int Engine() => Number(
        Checkout.At("src", "Winwright", "Acting", "Keys.cs"),
        @"FirstLookMs\s*=\s*(\d+)",
        "Keys.FirstLookMs");

    /// <summary>The ladder's copy, spent between the focus and the keys. <c>Transfer.GuardMs</c>.</summary>
    private static int Ladder() => Number(
        Checkout.At("tools", "Winwright.Typing", "Transfer.cs"),
        @"GuardMs\s*=\s*(\d+)",
        "Transfer.GuardMs");

    [Fact]
    public void The_ladder_moves_the_interval_the_engine_actually_pays()
    {
        // The two that matter, in different assemblies with nothing between them until this line.
        // A rung sleeping a different number is not a worse measurement of the same question, it is
        // a measurement of a different one — and the rows it prints look exactly the same.
        Assert.Equal(Engine(), Ladder());
    }

    /// <summary>
    /// What the interval is called in the prose a person reads. WW395.
    /// <para>
    /// One entry, and the absence of a second is the point: a value this has no word for is the
    /// interval having moved without <c>run-typing.cmd</c> being told, which is exactly the state
    /// this case exists to refuse. The failure says so rather than skipping.
    /// </para>
    /// </summary>
    private static readonly Dictionary<int, string> InWords = new() { [50] = "fifty" };

    [Fact]
    public void The_runner_a_person_reads_names_the_same_interval()
    {
        // The third spelling. It is prose and it is what somebody reads before starting a run, so a
        // paragraph describing an interval the tool no longer spends is a reader misled about the
        // one thing the run is varying.
        var engine = Engine();

        Assert.True(
            InWords.TryGetValue(engine, out var said),
            $"the interval is {engine}ms now and this case has no word for it — run-typing.cmd says "
                + "it in prose, so the paragraph about `guard` needs reading before the number moves");

        var runner = File.ReadAllText(Checkout.At("run-typing.cmd"));

        Assert.Contains($"{said} milliseconds", runner, StringComparison.Ordinal);
    }

    /// <summary>
    /// The number that declaration carries, refusing where the reading found none. WW395: a regex
    /// that stopped matching would make both cases above agree about nothing, which is the failure
    /// every sweep in this suite carries a control against.
    /// </summary>
    /// <param name="path">The file to read.</param>
    /// <param name="pattern">What the declaration looks like, with the number as its one group.</param>
    /// <param name="named">What to call it in the refusal.</param>
    private static int Number(string path, string pattern, string named)
    {
        var found = System.Text.RegularExpressions.Regex.Match(File.ReadAllText(path), pattern);

        Assert.True(found.Success, $"{named} was not found in {path}, so nothing here is comparing anything");

        return int.Parse(found.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    }
}

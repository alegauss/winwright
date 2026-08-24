using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW203. A deadline is about the thing it names, and a wait that reaches past that thing is a
/// budget covering work another deadline is separately given more time for.
/// <para>
/// Measured on two guest runs a task apart: 5006ms over 158 looks and 5009ms over 159, both against
/// the 5000ms declared for <c>wrote</c> — and both waits started from a standing start, so that
/// budget was covering a cold start of the fixture as well. <c>draw</c> is given twice as long for
/// the cold start on its own, and says so in as many words.
/// </para>
/// <para>
/// The red said the fixture never wrote what it drew. That is a claim about the application under
/// test, and what had happened is that this suite asked for less time than the work takes — the
/// misattribution Block A's criterion is about, arriving through a number.
/// </para>
/// </summary>
public sealed class WaitedForTests
{
    /// <summary>
    /// The waits that follow a launch and are about a file rather than about the window.
    /// <para>
    /// Named rather than swept. Which wait reaches past its own subject is a judgement about what
    /// the fixture is doing between the launch and the file, and a reading that guessed it would be
    /// guessing at the very thing that has to be decided.
    /// </para>
    /// </summary>
    private static readonly (string File, string Case)[] AfterALaunch =
    [
        ("FixtureTests.cs", "Driven"),
        ("ProvokedByFlagTests.cs", "Ran"),
    ];

    [Fact]
    public void A_wait_for_a_file_the_fixture_writes_waits_for_its_window_first()
    {
        // The repair, read out of the sources. A caller that waits on `wrote` from a standing start
        // is asking one budget to cover a cold start, a layout and a write — which is what went red
        // twice, and what nothing here would have said a word about.
        foreach (var (file, method) in AfterALaunch)
        {
            var body = Body(file, method);

            var draws = body.FindIndex(one => one.Contains("\"draw\"", StringComparison.Ordinal));
            var writes = body.FindIndex(one => one.Contains("\"wrote\"", StringComparison.Ordinal));

            Assert.True(writes >= 0, $"{file}.{method} no longer waits on 'wrote', so this reading is stale");
            Assert.True(
                draws >= 0 && draws < writes,
                $"{file}.{method} waits on 'wrote' without waiting for the window first, so that "
                    + "budget is covering a cold start as well as the write");
        }
    }

    [Fact]
    public void The_cold_start_is_given_longer_than_the_write_that_follows_it()
    {
        // The arithmetic the repair depends on, asserted rather than assumed. A `wrote` wider than
        // `draw` would mean the split had bought nothing, and a `draw` narrowed to `wrote` would
        // put the cold start back inside a budget too small for it.
        var draw = Waits.Declared.For("draw");
        var wrote = Waits.Declared.For("wrote");

        Assert.True(draw > wrote, $"'draw' is {draw}ms and 'wrote' is {wrote}ms, so the cold start has the lesser budget");
    }

    [Fact]
    public void The_reading_finds_a_body_rather_than_an_empty_one()
    {
        // A sweep that read nothing would pass the rule above by finding no lines to disagree with.
        Assert.All(AfterALaunch, one => Assert.NotEmpty(Body(one.File, one.Case)));
    }

    /// <summary>The lines of one method, as code, from its signature to the next member.</summary>
    private static List<string> Body(string file, string method)
    {
        // Spoken and not Code, which this check got wrong on its first run and said so plainly: a
        // deadline is named by a string, and the reading that drops strings had deleted both names
        // and reported that the caller no longer waits at all. WW202 left two readings for exactly
        // this, and the prose either of them drops is what matters here — the comment above each
        // wait names the other deadline.
        var lines = File.ReadLines(Path.Combine(Checkout.Suite, "Winwright.Tests", file))
            .Select(Checkout.Spoken)
            .ToList();

        // The declaration and never a call. A case that calls this method mentions it the same way,
        // and the first draft took the first mention — which handed this the body of whichever case
        // happened to call it first, and reported that the method no longer waits at all.
        var began = lines.FindIndex(one =>
            one.Contains($" {method}(", StringComparison.Ordinal)
            && (one.StartsWith("    private ", StringComparison.Ordinal)
                || one.StartsWith("    internal ", StringComparison.Ordinal)
                || one.StartsWith("    public ", StringComparison.Ordinal)
                || one.StartsWith("    static ", StringComparison.Ordinal)));

        if (began < 0)
            return [];

        var body = new List<string>();
        for (var at = began + 1; at < lines.Count && !lines[at].StartsWith("    }", StringComparison.Ordinal); at++)
            body.Add(lines[at]);

        return body;
    }
}

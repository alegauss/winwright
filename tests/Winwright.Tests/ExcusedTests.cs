using Winwright.RollCall;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW231. <c>BusyDesk.Excused</c> is called at eighty-one sites across twenty classes, and until now
/// it was counted nowhere: an excused case returns, xunit records a pass, and the roll call's
/// arithmetic — discovery against results — balances perfectly. A run on a locked desk reported every
/// case passing.
/// <para>
/// That is the founding defect wearing the other hat. The suite that reported 352 where the run
/// before had 374 at least moved a number; this moved none. And it cost an answer in the session that
/// found it: WW229's two window-driving cases came back green and nothing in the results said whether
/// the positive one proved its claim or excused itself.
/// </para>
/// <para>
/// Three states and not two, which is the part worth checking. A caller that never asked is silent; a
/// caller that asked and found no ledger is told it is unknown; a caller that read one gets the
/// count. Collapsing the first two would put "not read" on the end of every sentence this tool
/// prints, and a clause on every line is a clause nobody reads by the third run.
/// </para>
/// </summary>
public sealed class ExcusedTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-excused-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private static IReadOnlyList<string> One => ["A.B"];

    private static IReadOnlyList<Recorded> Ran => [new("A.B", "Passed", true)];

    [Fact]
    public void A_roll_nobody_asked_about_excuses_says_nothing_about_them()
    {
        var roll = Roll.Of(One, Ran);

        Assert.False(roll.Asked);
        Assert.Null(roll.Holes);
        Assert.Equal("all 1 discovered cases ran.", roll.Sentence());
    }

    [Fact]
    public void A_roll_asked_with_no_ledger_says_it_does_not_know()
    {
        // The distinction this whole tool is about, one level up: not read is not zero, and a green
        // that cannot say how much it covers has to say that instead of implying everything.
        var roll = Roll.Of(One, Ran, null);

        Assert.True(roll.Asked);
        Assert.Null(roll.Holes);
        Assert.Contains("was not read", roll.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_run_that_excused_nothing_says_nothing_extra()
    {
        // Silent on the ordinary case. A clause saying "and none were excused" on every green is one
        // nobody reads by the third run, and then the run that says twelve reads the same.
        var roll = Roll.Of(One, Ran, []);

        Assert.True(roll.Asked);
        Assert.Equal(0, roll.Holes);
        Assert.Equal("all 1 discovered cases ran.", roll.Sentence());
    }

    [Fact]
    public void A_green_that_covers_less_than_it_looks_says_how_much_less()
    {
        var roll = Roll.Of(
            ["A.B", "A.C", "A.D"],
            [new("A.B", "Passed", true), new("A.C", "Passed", true), new("A.D", "Passed", true)],
            ["a foreground to take", "a foreground to take", "a display that renders"]);

        // Still whole, and that is deliberate: an excuse is a hole and never a failure, and a roll
        // that went red over one would have every desk-dependent case turned off inside a week.
        Assert.True(roll.Whole);
        Assert.Equal(3, roll.Holes);

        var said = roll.Sentence();
        Assert.Contains("all 3 discovered cases ran", said, StringComparison.Ordinal);
        Assert.Contains("3 check(s) were excused", said, StringComparison.Ordinal);

        // The conditions and not only the count: three for one absent foreground is a desk somebody
        // was using, three for three different facts is a machine that cannot observe at all, and a
        // reader's next move differs for each. Commonest first.
        Assert.Contains("2 for a foreground to take", said, StringComparison.Ordinal);
        Assert.Contains("1 for a display that renders", said, StringComparison.Ordinal);
        Assert.EndsWith("1 for a display that renders", said.TrimEnd('.'), StringComparison.Ordinal);
    }

    [Fact]
    public void The_excuses_ride_on_a_red_sentence_too()
    {
        // A run that lost cases and excused others owes both numbers. The excuses are not a
        // consolation for the reds and are not hidden behind them.
        var roll = Roll.Of(["A.B", "A.C"], [new("A.B", "Passed", true)], ["a foreground to take"]);

        Assert.False(roll.Whole);

        var said = roll.Sentence();
        Assert.Contains("were never recorded at all", said, StringComparison.Ordinal);
        Assert.Contains("1 check(s) were excused", said, StringComparison.Ordinal);
    }

    [Fact]
    public void A_ledger_that_is_not_there_reads_as_unknown_and_never_as_empty()
    {
        Assert.Null(Readers.ExcusedIn(Path.Combine(root, "never-written.txt")));
    }

    [Fact]
    public void A_ledger_reads_one_condition_per_line_and_ignores_the_blanks()
    {
        var path = Path.Combine(root, "excused.txt");
        File.WriteAllText(path, "a foreground to take\n\n  a display that renders  \n");

        Assert.Equal(["a foreground to take", "a display that renders"], Readers.ExcusedIn(path));
    }

    [Fact]
    public void This_suite_writes_its_own_ledger_where_the_roll_call_is_told_to_look()
    {
        // The two halves are wired by a path in the csproj, so a rename here and a rename there are
        // the one thing nothing else would catch. Touching BusyDesk is what creates it.
        Assert.False(BusyDesk.Excused(Winwright.Verdicts.Precondition.Met(Winwright.Windowing.Desk.InputDesktop)));

        Assert.True(
            File.Exists(BusyDesk.Ledger),
            $"nothing wrote {BusyDesk.Ledger}, so this run's excuses are a number the roll call cannot read");
        Assert.Equal("excused.txt", Path.GetFileName(BusyDesk.Ledger));
    }
}

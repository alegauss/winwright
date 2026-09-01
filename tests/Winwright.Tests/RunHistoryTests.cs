using Winwright.RollCall;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// What the runs before this one said, and the three readings that take from it.
/// <para>
/// WW289 is where it starts. A guest run of 1747 passed having excused 49 checks where every run
/// before it excused 8: a Windows notification toast held the foreground, so forty-three input cases
/// could send nothing and each answered the hole it is built to answer. Everything worked — WW133
/// makes a refused foreground a hole, WW281 puts every excuse in one ledger, and all forty-nine were
/// printed. What was missing is comparison: the number that matters is not 49 but 49-against-8, and
/// a reader told only the first cannot know whether the run they are holding is the ordinary one.
/// </para>
/// <para>
/// WW298 made that a series rather than one difference, so a desk busy for two runs cannot become
/// its own baseline. WW299 reads the same history for what each run discovered, because a suite that
/// quietly stopped loading a class is whole by its own arithmetic. WW248 reads it for which excuses
/// recur, which is the difference between this suite's own structure and a desk somebody else was
/// using.
/// </para>
/// <para>
/// One rule throughout: absent is <em>unknown</em> and never <em>none</em>. A first run reported as
/// zero would read as an improvement on nothing.
/// </para>
/// </summary>
public sealed class RunHistoryTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-before-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    /// <summary>
    /// One excused row in the shape the suite writes it: fact, case, absence, kind, tab-separated.
    /// </summary>
    /// <param name="named">The case the row is about, which is what recurrence is counted by.</param>
    private static string Row(string named) =>
        $"the foreground belongs to the window under test	Winwright.Tests.{named}	something held it	Desk";

    /// <summary>One run's results directory, carrying a ledger of that many excuses.</summary>
    /// <param name="named">What the run is called, which is never what orders it.</param>
    /// <param name="excuses">How many lines its ledger holds.</param>
    /// <param name="written">When it ran, so the ordering is by clock and not by name.</param>
    private string Run(string named, int excuses, DateTime written, int? discovered = null)
    {
        var directory = Directory.CreateDirectory(Path.Combine(root, named)).FullName;
        var ledger = Path.Combine(directory, Readers.Excused);

        File.WriteAllLines(ledger, Enumerable.Range(0, excuses).Select(one => Row($"Case{one}")));
        File.SetLastWriteTimeUtc(ledger, written);

        if (discovered is not { } many)
            return directory;

        // Discovery's own output, in the shape the parser reads: a preamble, then one indented name
        // per case. Written as the real file is rather than as a count, because the count is derived.
        var listing = Path.Combine(directory, Readers.Listing);
        var lines = new List<string> { "Os Testes a seguir estão disponíveis:" };
        lines.AddRange(Enumerable.Range(0, many).Select(one => $"    Winwright.Tests.A.Case{one}"));

        File.WriteAllLines(listing, lines);
        File.SetLastWriteTimeUtc(listing, written);

        return directory;
    }

    [Fact]
    public void The_run_before_is_the_one_that_ran_last_and_never_the_one_that_sorts_last()
    {
        // By write time deliberately. A caller may name its own run — the VM runner does — so sorting
        // the names would compare against whichever directory happens to sort last rather than
        // whichever actually ran before this one.
        Run("zzz-oldest", 3, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Run("aaa-newest", 8, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));
        var mine = Run("mmm-this-run", 49, new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal([3, 8], Readers.ExcusedRecently(root, mine));
    }

    [Fact]
    public void A_run_is_never_its_own_predecessor()
    {
        // The obvious way to get 49-against-49 and report a toast-ridden run as ordinary.
        var mine = Run("only-run", 49, DateTime.UtcNow);

        Assert.Empty(Readers.ExcusedRecently(root, mine));
    }

    [Fact]
    public void A_first_run_has_no_earlier_one_and_that_is_not_zero()
    {
        // A fresh checkout is always this, and reporting it as zero would read a first run as an
        // improvement on nothing — the same collapse between "unknown" and "none" the ledger itself
        // is careful about.
        Assert.Empty(Readers.ExcusedRecently(root, Path.Combine(root, "this-run")));
        Assert.Empty(Readers.ExcusedRecently(Path.Combine(root, "nothing-here"), Path.Combine(root, "this-run")));
    }

    [Fact]
    public void A_run_that_wrote_no_ledger_is_not_the_run_before()
    {
        // A directory holding only a trx is a run whose excuses nobody counted. Comparing against it
        // would put a number on a run that never reported one.
        Directory.CreateDirectory(Path.Combine(root, "no-ledger"));
        Run("with-ledger", 8, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var mine = Run("this-run", 49, DateTime.UtcNow);

        Assert.Equal([8], Readers.ExcusedRecently(root, mine));
    }

    [Fact]
    public void The_sentence_says_the_count_against_the_one_before_it()
    {
        var roll = Roll.Of(
            ["Winwright.Tests.A.One"],
            [new Recorded("Winwright.Tests.A.One", "Passed", true)],
            ["desk|the foreground belongs to the window under test|x"],
            new Earlier([8], [], []));

        var said = roll.Sentence();

        Assert.Contains("1 check(s) were excused against 8 the run before", said, StringComparison.Ordinal);
    }

    [Fact]
    public void A_first_run_says_there_was_nothing_to_compare_with_rather_than_a_number()
    {
        var roll = Roll.Of(
            ["Winwright.Tests.A.One"],
            [new Recorded("Winwright.Tests.A.One", "Passed", true)],
            ["desk|the foreground belongs to the window under test|x"],
            new Earlier([], [], []));

        Assert.Contains("no earlier run", roll.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void Several_earlier_runs_are_said_as_a_series_rather_than_reduced_to_one()
    {
        // WW298's whole point in one reading: the run excused nine, the four before it excused eight
        // apiece bar one that excused forty-three, and a reader sees that without a threshold.
        var roll = Roll.Of(
            ["Winwright.Tests.A.One"],
            [new Recorded("Winwright.Tests.A.One", "Passed", true)],
            ["desk|the foreground belongs to the window under test|x"],
            new Earlier([8, 43, 8, 8], [], []));

        Assert.Contains("where the 4 runs before it excused 8, 43, 8 and 8", roll.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_busy_desk_twice_over_no_longer_reads_as_a_steady_state()
    {
        // The defect WW298 is: with one predecessor this said "43 against 43", which is the tool
        // reporting that nothing changed on the second of two runs a toast had ruined.
        var roll = Roll.Of(
            ["Winwright.Tests.A.One"],
            [new Recorded("Winwright.Tests.A.One", "Passed", true)],
            ["desk|the foreground belongs to the window under test|x"],
            new Earlier([8, 8, 43], [], []));

        var said = roll.Sentence();

        Assert.Contains("8, 8 and 43", said, StringComparison.Ordinal);
        Assert.DoesNotContain("the run before ", said, StringComparison.Ordinal);
    }

    /// <summary>One run whose ledger names exactly these cases.</summary>
    /// <param name="named">What the run is called.</param>
    /// <param name="cases">The cases it excused.</param>
    /// <param name="written">When it ran, which is what orders it.</param>
    private string Excusing(string named, IEnumerable<string> cases, DateTime written)
    {
        var directory = Directory.CreateDirectory(Path.Combine(root, named)).FullName;
        var ledger = Path.Combine(directory, Readers.Excused);

        File.WriteAllLines(ledger, cases.Select(Row));
        File.SetLastWriteTimeUtc(ledger, written);

        return directory;
    }

    [Fact]
    public void An_excuse_every_earlier_run_made_is_told_from_one_that_happened_once()
    {
        // WW248's whole difference. NudgeTests holds a dialog that takes the foreground from the
        // fixture launched beside it, so it is excused every run; a desk somebody else was using is
        // not. Both are honest excuses and only one is the suite's own structure.
        Excusing("first", ["NudgeTests.A_nudge", "PointerTests.A_click"], new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Excusing("second", ["NudgeTests.A_nudge"], new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        Excusing("third", ["NudgeTests.A_nudge"], new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        var mine = Excusing("this-run", ["NudgeTests.A_nudge"], DateTime.UtcNow);

        Assert.Equal(["Winwright.Tests.NudgeTests.A_nudge"], Readers.ExcusedEveryTime(root, mine));
    }

    [Fact]
    public void One_earlier_run_is_not_a_pattern_and_agrees_with_itself()
    {
        // A single run agreeing with itself would make the first coincidence structural, which is the
        // conclusion this reading exists to refuse.
        Excusing("only", ["NudgeTests.A_nudge"], new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var mine = Excusing("this-run", ["NudgeTests.A_nudge"], DateTime.UtcNow);

        Assert.Empty(Readers.ExcusedEveryTime(root, mine));
    }

    [Fact]
    public void A_recurring_excuse_is_marked_with_what_was_read_and_never_with_the_word_structural()
    {
        var roll = Roll.Of(
            ["Winwright.Tests.NudgeTests.A_nudge"],
            [new Recorded("Winwright.Tests.NudgeTests.A_nudge", "Passed", true)],
            [Row("NudgeTests.A_nudge"), Row("PointerTests.A_click")],
            new Earlier([2, 2, 2, 2], [], ["Winwright.Tests.NudgeTests.A_nudge"]));

        var said = roll.ToString();

        Assert.Contains("NudgeTests.A_nudge: the foreground belongs", said, StringComparison.Ordinal);
        Assert.Contains("(in all 4 runs before it)", said, StringComparison.Ordinal);
        Assert.DoesNotContain("structural", said, StringComparison.OrdinalIgnoreCase);

        // Per line because the two excuses differ. Where they do not, the same fact is one clause in
        // the sentence instead — see below.
        Assert.DoesNotContain("none of them is new", said, StringComparison.Ordinal);

        // The one that happened once carries no mark, because a mark on every line marks nothing.
        var once = said.Split('\n').Single(one => one.Contains("PointerTests.A_click", StringComparison.Ordinal));
        Assert.DoesNotContain("runs before it", once, StringComparison.Ordinal);
    }

    [Fact]
    public void Where_every_excuse_recurs_it_is_said_once_and_not_on_every_line()
    {
        // Measured on a guest run of 1815: all eight excuses recurred and all eight carried the mark,
        // because this suite's steady state is that every excuse it makes is its own structure — the
        // cases that make them open a decoy or declare a budget on purpose. A mark on every line
        // marks nothing, so the whole story is told once.
        var roll = Roll.Of(
            ["Winwright.Tests.NudgeTests.A_nudge"],
            [new Recorded("Winwright.Tests.NudgeTests.A_nudge", "Passed", true)],
            [Row("NudgeTests.A_nudge"), Row("PointerTests.A_click")],
            new Earlier(
                [2, 2, 2, 2],
                [],
                ["Winwright.Tests.NudgeTests.A_nudge", "Winwright.Tests.PointerTests.A_click"]));

        var said = roll.ToString();

        Assert.Contains("excused 2, 2, 2 and 2, and none of them is new", said, StringComparison.Ordinal);
        Assert.DoesNotContain("(in all 4 runs before it)", said, StringComparison.Ordinal);
    }

    [Fact]
    public void What_earlier_runs_discovered_is_read_off_the_listing_they_kept()
    {
        Run("older", 8, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), discovered: 1805);
        Run("newer", 8, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), discovered: 1807);
        var mine = Run("this-run", 8, DateTime.UtcNow, discovered: 1807);

        Assert.Equal([1805, 1807], Readers.DiscoveredRecently(root, mine));
    }

    [Fact]
    public void A_run_that_kept_no_listing_is_not_counted_as_having_discovered_nothing()
    {
        // The whole rule this file is under: absent is unknown, and never zero. Counting a run that
        // filed no listing as zero would put a collapse in the series that never happened.
        Run("no-listing", 8, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Run("with-listing", 8, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), discovered: 1805);
        var mine = Run("this-run", 8, DateTime.UtcNow, discovered: 1805);

        Assert.Equal([1805], Readers.DiscoveredRecently(root, mine));
    }

    [Fact]
    public void A_suite_that_quietly_stopped_discovering_600_cases_says_what_the_runs_before_found()
    {
        // WW299 itself: 1204 of 1807, which the roll's own arithmetic calls whole because discovery
        // and recording fell together.
        var roll = Roll.Of(
            Enumerable.Range(0, 1204).Select(one => $"Winwright.Tests.A.Case{one}"),
            Enumerable.Range(0, 1204).Select(one => new Recorded($"Winwright.Tests.A.Case{one}", "Passed", true)),
            excused: null,
            new Earlier([8, 8, 8, 8], [1807, 1807, 1805, 1807], []));

        Assert.Contains(
            "all 1204 discovered cases ran, where the 4 runs before it discovered 1807, 1807, 1805 and 1807",
            roll.Sentence(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_run_that_discovered_what_the_last_one_did_says_nothing_about_discovery()
    {
        // Discovery is meant to hold still, so the change is the news. A series printed beside every
        // run is a clause nobody finishes, and then the one that matters reads the same.
        var roll = Roll.Of(
            ["Winwright.Tests.A.One"],
            [new Recorded("Winwright.Tests.A.One", "Passed", true)],
            excused: null,
            new Earlier([8], [4, 1], []));

        Assert.DoesNotContain("discovered 4", roll.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_suite_that_grew_says_so_as_readily_as_one_that_shrank()
    {
        // Both directions, because a rule that only ever reports bad news gets read as noise.
        var roll = Roll.Of(
            ["Winwright.Tests.A.One", "Winwright.Tests.A.Two"],
            [
                new Recorded("Winwright.Tests.A.One", "Passed", true),
                new Recorded("Winwright.Tests.A.Two", "Passed", true),
            ],
            excused: null,
            new Earlier([8], [1], []));

        Assert.Contains("where the run before discovered 1", roll.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_caller_that_did_not_ask_hears_nothing_about_the_run_before()
    {
        // The third state, and the reason it is not two: a clause about comparison on every run that
        // never asked for one is a clause nobody reads by the third run.
        var roll = Roll.Of(
            ["Winwright.Tests.A.One"],
            [new Recorded("Winwright.Tests.A.One", "Passed", true)],
            ["desk|the foreground belongs to the window under test|x"]);

        Assert.DoesNotContain("the run before", roll.Sentence(), StringComparison.Ordinal);
        Assert.DoesNotContain("no earlier run", roll.Sentence(), StringComparison.Ordinal);
    }
    [Fact]
    public void A_recurring_excuse_that_has_not_said_it_means_it_makes_the_run_red()
    {
        // WW248, and the whole of it. The excuse arrives every run — so it is not the machine having
        // a bad afternoon, it is this suite's own structure holding a check permanently shut — and
        // nothing has written down that it is meant.
        var roll = Roll.Of(
            ["Winwright.Tests.A.One"],
            [new Recorded("Winwright.Tests.A.One", "Passed", true)],
            ["the foreground belongs to the window under test	NudgeTests.A_nudge	another window	Desk	"],
            new Earlier([8, 8], [], ["NudgeTests.A_nudge"]));

        Assert.False(roll.Whole);
        Assert.Equal(["NudgeTests.A_nudge"], roll.Unaccounted);

        // And the red says what to do rather than only what happened.
        var said = string.Join(Environment.NewLine, roll.Render());
        Assert.Contains("has not said it means to be", said, StringComparison.Ordinal);
        Assert.Contains("MeantExcuses.Known", said, StringComparison.Ordinal);
    }

    [Fact]
    public void The_same_excuse_accounted_for_leaves_the_run_whole()
    {
        var roll = Roll.Of(
            ["Winwright.Tests.A.One"],
            [new Recorded("Winwright.Tests.A.One", "Passed", true)],
            ["the foreground belongs to the window under test	MenuTests.A_key	another window	Desk	Meant"],
            new Earlier([8, 8], [], ["MenuTests.A_key"]));

        Assert.True(roll.Whole);
        Assert.Empty(roll.Unaccounted);
    }

    [Fact]
    public void An_excuse_that_does_not_recur_is_circumstance_and_never_refused()
    {
        // The other half of the rule. A desk somebody else was using is a hole, and a hole is not a
        // failure — which is what this tool has said about every excuse since WW231.
        var roll = Roll.Of(
            ["Winwright.Tests.A.One"],
            [new Recorded("Winwright.Tests.A.One", "Passed", true)],
            ["the foreground belongs to the window under test	OnceTests.A_case	another window	Desk	"],
            new Earlier([8, 8], [], ["SomebodyElse.A_case"]));

        Assert.True(roll.Whole);
        Assert.Empty(roll.Unaccounted);
    }

    [Fact]
    public void A_ledger_from_a_build_without_the_column_refuses_nothing()
    {
        // What keeps the first run after this change from refusing the history it compares against.
        var roll = Roll.Of(
            ["Winwright.Tests.A.One"],
            [new Recorded("Winwright.Tests.A.One", "Passed", true)],
            ["the foreground belongs to the window under test	NudgeTests.A_nudge	another window	Desk"],
            new Earlier([8, 8], [], ["NudgeTests.A_nudge"]));

        Assert.True(roll.Whole);
        Assert.Empty(roll.Unaccounted);
    }

}

using Winwright.RollCall;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW117. A test declared a sixteen-byte RECT as an eight-byte long, the call corrupted the stack,
/// and the host died partway through an unrelated class. The runner printed a pass with no
/// failures and a total of 352 where the run before had 374 — twenty-two tests gone, and the only
/// sign was a number nobody had a reason to read.
/// <para>
/// The count is the check. These are about the arithmetic and the two readers, because the crash
/// itself cannot be provoked on demand and does not need to be: what is being proved is that a run
/// short of what it discovered is not called a pass.
/// </para>
/// </summary>
public sealed class RollCallTests
{
    private const string Listing = """
        Execução de teste para D:\Git\alegauss\winwright\tests\Winwright.Tests\bin\Winwright.Tests.dll
        Os Testes a seguir estão disponíveis:
            Winwright.Tests.ActTests.A_toggle_lands
            Winwright.Tests.ActTests.An_invoke_lands
            Winwright.Tests.LabelTests.Every_placeholder(text: "Welcome, {0}")
            Winwright.Tests.LabelTests.Every_placeholder(text: "🗑 Delete")
        """;

    private static string Trx(params (string Name, string Ended)[] results) => Trx("Passed", results);

    private static string Trx(string outcome, params (string Name, string Ended)[] results) =>
        """<?xml version="1.0" encoding="UTF-8"?><TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results>"""
        + string.Concat(results.Select(one =>
            $"""<UnitTestResult testName="{Escaped(one.Name)}" endTime="{one.Ended}" outcome="{outcome}" />"""))
        + "</Results></TestRun>";

    /// <summary>As the real results file writes a name: the quotation marks a theory carries escaped.</summary>
    private static string Escaped(string name) => name.Replace("\"", "&quot;", StringComparison.Ordinal);

    /// <summary>Cases that ran, which is what a plain list of names used to mean.</summary>
    private static Recorded[] Ran(params string[] names) =>
        names.Select(one => new Recorded(one, "Passed", true)).ToArray();

    /// <summary>Cases the run wrote down and never executed.</summary>
    private static Recorded[] Skipped(params string[] names) =>
        names.Select(one => new Recorded(one, "NotExecuted", false)).ToArray();

    private static string Written(string text)
    {
        var path = Path.Combine(Path.GetTempPath(), $"winwright-roll-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, text);
        return path;
    }

    [Fact]
    public void A_run_that_lost_tests_is_short_and_names_how_many_and_where_it_stopped()
    {
        var roll = Roll.Of(["a.one", "a.two", "b.three", "b.four"], Ran("a.one", "a.two"));

        Assert.False(roll.Complete);
        Assert.Equal(2, roll.Absent);
        Assert.Equal(["b.three", "b.four"], roll.Missing.Select(one => one.Method));
        Assert.Equal("a.two", roll.LastAnswered);
        Assert.Contains("2 of 4 were never recorded at all", roll.Sentence());
        Assert.Contains("the last to answer being a.two", roll.Sentence());
    }

    [Fact]
    public void A_run_where_everybody_answered_says_so_and_nothing_else()
    {
        var roll = Roll.Of(["one.a", "two.b"], Ran("two.b", "one.a"));

        Assert.True(roll.Whole);
        Assert.Empty(roll.Unexpected);
        Assert.Equal("all 2 discovered cases ran.", roll.Sentence());
        Assert.Single(roll.Render());
    }

    [Fact]
    public void A_theory_that_lost_one_of_its_rows_is_a_method_that_answered_three_of_four()
    {
        // Counted per method, so losing a single data row is caught without the two tools having
        // to agree on how an argument is spelled.
        var roll = Roll.Of(
            ["a.Every(x: 1)", "a.Every(x: 2)", "a.Every(x: 3)", "a.Every(x: 4)"],
            Ran("a.Every(x: 1)", "a.Every(x: 2)", "a.Every(x: 3)"));

        Assert.Equal(1, roll.Absent);
        Assert.Equal("a.Every ran 3 of 4 cases", Assert.Single(roll.Missing).ToString());
    }

    [Fact]
    public void Two_spellings_of_the_same_case_are_the_same_case()
    {
        // Measured against the real files: the results document writes an emoji as an escape
        // where the listing writes the character, and comparing those texts compares spellings.
        var roll = Roll.Of(
            ["""a.Every(name: "🗑 Delete")"""],
            Ran("""a.Every(name: "\ud83d\uddd1 Delete")"""));

        Assert.True(roll.Whole);
        Assert.Empty(roll.Missing);
        Assert.Empty(roll.Unexpected);
    }

    [Fact]
    public void A_suite_that_discovered_nothing_is_not_a_suite_that_passed()
    {
        // The wipeout case: a build that produced no tests reads as a clean run to anything
        // counting failures, which is the same hole one size larger.
        var roll = Roll.Of([], Ran());

        Assert.False(roll.Whole);
        Assert.Contains("no test at all", roll.Sentence());
    }

    [Fact]
    public void A_method_the_run_recorded_that_discovery_never_found_is_said_rather_than_ignored()
    {
        var roll = Roll.Of(["a.one"], Ran("a.one", "a.ghost"));

        Assert.True(roll.Complete);
        Assert.False(roll.Whole);
        Assert.Equal("a.ghost answered 1 case that discovery never found", Assert.Single(roll.Unexpected).ToString());
        Assert.Contains("answered with cases discovery never found", roll.Sentence());
    }

    [Fact]
    public void Nothing_answering_at_all_is_said_plainly_rather_than_as_a_missing_name()
    {
        var roll = Roll.Of(["a.one", "a.two"], Ran());

        Assert.Null(roll.LastAnswered);
        Assert.Contains("nothing ran at all", roll.Sentence());
        Assert.Equal(["a.one never ran (1 case)", "a.two never ran (1 case)"], roll.Missing.Select(one => one.ToString()));
    }

    [Fact]
    public void The_listing_of_missing_methods_is_bounded_and_says_how_many_it_cut()
    {
        var many = Enumerable.Range(0, 40).Select(index => $"a.test{index}").ToList();

        var rendered = Roll.Of(many, Ran()).Render(most: 5);

        Assert.Equal(7, rendered.Count);
        Assert.Contains("... and 35 more", rendered[^1]);
    }

    [Fact]
    public void Discovery_is_read_by_shape_rather_than_by_a_header_that_is_translated()
    {
        // The banner naming the assembly is not indented and the header is in whatever language
        // the machine is in, so neither is what this reads — and a theory's arguments, which carry
        // spaces and quotation marks, are not read at all.
        var found = Readers.Discovered(Listing);

        Assert.Equal(4, found.Count);
        Assert.Equal("Winwright.Tests.ActTests.A_toggle_lands", found[0]);
        Assert.Contains("""Winwright.Tests.LabelTests.Every_placeholder(text: "Welcome, {0}")""", found);
        Assert.DoesNotContain(found, one => one.Contains("Execução", StringComparison.Ordinal));
    }

    [Fact]
    public void The_results_are_read_in_the_order_they_finished_and_not_in_document_order()
    {
        var path = Written(Trx(
            ("a.second", "2026-08-22T19:02:44.0000000-03:00"),
            ("a.first", "2026-08-22T19:02:43.0000000-03:00")));

        try
        {
            // What the roll wants from this list is which case answered last, and the document's
            // order is the runner's business rather than the run's.
            Assert.Equal(["a.first", "a.second"], Readers.RecordedIn(path).Select(one => one.Name));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A_results_file_the_crash_truncated_is_named_rather_than_thrown_as_a_parser_error()
    {
        // The ordinary case here, not an exceptional one: the run this check exists for is the
        // one that was killed, sometimes while it was writing this very file.
        var path = Written(Trx(("a.one", "2026-08-22T19:02:43.0000000-03:00"))[..80]);

        try
        {
            var refused = Assert.Throws<InvalidDataException>(() => Readers.RecordedIn(path));
            Assert.Contains("killed while writing one", refused.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void The_two_readers_meet_in_the_middle_on_a_run_that_lost_its_host()
    {
        // The whole check end to end, against the shapes the runner really writes: four cases
        // discovered, the host died after the second, and the answer is a red with the count in it.
        var listing = Written(Listing);
        var results = Written(Trx(
            ("Winwright.Tests.ActTests.A_toggle_lands", "2026-08-22T19:02:43.0000000-03:00"),
            ("Winwright.Tests.ActTests.An_invoke_lands", "2026-08-22T19:02:44.0000000-03:00")));

        try
        {
            var roll = Roll.Of(Readers.DiscoveredIn(listing), Readers.RecordedIn(results));

            Assert.False(roll.Whole);
            Assert.Equal(2, roll.Absent);
            Assert.Equal("Winwright.Tests.LabelTests.Every_placeholder", Assert.Single(roll.Missing).Method);
            Assert.Equal("Winwright.Tests.ActTests.An_invoke_lands", roll.LastAnswered);
            Assert.Contains("2 of 4 were never recorded at all", roll.ToString());
        }
        finally
        {
            File.Delete(listing);
            File.Delete(results);
        }
    }

    [Fact]
    public void A_case_recorded_and_never_executed_is_short_rather_than_whole()
    {
        // WW137, the whole of it: every name is present and none of them ran. Counting names would
        // call this whole, for exactly the reason 352 of 374 read as a pass.
        var roll = Roll.Of(["a.one", "a.two"], Skipped("a.one", "a.two"));

        Assert.False(roll.Whole);
        Assert.True(roll.Complete, "the names were all written down, so nothing is absent");
        Assert.Equal(0, roll.Absent);
        Assert.Equal(2, roll.Skipped);
        Assert.Contains("2 of 2 were recorded and never ran", roll.Sentence());
    }

    [Fact]
    public void A_skip_and_a_lost_host_are_kept_apart_rather_than_added()
    {
        // A recorded skip and an executed pass are different facts, and so are a skip and a case
        // the run never wrote down: the reader's next move differs for each.
        var roll = Roll.Of(
            ["a.Every(x: 1)", "a.Every(x: 2)", "b.three"],
            [.. Ran("a.Every(x: 1)"), .. Skipped("a.Every(x: 2)")]);

        Assert.Equal(1, roll.Absent);
        Assert.Equal(1, roll.Skipped);
        Assert.Equal("b.three", Assert.Single(roll.Missing).Method);
        Assert.Equal("a.Every", Assert.Single(roll.Skipping).Method);

        var said = roll.Sentence();
        Assert.Contains("1 of 3 were never recorded at all", said);
        Assert.Contains("1 of 3 were recorded and never ran", said);
    }

    [Fact]
    public void The_line_for_a_method_says_how_many_ran_and_how_many_were_only_written_down()
    {
        var roll = Roll.Of(
            ["a.Every(x: 1)", "a.Every(x: 2)", "a.Every(x: 3)"],
            [.. Ran("a.Every(x: 1)"), .. Skipped("a.Every(x: 2)", "a.Every(x: 3)")]);

        Assert.Equal("a.Every ran 1 of 3 cases, 2 recorded without running", Assert.Single(roll.Skipping).ToString());
    }

    [Fact]
    public void An_outcome_this_reader_has_never_heard_of_counts_as_having_run()
    {
        // The direction that stays honest: a runner naming an outcome this does not know keeps
        // being reported rather than quietly excused.
        var path = Written(Trx("SomethingNew", ("a.one", "2026-08-22T19:02:43.0000000-03:00")));

        try
        {
            Assert.True(Assert.Single(Readers.RecordedIn(path)).Ran);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void The_reader_says_which_outcome_a_case_that_did_not_run_had()
    {
        var path = Written(Trx("NotExecuted", ("a.one", "2026-08-22T19:02:43.0000000-03:00")));

        try
        {
            var recorded = Assert.Single(Readers.RecordedIn(path));

            Assert.False(recorded.Ran);
            Assert.Equal("NotExecuted", recorded.Outcome);
            Assert.Equal("a.one (NotExecuted)", recorded.ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A_roll_that_could_not_be_taken_exits_differently_from_one_that_found_nothing_wrong()
    {
        // The one thing that must not happen: a check that could not run reading as a check that
        // ran and was happy. Three exit codes, and zero is only ever the last of them.
        Assert.Equal(Program.Unreadable, Program.Main(["--results", "nowhere.trx"]));
        Assert.Equal(
            Program.Unreadable,
            Program.Main(["--discovered", "no-such-listing.txt", "--results", "no-such.trx"]));
        Assert.NotEqual(0, Program.Short);
    }

    [Fact]
    public void The_exit_code_is_zero_only_where_every_discovered_case_answered()
    {
        var listing = Written(Listing);
        var whole = Written(Trx(
            ("Winwright.Tests.ActTests.A_toggle_lands", "2026-08-22T19:02:43.0000000-03:00"),
            ("Winwright.Tests.ActTests.An_invoke_lands", "2026-08-22T19:02:44.0000000-03:00"),
            ("""Winwright.Tests.LabelTests.Every_placeholder(text: "Welcome, {0}")""", "2026-08-22T19:02:45.0000000-03:00"),
            ("""Winwright.Tests.LabelTests.Every_placeholder(text: "\ud83d\uddd1 Delete")""", "2026-08-22T19:02:46.0000000-03:00")));
        var short_ = Written(Trx(("Winwright.Tests.ActTests.A_toggle_lands", "2026-08-22T19:02:43.0000000-03:00")));

        // WW137: every name written down and none of them executed. The exit code used to restate
        // the rule rather than ask the roll for it, so when the roll learned that a recorded skip
        // is not an answer, the number it left behind went on saying it was.
        var skipped = Written(Trx(
            "NotExecuted",
            ("Winwright.Tests.ActTests.A_toggle_lands", "2026-08-22T19:02:43.0000000-03:00"),
            ("Winwright.Tests.ActTests.An_invoke_lands", "2026-08-22T19:02:44.0000000-03:00"),
            ("""Winwright.Tests.LabelTests.Every_placeholder(text: "Welcome, {0}")""", "2026-08-22T19:02:45.0000000-03:00"),
            ("""Winwright.Tests.LabelTests.Every_placeholder(text: "🗑 Delete")""", "2026-08-22T19:02:46.0000000-03:00")));

        try
        {
            Assert.Equal(0, Program.Main(["--discovered", listing, "--results", whole]));
            Assert.Equal(Program.Short, Program.Main(["--discovered", listing, "--results", short_]));
            Assert.Equal(Program.Short, Program.Main(["--discovered", listing, "--results", skipped]));
        }
        finally
        {
            File.Delete(listing);
            File.Delete(whole);
            File.Delete(short_);
            File.Delete(skipped);
        }
    }
}

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

    /// <summary>
    /// One excused row as the ledger writes one: the fact, the case, what was absent, and the kind.
    /// WW389, and spelled here because every reading below is about rows and none of them could be
    /// driven before: this class had no case that made the roll say anything about an excuse at all.
    /// </summary>
    /// <param name="named">The case the row names.</param>
    /// <param name="kind">Whether the desk or a budget this suite chose.</param>
    private static string Row(string named, string kind = Readers.Desk) =>
        $"the foreground belongs to the window under test\t{named}\tsomething else owns it\t{kind}";

    /// <summary>What the runs before this one said, built for one reading at a time. WW389.</summary>
    /// <param name="always">The cases every earlier run excused.</param>
    /// <param name="often">How many of the deeper window's runs excused each case.</param>
    /// <param name="ledgers">How many runs that deeper window read.</param>
    private static Earlier Before(
        IEnumerable<string> always, (string Case, int Times)[]? often = null, int ledgers = 20) =>
        new(
            [8, 8, 8, 8],
            [2000, 2000, 2000, 2000],
            always,
            often is null
                ? null
                : new HowOften(ledgers, often.ToDictionary(one => one.Case, one => one.Times, StringComparer.Ordinal)));

    /// <summary>
    /// One run built to make each reading speak, and the text it speaks in. WW389.
    /// <para>
    /// A recipe a row rather than one run for all of them, because several of these are silent
    /// exactly where another is speaking — that is the composition the list states, so a single run
    /// could never make more than a few of them talk at once.
    /// </para>
    /// <para>
    /// The text is the whole sentence for a reading that lands there and one excused line for a
    /// reading that lands on one. Per line and not per report, which a run got wrong first: the
    /// precedence between the two line readings is about one case's line, and a report carrying
    /// another case's rate reads as both firing when neither line does.
    /// </para>
    /// </summary>
    private static IReadOnlyDictionary<string, string> Speaking() =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // WW298's series: four runs of eight against this run's one, which is what a series is.
            ["the series"] = Sentence(["a.one"], Before(always: [])),

            // WW248: every excuse recurring in every earlier run, which is this suite's normal run.
            // Given a rate as well, so what silences WW376 here is the precedence and not an absence.
            ["none of them is new"] = Sentence(["a.one"], Before(always: ["a.one"], often: [("a.one", 17)])),

            // WW376: not every one recurs, so the stronger clause is quiet and this counts the set
            // against the deeper window instead.
            ["how many the ledgers have seen"] =
                Sentence(["a.one", "a.two"], Before(always: ["a.one"], often: [("a.one", 9), ("a.two", 4)])),

            // WW281: one of each kind, which is the only shape that divides them.
            ["the desk against a budget"] =
                Sentence(["a.one", "a.two"], Before(always: []), budget: "a.two"),

            // WW248 again, on the line: one case recurs and one does not, so the run is not
            // everywhere and the mark divides the rows. Both carry a rate, so this line being
            // without one is WW363 giving way rather than nothing to give.
            ["in all N runs before it"] = LineFor(
                "a.one", ["a.one", "a.two"], Before(always: ["a.one"], often: [("a.one", 17), ("a.two", 3)])),

            // WW363: the same run's other line, which recurs nowhere and has a rate to report.
            ["excused in N of the last M"] = LineFor(
                "a.two", ["a.one", "a.two"], Before(always: ["a.one"], often: [("a.one", 17), ("a.two", 3)])),
        };

    [Fact]
    public void Every_reading_of_the_excuses_speaks_where_the_list_says_it_does()
    {
        // WW389. Five readings come off one set of ledgers and each knew when to be quiet only
        // because whoever added it had read the ones already there — WW376's rule cites WW363's,
        // which cites WW248's. Nothing composed them, and nothing drove any of them: this class had
        // no case that made the roll say a word about an excuse, so a clause that stopped firing
        // stopped in silence. WW363's rate did exactly that for a whole session.
        //
        // So each row of the list gets a run built to make it speak, and the marker it declares is
        // what is looked for. A reading that goes quiet fails here rather than in a report nobody
        // can tell was short.
        var said = Speaking();

        Assert.All(
            Readings.All,
            one => Assert.True(
                said.TryGetValue(one.Named, out var read) && read.Contains(one.Marker, StringComparison.Ordinal),
                $"nothing here makes '{one.Named}' ({one.Task}) speak, and it declares it says"
                    + $" '{one.Marker}' on {one.Where}: {said.GetValueOrDefault(one.Named, "<no run built for it>")}"));
    }

    [Fact]
    public void A_reading_the_list_calls_the_quieter_one_is_silent_where_the_louder_speaks()
    {
        // WW389, and the half the list exists for. Two clauses that both fire say the same thing
        // twice, which is the failure WW363 wrote its own rule against — and every one of those
        // rules lives where it was added, so the next reading's author has to find all of them
        // before deciding their own. This is that composition, read back.
        //
        // Read off the run built to make the LOUDER one speak, which is the only run the precedence
        // is about — and the precedence chains, so a run built for the quieter one is often a run
        // where the louder is itself outranked by something else. That is how this first went red:
        // one excused case that recurs makes `none of them is new` speak, which silences the line
        // mark that was supposed to be doing the silencing.
        var said = Speaking();
        var pairs = Readings.All.Where(one => one.Louder.Length > 0).ToList();
        Assert.NotEmpty(pairs);

        foreach (var quieter in pairs)
        {
            var louder = Readings.Named(quieter.Louder);
            Assert.True(
                louder is not null,
                $"'{quieter.Named}' says it is silenced by '{quieter.Louder}', which is no reading here");

            var read = said[louder!.Named];

            Assert.Contains(louder.Marker, read, StringComparison.Ordinal);
            Assert.False(
                read.Contains(quieter.Marker, StringComparison.Ordinal),
                $"'{quieter.Named}' says '{louder.Named}' silences it and both are in: {read}");
        }
    }

    /// <summary>
    /// The two shapes this suite's own runs actually take, and what each is called. WW407.
    /// <para>
    /// Every excuse here is the suite's own structure — a case opens a decoy on purpose, or declares
    /// a budget — so a run either repeats the whole set or repeats most of it. Both were read off
    /// guest runs an hour apart rather than reasoned about.
    /// </para>
    /// </summary>
    private static IReadOnlyDictionary<string, Roll> Ordinary() =>
        new Dictionary<string, Roll>(StringComparer.Ordinal)
        {
            // One of each kind, which is not decoration: every guest run of this suite excuses some
            // checks for the desk and some against a budget it declared, and a shape carrying only
            // one kind silences WW281 — which is how this pair was found to be a model of a run
            // nobody has rather than of the run everybody reads.
            //
            // Everything recurs, which is the run a person reads most weeks: the sentence carries
            // the fact once and each line carries its own depth.
            ["every excuse recurring"] = Rolled(
                ["a.one", "a.two"],
                Before(always: ["a.one", "a.two"], often: [("a.one", 20), ("a.two", 19)]),
                budget: "a.two"),

            // One of them missed a run, which is the other shape and the more common of the two on
            // a desk that is doing anything else. Here the two line readings divide the rows.
            ["one of them not"] = Rolled(
                ["a.one", "a.two"],
                Before(always: ["a.one"], often: [("a.one", 20), ("a.two", 18)]),
                budget: "a.two"),
        };

    [Fact]
    public void Every_reading_speaks_on_a_run_of_the_shape_this_suite_actually_produces()
    {
        // WW407. WW389 proved each reading can speak, by building the run that makes it — which is
        // the right check and answers a different question from this one. A reading can be perfectly
        // reachable and never reached: WW363's rate was silent for a whole session under the clause
        // above it, and what made that invisible was that nothing looked at an ordinary run.
        //
        // So this asks the ordinary question instead. Between the two shapes a run of this suite
        // takes, every reading has to say its piece somewhere — a reading that only speaks in a run
        // nobody has is a reading nobody reads.
        var reports = Ordinary().ToDictionary(
            one => one.Key,
            one => string.Join(Environment.NewLine, one.Value.Render()),
            StringComparer.Ordinal);

        var quiet = Readings.All
            .Where(one => !reports.Values.Any(read => read.Contains(one.Marker, StringComparison.Ordinal)))
            .ToList();

        Assert.True(
            quiet.Count == 0,
            $"{quiet.Count} reading(s) say nothing on either shape this suite's runs take, so they are "
                + "correct and unread: "
                + string.Join("; ", quiet.Select(one => $"'{one.Named}' ({one.Task}) wants '{one.Marker}'")));
    }

    [Fact]
    public void The_two_ordinary_shapes_are_told_apart_by_which_line_reading_speaks()
    {
        // The control for the case above, which would pass on two runs that were the same run. What
        // divides them is the precedence WW363 declares: where every excuse recurs the fact is said
        // once in the sentence and every line carries its rate, and where one does not the mark
        // moves down on to the lines that earned it and takes those rates with it.
        var ordinary = Ordinary();

        var whole = string.Join(Environment.NewLine, ordinary["every excuse recurring"].Render());
        var divided = string.Join(Environment.NewLine, ordinary["one of them not"].Render());

        Assert.Contains("none of them is new", whole, StringComparison.Ordinal);
        Assert.DoesNotContain(" (in all ", whole, StringComparison.Ordinal);
        Assert.Contains("(excused in 20 of the last 20 runs)", whole, StringComparison.Ordinal);

        Assert.DoesNotContain("none of them is new", divided, StringComparison.Ordinal);
        Assert.Contains(" (in all ", divided, StringComparison.Ordinal);
        Assert.Contains("(excused in 18 of the last 20 runs)", divided, StringComparison.Ordinal);
    }

    [Fact]
    public void A_row_the_ledger_cannot_place_moves_the_mark_on_to_every_line_and_takes_the_rates()
    {
        // WW407, and the regime the rate really did go quiet in. `Everywhere` is false where any row
        // names no case — a row it cannot place makes "every excuse recurred" a claim nobody can
        // make — and that is a fact about the ledger rather than about recurrence. What follows is
        // that the mark drops on to the lines, and the mark silences the rate on each one it takes.
        //
        // So a report can carry no rate at all while every clause in it is right, which is what a
        // session of them looked like. It is the precedence working and it is worth being able to
        // point at: an older build's ledger is enough to make a whole reading disappear.
        var roll = Roll.Of(
            ["a.one", "a.two"],
            Ran("a.one", "a.two"),
            [Row("a.one"), "the foreground belongs to the window under test\t\tsomething else owns it"],
            Before(always: ["a.one"], often: [("a.one", 17)]));

        var report = string.Join(Environment.NewLine, roll.Render());

        Assert.Contains(" (in all ", report, StringComparison.Ordinal);
        Assert.DoesNotContain("excused in", report, StringComparison.Ordinal);
        Assert.DoesNotContain("none of them is new", report, StringComparison.Ordinal);
    }

    /// <summary>The roll's one sentence for a run that excused these cases. WW389.</summary>
    /// <param name="excused">The cases this run excused.</param>
    /// <param name="earlier">What the runs before it said.</param>
    /// <param name="budget">One of them that was a budget this suite chose rather than the desk.</param>
    private static string Sentence(string[] excused, Earlier earlier, string? budget = null) =>
        Rolled(excused, earlier, budget).Sentence();

    /// <summary>
    /// The one excused line naming that case. WW389, and one line rather than all of them: the two
    /// line readings compose per case, so a report carrying another case's rate would read as both
    /// firing where neither did on the line in question.
    /// </summary>
    /// <param name="named">The case whose line is wanted.</param>
    /// <param name="excused">The cases this run excused.</param>
    /// <param name="earlier">What the runs before it said.</param>
    private static string LineFor(string named, string[] excused, Earlier earlier) =>
        Assert.Single(
            Rolled(excused, earlier, budget: null).Render(),
            one => one.StartsWith($"  excused   {named}:", StringComparison.Ordinal));

    /// <summary>A whole run whose only interesting fact is what it excused. WW389.</summary>
    /// <param name="excused">The cases this run excused.</param>
    /// <param name="earlier">What the runs before it said.</param>
    /// <param name="budget">One of them that was a budget rather than the desk.</param>
    private static Roll Rolled(string[] excused, Earlier earlier, string? budget) =>
        Roll.Of(
            excused,
            Ran(excused),
            excused.Select(one => Row(one, one == budget ? Readers.Budget : Readers.Desk)),
            earlier);

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

    /// <summary>
    /// Take the roll into a pair of writers of this case's own, and hand back both.
    /// <para>
    /// WW149. Every case here used to let the entry point write to the console it found, so a real
    /// run carried these sentences above its own — two verdicts about different runs, in the one
    /// output a reader is meant to be able to act on without opening the log.
    /// </para>
    /// </summary>
    /// <param name="args">What the entry point is being given.</param>
    private static (int Code, string Said, string Wrong) Taking(params string[] args)
    {
        using var said = new StringWriter();
        using var wrong = new StringWriter();

        var code = Program.Take(args, said, wrong);
        return (code, said.ToString(), wrong.ToString());
    }

    [Fact]
    public void A_roll_that_could_not_be_taken_exits_differently_from_one_that_found_nothing_wrong()
    {
        // The one thing that must not happen: a check that could not run reading as a check that
        // ran and was happy. Three exit codes, and zero is only ever the last of them.
        var missingArguments = Taking("--results", "nowhere.trx");
        Assert.Equal(Program.Unreadable, missingArguments.Code);

        var missingFiles = Taking("--discovered", "no-such-listing.txt", "--results", "no-such.trx");
        Assert.Equal(Program.Unreadable, missingFiles.Code);
        Assert.NotEqual(0, Program.Short);

        // And it says which of the two it was, on the stream a reader looks at for a refusal. A
        // check that could not be taken and one that was taken and was happy differ by these words
        // as well as by the code, and nothing checked the words at all before this.
        Assert.Contains("usage: Winwright.RollCall", missingArguments.Wrong);
        Assert.Contains("the roll could not be taken", missingFiles.Wrong);
        Assert.Empty(missingArguments.Said);
        Assert.Empty(missingFiles.Said);
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
            var answered = Taking("--discovered", listing, "--results", whole);
            var lost = Taking("--discovered", listing, "--results", short_);
            var kept = Taking("--discovered", listing, "--results", skipped);

            Assert.Equal(0, answered.Code);
            Assert.Equal(Program.Short, lost.Code);
            Assert.Equal(Program.Short, kept.Code);

            // Which stream carries the verdict is the code said a second way, and it is the half a
            // reader acts on: a whole roll is an answer and a short one is a refusal.
            Assert.Equal("all 4 discovered cases ran.", answered.Said.Trim());
            Assert.Empty(answered.Wrong);

            Assert.Empty(lost.Said);
            Assert.Contains("3 of 4 were never recorded at all", lost.Wrong);

            // WW137 said in the words as well as in the number: every name written down, none of
            // them run, and the sentence a reader gets says recorded rather than missing.
            Assert.Empty(kept.Said);
            Assert.Contains("4 of 4 were recorded and never ran", kept.Wrong);
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

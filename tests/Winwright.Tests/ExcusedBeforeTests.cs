using Winwright.RollCall;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW289. A guest run of 1747 passed having excused 49 checks where every run before it excused 8: a
/// Windows notification toast held the foreground, so forty-three input cases could send nothing and
/// each answered the hole it is built to answer.
/// <para>
/// Everything worked. WW133 makes a refused foreground a hole, WW281 puts every excuse in one ledger,
/// and all forty-nine were printed. What was missing is comparison — the number that matters is not
/// 49 but 49-against-8, and a reader told only the first has no way to know whether the run they are
/// holding is the ordinary one.
/// </para>
/// </summary>
public sealed class ExcusedBeforeTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-before-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    /// <summary>One run's results directory, carrying a ledger of that many excuses.</summary>
    /// <param name="named">What the run is called, which is never what orders it.</param>
    /// <param name="excuses">How many lines its ledger holds.</param>
    /// <param name="written">When it ran, so the ordering is by clock and not by name.</param>
    private string Run(string named, int excuses, DateTime written)
    {
        var directory = Directory.CreateDirectory(Path.Combine(root, named)).FullName;
        var ledger = Path.Combine(directory, Readers.Excused);

        File.WriteAllLines(ledger, Enumerable.Range(0, excuses).Select(one => $"desk|a fact|{one}"));
        File.SetLastWriteTimeUtc(ledger, written);

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

        Assert.Equal(8, Readers.ExcusedBefore(root, mine));
    }

    [Fact]
    public void A_run_is_never_its_own_predecessor()
    {
        // The obvious way to get 49-against-49 and report a toast-ridden run as ordinary.
        var mine = Run("only-run", 49, DateTime.UtcNow);

        Assert.Null(Readers.ExcusedBefore(root, mine));
    }

    [Fact]
    public void A_first_run_has_no_earlier_one_and_that_is_not_zero()
    {
        // A fresh checkout is always this, and reporting it as zero would read a first run as an
        // improvement on nothing — the same collapse between "unknown" and "none" the ledger itself
        // is careful about.
        Assert.Null(Readers.ExcusedBefore(root, Path.Combine(root, "this-run")));
        Assert.Null(Readers.ExcusedBefore(Path.Combine(root, "nothing-here"), Path.Combine(root, "this-run")));
    }

    [Fact]
    public void A_run_that_wrote_no_ledger_is_not_the_run_before()
    {
        // A directory holding only a trx is a run whose excuses nobody counted. Comparing against it
        // would put a number on a run that never reported one.
        Directory.CreateDirectory(Path.Combine(root, "no-ledger"));
        Run("with-ledger", 8, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var mine = Run("this-run", 49, DateTime.UtcNow);

        Assert.Equal(8, Readers.ExcusedBefore(root, mine));
    }

    [Fact]
    public void The_sentence_says_the_count_against_the_one_before_it()
    {
        var roll = Roll.Of(
            ["Winwright.Tests.A.One"],
            [new Recorded("Winwright.Tests.A.One", "Passed", true)],
            ["desk|the foreground belongs to the window under test|x"],
            before: 8);

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
            before: null);

        Assert.Contains("no earlier run", roll.Sentence(), StringComparison.Ordinal);
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
}

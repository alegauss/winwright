using Winwright.RollCall;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW248. The catalogue of excuses this suite means, checked the way every other catalogue here is.
/// <para>
/// The reading it protects lives in the roll call, which runs after the host and reads the ledger.
/// What is checked here is the half a person maintains: that the list is well formed, that it says
/// something about each entry, and that the ledger this run wrote carries the column the roll reads.
/// Whether a recurring excuse is accounted for is the roll's arithmetic, and it is tested there.
/// </para>
/// </summary>
public sealed class MeantExcusesTests
{
    [Fact]
    public void Every_entry_names_a_case_and_says_why_it_means_it()
    {
        Assert.NotEmpty(MeantExcuses.Known);

        Assert.All(MeantExcuses.Known, one =>
        {
            // TypeTests.Method_name, which is what the ledger writes and what the roll compares
            // against. An entry spelled any other way accounts for nothing and reads as though it did.
            Assert.Contains('.', one.Case);
            Assert.DoesNotContain(' ', one.Case);

            // A reason and not a restatement. The rule this list exists under is that intent is
            // written down, and "it is excused" is not a reason for being excused.
            Assert.True(one.Because.Length > 40, $"{one.Case} says too little to be an account: {one.Because}");
            Assert.DoesNotContain("excused every run", one.Because, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void No_case_is_accounted_for_twice()
    {
        var named = MeantExcuses.Known.Select(one => one.Case).ToList();

        Assert.Equal(named.Count, named.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void The_ledger_this_run_writes_carries_the_column_the_roll_reads()
    {
        // The join. The catalogue is only worth anything if the answer reaches the file, and the two
        // halves live in different assemblies — so a rename here and a reader there would drift in
        // silence, which is the failure this whole ledger exists to stop.
        //
        // Composed rather than written. The first version of this appended a real row, and the run
        // then reported a fabricated hole in its own arithmetic and raised the count every later run
        // is compared against: a check that dirties the reading it is about is checking the wrong
        // thing.
        var accounted = Excuses.Row(ExcusedBy.Desk, "a foreground to take", MeantExcuses.Known[0].Case, "somebody else has it");
        var not = Excuses.Row(ExcusedBy.Desk, "a foreground to take", "NobodyTests.Accounts_for_this", "somebody else has it");

        Assert.True(Readers.Accounted(accounted));
        Assert.False(Readers.Accounted(not));

        // And the columns before it still say what they said, because appending one must not move
        // the four the roll already reads.
        Assert.Equal(MeantExcuses.Known[0].Case, Readers.Excuse(accounted).Case);
        Assert.Equal("a foreground to take", Readers.Excuse(accounted).Fact);
        Assert.Equal(Readers.Desk, Readers.Excuse(accounted).Kind);
    }

    [Fact]
    public void A_row_from_a_build_that_had_no_such_column_is_unknown_rather_than_unaccounted()
    {
        // The half that keeps the first run after this change from refusing the whole history it is
        // comparing against. Where a missing kind was the answer, a missing account is not.
        Assert.Null(Readers.Accounted("a fact\tSomeTests.Some_case\tan absence\tDesk"));
        Assert.False(Readers.Accounted("a fact\tSomeTests.Some_case\tan absence\tDesk\t"));
        Assert.True(Readers.Accounted("a fact\tSomeTests.Some_case\tan absence\tDesk\tMeant"));
    }
}

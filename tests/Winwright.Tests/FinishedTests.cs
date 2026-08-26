using Winwright.InApp;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW218. Both single-shot writers in the in-app half truncated the file they were about to fill, so
/// a harness in another process could see a file that was there and held nothing. WW164 taught the
/// reading side to distrust that, and it still cost two guest runs a red at the boundary of a
/// five-second budget — a sentence about the application under test arriving through a number the
/// suite chose.
/// <para>
/// What is asserted here is the property that replaces the distrust: an existing file is a finished
/// file, because the content is filled beside the name and moved over it.
/// </para>
/// </summary>
public sealed class FinishedTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-finished-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private string At(string name) => Path.Combine(root, name);

    [Fact]
    public void The_content_is_written_beside_the_name_and_never_into_it()
    {
        // The whole claim, taken from inside the write: at the moment the writer is filling its
        // file, the name a harness is watching does not exist at all.
        var destination = At("dump.tsv");
        var existed = true;

        Finished.Writing(destination, beside =>
        {
            existed = File.Exists(destination);
            Assert.EndsWith(Finished.Suffix, beside, StringComparison.Ordinal);
            File.WriteAllText(beside, "one\ttwo\n");
        });

        Assert.False(existed, "the destination existed while it was still being filled");
        Assert.Equal("one\ttwo\n", File.ReadAllText(destination));
    }

    [Fact]
    public void An_existing_file_is_never_the_empty_half_of_a_replacement()
    {
        // The failure this closes, read the other way: overwriting used to truncate first, so a
        // reader looking in the gap found the file it was waiting for holding nothing.
        var destination = At("dump.tsv");
        File.WriteAllText(destination, "the older and longer dump\nwith two lines\n");

        Finished.Writing(destination, beside =>
        {
            Assert.Equal("the older and longer dump\nwith two lines\n", File.ReadAllText(destination));
            File.WriteAllText(beside, "new\n");
        });

        Assert.Equal("new\n", File.ReadAllText(destination));
    }

    [Fact]
    public void Nothing_is_left_beside_the_name_once_the_write_has_landed()
    {
        var destination = At("dump.tsv");

        Finished.Writing(destination, beside => File.WriteAllText(beside, "one\n"));

        Assert.Empty(Directory.GetFiles(root, $"*{Finished.Suffix}"));
    }

    [Fact]
    public void A_write_that_threw_leaves_neither_a_sibling_nor_a_truncated_destination()
    {
        // A stale sibling is a file an operator would open expecting the dump, and a truncated
        // destination is the very confusion this exists to end.
        var destination = At("dump.tsv");
        File.WriteAllText(destination, "what was there before\n");

        Assert.Throws<InvalidOperationException>(() => Finished.Writing(destination, beside =>
        {
            File.WriteAllText(beside, "half of something\n");
            throw new InvalidOperationException("the walk gave up");
        }));

        Assert.Equal("what was there before\n", File.ReadAllText(destination));
        Assert.Empty(Directory.GetFiles(root, $"*{Finished.Suffix}"));
    }

    [Fact]
    public void The_directory_is_made_where_the_harness_named_one_that_is_not_there_yet()
    {
        var destination = At(Path.Combine("reports", "run-1", "dump.tsv"));

        Finished.Writing(destination, beside => File.WriteAllText(beside, "one\n"));

        Assert.Equal("one\n", File.ReadAllText(destination));
    }

    [Fact]
    public async Task A_replacement_lands_through_a_reader_that_briefly_holds_the_destination_open()
    {
        // The one thing the sibling costs: a replace needs delete access to the target, and a reader
        // polling the file has it open. The move is attempted more than once for exactly that.
        var destination = At("dump.tsv");
        File.WriteAllText(destination, "older\n");

        using var holding = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var reader = Task.Run(() =>
        {
            using var open = File.Open(destination, FileMode.Open, FileAccess.Read, FileShare.Read);
            holding.Set();
            release.Wait(5000);
        });

        Assert.True(holding.Wait(5000), "the reader never opened the file");
        try
        {
            // Released on a timer rather than on the move landing, because the move is what is being
            // measured: it has to get through a collision that is still in progress when it starts.
            _ = Task.Delay(Finished.BetweenMs * 2).ContinueWith(_ => release.Set(), TaskScheduler.Default);
            Finished.Writing(destination, beside => File.WriteAllText(beside, "newer\n"));
        }
        finally
        {
            // Whatever the move did, the handle goes: a reader left holding it would fail the
            // tear-down and report this case as a directory that would not delete.
            release.Set();
            await reader;
        }

        Assert.Equal("newer\n", File.ReadAllText(destination));
        Assert.True(Finished.Attempts > 1, "one attempt is not a retry");
    }

    [Fact]
    public void A_name_nobody_gave_is_refused_rather_than_written_somewhere()
    {
        Assert.Throws<ArgumentException>(() => Finished.Writing("  ", _ => { }));
        Assert.Throws<ArgumentNullException>(() => Finished.Writing(At("dump.tsv"), null!));
    }
}

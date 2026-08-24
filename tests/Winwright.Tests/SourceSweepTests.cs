using System.Collections.ObjectModel;

using Xunit;

namespace Winwright.Tests;

/// <summary>Why a sweep over this suite's own C# sources need not read them as code.</summary>
internal enum Sweeping
{
    /// <summary>It is the reading itself, so asking it to use the reading is circular.</summary>
    TheReading,

    /// <summary>What it looks for cannot appear in a comment or a string, so prose cannot supply it.</summary>
    Unmistakable,
}

/// <summary>One sweep that reads C# and does not go through <see cref="Checkout.Code" />.</summary>
/// <param name="File">The file, by name.</param>
/// <param name="Kind">Why it does not have to.</param>
/// <param name="Because">The sentence a reader needs.</param>
internal sealed record SourceSweep(string File, Sweeping Kind, string Because)
{
    public override string ToString() => $"{Kind,-13} {File}: {Because}";
}

/// <summary>
/// WW202. Four sweeps in this suite read what somebody wrote about a call as the call itself, each
/// found by a red and repaired on its own — WW191 in <c>DeskAsks</c>, WW197 in <c>Flattening</c> and
/// again on a doc comment, WW198 in <c>Sleeps</c> and in <c>SerialCollectionTests</c>. Three more
/// still matched raw text when this was written, and one of them was <c>Deadlines</c>: the sibling
/// of the catalogue repaired one task earlier, written in the same shape and left with the same
/// defect.
/// <para>
/// Nothing was miscounted, which is what made it a task rather than a red. The next catalogue entry
/// explaining itself in prose is what breaks a count, and the reader meets it as arithmetic failing
/// in a file nobody edited.
/// </para>
/// <para>
/// So the rule holds rather than being remembered, which is WW190's shape one floor down. A sweep is
/// found by what it walks — this suite's own C# — and a sweep that walks it without the one reading
/// is red here until somebody says why prose cannot reach it.
/// </para>
/// </summary>
public sealed class SourceSweepTests
{
    /// <summary>
    /// What walking C# sources looks like, however it is spelled: the shared walk, or an
    /// enumeration naming a C# pattern. Matched over the whole file rather than line by line,
    /// because <c>Checkout</c> and <c>.SourcesIn(</c> are usually on two lines and a per-line reader
    /// saw neither — which is how the first draft of this missed three of the sweeps it governs.
    /// </summary>
    private static bool Walks(string text)
    {
        // Whitespace taken out rather than lines joined, which is not the same thing: `Checkout`
        // and `.SourcesIn(` sit on two lines with an indent between them, and a reader that only
        // joined the lines still saw a newline where the dot should be. That missed three of the
        // sweeps this governs, including the one the task was written about.
        var squashed = new string(text.Where(one => !char.IsWhiteSpace(one)).ToArray());

        return squashed.Contains("Checkout.Sources", StringComparison.Ordinal)
            || (squashed.Contains("EnumerateFiles(", StringComparison.Ordinal)
                && squashed.Contains(".cs\"", StringComparison.Ordinal));
    }

    /// <summary>
    /// Either reading. The invariant is that prose is not code, which is the half they agree on;
    /// whether the strings go too is the sweep's own business, and a sweep looking for a name inside
    /// a string would be handed an empty line by the stricter one.
    /// </summary>
    private static readonly string[] Reads = ["Checkout.Code", "Checkout.Spoken"];

    /// <summary>The ones that walk it and read it raw, with why prose cannot reach them.</summary>
    private static IReadOnlyList<SourceSweep> Excused { get; } = new ReadOnlyCollection<SourceSweep>(
    [
        new($"{nameof(Checkout)}.cs", Sweeping.TheReading,
            "it is the walk and the reading both — asking the file that defines Code to call Code is "
                + "circular, and it matches nothing in what it walks: it hands the files on"),
    ]);

    /// <summary>Every file that walks C# sources, read with its strings kept.</summary>
    private static IReadOnlyList<string> Sweeps() => Checkout
        .SourcesIn(Checkout.Suite, except: $"{nameof(SourceSweepTests)}.cs")
        .Where(one => Walks(Spoken(one)))
        .Select(Path.GetFileName)
        .OfType<string>()
        .OrderBy(one => one, StringComparer.Ordinal)
        .ToList();

    private static string Spoken(string file) =>
        string.Join('\n', File.ReadLines(file).Select(Checkout.Spoken));

    private static bool ReadsAsCode(string named) => Reads.Any(one => File
        .ReadLines(Path.Combine(Checkout.Suite, "Winwright.Tests", named))
        .Any(line => line.Contains(one, StringComparison.Ordinal)));

    [Fact]
    public void Every_sweep_over_this_suites_sources_reads_them_as_code()
    {
        var excused = Excused.Select(one => one.File).ToHashSet(StringComparer.Ordinal);

        var raw = Sweeps().Where(one => !excused.Contains(one) && !ReadsAsCode(one)).ToList();

        Assert.True(
            raw.Count == 0,
            $"{raw.Count} sweep(s) walk this suite's C# and match it raw, so a comment or a string "
                + $"naming what they look for is counted as the thing: {string.Join(", ", raw)}");
    }

    [Fact]
    public void Nothing_is_excused_that_no_longer_sweeps_or_now_reads_as_code()
    {
        var sweeping = Sweeps().ToHashSet(StringComparer.Ordinal);

        // Both directions. An exception left standing after the file it names started reading as
        // code is a reason nobody needs, and the next reader takes the list for the state of things.
        var stale = Excused
            .Where(one => !sweeping.Contains(one.File) || ReadsAsCode(one.File))
            .ToList();

        Assert.True(
            stale.Count == 0,
            $"{stale.Count} excuse(s) name a file that no longer sweeps, or that now reads as code: "
                + string.Join(", ", stale.Select(one => one.File)));
    }

    [Fact]
    public void The_reading_finds_the_sweeps_it_was_written_about()
    {
        // A sweep that found nothing would pass the rule above by arithmetic. Named at both ends:
        // the twin WW202 was really about, and a file that reads the roadmap rather than any source.
        var found = Sweeps();

        Assert.Contains($"{nameof(Deadlines)}.cs", found, StringComparer.Ordinal);
        Assert.Contains($"{nameof(Sleeps)}.cs", found, StringComparer.Ordinal);
        Assert.Contains($"{nameof(DeskAsks)}.cs", found, StringComparer.Ordinal);

        Assert.DoesNotContain($"{nameof(Criteria)}.cs", found, StringComparer.Ordinal);
    }

    [Fact]
    public void Every_excuse_says_something_and_names_a_file_once()
    {
        var named = Excused.Select(one => one.File).ToList();

        Assert.Equal(named.Count, named.Distinct(StringComparer.Ordinal).Count());
        Assert.All(Excused, one => Assert.False(string.IsNullOrWhiteSpace(one.Because)));
    }

    [Fact]
    public void The_two_readings_are_offered_because_one_of_them_deletes_what_a_sweep_looks_for()
    {
        // Why Spoken exists beside Code. A sweep looking for a call wants the strings gone; a sweep
        // looking for a file pattern is looking for a string, and Code would hand it an empty line.
        const string walking = """        .EnumerateFiles(tree, "*.cs", SearchOption.AllDirectories) // every source""";

        Assert.DoesNotContain("*.cs", Checkout.Code(walking), StringComparison.Ordinal);
        Assert.Contains("*.cs", Checkout.Spoken(walking), StringComparison.Ordinal);

        // And both drop the comment, which is the half they agree on.
        Assert.DoesNotContain("every source", Checkout.Code(walking), StringComparison.Ordinal);
        Assert.DoesNotContain("every source", Checkout.Spoken(walking), StringComparison.Ordinal);
    }
}

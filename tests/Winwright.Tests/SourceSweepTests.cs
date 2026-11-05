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

    /// <summary>
    /// Whether the member opens what it walked, which is what makes it a sweep at all.
    /// <para>
    /// WW206. A member that walks and hands the files on cannot misread them — <c>Checkout.Sources</c>
    /// and <c>FixtureNeedsTests.Sources</c> both do exactly that. Asking those to read as code was
    /// asking the wrong question, and the first draft of this answered it with two hand-written
    /// excuses rather than by narrowing what a sweep is.
    /// </para>
    /// </summary>
    private static bool Opens(string text) =>
        text.Contains("File.ReadLines", StringComparison.Ordinal)
        || text.Contains("File.ReadAllText", StringComparison.Ordinal)
        || text.Contains("File.ReadAllLines", StringComparison.Ordinal);

    /// <summary>
    /// The ones that walk it, open it and match it raw, with why prose cannot reach them.
    /// <para>
    /// Empty, and that is the reading rather than an absence. Every sweep in this suite reads what it
    /// walks as code; the list is kept so the day one cannot is the day somebody writes down why,
    /// which is the argument <c>Without.NotYet</c> makes about its own empty bucket.
    /// </para>
    /// </summary>
    private static IReadOnlyList<SourceSweep> Excused { get; } =
        new ReadOnlyCollection<SourceSweep>([]);

    /// <summary>
    /// Every member that walks C# sources, and whether that member reads what it walks as code.
    /// <para>
    /// WW206 moved the unit here. It used to ask the question of a file — does this file walk
    /// sources, and does it mention a reading anywhere in it — and a file may hold two sweeps.
    /// <c>FixtureNeedsTests</c> holds exactly two, WW202 repaired one and left the other reading
    /// raw, and the check called the file clean because the repair was somewhere in it. That is
    /// WW197's finding in a different file: a rule keyed on a mention credits the whole for one part.
    /// </para>
    /// </summary>
    private static IReadOnlyList<(string Named, bool AsCode)> Sweeps() => Checkout
        .SourcesIn(Checkout.Suite, except: $"{nameof(SourceSweepTests)}.cs")
        .SelectMany(InFile)
        .ToList();

    private static IEnumerable<(string Named, bool AsCode)> InFile(string file)
    {
        var owner = Path.GetFileNameWithoutExtension(file);
        var bodies = Members(File.ReadLines(file).Select(Checkout.Spoken));

        // The walk and the reading are often two members, and either can be the one that calls. A
        // case may ask a helper for the files and open them itself, or a walker may hand each file
        // to a reader. WW202's check saw one file and asked one question; splitting by member showed
        // the halves, and a rule that demanded both in one member would have found neither.
        var walkers = bodies.Where(one => Walks(one.Value)).Select(one => one.Key).ToList();

        return bodies
            .Where(one => Opens(one.Value))
            .Where(one => Walks(one.Value)
                || walkers.Any(walk => one.Value.Contains(walk, StringComparison.Ordinal))
                || walkers.Any(walk => bodies[walk].Contains(one.Key, StringComparison.Ordinal)))
            .Select(one => ($"{owner}.{one.Key}", Reads.Any(read => one.Value.Contains(read, StringComparison.Ordinal))));
    }

    /// <summary>Each member of one file, with the lines under it.</summary>
    private static Dictionary<string, string> Members(IEnumerable<string> lines)
    {
        var found = new Dictionary<string, string>(StringComparer.Ordinal);
        var member = "";
        var body = new List<string>();

        foreach (var line in lines)
        {
            if (Checkout.Member(line) is { } next)
            {
                Close();
                member = next;

                // The declaring line belongs to the member too. An expression-bodied one carries its
                // whole body there — `Scan() => Checkout` with `.SourcesIn(` underneath — and a
                // reader that started below it saw the call and never the thing it was called on.
                body.Add(line);
            }
            else if (member.Length > 0)
            {
                body.Add(line);
            }
        }

        Close();
        return found;

        void Close()
        {
            if (member.Length > 0)
                found[member] = string.Join('\n', body);

            member = "";
            body = [];
        }
    }

    [Fact]
    public void Every_sweep_over_this_suites_sources_reads_them_as_code()
    {
        var excused = Excused.Select(one => one.File).ToHashSet(StringComparer.Ordinal);

        var raw = Sweeps()
            .Where(one => !one.AsCode && !excused.Contains(one.Named))
            .Select(one => one.Named)
            .ToList();

        Assert.True(
            raw.Count == 0,
            $"{raw.Count} sweep(s) walk this suite's C# and match it raw, so a comment or a string "
                + $"naming what they look for is counted as the thing: {string.Join(", ", raw)}");
    }

    [Fact]
    public void Nothing_is_excused_that_no_longer_sweeps_or_now_reads_as_code()
    {
        var sweeping = Sweeps().ToDictionary(one => one.Named, one => one.AsCode, StringComparer.Ordinal);

        // Both directions. An exception left standing after the sweep it names started reading as
        // code is a reason nobody needs, and the next reader takes the list for the state of things.
        var stale = Excused
            .Where(one => !sweeping.TryGetValue(one.File, out var code) || code)
            .ToList();

        Assert.True(
            stale.Count == 0,
            $"{stale.Count} excuse(s) name a sweep that no longer walks, or that now reads as code: "
                + string.Join(", ", stale.Select(one => one.File)));
    }

    [Fact]
    public void The_reading_finds_the_sweeps_it_was_written_about()
    {
        // A sweep that found nothing would pass the rule above by arithmetic. Named at both ends:
        // the twin WW202 was really about, and a file that reads the roadmap rather than any source.
        var found = Sweeps().Select(one => one.Named).ToList();

        Assert.Contains($"{nameof(Deadlines)}.Scan", found, StringComparer.Ordinal);
        Assert.Contains($"{nameof(Sleeps)}.Scan", found, StringComparer.Ordinal);
        Assert.Contains($"{nameof(DeskAsks)}.InFile", found, StringComparer.Ordinal);

        Assert.DoesNotContain(found, one => one.StartsWith($"{nameof(Criteria)}.", StringComparison.Ordinal));
    }

    [Fact]
    public void A_file_holding_two_sweeps_is_two_sweeps_here()
    {
        // The whole of WW206, as arithmetic. FixtureNeedsTests walks the sources twice — once to
        // assert the fixture reaches for nothing, once as the control asserting the same reading
        // finds plenty in the engine — and the rule above used to credit both to whichever of them
        // had been repaired.
        var itsOwn = Sweeps()
            .Where(one => one.Named.StartsWith($"{nameof(FixtureNeedsTests)}.", StringComparison.Ordinal))
            .ToList();

        Assert.True(itsOwn.Count > 1, $"{nameof(FixtureNeedsTests)} holds {itsOwn.Count} sweep(s), which is unexpected");
        Assert.All(itsOwn, one => Assert.True(one.AsCode, one.Named));
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

using System.Text.RegularExpressions;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW431. Every case marked as needing no desk, held to that.
/// <para>
/// The mark is what lets the gate answer a case inside a serial class, and the failure it could
/// produce is the one WW417 says the gate must never produce: a case run on the host that wanted a
/// desk is a red about the host. So the claim is checked rather than trusted, in the two places it
/// can be false — the case reaching for the desk itself, and the class it sits in building
/// something that does, which xUnit does for every case it runs.
/// </para>
/// </summary>
public sealed class NoDeskTests
{
    /// <summary>How the mark is spelled, as the sources spell it.</summary>
    private const string Marked = "[Trait(NoDesk.Key, NoDesk.Free)]";

    /// <summary>Every marked case in this suite, with the file and the class it is in.</summary>
    private static IReadOnlyList<(string File, string Owner, string Case, string Body)> Marks() =>
        Marks(Checkout.SourcesIn(Checkout.Suite, except: $"{nameof(NoDeskTests)}.cs"));

    /// <summary>
    /// The same over files a caller names, which is what lets the reading be driven rather than
    /// only run. WW445: every check here starts from this, so a shape it cannot see is a marked case
    /// held to nothing — and a sample is the only way to prove it sees one.
    /// </summary>
    /// <param name="files">The sources to read.</param>
    private static IReadOnlyList<(string File, string Owner, string Case, string Body)> Marks(
        IEnumerable<string> files)
    {
        var found = new List<(string, string, string, string)>();

        foreach (var file in files)
        {
            // Read as code, which is this suite's rule for any sweep over its own sources and is
            // what makes the mark a mark: prose about the attribute — this file's own paragraphs
            // about it, or a comment beside a case explaining why it is not marked — would otherwise
            // read as a case carrying it.
            var text = string.Join('\n', File.ReadLines(file).Select(Checkout.Code));
            if (!text.Contains(Marked, StringComparison.Ordinal))
                continue;

            var owner = Path.GetFileNameWithoutExtension(file);

            foreach (var member in Checkout.Members(file))
            {
                // The mark sits above the declaration, so the member's own body does not carry it:
                // what says a case is marked is the text between the case before it and its own
                // opening line, which is where the attribute is.
                //
                // WW445. That opening line used to be spelled here as `public void <name>(`, and a
                // case is not only ever that: `public async Task` is a case xUnit runs and VSTest
                // filters exactly like any other, and this saw none of them — so one could carry the
                // mark, be gated, run on somebody's desk and be held to nothing by all three checks
                // below. A sweep that misses a case reports a clean pass over the ones it did find,
                // which is this project's own definition of an unearned green, and what it would be
                // clean about is the one failure WW417 says the gate must never produce.
                //
                // The member's own first line instead, which is what `Checkout.Members` opened it on:
                // one reading of what a declaration looks like, in the place that already had it.
                var declaration = member.Body.Split('\n')[0];
                var declared = text.IndexOf(declaration, StringComparison.Ordinal);
                if (declared < 0)
                    continue;

                var above = text[..declared];
                var lastMark = above.LastIndexOf(Marked, StringComparison.Ordinal);
                if (lastMark < 0)
                    continue;

                // Nothing but the attribute lines and the doc comment may stand between them, or the
                // mark belongs to the case before this one.
                var between = above[(lastMark + Marked.Length)..];
                if (Regex.IsMatch(between, @"^[\s]*(?:\[[^\]]*\]\s*|///[^\r\n]*\r?\n\s*)*$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)))
                    found.Add((file, owner, $"{owner}.{member.Name}", member.Body));
            }
        }

        return found;
    }

    [Fact]
    public void A_case_is_found_by_being_one_and_not_by_how_it_gives_its_answer()
    {
        // WW445. The reading is driven over a file written for it, because the miss cannot be shown
        // any other way: a shape this sweep cannot see is invisible to every check below, and to a
        // suite that has no case of that shape today. Found while writing WW436's cases — the first
        // draft was `public async Task`, because its bound was a task, and the mark on it would have
        // been checked by nothing.
        // In a directory of its own so the file can carry the name the reading uses as the owner:
        // a case is named for its file here, which is what a sweep over sources has instead of a
        // type. Not under the suite's own tree — a file there would be swept by every rule in this
        // suite, and this one is written to be wrong in ways those rules exist to refuse.
        var directory = Directory.CreateTempSubdirectory("winwright-ww445-");
        var sample = Path.Combine(directory.FullName, "SampleTests.cs");
        File.WriteAllText(sample, $$"""
            namespace Winwright.Tests;

            public sealed class SampleTests
            {
                [Fact]
                [Trait(NoDesk.Key, NoDesk.Free)]
                public void A_marked_case_that_answers_directly()
                {
                }

                [Fact]
                [Trait(NoDesk.Key, NoDesk.Free)]
                public async Task A_marked_case_that_answers_with_a_task()
                {
                    await Task.Yield();
                }

                [Fact]
                public void A_case_nobody_marked()
                {
                }
            }
            """);

        try
        {
            var marked = Marks([sample]).Select(one => one.Case).ToList();

            Assert.Contains("SampleTests.A_marked_case_that_answers_directly", marked);
            Assert.Contains("SampleTests.A_marked_case_that_answers_with_a_task", marked);
            Assert.DoesNotContain("SampleTests.A_case_nobody_marked", marked);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Something_is_marked_at_all()
    {
        // The control on every check below: a sweep that found nothing would report a clean suite
        // about a mark nobody uses, and the gate would go on waiting for the guest in silence.
        var marks = Marks();

        Assert.NotEmpty(marks);
        Assert.Contains(
            marks,
            one => one.Case == "DeskProbeTests.The_session_probe_is_the_one_call_that_waits_for_a_guest_to_finish_logging_in");
    }

    [Fact]
    public void No_marked_case_reaches_for_the_desk()
    {
        var reaching = new List<string>();

        foreach (var mark in Marks())
        {
            var code = string.Join('\n', mark.Body.Split('\n').Select(Checkout.Code));

            reaching.AddRange(NoDesk.Reaching
                .Where(one => code.Contains(one, StringComparison.Ordinal))
                .Select(one => $"{mark.Case} reaches for {one}"));
        }

        Assert.True(
            reaching.Count == 0,
            $"{reaching.Count} marked case(s) reach for the desk, and the gate would run them on the "
                + $"host where that is a red about the host: {string.Join("; ", reaching)}");
    }

    [Fact]
    public void No_class_holding_a_marked_case_builds_anything_that_needs_a_desk()
    {
        // xUnit builds the class for every case it runs, so a field or a constructor that opens a
        // window makes every case in it a desk case whatever its own body says. NotificationAreaTests
        // is exactly that: it adds a tray icon before any case runs, so the case in it that reads the
        // skill cannot be marked until it moves.
        var building = new List<string>();

        foreach (var owner in Marks().Select(one => one.File).Distinct(StringComparer.Ordinal))
        {
            var text = File.ReadAllText(owner);
            var opens = text.IndexOf("public sealed class ", StringComparison.Ordinal);
            var first = text.IndexOf("    [Fact]", StringComparison.Ordinal);

            if (opens < 0 || first < 0 || first < opens)
                continue;

            var fixture = string.Join('\n', text[opens..first].Split('\n').Select(Checkout.Code));

            building.AddRange(NoDesk.Reaching
                .Where(one => fixture.Contains(one, StringComparison.Ordinal))
                .Select(one => $"{Path.GetFileNameWithoutExtension(owner)} builds {one}"));
        }

        Assert.True(
            building.Count == 0,
            $"{building.Count} class(es) holding a marked case build something that needs a desk "
                + $"before any case runs: {string.Join("; ", building)}");
    }

    [Fact]
    public void No_case_that_only_reads_the_checkout_sits_in_a_class_that_acts_before_it()
    {
        // WW443. The mark cannot reach a case whose class acts in its fixture — xUnit builds that
        // class for every case it runs, so a case reading a file would add a tray icon to the
        // operator's shell before reading a byte, and `No_class_holding_a_marked_case_builds_...`
        // above refuses the mark for exactly that. The refusal is right and leaves the case in the
        // guest: eleven minutes for a reading the gate answers in seconds, which is the run WW431
        // was filed over.
        //
        // So the rule is the other side of that one. A case that reads this checkout and reaches for
        // nothing has no business in a class that acts: it belongs where its subject is, in a class
        // that needs nothing. Two moved under WW443 — the skill's tray sentence to `SkillTests`, the
        // fixture's themed-control check to `FixtureNeedsTests` — and both had a home already.
        //
        // Narrow on purpose. A case that reads a file *and* drives something is where it belongs,
        // which is why reaching for anything at all takes it out of this list.
        var stranded = new List<string>();

        foreach (var file in Checkout.SourcesIn(Checkout.Suite, except: $"{nameof(NoDeskTests)}.cs"))
        {
            var text = string.Join('\n', File.ReadLines(file).Select(Checkout.Code));
            if (!text.Contains(nameof(WindowFixture.Serial), StringComparison.Ordinal))
                continue;

            // What the class does before any case runs: its fields and its constructor, which is
            // the region that decides whether a case in it can ever be answered on the host.
            var opens = text.IndexOf("public sealed class ", StringComparison.Ordinal);
            var first = text.IndexOf("    [Fact]", StringComparison.Ordinal);
            if (opens < 0 || first < 0 || first < opens)
                continue;

            if (!NoDesk.Reaching.Any(one => text[opens..first].Contains(one, StringComparison.Ordinal)))
                continue;

            foreach (var member in Checkout.Members(file))
            {
                // A case and not a helper, asked of the assembly rather than of the source: a
                // private reader that walks the checkout is how half the cases in a class get their
                // file, and moving one of those moves nothing.
                var named = $"{Path.GetFileNameWithoutExtension(file)}.{member.Name}";
                if (Provocation.CaseNamed(named) is not { } found || !Provocation.IsACase(found))
                    continue;

                var code = string.Join('\n', member.Body.Split('\n').Select(Checkout.Code));

                if (code.Contains($"{nameof(Checkout)}.", StringComparison.Ordinal)
                    && !NoDesk.Reaching.Any(one => code.Contains(one, StringComparison.Ordinal)))
                {
                    stranded.Add(named);
                }
            }
        }

        Assert.True(
            stranded.Count == 0,
            $"{stranded.Count} case(s) read this checkout and reach for nothing, in classes that act "
                + "before any case runs — so the gate can never answer them and a guest run is what "
                + $"says they are red: {string.Join(", ", stranded.Order(StringComparer.Ordinal))}");
    }

    [Fact]
    public void The_gate_takes_the_marked_cases_as_well_as_the_classes()
    {
        // The mark is worth nothing if the filter does not carry it, and that is invisible: the gate
        // would go on passing, on fewer cases than its reader thinks.
        var gate = File.ReadAllText(Checkout.At("tools", "host-gate.ps1"));

        Assert.Contains($"{NoDesk.Key}={NoDesk.Free}", gate, StringComparison.Ordinal);
    }
}

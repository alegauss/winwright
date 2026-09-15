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

    /// <summary>Every marked case, with the file and the class it is in.</summary>
    private static IReadOnlyList<(string File, string Owner, string Case, string Body)> Marks()
    {
        var found = new List<(string, string, string, string)>();

        foreach (var file in Checkout.SourcesIn(Checkout.Suite, except: $"{nameof(NoDeskTests)}.cs"))
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
                var declared = text.IndexOf($"public void {member.Name}(", StringComparison.Ordinal);
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
    public void The_gate_takes_the_marked_cases_as_well_as_the_classes()
    {
        // The mark is worth nothing if the filter does not carry it, and that is invisible: the gate
        // would go on passing, on fewer cases than its reader thinks.
        var gate = File.ReadAllText(Checkout.At("tools", "host-gate.ps1"));

        Assert.Contains($"{NoDesk.Key}={NoDesk.Free}", gate, StringComparison.Ordinal);
    }
}

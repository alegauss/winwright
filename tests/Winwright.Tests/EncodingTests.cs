using System.Text;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW284. That no tracked text file carries text which was UTF-8 once, read as a codepage, and
/// written back as UTF-8.
/// <para>
/// Six em-dashes in this repository were spelled as three characters each, four in a test's comments
/// and two in <c>Act.cs</c> - one of those on a public member, so it was in the XML documentation the
/// package ships. Nothing catches that. The compiler does not read a comment, the concordance reads
/// sources for structure rather than prose, and one wrong character in a doc comment is exactly the
/// size of thing a person skims past. They were found by a tool in this session making the mistake,
/// and then by looking for whether anything else already had it.
/// </para>
/// <para>
/// The check is the inverse of the damage rather than a list of the shapes it takes. Encode the text
/// back to the codepage it would have been misread as; if those bytes are themselves valid UTF-8 for
/// something shorter, the text is what that shorter thing became. Prose does not do this by accident
/// - the Portuguese for "overview" encodes to bytes whose 0xE3 is followed by an ASCII letter, which
/// is not a UTF-8 sequence at all, so this repository's language files pass. A rule banning
/// non-ASCII could never have done that.
/// </para>
/// <para>
/// A line at a time, and not a file at a time. Whole-file was the first draft and it hid the thing
/// it was for: one character anywhere that cannot round-trip makes the whole file come back invalid,
/// so real damage on line 12 is masked by a Portuguese string on line 400. This file proved it by
/// being that file.
/// </para>
/// <para>
/// Every non-ASCII character here is written as an escape, deliberately. A check on encoding that
/// carried its own non-ASCII would be one more file for the next tool to damage, its own literals
/// would be findings, and its failure would look exactly like the thing it exists to find.
/// </para>
/// </summary>
public sealed class EncodingTests
{
    /// <summary>
    /// The thirty-two positions where Windows-1252 and Latin-1 disagree: 0x80 to 0x9F, which Latin-1
    /// leaves as control characters and this codepage spells as punctuation. Everything else in the
    /// byte range is its own code point, so only these need a table.
    /// <para>
    /// Five of the thirty-two are undefined and are held by U+FFFF, which is not a character any text
    /// contains - so the slot is occupied, the indices stay aligned with the bytes, and the lookup
    /// can never match it. Writing the table without the gaps is the bug this comment exists to have
    /// already caught: every entry after the first gap would name the wrong byte.
    /// </para>
    /// <para>
    /// Written out because the alternative is a package. <c>Encoding.GetEncoding(1252)</c> wants the
    /// code-pages provider registered, and taking a dependency to read thirty-two characters is a
    /// worse trade than the thirty-two characters.
    /// </para>
    /// </summary>
    internal const string Upper =
        "€￿‚ƒ„…†‡"
        + "ˆ‰Š‹Œ￿Ž￿"
        + "￿‘’“”•–—"
        + "˜™š›œ￿žŸ";

    /// <summary>
    /// The text as the codepage would have written it, or null where it holds something the codepage
    /// cannot spell - which is text that cannot have come from this mistake.
    /// </summary>
    /// <param name="text">The line, decoded as the UTF-8 it is.</param>
    private static byte[]? AsCodepage(string text)
    {
        var bytes = new byte[text.Length];
        for (var at = 0; at < text.Length; at++)
        {
            var one = text[at];
            if (one < 0x80 || (one >= 0xA0 && one <= 0xFF))
            {
                bytes[at] = (byte)one;
                continue;
            }

            var upper = Upper.IndexOf(one);
            if (upper < 0)
                return null;

            bytes[at] = (byte)(0x80 + upper);
        }

        return bytes;
    }

    /// <summary>
    /// Whether this text is something else that was encoded twice, and what it was.
    /// <para>
    /// Strict on the way back, which is what makes it a reading rather than a guess: an invalid
    /// sequence throws instead of becoming a replacement character, so the only text that survives
    /// the round trip is text that genuinely was UTF-8 before somebody misread it.
    /// </para>
    /// </summary>
    /// <param name="text">The line, decoded as the UTF-8 it is.</param>
    /// <param name="was">What it was before, where it was anything.</param>
    internal static bool EncodedTwice(string text, out string was)
    {
        was = "";

        var bytes = AsCodepage(text);
        if (bytes is null)
            return false;

        try
        {
            was = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return false;
        }

        // Shorter, and not merely different. Plain ASCII round-trips to itself and is not a finding;
        // text that came back shorter had multi-byte sequences that were single characters before.
        return was.Length < text.Length;
    }

    [Fact]
    public void The_table_holds_one_entry_per_byte_it_stands_for()
    {
        // The gaps are the whole point: without them every entry after the first would name a byte
        // two or three places along, and the check would report the wrong character while still
        // going red on the right files.
        Assert.Equal(32, Upper.Length);

        Assert.Equal(0x80, 0x80 + Upper.IndexOf('€'));
        Assert.Equal(0x97, 0x80 + Upper.IndexOf('—'));
        Assert.Equal(0x99, 0x80 + Upper.IndexOf('™'));
        Assert.Equal(0x9F, 0x80 + Upper.IndexOf('Ÿ'));
    }

    [Fact]
    public void The_reading_knows_the_damage_from_the_prose_it_must_not_refuse()
    {
        // The thing it is for, spelled both ways: an em-dash, and the em-dash after the mistake.
        //
        // Built from code points rather than typed, and that is not fussiness. Typed, this line is
        // itself a line of damage, and the check below reads every tracked file including this one -
        // so the test that proves the reading works would be the reading's only finding.
        Assert.True(EncodedTwice($"a dash {Damaged} here", out var was));
        Assert.Equal($"a dash {Dash} here", was);

        Assert.False(EncodedTwice($"a dash {Dash} here", out _));

        // And the prose it must not refuse. This repository ships Portuguese language files, so a
        // rule that banned non-ASCII would have to be turned off for exactly the files most likely
        // to carry the damage.
        Assert.False(EncodedTwice("Visão geral", out _));
        Assert.False(EncodedTwice("Sessões", out _));
        Assert.False(EncodedTwice("Relatório", out _));
        Assert.False(EncodedTwice("Configuração", out _));
        Assert.False(EncodedTwice("plain ascii", out _));
        Assert.False(EncodedTwice("", out _));
    }

    /// <summary>An em-dash, and the three characters it becomes when it is encoded twice.</summary>
    private static string Dash => ((char)0x2014).ToString();

    private static string Damaged =>
        new([(char)0x00E2, (char)0x20AC, (char)0x201D]);

    [Fact]
    public void A_line_of_damage_is_found_beside_prose_that_cannot_round_trip()
    {
        // The first draft read whole files and this is what it missed. Both lines are in one file:
        // the second cannot round-trip, so a whole-file reading came back invalid and reported
        // nothing at all - which is a green covering the line above it.
        var file = $"a dash {Damaged} here\nVis{(char)0x00E3}o geral\n";

        Assert.False(EncodedTwice(file, out _));
        Assert.True(EncodedTwice(file.Split('\n')[0], out _));
    }

    [Fact]
    public void No_tracked_text_file_was_encoded_twice()
    {
        var found = new List<string>();

        foreach (var path in Tracked())
        {
            var lines = File.ReadAllText(path, Encoding.UTF8).Split('\n');
            for (var at = 0; at < lines.Length; at++)
            {
                // The line and its text, because a path alone sends a reader to a file to search it
                // by eye for a character they cannot see - which is how six of these went unnoticed.
                if (EncodedTwice(lines[at], out _))
                    found.Add($"{Path.GetRelativePath(Checkout.Root, path)}:{at + 1}: {lines[at].Trim()}");
            }
        }

        Assert.True(
            found.Count == 0,
            "these lines hold text that was UTF-8, was read as Windows-1252, and was written back as "
                + $"UTF-8:{Environment.NewLine}{string.Join(Environment.NewLine, found)}");
    }

    [Fact]
    public void The_walk_reads_the_scripts_that_used_to_be_outside_it()
    {
        // WW421, and the whole of what it moved. This check read seven globs and called itself
        // every text file; the runner is the file this repository edits most, it is prose almost
        // throughout, and it carried five damaged em-dashes past every green run for months.
        //
        // Named rather than counted, because a count goes up for any reason: what is asserted is
        // that the kinds this repository actually writes prose in are read.
        var walked = Tracked().Select(one => Path.GetRelativePath(Checkout.Root, one)).ToList();

        foreach (var wanted in new[]
        {
            Path.Combine("tools", "run-tests-vm.ps1"),
            Path.Combine("tools", "desk-probe.ps1"),
            Path.Combine("tools", "winwright-mcp.cmd"),

            // Asked for rather than spelled. WW193 refuses a file that names the solution, because
            // every copy of the walk it replaced named it in a comment — and a case listing it as
            // one more file to read looks exactly like the nineteenth copy.
            Checkout.Marker,
            ".gitignore",
        })
        {
            Assert.Contains(wanted, walked, StringComparer.OrdinalIgnoreCase);
        }

        // And the sources it always read, so a walk that swapped one set of blind spots for another
        // is red rather than differently quiet.
        Assert.Contains(Path.Combine("tests", "Winwright.Tests", "EncodingTests.cs"), walked, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(Path.Combine("docs", "ROADMAP.md"), walked, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void What_a_build_left_and_what_is_not_text_are_both_stepped_over()
    {
        // The other half: a walk admitting everything reads a nupkg on every run, and the answer
        // would still be right — replacement characters do not encode back to the codepage — but a
        // check that reads the whole packages directory to conclude nothing is a check nobody keeps.
        var walked = Tracked().ToList();

        Assert.DoesNotContain(walked, one => Path.GetExtension(one).Equals(".dll", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(walked, one => Path.GetExtension(one).Equals(".png", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            walked,
            one => one.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>What is not source, and would be read by a walk that did not say so.</summary>
    private static readonly string[] Built = ["bin", "obj", ".git", "TestResults", "packages", "node_modules"];

    /// <summary>
    /// What a file has to be, to be text somebody wrote. WW421: everything that is not one of these,
    /// where it used to be everything that was one of seven globs.
    /// <para>
    /// The list this replaces read as a claim about every text file and was seven extensions.
    /// <c>tools/run-tests-vm.ps1</c> carried five double-encoded em-dashes past it for months, green
    /// every run, because a <c>.ps1</c> was not among them — and neither was a <c>.cmd</c>, a
    /// <c>.slnx</c>, a <c>.yml</c> or a <c>.gitignore</c>. They were found by eye, by somebody
    /// editing the file for another task.
    /// </para>
    /// <para>
    /// Admitting by extension is the mistake, because the extension nobody thought of is the one the
    /// damage is in: a list of what to read has to be extended by whoever adds a file kind, and a
    /// list of what to skip has to be extended by whoever adds a binary — and this repository adds
    /// prose far more often than it adds binaries.
    /// </para>
    /// <para>
    /// Read as text and refused as a finding only where the round trip says so, which means a binary
    /// admitted here costs a slow read rather than a wrong answer: bytes that are not valid UTF-8
    /// become replacement characters, which do not encode back to the codepage, so
    /// <see cref="AsCodepage" /> returns null and the line is not a finding. The list below is
    /// therefore about speed and noise rather than about correctness.
    /// </para>
    /// </summary>
    private static readonly string[] Binary =
    [
        ".png", ".jpg", ".jpeg", ".gif", ".ico", ".bmp", ".pdf", ".zip", ".nupkg", ".snupkg",
        ".dll", ".exe", ".pdb", ".ttf", ".otf", ".woff", ".woff2", ".dmp", ".vmdk",
    ];

    /// <summary>
    /// Every text file under the checkout, walked rather than asked of git.
    /// <para>
    /// Asking git was the first draft and it would have run here and nowhere else. The guest takes
    /// its tree as a zip of a listing made on the host, so <c>C:\src\winwright</c> has no <c>.git</c>
    /// in it at all - and a check that answers only on the machine where the damage did not happen
    /// is not a check. A walk needs nothing from the tree but the tree.
    /// </para>
    /// </summary>
    private static IEnumerable<string> Tracked()
    {
        var walked = Directory
            .EnumerateFiles(Checkout.Root, "*", SearchOption.AllDirectories)
            .Where(one => !Built.Any(skip =>
                one.Contains($"{Path.DirectorySeparatorChar}{skip}{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)))
            .Where(one => !Binary.Contains(Path.GetExtension(one), StringComparer.OrdinalIgnoreCase))
            .ToList();

        // A walk that found nothing is not a clean tree. Every file passing because the root was
        // wrong is the shape of green this whole repository exists to withdraw.
        Assert.NotEmpty(walked);

        return walked;
    }
}

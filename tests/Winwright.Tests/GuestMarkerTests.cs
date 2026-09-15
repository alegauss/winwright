using System.Text.RegularExpressions;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW416. The words that cross between the two machines, held in both directions.
/// <para>
/// <c>sync.ps1</c> and <c>run.cmd</c> are generated on the host, run in the guest, and answer
/// through a log file. What crosses that gap is a handful of capitalised words at the start of a
/// line, and the host switches on them with <c>-match</c>. It is a protocol, and until this it was
/// the only list in this project nobody read back.
/// </para>
/// <para>
/// The failure it allows is quiet both ways. A marker the guest writes that no arm matches falls
/// through to the general refusal, which prints the log and loses the point of having written it;
/// an arm matching a marker the guest stopped writing is dead code that reads as coverage. Neither
/// shows up as a red, because a sync failing at all is already rare enough to be somebody's
/// afternoon.
/// </para>
/// <para>
/// What made it hard to check is that both halves are one file. The guest's programs live inside
/// <c>@"</c> here-strings in the runner, so a sweep for a marker finds the writing and the reading
/// together and can tell neither from the other. Cutting the file at those delimiters is what this
/// needed, and the cut is worth naming on its own: it is the line between the two machines.
/// </para>
/// <para>
/// WW439 brought the numbers under the same rule, and the rule then took a word away. The guest
/// leaves with an exit code the host now switches on, so <c>GUEST-MISSING</c> — which said the guest
/// has no SDK, the one thing that code already says — had nothing left to carry and no arm matching
/// it. It is a plain sentence in the log now, naming where the guest looked, which is what somebody
/// opening that log at the guest console wanted from it. The two that stayed carry what a number
/// cannot: Windows' own words about the file that would not delete, and the names of what holds it.
/// </para>
/// </summary>
public sealed class GuestMarkerTests
{
    /// <summary>
    /// Every word that crosses, and what it says. The guest writes each one at the start of a line
    /// and the host has an arm for it.
    /// </summary>
    private static readonly (string Marker, string Means)[] Markers =
    [
        ("GUEST-HELD", "the tree would not delete, and this is the sentence Windows gave for it"),
        ("GUEST-HOLDER", "one process running from under that tree, named so a reader is not sent to Task Manager"),
    ];

    /// <summary>
    /// The protocol's own shape: a word this project made up, so nothing else in either half can
    /// look like one by accident.
    /// </summary>
    private static readonly Regex Crossing = new(
        "GUEST-[A-Z][A-Z-]*",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    [Fact]
    public void Every_word_that_crosses_is_written_by_the_guest_and_switched_on_by_the_host()
    {
        // Both halves in one case, because the failure is always the pair. A marker written and
        // never matched is a refusal that says nothing; an arm for a marker nobody writes is dead
        // code that reads as coverage.
        var (host, guest) = Halves();

        Assert.All(
            Markers,
            one =>
            {
                Assert.True(
                    guest.Contains(one.Marker, StringComparison.Ordinal),
                    $"nothing in the guest's half writes '{one.Marker}', so no run can produce it");

                Assert.True(
                    host.Contains(one.Marker, StringComparison.Ordinal),
                    $"the host has no arm for '{one.Marker}', so a guest that writes it falls through "
                        + "to the general refusal — which prints the log and says nothing about "
                        + one.Means);
            });
    }

    [Fact]
    public void Nothing_crosses_that_this_catalogue_does_not_name()
    {
        // The half that keeps it a catalogue rather than a list. A fourth marker added to either
        // side is red here until somebody writes down what it says, which is the question WW404
        // answered for two of the three by hand and nothing asked.
        var (host, guest) = Halves();
        var known = Markers.Select(one => one.Marker).ToHashSet(StringComparer.Ordinal);

        var strangers = Spelled(host)
            .Concat(Spelled(guest))
            .Where(one => !known.Contains(one))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            strangers.Count == 0,
            $"{strangers.Count} word(s) cross between the machines and nothing here says what they "
                + $"mean: {string.Join(", ", strangers)}");
    }

    /// <summary>
    /// How the guest's program leaves with a code, and how the host asks for one. WW439: the numbers
    /// are not spelled at either end any more, so what a sweep looks for is the name in between.
    /// </summary>
    private static readonly Regex Leaving = new(
        @"exit \$script:(?<named>\w+)",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    [Fact]
    public void Every_code_the_guest_leaves_with_is_one_the_host_switches_on()
    {
        // WW439. The other half of the protocol, and the half nothing read: `sync.ps1` exited 91 for
        // a guest with no SDK and 92 for a tree that would not delete, and the host took whether the
        // code was zero and then decided which failure it was by matching the log. The numbers were a
        // second copy of what the markers already said, asked for by nobody.
        //
        // Measured before it was changed, because "the code arrives" was the part nothing had
        // established: a guest .cmd exiting 92 makes vmrun exit 92, and so does the real shape — a
        // `powershell -File` that exits 91, under `exit /b %ERRORLEVEL%`.
        var (host, guest) = Halves();

        var leaving = Leaving.Matches(guest)
            .Select(one => one.Groups["named"].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(leaving);

        Assert.All(
            leaving,
            one =>
            {
                Assert.True(
                    host.Contains($"$script:{one} = ", StringComparison.Ordinal),
                    $"the guest exits with $script:{one} and the host declares no such code, so the "
                        + "number it leaves with is whatever an empty variable interpolates to");

                Assert.True(
                    host.Contains($"$synced.ExitCode -eq $script:{one}", StringComparison.Ordinal),
                    $"nothing on the host switches on $script:{one}, so a guest that exits with it "
                        + "falls through to the general refusal — which is what this whole task was");
            });
    }

    [Fact]
    public void No_exit_code_is_a_number_written_at_both_ends()
    {
        // The half that keeps it one copy. A number spelled in the guest's program and again in the
        // host's arm is the drift this protocol had: the two ends can disagree and only a sync that
        // actually fails would ever show it, which is rare enough to be somebody's afternoon.
        var (host, guest) = Halves();

        var spelled = Regex.Matches(guest, @"exit \d+", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))
            .Select(one => one.Value)
            .ToList();

        Assert.True(
            spelled.Count == 0,
            $"the guest's half leaves with a number written into it: {string.Join(", ", spelled)}. "
                + "The host generates that program, so a code belongs in one variable both halves read");
    }

    [Fact]
    public void The_file_really_was_cut_into_the_two_machines_it_holds()
    {
        // The control. Every check above passes trivially against a cut that produced one empty
        // half — the markers would all be on one side and no stranger would be found anywhere —
        // and a reader would be told the protocol agrees with itself.
        var (host, guest) = Halves();

        Assert.NotEqual(0, host.Length);
        Assert.NotEqual(0, guest.Length);

        // One line that can only be the guest's: it extracts the carried tree, in the guest, out of
        // a zip the host put there.
        Assert.Contains("ExtractToDirectory", guest, StringComparison.Ordinal);
        Assert.DoesNotContain("ExtractToDirectory", host, StringComparison.Ordinal);

        // And one that can only be the host's: vmrun is what the host drives the guest through, and
        // a guest running it would be driving itself.
        Assert.Contains("Invoke-VmRun", host, StringComparison.Ordinal);
        Assert.DoesNotContain("Invoke-VmRun", guest, StringComparison.Ordinal);

        // The other half of the same control, and the one the catalogue leans on: a sweep that
        // matched nothing at all would report no strangers either, and be read as the two ends
        // agreeing. Counted on both sides, because each side is what the other is checked against.
        Assert.Equal(Markers.Length, Spelled(guest).Count);
        Assert.Equal(Markers.Length, Spelled(host).Count);
    }

    /// <summary>Every distinct crossing word in one half.</summary>
    /// <param name="half">The host's side of the file, or the guest's.</param>
    private static IReadOnlyCollection<string> Spelled(string half) =>
        Crossing.Matches(half).Select(one => one.Value).Distinct(StringComparer.Ordinal).ToList();

    /// <summary>
    /// The runner cut at the line between the two machines: what the host runs, and what it writes
    /// into the guest to be run there.
    /// <para>
    /// The delimiters are the cut. A here-string opening on its own line is the start of a program
    /// for the other machine, and the line closing it hands that program to
    /// <c>Set-Content</c> — so the file says where the boundary is and this does not have to be
    /// told. Neither delimiter goes into either half: they are the seam and belong to neither side.
    /// </para>
    /// </summary>
    private static (string Host, string Guest) Halves()
    {
        var host = new List<string>();
        var guest = new List<string>();
        var crossed = false;

        foreach (var line in File.ReadLines(Checkout.At("tools", "run-tests-vm.ps1")))
        {
            if (!crossed && line.TrimEnd() == "@\"")
            {
                crossed = true;
                continue;
            }

            if (crossed && line.StartsWith("\"@", StringComparison.Ordinal))
            {
                crossed = false;
                continue;
            }

            (crossed ? guest : host).Add(line);
        }

        Assert.False(crossed, "the runner has a here-string that never closes, so the cut ran off the end");

        return (string.Join('\n', host), string.Join('\n', guest));
    }
}

using System.Text.RegularExpressions;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW203. A deadline is about the thing it names, and a wait that reaches past that thing is a
/// budget covering work another deadline is separately given more time for.
/// <para>
/// Measured on two guest runs a task apart: 5006ms over 158 looks and 5009ms over 159, both against
/// the 5000ms declared for <c>wrote</c> — and both waits started from a standing start, so that
/// budget was covering a cold start of the fixture as well. <c>draw</c> is given twice as long for
/// the cold start on its own, and says so in as many words.
/// </para>
/// <para>
/// The red said the fixture never wrote what it drew. That is a claim about the application under
/// test, and what had happened is that this suite asked for less time than the work takes — the
/// misattribution Block A's criterion is about, arriving through a number.
/// </para>
/// </summary>
public sealed class WaitedForTests
{
    /// <summary>
    /// A wait actually taken on a named deadline, and not the deadline's name appearing somewhere.
    /// <para>
    /// WW211 walked into this. The first reading looked for <c>"wrote"</c> anywhere inside a member,
    /// which is the mistake WW191 and WW197 each found one level up: a name is a subject as often as
    /// it is an act. A case asserting that <c>DeskFacts</c> does not call <c>wrote</c> the desk's,
    /// and listing the deadlines this suite declares to prove each is reachable, was reported as a
    /// caller waiting on the write without waiting for a window — while waiting for nothing at all.
    /// </para>
    /// <para>
    /// So the verb is matched with the name, across the newline that a wrapped call puts between
    /// them. Only the two that wait: <c>Missed</c> builds a sentence and <c>Declared.For</c> reads a
    /// number, and neither is a wait that a budget could be too small for.
    /// </para>
    /// </summary>
    private static readonly Regex TakenOnTheWrite = TakenOn("wrote");

    /// <summary>And the one that has to come first.</summary>
    private static readonly Regex TakenOnTheWindow = TakenOn("draw");

    private static Regex TakenOn(string named) => new(
        $"""Waits\.(Until|Trying)\s*(<[^>\n]*>)?\s*\(\s*"{named}"\s*,""",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(5));

    [Fact]
    public void A_wait_for_a_file_the_fixture_writes_waits_for_its_window_first()
    {
        // Swept and not listed. The first draft named the two callers by hand, which is the shape
        // this repository keeps filing tasks about: a rule applied where it is needed today and
        // silent about the caller written tomorrow. Whichever method waits on the write is found,
        // and the window has to have been waited for above it in that same method.
        var standing = Waiting()
            .Where(one => one.Writes >= 0 && (one.Draws < 0 || one.Draws > one.Writes))
            .Select(one => one.Named)
            .ToList();

        Assert.True(
            standing.Count == 0,
            $"{standing.Count} caller(s) wait on 'wrote' without waiting for the window first, so "
                + $"that budget is covering a cold start as well as the write: {string.Join(", ", standing)}");
    }

    [Fact]
    public void The_sweep_finds_the_two_callers_it_was_measured_on()
    {
        // A sweep that found nothing would pass the rule above by arithmetic, and both of these are
        // the ones the guest actually went red on.
        var named = Waiting().Select(one => one.Named).ToList();

        Assert.Contains("FixtureTests.Driven", named, StringComparer.Ordinal);
        Assert.Contains("ProvokedByFlagTests.Ran", named, StringComparer.Ordinal);
    }

    [Fact]
    public void Naming_a_deadline_is_not_waiting_on_one()
    {
        // WW211. The reading this replaced looked for the name anywhere in a member, and reported a
        // case that names every deadline this suite declares — to assert each is reachable, waiting
        // for nothing — as a caller waiting on the write with no window waited for first. A rule
        // that fires on a member which takes no wait at all is a rule nobody can act on.
        var named = Waiting().Select(one => one.Named).ToList();

        Assert.DoesNotContain(
            "SlowMachineTests.This_gate_is_not_the_desks_and_says_so_by_taking_a_name_the_desk_would_refuse",
            named,
            StringComparer.Ordinal);

        // Not by having narrowed the sweep until it finds nothing: the member it was measured on is
        // still found, and it is found because it takes a wait rather than because it says a word.
        // Driven names 'wrote' three more times after its wait comes back — building the sentence a
        // red prints, and excusing the hole — and none of those is a wait a budget could be too
        // small for.
        Assert.Contains("FixtureTests.Driven", named, StringComparer.Ordinal);
    }

    /// <summary>
    /// Every member of this suite that waits on the write, and where each deadline is named in it.
    /// </summary>
    private static IReadOnlyList<(string Named, int Draws, int Writes)> Waiting()
    {
        var found = new List<(string Named, int Draws, int Writes)>();
        foreach (var file in Checkout.SourcesIn(Checkout.Suite, except: $"{nameof(WaitedForTests)}.cs"))
        {
            var owner = Path.GetFileNameWithoutExtension(file);
            var member = "";
            var body = new List<string>();

            // Spoken and not Code: a deadline is named by a string, and the reading that drops
            // strings deletes both names. What has to go is the prose, since the comment above each
            // wait names the other deadline.
            foreach (var line in File.ReadLines(file).Select(Checkout.Spoken))
            {
                if (Checkout.Member(line) is { } next)
                {
                    Close();
                    member = next;
                }
                else if (member.Length > 0)
                {
                    body.Add(line);
                }
            }

            Close();

            void Close()
            {
                // Joined, because a wrapped call puts the verb and the deadline it waits on either
                // side of a newline — which is exactly how the caller this rule was measured on
                // spells it. Ordered by where each match begins, which is the same ordering line
                // numbers gave and needs no second count.
                var text = string.Join('\n', body);
                var writes = member.Length > 0 ? TakenOnTheWrite.Match(text) : Match.Empty;
                if (writes.Success)
                {
                    var draws = TakenOnTheWindow.Match(text);
                    found.Add(($"{owner}.{member}", draws.Success ? draws.Index : -1, writes.Index));
                }

                member = "";
                body = [];
            }
        }

        return found;
    }

    [Fact]
    public void The_cold_start_is_given_longer_than_the_write_that_follows_it()
    {
        // The arithmetic the repair depends on, asserted rather than assumed. A `wrote` wider than
        // `draw` would mean the split had bought nothing, and a `draw` narrowed to `wrote` would
        // put the cold start back inside a budget too small for it.
        var draw = Waits.Declared.For("draw");
        var wrote = Waits.Declared.For("wrote");

        Assert.True(draw > wrote, $"'draw' is {draw}ms and 'wrote' is {wrote}ms, so the cold start has the lesser budget");
    }
}

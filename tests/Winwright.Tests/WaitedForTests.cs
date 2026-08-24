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
    /// <summary>How the deadlines are named where a wait is taken, which is in a string.</summary>
    private const string ForTheWrite = "\"wrote\"";

    /// <summary>And the one that has to come first.</summary>
    private const string ForTheWindow = "\"draw\"";

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
            var draws = -1;
            var writes = -1;
            var at = 0;

            // Spoken and not Code: a deadline is named by a string, and the reading that drops
            // strings deletes both names. What has to go is the prose, since the comment above each
            // wait names the other deadline.
            foreach (var line in File.ReadLines(file).Select(Checkout.Spoken))
            {
                at++;
                if (Checkout.Member(line) is { } next)
                {
                    Close();
                    member = next;
                }
                else if (member.Length > 0)
                {
                    if (draws < 0 && line.Contains(ForTheWindow, StringComparison.Ordinal))
                        draws = at;
                    if (writes < 0 && line.Contains(ForTheWrite, StringComparison.Ordinal))
                        writes = at;
                }
            }

            Close();

            void Close()
            {
                if (member.Length > 0 && writes >= 0)
                    found.Add(($"{owner}.{member}", draws, writes));

                member = "";
                draws = -1;
                writes = -1;
            }
        }

        return found;
    }

    /// <summary>The member a line declares, at the one indentation a method of a class sits at.</summary>

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

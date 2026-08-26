using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW210. The criterion read as a measurement rather than as a habit — see <see cref="Synthesising" />
/// for why the shape is a catalogue of how each escalation is declared, and not the claim that none
/// happens that this task was filed expecting to assert.
/// </summary>
public sealed class SynthesisingTests
{
    [Fact]
    public void The_default_act_reaches_no_synthesised_input()
    {
        // The criterion's first half, and the whole of what was missing. Act is the route every
        // pattern act takes; not one of its verbs, at any depth and across any file, reaches the
        // send. It holds today because nobody wrote the fallback, and from here it holds because
        // writing one is red.
        var fell = Synthesising.ReachingAtAll()
            .Where(one => one.StartsWith($"{Synthesising.TheDefaultAct}.", StringComparison.Ordinal))
            .ToList();

        Assert.True(
            fell.Count == 0,
            $"the default act now reaches synthesised input through {string.Join(", ", fell)}, so an "
                + "act that could go through a pattern no longer has to");
    }

    [Fact]
    public void Every_verb_that_reaches_it_says_how_a_caller_finds_out()
    {
        // The both-ways half. A verb that starts synthesising is red here until somebody has
        // written down how the caller learns — at the moment they write it, and not on the run
        // where a green covered a click nobody asked for.
        var offered = Synthesising.Reaching();
        var listed = Synthesising.Known.Select(one => one.Named).ToList();

        Assert.Empty(offered.Except(listed, StringComparer.Ordinal));
    }

    [Fact]
    public void Nothing_is_catalogued_that_no_longer_reaches_it()
    {
        var offered = Synthesising.Reaching();

        Assert.Empty(Synthesising.Known.Select(one => one.Named).Except(offered, StringComparer.Ordinal));
    }

    [Fact]
    public void No_verb_is_catalogued_twice()
    {
        var listed = Synthesising.Known.Select(one => one.Named).ToList();

        Assert.Equal(listed.Count, listed.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void The_sweep_reaches_across_files_and_not_one_level()
    {
        // The reason this is a sweep and not a grep. Selecting.Confirmed calls Pointer.Click, which
        // calls Pointer.Run, which calls Win32.SendInput, which calls the import — four hops and
        // three files. A reading that stopped at one level would find Pointer and report the verb
        // this task is about as clean.
        var reaching = Synthesising.ReachingAtAll();

        Assert.Contains("Win32.SendInput", reaching, StringComparer.Ordinal);
        Assert.Contains("Pointer.Run", reaching, StringComparer.Ordinal);
        Assert.Contains("Pointer.Click", reaching, StringComparer.Ordinal);
        Assert.Contains("Selecting.Confirmed", reaching, StringComparer.Ordinal);
    }

    [Fact]
    public void Reading_the_desk_is_not_writing_to_it()
    {
        // The seeds are two, and their neighbours in the same file are the control. A list that took
        // every input-shaped import would report the verbs that read the desk as verbs that drive
        // it, and the catalogue would be a list of everything.
        var reaching = Synthesising.ReachingAtAll();

        Assert.DoesNotContain("Foreground.Now", reaching, StringComparer.Ordinal);
        Assert.DoesNotContain("Foreground.Check", reaching, StringComparer.Ordinal);
        Assert.DoesNotContain("ForeignInput.Read", reaching, StringComparer.Ordinal);
        Assert.DoesNotContain("Traversal.WhoHasFocus", reaching, StringComparer.Ordinal);
    }

    [Fact]
    public void The_two_pattern_routes_are_the_ones_that_had_to_be_argued()
    {
        // Everything else here synthesises because that is its name. These two are reached by a
        // caller wanting a value selected, which is a thing a pattern can usually do — so how the
        // caller learns is the whole question, and each answers it differently.
        Assert.Equal(
            Asking.TheCallerOptedIn,
            Synthesising.Known.Single(one => one.Named == "Selecting.Confirmed").How);

        Assert.Equal(
            Asking.TheResultSaysWhichRouteRan,
            Synthesising.Known.Single(one => one.Named == "Pick.Value").How);

        Assert.All(
            Synthesising.Known.Where(one => one.How == Asking.ItIsTheAct),
            one => Assert.Contains(
                one.Named.Split('.')[0],
                // WW225 added a sixth, and it belongs by the same test the other five pass: the
                // family is called Synthesised, so a caller reaching for it has said what it wants
                // by choosing the name.
                new[] { "Keyboard", "Pointer", "Traversal", "Menu", "NotificationArea", "Synthesised" },
                StringComparer.Ordinal));
    }

    [Fact]
    public void Every_way_of_finding_out_is_worded_rather_than_printed_as_a_name()
    {
        Assert.All(
            Enum.GetValues<Asking>(),
            one => Assert.DoesNotContain(one.ToString(), Synthesising.Worded(one), StringComparison.Ordinal));

        Assert.Throws<ArgumentOutOfRangeException>(() => Synthesising.Worded((Asking)99));
    }

    [Fact]
    public void The_catalogue_reads_as_counts_and_then_a_line_each()
    {
        var rendered = Synthesising.Render();

        Assert.Equal(Synthesising.Known.Count + 1, rendered.Count);
        Assert.Contains("reach synthesised input", rendered[0]);
        Assert.Contains("are the act itself", rendered[0]);
        Assert.All(rendered.Skip(1), one => Assert.StartsWith("  ", one));
    }
}

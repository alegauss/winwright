using Winwright.Typing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW354. The measurement tool's arms were a paragraph each in <c>run-typing.cmd</c>, where a person
/// reads what it can do, and a <c>string.Equals</c> each in <c>Program</c>, where the second word is
/// parsed — and neither knew about the other.
/// <para>
/// The failure was silent in the direction that matters. A word in the .cmd and not in the switch
/// was not recognised, nothing refused it, and the tool ran its default experiment and printed that
/// experiment's numbers under a run somebody started for something else. A typo did the same. This
/// is the both-way catalogue every other list in this project already has, reached at last by the
/// one tool that sits outside the suite.
/// </para>
/// <para>
/// Nothing here runs an arm. That is the whole reason the tool is outside: it takes the desk for
/// minutes, and a guest run should not pay for a question asked once.
/// </para>
/// </summary>
public class TypingArmTests
{
    /// <summary>What a person reads, which is the other list this holds the arms to.</summary>
    private static string Runner() => File.ReadAllText(Path.Combine(Checkout.At(), "run-typing.cmd"));

    [Fact]
    public void Every_arm_the_tool_parses_is_one_the_command_a_person_types_describes()
    {
        // The direction that used to fail silently, read the way it fails: an arm the .cmd never
        // mentions is a measurement nobody can find, and the person who added it is the last one
        // who knew it was there.
        var said = Runner();

        Assert.All(
            Arms.All,
            one => Assert.True(
                said.Contains($"`{one.Name}`", StringComparison.Ordinal),
                $"run-typing.cmd never names the '{one.Name}' arm"));
    }

    [Fact]
    public void Every_arm_the_command_describes_is_one_the_tool_would_run()
    {
        // And the worse direction. The .cmd's closing paragraph lists what a person types, so a word
        // there that the tool does not know is a run that answers with the wrong experiment — which
        // is what this whole entry is about.
        var typed = Runner()
            .Split('`')
            .Where((_, at) => at % 2 == 1)
            .Select(one => one.Trim())
            .Where(one => one.StartsWith("run-typing.cmd ", StringComparison.Ordinal))
            .Select(one => one.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(one => one.Length > 2)
            .Select(one => one[2])
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(typed);
        Assert.All(
            typed,
            one => Assert.True(
                Arms.Named(one) is not null,
                $"run-typing.cmd tells a person to type '{one}', which names no arm the tool has"));
    }

    [Fact]
    public void A_word_that_names_no_arm_is_refused_rather_than_answered_by_the_default()
    {
        // The repair, and the reason it is a separate question from the catalogue: holding the two
        // lists together stops an arm going missing, and this stops a typo being answered.
        Assert.True(Arms.Unrecognised("actz"));
        Assert.Null(Arms.Named("actz"));

        var refusing = Arms.Refusing("actz");

        Assert.Contains(refusing, one => one.Contains("'actz'", StringComparison.Ordinal));
        Assert.All(
            Arms.All,
            one => Assert.Contains(refusing, line => line.Contains(one.Name, StringComparison.Ordinal)));
    }

    [Fact]
    public void A_bare_run_names_no_arm_and_is_not_a_word_that_missed()
    {
        // The default is the absence of an arm rather than one of them, and the two are answered
        // differently: a caller that could not tell them apart would refuse a bare run, which is
        // the run the tool was built for.
        Assert.Null(Arms.Named(""));
        Assert.Null(Arms.Named("   "));
        Assert.False(Arms.Unrecognised(""));
        Assert.False(Arms.Unrecognised(null));

        // And the default is in neither list, so nobody can misspell a name it does not have.
        Assert.DoesNotContain(Arms.All, one => one.Name.Length == 0);
    }

    [Fact]
    public void An_arm_is_named_however_a_person_capitalised_it()
    {
        Assert.Equal("acts", Arms.Named("ACTS")?.Name);
        Assert.Equal("sweep", Arms.Named(" Sweep ")?.Name);
    }

    [Fact]
    public void The_arm_that_needs_a_shape_of_the_fixture_says_so_where_it_is_declared()
    {
        // WW341's arm is the only one that needs `--ranges`, and it used to be a branch beside the
        // launch. Declared here, the next arm needing a shape of the fixture says so in the one
        // place an arm is described.
        Assert.True(Arms.Named("acts")!.NeedsRanges);
        Assert.Equal(["acts"], Arms.All.Where(one => one.NeedsRanges).Select(one => one.Name));
    }

    [Fact]
    public void Every_arm_says_which_task_built_it_and_what_it_drives()
    {
        // The .cmd's paragraphs are what a person reads to choose one, so an arm that carried only
        // a name would move the prose back out of the list this exists to make single.
        Assert.All(
            Arms.All,
            one =>
            {
                Assert.StartsWith("WW", one.Task, StringComparison.Ordinal);
                Assert.False(string.IsNullOrWhiteSpace(one.Drives), $"{one.Name} says nothing about what it drives");
                Assert.Contains(one.Name, one.ToString(), StringComparison.Ordinal);
            });
    }
}

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW331. The guest runner's desk probe, read as text — which is the only way anything here can
/// read it.
/// <para>
/// The probe is PowerShell that runs in the guest before a tree is carried, and it refused a
/// session: <c>the guest's desk is waiting for an answer: explorer (pid 1008, Shell_TrayWnd) ''
/// held the foreground for every look</c>. A capture taken seconds later showed an ordinary
/// desktop with the overflow chevron focused and nothing to answer. The reading was right and the
/// word for it was wrong.
/// </para>
/// <para>
/// The first repair was worse than the defect and is what these cases are really for: the shell's
/// classes were added to the list of things that are <em>the desktop</em>, which made the refusal go
/// away by making the reading say nothing at all — a desk somebody had left the taskbar selected on
/// then read as "nothing but the desktop held the foreground". So the two lists are asserted
/// disjoint, and the state that tells them apart is asserted to exist and not to refuse.
/// </para>
/// <para>
/// What this cannot do is run it. The probe reads the live foreground in another machine's session,
/// and nothing in this suite has one — so these are claims about the text, and the arm they are
/// about is provoked by a desk rather than by a case. That gap is worth naming rather than papering
/// over, and it is filed.
/// </para>
/// </summary>
public sealed class DeskProbeTests
{
    /// <summary>The runner, which carries the probe inside it as a here-string.</summary>
    private static string Runner() => File.ReadAllText(Checkout.At("tools", "run-tests-vm.ps1"));

    /// <summary>Every answer the probe can write, and the runner has an arm for each.</summary>
    private static readonly string[] States = ["clear", "busy", "asking", "shell", "broken"];

    [Fact]
    public void The_probe_answers_the_states_the_runner_switches_on()
    {
        // Both halves in one case, because the failure is always the pair: a state the probe writes
        // and the runner has no arm for falls through to `default`, which says the desk could not be
        // read — a refusal about the probe for a probe that answered.
        var runner = Runner();

        Assert.All(
            States,
            one => Assert.True(
                runner.Contains($"'{one}' {{", StringComparison.Ordinal),
                $"the runner has no arm for '{one}', so that answer would read as a desk it could not read"));

        Assert.All(
            States,
            one => Assert.True(
                runner.Contains($"\"{one}|", StringComparison.Ordinal)
                    || runner.Contains($"'{one}||||", StringComparison.Ordinal)
                    || runner.Contains($"{{ '{one}' }}", StringComparison.Ordinal),
                $"nothing in the probe writes '{one}', so the runner has an arm for an answer it never gets"));
    }

    [Fact]
    public void The_shell_is_not_on_the_list_of_things_that_are_the_desktop()
    {
        // The repair that hid the reading. Folding the taskbar in with Progman and WorkerW makes a
        // focused shell answer `clear`, which is the sentence "nothing but the desktop held the
        // foreground" said about a desk the shell was holding.
        var runner = Runner();

        var desktop = Between(runner, "$desktop = @(", ")");
        var shell = Between(runner, "$shellSurfaces = @(", ")");

        Assert.Contains("Progman", desktop, StringComparison.Ordinal);
        Assert.Contains("Shell_TrayWnd", shell, StringComparison.Ordinal);

        // The three the session was lost to, none of them on the desktop's list.
        foreach (var one in new[] { "Shell_TrayWnd", "Shell_SecondaryTrayWnd", "TopLevelWindowForOverflowXamlIsland" })
        {
            Assert.Contains(one, shell, StringComparison.Ordinal);
            Assert.DoesNotContain(one, desktop, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void A_question_refuses_the_run_and_a_selected_shell_does_not()
    {
        // The distinction the whole task is about, as the two arms actually do it. A question is
        // some application's and no amount of waiting answers it, so the run stops and a person is
        // sent to the guest console. The shell asks nothing, so sending anybody there is sending
        // them to look at a prompt that is not on the screen.
        var runner = Runner();

        Assert.Contains("Refuse", Arm(runner, "asking"), StringComparison.Ordinal);

        var selected = Arm(runner, "shell");
        Assert.DoesNotContain("Refuse", selected, StringComparison.Ordinal);
        Assert.Contains("Write-Host", selected, StringComparison.Ordinal);

        // And it names the task that stops a run leaving one behind, because a reader who sees this
        // line wants to know what put the desk there rather than what to click.
        Assert.Contains("WW330", selected, StringComparison.Ordinal);
    }

    /// <summary>The text between two markers, or empty where either is missing.</summary>
    /// <param name="text">The whole file.</param>
    /// <param name="opens">What the region starts after.</param>
    /// <param name="closes">What ends it.</param>
    private static string Between(string text, string opens, string closes)
    {
        var from = text.IndexOf(opens, StringComparison.Ordinal);
        if (from < 0)
            return "";

        from += opens.Length;
        var to = text.IndexOf(closes, from, StringComparison.Ordinal);
        return to < 0 ? "" : text[from..to];
    }

    /// <summary>
    /// One arm of the runner's switch over the probe's answer, up to the start of the next.
    /// <para>
    /// Read to the next arm rather than to a closing brace, because the arms hold braces of their
    /// own — an arm read to the first <c>}</c> would end inside its own string interpolation and
    /// answer that it refuses nothing.
    /// </para>
    /// </summary>
    /// <param name="runner">The whole file.</param>
    /// <param name="state">Which arm.</param>
    private static string Arm(string runner, string state)
    {
        var from = runner.IndexOf($"'{state}' {{", StringComparison.Ordinal);
        Assert.True(from >= 0, $"the runner has no arm for '{state}'");

        var next = States
            .Select(one => runner.IndexOf($"'{one}' {{", from + 1, StringComparison.Ordinal))
            .Where(at => at > 0)
            .DefaultIfEmpty(runner.Length)
            .Min();

        // `default` closes the last arm, and an arm read past it would take the next one's words.
        var fallback = runner.IndexOf("default {", from + 1, StringComparison.Ordinal);
        if (fallback > 0 && fallback < next)
            next = fallback;

        return runner[from..next];
    }
}

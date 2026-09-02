using System.Diagnostics;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW331, WW345. The guest runner's desk probe — the reading that decides whether twenty minutes
/// are spent on a machine, and whether a person is sent to a guest console to answer a prompt.
/// <para>
/// It refused a session: <c>the guest's desk is waiting for an answer: explorer (pid 1008,
/// Shell_TrayWnd) '' held the foreground for every look</c>. A capture taken seconds later showed an
/// ordinary desktop with the overflow chevron focused and nothing to answer. The reading was right
/// and the word for it was wrong.
/// </para>
/// <para>
/// The first repair was worse than the defect and is what half of these cases are for: the shell's
/// classes were added to the list of things that are <em>the desktop</em>, which made the refusal go
/// away by making the reading say nothing at all — a desk somebody had left the taskbar selected on
/// then read as "nothing but the desktop held the foreground".
/// </para>
/// <para>
/// WW345 is why the other half can exist. The probe used to be a here-string inside the runner, and
/// a here-string has no caller but the function holding it — so every check here was a claim about
/// text, and a classification that answered <c>busy</c> where it meant <c>shell</c> would have
/// passed all of them. It is a file now, with the classification as a function, and these cases call
/// it with looks they made up. The runner sends that same file to the guest, so what is exercised
/// here and what refuses a run are one file rather than two copies.
/// </para>
/// <para>
/// What is still not run here is the polling. Twelve looks half a second apart at the live
/// foreground of a session this process is not sitting in is not something a case can arrange, and
/// the arm that matters — one window for every look — is a desk rather than a case. What these do
/// is take that array as given and check what it is called, which is where both defects were.
/// </para>
/// <para>
/// Serial since WW345, and WW125's rule is why: running the classification means starting a real
/// PowerShell, and a process this suite launches is a process that can take the foreground away from
/// whatever case is measuring it. The console is suppressed below as well — both, because one is the
/// rule and the other is the thing the rule is about.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class DeskProbeTests
{
    /// <summary>The runner, which carries the probe to the guest and switches on its answer.</summary>
    private static string Runner() => File.ReadAllText(Checkout.At("tools", "run-tests-vm.ps1"));

    /// <summary>The probe itself, which decides what a desk is called.</summary>
    private static string Probe() => File.ReadAllText(Checkout.At("tools", "desk-probe.ps1"));

    /// <summary>Every answer the probe can write, and the runner has an arm for each.</summary>
    private static readonly string[] States = ["clear", "busy", "asking", "shell", "broken"];

    [Fact]
    public void The_probe_answers_the_states_the_runner_switches_on()
    {
        // Both halves in one case, because the failure is always the pair: a state the probe writes
        // and the runner has no arm for falls through to `default`, which says the desk could not be
        // read — a refusal about the probe for a probe that answered.
        var runner = Runner();
        var probe = Probe();

        Assert.All(
            States,
            one => Assert.True(
                runner.Contains($"'{one}' {{", StringComparison.Ordinal),
                $"the runner has no arm for '{one}', so that answer would read as a desk it could not read"));

        Assert.All(
            States,
            one => Assert.True(
                probe.Contains($"\"{one}|", StringComparison.Ordinal)
                    || probe.Contains($"'{one}||||", StringComparison.Ordinal)
                    || probe.Contains($"{{ '{one}' }}", StringComparison.Ordinal),
                $"nothing in the probe writes '{one}', so the runner has an arm for an answer it never gets"));
    }

    [Fact]
    public void The_shell_is_not_on_the_list_of_things_that_are_the_desktop()
    {
        // The repair that hid the reading. Folding the taskbar in with Progman and WorkerW makes a
        // focused shell answer `clear`, which is the sentence "nothing but the desktop held the
        // foreground" said about a desk the shell was holding.
        var probe = Probe();

        var desktop = Between(probe, "$script:Desktop = @(", ")");
        var shell = Between(probe, "$script:ShellSurfaces = @(", ")");

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

    [Fact]
    public void Every_answer_is_produced_by_running_the_classification_and_not_by_reading_it()
    {
        // WW345, and the case the three above could not be. Every state the runner switches on,
        // asked of the function that decides them, with looks this case made up — so a
        // classification that answered the wrong word is a red here rather than a run refused on
        // another machine in a fortnight.
        var said = Classified(
            "@($null, $null, $null)",
            "@((Look 7 'Chrome_WidgetWin_1' 'a toast'), $null, (Look 7 'Chrome_WidgetWin_1' 'a toast'))",
            "@((Look 7 'Window' 'Habilitar o Backup'), (Look 7 'Window' 'Habilitar o Backup'))",
            "@((Look 9 'Shell_TrayWnd' ''), (Look 9 'Shell_TrayWnd' ''))",
            "@($null, $null, $null) -StillNothing $true");

        Assert.StartsWith("clear|", said[0], StringComparison.Ordinal);
        Assert.StartsWith("busy|", said[1], StringComparison.Ordinal);
        Assert.StartsWith("asking|", said[2], StringComparison.Ordinal);
        Assert.StartsWith("shell|", said[3], StringComparison.Ordinal);
        Assert.StartsWith("broken|", said[4], StringComparison.Ordinal);

        // The named half of the two that are not `clear`, because a person is sent to a console by
        // one of them and has to be told which window.
        Assert.Contains("Habilitar o Backup", said[2], StringComparison.Ordinal);
        Assert.Contains("Shell_TrayWnd", said[3], StringComparison.Ordinal);
    }

    [Fact]
    public void A_taskbar_that_held_every_look_is_the_shell_and_never_a_question_or_a_quiet_desk()
    {
        // Both defects this reading has had, run rather than read. The first called it a question
        // and refused every later run; the second called it the desktop, which made a desk somebody
        // had genuinely left the shell selected on read as nothing at all.
        var said = Classified(
            "@((Look 9 'Shell_TrayWnd' ''), (Look 9 'Shell_TrayWnd' ''), (Look 9 'Shell_TrayWnd' ''))",
            "@((Look 9 'TopLevelWindowForOverflowXamlIsland' ''), (Look 9 'TopLevelWindowForOverflowXamlIsland' ''))");

        Assert.All(
            said,
            one =>
            {
                Assert.StartsWith("shell|", one, StringComparison.Ordinal);
                Assert.DoesNotContain("asking", one, StringComparison.Ordinal);
                Assert.DoesNotContain("clear", one, StringComparison.Ordinal);
            });
    }

    [Fact]
    public void One_window_for_every_look_is_what_separates_a_question_from_a_desk_that_moved()
    {
        // The measurement the whole probe is, and the reason it polls at all: a toast goes and a
        // question does not. The same window twice is a question; the same window with one look in
        // between where it was not there is a desk that moved, whatever it moved to.
        var said = Classified(
            "@((Look 7 'Window' 'a prompt'), (Look 7 'Window' 'a prompt'), (Look 7 'Window' 'a prompt'))",
            "@((Look 7 'Window' 'a prompt'), (Look 8 'Window' 'another'), (Look 7 'Window' 'a prompt'))");

        Assert.StartsWith("asking|", said[0], StringComparison.Ordinal);
        Assert.StartsWith("busy|", said[1], StringComparison.Ordinal);
    }

    /// <summary>
    /// Run the probe's own classification over each set of looks, and answer what it called them.
    /// <para>
    /// Through PowerShell and not reimplemented here, which is the point: a copy of the rule in C#
    /// would agree with itself forever while the file that refuses runs drifted away from it. The
    /// probe is dot-sourced with <c>-DefineOnly</c>, so nothing reads a desk.
    /// </para>
    /// </summary>
    /// <param name="looks">One PowerShell expression per set, each an array of looks or nulls.</param>
    private static IReadOnlyList<string> Classified(params string[] looks)
    {
        var script = Path.Combine(Path.GetTempPath(), $"winwright-ww345-{Guid.NewGuid():N}.ps1");
        var probe = Checkout.At("tools", "desk-probe.ps1");

        // `Look` builds what the polling loop builds, field for field. Named here rather than in the
        // probe because it is the shape of a look and not part of deciding what looks mean — and a
        // helper the probe carried for a test would be a line the guest runs for nothing.
        File.WriteAllText(
            script,
            $$"""
            Set-StrictMode -Version Latest
            $ErrorActionPreference = 'Stop'
            . '{{probe}}' -DefineOnly
            function Look($handle, $class, $title) {
                [pscustomobject]@{ Handle = $handle; Pid = 42; Process = 'prompt'; Class = $class; Title = $title }
            }
            {{string.Join(Environment.NewLine, looks.Select(one => $"Read-DeskState -Looks {one}"))}}
            """);

        try
        {
            var ran = Process.Start(new ProcessStartInfo("powershell.exe")
            {
                ArgumentList = { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", script },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,

                // No console, so this cannot be the window that takes the desk. The class is serial
                // as well, which is the rule; this is the reason the rule exists.
                CreateNoWindow = true,
            })!;

            var said = ran.StandardOutput.ReadToEnd();
            var wrong = ran.StandardError.ReadToEnd();
            ran.WaitForExit(30000);

            var lines = said.Split('\n').Select(one => one.Trim('\r', ' ')).Where(one => one.Length > 0).ToList();

            // The count and not only the content: a probe that threw halfway answers fewer lines
            // than it was asked for, and comparing the ones that arrived against the first few
            // expectations would report the wrong state as the wrong answer.
            Assert.True(
                lines.Count == looks.Length,
                $"asked for {looks.Length} classification(s) and got {lines.Count}: {said}{wrong}");

            return lines;
        }
        finally
        {
            File.Delete(script);
        }
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

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
/// WW357 reached the other half. The polling was still run by nothing but a real guest, and a look
/// built wrong classifies perfectly — a window whose class comes back empty is not the desktop and
/// not a shell surface, so a quiet desk reads as a question and refuses the run, which is the
/// failure this probe has already caused once arrived at from the other end. The loop is a function
/// taking its count and its pause, so a case that owns the foreground asks for two looks with no
/// pause and every line of it runs.
/// </para>
/// <para>
/// The twelve looks over six seconds stay the guest's, and they are a measurement rather than a
/// shape: what they are for is that a toast lives for seconds and the prompt that cost a run had
/// been up for hours. A case that waited them out would be paying six seconds to learn what two
/// looks already say about how a look is built.
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

    /// <summary>The clearer, which decides what a run may put away. WW371.</summary>
    private static string Clearer() => File.ReadAllText(Checkout.At("tools", "desk-clear.ps1"));

    /// <summary>Every answer the probe can write, and the runner has an arm for each.</summary>
    private static readonly string[] States = ["clear", "busy", "asking", "shell", "stale", "broken"];

    /// <summary>What <see cref="Looked" /> prints for a look the probe skipped. WW357.</summary>
    private const string Desktop = "the desktop";

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
        var lines = Ran(string.Join(Environment.NewLine, looks.Select(one => $"Read-DeskState -Looks {one}")));

        // The count and not only the content: a probe that threw halfway answers fewer lines than it
        // was asked for, and comparing the ones that arrived against the first few expectations would
        // report the wrong state as the wrong answer.
        Assert.True(
            lines.Count == looks.Length,
            $"asked for {looks.Length} classification(s) and got {lines.Count}: {string.Join(" / ", lines)}");

        return lines;
    }

    /// <summary>
    /// Run <paramref name="body"/> against the real probe, dot-sourced with <c>-DefineOnly</c> so
    /// nothing reads a desk it was not asked to. WW357 pulled this out of <see cref="Classified" />,
    /// because the polling needs the same launch and none of the counting.
    /// </summary>
    /// <param name="body">The PowerShell to run once the probe is defined.</param>
    private static IReadOnlyList<string> Ran(string body)
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
            function Look($handle, $class, $title, $iconic = $false) {
                [pscustomobject]@{
                    Handle = $handle; Pid = 42; Process = 'prompt'; Class = $class; Title = $title
                    Iconic = $iconic
                }
            }
            {{body}}
            """);

        try
        {
            return Answered(script);
        }
        finally
        {
            File.Delete(script);
        }
    }

    /// <summary>
    /// Start PowerShell on a script and hand back the lines it wrote. WW371 pulled it out of
    /// <see cref="Ran" />, because the clearer needs the same launch and a different preamble.
    /// </summary>
    /// <param name="script">The file to run.</param>
    private static IReadOnlyList<string> Answered(string script)
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

        Assert.True(wrong.Trim().Length == 0, $"the script wrote to standard error: {wrong}");

        return said.Split('\n').Select(one => one.Trim('\r', ' ')).Where(one => one.Length > 0).ToList();
    }

    [Fact]
    public void The_polling_builds_a_look_out_of_the_window_that_actually_holds_the_desk()
    {
        // WW357. The half WW345 left: the classification was made runnable and the loop that feeds
        // it was not, so a look built wrong classified perfectly. A window whose class came back
        // empty is not the desktop and not a shell surface, which makes a quiet desk read as a
        // question and refuse the run — the exact failure this probe has already caused once,
        // arrived at from the other end.
        //
        // Two looks and no pause, which is why this can be a case at all. The guest's twelve over
        // six seconds are a measurement about how long a prompt outlives a toast; the shape of a
        // look is not, and every line of the loop runs either way.
        using var dialog = PumpedDialog.Open("winwright desk probe");
        dialog.BringToFront();

        if (BusyDesk.Excused(Winwright.Windowing.Foreground.Check(dialog.Frame).AsPrecondition()))
            return;

        var look = Looked("-Count 2 -PauseMs 0").FirstOrDefault();

        Assert.True(look is not null, "the polling built no look at all");

        // The probe calling it the desktop is the desk and not the loop: something took the
        // foreground between the check above and the poll below. Excused rather than split, because
        // splitting it fails as an index out of range and says nothing about either.
        if (look == Desktop
            && BusyDesk.Excused(
                Winwright.Verdicts.Precondition.Absent(
                    "the foreground belongs to the window under test",
                    "the probe's look found the desktop rather than the dialog this case put up")))
        {
            return;
        }

        // Field for field, because each is a way for a look to be built wrong and every one of them
        // reaches Read-DeskState as a fact it has no way to doubt. The class is the one the shell
        // surfaces are matched against; the handle is what "one window for every look" compares.
        var fields = look!.Split('|');

        Assert.Equal(dialog.Frame.ToString(System.Globalization.CultureInfo.InvariantCulture), fields[0]);
        Assert.Equal(
            Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture), fields[1]);

        Assert.Equal("testhost", fields[2]);
        Assert.Equal("Static", fields[3]);
        Assert.Equal("winwright desk probe", fields[4]);

        // WW375's field, read with the rest: the classification is a pure function of what the loop
        // returns, and a state it had to infer is a state it would get wrong. This dialog is up and
        // in front, so it is the answer that says a look carries the reading at all.
        Assert.Equal("False", fields[5]);
    }

    [Fact]
    public void A_minimised_window_holding_the_desk_is_not_a_question_anybody_can_answer()
    {
        // WW375, measured on this guest: an Edge window left focused held the foreground for all
        // twelve looks and refused every run, and IsIconic on that handle answered true throughout.
        // It was minimised, and Windows keeps a minimised window as the foreground until something
        // else claims it — so the refusal sent a reader to the guest console to answer a window
        // nobody can see.
        //
        // Its own word rather than folded into the desktop, which is WW331's lesson: making the
        // refusal go away by calling this "nothing but the desktop held the foreground" is a
        // sentence that is not true about a desk a window is holding. The reading was right and
        // there was no word for it.
        var said = Classified(
            "@((Look 1 'Chrome_WidgetWin_1' 'a browser' $true), (Look 1 'Chrome_WidgetWin_1' 'a browser' $true))",
            "@((Look 1 '#32770' 'Save changes?'), (Look 1 '#32770' 'Save changes?'))",

            // And the shell still wins where both would answer, because a taskbar is the shell's
            // whatever state its window is in — the two words are about different things.
            "@((Look 1 'Shell_TrayWnd' '' $true), (Look 1 'Shell_TrayWnd' '' $true))");

        Assert.StartsWith("stale|prompt|42|Chrome_WidgetWin_1|a browser", said[0], StringComparison.Ordinal);
        Assert.StartsWith("asking|", said[1], StringComparison.Ordinal);
        Assert.StartsWith("shell|", said[2], StringComparison.Ordinal);
    }

    [Fact]
    public void A_window_with_no_minimise_button_is_the_one_a_run_leaves_alone()
    {
        // WW371, and the whole of the line it draws. The refusal used to have one remedy — a person
        // at the guest console — so a session working a backlog could not run at all on a desk
        // somebody had left a browser in front of. Measured on WW358: a cold start passed 1961
        // cases, and the next run refused because Edge had restored its session at login.
        //
        // What separates that from a question is the minimise button. A window a person can put
        // away is one a run may put away, and a modal prompt has none — which is what makes it a
        // question rather than clutter. So this drives the decision with the styles rather than the
        // windows, because what is under test is where the line is and not that Windows sets bits.
        var said = Cleared(
            "Test-Clearable -Class 'Chrome_WidgetWin_1' -Style 0x00020000",
            "Test-Clearable -Class 'Chrome_WidgetWin_1' -Style 0x00000000",
            "Test-Clearable -Class '#32770' -Style 0x00000000",

            // The shell's own, refused whatever its style says. A taskbar holding the desk is
            // `shell` and never reaches this file, and a run that put it away would be taking the
            // thing WW330 exists to give back.
            "Test-Clearable -Class 'Shell_TrayWnd' -Style 0x00020000");

        Assert.Equal(["True", "False", "False", "False"], said);
    }

    [Fact]
    public void The_runner_reads_the_desk_again_rather_than_believing_the_clearing()
    {
        // The rule that keeps WW311 intact while WW371 relaxes the refusal: a repair is attempted
        // and never trusted. The clearer says what it did; the desk is read a second time, and it
        // is the second reading that decides — so a window that comes back is a window that refuses
        // this run, and a prompt is still exactly what a person has to go and answer.
        var runner = Runner();
        var asking = Arm(runner, "asking");

        Assert.Contains("Clear-GuestDesk", asking, StringComparison.Ordinal);
        Assert.Contains("Read-GuestDesk", asking, StringComparison.Ordinal);

        // And it still refuses. A relaxation that could not refuse would be the reading thrown away
        // rather than acted on, which is the repair WW331 was filed about wearing a new name.
        Assert.Contains("Refuse", asking, StringComparison.Ordinal);
        Assert.Contains("Waiting does not clear a question", asking, StringComparison.Ordinal);

        // The clearer is carried the way the probe is, which is what makes the file the guest runs
        // the file this suite exercises.
        Assert.Contains("desk-clear.ps1", runner, StringComparison.Ordinal);
    }

    [Fact]
    public void The_clearer_moves_a_window_and_hands_the_foreground_on()
    {
        // WW371 was filed on a wrong premise and this is the sentence that corrects it. The entry
        // said a browser window survived twelve SW_MINIMIZE calls, each reporting the same handle in
        // the foreground 600ms later, and concluded something was restoring it. Read again, IsIconic
        // on that handle answered true throughout: the minimise had worked every time, and Windows
        // keeps a minimised window as the foreground until something else claims it.
        //
        // So a repair that only minimises reads as one that did nothing, which is what the twelve
        // calls were. Handing the foreground on is the half that was missing, and it is asserted
        // here because it is the half a reader of that entry would leave out.
        var clearer = Clearer();

        Assert.Contains("ShowWindow", clearer, StringComparison.Ordinal);
        Assert.Contains("ShowTheDesktop", clearer, StringComparison.Ordinal);

        // Nothing is closed and nothing is killed, which is WW311's lesson kept: killing a prompt's
        // owner cleared the prompt and cost the tray, and the run after it went red with no icon
        // anywhere.
        foreach (var one in new[] { "TerminateProcess", "Stop-Process", "CloseWindow", "WM_CLOSE" })
            Assert.DoesNotContain(one, clearer, StringComparison.Ordinal);
    }

    [Fact]
    public void A_class_on_the_desktop_list_is_skipped_and_the_look_is_nothing()
    {
        // WW370. The one branch of the polling that answers $null, and until now nothing ran it: the
        // cases above arrange a window, so every look they build is a window. What that branch does
        // is what makes a quiet desk read `clear` — Progman and WorkerW hold the foreground of an
        // idle logged-in session, and a loop that stopped skipping them would build twelve looks of
        // one window and hand up a question. That is WW331's refusal produced by the loop instead of
        // by the classification, and it would have been invisible here.
        //
        // A case cannot arrange a desk the desktop is holding. What it can do is name the class of
        // the window it has just put up, which puts the same branch under the same test: a class on
        // the list is skipped. What the list actually says stays checked where it was, by the case
        // that reads it out of the file beside the shell surfaces.
        using var dialog = PumpedDialog.Open("winwright desktop stand-in");
        dialog.BringToFront();

        if (BusyDesk.Excused(Winwright.Windowing.Foreground.Check(dialog.Frame).AsPrecondition()))
            return;

        // Both looks, because the branch has to answer for every one of them: a loop that skipped
        // the first and kept the second would build a set Read-DeskState reads as `busy`, which is
        // a desk somebody was using said about a desk nobody was.
        var skipped = Looked("-Count 2 -PauseMs 0 -Desktop 'Static'");

        Assert.Equal([Desktop, Desktop], skipped);

        // And the same window with the list back as it is, so what the case just proved is the list
        // being read rather than this dialog being unreadable.
        var kept = Looked("-Count 2 -PauseMs 0").FirstOrDefault();

        if (kept == Desktop
            && BusyDesk.Excused(
                Winwright.Verdicts.Precondition.Absent(
                    "the foreground belongs to the window under test",
                    "the probe's look found the desktop rather than the dialog this case put up")))
        {
            return;
        }

        Assert.StartsWith(
            dialog.Frame.ToString(System.Globalization.CultureInfo.InvariantCulture),
            kept,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_desk_this_process_is_holding_is_read_as_a_question_end_to_end()
    {
        // WW357, and the two halves joined: the loop builds the looks and the classification names
        // them, in one run, against a desk this suite arranged. Every case before this one handed
        // Read-DeskState looks somebody typed — which is what made a loop that built them wrong
        // invisible.
        //
        // 'asking' is the right answer here and reads oddly: this dialog is not waiting for anybody.
        // What the word means is one window held the foreground for every look, and that is exactly
        // true — which is the reading the probe is for, and the reason the runner names the process
        // and the title rather than refusing on the state alone.
        using var dialog = PumpedDialog.Open("winwright desk held");
        dialog.BringToFront();

        if (BusyDesk.Excused(Winwright.Windowing.Foreground.Check(dialog.Frame).AsPrecondition()))
            return;

        var answer = Classified("(Get-DeskLooks -Count 2 -PauseMs 0)").Single();

        Assert.StartsWith("asking|testhost|", answer, StringComparison.Ordinal);
        Assert.Contains("|Static|winwright desk held", answer, StringComparison.Ordinal);
    }

    /// <summary>
    /// Run the probe's own polling and hand back one line per look. WW357.
    /// <para>
    /// Pipe-separated for the reason the probe's own answer is: the fields carry spaces and a reader
    /// splitting on those would find five where a title had two words. A null look prints nothing,
    /// which is how a case tells "the desktop was there" from "a window was".
    /// </para>
    /// </summary>
    /// <param name="arguments">What to pass Get-DeskLooks, as PowerShell spells it.</param>
    private static IReadOnlyList<string> Looked(string arguments) => Ran($$"""
        $looks = Get-DeskLooks {{arguments}}
        foreach ($one in $looks) {
            if ($null -eq $one) { 'the desktop' }
            else { "$($one.Handle)|$($one.Pid)|$($one.Process)|$($one.Class)|$($one.Title)|$($one.Iconic)" }
        }
        """);

    /// <summary>
    /// Run the clearer's own decision over each window, and answer what it said. WW371.
    /// <para>
    /// Through PowerShell and dot-sourced with <c>-DefineOnly</c>, for the reason
    /// <see cref="Classified" /> is: a copy of the rule in C# would agree with itself forever while
    /// the file the guest runs drifted away from it, and nothing here touches a desk.
    /// </para>
    /// </summary>
    /// <param name="windows">One <c>Test-Clearable</c> call per window, as PowerShell spells it.</param>
    private static IReadOnlyList<string> Cleared(params string[] windows)
    {
        var script = Path.Combine(Path.GetTempPath(), $"winwright-ww371-{Guid.NewGuid():N}.ps1");
        var clearer = Checkout.At("tools", "desk-clear.ps1");

        File.WriteAllText(
            script,
            $$"""
            Set-StrictMode -Version Latest
            $ErrorActionPreference = 'Stop'
            . '{{clearer}}' -DefineOnly
            {{string.Join(Environment.NewLine, windows)}}
            """);

        try
        {
            var said = Answered(script);

            // The count and not only the content, for the reason Classified checks it: a script that
            // threw halfway answers fewer lines than it was asked for, and comparing the ones that
            // arrived against the first few expectations reports the wrong window as the wrong answer.
            Assert.True(
                said.Count == windows.Length,
                $"asked about {windows.Length} window(s) and got {said.Count}: {string.Join(" / ", said)}");

            return said;
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

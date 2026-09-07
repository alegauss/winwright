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
/// WW370 and WW383 are the same repair on the two lists this file turns on. Neither can be arranged:
/// no case can hold a desk the desktop is holding, and none can hold one the taskbar is. So each
/// list takes a parameter defaulting to itself, a case names its own dialog's class, and the branch
/// that was reachable only from a real guest runs against a window this suite put up. What is on
/// each list stays a separate claim, checked by the case that reads both out of the file — a
/// parameter says the list is consulted and says nothing about what is in it.
/// </para>
/// <para>
/// The twelve looks over six seconds stay the guest's, and they are a measurement rather than a
/// shape: what they are for is that a toast lives for seconds and the prompt that cost a run had
/// been up for hours. A case that waited them out would be paying six seconds to learn what two
/// looks already say about how a look is built.
/// </para>
/// <para>
/// WW384 reached the clearer's acting half, and reached one arm of it. `Clear-TheDesk` is a function
/// now, so a case puts up a window with no minimise button, runs the whole repair against the desk
/// that window is holding, and reads off the desk that nothing moved — the arm that must never move
/// one.
/// </para>
/// <para>
/// The other arm cannot be a case here, and that was measured rather than argued. Written, it passed
/// and took five cases in two other classes down with it and excused four more in this one; written
/// again with the desktop handed back by the same key that took it, identically. The repair ends in
/// Win+D, and showing the desktop sets Windows' foreground lock — for the timeout after it nothing
/// this process asks for is granted, not a restore, not <c>BringToFront</c>, not a window a later
/// case creates and activates from its own thread. So the price of that reading is every case that
/// runs in the next few minutes, which is an unrelated red bought with a real one.
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
    public void The_guest_console_is_started_with_handles_of_its_own()
    {
        // WW396. `vmrun start ... gui` launches VMware's own window, which outlives this script by
        // design — it is the console a person watches — and it inherits the handles it was launched
        // with. Started inside a job those are the runner's, and the runner's are its caller's: a
        // run piped anywhere printed nothing for sixty-five minutes after the script had exited,
        // because the write end of that pipe was open in a window nobody was waiting for.
        //
        // Read as the shape of the launch, which is the only place the fault can be. Nothing in a
        // suite can watch a pipe stay open for an hour, and nothing here starts a VM — so what is
        // checked is that the line which does hands its child files rather than whatever it was
        // handed.
        var runner = Runner();

        // The code and not the prose around it, which this case learned by failing on the comment
        // that explains it: a rule written as "these words are not here" is one every sentence
        // about the rule breaks.
        var starting = string.Join(
            Environment.NewLine,
            Between(runner, "$argv = Get-VmRunArguments -Arguments @('start'", "$deadline")
                .Split('\n')
                .Where(one => !one.TrimStart().StartsWith('#')));

        // Neither of the two things that make Start-Process launch the child itself, because either
        // one passes this process's handles down. Redirecting the start's own output was tried and
        // measured: a cold run hung exactly as it had, since a redirected launch still inherits
        // every other handle and the caller's pipe is one of them.
        Assert.DoesNotContain("-NoNewWindow", starting, StringComparison.Ordinal);
        Assert.DoesNotContain("-RedirectStandard", starting, StringComparison.Ordinal);

        // And not through a job either, which is the shape it started as: a job's child gets this
        // process's handles for the same reason.
        Assert.DoesNotContain("Start-Job", starting, StringComparison.Ordinal);

        // What is left has to actually start something, or this is three absences about nothing.
        Assert.Contains("Start-Process", starting, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_desk_the_runner_tidies_is_one_it_declares_it_tidies()
    {
        // WW388. WW371 and WW375 landed an hour apart and answered one desk two ways — a minimised
        // window holding the foreground is both `stale`, which the run goes on with, and exactly
        // what the clearer puts away. Nothing decided between them and the order settled it: `stale`
        // is classified first, so the window WW371 was filed about was reported and stepped over.
        //
        // Both are tidied now, and what this holds is the sentence rather than the choice. A state
        // that quietly starts clearing is a run touching a desk nobody said it would; one that
        // quietly stops is the defect WW388 is, back again. The list is the claim and the arms are
        // the code, and this reads them against each other the way every other catalogue here is
        // read.
        var runner = Runner();

        var declared = Between(runner, "$script:Tidied = @(", ")")
            .Split(',')
            .Select(one => one.Trim().Trim('\''))
            .Where(one => one.Length > 0)
            .ToList();

        Assert.NotEmpty(declared);

        // Each arm's body is what stands between its own head and the next one, which is why the
        // default arm is matched too: without it the last state's body would run to the end of the
        // file and read as clearing whatever came after the switch.
        var arms = System.Text.RegularExpressions.Regex
            .Matches(runner, @"(?m)^    (?:'(?<state>\w+)'|(?<state>default)) \{")
            .ToList();

        Assert.True(arms.Count > States.Length, "the runner's switch was not read, so nothing below means anything");

        var tidies = new List<string>();
        for (var at = 0; at < arms.Count - 1; at++)
        {
            var body = runner[arms[at].Index..arms[at + 1].Index];
            if (body.Contains("Clear-GuestDesk", StringComparison.Ordinal))
                tidies.Add(arms[at].Groups["state"].Value);
        }

        Assert.Equal(declared.Order(StringComparer.Ordinal), tidies.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void A_tree_something_is_running_from_is_reported_with_the_name_holding_it()
    {
        // WW404. Windows refuses the sync with the directory it would not delete and never with the
        // name of what has it open, and that sentence was the whole of what came back — WW386's
        // bound leaves a suite exactly where it was, so the next run met `the process cannot access
        // the file C:\src\winwright` and finding out which process meant already knowing.
        //
        // The walk runs in the guest, so what is checked here is the walk and not the refusal: this
        // builds the state the guest gets into — a tree with a live process running out of it — and
        // asks the runner's own code who has it. Read out of the generated script rather than
        // copied, because a case holding its own copy would pass over a guest that lost this one.
        var tree = Path.Combine(Path.GetTempPath(), $"winwright-ww404-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tree);

        // A copy, so what answers is a process of this tree and not the system's own cmd. It runs
        // long enough for the walk to see it and is stopped below either way.
        var holder = Path.Combine(tree, "holder.exe");
        File.Copy(Path.Combine(Environment.SystemDirectory, "cmd.exe"), holder);

        var script = Path.Combine(Path.GetTempPath(), $"winwright-ww404-{Guid.NewGuid():N}.ps1");

        try
        {
            // WW201's door, and this case is the exact shape it was written for: the tree deleted
            // below is the one the image runs out of, and stopped is not gone. The block closes
            // before the delete, on the way out of an assertion as much as past one.
            using (var settling = Attachable.Settling())
            {
                var walking = Attachable.Launch(
                    settling.Register,
                    new ProcessStartInfo(holder)
                    {
                        ArgumentList = { "/c", "ping", "127.0.0.1", "-n", "60" },
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    });

                // WW415: dot-sourced off the file rather than cut out of a here-string. The walk
                // has two callers now — the sync that cannot delete the tree, and the bound that is
                // about to leave a process running in it — so it is a script of its own, and this
                // drives the one the guest is sent.
                var walk = Checkout.At("tools", "holders.ps1");

                Assert.True(File.Exists(walk), $"the holder walk is missing: {walk}");

                File.WriteAllText(
                    script,
                    $$"""
                    $ErrorActionPreference = 'Stop'
                    . '{{walk}}' -DefineOnly
                    Get-WhatHolds -Tree '{{tree}}' | ForEach-Object { Write-Output $_ }
                    """);

                var said = Answered(script);

                // The pid as well as the name, because the sentence exists to be acted on: two runs
                // wedged in the guest are two of the same name, and a reader ending the wrong one
                // has done nothing except lose the state that would have said why.
                Assert.Contains(
                    said,
                    one => one.Contains($"({walking.Pid})", StringComparison.Ordinal)
                        && one.Contains("holder", StringComparison.OrdinalIgnoreCase));

                // And nothing else, which is the half that makes the sentence worth printing: a
                // walk answering every process on the machine names the holder and buries it.
                Assert.All(said, one => Assert.Contains(tree, one, StringComparison.OrdinalIgnoreCase));
            }
        }
        finally
        {
            File.Delete(script);
            Directory.Delete(tree, recursive: true);
        }
    }

    [Fact]
    public void What_the_collector_left_is_gathered_to_the_names_the_host_knows_how_to_ask_for()
    {
        // WW406. A test host that stops answering leaves a dump of every thread and a sequence
        // naming the case still running, and both were lost three times: they are written under a
        // directory named for a GUID, in a file named for the host's pid and the minute it gave up,
        // and the run that would explain the last one is the run whose sync deletes it.
        //
        // vmrun copies by exact path and cannot glob, so the guest has to do the finding. This runs
        // that finding — read out of the generated script, not copied — against a tree shaped like
        // the one the collector leaves, including the second copy it makes of both files.
        var where = Path.Combine(Path.GetTempPath(), $"winwright-ww406-{Guid.NewGuid():N}");
        var sync = Path.Combine(Path.GetTempPath(), $"winwright-ww406-sync-{Guid.NewGuid():N}");
        var script = Path.Combine(Path.GetTempPath(), $"winwright-ww406-{Guid.NewGuid():N}.ps1");

        Directory.CreateDirectory(sync);
        var under = Directory.CreateDirectory(Path.Combine(where, Guid.NewGuid().ToString())).FullName;
        var copied = Directory.CreateDirectory(Path.Combine(where, "oobe_MACHINE", "In", "MACHINE")).FullName;

        try
        {
            // The one that matters is the larger, because a truncated write is the other one: the
            // collector copies what it has when it copies, and the file it is still writing is the
            // file with the whole answer in it.
            var whole = Path.Combine(under, "testhost_10400_20260906T150844_hangdump.dmp");
            File.WriteAllBytes(whole, new byte[4096]);
            File.WriteAllBytes(Path.Combine(copied, "testhost_10400_20260906T150844_hangdump.dmp"), new byte[1024]);

            var older = Path.Combine(copied, "Sequence_aaa.xml");
            File.WriteAllText(older, "<TestSequence />");
            File.SetLastWriteTimeUtc(older, DateTime.UtcNow.AddMinutes(-10));

            var newest = Path.Combine(under, "Sequence_bbb.xml");
            File.WriteAllText(newest, "<TestSequence><Test Name=\"the last one\" /></TestSequence>");

            File.WriteAllText(script, Gathering(where, sync));
            Answered(script);

            var said = File.ReadAllText(Path.Combine(sync, "blame.txt"));

            Assert.Contains(Path.GetFileName(whole), said, StringComparison.Ordinal);
            Assert.Contains(Path.GetFileName(newest), said, StringComparison.Ordinal);

            // The files themselves and not only the sentence, which is the whole point of the step:
            // a note naming a dump nobody copied is the state this replaced.
            Assert.Equal(4096, new FileInfo(Path.Combine(sync, "blame.dmp")).Length);
            Assert.Contains("the last one", File.ReadAllText(Path.Combine(sync, "blame-sequence.xml")), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(script);
            Directory.Delete(where, recursive: true);
            Directory.Delete(sync, recursive: true);
        }
    }

    [Fact]
    public void A_run_the_collector_left_nothing_for_says_so_rather_than_going_quiet()
    {
        // WW406, the other answer. A host that exited on its own leaves no dump, and that is a
        // reading: it says the run ended rather than was waited out. Written down, because a line
        // that appears only when there is a dump makes its absence read as a step that did not run.
        var where = Directory.CreateTempSubdirectory("winwright-ww406-empty-").FullName;
        var sync = Directory.CreateTempSubdirectory("winwright-ww406-nothing-").FullName;
        var script = Path.Combine(Path.GetTempPath(), $"winwright-ww406-{Guid.NewGuid():N}.ps1");

        try
        {
            File.WriteAllText(script, Gathering(where, sync));
            Answered(script);

            Assert.Equal("nothing", File.ReadAllText(Path.Combine(sync, "blame.txt")).Trim());
            Assert.False(File.Exists(Path.Combine(sync, "blame.dmp")));
            Assert.False(File.Exists(Path.Combine(sync, "blame-sequence.xml")));
        }
        finally
        {
            File.Delete(script);
            Directory.Delete(where, recursive: true);
            Directory.Delete(sync, recursive: true);
        }
    }

    /// <summary>
    /// The gather the runner generates for the guest, aimed at two directories of this case's own.
    /// WW406.
    /// <para>
    /// Read out of the runner rather than copied here. A case carrying its own copy passes over a
    /// guest that lost this one, which is the failure the whole step exists to stop.
    /// </para>
    /// </summary>
    /// <param name="results">Where the collector's files are, standing in for the guest's results.</param>
    /// <param name="sync">Where the gather puts what it found, standing in for the sync folder.</param>
    private static string Gathering(string results, string sync)
    {
        var runner = Runner();
        var ends = runner.IndexOf("\"@ | Set-Content -LiteralPath (Join-Path $stage 'blame.ps1')", StringComparison.Ordinal);

        Assert.True(ends > 0, "the runner no longer generates a gather for the guest to run");

        var opens = runner.LastIndexOf("@\"", ends, StringComparison.Ordinal);

        return runner[(opens + 2)..ends]
            .Replace("$script:GuestRepo\\$script:ResultsIn", results, StringComparison.Ordinal)
            .Replace("$script:GuestSync", sync, StringComparison.Ordinal)
            .Replace("`$", "$", StringComparison.Ordinal);
    }

    [Fact]
    public void The_session_probe_is_the_one_call_that_waits_for_a_guest_to_finish_logging_in()
    {
        // WW412. The runner spends ten minutes on VMware Tools answering and used to spend nothing
        // at all on the desk those tools exist to reach: a guest powered on by this run had booted,
        // had not finished logging in, and was refused — the same command ninety seconds later
        // carried the whole suite. A refusal true when it was made and false about the machine.
        //
        // Read as which call asks for the wait, because that is the half a reader cannot see and
        // the half that would go wrong: every other caller runs after a session has been proved, so
        // one that asked for a wait would be quietly sitting out a real refusal.
        var runner = Runner();

        var asking = System.Text.RegularExpressions.Regex
            .Matches(runner, @"-SessionWithinMinutes\s+(?<given>\S+)")
            .Select(one => one.Groups["given"].Value)
            .ToList();

        Assert.Single(asking);
        Assert.Equal("$script:SessionMinutes", asking[0]);

        // The call it is on, so the one wait cannot drift to a different question. `cmd /c exit` is
        // the session probe: the cheapest program that cannot run without a session.
        var probe = Between(runner, "'C:\\Windows\\System32\\cmd.exe', '/c', 'exit')", "Write-Host '  desk");
        Assert.Contains("-SessionWithinMinutes", probe, StringComparison.Ordinal);

        // And a number that is a wait rather than a nod at one. Bounded for the reason WW386 gives
        // about the run itself: a wait that cannot end is worse than a refusal.
        var minutes = Between(runner, "$script:SessionMinutes = ", "\n").Trim();

        Assert.True(int.TryParse(minutes, out var waiting), $"the session wait is '{minutes}'");
        Assert.InRange(waiting, 1, 10);
    }

    [Fact]
    public void The_host_gate_takes_the_classes_that_do_not_need_a_desk_and_only_those()
    {
        // WW417. The gate is derived from the collection this project already uses to say which
        // classes need a desk, so it cannot drift the way a written list would — and it is run
        // rather than read, because what would go wrong is the derivation and not its shape.
        //
        // Both ways, off classes this file can name for certain: it is itself in the serial
        // collection and must not be gated, and the rules that read sources must be.
        var said = RanGate("Get-HostFilter -Suite '" + Checkout.Suite.Replace("'", "''") + "'");
        var filter = string.Join("", said);

        Assert.Contains("FullyQualifiedName~Winwright.Tests.SettledTeardownTests.", filter, StringComparison.Ordinal);
        Assert.Contains("FullyQualifiedName~Winwright.Tests.CriteriaTests.", filter, StringComparison.Ordinal);

        // And not one of the ones that take the desk. This class is the example it can be surest
        // about: everything in this file drives a real foreground.
        Assert.DoesNotContain($"Winwright.Tests.{nameof(DeskProbeTests)}.", filter, StringComparison.Ordinal);
        Assert.DoesNotContain("Winwright.Tests.NotificationAreaTests.", filter, StringComparison.Ordinal);

        // Anchored on the namespace and closed with a dot, which is not decoration: `SweepTests`
        // and `SourceSweepTests` are a substring pair, and a filter that matched loosely would pull
        // a serial class in behind one that is not.
        Assert.All(
            filter.Split('|'),
            one => Assert.Matches(@"^FullyQualifiedName~Winwright\.Tests\.\w+\.$", one));
    }

    /// <summary>Dot-source the host gate and run what a caller asked. WW417.</summary>
    /// <param name="line">The PowerShell to run once the gate is defined.</param>
    private static IReadOnlyList<string> RanGate(string line)
    {
        var script = Path.Combine(Path.GetTempPath(), $"winwright-ww417-{Guid.NewGuid():N}.ps1");
        var gate = Checkout.At("tools", "host-gate.ps1");

        File.WriteAllText(
            script,
            $$"""
            Set-StrictMode -Version Latest
            $ErrorActionPreference = 'Stop'
            . '{{gate}}' -DefineOnly
            {{line}}
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
    private static IReadOnlyList<string> Classified(params string[] looks) => ClassifiedWith("", looks);

    /// <summary>
    /// The same, with something further said to the classification. WW383, which gave it a list to
    /// take.
    /// <para>
    /// A method of its own rather than an optional argument on <see cref="Classified" />, because an
    /// overload taking a string first would swallow every existing single-set call: the compiler
    /// would bind the looks to the new parameter and hand the classification nothing, and the case
    /// would fail somewhere that says nothing about what it was asking.
    /// </para>
    /// </summary>
    /// <param name="andThen">What follows <c>-Looks</c>, as PowerShell spells it.</param>
    /// <param name="looks">One PowerShell expression per set, each an array of looks or nulls.</param>
    private static IReadOnlyList<string> ClassifiedWith(string andThen, params string[] looks)
    {
        var lines = Ran(string.Join(
            Environment.NewLine,
            looks.Select(one => $"Read-DeskState -Looks {one} {andThen}")));

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

    [Fact]
    public void A_desk_held_by_a_shell_surface_is_read_as_shell_end_to_end()
    {
        // WW383. `shell` is the one answer nothing had ever produced from a real look. WW345 made the
        // classification runnable and WW357 made the loop runnable, and the case joining them arrives
        // at `asking` — because a case can arrange a desk its own dialog is holding and cannot arrange
        // one the taskbar is. So the arm that decides whether a reader is sent to a guest console was
        // only ever reached by looks somebody typed, and a typed look is one that cannot be built
        // wrong.
        //
        // Which matters here more than anywhere else in this file, because `shell` is the answer a
        // person acts on by NOT going to the console. WW331 is what it costs when the word is wrong:
        // a focused chevron read as a question and refused every later run, and a reader was sent to
        // answer a prompt a capture showed was not there.
        //
        // The join is WW370's, one list over. The case names its own dialog's class a shell surface
        // and the probe polls the live foreground, so what runs is the whole path — a real window, a
        // look this suite did not write, the list consulted, and the word the runner switches on.
        using var dialog = PumpedDialog.Open("winwright desk shell");
        dialog.BringToFront();

        if (BusyDesk.Excused(Winwright.Windowing.Foreground.Check(dialog.Frame).AsPrecondition()))
            return;

        var held = ClassifiedWith("-Shell 'Static'", "(Get-DeskLooks -Count 2 -PauseMs 0)").Single();

        // Something else taking the foreground between the looks is the desk and not the list: the
        // answer is `busy` or `clear`, and neither says anything about which word a held desk gets.
        // `asking` is not excused, because that is the list going unread — the whole finding.
        if (!held.StartsWith("shell|", StringComparison.Ordinal)
            && !held.StartsWith("asking|", StringComparison.Ordinal)
            && BusyDesk.Excused(
                Winwright.Verdicts.Precondition.Absent(
                    "the foreground belongs to the window under test",
                    $"the probe read the desk as '{held}' rather than as the dialog this case put up")))
        {
            return;
        }

        Assert.StartsWith("shell|testhost|", held, StringComparison.Ordinal);
        Assert.Contains("|Static|winwright desk shell", held, StringComparison.Ordinal);

        // And the same window with the list back as it is, which is what makes the line above about
        // the list rather than about this dialog. WW370's case ends the same way and for the reason:
        // a case that only ever passed its own list could not tell a list being read from a window
        // that reads as a shell surface whatever anybody says.
        var byTheList = Classified("(Get-DeskLooks -Count 2 -PauseMs 0)").Single();

        if (!byTheList.StartsWith("shell|", StringComparison.Ordinal)
            && !byTheList.StartsWith("asking|", StringComparison.Ordinal)
            && BusyDesk.Excused(
                Winwright.Verdicts.Precondition.Absent(
                    "the foreground belongs to the window under test",
                    $"the probe read the desk as '{byTheList}' rather than as the dialog this case put up")))
        {
            return;
        }

        Assert.StartsWith("asking|testhost|", byTheList, StringComparison.Ordinal);
    }

    [Fact]
    public void The_clearer_leaves_a_real_window_with_no_minimise_button_exactly_where_it_is()
    {
        // WW384. `Test-Clearable` has been reachable since WW371 and the acting half had not — it ran
        // for nobody but the runner, on a refusal, on a guest. So every case about this file asked
        // what the decision says and none asked what the script does with the answer, which is the
        // room WW345 left in the probe: a decision that is right and an act that is not.
        //
        // This is the arm that must never move a window. A modal question has no minimise button and
        // is the whole reason the clearer exists to be careful, so what is asserted is a window still
        // standing after the script ran against it — not a sentence, which is the thing that would go
        // on being written by a script that had minimised it anyway.
        //
        // `PumpedDialog.Open` is WS_POPUP, so it carries no WS_MINIMIZEBOX and it is a question as far
        // as this file is concerned. The dialog holds the foreground, which is what makes the script
        // find it rather than something else.
        using var dialog = PumpedDialog.Open("winwright clearer leaves alone");
        dialog.BringToFront();

        if (BusyDesk.Excused(Winwright.Windowing.Foreground.Check(dialog.Frame).AsPrecondition()))
            return;

        var said = Clearing();

        Assert.Contains("left 'winwright clearer leaves alone' (Static) alone", said, StringComparison.Ordinal);

        // And the desk itself, because the sentence is the script's own account of what it did. This
        // is the assertion the words cannot stand in for: the window is up, unminimised, and still
        // holds the foreground it held before the script ran.
        Assert.False(Iconic(dialog.Frame), $"the clearer minimised a window it said it left alone: {said}");
        Assert.True(
            Winwright.Windowing.Foreground.Check(dialog.Frame).Ours,
            $"the clearer handed the desk on from a window it said it left alone: {said}");
    }

    [Fact]
    public void A_minimised_window_that_still_holds_the_desk_is_read_as_stale_end_to_end()
    {
        // WW400. `stale` was only ever made of looks somebody typed, and it is the answer whose
        // whole content is a field the loop reads: WW375 put `Iconic` on the look because the
        // classification is a pure function of what the loop returns, and a loop that answered it
        // wrong would send a reader to a guest console to answer a window nobody can see — which is
        // the failure WW375 exists for, arrived at from the loop instead of from the words.
        //
        // Arranged rather than waited for, and WW375's own finding is what makes it arrangeable:
        // Windows keeps a minimised window as the foreground until something else claims it, so a
        // dialog this case put down is both down and in front.
        using var dialog = PumpedDialog.Open("winwright desk stale");
        dialog.BringToFront();

        if (BusyDesk.Excused(Winwright.Windowing.Foreground.Check(dialog.Frame).AsPrecondition()))
            return;

        Assert.True(ShowWindow(dialog.Frame, Minimise), "the window would not go down");

        var answer = Classified("(Get-DeskLooks -Count 2 -PauseMs 0)").Single();

        // Something else taking the desk while the window went down is the desk and not the loop.
        // `asking` is deliberately not excused: that is the loop reading a minimised window as an
        // ordinary one, which is the whole of what this case is for.
        if (!answer.StartsWith("stale|", StringComparison.Ordinal)
            && !answer.StartsWith("asking|", StringComparison.Ordinal)
            && BusyDesk.Excused(
                Winwright.Verdicts.Precondition.Absent(
                    "the foreground belongs to the window under test",
                    $"the probe read the desk as '{answer}' after this case put its window down")))
        {
            return;
        }

        Assert.StartsWith("stale|testhost|", answer, StringComparison.Ordinal);
        Assert.Contains("|Static|winwright desk stale", answer, StringComparison.Ordinal);
    }

    [Fact]
    public void A_desk_with_nothing_but_the_desktop_on_it_is_read_as_clear_end_to_end()
    {
        // WW400, and one line past where WW370 stopped. That case asserts the loop skips a class on
        // the desktop list and never hands the looks it built to the classification — so "nothing
        // but the desktop held the foreground", which is what an idle logged-in desk answers and
        // what decides a run happens at all, was a sentence no case had produced from a real poll.
        //
        // The desktop cannot be arranged and does not have to be: a case names its own window's
        // class as the desktop's, the loop skips it exactly as it skips Progman, and every look is
        // nothing. That is the same set of looks an idle desk produces, built by the loop.
        using var dialog = PumpedDialog.Open("winwright desk clear");
        dialog.BringToFront();

        if (BusyDesk.Excused(Winwright.Windowing.Foreground.Check(dialog.Frame).AsPrecondition()))
            return;

        // The list goes to the loop and nowhere else: what it decides is which windows are skipped
        // while looking, and the classification is a pure function of the looks that came back.
        var answer = Classified("(Get-DeskLooks -Count 2 -PauseMs 0 -Desktop 'Static')").Single();

        Assert.Equal("clear||||nothing but the desktop held the foreground", answer);
    }

    /// <summary>SW_MINIMIZE, which puts a window down without activating what is behind it. WW400.</summary>
    private const int Minimise = 6;

    /// <summary>
    /// Put a window down. WW400, and the one act this class performs on its own window: every other
    /// case here arranges a desk by what it shows, and `stale` is the one answer that needs a window
    /// shown and then hidden.
    /// </summary>
    /// <param name="window">The window to put down.</param>
    /// <param name="how">What to do with it.</param>
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint window, int how);

    /// <summary>
    /// Whether a window is down, which is the half of the repair its own sentence cannot show. WW384,
    /// and read here rather than taken from the script for that reason: the script saying
    /// <c>iconic=True</c> is the script's account of itself, and a repair that moved no window would
    /// go on writing it.
    /// </summary>
    /// <param name="window">The window to read.</param>
    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "IsIconic")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool Iconic(nint window);

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
    private static IReadOnlyList<string> Cleared(params string[] windows) => RanClearer(windows);

    /// <summary>
    /// Run the clearer against the desk this case is holding, and answer the one sentence it wrote.
    /// WW384.
    /// <para>
    /// The same launch as <see cref="Cleared" /> and a different thing entirely: that one asks the
    /// decision about windows nobody put up, and this one lets the script find the foreground for
    /// itself, act on it, and say what it did. It is the half that touches a desk, which is why it
    /// has a name of its own — a caller reaching for the wrong one would minimise the window under
    /// test.
    /// </para>
    /// </summary>
    private static string Clearing() => RanClearer(["Clear-TheDesk"]).Single();

    /// <summary>Dot-source the clearer and run what a caller asked, line by line. WW371, WW384.</summary>
    /// <param name="lines">The PowerShell to run once the clearer is defined.</param>
    private static IReadOnlyList<string> RanClearer(IReadOnlyList<string> lines)
    {
        var script = Path.Combine(Path.GetTempPath(), $"winwright-ww371-{Guid.NewGuid():N}.ps1");
        var clearer = Checkout.At("tools", "desk-clear.ps1");

        File.WriteAllText(
            script,
            $$"""
            Set-StrictMode -Version Latest
            $ErrorActionPreference = 'Stop'
            . '{{clearer}}' -DefineOnly
            {{string.Join(Environment.NewLine, lines)}}
            """);

        try
        {
            var said = Answered(script);

            // The count and not only the content, for the reason Classified checks it: a script that
            // threw halfway answers fewer lines than it was asked for, and comparing the ones that
            // arrived against the first few expectations reports the wrong window as the wrong answer.
            Assert.True(
                said.Count == lines.Count,
                $"asked about {lines.Count} window(s) and got {said.Count}: {string.Join(" / ", said)}");

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

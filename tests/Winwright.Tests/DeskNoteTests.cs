using System.Diagnostics;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW600. What a red guest run may say about the desk it started on.
/// <para>
/// WW472's note said a red run on a 'shell' desk "reads as a broken tree and is not". wintimeline met
/// it twice on 2026-11-04 and both reds were its own defects, so the note sent the session to restart
/// the guest shell instead of reading its failure. The note now claims only what the run can show:
/// the roll call's excused count where the guest's log holds one, and otherwise that the runner
/// cannot tell. This drives <c>tools/desk-note.ps1</c>, the script the runner dot-sources.
/// </para>
/// <para>
/// Serial because it starts a process, and needing no desk because that process is a windowless
/// PowerShell that reads a string.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class DeskNoteTests : IDisposable
{
    private readonly string where = Directory.CreateTempSubdirectory("winwright-desk-note-").FullName;

    public void Dispose() => Directory.Delete(where, recursive: true);

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_log_holding_the_roll_calls_count_has_that_count_quoted()
    {
        var said = Noted(
            "shell",
            "  Passed!  - Failed: 1, Passed: 1112\r\n"
                + "  all 1113 discovered cases ran, and 137 check(s) were excused against 9 the run before"
                + " — 137 for no foreground\r\n");

        var whole = string.Join("\n", said);
        Assert.Contains("read 'shell'", whole, StringComparison.Ordinal);
        Assert.Contains(
            "the roll call says: 137 check(s) were excused against 9 the run before",
            whole,
            StringComparison.Ordinal);

        // Cut where the ASCII ends: the facts after the dash are not the count, and an older log
        // holds that dash double-encoded.
        Assert.DoesNotContain("for no foreground", whole, StringComparison.Ordinal);
        Assert.DoesNotContain("broken tree and is not", whole, StringComparison.Ordinal);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_log_without_the_count_says_the_runner_cannot_tell_and_never_that_the_tree_is_fine()
    {
        // An adopter's log, which carries no roll call at all, and one whose ledger was never written.
        foreach (var log in new[]
        {
            "  Failed WT160 [step 2] nothing matched TabItem #TimelineTab0 (selected)\r\n",
            "  all 40 discovered cases ran, and how many checks the desk excused was not read.\r\n",
        })
        {
            var whole = string.Join("\n", Noted("stale", log));

            Assert.Contains("read 'stale'", whole, StringComparison.Ordinal);
            Assert.Contains("cannot tell a held desk from a broken tree", whole, StringComparison.Ordinal);
            Assert.Contains("Read the red first", whole, StringComparison.Ordinal);
            Assert.DoesNotContain("roll call says", whole, StringComparison.Ordinal);
            Assert.DoesNotContain("and is not", whole, StringComparison.Ordinal);
        }
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void No_log_back_is_read_as_no_count()
    {
        var whole = string.Join("\n", Noted("shell", ""));

        Assert.Contains("cannot tell", whole, StringComparison.Ordinal);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_desk_that_started_clear_has_nothing_to_explain()
    {
        Assert.Empty(Noted("clear", "  all 10 discovered cases ran, and 2 check(s) were excused\r\n"));
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void The_runner_prints_this_note_and_no_other_under_a_failure()
    {
        var runner = File.ReadAllText(Checkout.At("tools", "run-tests-vm.ps1"));

        Assert.Contains("'desk-note.ps1') -DefineOnly", runner, StringComparison.Ordinal);
        Assert.DoesNotContain("reads as a broken tree and is not", runner, StringComparison.Ordinal);
    }

    /// <summary>
    /// The lines <c>Get-DeskNote</c> answers for a run that started on <paramref name="desk"/> and
    /// whose guest log held <paramref name="log"/>, passed through a file so no quoting reaches it.
    /// </summary>
    private List<string> Noted(string desk, string log)
    {
        var logFile = Path.Combine(where, "vm-run.log");
        File.WriteAllText(logFile, log);

        var script = Path.Combine(where, "note.ps1");
        File.WriteAllText(
            script,
            $$"""
            $ErrorActionPreference = 'Stop'
            . '{{Checkout.At("tools", "desk-note.ps1")}}' -DefineOnly
            $log = [IO.File]::ReadAllText('{{logFile}}', [Text.Encoding]::UTF8)
            Get-DeskNote -Desk '{{desk}}' -Log $log | ForEach-Object { Write-Output $_ }
            """);

        var ran = Process.Start(new ProcessStartInfo("powershell.exe")
        {
            ArgumentList = { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", script },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        })!;

        var said = ran.StandardOutput.ReadToEnd();
        var wrong = ran.StandardError.ReadToEnd();
        ran.WaitForExit(30000);

        Assert.True(wrong.Trim().Length == 0, $"the script wrote to standard error: {wrong}");
        return said.Split('\n').Select(one => one.Trim('\r', ' ')).Where(one => one.Length > 0).ToList();
    }
}

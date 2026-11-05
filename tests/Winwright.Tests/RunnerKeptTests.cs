using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW599. A guest run names, as its last lines, where it kept the guest's log on the host.
/// <para>
/// Met in wintimeline: a red run's console was read through a filter, the lines naming the broken
/// case were dropped, and the session reran a ten-minute guest run to read a log that was already in
/// <c>TestResults\vm\vm-run.log</c>. Nothing the runner printed named that file. Read from the
/// runner's own text, the way the other runner cases are, because the closing lines only run at the
/// end of a guest run.
/// </para>
/// </summary>
public sealed class RunnerKeptTests
{
    private static string Runner() => File.ReadAllText(Checkout.At("tools", "run-tests-vm.ps1"));

    [Fact]
    public void The_log_path_is_named_after_the_verdict_so_any_tail_of_the_output_keeps_it()
    {
        var runner = Runner();

        var verdict = runner.IndexOf("run-tests-vm: the guest run exited $code.", StringComparison.Ordinal);
        var named = runner.IndexOf("Write-Host \"  log         $logFile (the guest's console, whole)\"", StringComparison.Ordinal);
        var kept = runner.IndexOf("foreach ($path in $fetchedHere) { Write-Host \"  kept        $path\" }", StringComparison.Ordinal);
        var exit = runner.LastIndexOf("exit $code", StringComparison.Ordinal);

        Assert.True(verdict > 0, "the runner no longer prints its verdict line");
        Assert.True(named > verdict, "the log's host path must be printed after the verdict");
        Assert.True(kept > verdict, "the brought files must be named after the verdict");
        Assert.True(exit > named && exit > kept, "the paths must be printed before the runner exits");
    }

    [Fact]
    public void Only_what_this_run_fetched_is_named_never_an_earlier_runs_file()
    {
        var runner = Runner();

        // The log's line and its whole printout both hang off this run's own fetch, not off the
        // file existing: TestResults\vm outlives runs, so an old log there is the wrong evidence.
        Assert.Contains("$logBack = Invoke-VmRun -Guest -Arguments @('copyFileFromGuestToHost', $vmxPath, \"$script:GuestSync\\vm-run.log\", $logFile)", runner, StringComparison.Ordinal);
        Assert.Contains("if ($logBack.Ok) { Write-Host \"  log ", runner, StringComparison.Ordinal);
        Assert.Contains("if ($logBack.Ok -and (Test-Path -LiteralPath $logFile))", runner, StringComparison.Ordinal);

        // Every brought file joins the list only when its copy answered.
        Assert.Contains("else { $fetchedHere.Add((Join-Path $results $first)) }", runner, StringComparison.Ordinal);
        Assert.Contains("else { $fetchedHere.Add((Join-Path $results $also)) }", runner, StringComparison.Ordinal);
    }
}

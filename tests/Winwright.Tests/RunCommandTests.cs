using System.Diagnostics;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW576. A guest run's <c>-Run</c>, handed to cmd as a line typed at a prompt.
/// <para>
/// Found by Modelado: the guest script said <c>call &lt;Run&gt; &gt; vm-run.log 2&gt;&amp;1</c>, so a
/// chain had the redirection bound to its last command and the build before it logged nothing, and a
/// <c>for %B</c> was read as a batch line and refused. This drives <c>tools/run-command.ps1</c>, the
/// script the guest is sent, with its output redirected the way the guest's run script redirects it.
/// </para>
/// <para>
/// Serial because it starts a process, and needing no desk because that process is a windowless cmd.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class RunCommandTests : IDisposable
{
    private readonly string where = Directory.CreateTempSubdirectory("winwright-run-command-").FullName;

    public void Dispose() => Directory.Delete(where, recursive: true);

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void Every_command_in_a_chain_reaches_the_log()
    {
        var (said, code) = Ran("echo first & echo second && echo third");

        Assert.Equal(["first", "second", "third"], said);
        Assert.Equal(0, code);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_failing_command_early_in_a_chain_is_the_exit_code_a_prompt_would_give()
    {
        // The control for the code: && stops at the first failure and its code is the line's.
        var (said, code) = Ran("echo building && cmd /c exit 3 && echo never");

        Assert.Equal(["building"], said);
        Assert.Equal(3, code);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_for_loop_is_read_as_a_prompt_reads_it()
    {
        var (said, code) = Ran("for %B in (alpha beta) do @echo %B");

        Assert.Equal(["alpha", "beta"], said);
        Assert.Equal(0, code);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void Quotes_inside_the_command_reach_cmd_as_written()
    {
        var (said, _) = Ran("""echo "a b" & cmd /c "echo c & echo d" """);

        Assert.Equal(["\"a b\"", "c", "d"], said);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void The_runner_hands_its_run_to_this_script_and_sends_it_to_the_guest()
    {
        var runner = File.ReadAllText(Checkout.At("tools", "run-tests-vm.ps1"));

        Assert.DoesNotContain("call $script:Run", runner, StringComparison.Ordinal);
        Assert.Contains("run-command.ps1\" -CommandFile", runner, StringComparison.Ordinal);
        Assert.Contains("'run-command.ps1', 'run-command.txt'", runner, StringComparison.Ordinal);
    }

    /// <summary>
    /// Run <paramref name="command"/> through the script as the guest does, its output redirected to a
    /// file, and answer the lines it wrote and the code it exited with.
    /// </summary>
    private (List<string> Said, int Code) Ran(string command)
    {
        var file = Path.Combine(where, "run-command.txt");
        File.WriteAllText(file, command);
        var log = Path.Combine(where, "vm-run.log");
        var batch = Path.Combine(where, "run.cmd");
        File.WriteAllText(
            batch,
            $"""
            @echo off
            powershell -NoProfile -ExecutionPolicy Bypass -File "{Checkout.At("tools", "run-command.ps1")}" -CommandFile "{file}" -In "{where}" > "{log}" 2>&1
            exit /b %ERRORLEVEL%
            """);

        var ran = Process.Start(new ProcessStartInfo("cmd.exe")
        {
            ArgumentList = { "/d", "/c", batch },
            UseShellExecute = false,
            CreateNoWindow = true,
        })!;
        ran.WaitForExit(30000);

        var said = File.ReadAllLines(log).Select(one => one.Trim()).Where(one => one.Length > 0).ToList();
        return (said, ran.ExitCode);
    }
}

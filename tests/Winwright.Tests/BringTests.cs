using System.Diagnostics;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW578. What a guest run brings back, named through <c>run-tests-vm.cmd</c>.
/// <para>
/// Found by Modelado: <c>-Bring robot.glb,robot-poster.png</c> through the wrapper arrived as one name
/// holding a comma, because <c>powershell -File</c> binds a comma list as one string, and the runner
/// answered that no such file came back. Measured here by asking the runner's own parameter block how
/// the wrapper's spelling binds, and then splitting it the way the runner does.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class BringTests
{
    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_comma_list_through_powershell_file_arrives_as_one_string_and_the_runner_splits_it()
    {
        // The binding the wrapper gets, measured rather than assumed: a script declaring
        // [string[]] $Bring, called with -File and 'a,b', sees one element.
        var where = Directory.CreateTempSubdirectory("winwright-bring-").FullName;
        try
        {
            var script = Path.Combine(where, "bind.ps1");
            File.WriteAllText(script, "param([string[]] $Bring)\n$Bring.Count\n");

            var ran = Process.Start(new ProcessStartInfo("powershell.exe")
            {
                ArgumentList = { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", script, "-Bring", "robot.glb,robot-poster.png" },
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            })!;
            var said = ran.StandardOutput.ReadToEnd().Trim();
            ran.WaitForExit(30000);

            Assert.Equal("1", said);
        }
        finally
        {
            Directory.Delete(where, recursive: true);
        }

        // And the runner splits what arrived, so the wrapper's spelling names two files.
        var runner = File.ReadAllText(Checkout.At("tools", "run-tests-vm.ps1"));
        Assert.Contains("$Bring | ForEach-Object { $_ -split ',' }", runner, StringComparison.Ordinal);
        Assert.Contains("if ($code -eq 0 -or $Run)", runner, StringComparison.Ordinal);
    }
}

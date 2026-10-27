using System.Diagnostics;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW574. The files a guest run carries beside its tree's git listing.
/// <para>
/// Found by Modelado (MOD28): its evidence is its own installer, under an ignored <c>artifacts\</c>,
/// and the runner carried only the git listing, so the project staged a throwaway folder with
/// <c>git init</c> in it. This drives the <c>tools/carry.ps1</c> the runner dot-sources, against a
/// tree built here, so what is checked is the code the runner runs and not a copy of it.
/// </para>
/// <para>
/// Serial because it starts a process, and every case marked as needing no desk because that process
/// is a windowless PowerShell that reads files.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class CarryTests : IDisposable
{
    private readonly string tree = Directory.CreateTempSubdirectory("winwright-carry-").FullName;

    public CarryTests()
    {
        Directory.CreateDirectory(Path.Combine(tree, "artifacts", "installer"));
        File.WriteAllText(Path.Combine(tree, "artifacts", "installer", "Setup-1.0.exe"), "one");
        File.WriteAllText(Path.Combine(tree, "artifacts", "installer", "Setup-1.1.exe"), "two");
        File.WriteAllText(Path.Combine(tree, "artifacts", "notes.txt"), "three");
    }

    public void Dispose() => Directory.Delete(tree, recursive: true);

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_wildcard_names_every_file_it_matches_relative_to_the_tree()
    {
        var said = Carried(@"'artifacts\installer\*.exe'");

        Assert.Equal([@"artifacts\installer\Setup-1.0.exe", @"artifacts\installer\Setup-1.1.exe"], said.Order());
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void Several_patterns_bound_as_one_string_are_split_on_their_commas()
    {
        // WW578's binding: `powershell -File` hands 'a,b' over as one string, so it is split here and
        // a list given through run-tests-vm.cmd carries both files rather than one with a comma in it.
        var said = Carried(@"'artifacts\notes.txt, artifacts\installer\Setup-1.0.exe'");

        Assert.Equal([@"artifacts\installer\Setup-1.0.exe", @"artifacts\notes.txt"], said.Order());
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_file_named_twice_is_carried_once()
    {
        var said = Carried(@"'artifacts\installer\*.exe', 'artifacts\installer\Setup-1.0.exe'");

        Assert.Equal(2, said.Count);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_pattern_that_matches_nothing_is_refused_naming_it()
    {
        // The control: a carry that quietly carried nothing is a guest run that fails on a file nobody
        // named, so the pattern is refused before the guest is touched.
        var said = Carried(@"'artifacts\installer\*.msi'");

        Assert.Equal([$@"REFUSED -Carry 'artifacts\installer\*.msi' matched no file under {tree}"], said);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_file_outside_the_tree_is_refused()
    {
        var said = Carried(@"'..\*'");

        Assert.Single(said);
        Assert.StartsWith("REFUSED ", said[0], StringComparison.Ordinal);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void The_runner_reads_the_carry_and_declares_it()
    {
        var runner = File.ReadAllText(Checkout.At("tools", "run-tests-vm.ps1"));

        Assert.Contains("[string[]] $Carry", runner, StringComparison.Ordinal);
        Assert.Contains("'carry.ps1') -DefineOnly", runner, StringComparison.Ordinal);
        Assert.Contains("Get-Carried -Tree $script:Tree -Pattern $Carry", runner, StringComparison.Ordinal);
    }

    /// <summary>
    /// What <c>Get-Carried</c> answers for <paramref name="patterns"/>, a PowerShell list as written,
    /// one line per file, or one line beginning REFUSED with what it threw.
    /// </summary>
    private List<string> Carried(string patterns)
    {
        var script = Path.Combine(Path.GetTempPath(), $"winwright-carry-{Guid.NewGuid():N}.ps1");
        File.WriteAllText(
            script,
            $$"""
            $ErrorActionPreference = 'Stop'
            . '{{Checkout.At("tools", "carry.ps1")}}' -DefineOnly
            try { Get-Carried -Tree '{{tree}}' -Pattern @({{patterns}}) | ForEach-Object { Write-Output $_ } }
            catch { Write-Output ('REFUSED ' + $_.Exception.Message) }
            """);

        try
        {
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
        finally
        {
            File.Delete(script);
        }
    }
}

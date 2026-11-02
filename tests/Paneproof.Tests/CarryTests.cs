using System.Diagnostics;

using Xunit;

namespace Paneproof.Tests;

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

    public void Dispose()
    {
        // Git writes its objects read-only, and a read-only file stops a recursive delete.
        foreach (var file in Directory.EnumerateFiles(tree, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);

        Directory.Delete(tree, recursive: true);
    }

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

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void Committed_only_carries_what_head_has_and_not_what_the_disk_or_the_index_has()
    {
        // WW589. Found by revelei gating v0.4.0 at HEAD in a checkout another session was editing:
        // the list came from the index and the bytes from the disk, so the guest built a tree that
        // existed nowhere and the runner called it HEAD.
        Committed();

        var carried = Archived(committedOnly: true, @"'artifacts\notes.txt'");

        Assert.Equal("as committed", carried["edited.txt"]);
        Assert.False(carried.ContainsKey("staged.txt"), "a file only staged is not HEAD's");
        Assert.Equal("as committed", carried["deleted.txt"]);

        // -Carry beside it still reads the disk, which is what a carried build output is for.
        Assert.Equal("three", carried["artifacts/notes.txt"]);
        Assert.False(carried.ContainsKey("artifacts/installer/Setup-1.0.exe"));
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void The_working_tree_is_carried_as_it_stands_when_head_alone_is_not_asked_for()
    {
        // The control: the same tree without the flag is the disk, so the case above cannot pass by
        // reading the disk either way.
        Committed();

        var carried = Archived(committedOnly: false, "");

        Assert.Equal("edited on disk", carried["edited.txt"]);
        Assert.Equal("staged only", carried["staged.txt"]);
        Assert.False(carried.ContainsKey("deleted.txt"));
        Assert.Equal("one", carried["artifacts/installer/Setup-1.0.exe"]);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void The_runner_builds_its_archive_through_the_function_the_suite_drives()
    {
        var runner = File.ReadAllText(Checkout.At("tools", "run-tests-vm.ps1"));

        Assert.Contains("New-CarriedArchive -Tree $script:Tree -Zip $zip -CommittedOnly:$CommittedOnly -Carried $carried", runner, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateEntryFromFile", runner, StringComparison.Ordinal);
    }

    /// <summary>
    /// A repository in the tree with HEAD holding two files, then one edited on disk, one deleted on
    /// disk and a third only staged. <c>artifacts\</c> stays untracked.
    /// </summary>
    private void Committed()
    {
        File.WriteAllText(Path.Combine(tree, "edited.txt"), "as committed");
        File.WriteAllText(Path.Combine(tree, "deleted.txt"), "as committed");

        Git("init", "-q");
        Git("add", "edited.txt", "deleted.txt");
        Git("-c", "user.name=winwright", "-c", "user.email=winwright@example.invalid", "commit", "-q", "-m", "head");

        File.WriteAllText(Path.Combine(tree, "edited.txt"), "edited on disk");
        File.Delete(Path.Combine(tree, "deleted.txt"));
        File.WriteAllText(Path.Combine(tree, "staged.txt"), "staged only");
        Git("add", "staged.txt");
    }

    private void Git(params string[] arguments)
    {
        var starting = new ProcessStartInfo("git")
        {
            WorkingDirectory = tree,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var one in arguments)
            starting.ArgumentList.Add(one);

        // core.autocrlf would rewrite nothing here, the files having no line ends, but it is pinned so
        // a machine's own setting cannot make the bytes compared below differ from what was written.
        starting.ArgumentList.Insert(0, "core.autocrlf=false");
        starting.ArgumentList.Insert(0, "-c");

        using var ran = Process.Start(starting)!;
        var wrong = ran.StandardError.ReadToEnd();
        ran.StandardOutput.ReadToEnd();
        ran.WaitForExit(30000);
        Assert.True(ran.ExitCode == 0, $"git {string.Join(' ', arguments)} failed: {wrong}");
    }

    /// <summary>What <c>New-CarriedArchive</c> wrote, by entry name and text, files only.</summary>
    private Dictionary<string, string> Archived(bool committedOnly, string carried)
    {
        var zip = Path.Combine(Path.GetTempPath(), $"winwright-carry-{Guid.NewGuid():N}.zip");
        try
        {
            var said = Powershell(
                $"New-CarriedArchive -Tree '{tree}' -Zip '{zip}' -CommittedOnly:${committedOnly.ToString().ToLowerInvariant()} -Carried @({(carried.Length == 0 ? "" : $"Get-Carried -Tree '{tree}' -Pattern @({carried}) | ForEach-Object {{ $_ -replace '\\\\', '/' }}")})");

            using var archive = System.IO.Compression.ZipFile.OpenRead(zip);
            var files = archive.Entries.Where(one => !one.FullName.EndsWith('/')).ToList();

            // The count the runner prints is the count of what is in the archive.
            Assert.Equal([files.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)], said);

            return files.ToDictionary(
                one => one.FullName,
                one =>
                {
                    using var reading = new StreamReader(one.Open());
                    return reading.ReadToEnd();
                });
        }
        finally
        {
            File.Delete(zip);
        }
    }

    /// <summary>
    /// What <c>Get-Carried</c> answers for <paramref name="patterns"/>, a PowerShell list as written,
    /// one line per file, or one line beginning REFUSED with what it threw.
    /// </summary>
    private List<string> Carried(string patterns) =>
        Powershell($"Get-Carried -Tree '{tree}' -Pattern @({patterns}) | ForEach-Object {{ Write-Output $_ }}");

    /// <summary>
    /// Run <paramref name="call"/> with <c>carry.ps1</c> defined, one line of output per line, or one
    /// line beginning REFUSED with what it threw.
    /// </summary>
    private static List<string> Powershell(string call)
    {
        var script = Path.Combine(Path.GetTempPath(), $"winwright-carry-{Guid.NewGuid():N}.ps1");
        File.WriteAllText(
            script,
            $$"""
            $ErrorActionPreference = 'Stop'
            . '{{Checkout.At("tools", "carry.ps1")}}' -DefineOnly
            try { {{call}} }
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

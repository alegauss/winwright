using System.Diagnostics;

using Microsoft.Win32;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW618. What a guest run declares it needs, read before the carry and refused up front.
/// <para>
/// Found by Modelado (MOD73): registering an unsigned MSIX layout met 0x80073CFF after the carry,
/// because Developer Mode was off in the guest and nothing said so. This drives the
/// <c>tools/guest-needs.ps1</c> the runner sends into the guest, pointed at a key of its own under
/// HKCU so the machine's setting is never read or touched. The reading in the guest itself is proven
/// by a guest run.
/// </para>
/// <para>
/// Serial because it starts a process, and needing no desk because that process is a windowless
/// PowerShell that reads the registry.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class GuestNeedsTests : IDisposable
{
    private readonly string key = $@"Software\winwright-tests\needs-{Guid.NewGuid():N}";

    public void Dispose() => Registry.CurrentUser.DeleteSubKeyTree(key, throwOnMissingSubKey: false);

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_need_the_guest_has_is_met()
    {
        Set(1);

        Assert.Equal(["met|developer-mode|AllowDevelopmentWithoutDevLicense is 1"], Read("developer-mode"));
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_need_switched_off_is_unmet_and_names_the_setting_to_change()
    {
        Set(0);

        var said = Assert.Single(Read("developer-mode"));
        Assert.StartsWith("unmet|developer-mode|AllowDevelopmentWithoutDevLicense is 0; turn on Developer Mode", said, StringComparison.Ordinal);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_need_never_set_is_unmet_rather_than_read_as_met()
    {
        // The control: a key with no value at all is the guest Modelado met, and a reading that
        // treated absent as fine would be the green this whole file exists to refuse.
        Registry.CurrentUser.CreateSubKey(key).Dispose();

        var said = Assert.Single(Read("developer-mode"));
        Assert.StartsWith("unmet|developer-mode|AllowDevelopmentWithoutDevLicense is not set;", said, StringComparison.Ordinal);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_name_it_does_not_know_is_refused_as_a_parameter()
    {
        var said = Powershell("Get-NeedNames -Need @('developer-mode, sideloading') | ForEach-Object { Write-Output $_ }");

        Assert.Equal(["REFUSED -Needs 'sideloading' is not a requirement this runner knows; it knows: developer-mode"], said);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void Names_bound_as_one_string_are_split_on_their_commas_and_counted_once()
    {
        var said = Powershell("Get-NeedNames -Need @('developer-mode,developer-mode') | ForEach-Object { Write-Output $_ }");

        Assert.Equal(["developer-mode"], said);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void The_runner_refuses_an_unknown_need_before_the_vm_and_reads_the_rest_before_the_carry()
    {
        var runner = File.ReadAllText(Checkout.At("tools", "run-tests-vm.ps1"));

        Assert.Contains("[string[]] $Needs", runner, StringComparison.Ordinal);

        var named = runner.IndexOf("Get-NeedNames -Need $Needs", StringComparison.Ordinal);
        var claimed = runner.IndexOf("Enter-DeskClaim -VmxPath $vmxPath", StringComparison.Ordinal);
        Assert.True(named > 0 && named < claimed, "an unknown need is not refused before the VM is touched");

        var read = runner.IndexOf("\"$script:GuestSync\\needs.ps1\"", named, StringComparison.Ordinal);
        var desk = runner.IndexOf("$desk = Read-GuestDesk -Vmx $vmxPath -Stage $stage", StringComparison.Ordinal);
        var carried = runner.IndexOf("New-CarriedArchive -Tree", StringComparison.Ordinal);
        Assert.True(read > claimed && read < desk && read < carried, "the needs are not read before the desk and the carry");
    }

    private void Set(int value)
    {
        using var opened = Registry.CurrentUser.CreateSubKey(key);
        opened.SetValue("AllowDevelopmentWithoutDevLicense", value, RegistryValueKind.DWord);
    }

    private List<string> Read(string name) =>
        Powershell($@"Test-Need -Name '{name}' -Key 'HKCU:\{key}' | ForEach-Object {{ Write-Output $_ }}");

    /// <summary>
    /// Run <paramref name="call"/> with <c>guest-needs.ps1</c> defined, one line of output per line,
    /// or one line beginning REFUSED with what it threw.
    /// </summary>
    private static List<string> Powershell(string call)
    {
        var script = Path.Combine(Path.GetTempPath(), $"winwright-needs-{Guid.NewGuid():N}.ps1");
        File.WriteAllText(
            script,
            $$"""
            $ErrorActionPreference = 'Stop'
            . '{{Checkout.At("tools", "guest-needs.ps1")}}' -DefineOnly
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

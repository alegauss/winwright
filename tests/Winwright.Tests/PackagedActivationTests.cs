using System.Diagnostics;

using Microsoft.Win32;

using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW619, its second half. A fixture that activates a file in a packaged application by its user
/// model ID, through the ProgID its package registered for the type.
/// <para>
/// Found by Modelado (MOD75, MOD78): its Store package declares a file type association, and the claim
/// that a model opened through it lands in the window could only be proven by a script beside the
/// case. Here this repository's fixture is laid out as an unsigned MSIX declaring a type of its own,
/// registered for this user with <c>Add-AppxPackage -Register</c>, and removed again afterwards. An
/// unsigned layout registers only where Developer Mode is on, which the guest runner's
/// <c>-Needs developer-mode</c> reads; a desk without it is excused by name rather than read as red.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class PackagedActivationTests : IDisposable
{
    /// <summary>A transparent 1x1 PNG, which is all a layout's logos have to be for it to register.</summary>
    private const string Logo =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

    private readonly string root = Directory.CreateTempSubdirectory("winwright-packaged-").FullName;
    private readonly string tag = Guid.NewGuid().ToString("N")[..10];
    private bool registered;

    private string Package => $"Winwright.Fixture.{tag}";

    private string Extension => $".wwpkg{tag}";

    public void Dispose()
    {
        if (registered)
            Powershell($"Get-AppxPackage -Name '{Package}' | Remove-AppxPackage");

        Directory.Delete(root, recursive: true);
    }

    [Theory]
    [Trait(NoDesk.Key, NoDesk.Free)]
    [InlineData("Modelado_8wekyb3d8bbwe!App", true)]
    [InlineData("Modelado.glb", false)]
    public void A_user_model_id_is_told_from_a_progid_by_its_bang(string through, bool packaged)
    {
        Assert.Equal(packaged, Activation.Packaged(through));

        // And it is accepted by a fixture now, where the first half refused it as not supported yet.
        Assert.Equal(through, FixtureDeclaration.Of("opened", activates: "cube.glb", activatesThrough: through).Through);
    }

    [Fact]
    public void A_file_activated_through_a_packages_user_model_id_opens_its_window()
    {
        if (!Desk.Read().CanObserve || BusyDesk.Excused(DeveloperMode()))
            return;

        var family = Registered();
        var verdict = Run($"{family}!Fixture", Extension);
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_user_model_id_whose_package_does_not_declare_the_type_is_refused_before_anything_starts()
    {
        if (!Desk.Read().CanObserve || BusyDesk.Excused(DeveloperMode()))
            return;

        var family = Registered();

        // The control: the same registered package, and a file of a type it never declared. The type
        // is given an association of its own, so the refusal is about the package and not the type.
        var other = $".wwother{tag}";
        using (Registry.CurrentUser.CreateSubKey($@"Software\Classes\{other}")) { }

        try
        {
            var refused = Assert.Throws<ScenarioRefusedException>(() => Run($"{family}!Fixture", other));

            Assert.Contains($"'{family}!Fixture' does not declare '{other}'", refused.Because, StringComparison.Ordinal);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{other}", throwOnMissingSubKey: false);
        }
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>
    /// Whether this desk has Developer Mode on, which an unsigned layout needs to register: met, or
    /// absent naming it, so a desk without it is excused by name rather than read as red.
    /// </summary>
    private static Precondition DeveloperMode()
    {
        using var unlock = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock");
        return unlock?.GetValue("AllowDevelopmentWithoutDevLicense") is 1
            ? Precondition.Met("Developer Mode")
            : Precondition.Absent("Developer Mode", "an unsigned MSIX layout registers only where it is on, and this desk has it off");
    }

    /// <summary>
    /// Lay the fixture out as an unsigned package declaring <see cref="Extension"/>, register it for
    /// this user, and answer its family name.
    /// </summary>
    private string Registered()
    {
        var layout = Path.Combine(root, "layout");
        var built = Path.GetDirectoryName(Fixture.Executable())!;
        foreach (var file in Directory.EnumerateFiles(built, "*", SearchOption.AllDirectories))
        {
            var to = Path.Combine(layout, Path.GetRelativePath(built, file));
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.Copy(file, to);
        }

        File.WriteAllBytes(Path.Combine(layout, "logo.png"), Convert.FromBase64String(Logo));
        File.WriteAllText(Path.Combine(layout, "AppxManifest.xml"), Manifest(Path.GetFileName(Fixture.Executable())));

        var said = Powershell(
            $"Add-AppxPackage -Register '{Path.Combine(layout, "AppxManifest.xml")}'; "
                + $"(Get-AppxPackage -Name '{Package}').PackageFamilyName");
        registered = true;

        var family = said.LastOrDefault(one => one.StartsWith(Package, StringComparison.Ordinal));
        Assert.True(family is not null, $"the layout did not register: {string.Join(" | ", said)}");
        return family!;
    }

    /// <summary>The package's manifest: the fixture as a full-trust desktop application declaring one type.</summary>
    private string Manifest(string executable) => $$"""
        <?xml version="1.0" encoding="utf-8"?>
        <Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
                 xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
                 xmlns:uap3="http://schemas.microsoft.com/appx/manifest/uap/windows10/3"
                 xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
                 IgnorableNamespaces="uap uap3 rescap">
          <Identity Name="{{Package}}" Publisher="CN=winwright" Version="1.0.0.0" ProcessorArchitecture="x64" />
          <Properties>
            <DisplayName>winwright fixture</DisplayName>
            <PublisherDisplayName>winwright</PublisherDisplayName>
            <Logo>logo.png</Logo>
          </Properties>
          <Dependencies>
            <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.26100.0" />
          </Dependencies>
          <Resources>
            <Resource Language="en-us" />
          </Resources>
          <Applications>
            <Application Id="Fixture" Executable="{{executable}}" EntryPoint="Windows.FullTrustApplication">
              <uap:VisualElements DisplayName="winwright fixture" Description="winwright fixture" BackgroundColor="transparent"
                                  Square150x150Logo="logo.png" Square44x44Logo="logo.png" />
              <Extensions>
                <uap3:Extension Category="windows.fileTypeAssociation">
                  <uap3:FileTypeAssociation Name="wwpkg{{tag}}" Parameters="--status &quot;--title=%1&quot;">
                    <uap:SupportedFileTypes>
                      <uap:FileType>{{Extension}}</uap:FileType>
                    </uap:SupportedFileTypes>
                  </uap3:FileTypeAssociation>
                </uap3:Extension>
              </Extensions>
            </Application>
          </Applications>
          <Capabilities>
            <rescap:Capability Name="runFullTrust" />
          </Capabilities>
        </Package>
        """;

    /// <summary>
    /// Read the fixture's heading in the window an activation of a file of <paramref name="type"/>
    /// through <paramref name="aumid"/> opens, under a project whose own executable is not there.
    /// </summary>
    private SuiteVerdict? Run(string aumid, string type)
    {
        File.WriteAllText(Path.Combine(root, $"cube{type}"), "a model");

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            """
            {
              "executable": "not-built.exe",
              "timeouts": { "launch": 30000, "resolve": 4000, "act": 4000, "poll": 25 }
            }
            """);

        var cases = $$"""
            {
              "fixtures": [ { "name": "opened in the package", "activates": "cube{{type}}", "through": "{{aumid}}" } ],
              "cases": [
                {
                  "name": "a model opened in the packaged application the way Open with does",
                  "catches": "a packaged file association no case could activate, so the activation itself stayed unread",
                  "fixture": "opened in the package",
                  "steps": [
                    { "locator": "Text#heading", "act": "read", "reads": "name", "expect": "winwright fixture", "named": "the window is the fixture's" }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        try
        {
            return Suite.Launch(ScenarioFile.Read("packaged.cases.json", cases), Selection.All, register, project);
        }
        finally
        {
            // Settled and not only stopped: the layout this ran from is deleted in Dispose, and Windows
            // will not delete a running image (WW201).
            Attachable.StopAndSettle(register);
        }
    }

    /// <summary>Run Windows PowerShell with <paramref name="command"/>, one line of output per line, standard error included.</summary>
    private static List<string> Powershell(string command)
    {
        var ran = Process.Start(new ProcessStartInfo("powershell.exe")
        {
            ArgumentList = { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-Command", command },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        })!;

        var said = ran.StandardOutput.ReadToEnd() + ran.StandardError.ReadToEnd();
        ran.WaitForExit(120000);
        return said.Split('\n').Select(one => one.Trim('\r', ' ')).Where(one => one.Length > 0).ToList();
    }
}
